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
using System.Drawing.Text;
using System.Text.RegularExpressions;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression

// My classes
using static EditClipboardItems.ClipboardFormats;
using static EditClipboardItems.FormatHandleTranslators;
using EditClipboardItems;


namespace ClipboardManager
{
    public partial class MainForm : Form
    {
        private readonly List<ClipboardItem> clipboardItems = new List<ClipboardItem>();
        private List<ClipboardItem> editedClipboardItems = new List<ClipboardItem>(); // Add this line

        // Other globals
        public static bool hasPendingChanges = false;
        public static bool enableSplitHexView = false;

        // Global constants
        public const int maxRawSizeDefault = 50000;

        // Variables to store info about initial GUI state
        public int hexTextBoxTopBuffer { get; init; }

        // Get version number from assembly
        static readonly System.Version versionFull = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        public readonly string versionString = $"{versionFull.Major}.{versionFull.Minor}.{versionFull.Build}";

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


        public MainForm()
        {
            InitializeComponent();
            hexTextBoxTopBuffer = richTextBoxContents.Height - richTextBox_HexPlaintext.Height;

            InitializeDataGridView();
            UpdateToolLocations();

            // Initial tool settings
            dropdownContentsViewMode.SelectedIndex = 0; // Default index 0 is "Text" view mode
            dropdownHexToTextEncoding.SelectedIndex = 0; // Default index 0 is "UTF-8" encoding

            // Set color of toolstrip manually because it doesn't set it apparently
            toolStrip1.BackColor = SystemColors.Control;

            // Set the version number label
            labelVersion.Text = $"Version {versionString}";

        }

