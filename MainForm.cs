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
using System.Globalization;

#pragma warning disable IDE1006 // Disable messages about Naming Styles

// My classes
using static EditClipboardItems.ClipboardFormats;
using System.Drawing.Text;


namespace ClipboardManager
{
    public partial class MainForm : Form
    {
        public const string VERSION = "0.1.0";

        private readonly List<ClipboardItem> clipboardItems = new List<ClipboardItem>();
        private List<ClipboardItem> editedClipboardItems = new List<ClipboardItem>(); // Add this line

        private StreamWriter logFile;

        public static bool hasPendingChanges = false;
        public static bool enableSplitHexView = false;

        // Variables to store info about initial GUI state
        public int hexTextBoxTopBuffer { get; init; }

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
            hexTextBoxTopBuffer = richTextBoxContents.Height - richTextBox_HexPlaintext.Height;

            InitializeLogging();
            InitializeDataGridView();
            UpdateToolLocations();

            // Initial tool settings
            dropdownContentsViewMode.SelectedIndex = 0; // Default index 0 is "Text" view mode
            dropdownHexToTextEncoding.SelectedIndex = 0; // Default index 0 is "UTF-8" encoding


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

            // Add padding to the text preview column
            // Get the current padding for text preview column
            Padding textPreviewPadding = dataGridViewClipboard.Columns["TextPreview"].DefaultCellStyle.Padding;
            textPreviewPadding.Left = 3;
            dataGridViewClipboard.Columns["TextPreview"].DefaultCellStyle.Padding = textPreviewPadding;

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
                    dataGridViewClipboard.ClearSelection();
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
                    result = utf16Result;
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
                    result = utf8Result;
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
            int bottomBuffer = 30; // Adjust this value to set the desired buffer size

            int splitterPanelsBottomPosition = this.Height - toolStrip1.Height - titlebarAccomodate;

            // Resize splitContainerMain to fit the form
            splitContainerMain.Width = this.Width - 32;
            splitContainerMain.Height = splitterPanelsBottomPosition - bottomBuffer;

            // Resize splitterContainer_InnerTextBoxes to fit the form
            splitterContainer_InnerTextBoxes.Width = splitContainerMain.Width;
            splitterContainer_InnerTextBoxes.Height = splitContainerMain.Panel2.Height - splitterBorderAccomodate - bottomBuffer;

            // If the hex view is disabled, force the hex panel to zero width
            if (!enableSplitHexView)
            {
                splitterContainer_InnerTextBoxes.Panel2Collapsed = true;
            }

            // Auto-resize the text boxes to match the panels
            richTextBoxContents.Height = splitterContainer_InnerTextBoxes.Height;
            richTextBox_HexPlaintext.Height = splitterContainer_InnerTextBoxes.Height - hexTextBoxTopBuffer; // Adds some space for encoding selection dropdown. Based on initial GUI settings.
            richTextBoxContents.Width = splitterContainer_InnerTextBoxes.Panel1.Width;
            richTextBox_HexPlaintext.Width = splitterContainer_InnerTextBoxes.Panel2.Width;

            // Resize processedData grid within panel to match panel size
            dataGridViewClipboard.Width = splitContainerMain.Panel1.Width - splitterBorderAccomodate;
            dataGridViewClipboard.Height = splitContainerMain.Panel1.Height - splitterBorderAccomodate;

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

        private void ChangeCellFocus(int rowIndex, int cellIndex = -1)
        {
            if (rowIndex >= 0)
            {
                // Set the selected cell if a valid cell index is provided
                if (cellIndex >= 0)
                {
                    dataGridViewClipboard.CurrentCell = dataGridViewClipboard.Rows[rowIndex].Cells[cellIndex];
                }

                DataGridViewRow selectedRow = dataGridViewClipboard.Rows[rowIndex];
                // Updates the text box with data for the selected row's format
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
                    // Show hex data in the left panel text box
                    richTextBoxContents.Text = BitConverter.ToString(item.RawData).Replace("-", " ");
                    richTextBoxContents.ReadOnly = true;
                    UpdatePlaintextFromHexView();
                    break;

                case 2: // Hex (Editable) view mode
                    richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
                    richTextBoxContents.Text = BitConverter.ToString(item.RawData).Replace("-", " ");
                    richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;

                    richTextBoxContents.ReadOnly = false;
                    UpdatePlaintextFromHexView();
                    break;
                case 3: // Object / Struct View
                    richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
                    richTextBoxContents.Text = FormatInspector.InspectFormat(formatName: GetStandardFormatName(item.FormatId), data: item.RawData, fullItem: item);
                    richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;

                    richTextBoxContents.ReadOnly = true;
                    break;

                default:
                    richTextBoxContents.Text = "Unknown view mode";
                    break;
            }
        }

