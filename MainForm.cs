using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using System.Drawing.Imaging;
using System.Reflection;

// My classes
using static EditClipboardItems.ClipboardFormats;
using System.Globalization;
using System.Net.NetworkInformation;

namespace ClipboardManager
{
    public partial class MainForm : Form
    {
        private List<ClipboardItem> clipboardItems = new List<ClipboardItem>();
        private List<ClipboardItem> editedClipboardItems = new List<ClipboardItem>(); // Add this line

        private StreamWriter logFile;

        public static bool hasPendingChanges = false;

        // Dictionary of formats that can be synthesized from other formats, and which they can be synthesized to
        private static readonly Dictionary<uint, List<uint>> SynthesizedFormatsMap = new Dictionary<uint, List<uint>>()
        {
            { 2, new List<uint> { 8, 17 } }, // CF_BITMAP -> CF_DIB, CF_DIBV5
            { 8, new List<uint> { 2, 9, 17 } }, // CF_DIB -> CF_BITMAP, CF_PALETTE, CF_DIBV5
            { 17, new List<uint> { 2, 8, 9 } }, // CF_DIBV5 -> CF_BITMAP, CF_DIB, CF_PALETTE
            { 14, new List<uint> { 3 } }, // CF_ENHMETAFILE -> CF_METAFILEPICT
            { 3, new List<uint> { 14 } }, // CF_METAFILEPICT -> CF_ENHMETAFILE
            { 7, new List<uint> { 1, 13 } }, // CF_OEMTEXT -> CF_TEXT, CF_UNICODETEXT
            { 1, new List<uint> { 7, 13 } }, // CF_TEXT -> CF_OEMTEXT, CF_UNICODETEXT
            { 13, new List<uint> { 7, 1 } }, // CF_UNICODETEXT -> CF_OEMTEXT, CF_TEXT
        };

        // List of format names that are potentially synthesized by Windows and will be re-created if removed
        private static readonly List<string> SynthesizedFormatNames = new List<string>
        {
            "CF_LOCALE", // Not technically synthesized but will be re-created if CF_TEXT is set
            "CF_DIB",
            "CF_BITMAP",
            "CF_DIBV5",
            "CF_PALETTE",
            "CF_ENHMETAFILE",
            "CF_METAFILEPICT",
            "CF_OEMTEXT",
            "CF_TEXT",
            "CF_UNICODETEXT",
        };