        private int CompensateDPI(int originalValue)
        {
            float scaleFactor = this.DeviceDpi / 96f; // 96 is the default DPI
            return (int)(originalValue * scaleFactor);
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

        // Update processedData grid view with clipboard contents during refresh
        private void UpdateClipboardItemsGridView(string formatName, string formatID, string handleType, string dataSize, List<string> dataInfo, byte[] rawData)
        {
            // Preprocess certain info
            string textPreview = TryParseText(rawData, maxLength: 200, prefixEncodingType: false, debugging_formatName: formatName, debugging_callFrom: "Text Preview / UpdateClipboardItemsGridView");
            
            // The first item will have selected important info, to ensure it's not too long. The rest will show in data box in object/struct view mode
            string dataInfoString = dataInfo[0];

            if (string.IsNullOrEmpty(dataInfoString))
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

                if (column.Name == "DataInfo" && (string.IsNullOrEmpty(dataInfo[0]) || dataInfoString == "N/A" || dataInfoString.ToLower() == "[null]")) // Check for both N/A or null in case we add more reasons to set N/A later
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
            if (dataGridViewClipboard.Columns["DataInfo"].Width > CompensateDPI(200))
            {
                dataGridViewClipboard.Columns["DataInfo"].Width = CompensateDPI(200);
            }

            // Reset selection to none
            dataGridViewClipboard.ClearSelection();
        }

        // Function to try and parse the raw data for text if it is text
        private string TryParseText(byte[] rawData, int maxLength = 150, bool prefixEncodingType = false, string debugging_formatName = "", string debugging_callFrom = "")
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

            //bool invalidUTF8 = false;
            //bool invalidUTF16 = false;

            // Try UTF-8
            try
            {
                utf8Result = utf8Encoding.GetString(rawData);
            }
            catch (DecoderFallbackException)
            {
                // Invalid UTF-8, utf8Result remains empty
                //invalidUTF8 = true;

            }

            // Try UTF-16
            try
            {
                utf16Result = utf16Encoding.GetString(rawData);
            }
            catch (DecoderFallbackException)

            {
                // Invalid UTF-16, utf16Result remains empty
                //invalidUTF16 = true;
            }

            //if (invalidUTF8 && invalidUTF16)
            //{
            //    // Both Invalid
            //    Console.WriteLine("Both UTF-8 and UTF-16 are invalid");
            //}
            //if (invalidUTF8 && !invalidUTF16)
            //{
            //    // Valid UTF-16 but Invalid UTF-8
            //    Console.WriteLine("Only UTF-16 is valid");
            //}
            //if (!invalidUTF8 && invalidUTF16)
            //{
            //    // Valid UTF-8, but invalid UTF-16
            //    Console.WriteLine("Only UTF-8 is valid");
            //}

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
            if (!string.IsNullOrEmpty(utf8Result))
            {
                utf8Result = utf8Result.Replace("\0", "");
            }
            if (!string.IsNullOrEmpty(utf16Result))
            {
                utf16Result = utf16Result.Replace("\0", "");
            }

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

        //Function to fit processedData grid view to the form window
        private void UpdateToolLocations()
        {
            int titlebarAccomodate = CompensateDPI(40);
            int bottomBuffer = CompensateDPI(30); // Adjust this value to set the desired buffer size

            int splitterPanelsBottomPosition = this.Height - toolStrip1.Height - titlebarAccomodate;

            // Resize splitContainerMain to fit the form
            splitContainerMain.Width = this.Width - CompensateDPI(32);
            splitContainerMain.Height = splitterPanelsBottomPosition - bottomBuffer;

            // Resize splitterContainer_InnerTextBoxes to fit the form
            splitterContainer_InnerTextBoxes.Width = splitContainerMain.Width;
            splitterContainer_InnerTextBoxes.Height = splitContainerMain.Panel2.Height - bottomBuffer;

            // If the hex view is disabled, force the hex panel to zero width
            if (!enableSplitHexView)
            {
                splitterContainer_InnerTextBoxes.Panel2Collapsed = true;
            }

            splitterContainer_InnerTextBoxes.SplitterWidth = 10;

            // Auto-resize the text boxes to match the panels
            richTextBoxContents.Height = splitterContainer_InnerTextBoxes.Height;
            richTextBox_HexPlaintext.Height = splitterContainer_InnerTextBoxes.Height - hexTextBoxTopBuffer; // Adds some space for encoding selection dropdown. Based on initial GUI settings.
            richTextBoxContents.Width = splitterContainer_InnerTextBoxes.Panel1.Width;
            richTextBox_HexPlaintext.Width = splitterContainer_InnerTextBoxes.Panel2.Width;

            // Resize processedData grid within panel to match panel size
            dataGridViewClipboard.Width = splitContainerMain.Panel1.Width;
            dataGridViewClipboard.Height = splitContainerMain.Panel1.Height - CompensateDPI(3);

        }

        private void RefreshClipboardItems()
        {
            //Console.WriteLine("Starting RefreshClipboardItems");

            // Count the number of different data formats currently on the clipboard
            //int formatCount = NativeMethods.CountClipboardFormats();
            //Console.WriteLine($"Number of clipboard formats: {formatCount}");

            // Attempt to open the clipboard, retrying up to 10 times with a 10ms delay
            //Console.WriteLine("Attempting to open clipboard");
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
                //Console.WriteLine("Failed to open clipboard");
                MessageBox.Show("Failed to open clipboard.");
                return;
            }

            try
            {
                CopyClipboardData();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while copying clipboard: " + ex);
            }
            finally
            {
                //Console.WriteLine("Closing clipboard");
                NativeMethods.CloseClipboard();
            }

            DetermineSynthesizedFormats();
            ProcessClipboardData();
            editedClipboardItems = clipboardItems.Select(item => (ClipboardItem)item.Clone()).ToList(); // Clone clipboardItems to editedClipboardItems

            //Console.WriteLine("RefreshClipboardItems completed");
        }

        private void CopyClipboardData()
        {
            clipboardItems.Clear();
            editedClipboardItems.Clear();
            int formatCount = NativeMethods.CountClipboardFormats();
            uint format = 0;
            int currentCount = 0;

            while (true)
            {
                format = NativeMethods.EnumClipboardFormats(format);
                if (format == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 0) // ERROR_SUCCESS
                    {
                        // End of enumeration
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"EnumClipboardFormats failed. Error code: {error}");
                        break;
                    }
                }

                currentCount++;

                // -------- Start / Continue Enumeration ------------

                string formatName = GetClipboardFormatName(format);
                ulong dataSize = 0;
                byte[] rawData = null;
                //Console.WriteLine($"Checking Format {currentCount}: {formatName} ({format})"); // Debugging

                IntPtr hData = NativeMethods.GetClipboardData(format);
                if (hData == IntPtr.Zero)
                {
                    Console.WriteLine($"GetClipboardData returned null for format {format}");
                }