        private void UpdatePlaintextFromHexView()
        {
            // Set encoding mode based on dropdown
            Encoding encodingToUse;
            if (dropdownHexToTextEncoding.SelectedIndex == 0) // UTF-8
            {
                encodingToUse = Encoding.UTF8;
            }
            else if (dropdownHexToTextEncoding.SelectedIndex == 1) // UTF-16
            {
                encodingToUse = Encoding.Unicode;
            }
            else
            {
                encodingToUse = Encoding.UTF8;
            }

            // Use the text from the other text box to ensure the text is up to date
            string textToConvert = richTextBoxContents.Text.Replace(" ", "");

            byte[] byteData = new byte[0];

            if (!string.IsNullOrEmpty(textToConvert))
            {
                if (textToConvert.Length % 2 != 0)
                {
                    // If the length is odd, add a space to the end
                    //textToConvert += " ";
                }
                try
                {
                    byteData = Enumerable.Range(0, textToConvert.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(textToConvert.Substring(x, 2), 16))
                    .ToArray();
                }
                // Probably not text
                catch (Exception ex)
                {
                    Console.WriteLine($"Error converting hex to text: {ex.Message}");
                }

            }

            string plaintextRaw = encodingToUse.GetString(byteData); // This could contain null characters
            string plaintext;

            if (checkBoxPlainTextEditing.Checked)
            {
                // If the checkbox is checked, show null characters as dots
                plaintext = EscapeString(plaintextRaw);
            }
            else
            {
                plaintext = ReplaceEscapeWithChar(plaintextRaw); // Remove null characters
            }

            // Convert the bytes to text and update the text box. First disable textchanged event to prevent infinite loop
            richTextBox_HexPlaintext.TextChanged -= richTextBox_HexPlaintext_TextChanged;
            richTextBox_HexPlaintext.Text = plaintext;
            richTextBox_HexPlaintext.TextChanged += richTextBox_HexPlaintext_TextChanged;
        }

        // Converts the text in the hex text box to hex and updates the other text box. Encoding based on dropdown
        private void UpdateHexViewChanges()
        {
            Encoding encoding = Encoding.UTF8;
            if (dropdownHexToTextEncoding.SelectedIndex == 0) // UTF-8
            {
                encoding = Encoding.UTF8;
            }
            else if (dropdownHexToTextEncoding.SelectedIndex == 1) // UTF-16
            {
                encoding = Encoding.Unicode;
            }
            else
            {
                encoding = Encoding.UTF8;
            }

            // Get the text from the plaintext box
            string text = UnescapeString(richTextBox_HexPlaintext.Text);


            // Convert the text to bytes
            byte[] byteData = encoding.GetBytes(text);

            // Convert the bytes to hex
            string hexString = BitConverter.ToString(byteData).Replace("-", " ");

            // Update the hex text box. First disable textchanged event to prevent infinite loop
            richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
            richTextBoxContents.Text = hexString;
            richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;

        }

        private string UnescapeString (string inputString)
        {
            inputString = inputString.Replace("\\0", "\0");
            inputString = inputString.Replace("\\a", "\a");
            inputString = inputString.Replace("\\b", "\b");
            inputString = inputString.Replace("\\f", "\f");
            inputString = inputString.Replace("\\n", "\n");
            inputString = inputString.Replace("\\r", "\r");
            inputString = inputString.Replace("\\t", "\t");
            inputString = inputString.Replace("\\v", "\v");

            return inputString;
        }

        private string EscapeString(string inputString)
        {
            inputString = inputString.Replace("\0", "\\0");
            inputString = inputString.Replace("\a", "\\a");
            inputString = inputString.Replace("\b", "\\b");
            inputString = inputString.Replace("\f", "\\f");
            inputString = inputString.Replace("\n", "\\n");
            inputString = inputString.Replace("\r", "\\r");
            inputString = inputString.Replace("\t", "\\t");
            inputString = inputString.Replace("\v", "\\v");

            return inputString;
        }