        private bool IsFormatUsingHGlobal(uint format)
        {
            switch (format)
            {
                case 2:  // CF_BITMAP
                case 3:  // CF_METAFILEPICT
                case 14: // CF_ENHMETAFILE
                case 15: // CF_HDROP (Handle to an HDROP structure)
                case 9:  // Palette
                    return false;
                default:
                    return true;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dataGridViewClipboard.MouseWheel += dataGridViewClipboard_MouseWheel;
        }


        public MainForm()
        {
            InitializeComponent();
            InitializeLogging();
            InitializeDataGridView();
            UpdateToolLocations();

            // Initial tool settings
            dropdownContentsViewMode.SelectedIndex = 0; // Default index 0 is "Text" view mode

        }

        private void InitializeLogging()
        {
            string logPath = Path.Combine(Application.StartupPath, "clipboard_log.txt");
            logFile = new StreamWriter(logPath, true);
            logFile.AutoFlush = true;
        }


        private void InitializeDataGridView()
        {
            dataGridViewClipboard.Columns.Add("FormatName", "Format Name");
            dataGridViewClipboard.Columns.Add("FormatId", "Format ID");
            dataGridViewClipboard.Columns.Add("HandleType", "Handle Type");
            dataGridViewClipboard.Columns.Add("DataSize", "Data Size");
            dataGridViewClipboard.Columns.Add("DataInfo", "Data Info");
            dataGridViewClipboard.Columns.Add("TextPreview", "Text Preview");

            // Set autosize for all columns
            foreach (DataGridViewColumn column in dataGridViewClipboard.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }

            // Set default AutoSizeMode
            //dataGridViewClipboard.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewClipboard.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Set Resizable
            dataGridViewClipboard.Columns["FormatName"].Resizable = DataGridViewTriState.True;
            dataGridViewClipboard.Columns["TextPreview"].Resizable = DataGridViewTriState.True;
            dataGridViewClipboard.Columns["DataInfo"].Resizable = DataGridViewTriState.True;

            // Hide the row headers (the leftmost column)
            dataGridViewClipboard.RowHeadersVisible = false;

            // Add event handler for scroll wheel
            dataGridViewClipboard.MouseWheel += new MouseEventHandler(dataGridViewClipboard_MouseWheel);
        }

        private void dataGridViewClipboard_MouseWheel(object sender, MouseEventArgs e)
        {
            if (((HandledMouseEventArgs)e).Handled == true)
            {
                return;
            }
            //// Ensure the DataGridView has focus
            //if (!dataGridViewClipboard.Focused)
            //{
            //    dataGridViewClipboard.Focus();
            //}

            // Determine direction: -1 for up, 1 for down
            int direction = e.Delta > 0 ? -1 : 1;

            // Get current selected row index
            int currentIndex = dataGridViewClipboard.CurrentCell?.RowIndex ?? -1;

            if (currentIndex != -1)
            {
                // Calculate new index
                int newIndex = currentIndex + direction;

                // Ensure new index is within bounds
                int rowCount = dataGridViewClipboard.Rows.Count;
                if (newIndex < 0)
                {
                    newIndex = 0;
                }
                else if (newIndex >= rowCount)
                {
                    newIndex = rowCount - 1;
                }

                // If the index has changed, update selection
                if (newIndex != currentIndex)
                {
                    //dataGridViewClipboard.ClearSelection();
                    dataGridViewClipboard.Rows[newIndex].Selected = true;
                    dataGridViewClipboard.CurrentCell = dataGridViewClipboard.Rows[newIndex].Cells[0];

                    // Ensure the selected row is visible
                    dataGridViewClipboard.FirstDisplayedScrollingRowIndex = newIndex;
                }

                ChangeCellFocus(newIndex);

            }

            // Mark as handled because the event might get fired multiple times per scroll
            ((HandledMouseEventArgs)e).Handled = true;
        }

        // Update processedData grid view with clipboard contents during refresh
        private void UpdateClipboardItemsGridView(string formatName, string formatID, string handleType, string dataSize, string dataInfo, byte[] rawData)
        {
            // Preprocess certain info
            string textPreview = TryParseText(rawData, maxLength: 200, prefixEncodingType: false);
            string dataInfoString = dataInfo;

            if (string.IsNullOrEmpty(dataInfo))
            {
                dataInfoString = "N/A";
            }

            // Manually handle certain known formats
            if (formatName == "CF_LOCALE") 
            {
                textPreview = "";
            }

            // Add info to the grid view, then will be resized
            dataGridViewClipboard.Rows.Add(formatName, formatID, handleType, dataSize, dataInfoString, textPreview);

            // Temporarily set AutoSizeMode to calculate proper widths
            foreach (DataGridViewColumn column in dataGridViewClipboard.Columns)
            {
                // Manually set width to minimal 5 to be resized auto later. Apparently autosize will only make columns larger, not smaller
                column.Width = 5;

                if (column.Name != "TextPreview")
                {
                    // Use all cells instead of displayed cells, otherwise those scrolled out of view won't count
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }

                if (column.Name == "DataInfo" && string.IsNullOrEmpty(dataInfo))
                {
                    // Make this cell in this column gray text
                    dataGridViewClipboard.Rows[dataGridViewClipboard.Rows.Count - 1].Cells[column.Name].Style.ForeColor = Color.Gray;
                }
            }

            // Allow layout to update
            dataGridViewClipboard.PerformLayout();

            // Set final column properties
            foreach (DataGridViewColumn column in dataGridViewClipboard.Columns)
            {
                // Keep the TextPreview column as fill
                if (column.Name == "TextPreview")
                {
                    continue;
                }
                int originalWidth = column.Width;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.Resizable = DataGridViewTriState.True;

                if (column.Name == "FormatName")
                {
                    column.Width = originalWidth + 20; // Add some padding
                } else
                {
                    column.Width = originalWidth + 0; // For some reason this is necessary after setting resizable and autosize modes
                }
            }

            // Ensure TextPreview fills remaining space
            dataGridViewClipboard.Columns["TextPreview"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewClipboard.Columns["TextPreview"].Resizable = DataGridViewTriState.True;

            // If DataInfo is too long, manually set a max width
            if (dataGridViewClipboard.Columns["DataInfo"].Width > 200)
            {
                dataGridViewClipboard.Columns["DataInfo"].Width = 200;
            }

            // Reset selection to none
            dataGridViewClipboard.ClearSelection();
        }

        // Function to try and parse the raw data for text if it is text
        private string TryParseText(byte[] rawData, int maxLength = 150, bool prefixEncodingType = false)
        {
            if (rawData == null || rawData.Length == 0)
            {
                return "";
            }

            // Create encodings that throw on invalid bytes
            var utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var utf16Encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true);

            string utf8Result = "";
            string utf16Result = "";

            // Try UTF-8
            try
            {
                utf8Result = utf8Encoding.GetString(rawData);
            }
            catch (DecoderFallbackException)
            {
                // Invalid UTF-8, utf8Result remains empty
            }

            // Try UTF-16
            try
            {
                utf16Result = utf16Encoding.GetString(rawData);
            }
            catch (DecoderFallbackException)
            {
                // Invalid UTF-16, utf16Result remains empty
            }

            string result;
            bool likelyUTF16 = false;
            int nullCount = 0;
            double nullRatio = 0;

            // Improved UTF-16 detection
            if (!string.IsNullOrEmpty(utf16Result))
            {
                // Count the number of null characters in the UTF-8 result, indicating that it's likely UTF-16
                nullCount = utf8Result.Count(c => c == '\0');
                nullRatio = (double)nullCount / utf16Result.Length;

                // If more than some percentage of characters are null, it's likely UTF-16
                if (nullRatio > 0.80)
                {
                    likelyUTF16 = true;
                }
            }

            // Strip out null characters from both results. By now UTF-16 should not have any null characters since it's been decoded
            utf8Result = utf8Result.Replace("\0", "");
            utf16Result = utf16Result.Replace("\0", "");

            if (likelyUTF16 && !string.IsNullOrEmpty(utf16Result))
            {
                if (prefixEncodingType)
                {
                    result = "[UTF-16]  " + utf16Result;
                }
                else
                {
                    result = " " + utf16Result;
                }
            }
            else if (!string.IsNullOrEmpty(utf8Result))
            {
                if (prefixEncodingType)
                {
                    result = "[UTF-8]    " + utf8Result;
                }
                else
                {
                    result = " " + utf8Result;
                }
            }
            else
            {
                result = "";
            }

            // Truncate if necessary. Can be set to not truncate by setting maxLength to 0 or less
            if (maxLength > 0 && result.Length > maxLength)
            {
                result = result.Substring(0, maxLength) + "...";
            }

            return result;
        }



        private void MainForm_Resize(object sender, EventArgs e)
        {
            UpdateToolLocations();
        }

        //Function to fit processedData grid view to the form window
        private void UpdateToolLocations()
        {
            int titlebarAccomodate = 40;
            int splitterBorderAccomodate = 5;
            int bottomBuffer = 27; // Adjust this value to set the desired buffer size

            int splitterPanelsBottomPosition = this.Height - toolStrip1.Height - titlebarAccomodate;

            // Resize splitContainer1 to fit the form
            splitContainer1.Width = this.Width - 32;
            splitContainer1.Height = splitterPanelsBottomPosition - bottomBuffer;

            // Resize processedData grid within panel to match panel size
            dataGridViewClipboard.Width = splitContainer1.Panel1.Width - splitterBorderAccomodate;
            dataGridViewClipboard.Height = splitContainer1.Panel1.Height - splitterBorderAccomodate;
            richTextBoxContents.Width = splitContainer1.Panel2.Width - splitterBorderAccomodate;
            richTextBoxContents.Height = splitContainer1.Panel2.Height - splitterBorderAccomodate - bottomBuffer;

            // This is the original code outside the split panels
            //int dataGridBottomPosition = this.Height - toolStrip1.Height - richTextBoxContents.Height - titlebarAccomodate;
            // Resize dataGridViewClipboard to fit the form
            //dataGridViewClipboard.Width = this.Width - 40;
            //dataGridViewClipboard.Height = dataGridBottomPosition - bottomBuffer;
            //// Resize richTextBoxContents to fit the form
            //richTextBoxContents.Width = this.Width - 40;
            //richTextBoxContents.Location = new System.Drawing.Point(12, dataGridBottomPosition);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            RefreshClipboardItems();
        }

        private void RefreshClipboardItems()
        {
            Console.WriteLine("Starting RefreshClipboardItems");

            // Count the number of different data formats currently on the clipboard
            int formatCount = NativeMethods.CountClipboardFormats();
            Console.WriteLine($"Number of clipboard formats: {formatCount}");

            // Attempt to open the clipboard, retrying up to 10 times with a 10ms delay
            Console.WriteLine("Attempting to open clipboard");
            int retryCount = 10;  // Number of retries
            int retryDelay = 10;  // Delay in milliseconds
            bool clipboardOpened = false;

            for (int i = 0; i < retryCount; i++)
            {
                if (NativeMethods.OpenClipboard(this.Handle))
                {
                    clipboardOpened = true;
                    break;
                }
                System.Threading.Thread.Sleep(retryDelay);
            }

            if (!clipboardOpened)
            {
                Console.WriteLine("Failed to open clipboard");
                MessageBox.Show("Failed to open clipboard.");
                return;
            }

            try
            {
                CopyClipboardData();
            }
            finally
            {
                Console.WriteLine("Closing clipboard");
                NativeMethods.CloseClipboard();
            }

            DetermineSynthesizedFormats();
            ProcessClipboardData();
            CloneClipboardItemsToEditedVariable(); // Clone clipboardItems to editedClipboardItems

            Console.WriteLine("RefreshClipboardItems completed");
        }

        private void CloneClipboardItemsToEditedVariable()
        {
            editedClipboardItems = clipboardItems.Select(item => (ClipboardItem)item.Clone()).ToList();
        }


        private void CopyClipboardData()
        {
            clipboardItems.Clear();

            uint format = 0;
            while ((format = NativeMethods.EnumClipboardFormats(format)) != 0)
            {
                string formatName = GetClipboardFormatName(format);
                Console.WriteLine($"Processing format: {format} ({formatName})");

                IntPtr hData = NativeMethods.GetClipboardData(format);
                if (hData == IntPtr.Zero)
                {
                    Console.WriteLine($"GetClipboardData returned null for format {format}");
                    continue;
                }

                ulong dataSize = 0;
                byte[] rawData = null;

                try
                {
                    if (IsFormatUsingHGlobal(format))
                    {
                        IntPtr pData = NativeMethods.GlobalLock(hData);
                        if (pData != IntPtr.Zero)
                        {
                            try
                            {
                                dataSize = (ulong)NativeMethods.GlobalSize(hData).ToUInt64();
                                rawData = new byte[dataSize];
                                Marshal.Copy(pData, rawData, 0, (int)dataSize);
                            }
                            finally
                            {
                                NativeMethods.GlobalUnlock(hData);
                            }
                        }
                    }
                    else if (format == 2) // CF_BITMAP
                    {
                        // Handle CF_BITMAP
                        IntPtr hBitmap = hData; // hData is the HBITMAP handle

                        using (Bitmap bitmap = Image.FromHbitmap(hBitmap))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bitmap.Save(ms, ImageFormat.Bmp);
                                rawData = ms.ToArray();
                                dataSize = (ulong)rawData.Length;
                            }
                        }
                    }
                    else
                    {
                        // Handle other formats appropriately
                        dataSize = 0; // Size may not be applicable
                        rawData = null; // Data extraction not performed here
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing format {format}: {ex.Message}");
                }

                var item = new ClipboardItem
                {
                    FormatName = formatName,
                    FormatId = format,
                    Handle = hData,
                    DataSize = dataSize,
                    RawData = rawData,
                    Data = rawData
                };

                clipboardItems.Add(item);
            }
        }


        private void ProcessClipboardData()
        {
            dataGridViewClipboard.Rows.Clear();

            foreach (var item in clipboardItems)
            {
                List<string> dataInfoList = new List<string>();
                byte[] processedData = item.RawData;

                switch (item.FormatId)
                {
                    case 1: // CF_TEXT
                        //Console.WriteLine("Processing CF_TEXT");
                        processedData = ProcessCFText(item.RawData);
                        string ansiText = Encoding.Default.GetString(processedData);
                        int asciiTextLength = ansiText.Length;
                        dataInfoList.Add($"Encoding: ANSI");
                        dataInfoList.Add($"Chars: {asciiTextLength}");
                        //dataInfo = $"Encoding: ASCII, Chars: {asciiTextLength}";
                        
                        break;

                    case 13: // CF_UNICODETEXT
                        //Console.WriteLine("Processing CF_UNICODETEXT");
                        string unicodeText = Encoding.Unicode.GetString(item.RawData);
                        int unicodeTextLength = unicodeText.Length;
                        dataInfoList.Add($"Encoding: Unicode");
                        dataInfoList.Add($"Chars: {unicodeTextLength}");
                        processedData = Encoding.Unicode.GetBytes(unicodeText);
                        break;

                    case 2: // CF_BITMAP
                        //Console.WriteLine("Processing CF_BITMAP");
                        using (Bitmap bmp = Bitmap.FromHbitmap(item.Handle))
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                                processedData = ms.ToArray();
                                item.RawData = processedData;
                                item.DataSize = (ulong)processedData.Length;

                                dataInfoList = new List<string>
                                {
                                    $"Size: {bmp.Width}x{bmp.Height}",
                                    $"Format: {bmp.PixelFormat}"
                                };
                            }
                        }
                        break;


                    case 8: // CF_DIB
                    case 17: // CF_DIBV5
                        //Console.WriteLine($"Processing bitmap format: {(selectedItem.FormatId == 8 ? "CF_DIB" : "CF_DIBV5")}");
                        dataInfoList.Add($"Format: {item.FormatName}");
                        dataInfoList.Add($"Size: {item.DataSize} bytes");
                        break;

                    case 15: // CF_HDROP
                        //Console.WriteLine("Processing CF_HDROP");
                        uint fileCount = NativeMethods.DragQueryFile(item.Handle, 0xFFFFFFFF, null, 0);
                        StringBuilder fileNames = new StringBuilder();
                        for (uint i = 0; i < fileCount; i++)
                        {
                            StringBuilder fileName = new StringBuilder(260);
                            NativeMethods.DragQueryFile(item.Handle, i, fileName, (uint)fileName.Capacity);
                            fileNames.AppendLine(fileName.ToString());
                        }
                        dataInfoList.Add($"File Drop: {fileCount} file(s)");
                        break;

                    case 16: // CF_LOCALE
                        //Console.WriteLine("Processing CF_LOCALE");
                        dataInfoList.Add(ProcessCFLocale(item.RawData));
                        break;

                    // Add more cases for other formats as needed...

                    default:
                        //Console.WriteLine($"Processing unknown format: {selectedItem.FormatId}");
                        dataInfoList.Add("");
                        break;
                }