                try
                {
                    // First need to speciall handle certain formats that don't use HGlobal
                    switch (format)
                    {
                        case 2: // CF_BITMAP
                            using (Bitmap bitmap = Image.FromHbitmap(hData))
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    bitmap.Save(ms, ImageFormat.Bmp);
                                    rawData = ms.ToArray();
                                    dataSize = (ulong)rawData.Length;
                                }
                            }
                            break;
                        case 3: // CF_METAFILEPICT
                            rawData = FormatHandleTranslators.MetafilePict_RawData_FromHandle(hData);
                            dataSize = (ulong)(rawData?.Length ?? 0);
                            break;
                        case 9: // CF_PALETTE -- NOT YET HANDLED
                            rawData = FormatHandleTranslators.CF_PALETTE_RawData_FromHandle(hData);
                            dataSize = (ulong)(rawData?.Length ?? 0);
                            break;
                        case 14: // CF_ENHMETAFILE
                            rawData = FormatHandleTranslators.EnhMetafile_RawData_FromHandle(hData);
                            dataSize = (ulong)(rawData?.Length ?? 0);
                            break;
                        case 15: // CF_HDROP
                            rawData = FormatHandleTranslators.CF_HDROP_RawData_FromHandle(hData);
                            dataSize = (ulong)(rawData?.Length ?? 0);
                            break;
                        
                        // All other formats that use Hglobal
                        default:
                            IntPtr pData = NativeMethods.GlobalLock(hData);
                            if (pData != IntPtr.Zero)
                            {
                                try
                                {
                                    dataSize = (ulong)NativeMethods.GlobalSize(hData).ToUInt64();
                                    rawData = new byte[dataSize];
                                    Marshal.Copy(pData, rawData, 0, (int)dataSize);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error processing format {format}: {ex.Message}");
                                }
                                finally
                                {
                                    NativeMethods.GlobalUnlock(hData);
                                }
                            }
                            else { 
                                Console.WriteLine($"GlobalLock returned null for format {format}");
                            }
                            break;
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
            //Console.WriteLine($"Checked {currentCount} formats out of {formatCount} reported formats.");
            if (currentCount < formatCount)
            {
                Console.WriteLine("Warning: Not all reported formats were enumerated.");
            }
        }