        private string ReplaceEscapeWithChar(string inputString)
        {
            string replacement = ".";
            inputString = inputString.Replace("\0", replacement);
            inputString = inputString.Replace("\a", replacement);
            inputString = inputString.Replace("\b", replacement);
            inputString = inputString.Replace("\f", replacement);
            inputString = inputString.Replace("\n", replacement);
            inputString = inputString.Replace("\r", replacement);
            inputString = inputString.Replace("\t", replacement);
            inputString = inputString.Replace("\v", replacement);
            return inputString;
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

            // Show buttons and labels for edited mode
            UpdateEditControlsVisibility();

            ClipboardItem item = GetSelectedClipboardItemObject();
            if (item == null)
            {
                return;
            }

            DisplayClipboardData(item);
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

            // If it's hex edit mode or hex view mode, enable enableSplitHexView, regardless of selection
            if (dropdownContentsViewMode.SelectedIndex == 2 || dropdownContentsViewMode.SelectedIndex == 1)
            {
                enableSplitHexView = true;
                splitterContainer_InnerTextBoxes.Panel2Collapsed = false;
            }
            else
            {
                enableSplitHexView = false;
                splitterContainer_InnerTextBoxes.Panel2Collapsed = true;
            }
            UpdateToolLocations(); // Ensure the text boxes in the hex view are properly sized after collapsing the hex view panel

            // Make the "plaintext editing" checkbox visible only in hex edit mode
            if (dropdownContentsViewMode.SelectedIndex == 2)
            {
                checkBoxPlainTextEditing.Visible = true;
            }
            else
            {
                checkBoxPlainTextEditing.Checked = false;
                checkBoxPlainTextEditing.Visible = false;
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

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName = defaultFileName
            };

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


        private void toolStripButtonSaveEdited_Click(object sender, EventArgs e)
        {
            SaveClipboardData();
            RefreshClipboardItems();
            hasPendingChanges = false;
            UpdateEditControlsVisibility();
        }

        private void menuFile_ExportSelectedAsRawHex_Click(object sender, EventArgs e)
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

        private void menuFile_ExportSelectedStruct_Click(object sender, EventArgs e)
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
                string data = FormatInspector.InspectFormat(formatName: GetStandardFormatName(itemToExport.FormatId), data: itemToExport.RawData, fullItem: itemToExport);
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

        private void menuFile_ExportSelectedAsFile_Click(object sender, EventArgs e)
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

                // Enable menu buttons that require a selectedItem
                menuEdit_CopySelectedRows.Enabled = true;
                menuFile_ExportSelectedAsRawHex.Enabled = true;
                menuFile_ExportSelectedStruct.Enabled = true;
                menuFile_ExportSelectedAsFile.Enabled = true;
            }
            else
            {
                richTextBoxContents.Clear();

                // Disable menu buttons that require a selectedItem
                menuEdit_CopySelectedRows.Enabled = false;
                menuFile_ExportSelectedAsRawHex.Enabled = false;
                menuFile_ExportSelectedStruct.Enabled = false;
                menuFile_ExportSelectedAsFile.Enabled = false;
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
            string data = FormatInspector.InspectFormat(formatName: GetStandardFormatName(itemToCopy.FormatId), data: itemToCopy.RawData, fullItem: itemToCopy);
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
            copyTableRows(copyEntireTable: false);
        }

        private void menuEdit_CopyEntireTable_Click(object sender, EventArgs e)
        {
            copyTableRows(copyEntireTable: true);
        }