                item.Data = processedData; // Update the processed data in the selectedItem
                item.DataInfoList = dataInfoList; // Update the data info in the selectedItem
                string handleType = item.AssumedSynthesized ? "Synthesized" : "Standard"; // Determine handle type

                UpdateClipboardItemsGridView(formatName: item.FormatName, formatID: item.FormatId.ToString(), handleType: handleType, dataSize: item.DataSize.ToString(), dataInfo: item.DataInfoString, rawData: item.RawData);
            }
        }

        private void DetermineSynthesizedFormats()
        {
            List<uint> formatOrder = clipboardItems.Select(item => item.FormatId).ToList();
            HashSet<uint> synthesizeTargets = new HashSet<uint>(SynthesizedFormatsMap.SelectMany(kvp => kvp.Value));

            for (int i = formatOrder.Count - 1; i >= 0; i--)
            {
                uint currentFormat = formatOrder[i];

                // Check for special cases first
                if (currentFormat == 16) // CF_LOCALE - Auto created if CF_TEXT is set and CF_LOCALE doesn't exist already
                {
                    ClipboardItem item = clipboardItems.Find(ci => ci.FormatId == currentFormat);
                    if (item != null)
                    {
                        item.AssumedSynthesized = true;
                    }
                    continue;
                }

                // If the current format is not a potential synthesized format, stop the loop
                if (!synthesizeTargets.Contains(currentFormat))
                {
                    break;
                }

                foreach (var kvp in SynthesizedFormatsMap)
                {
                    if (kvp.Value.Contains(currentFormat))
                    {
                        bool isSynthesized = false;

                        // Check previous formats to determine if they are the origin format
                        for (int j = i - 1; j >= 0; j--)
                        {
                            uint potentialOriginFormat = formatOrder[j];

                            if (kvp.Key == potentialOriginFormat)
                            {
                                // Mark as synthesized since we found an origin format before the current format
                                isSynthesized = true;
                                break;
                            }
                        }

                        if (isSynthesized)
                        {
                            ClipboardItem item = clipboardItems.Find(ci => ci.FormatId == currentFormat);
                            if (item != null)
                            {
                                item.AssumedSynthesized = true;
                            }
                        }
                    }
                }
            }
        }
        
        // Convert to ASCII Bytes
        private byte[] ProcessCFText(byte[] data)
        {
            // Use Windows-1252 encoding (commonly referred to as ANSI in Windows)
            Encoding ansiEncoding = Encoding.GetEncoding(1252);

            // Convert bytes to string, stopping at the first null character
            string text = "";
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0) break; // Stop at null terminator
                text += (char)data[i];
            }

            return ansiEncoding.GetBytes(text);
        }

        private string ProcessCFLocale(byte[] rawBytes)
        {
            string dataInfo;
            if (rawBytes.Length >= 4)
            {
                int lcid = BitConverter.ToInt32(rawBytes, 0);
                try
                {
                    CultureInfo culture = new CultureInfo(lcid);
                    dataInfo = $"Locale: {culture.Name} (LCID: {lcid})";
                }
                catch (CultureNotFoundException)
                {
                    dataInfo = $"Unknown Locale (LCID: {lcid})";
                }
            }
            else
            {
                dataInfo = "Invalid CF_LOCALE data";
            }
            return dataInfo;
        }

        private string GetClipboardFormatName(uint format)
        {
            StringBuilder formatName = new StringBuilder(256);
            return NativeMethods.GetClipboardFormatName(format, formatName, formatName.Capacity) > 0
                ? formatName.ToString()
                : GetStandardFormatName(format);
        }


        private bool RemoveClipboardFormat(uint formatToRemove)
        {
            Console.WriteLine($"Attempting to remove format: {formatToRemove}");

            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                Console.WriteLine("Failed to open clipboard.");
                MessageBox.Show("Failed to open clipboard.");
                return false;
            }

            try
            {
                List<ClipboardFormatData> formatsToKeep = new List<ClipboardFormatData>();
                uint format = 0;
                while ((format = NativeMethods.EnumClipboardFormats(format)) != 0)
                {
                    Console.WriteLine($"Processing format: {format}");
                    if (format != formatToRemove)
                    {
                        IntPtr hData = NativeMethods.GetClipboardData(format);
                        if (hData != IntPtr.Zero)
                        {
                            UIntPtr size;
                            IntPtr hGlobal;

                            // Special handling for CF_BITMAP and other problematic formats
                            if (format == 2 || format == 3 || format == 8 || format == 14 || format == 17)
                            {
                                Console.WriteLine($"Special handling for format: {format}");
                                size = UIntPtr.Zero;
                                hGlobal = CopySpecialFormat(format, hData);
                            }
                            else
                            {
                                size = NativeMethods.GlobalSize(hData);
                                Console.WriteLine($"Format {format} size: {size}");
                                hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, size);
                                if (hGlobal != IntPtr.Zero)
                                {
                                    IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                                    IntPtr pData = NativeMethods.GlobalLock(hData);
                                    if (pGlobal != IntPtr.Zero && pData != IntPtr.Zero)
                                    {
                                        Console.WriteLine($"Copying data for format: {format}");
                                        NativeMethods.CopyMemory(pGlobal, pData, size);
                                        NativeMethods.GlobalUnlock(hData);
                                        NativeMethods.GlobalUnlock(hGlobal);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Failed to lock memory for format: {format}");
                                        NativeMethods.GlobalFree(hGlobal);
                                        hGlobal = IntPtr.Zero;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to allocate memory for format: {format}");
                                }
                            }

                            if (hGlobal != IntPtr.Zero)
                            {
                                formatsToKeep.Add(new ClipboardFormatData { Format = format, Data = hGlobal });
                                Console.WriteLine($"Added format {format} to keep list");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"GetClipboardData returned null for format: {format}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipping format to remove: {format}");
                    }
                }

                Console.WriteLine("Emptying clipboard");
                NativeMethods.EmptyClipboard();

                Console.WriteLine("Setting new clipboard data");
                foreach (var item in formatsToKeep)
                {
                    Console.WriteLine($"Setting data for format: {item.Format}");
                    NativeMethods.SetClipboardData(item.Format, item.Data);
                }

                Console.WriteLine("Clipboard format removal completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing clipboard format: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error removing clipboard format: {ex.Message}");
                return false;
            }
            finally
            {
                Console.WriteLine("Closing clipboard");
                NativeMethods.CloseClipboard();
            }
        }

        private IntPtr CopySpecialFormat(uint format, IntPtr hData)
        {
            switch (format)
            {
                case 2: // CF_BITMAP
                    return CopyBitmap(hData);
                case 3: // CF_METAFILEPICT
                case 14: // CF_ENHMETAFILE
                    return CopyMetafile(format, hData);
                case 8: // CF_DIB
                case 17: // CF_DIBV5
                    return CopyDIB(hData);
                default:
                    Console.WriteLine($"Unexpected special format: {format}");
                    return IntPtr.Zero;
            }
        }

        private IntPtr CopyBitmap(IntPtr hBitmap)
        {
            BITMAP bmp = new BITMAP();
            NativeMethods.GetObject(hBitmap, Marshal.SizeOf(typeof(BITMAP)), ref bmp);

            IntPtr hBitmapCopy = NativeMethods.CreateBitmap(bmp.bmWidth, bmp.bmHeight, bmp.bmPlanes, bmp.bmBitsPixel, IntPtr.Zero);

            IntPtr hdcScreen = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr hdcSrc = NativeMethods.CreateCompatibleDC(hdcScreen);
            IntPtr hdcDest = NativeMethods.CreateCompatibleDC(hdcScreen);

            IntPtr hOldSrcBitmap = NativeMethods.SelectObject(hdcSrc, hBitmap);
            IntPtr hOldDestBitmap = NativeMethods.SelectObject(hdcDest, hBitmapCopy);

            NativeMethods.BitBlt(hdcDest, 0, 0, bmp.bmWidth, bmp.bmHeight, hdcSrc, 0, 0, 0x00CC0020 /* SRCCOPY */);

            NativeMethods.SelectObject(hdcSrc, hOldSrcBitmap);
            NativeMethods.SelectObject(hdcDest, hOldDestBitmap);
            NativeMethods.DeleteDC(hdcSrc);
            NativeMethods.DeleteDC(hdcDest);
            NativeMethods.ReleaseDC(IntPtr.Zero, hdcScreen);

            return hBitmapCopy;
        }

        private IntPtr CopyMetafile(uint format, IntPtr hMetafile)
        {
            if (format == 3) // CF_METAFILEPICT
            {
                // Implementation for CF_METAFILEPICT
                // This is more complex and requires additional Windows API calls
                Console.WriteLine("CF_METAFILEPICT copying not implemented");
                return IntPtr.Zero;
            }
            else // CF_ENHMETAFILE
            {
                return NativeMethods.CopyEnhMetaFile(hMetafile, null);
            }
        }

        private IntPtr CopyDIB(IntPtr hDib)
        {
            UIntPtr size = NativeMethods.GlobalSize(hDib);
            IntPtr hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, size);
            if (hGlobal != IntPtr.Zero)
            {
                IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                IntPtr pData = NativeMethods.GlobalLock(hDib);
                if (pGlobal != IntPtr.Zero && pData != IntPtr.Zero)
                {
                    NativeMethods.CopyMemory(pGlobal, pData, size);
                    NativeMethods.GlobalUnlock(hDib);
                    NativeMethods.GlobalUnlock(hGlobal);
                }
                else
                {
                    NativeMethods.GlobalFree(hGlobal);
                    hGlobal = IntPtr.Zero;
                }
            }
            return hGlobal;
        }
        internal class ClipboardFormatData
        {
            public uint Format { get; set; }
            public IntPtr Data { get; set; }
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (logFile != null)
            {
                logFile.Close();
                logFile.Dispose();
            }
        }

        private string GetStandardFormatName(uint format)
        {
            switch (format)
            {
                case 1: return "CF_TEXT";
                case 2: return "CF_BITMAP";
                case 3: return "CF_METAFILEPICT";
                case 4: return "CF_SYLK";
                case 5: return "CF_DIF";
                case 6: return "CF_TIFF";
                case 7: return "CF_OEMTEXT";
                case 8: return "CF_DIB";
                case 9: return "CF_PALETTE";
                case 10: return "CF_PENDATA";
                case 11: return "CF_RIFF";
                case 12: return "CF_WAVE";
                case 13: return "CF_UNICODETEXT";
                case 14: return "CF_ENHMETAFILE";
                case 15: return "CF_HDROP";
                case 16: return "CF_LOCALE";
                case 17: return "CF_DIBV5";
                case 0x0080: return "CF_OWNERDISPLAY";
                case 0x0081: return "CF_DSPTEXT";
                case 0x0082: return "CF_DSPBITMAP";
                case 0x0083: return "CF_DSPMETAFILEPICT";
                case 0x008E: return "CF_DSPENHMETAFILE";
            }

            if (format >= 0x0200 && format <= 0x02FF)
            {
                return $"CF_PRIVATEFIRST-CF_PRIVATELAST ({format:X4})";
            }

            if (format >= 0x0300 && format <= 0x03FF)
            {
                return $"CF_GDIOBJFIRST-CF_GDIOBJLAST ({format:X4})";
            }

            return $"Unknown Format ({format:X4})";
        }


        private void dataGridViewClipboard_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            ChangeCellFocus(e.RowIndex);
            UpdateEditControlsVisibility();
        }

        private void ChangeCellFocus(int rowIndex)
        {
            if (rowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.Rows[rowIndex];
                if (uint.TryParse(selectedRow.Cells["FormatId"].Value.ToString(), out uint formatId))
                {
                    ClipboardItem item = editedClipboardItems.Find(i => i.FormatId == formatId); // Use editedClipboardItems

                    if (item == null)
                    {
                        return;
                    }

                    richTextBoxContents.Clear();
                    DisplayClipboardData(item);

                    // Check if it's a synthesized name in SynthesizedFormatNames and show a warning
                    if (SynthesizedFormatNames.Contains(item.FormatName))
                    {
                        labelSynthesizedTypeWarn.Visible = true;
                    }
                    else
                    {
                        labelSynthesizedTypeWarn.Visible = false;
                    }
                }
            }
        }


        private void DisplayClipboardData(ClipboardItem item)
        {
            if (item == null || item.RawData == null)
            {
                richTextBoxContents.Text = "Data not available";
                richTextBoxContents.ForeColor = Color.Red;
                return;
            }

            int modeIndex = dropdownContentsViewMode.SelectedIndex;

            // For data larger than 50K, display a warning and don't display the data unless the checkbox is checked
            if (modeIndex != 3 && item.RawData.Length > 50000)
            {
                if (!menuOptions_ShowLargeHex.Checked)
                {
                    richTextBoxContents.Text = "Data is too large to display preview.\nThis can be changed in the options menu, but the program may freeze for large amounts of data.";
                    // Set color to red
                    richTextBoxContents.ForeColor = Color.Red;
                    return;
                }
            }

            // Set color to black for default
            richTextBoxContents.ForeColor = Color.Black;


            switch (modeIndex)
            {
                case 0: // Text view mode
                    richTextBoxContents.Text = TryParseText(item.RawData, maxLength: 0, prefixEncodingType: false);
                    richTextBoxContents.ReadOnly = true;
                    break;

                case 1: // Hex view mode
                    richTextBoxContents.Text = BitConverter.ToString(item.RawData).Replace("-", " ");
                    richTextBoxContents.ReadOnly = true;
                    break;

                case 2: // Hex (Editable) view mode
                    richTextBoxContents.Text = BitConverter.ToString(item.RawData).Replace("-", " ");
                    richTextBoxContents.ReadOnly = false;
                    break;
                case 3: // Object / Struct View
                    richTextBoxContents.Text = FormatInspector.InspectFormat(formatName: GetStandardFormatName(item.FormatId), data: item.RawData, fullItem: item, allowLargeHex: menuOptions_ShowLargeHex.Checked);
                    richTextBoxContents.ReadOnly = true;
                    break;

                default:
                    richTextBoxContents.Text = "Unknown view mode";
                    break;
            }
        }


        private void SaveClipboardData()
        {
            try
            {
                // First, process the edited hex string of the actively selected clipboard selectedItem
                foreach (var item in editedClipboardItems)
                {
                    // Check if selectedItem matches currently selected clipboard selectedItem
                    if (item.FormatId != GetSelectedClipboardItemObject().FormatId)
                    {
                        continue;
                    }
                    // Check if the selectedItem is in hex mode
                    if (dropdownContentsViewMode.SelectedIndex != 2)
                    {
                        MessageBox.Show("To update the clipboard, you must be in hex edit mode for the selected item.");
                        return;
                    }

                    string hexString = richTextBoxContents.Text.Replace(" ", "");
                    byte[] rawData = Enumerable.Range(0, hexString.Length)
                        .Where(x => x % 2 == 0)
                        .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                        .ToArray();
                    item.RawData = rawData;
                    item.DataSize = (ulong)rawData.Length;
                }

                // Now, save the processed data to the clipboard
                if (NativeMethods.OpenClipboard(this.Handle))
                {
                    NativeMethods.EmptyClipboard();

                    foreach (var item in editedClipboardItems)
                    {
                        if (item.RawData != null && item.RawData.Length > 0)
                        {
                            IntPtr hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)item.RawData.Length);
                            if (hGlobal != IntPtr.Zero)
                            {
                                IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                                if (pGlobal != IntPtr.Zero)
                                {
                                    try
                                    {
                                        Marshal.Copy(item.RawData, 0, pGlobal, item.RawData.Length);
                                    }
                                    finally
                                    {
                                        NativeMethods.GlobalUnlock(hGlobal);
                                    }

                                    if (NativeMethods.SetClipboardData(item.FormatId, hGlobal) == IntPtr.Zero)
                                    {
                                        NativeMethods.GlobalFree(hGlobal);
                                        Console.WriteLine($"Failed to set clipboard data for format: {item.FormatId}");
                                    }
                                }
                                else
                                {
                                    NativeMethods.GlobalFree(hGlobal);
                                    Console.WriteLine($"Failed to lock memory for format: {item.FormatId}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Failed to allocate memory for format: {item.FormatId}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No data to set for format: {item.FormatId}");
                        }
                    }

                    NativeMethods.CloseClipboard();
                    MessageBox.Show("Clipboard data saved successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to open clipboard.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save clipboard data: {ex.Message}");
            }
        }


        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            int selectedFormatId = -1;
            // New scope, only need item for this operation
            {
                ClipboardItem item = GetSelectedClipboardItemObject();
                if (item != null)
                {
                    selectedFormatId = (int)item.FormatId;
                }
            }

            RefreshClipboardItems();
            hasPendingChanges = false;

            // If the new clipboard data contains the same format as the previously selected item, re-select it
            if (selectedFormatId > 0 && clipboardItems != null && clipboardItems.Any(ci => ci.FormatId == selectedFormatId))
            {
                // If format id is still in the new clipboard, select it
                int rowIndex = dataGridViewClipboard.Rows.Cast<DataGridViewRow>().ToList().FindIndex(r => r.Cells["FormatId"].Value.ToString() == selectedFormatId.ToString());
                if (rowIndex >= 0)
                {
                    dataGridViewClipboard.Rows[rowIndex].Selected = true;
                    dataGridViewClipboard.FirstDisplayedScrollingRowIndex = rowIndex;
                }
            }
            UpdateEditControlsVisibility();
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.SelectedRows[0];
                if (uint.TryParse(selectedRow.Cells["FormatId"].Value.ToString(), out uint formatIdToRemove))
                {
                    //LogClipboardContents("Clipboard contents before removal:");
                    if (RemoveClipboardFormat(formatIdToRemove))
                    {
                        //LogClipboardContents("Clipboard contents after removal:");
                        //MessageBox.Show($"Format {formatIdToRemove} removed successfully.");
                    }
                    else
                    {
                        MessageBox.Show($"Failed to remove format {formatIdToRemove}.");
                    }
                    RefreshClipboardItems();
                }
                else
                {
                    MessageBox.Show("Unable to determine the format ID of the selected item.");
                }
            }
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // Resize processedData grid view to fit the form window
            UpdateToolLocations();
        }

        private void dropdownContentsViewMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Indexes:
            // 0: Text
            // 1: Hex
            // 2: Hex (Editable)
            // 3: Object / Struct View

            ClipboardItem item = GetSelectedClipboardItemObject();
            if (item == null)
            {
                return;
            }

            DisplayClipboardData(item);

            // Show buttons and labels for edited mode
            UpdateEditControlsVisibility();
        }

        private void UpdateEditControlsVisibility(ClipboardItem selectedItem = null, ClipboardItem selectedEditedItem = null)
        {
            if (selectedItem == null)
            {
                selectedItem = GetSelectedClipboardItemObject();
            }
            if (selectedEditedItem == null)
            {
                selectedEditedItem = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
            }

            // Visibility updates to make regardless of view mode and selected item
            if (hasPendingChanges)
            {
                labelPendingChanges.Visible = true;
            }
            else
            {
                labelPendingChanges.Visible = false;
                // Reset all row colors to black
                foreach (DataGridViewRow row in dataGridViewClipboard.Rows)
                {
                    row.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                    row.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
                }
            }

            // Beyond here, we need a selected item. If there isn't one, set some buttons that require a selectedItem to be disabled
            if (selectedItem == null || selectedEditedItem == null)
            {
                buttonResetEdit.Enabled = false;
                buttonApplyEdit.Enabled = false;
                menuEdit_CopyEditedHexAsText.Enabled = false;
                return;
            }

            // For any items in editedClipboardItems that has pending changes, make its row text color red
            foreach (var editedItem in editedClipboardItems)
            {
                if (editedItem.HasPendingEdit)
                {
                    int rowIndex = dataGridViewClipboard.Rows.Cast<DataGridViewRow>().ToList().FindIndex(r => r.Cells["FormatId"].Value.ToString() == editedItem.FormatId.ToString());
                    if (rowIndex >= 0)
                    {
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Red;
                        // Also text color while selected
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.SelectionForeColor = Color.Yellow;
                    }
                }
            }

            // Updates based on selected selectedItem only, regardless of view mode
            if (selectedEditedItem.HasPendingEdit)
            {
                buttonResetEdit.Enabled = true;
                menuEdit_CopyEditedHexAsText.Enabled = true;
            }
            else
            {
                buttonResetEdit.Enabled = false;
                menuEdit_CopyEditedHexAsText.Enabled = false;
            }

            // Show apply edit button if the selectedItem is in hex edit mode
            if (dropdownContentsViewMode.SelectedIndex == 2)
            {
                buttonApplyEdit.Enabled = true;
                buttonApplyEdit.Visible = true;
            }
            else
            {
                buttonApplyEdit.Enabled = false;
                buttonApplyEdit.Visible = false;
            }

        }

        private void toolStripButtonExportSelected_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToExport = GetSelectedClipboardItemObject();

            if (itemToExport == null)
            {
                return;
            }
            string nameStem = itemToExport.FormatName;

            // If it's a DIBV5 format, convert it to a bitmap
            if (itemToExport.FormatId == 17)
            {
                Bitmap bitmap = CF_DIBV5ToBitmap(itemToExport.Data);

                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);
                    return;
                }
            }
            else if (itemToExport.FormatId == 8) // CF_DIB
            {
                Bitmap bitmap = CF_DIBToBitmap(itemToExport.Data);
                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);
                    return;
                }
            }
            else if (itemToExport.FormatId == 2) // CF_BITMAP
            {
                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    // Assuming itemToCopy.Data contains the raw bitmap data
                    using (MemoryStream ms = new MemoryStream(itemToExport.Data))
                    {
                        using (Bitmap bitmap = new Bitmap(ms))
                        {
                            bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);
                            return;
                        }
                    }
                }
            }

            string[] knownFormatExtensions = new string[] { "PNG" };
            string fileExt = "dat"; // Default extension if not in the list of known formats

            // Just export the raw data as a file. If it's in the list of known formats where the raw data is the actual file data, and the extension matches the format name, use that extension
            if (knownFormatExtensions.Contains(nameStem.ToUpper()))
            {
                fileExt = nameStem;
                nameStem = "Clipboard";
            }

            SaveFileDialog saveRawFileDialogResult = SaveFileDialog(extension: fileExt, defaultFileNameStem: nameStem);
            if (saveRawFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(saveRawFileDialogResult.FileName, itemToExport.Data);
            }
        }

        // Function to display save dialog and save the clipboard data to a file
        private SaveFileDialog SaveFileDialog(string extension = "dat", string defaultFileNameStem = "clipboard_data")
        {
            if (string.IsNullOrEmpty(extension))
            {
                extension = "dat";
            }

            string defaultFileName = $"{defaultFileNameStem}.{extension}";
            
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FileName = defaultFileName;

            return saveFileDialog;
        }

        private ClipboardItem GetSelectedClipboardItemObject(bool returnEditedItemVersion = false)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.SelectedRows[0];
                if (uint.TryParse(selectedRow.Cells["FormatId"].Value.ToString(), out uint formatId))
                {
                    if (returnEditedItemVersion)
                    {
                        return editedClipboardItems.Find(i => i.FormatId == formatId);
                    }
                    else
                    {
                        return clipboardItems.Find(i => i.FormatId == formatId);
                    }
                    
                }
            }
            return null;
        }

        private Dictionary<string, string> GetDataGridItemContents()
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.SelectedRows[0];
                return new Dictionary<string, string>
                {
                    ["FormatName"] = selectedRow.Cells["FormatName"].Value.ToString(),
                    ["FormatId"] = selectedRow.Cells["FormatId"].Value.ToString(),
                    ["HandleType"] = selectedRow.Cells["HandleType"].Value.ToString(),
                    ["DataSize"] = selectedRow.Cells["DataSize"].Value.ToString(),
                    ["DataInfo"] = selectedRow.Cells["DataInfo"].Value.ToString()
                };
            }
            else
            {
                return null;
            }
        }

        private void toolStripButtonSaveEdited_Click(object sender, EventArgs e)
        {
            SaveClipboardData();
            RefreshClipboardItems();
            hasPendingChanges = false;
            UpdateEditControlsVisibility();
        }

        private void menuFile_ExportAsRawHex_Click(object sender, EventArgs e)
        {
            ClipboardItem itemToExport = GetSelectedClipboardItemObject();
            if (itemToExport == null)
            {
                return;
            }

            string nameStem = itemToExport.FormatName + "_RawHex";
            SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "txt", defaultFileNameStem: nameStem);
            if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                // Get the hex information
                string data = BitConverter.ToString(itemToExport.RawData).Replace("-", " ");
                // Save the data to a file
                File.WriteAllText(saveFileDialogResult.FileName, data);
            }
        }

        private void menuItem_ExportSelectedStruct_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToExport = GetSelectedClipboardItemObject();
            if (itemToExport == null)
            {
                return;
            }
            string nameStem = itemToExport.FormatName + "_StructInfo";
            SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "txt", defaultFileNameStem: nameStem);
            if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                // Get the hex information
                string data = FormatInspector.InspectFormat(formatName: GetStandardFormatName(itemToExport.FormatId), data: itemToExport.RawData, fullItem: itemToExport, allowLargeHex: menuOptions_ShowLargeHex.Checked);
                // TO DO - Export details of each object in the struct

                // Save the data to a file
                File.WriteAllText(saveFileDialogResult.FileName, data);
            }

            //// If it's DIBV5 format use special hex conversion
            //if (itemToCopy.FormatId == 17)
            //{
            //    string hexString = CF_DIBV5ToHex(itemToCopy.Data);
            //    SaveFileDialog saveFileDialogResult = SaveFileDialog();
            //    if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            //    {
            //        File.WriteAllText(saveFileDialogResult.FileName, hexString);
            //    }
            //}
        }

        private void menuItem_ExportSelectedAsFile_Click(object sender, EventArgs e)
        {
            toolStripButtonExportSelected_Click(null, null);
        }


        public static Bitmap CF_DIBV5ToBitmap(byte[] data)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(BITMAPV5HEADER));

                int width = Math.Abs(bmi.bV5Width);  // Ensure positive width
                int height = Math.Abs(bmi.bV5Height); // Ensure positive height
                PixelFormat pixelFormat;

                switch (bmi.bV5BitCount)
                {
                    case 24:
                        pixelFormat = PixelFormat.Format24bppRgb;
                        break;
                    case 32:
                        pixelFormat = PixelFormat.Format32bppArgb;
                        break;
                    default:
                        throw new NotSupportedException($"Bit depth {bmi.bV5BitCount} is not supported.");
                }

                int stride = ((width * bmi.bV5BitCount + 31) / 32) * 4;
                bool isTopDown = bmi.bV5Height < 0;

                IntPtr scan0 = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + bmi.bV5Size);
                if (!isTopDown)
                {
                    scan0 = new IntPtr(scan0.ToInt64() + (height - 1) * stride);
                    stride = -stride;
                }

                Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);

                // Create a new bitmap to return, because the original one is tied to the pinned memory
                Bitmap result = new Bitmap(bitmap);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CF_DIBV5ToBitmap: {ex.Message}");
                throw;
            }
            finally
            {
                handle.Free();
            }
        }

        private static Bitmap CF_DIBToBitmap(byte[] data)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var bmi = (BITMAPINFO)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(BITMAPINFO));
                int width = bmi.bmiHeader.biWidth;
                int height = Math.Abs(bmi.bmiHeader.biHeight); // Handle both top-down and bottom-up DIBs
                PixelFormat pixelFormat;

                switch (bmi.bmiHeader.biBitCount)
                {
                    case 24:
                        pixelFormat = PixelFormat.Format24bppRgb;
                        break;
                    case 32:
                        pixelFormat = PixelFormat.Format32bppArgb;
                        break;
                    default:
                        throw new NotSupportedException($"Bit depth {bmi.bmiHeader.biBitCount} is not supported.");
                }

                int stride = ((width * bmi.bmiHeader.biBitCount + 31) / 32) * 4;
                int imageSize = stride * height;

                IntPtr scan0 = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + Marshal.SizeOf(typeof(BITMAPINFOHEADER)));
                if (bmi.bmiHeader.biHeight > 0) // Bottom-up DIB
                {
                    scan0 = new IntPtr(scan0.ToInt64() + (height - 1) * stride);
                    stride = -stride;
                }

                Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);

                // Create a new bitmap to return, because the original one is tied to the pinned memory
                Bitmap result = new Bitmap(bitmap);
                return result;
            }
            finally
            {
                handle.Free();
            }
        }

        // Updates selected clipboard selectedItem in editedClipboardItems list. Does not update the actual clipboard.
        private void UpdateEditedClipboardItem(int formatId, byte[] rawData, bool setPending = true)
        {
            // Match the selectedItem in the editedClipboardItems list
            for (int i = 0; i < editedClipboardItems.Count; i++)
            {
                if (editedClipboardItems[i].FormatId == formatId)
                {
                    editedClipboardItems[i].RawData = rawData;
                    editedClipboardItems[i].DataSize = (ulong)rawData.Length;
                    editedClipboardItems[i].HasPendingEdit = setPending;
                    return;
                }
            }
        }

        // Converts the hex string in the hex view to a byte array and updates the clipboard selectedItem in editedClipboardItems
        private void buttonApplyEdit_Click(object sender, EventArgs e)
        {
            // Get the hex string from the hex view
            string hexString = richTextBoxContents.Text.Replace(" ", "");
            byte[] rawData = Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
            // Get the format ID of the selected clipboard selectedItem
            int formatId = (int)GetSelectedClipboardItemObject().FormatId;

            // Check if the edited data is actually different from the original data, apply the change and set hasPendingChanges accordingly
            if (!GetSelectedClipboardItemObject().Data.SequenceEqual(rawData))
            {
                UpdateEditedClipboardItem(formatId, rawData);
                hasPendingChanges = true;
            }
            else
            {
                // Don't change hasPendingChanges to false because there might be other items with pending changes
            }

            UpdateEditControlsVisibility();
        }

        // Function to extract CF_DIBV5 structure into its hex components
        private static string CF_DIBV5ToHex(byte[] data)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(BITMAPV5HEADER));
            StringBuilder hexString = new StringBuilder();

            hexString.Append($"bV5Size: {bmi.bV5Size:X4}\n");
            hexString.Append($"bV5Width: {bmi.bV5Width:X4}\n");
            hexString.Append($"bV5Height: {bmi.bV5Height:X4}\n");
            hexString.Append($"bV5Planes: {bmi.bV5Planes:X4}\n");
            hexString.Append($"bV5BitCount: {bmi.bV5BitCount:X4}\n");
            hexString.Append($"bV5Compression: {bmi.bV5Compression:X4}\n");
            hexString.Append($"bV5SizeImage: {bmi.bV5SizeImage:X4}\n");
            hexString.Append($"bV5XPelsPerMeter: {bmi.bV5XPelsPerMeter:X4}\n");
            hexString.Append($"bV5YPelsPerMeter: {bmi.bV5YPelsPerMeter:X4}\n");
            hexString.Append($"bV5ClrUsed: {bmi.bV5ClrUsed:X4}\n");
            hexString.Append($"bV5ClrImportant: {bmi.bV5ClrImportant:X4}\n");
            hexString.Append($"bV5RedMask: {bmi.bV5RedMask:X4}\n");
            hexString.Append($"bV5GreenMask: {bmi.bV5GreenMask:X4}\n");
            hexString.Append($"bV5BlueMask: {bmi.bV5BlueMask:X4}\n");
            hexString.Append($"bV5AlphaMask: {bmi.bV5AlphaMask:X4}\n");
            hexString.Append($"bV5CSType: {bmi.bV5CSType:X4}\n");
            hexString.Append($"bV5Endpoints: {bmi.bV5Endpoints:X4}\n");
            hexString.Append($"bV5GammaRed: {bmi.bV5GammaRed:X4}\n");
            hexString.Append($"bV5GammaGreen: {bmi.bV5GammaGreen:X4}\n");
            hexString.Append($"bV5GammaBlue: {bmi.bV5GammaBlue:X4}\n");
            hexString.Append($"bV5Intent: {bmi.bV5Intent:X4}\n");
            hexString.Append($"bV5ProfileData: {bmi.bV5ProfileData:X4}\n");
            hexString.Append($"bV5ProfileSize: {bmi.bV5ProfileSize:X4}\n");
            hexString.Append($"bV5Reserved: {bmi.bV5Reserved:X4}\n");
            handle.Free();

            return hexString.ToString();
        }

        private void menuItemShowLargeHex_Click(object sender, EventArgs e)
        {
            // Toggle the check based on the current state
            menuOptions_ShowLargeHex.Checked = !menuOptions_ShowLargeHex.Checked;
        }

        // Give focus to control when mouse enters
        private void dataGridViewClipboard_MouseEnter(object sender, EventArgs e)
        {
            dataGridViewClipboard.Focus();
        }

        private void buttonResetEdit_Click(object sender, EventArgs e)
        {
            // Get the original item's data and apply it to the edited item
            UpdateEditedClipboardItem((int)GetSelectedClipboardItemObject().FormatId, GetSelectedClipboardItemObject().Data, setPending: false);

            // Check if any edited items have pending changes, and update the pending changes label if necessary
            hasPendingChanges = editedClipboardItems.Any(i => i.HasPendingEdit);

            // Update the view
            DisplayClipboardData(GetSelectedClipboardItemObject());
            UpdateEditControlsVisibility();

        }

        private void dataGridViewClipboard_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                // Assume focus of the first selected row if multiple are selected
                ChangeCellFocus(dataGridViewClipboard.SelectedRows[0].Index);
            }
            else
            {
                richTextBoxContents.Clear();
            }
        }

        private void menuEdit_CopyHexAsText_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToCopy = GetSelectedClipboardItemObject();
            if (itemToCopy == null)
            {
                return;
            }
            // Get the hex information that would be displayed in the hex view
            string data = BitConverter.ToString(itemToCopy.RawData).Replace("-", " ");

            // Copy the hex information to the clipboard
            Clipboard.SetText(data);
        }

        private void menuEdit_CopyObjectInfoAsText_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToCopy = GetSelectedClipboardItemObject();
            if (itemToCopy == null)
            {
                return;
            }
            // Get the struct / object info that would be displayed in object view of rich text box and copy it to clipboard
            string data = FormatInspector.InspectFormat(formatName: GetStandardFormatName(itemToCopy.FormatId), data: itemToCopy.RawData, fullItem: itemToCopy, allowLargeHex: menuOptions_ShowLargeHex.Checked);
            Clipboard.SetText(data);
        }

        private void menuEdit_CopyEditedHexAsText_Click(object sender, EventArgs e)
        {
            // Get the edited clipboard selectedItem and its info
            ClipboardItem itemToCopy = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
            if (itemToCopy == null)
            {
                return;
            }

            // Get the hex information that would be displayed in the hex view and copy it to clipboard
            string data = BitConverter.ToString(itemToCopy.RawData).Replace("-", " ");
            Clipboard.SetText(data);
        }

        private void menuEdit_CopySelectedRows_Click(object sender, EventArgs e)
        {
            // If no rows are selected, do nothing
            if (dataGridViewClipboard.SelectedRows.Count == 0)
            {
                return;
            }

            // Get the selected rows and put them in a list, each row a list of strings for the cell values
            List<List<string>> selectedRowsContents = new List<List<string>>();

            // If option to include headers is enabled, add that first
            if (menuOptions_IncludeRowHeaders.Checked)
            {
                // Just get the contents of the header row only
                List<string> headerRow = dataGridViewClipboard.Columns.Cast<DataGridViewColumn>().Select(col => col.HeaderText).ToList();
                selectedRowsContents.Add(headerRow);
            }

            // Create a list of selected rows based on index so we can get them in the desired order, same as displayed
            var selectedRowIndices = dataGridViewClipboard.SelectedRows.Cast<DataGridViewRow>()
                .Select(row => row.Index)
                .ToList();
            // Sort the indices to match the display order. This should still be correct regardless of sorting mode clicked on the header bar
            selectedRowIndices.Sort();

            foreach (int rowIndex in selectedRowIndices)
            {
                DataGridViewRow row = dataGridViewClipboard.Rows[rowIndex];
                // Create an array of the cell values so we can manage them individually if necessary
                List<string> rowCells = row.Cells.Cast<DataGridViewCell>().Select(cell => cell.Value.ToString()).ToList();
                // If the last cell is empty, remove it from the list
                if (string.IsNullOrEmpty(rowCells.Last()))
                {
                    rowCells.RemoveAt(rowCells.Count - 1);
                }

                selectedRowsContents.Add(rowCells);
            }

            // Remove any newlines from the cells of all rows
            for (int i = 0; i < selectedRowsContents.Count; i++)
            {
                for (int j = 0; j < selectedRowsContents[i].Count; j++)
                {
                    selectedRowsContents[i][j] = selectedRowsContents[i][j].Replace("\n", " ");
                }
            }

            string finalCombinedString = "";

            // If the option to separate by tabs is enabled, join the cells with tabs
            if (menuOptions_TabSeparation.Checked)
            {
                finalCombinedString = string.Join("\n", selectedRowsContents.Select(row => string.Join("\t", row)));
            }
            else if (menuOptions_CommaSeparation.Checked)
            {
                // If the option to separate by commas is enabled, join the cells with commas
                finalCombinedString = string.Join("\n", selectedRowsContents.Select(row => string.Join(", ", row)));
            }
            else if (menuOptions_PreFormatted.Checked)
            {
                // If the option to separate by commas is enabled, join the cells with commas
                finalCombinedString = string.Join("\n", selectedRowsContents.Select(row => string.Join(", ", row)));
            }
            // Shouldn't get to this point but have it just in case as a fallback
            else
            {
                // Otherwise, join the cells with newlines
                finalCombinedString = string.Join("\n", selectedRowsContents.Select(row => string.Join(", ", row)));
            }

            // Copy the list to the clipboard
            Clipboard.SetText(finalCombinedString);
        }

        private void menuOptions_IncludeRowHeaders_Click(object sender, EventArgs e)
        {
            // Toggle the check based on the current state
            menuOptions_IncludeRowHeaders.Checked = !menuOptions_IncludeRowHeaders.Checked;
        }

        // ---------------------- Table Copy Formatting Options ----------------------
        private void menuOptions_TabSeparation_Click(object sender, EventArgs e)
        {
            // Use pattern matchin to get the text of the clicked item and pass it in to the function automatically
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem.Text);
            }
        }

        private void menuOptions_CommaSeparation_Click(object sender, EventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem.Text);
            }
        }

        private void menuOptions_PreFormatted_Click(object sender, EventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem.Text);
            }
        }

        private void setCopyModeChecks(string newlyCheckedOption)
        {
            // Newly checked option is the text of the menu item that was just checked
            // Uncheck all other options and make sure the newly checked option is checked (this also handles if it was already checked)

            // Find the Options menu item
            MenuItem optionsMenuItem = null;
            foreach (MenuItem menuItem in mainMenu1.MenuItems)
            {
                if (menuItem.Text == "Options")
                {
                    optionsMenuItem = menuItem;
                    break;
                }
            }

            if (optionsMenuItem != null)
            {
                // Find the "Table Copying Mode" sub-menu
                MenuItem tableCopyingModeMenuItem = null;
                foreach (MenuItem subMenuItem in optionsMenuItem.MenuItems)
                {
                    if (subMenuItem.Text == "Table Copying Mode")
                    {
                        tableCopyingModeMenuItem = subMenuItem;
                        break;
                    }
                }

                if (tableCopyingModeMenuItem != null)
                {
                    // Iterate through the items in the "Table Copying Mode" sub-menu
                    foreach (MenuItem item in tableCopyingModeMenuItem.MenuItems)
                    {
                        // Uncheck all items except the newly checked one. And ensure the clicked one is checked.
                        item.Checked = (item.Text == newlyCheckedOption);
                    }
                }
            }

        }

        // -----------------------------------------------------------------------------
    }

    public class ClipboardItem : ICloneable
    {
        public string FormatName { get; set; }
        public uint FormatId { get; set; }
        public IntPtr Handle { get; set; }
        public ulong DataSize { get; set; }
        public byte[] Data { get; set; }
        public byte[] RawData { get; set; }
        public bool AssumedSynthesized { get; set; }
        public List<string> DataInfoList { get; set; }
        public string DataInfoString => string.Join(", ", DataInfoList ?? new List<string>());
        public bool HasPendingEdit { get; set; } = false;

        public object Clone()
        {
            return new ClipboardItem
            {
                FormatName = this.FormatName,
                FormatId = this.FormatId,
                Handle = this.Handle,
                DataSize = this.DataSize,
                Data = (byte[])this.Data?.Clone(),
                RawData = (byte[])this.RawData?.Clone(),
                AssumedSynthesized = this.AssumedSynthesized,
                DataInfoList = new List<string>(this.DataInfoList ?? new List<string>()),
                HasPendingEdit = false
            };
        }
    }


    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint EnumClipboardFormats(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, UIntPtr count);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

        [DllImport("gdi32.dll")]
        public static extern int GetObject(IntPtr hObject, int nCount, ref BITMAP lpObject);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, string lpszFile);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int CountClipboardFormats();


        public const uint GMEM_MOVEABLE = 0x0002;
    }


    public static class FormatInspector
    {
        public class FormatInfo
        {
            public uint Value { get; set; }
            public string Kind { get; set; }
            public string HandleOutput { get; set; }
            public Type StructType { get; set; }
        }

        private static readonly Dictionary<string, FormatInfo> FormatDictionary = new Dictionary<string, FormatInfo>
        {
        // "Kinds" of formats:
        //  - typedef: A simple typedef, like CF_TEXT or CF_BITMAP
        //  - struct: A complex structure, like CF_DIB or CF_METAFILEPICT
        //  - data: A simple data format, like CF_OEMTEXT or CF_WAVE

        {"CF_BITMAP", new FormatInfo {Value = 2, Kind = "typedef", HandleOutput = "HBITMAP"}},
        {"CF_DIB", new FormatInfo {Value = 8, Kind = "struct", HandleOutput = "BITMAPINFO followed by bitmap bits", StructType = typeof(BITMAPINFO)}},
        {"CF_DIBV5", new FormatInfo {Value = 17, Kind = "struct", HandleOutput = "BITMAPV5HEADER followed by color space info and bitmap bits", StructType = typeof(BITMAPV5HEADER)}},
        {"CF_DIF", new FormatInfo {Value = 5, Kind = "data", HandleOutput = "Software Arts' Data Interchange Format"}},
        {"CF_DSPBITMAP", new FormatInfo {Value = 0x0082, Kind = "data", HandleOutput = "Bitmap display data"}},
        {"CF_DSPENHMETAFILE", new FormatInfo {Value = 0x008E, Kind = "data", HandleOutput = "Enhanced metafile display data"}},
        {"CF_DSPMETAFILEPICT", new FormatInfo {Value = 0x0083, Kind = "data", HandleOutput = "Metafile picture display data"}},
        {"CF_DSPTEXT", new FormatInfo {Value = 0x0081, Kind = "data", HandleOutput = "Text display data"}},
        {"CF_ENHMETAFILE", new FormatInfo {Value = 14, Kind = "typedef", HandleOutput = "HENHMETAFILE"}},
        {"CF_GDIOBJFIRST", new FormatInfo {Value = 0x0300, Kind = "data", HandleOutput = "Start of range of integers for application-defined GDI object formats"}},
        {"CF_GDIOBJLAST", new FormatInfo {Value = 0x03FF, Kind = "data", HandleOutput = "End of range of integers for application-defined GDI object formats"}},
        {"CF_HDROP", new FormatInfo {Value = 15, Kind = "typedef", HandleOutput = "HDROP (list of files)"}},
        {"CF_LOCALE", new FormatInfo {Value = 16, Kind = "data", HandleOutput = "LCID (locale identifier)"}},
        {"CF_METAFILEPICT", new FormatInfo {Value = 3, Kind = "struct", HandleOutput = "METAFILEPICT", StructType = typeof(METAFILEPICT)}},
        {"CF_OEMTEXT", new FormatInfo {Value = 7, Kind = "data", HandleOutput = "Text in OEM character set"}},
        {"CF_OWNERDISPLAY", new FormatInfo {Value = 0x0080, Kind = "data", HandleOutput = "Owner-display format data"}},
        {"CF_PALETTE", new FormatInfo {Value = 9, Kind = "typedef", HandleOutput = "HPALETTE"}},
        {"CF_PENDATA", new FormatInfo {Value = 10, Kind = "data", HandleOutput = "Pen computing extension data"}},
        {"CF_PRIVATEFIRST", new FormatInfo {Value = 0x0200, Kind = "data", HandleOutput = "Start of range of integers for private clipboard formats"}},
        {"CF_PRIVATELAST", new FormatInfo {Value = 0x02FF, Kind = "data", HandleOutput = "End of range of integers for private clipboard formats"}},
        {"CF_RIFF", new FormatInfo {Value = 11, Kind = "data", HandleOutput = "Complex audio data, can be represented in a CF_WAVE standard wave format."}},
        {"CF_SYLK", new FormatInfo {Value = 4, Kind = "data", HandleOutput = "Microsoft Symbolic Link format (SYLK)"}},
        {"CF_TEXT", new FormatInfo {Value = 1, Kind = "data", HandleOutput = "ANSI text"}},
        {"CF_TIFF", new FormatInfo {Value = 6, Kind = "data", HandleOutput = "Tagged-image file format"}},
        {"CF_UNICODETEXT", new FormatInfo {Value = 13, Kind = "data", HandleOutput = "Unicode text"}},
        {"CF_WAVE", new FormatInfo {Value = 12, Kind = "data", HandleOutput = "Standard wave format audio data"}}
        };

        public static string InspectFormat(string formatName, byte[] data, ClipboardItem fullItem, string indent = "", bool allowLargeHex=false)
        {
            if (!FormatDictionary.TryGetValue(formatName, out FormatInfo formatInfo))
            {
                return $"{indent}Unknown format: {formatName}";
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine($"{indent}Format: {formatName}");
            result.AppendLine($"{indent}Format ID: {formatInfo.Value}");
            //result.AppendLine($"{indent}Kind: {formatInfo.Kind}");
            result.AppendLine($"{indent}Handle Output: {formatInfo.HandleOutput}");

            if (!string.IsNullOrEmpty(fullItem.DataInfoString))
            {
                result.AppendLine($"{indent}Data Info:");
                // Add each selectedItem in DataInfoList to the result indented
                foreach (string dataInfoItem in fullItem.DataInfoList)
                {
                    result.AppendLine($"{indent}  {dataInfoItem}");
                }
                result.AppendLine("");
            }

            if (formatInfo.Kind == "struct" && formatInfo.StructType != null && data != null)
            {
                result.AppendLine($"{indent}Struct Definition and Values:");
                int offset = 0;
                InspectStruct(formatInfo.StructType, data, ref result, indent + "  ", ref offset);
            }
            
            //else if (data != null)
            //{
            //    result.AppendLine($"\n{indent}Data:");
            //    // Display if not too big
            //    if (allowLargeHex || data.Length <= 50000)
            //    {
            //        result.AppendLine($"{BitConverter.ToString(data).Replace("-", " ")}");
            //    }
            //    else
            //    {
            //        result.AppendLine($"{indent}  [Data too large to display. Export raw hex data instead]");
            //    }

            //}

            return result.ToString();
        }

        private static string GetValueString(object value)
        {
            if (value == null)
                return "null";

            if (value is IntPtr ptr)
            {
                return $"0x{ptr.ToInt64():X}";
            }

            Type valueType = value.GetType();
            if (valueType.IsValueType && !valueType.IsPrimitive && valueType != typeof(IntPtr))
            {
                // For nested structs, we'll return a placeholder
                return $"[{valueType.Name}]";
            }

            return value.ToString();
        }

        private static void InspectStruct(Type structType, byte[] data, ref StringBuilder result, string indent, ref int offset)
        {
            var fields = structType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                result.AppendLine($"{indent}{field.FieldType.Name} {field.Name}");

                if (offset < data.Length)
                {
                    if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && field.FieldType != typeof(IntPtr))
                    {
                        // Nested struct
                        result.AppendLine($"{indent}  Value:");
                        InspectStruct(field.FieldType, data, ref result, indent + "    ", ref offset);
                    }
                    else if (field.FieldType.IsArray)
                    {
                        // Array field (like RGBQUAD[])
                        result.AppendLine($"{indent}  Value: [Array of {field.FieldType.GetElementType().Name}]");
                        // Note: We don't process array contents here as the length is unknown
                    }
                    else
                    {
                        object fieldValue = ReadValueFromBytes(data, ref offset, field.FieldType);
                        string valueStr = GetValueString(fieldValue);
                        result.AppendLine($"{indent}  Value: {valueStr}");
                    }
                }
                else
                {
                    result.AppendLine($"{indent}  Value: [Data not available]");
                }
            }
        }


        private static string GetHexString(object value, string indent)
        {
            if (value == null)
                return "null";

            Type valueType = value.GetType();

            if (valueType.IsPrimitive || valueType == typeof(decimal))
            {
                return value.ToString();
            }
            else if (valueType.IsEnum)
            {
                return $"{value} ({(int)value})";
            }
            else if (valueType.IsValueType && !valueType.IsPrimitive)
            {
                // For nested structs, we'll return a placeholder
                return $"[{valueType.Name}]";
            }
            else if (valueType == typeof(IntPtr))
            {
                return $"0x{((IntPtr)value).ToInt64():X}";
            }
            else
            {
                return value.ToString();
            }
        }

        private static string GetBytesString(object value)
        {
            if (value == null)
                return "null";

            try
            {
                byte[] bytes = GetBytes(value);
                return BitConverter.ToString(bytes).Replace("-", " ");
            }
            catch (ArgumentException)
            {
                return "Unable to get bytes for this type";
            }
        }

        private static object ReadValueFromBytes(byte[] data, ref int offset, Type fieldType)
        {
            if (fieldType == typeof(byte))
            {
                return data[offset++];
            }
            else if (fieldType == typeof(short) || fieldType == typeof(ushort))
            {
                var value = BitConverter.ToInt16(data, offset);
                offset += 2;
                return value;
            }
            else if (fieldType == typeof(int) || fieldType == typeof(uint))
            {
                var value = BitConverter.ToInt32(data, offset);
                offset += 4;
                return value;
            }
            else if (fieldType == typeof(long) || fieldType == typeof(ulong))
            {
                var value = BitConverter.ToInt64(data, offset);
                offset += 8;
                return value;
            }
            else if (fieldType == typeof(float))
            {
                var value = BitConverter.ToSingle(data, offset);
                offset += 4;
                return value;
            }
            else if (fieldType == typeof(double))
            {
                var value = BitConverter.ToDouble(data, offset);
                offset += 8;
                return value;
            }
            else if (fieldType == typeof(bool))
            {
                var value = BitConverter.ToBoolean(data, offset);
                offset += 1;
                return value;
            }
            else if (fieldType == typeof(char))
            {
                var value = BitConverter.ToChar(data, offset);
                offset += 2;
                return value;
            }
            else if (fieldType == typeof(IntPtr) || fieldType == typeof(UIntPtr))
            {
                var size = IntPtr.Size;
                var value = size == 4 ? BitConverter.ToInt32(data, offset) : BitConverter.ToInt64(data, offset);
                offset += size;
                return new IntPtr(value);
            }
            else
            {
                // For complex types, we'll just return the type name
                return $"[{fieldType.Name}]";
            }
        }

        private static byte[] GetBytes(object value)
        {
            Type type = value.GetType();

            if (type == typeof(bool)) return BitConverter.GetBytes((bool)value);
            if (type == typeof(char)) return BitConverter.GetBytes((char)value);
            if (type == typeof(double)) return BitConverter.GetBytes((double)value);
            if (type == typeof(short)) return BitConverter.GetBytes((short)value);
            if (type == typeof(int)) return BitConverter.GetBytes((int)value);
            if (type == typeof(long)) return BitConverter.GetBytes((long)value);
            if (type == typeof(float)) return BitConverter.GetBytes((float)value);
            if (type == typeof(ushort)) return BitConverter.GetBytes((ushort)value);
            if (type == typeof(uint)) return BitConverter.GetBytes((uint)value);
            if (type == typeof(ulong)) return BitConverter.GetBytes((ulong)value);
            if (type == typeof(byte)) return new[] { (byte)value };
            if (type == typeof(sbyte)) return new[] { (byte)(sbyte)value };
            if (type == typeof(DateTime)) return BitConverter.GetBytes(((DateTime)value).Ticks);
            if (type == typeof(IntPtr)) return BitConverter.GetBytes(((IntPtr)value).ToInt64());
            if (type == typeof(UIntPtr)) return BitConverter.GetBytes(((UIntPtr)value).ToUInt64());
            if (type == typeof(decimal))
            {
                int[] bits = decimal.GetBits((decimal)value);
                List<byte> bytes = new List<byte>();
                foreach (int part in bits)
                {
                    bytes.AddRange(BitConverter.GetBytes(part));
                }
                return bytes.ToArray();
            }

            throw new ArgumentException($"Unsupported type: {type.FullName}", nameof(value));
        }
    }

}