        private void ProcessClipboardData()
        {
            dataGridViewClipboard.Rows.Clear();

            foreach (var item in clipboardItems)
            {
                byte[] processedData = item.RawData;

                // Data info list contains metadata about the data. First item will show in the data info column, all will show in the text box in object/struct view mode
                List<string> dataInfoList = new List<string>();

                switch (item.FormatId)
                {
                    case 1: // CF_TEXT
                        // Use Windows-1252 encoding (commonly referred to as ANSI in Windows)
                        Encoding ansiEncoding = Encoding.GetEncoding(1252);

                        // Convert bytes to string, stopping at the first null character
                        string text = "";
                        for (int i = 0; i < item.RawData.Length; i++)
                        {
                            if (item.RawData[i] == 0) break; // Stop at null terminator
                            text += (char)item.RawData[i];
                        }
                        processedData = ansiEncoding.GetBytes(text);
                        //-----------------------------------------
                        string ansiText = Encoding.Default.GetString(processedData);
                        dataInfoList.Add($"{ansiText.Length} Chars (ANSI)");
                        dataInfoList.Add($"Encoding: ANSI");
                        dataInfoList.Add($"Chars: {ansiText.Length}");
                        break;

                    case 13: // CF_UNICODETEXT
                        //Console.WriteLine("Processing CF_UNICODETEXT");
                        string unicodeText = Encoding.Unicode.GetString(item.RawData);
                        int unicodeTextLength = unicodeText.Length;
                        dataInfoList.Add($"{unicodeTextLength} Chars (Unicode)");
                        dataInfoList.Add($"Encoding: Unicode (UTF-16)");
                        dataInfoList.Add($"Character Count: {unicodeTextLength}");
                        dataInfoList.Add($"Byte Count: {item.DataSize}");

                        processedData = Encoding.Unicode.GetBytes(unicodeText);
                        break;

                    case 2: // CF_BITMAP
                        //Console.WriteLine("Processing CF_BITMAP");
                        if (item.RawData != null && item.RawData.Length > 0)
                        {
                            using (MemoryStream ms = new MemoryStream(item.RawData))
                            {
                                using (Bitmap bmp = new Bitmap(ms))
                                {
                                    // Setting the contents of the data info list explicitly instead of using Add. It could be done the other way too.
                                    dataInfoList = new List<string>
                                    {
                                        $"{bmp.Width}x{bmp.Height}, {bmp.PixelFormat}",
                                        $"Size: {bmp.Width}x{bmp.Height}",
                                        $"Format: {bmp.PixelFormat}"
                                    };
                                }
                            }
                        }
                        else
                        {
                            dataInfoList.Add("Error: Bitmap data not available");
                        }
                        break;

                    case 8: // CF_DIB
                    case 17: // CF_DIBV5
                        //Console.WriteLine($"Processing bitmap format: {(selectedItem.FormatId == 8 ? "CF_DIB" : "CF_DIBV5")}");
                        dataInfoList.Add($"{item.FormatName}, {item.RawData.Length} bytes");
                        dataInfoList.Add($"Format: {item.FormatName}");
                        dataInfoList.Add($"Size: {item.DataSize} bytes");
                        break;

                    case 15: // CF_HDROP
                        {
                            // Process CF_HDROP using item.RawData
                            // Pin the raw data
                            GCHandle handle = GCHandle.Alloc(item.RawData, GCHandleType.Pinned);
                            try
                            {
                                IntPtr pData = handle.AddrOfPinnedObject();

                                // Read the DROPFILES structure
                                DROPFILES dropFiles = Marshal.PtrToStructure<DROPFILES>(pData);

                                // Determine if file names are Unicode
                                bool isUnicode = dropFiles.fWide != 0;
                                Encoding encodingType;
                                if (isUnicode)
                                {
                                    encodingType = Encoding.Unicode;
                                }
                                else
                                {
                                    encodingType = Encoding.Default;
                                }

                                // Get the offset to the file list
                                int fileListOffset = (int)dropFiles.pFiles;

                                // Read the file names from item.RawData starting at fileListOffset
                                List<string> fileNames = new List<string>();
                                if (fileListOffset < item.RawData.Length)
                                {
                                    int bytesCount = item.RawData.Length - fileListOffset;
                                    byte[] fileListBytes = new byte[bytesCount];
                                    Array.Copy(item.RawData, fileListOffset, fileListBytes, 0, bytesCount);


                                // Convert to string
                                string fileListString = encodingType.GetString(fileListBytes);

                                // Split on null character
                                string[] files = fileListString.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                                fileNames.AddRange(files);

                                }

                                // Add the file count and file paths to dataInfoList
                                dataInfoList.Add($"File Drop: {fileNames.Count} file(s)");
                                dataInfoList.AddRange(fileNames);
                            }
                            finally
                            {
                                handle.Free();
                            }
                            break;
                        }

                    case 16: // CF_LOCALE
                        string dataInfo = "Invalid CF_LOCALE data"; // Default to invalid data
                        if (item.RawData.Length >= 4)
                        {
                            int lcid = BitConverter.ToInt32(item.RawData, 0);
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
                        dataInfoList.Add(dataInfo);

                        break;

                    default:
                        //Console.WriteLine($"Processing unknown format: {selectedItem.FormatId}");
                        if (item.RawData == null)
                        {
                            dataInfoList.Add("[null]");
                        }
                        else
                        {
                            dataInfoList.Add("");
                        }
                        
                        break;
                }

                item.Data = processedData; // Update the processed data in the selectedItem
                item.DataInfoList = dataInfoList; // Update the data info in the selectedItem

                // Determine handle type
                string formatType = "";
                // If it's below 0xC0000 it's a standard format type. If it's between 0xC0000 and 0xFFFF it's a registered type.
                if (item.FormatId < 0xC000)
                {
                    formatType = "Standard";
                }
                else if (item.FormatId >= 0xC000 && item.FormatId <= 0xFFFF)
                {
                    formatType = "Registered";
                }
                else
                {
                    formatType = "Unknown";
                }

                if (item.AssumedSynthesized)
                {
                    formatType = "Synthesized";
                }

                item.FormatType = formatType; // Update the format type in the selectedItem

                UpdateClipboardItemsGridView(formatName: item.FormatName, formatID: item.FormatId.ToString(), handleType: formatType, dataSize: item.DataSize.ToString(), dataInfo: item.DataInfoList, rawData: item.RawData);
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

        private string GetClipboardFormatName(uint format)
        {
            // Ensure the format ID is not above the maximum of 0xFFFF, or below 1 (it shouldn't be but just in case)
            if (format > 0xFFFF || format < 1)
            {
                return GetStandardFormatName(format);
            }

            // Define a sufficient buffer size
            StringBuilder formatName = new StringBuilder(256);
            int result = NativeMethods.GetClipboardFormatNameA(format, formatName, formatName.Capacity);

            if (result > 0)
            {
                return formatName.ToString();
            }
            else
            {
                return GetStandardFormatName(format);
            }
        }



        internal class ClipboardFormatData
        {
            public uint Format { get; set; }
            public IntPtr Data { get; set; }
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
                richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
                richTextBoxContents.Text = "Data not available";
                richTextBoxContents.ForeColor = Color.Red;
                richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;

                richTextBox_HexPlaintext.TextChanged -= richTextBox_HexPlaintext_TextChanged;
                richTextBox_HexPlaintext.Text = "";
                richTextBox_HexPlaintext.TextChanged += richTextBox_HexPlaintext_TextChanged;

                // Disable plaintext box and related controls
                richTextBox_HexPlaintext.Enabled = false;
                checkBoxPlainTextEditing.Enabled = false;
                dropdownHexToTextEncoding.Enabled = false; 

                return;
            }

            int modeIndex = dropdownContentsViewMode.SelectedIndex;

            // For data larger than 50K, display a warning and don't display the data unless the checkbox is checked
            if (modeIndex != 3 && item.RawData.Length > maxRawSizeDefault)
            {
                if (!menuOptions_ShowLargeHex.Checked)
                {
                    richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged; // Don't trigger update event handler so it doesn't try to parse it as hex
                    richTextBoxContents.Text = "Data is too large to display preview.\nThis can be changed in the options menu, but the program may freeze for large amounts of data.";
                    richTextBoxContents.ForeColor = Color.Red;
                    richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;

                    richTextBox_HexPlaintext.TextChanged -= richTextBox_HexPlaintext_TextChanged;
                    richTextBox_HexPlaintext.Text = "";
                    richTextBox_HexPlaintext.TextChanged += richTextBox_HexPlaintext_TextChanged;

                    // Disable plaintext box and related controls
                    richTextBox_HexPlaintext.Enabled = false;
                    checkBoxPlainTextEditing.Enabled = false;
                    dropdownHexToTextEncoding.Enabled = false;

                    return;
                }
            }

            // Enable plaintext box and related controls if not already
            richTextBox_HexPlaintext.Enabled = true;
            checkBoxPlainTextEditing.Enabled = true;
            dropdownHexToTextEncoding.Enabled = true;

            // Set color to black for default
            richTextBoxContents.ForeColor = Color.Black;

            switch (modeIndex)
            {
                case 0: // Text view mode
                    richTextBoxContents.Text = TryParseText(item.RawData, maxLength: 0, prefixEncodingType: false, debugging_formatName: item.FormatName, debugging_callFrom: "Contents Text Box / DisplayClipboardData");
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
                    richTextBoxContents.Text = FormatInspector.CreateFormatDataStringForTextbox(formatName: GetStandardFormatName(item.FormatId), data: item.RawData, fullItem: item);
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

            // --------------------------------------------------------------------------------
            static string EscapeString(string inputString)
            {
                return inputString.Replace("\0", "\\0").Replace("\a", "\\a").Replace("\b", "\\b").Replace("\f", "\\f").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("\v", "\\v");
            }
            static string ReplaceEscapeWithChar(string inputString)
            {
                const string rep = "."; // Replacement character
                return inputString.Replace("\0", rep).Replace("\a", rep).Replace("\b", rep).Replace("\f", rep).Replace("\n", rep).Replace("\r", rep).Replace("\t", rep).Replace("\v", rep);
            }
            // --------------------------------------------------------------------------------

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
            richTextBox_HexPlaintext.SelectionChanged -= richTextBox_HexPlaintext_SelectionChanged;
            richTextBox_HexPlaintext.Text = plaintext;
            richTextBox_HexPlaintext.TextChanged += richTextBox_HexPlaintext_TextChanged;
            richTextBox_HexPlaintext.SelectionChanged += richTextBox_HexPlaintext_SelectionChanged;
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
            static string UnescapeString(string inputString)
            {
                return inputString.Replace("\\0", "\0").Replace("\\a", "\a").Replace("\\b", "\b").Replace("\\f", "\f").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\v", "\v");
            }
            string text = UnescapeString(richTextBox_HexPlaintext.Text);


            // Convert the text to bytes
            byte[] byteData = encoding.GetBytes(text);

            // Convert the bytes to hex
            string hexString = BitConverter.ToString(byteData).Replace("-", " ");

            // Update the hex text box. First disable textchanged event to prevent infinite loop
            richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
            richTextBoxContents.SelectionChanged -= richTextBoxContents_SelectionChanged;
            richTextBoxContents.Text = hexString;
            richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;
            richTextBoxContents.SelectionChanged += richTextBoxContents_SelectionChanged;

        }


        private bool SaveClipboardData(List<uint> formatsToExclude = null)
        {
            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                Console.WriteLine("Failed to open clipboard.");
                MessageBox.Show("Failed to open clipboard.");
                return false;
            }

            try
            {
                NativeMethods.EmptyClipboard();
                foreach (var item in editedClipboardItems)
                {
                    if (formatsToExclude != null && formatsToExclude.Count !=0 && formatsToExclude.Contains(item.FormatId))
                    {
                        continue; // Skip this format if it's in the exclusion list
                    }

                    IntPtr hGlobal;

                    // Special handling for certain filetypes if necessary
                    switch (item.FormatId)
                    {
                        case 2: // CF_BITMAP
                            using (MemoryStream ms = new MemoryStream(item.RawData))
                            using (Bitmap bmp = new Bitmap(ms))
                            {
                                hGlobal = FormatHandleTranslators.Bitmap_hBitmapHandle_FromHandle(bmp.GetHbitmap());
                            }
                            break;
                        case 3: // CF_METAFILEPICT
                            hGlobal = FormatHandleTranslators.MetafilePict_Handle_FromRawData(item.RawData);
                            break;
                        case 9: // CF_PALETTE
                            hGlobal = FormatHandleTranslators.CF_PALETTE_Handle_FromRawData(item.RawData);
                            break;
                        case 14: // CF_ENHMETAFILE
                            hGlobal = FormatHandleTranslators.EnhMetafile_Handle_FromRawData(item.RawData);
                            break;
                        case 15: // CF_HDROP
                            hGlobal = FormatHandleTranslators.CF_HDROP_Handle_FromRawData(item.RawData);
                            break;
                        case 8: // CF_DIB
                        case 17: // CF_DIBV5
                            hGlobal = FormatHandleTranslators.BitmapDIB_hGlobalHandle_FromHandle(FormatHandleTranslators.AllocateGeneralHandle_FromRawData(item.RawData));
                            break;

                        // Default handling for all other formats
                        default:
                            hGlobal = FormatHandleTranslators.AllocateGeneralHandle_FromRawData(item.RawData);
                            break;
                    }

                    if (hGlobal != IntPtr.Zero)
                    {
                        if (NativeMethods.SetClipboardData(item.FormatId, hGlobal) == IntPtr.Zero)
                        {
                            NativeMethods.GlobalFree(hGlobal);
                            Console.WriteLine($"Failed to set clipboard data for format: {item.FormatId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to allocate memory for format: {item.FormatId}");
                    }
                }
                // Only show the message if saving edits. If just removing it will be visually apparent the change has been made
                if (formatsToExclude == null)
                {
                    MessageBox.Show("Clipboard data saved successfully.");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving clipboard data: {ex.Message}");
                MessageBox.Show($"Error saving clipboard data: {ex.Message}");
                return false;
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
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
        public string FormatType { get; set; } = "Unknown";

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
                HasPendingEdit = false,
                FormatType = this.FormatType
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

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetClipboardFormatNameA(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

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

        [DllImport("gdi32.dll")]
        public static extern IntPtr CopyMetaFile(IntPtr hMF, string lpFileName);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteMetaFile(IntPtr hMF);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SetEnhMetaFileBits(uint cbBuffer, byte[] lpData);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteEnhMetaFile(IntPtr hemf);

        [DllImport("gdi32.dll")]
        public static extern int GetMetaFileBitsEx(IntPtr hmf, int nSize, [In, Out] byte[] lpvData);

        [DllImport("gdi32.dll")]
        public static extern uint GetEnhMetaFileBits(IntPtr hemf, uint cbBuffer, [In, Out] byte[] lpbBuffer);

        [DllImport("shell32.dll", CharSet = CharSet.Ansi)]
        public static extern uint DragQueryFileA(IntPtr hDrop, uint iFile, [Out] StringBuilder lpszFile, uint cch);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreatePalette([In] ref LOGPALETTE lplgpl);

        public const uint GMEM_MOVEABLE = 0x0002;

        public const uint GMEM_ZEROINIT = 0x0040;
    }

}