        // Copies the selected rows to the clipboard, or the entire table if chosen. Null automatically determines entire table if no rows are selected, otherwise just selected
        private void copyTableRows(bool? copyEntireTable = false, bool forceNoHeader = false)
        {
            // Get the selected rows and put them in a list, each row a list of strings for the cell values
            List<List<string>> selectedRowsContents = new List<List<string>>();

            // If option to include headers is enabled, add that first
            bool includeHeader = true; // Adding this to make it easier to determine later for pre-formatting
            if (menuOptions_IncludeRowHeaders.Checked && forceNoHeader != true)
            {
                // Just get the contents of the header row only
                List<string> headerRow = dataGridViewClipboard.Columns.Cast<DataGridViewColumn>().Select(col => col.HeaderText).ToList();
                selectedRowsContents.Add(headerRow);
                includeHeader = true;
            }
            else
            {
                includeHeader = false;
            }

            // if copyEntire Table is null, then automatically assume entire table if no rows are selected
            if (copyEntireTable == null)
            {
                if (dataGridViewClipboard.SelectedRows.Count > 0)
                {
                    copyEntireTable = false;
                }
                else
                {
                    copyEntireTable = true;
                }
            }

            // Determine which rows need to be copied. Either entire table or just selected rows based on function argument
            List<int> selectedRowIndices = new List<int>();
            if (copyEntireTable == false)
            {
                // Create a list of selected rows based on index so we can get them in the desired order, same as displayed
                selectedRowIndices = dataGridViewClipboard.SelectedRows.Cast<DataGridViewRow>()
                    .Select(row => row.Index)
                    .ToList();
                // Sort the indices to match the display order. This should still be correct regardless of sorting mode clicked on the header bar
                selectedRowIndices.Sort();
            }
            // The case where copying the entire table - Get all rows
            else
            {
                selectedRowIndices = dataGridViewClipboard.Rows.Cast<DataGridViewRow>()
                    .Select(row => row.Index)
                    .ToList();
            }


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
                    selectedRowsContents[i][j] = selectedRowsContents[i][j].Replace("\n", " ").Replace("\r", " ");
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
                // Get the maximum width of each column
                List<int> columnWidths = selectedRowsContents
                    .SelectMany(row => row.Select((cell, i) => new { i, len = cell?.Length ?? 0 }))
                    .GroupBy(x => x.i, x => x.len)
                    .Select(g => g.Max())
                    .ToList();

                // Create the format string
                string formatString = string.Join(" | ", columnWidths.Select((width, i) => $"{{{{{{i}},-{width}}}}}"));

                // If including headers, create a separator row to add into the 2nd position
                if (includeHeader)
                {
                    List<string> separatorRow = columnWidths.Select(width => new string('-', width)).ToList();
                    selectedRowsContents.Insert(1, separatorRow);
                }

                // Format each row
                IEnumerable<string> formattedRows = selectedRowsContents.Select(row =>
                {
                    string[] paddedRow = row.Concat(Enumerable.Repeat(string.Empty, columnWidths.Count - row.Count)).ToArray();
                    object[] args = paddedRow.Cast<object>().ToArray();
                    try
                    {
                        return string.Format(formatString, args);
                    }
                    catch (FormatException)
                    {
                        // If formatting fails, fall back to a simple join.
                        // Without this it throws errors - Possibly because of empty text preview cells but I haven't tested enough to be sure. This handles it though.
                        return string.Join(" | ", paddedRow.Select((cell, i) => cell.PadRight(columnWidths[i])));
                    }
                });

                // Join the rows
                finalCombinedString = string.Join(Environment.NewLine, formattedRows);
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

        private void dataGridViewClipboard_KeyDown(object sender, KeyEventArgs e)
        {
            // If the user presses Ctrl+C, copy the selected rows to the clipboard
            if (e.Control && e.KeyCode == Keys.C)
            {
                e.Handled = true;  // Prevents the default copy operation
                copyTableRows(copyEntireTable: null); // Null means entire table will be copied if no rows are selected, otherwise just selected rows
            }
        }

        private void copyRowDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyTableRows(copyEntireTable: false);
        }

        private void copyCellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Get the contents of the selected cell
            string cellContents = dataGridViewClipboard.CurrentCell.Value.ToString();
            // Copy the cell contents to the clipboard
            Clipboard.SetText(cellContents);
        }

        private void copySelectedRowsNoHeaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyTableRows(copyEntireTable: false, forceNoHeader: true);
        }

        private void dataGridViewClipboard_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                bool isClickedRowSelected = false;

                // Check if the clicked row is part of the current selection
                foreach (DataGridViewRow row in dataGridViewClipboard.SelectedRows)
                {
                    if (row.Index == e.RowIndex)
                    {
                        isClickedRowSelected = true;
                        break;
                    }
                }

                // If the clicked row is not part of the current selection, clear the selection and re-set the clicked row as the only selected row
                if (!isClickedRowSelected)
                {
                    dataGridViewClipboard.ClearSelection();
                    dataGridViewClipboard.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
                    // Change the cell focus
                    ChangeCellFocus(rowIndex: e.RowIndex, cellIndex: e.ColumnIndex);
                }
            }
        }

        private void contextMenuStrip_dataGridView_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void splitterContainer_InnerTextBoxes_SplitterMoved(object sender, SplitterEventArgs e)
        {
            UpdateToolLocations();
        }

        private void dropdownHexToTextEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePlaintextFromHexView();
        }

        private void richTextBoxContents_TextChanged(object sender, EventArgs e)
        {
            // Only update if in edit mode
            if (dropdownContentsViewMode.SelectedIndex == 2)
            {
                UpdatePlaintextFromHexView();
            }
            
        }

        private void richTextBox_HexPlaintext_TextChanged(object sender, EventArgs e)
        {
            // Only bother if in edit mode
            if (dropdownContentsViewMode.SelectedIndex == 2)
            {
                UpdateHexViewChanges();
                
            }
        }

        private void checkBoxPlainTextEditing_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePlaintextFromHexView();
        }

        private int prevSelectionStart = -1;
        private int prevSelectionLength = 0;
        private void richTextBoxContents_SelectionChanged(object sender, EventArgs e)
        {
            void RoundSelection()
            {
                int selStart = richTextBoxContents.SelectionStart;
                int selLength = richTextBoxContents.SelectionLength;

                // Determine selection direction
                bool isSelectingForward = selStart >= prevSelectionStart;

                // Adjust selection to byte boundaries
                int newSelStart = selStart;
                int newSelLength = selLength;

                if (isSelectingForward)
                {
                    // Adjust start to previous byte boundary
                    newSelStart = selStart - (selStart % 3);
                    // Adjust end to next byte boundary
                    int selEnd = selStart + selLength;
                    int remainder = selEnd % 3;
                    int newSelEnd = remainder == 0 ? selEnd : selEnd + (3 - remainder);
                    newSelLength = newSelEnd - newSelStart;
                }
                else
                {
                    // Adjust end to previous byte boundary
                    int selEnd = selStart + selLength;
                    int remainder = selEnd % 3;
                    int newSelEnd = selEnd - remainder;
                    // Adjust start to previous byte boundary
                    newSelStart = selStart - (selStart % 3);
                    newSelLength = newSelEnd - newSelStart;
                }

                // Ensure the new selection is within the text bounds
                if (newSelStart < 0)
                    newSelStart = 0;
                if (newSelStart + newSelLength > richTextBoxContents.TextLength)
                    newSelLength = richTextBoxContents.TextLength - newSelStart;

                // Update the selection only if it has changed
                if (newSelStart != selStart || newSelLength != selLength)
                {
                    richTextBoxContents.SelectionChanged -= richTextBoxContents_SelectionChanged;
                    richTextBoxContents.Select(newSelStart, newSelLength);
                    richTextBoxContents.SelectionChanged += richTextBoxContents_SelectionChanged;
                }

                // Update previous selection values
                prevSelectionStart = selStart;
                prevSelectionLength = selLength;
            }

            if (dropdownContentsViewMode.SelectedIndex == 2 || dropdownContentsViewMode.SelectedIndex == 1)
            {
                //RoundSelection();
                SyncHexToPlaintext();
            }
        }


        private void richTextBox_HexPlaintext_SelectionChanged(object sender, EventArgs e)
        {
            if (dropdownContentsViewMode.SelectedIndex == 2 || dropdownContentsViewMode.SelectedIndex == 1)
            {
                SyncPlaintextToHex();
            }
        }

        private void SyncHexToPlaintext()
        {
            bool editMode = checkBoxPlainTextEditing.Checked;

            int hexStart = richTextBoxContents.SelectionStart;
            int hexLength = richTextBoxContents.SelectionLength;
            int plaintextStart = hexStart / 3;
            int plaintextLength = CalculatePlaintextLengthFromHex(hexStart, hexLength, editMode);

            richTextBox_HexPlaintext.SelectionChanged -= richTextBox_HexPlaintext_SelectionChanged;
            richTextBox_HexPlaintext.Select(plaintextStart, plaintextLength);
            richTextBox_HexPlaintext.SelectionChanged += richTextBox_HexPlaintext_SelectionChanged;
        }

        private void SyncPlaintextToHex()
        {
            bool editMode = checkBoxPlainTextEditing.Checked;

            int plaintextStart = richTextBox_HexPlaintext.SelectionStart;
            int plaintextLength = richTextBox_HexPlaintext.SelectionLength;
            int hexStart = plaintextStart * 3;
            int hexLength = CalculateHexLengthFromPlaintext(plaintextStart, plaintextLength, editMode);

            richTextBoxContents.SelectionChanged -= richTextBoxContents_SelectionChanged;
            richTextBoxContents.Select(hexStart, hexLength);
            richTextBoxContents.SelectionChanged += richTextBoxContents_SelectionChanged;
        }

        private int CalculateHexLengthFromPlaintext(int start, int length, bool editMode)
        {
            int hexLength = 0;
            string text = richTextBox_HexPlaintext.Text;

            for (int i = start; i < start + length && i < text.Length; i++)
            {
                if (editMode)
                {
                    if (i < text.Length - 1 && text[i] == '\\')
                    {
                        switch (text[i + 1])
                        {
                            case '0':
                            case 'a':
                            case 'b':
                            case 'f':
                            case 'n':
                            case 'r':
                            case 't':
                            case 'v':
                                hexLength += 3; // One byte in hex
                                i++; // Skip the next character
                                break;
                            default:
                                hexLength += 3; // Treat as normal character
                                break;
                        }
                    }
                    else
                    {
                        hexLength += 3; // Normal character
                    }
                }
                else
                {
                    hexLength += 3; // In non-edit mode, each character (including '.') represents one byte
                }
            }

            return hexLength;
        }

        private int CalculatePlaintextLengthFromHex(int start, int length, bool editMode)
        {
            int plaintextLength = 0;
            string text = richTextBoxContents.Text;
            bool isUtf16 = dropdownHexToTextEncoding.SelectedIndex == 1;

            string rawSelectedTextSection = text.Substring(start, length);
            string cleanedText = rawSelectedTextSection.Replace(" ", "");

            int byteSize = isUtf16 ? 4 : 2;

            for (int i = 0; i < cleanedText.Length; i += byteSize)
            {
                if ((i + byteSize - 1) < cleanedText.Length)
                {
                    string byteStr = cleanedText.Substring(i, byteSize);
                    if (byteSize == 2)
                    {
                        if (byte.TryParse(byteStr, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                        {
                            ProcessByte(b);
                        }
                    }
                    else if (byteSize == 4)
                    {
                        if (ushort.TryParse(byteStr, System.Globalization.NumberStyles.HexNumber, null, out ushort us))
                        {
                            ProcessUtf16(us);
                        }
                    }
                }
            }

            void ProcessByte(byte b)
            {
                if (editMode)
                {
                    switch (b)
                    {
                        case 0x00: // \0
                        case 0x07: // \a
                        case 0x08: // \b
                        case 0x0C: // \f
                        case 0x0A: // \n
                        case 0x0D: // \r
                        case 0x09: // \t
                        case 0x0B: // \v
                            plaintextLength += 4; // These are represented as two characters in edit mode
                            break;
                        default:
                            plaintextLength += 2; // Normal character
                            break;
                    }
                }
                else
                {
                    plaintextLength += 2; // UTF-8 mode
                }
            }

            void ProcessUtf16(ushort us)
            {
                if (editMode)
                {
                    switch (us)
                    {
                        case 0x0000: // \0
                        case 0x0007: // \a
                        case 0x0008: // \b
                        case 0x000C: // \f
                        case 0x000A: // \n
                        case 0x000D: // \r
                        case 0x0009: // \t
                        case 0x000B: // \v
                            plaintextLength += 4; // These are represented as two characters in edit mode
                            break;
                        default:
                            plaintextLength += 2; // Normal character
                            break;
                    }
                }
                else
                {
                    plaintextLength += 2; // UTF-16 mode, one character per 16-bit value
                }
            }

            // All lengths are multiplied by two so that we can divide by two now and account for UTF-16
            // Otherwise we would have had to use 0.5 for the UTF-16 case
            return plaintextLength / 2; 
        }

        private void menuHelp_About_Click(object sender, EventArgs e)
        {
            // Show message box
            MessageBox.Show("Edit Clipboard Items\n\n" +
                "Version: " + VERSION + "\n\n" +
                "Author: ThioJoe" +
                "   (https://github.com/ThioJoe)", 
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        public static string InspectFormat(string formatName, byte[] data, ClipboardItem fullItem, string indent = "")
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

    }

}