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
// Nullable reference types
#nullable enable

// My classes
using EditClipboardContents;
using System.Collections;
using static EditClipboardContents.ClipboardFormats;
using System.Threading.Tasks;
using System.Data.Common;
using System.Windows.Forms.Automation;


namespace EditClipboardContents
{
    public partial class MainForm : Form
    {
        private readonly List<ClipboardItem> clipboardItems = new List<ClipboardItem>();
        private List<ClipboardItem> editedClipboardItems = new List<ClipboardItem>(); // Add this line

        // Other globals
        public static bool anyPendingChanges = false;
        public static bool enableSplitHexView = false;
        public ClipboardItem? itemBeforeCellEditClone = null;

        // Global constants
        public const int maxRawSizeDefault = 50000;

        // Variables to store info about initial GUI state
        public int hexTextBoxTopBuffer { get; init; }
        public string defaultLoadingLabelText { get; init; }
        public Color defaultCellForeColor { get; init; }

        // Store recent GUI states
        public int previousWindowHeight = 0;
        public int previousSplitterDistance = 0;
        public bool isResizing = false;

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

            // Set init only GUI state variables
            hexTextBoxTopBuffer = richTextBoxContents.Height - richTextBox_HexPlaintext.Height;
            defaultLoadingLabelText = labelLoading.Text;
            defaultCellForeColor = dataGridViewClipboard.DefaultCellStyle.ForeColor;

            InitializeDataGridView();
            UpdateToolLocations();

            // Initial tool settings
            dropdownContentsViewMode.SelectedIndexChanged -= dropdownContentsViewMode_SelectedIndexChanged;
            dropdownHexToTextEncoding.SelectedIndexChanged -= dropdownHexToTextEncoding_SelectedIndexChanged;
            dropdownContentsViewMode.SelectedIndex = 0; // Default index 0 is "Text" view mode
            dropdownHexToTextEncoding.SelectedIndex = 0; // Default index 0 is "UTF-8" encoding
            dropdownContentsViewMode.SelectedIndexChanged += dropdownContentsViewMode_SelectedIndexChanged;
            dropdownHexToTextEncoding.SelectedIndexChanged += dropdownHexToTextEncoding_SelectedIndexChanged;

            // Set color of toolstrip manually because it doesn't set it apparently
            toolStrip1.BackColor = SystemColors.Control;

            // Set the version number label
            labelVersion.Text = $"Version {versionString}";

            previousWindowHeight = this.Height;
        }

        public int CompensateDPI(int originalValue)
        {
            float scaleFactor = this.DeviceDpi / 96f; // 96 is the default DPI
            return (int)(originalValue * scaleFactor);
        }

        public static int CompensateDPIStatic(int originalValue)
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpi = graphics.DpiX;
                float scaleFactor = dpi / 96f; // 96 is the default DPI
                return (int)(originalValue * scaleFactor);
            }
        }

        private void ShowLoadingIndicator(bool show)
        {
            // Calculate the position of the loading label based on center of SplitContainerMain Panel 1. Also ensures location uses label center
            int x = splitContainerMain.Panel1.Width / 2 - labelLoading.Width / 2;
            int y = splitContainerMain.Panel1.Height / 2 - labelLoading.Height / 2;

            labelLoading.Location = new Point(x, y);
            labelLoading.Visible = show;

            // Updates the cursor
            this.Cursor = show ? Cursors.WaitCursor : Cursors.Default;

            // Force the form to repaint immediately
            this.Update();
        }

        private void InitializeDataGridView()
        {
            // If addng a new column, don't forget to add it to the UpdateClipboardItemsGridViewWith_AdditionalItem function on the line where rows are added
            
            dataGridViewClipboard.Columns.Add(colName.Index, "");
            dataGridViewClipboard.Columns.Add(colName.UniqueID, ""); // This will be invisible but don't put it first or last to avoid default selection behavior. For IDing custom formats that may have same id or name
            dataGridViewClipboard.Columns.Add(colName.FormatName, "Format Name");
            dataGridViewClipboard.Columns.Add(colName.FormatId, "Format ID");
            dataGridViewClipboard.Columns.Add(colName.HandleType, "Format Type");
            dataGridViewClipboard.Columns.Add(colName.DataSize, "Data Size");
            dataGridViewClipboard.Columns.Add(colName.DataInfo, "Data Info");
            dataGridViewClipboard.Columns.Add(colName.TextPreview, "Text Preview");
            

            // Set autosize for all columns to none so we can control individually later
            foreach (DataGridViewColumn column in dataGridViewClipboard.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            }

            // Set unique id column to be invisible
            dataGridViewClipboard.Columns[colName.UniqueID].Visible = false;

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
            Padding formatNamePadding = dataGridViewClipboard.Columns["TextPreview"].DefaultCellStyle.Padding;
            formatNamePadding.Left = 3;
            dataGridViewClipboard.Columns["FormatName"].DefaultCellStyle.Padding = formatNamePadding;

            // Hide the row headers (the leftmost column)
            dataGridViewClipboard.RowHeadersVisible = false;

            // Set miscellaensous properties for specific columns
            dataGridViewClipboard.Columns["Index"].DefaultCellStyle.ForeColor = Color.Gray;
            dataGridViewClipboard.Columns["Index"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Add event handler for scroll wheel
            dataGridViewClipboard.MouseWheel += new MouseEventHandler(dataGridViewClipboard_MouseWheel);
        }

        private void UpdateClipboardItemsGridView_WithEmptyCustomFormat(ClipboardItem formatItem)
        {
            // Get needed data from the item
            //byte[] rawData = formatItem.RawData;
            string formatName = formatItem.FormatName;
            Guid uniqueID = formatItem.UniqueID;
            string formatType = formatItem.FormatType;
            string formatID = formatItem.FormatId.ToString();
            //List<string> dataInfo = formatItem.DataInfoList;
            string dataSize = formatItem.DataSize.ToString();
            int index = formatItem.OriginalIndex;
            
            string dataInfoString = MyStrings.CustomPendingData;
            string textPreview = "";

            dataGridViewClipboard.SelectionChanged -= dataGridViewClipboard_SelectionChanged;
            dataGridViewClipboard.Rows.Add(index, uniqueID, formatName, formatID, formatType, dataSize, dataInfoString, textPreview);
            dataGridViewClipboard.SelectionChanged += dataGridViewClipboard_SelectionChanged;
        }

        // Update processedData grid view with clipboard contents during refresh
        private void UpdateClipboardItemsGridViewWith_AdditionalItem(ClipboardItem formatItem)
        {
            // Get needed data from the item
            byte[]? rawData = formatItem.RawData;
            string formatName = formatItem.FormatName;
            string formatType = formatItem.FormatType;
            string formatID = formatItem.FormatId.ToString();
            List<string> dataInfo = formatItem.DataInfoList;
            string dataSize = formatItem.DataSize.ToString();
            int index = formatItem.OriginalIndex;
            Guid uniqueID = formatItem.UniqueID;

            // Preprocess certain info
            string textPreview = TryParseText(rawData, maxLength: 200, prefixEncodingType: false);

            // The first item will have selected important info, to ensure it's not too long. The rest will show in data box in object/struct view mode
            string dataInfoString;
            if (dataInfo.Count <= 0 || string.IsNullOrEmpty(dataInfo[0]))
            {
                dataInfoString = MyStrings.DataNotApplicable;
            }
            else
            {
                dataInfoString = dataInfo[0];
            }

            // Manually handle certain known formats
            if (formatName == "CF_LOCALE")
            {
                textPreview = "";
            }

            // Add info to the grid view, then will be resized
            // Disable selected index changed event to prevent it from firing
            dataGridViewClipboard.SelectionChanged -= dataGridViewClipboard_SelectionChanged;
            dataGridViewClipboard.Rows.Add(index, uniqueID, formatName, formatID, formatType, dataSize, dataInfoString, textPreview);
            dataGridViewClipboard.SelectionChanged += dataGridViewClipboard_SelectionChanged;

            // Temporarily set AutoSizeMode to calculate proper widths
            foreach (DataGridViewColumn column in dataGridViewClipboard.Columns)
            {
                // Manually set width to minimal number to be resized auto later. Apparently autosize will only make columns larger, not smaller
                column.Width = CompensateDPI(22);

                if (column.Name != colName.TextPreview && column.Name != colName.Index)
                {
                    // Use all cells instead of displayed cells, otherwise those scrolled out of view won't count
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
            }

            // Miscelaneous operations to always apply
            // Set index column text color to gray
            dataGridViewClipboard.Rows[dataGridViewClipboard.Rows.Count - 1].Cells[colName.Index].Style.ForeColor = Color.Gray;

            // Allow layout to update
            dataGridViewClipboard.PerformLayout();

            // Set final column properties
            foreach (DataGridViewColumn column in dataGridViewClipboard.Columns)
            {
                // Keep the autosize mode for certain columns
                if (column.Name == colName.TextPreview || column.Name == colName.Index || column.Name == colName.UniqueID)
                {
                    continue;
                }
                int originalWidth = column.Width;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.Resizable = DataGridViewTriState.True;

                if (column.Name == colName.FormatName)
                {
                    column.Width = originalWidth + 20; // Add some padding
                } 
                else
                {
                    column.Width = originalWidth + 0; // For some reason this is necessary after setting resizable and autosize modes
                }
            }

            // Ensure TextPreview fills remaining space
            dataGridViewClipboard.Columns[colName.TextPreview].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewClipboard.Columns[colName.TextPreview].Resizable = DataGridViewTriState.True;

            // If DataInfo is too long, manually set a max width
            if (dataGridViewClipboard.Columns[colName.DataInfo].Width > CompensateDPI(200))
            {
                dataGridViewClipboard.Columns[colName.DataInfo].Width = CompensateDPI(200);
            }

            // Reset selection to none
            dataGridViewClipboard.ClearSelection();
        }

        // Function to try and parse the raw data for text if it is text
        private string TryParseText(byte[]? rawData, int maxLength = 150, bool prefixEncodingType = false)
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
        private void UpdateToolLocations(WhichPanelResize splitAnchor = WhichPanelResize.None, int sizeDiff = 0)
        {
            splitContainerMain.SplitterMoved -= new SplitterEventHandler(splitContainerMain_SplitterMoved);
            int titlebarAccomodate = CompensateDPI(40);
            int bottomBuffer = CompensateDPI(30); // Adjust this value to set the desired buffer size
            int splitterPanelsBottomPosition = this.Height - toolStrip1.Height - titlebarAccomodate;

            //int splitDistancebeforeToolAdjust = splitContainerMain.SplitterDistance;
            int splitDistancebeforeToolAdjust = previousSplitterDistance;

            // Calculate difference between form height and splitter distance
            int splitDistanceBottomBeforeToolAdjust = splitContainerMain.Height - splitDistancebeforeToolAdjust;

            // Resize splitContainerMain to fit the form
            splitContainerMain.Width = this.Width - CompensateDPI(32);
            splitContainerMain.Height = splitterPanelsBottomPosition - bottomBuffer;


            // "Anchors" the splitter to prevent either top or bottom panel from resizing based on visibility of data grid view cells
            // This only applies if the window is being resized, not if the splitter is being moved manually
            if (splitAnchor != WhichPanelResize.None)
            {
                // Before setting SplitterDistance, ensure the value is valid
                int maxSplitterDistance = splitContainerMain.Height - splitContainerMain.Panel2MinSize - CompensateDPI(150);
                int minSplitterDistance = splitContainerMain.Panel1MinSize + CompensateDPI(150);

                // Position splitter based on anchoring
                if (splitAnchor == WhichPanelResize.Bottom)
                {
                    int desiredSplitterDistance = splitDistancebeforeToolAdjust;
                    splitContainerMain.SplitterDistance = Math.Max(minSplitterDistance, Math.Min(desiredSplitterDistance, maxSplitterDistance));
                }
                else if (splitAnchor == WhichPanelResize.Top)
                {
                    int desiredSplitterDistance = splitContainerMain.Height - splitDistanceBottomBeforeToolAdjust;
                    desiredSplitterDistance = Math.Max(minSplitterDistance, Math.Min(desiredSplitterDistance, maxSplitterDistance));
                    splitContainerMain.SplitterDistance = desiredSplitterDistance;
                }
            } // End of anchoring

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

            previousSplitterDistance = splitContainerMain.SplitterDistance;
            splitContainerMain.SplitterMoved += new SplitterEventHandler(splitContainerMain_SplitterMoved);
        }

        private void RefreshClipboardItems()
        {
            ShowLoadingIndicator(true);

            // Attempt to open the clipboard, retrying up to 10 times with a 10ms delay
            //Console.WriteLine("Attempting to open clipboard");
            int retryCount = 10;  // Number of retries
            int retryDelay = 10;  // Delay in milliseconds
            bool clipboardOpened = false;

            //TestingWinFormsClipboard(); // Debugging

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

            ShowLoadingIndicator(false);
            UpdateSplitterPosition_FitDataGrid();

            UpdateEditControlsVisibility_AndPendingGridAppearance();
        }

        private void CopyClipboardData()
        {
            clipboardItems.Clear();
            editedClipboardItems.Clear();
            int formatCount = NativeMethods.CountClipboardFormats();
            uint format = 0;
            int actuallyLoadableCount = 0;
            List<uint> formatsToRetry = new List<uint>();

            while (true)
            {
                format = NativeMethods.EnumClipboardFormats(format);
                if (format == 0)
                {
                    int enumError = Marshal.GetLastWin32Error();
                    if (enumError == 0) // ERROR_SUCCESS -- No more formats to enumerate
                    {
                        // End of enumeration
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"EnumClipboardFormats failed. Error code: {enumError}");
                        MessageBox.Show($"An error occurred trying to retrieve the list of clipboard items:\n Error Code: {enumError}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                }

                actuallyLoadableCount++; // Only increments if the format is successfully enumerated. Not if it reached end, or there was an error

                // Update label to show progress
                labelLoading.Text = $"{defaultLoadingLabelText}\n\n" + $"Loading: {actuallyLoadableCount} of {formatCount}...";
                // Update the form to show the new label
                this.Update();

                // -------- Start / Continue Enumeration ------------
                bool successResutl = CopyIndividualClipboardFormat(formatId: format, loadedFormatCount: actuallyLoadableCount);
                if (!successResutl)
                {
                    formatsToRetry.Add(format);
                }

            }
            //Console.WriteLine($"Checked {actuallyLoadableCount} formats out of {formatCount} reported formats.");
            if (actuallyLoadableCount < formatCount)
            {
                Console.WriteLine("Warning: Not all reported formats were enumerated.");
            }

            // Retry any formats that failed
            int retryCount = 1;
            foreach (uint formatId in formatsToRetry)
            {
                labelLoading.Text = $"{defaultLoadingLabelText}\n\n" + $"Retrying: {retryCount} of {formatsToRetry.Count}...";
                this.Update();
                RetryCopyClipboardFormat(formatId);
                retryCount++;
            }

        }

        private void RetryCopyClipboardFormat(uint formatId, string specifiedFormatName = "")
        {
            bool successResult = false;
            // First open the clipboard
            if (NativeMethods.OpenClipboard(this.Handle))
            {
                // Try the normal way again first
                ClipboardItem item;
                try
                {
                    CopyIndividualClipboardFormat(formatId, retryMode: true);
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                    // Wait a short time
                    System.Threading.Thread.Sleep(25);
                }

                // If the clipboard item is still null, it might be delayed render. Send request to the application to render the data
                item = clipboardItems.FirstOrDefault(item => item.FormatId == formatId);
                if (item != null && item.RawData == null)
                {
                    IntPtr windowHandle;
                    // Send a delayed rendering request to the application. The window handle should be available in the diagnostics report
                    if (item.ErrorDiagnosisReport != null && item.ErrorDiagnosisReport.OwnerWindowHandle != null)
                    {
                        windowHandle = item.ErrorDiagnosisReport.OwnerWindowHandle;
                    }
                    else
                    {
                        windowHandle = IntPtr.Zero;
                    }

                    bool result = RequestDelayedRendering(windowHandle, formatId);

                    if (result == true)
                    {
                        // If the delayed rendering was successful, retry the format
                        try
                        {
                            NativeMethods.OpenClipboard(this.Handle);
                            successResult = CopyIndividualClipboardFormat(formatId, retryMode: true);
                        }
                        finally
                        {
                            NativeMethods.CloseClipboard();
                        }
                    }
                }
            }
        }

        private (bool,bool) ManuallyCopySpecifiedClipboardFormat(uint formatId = 0, string? formatName = null, bool silent = false)
        {
            bool successResult = false;
            bool existingItem = false;

            ClipboardItem item;
            // Check if the format is already in the list, to know whether to use retry mode or not
            if (formatName != null)
            {
                // Look for exact match first
                item = clipboardItems.FirstOrDefault(item => item.FormatName == formatName);

                // Look for non-case sensitive match
                if (item == null)
                {
                    item = clipboardItems.FirstOrDefault(item => item.FormatName.ToLower().Contains(formatName.ToLower()));
                }
            }
            else if (formatId != 0)
            {
                item = clipboardItems.FirstOrDefault(item => item.FormatId == formatId);
            }
            else
            {
                if (!silent)
                {
                    MessageBox.Show("No format ID or name specified", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return (successResult, existingItem);
            }

            // If the item was not found and only the format name is provided, we can compare it against known and registered formats to find the ID
            if (item == null && formatId == 0 && formatName != null)
            {
                formatId = Utils.GetClipboardFormatIdFromName(formatName, caseSensitive: false);
            }

            // We need a format ID at least
            if (formatId == 0)
            {
                if (formatName != null)
                {
                    if (!silent)
                    {
                        MessageBox.Show("Error: Format name does not appear to match any standard or currently registered formats on the system.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    if (!silent)
                    {
                        MessageBox.Show("Error: No valid format ID given.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                return (successResult, existingItem);
            }

            bool retryMode;
            if (item == null)
            {
                retryMode = false;
            }
            else
            {
                retryMode = true;
                existingItem = true;
            }

            if (NativeMethods.OpenClipboard(this.Handle))
            {
                try
                {
                    successResult = CopyIndividualClipboardFormat(formatId, retryMode: retryMode);
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }

            return (successResult, existingItem);
        }


        private bool CopyIndividualClipboardFormat(uint formatId, int loadedFormatCount = -1, bool retryMode = false)
        {
            string formatName = Utils.GetClipboardFormatName(formatId);
            ulong dataSize = 0;
            byte[]? rawData = null;
            int? error; // Initializes as null anyway
            string? errorString = null;
            DiagnosticsInfo? diagnosisReport = null;
            int originalIndex = loadedFormatCount - 1; // Stored for reference of clipboard order. If loadCount is -1, it means it's a retry
            bool copySuccess = true;

            //Console.WriteLine($"Checking Format {actuallyLoadableCount}: {formatName} ({format})"); // Debugging

            IntPtr hData = NativeMethods.GetClipboardData(formatId);
            if (hData == IntPtr.Zero)
            {
                copySuccess = false;
                error = Marshal.GetLastWin32Error();
                string errorMessage = GetWin32ErrorMessage(error);

                Console.WriteLine($"GetClipboardData returned null for format {formatId}. Error: {error} | Message: {errorMessage}");

                if (!string.IsNullOrEmpty(formatName))
                {
                    diagnosisReport = (DiagnoseClipboardState(formatId, formatName));
                }
                else
                {
                    diagnosisReport = DiagnoseClipboardState(formatId);
                }

                if (error == null)
                {
                    errorString = "[Unknown Error]";
                }
                else if (error == 5)
                {
                    errorString = "[Error : Access Denied]";
                }
                else if (error == 0)
                {
                    errorString = null;
                }
                else
                {
                    errorString = $"[Error {error}]";
                }

            }

            try
            {
                // First need to specially handle certain formats that don't use HGlobal
                switch (formatId)
                {
                    case 2: // CF_BITMAP
                        // Test getting raw data using GetDIBits
                        //byte[] rawImageBitsOnly = FormatConverters.DIBits_From_HBitmap(hData);
                        //string byteStringHex = BitConverter.ToString(rawImageBitsOnly).Replace("-", " ");

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
                        rawData = FormatConverters.MetafilePict_RawData_FromHandle(hData);
                        dataSize = (ulong)(rawData?.Length ?? 0);
                        break;
                    case 9: // CF_PALETTE -- NOT YET HANDLED
                        rawData = FormatConverters.CF_PALETTE_RawData_FromHandle(hData);
                        dataSize = (ulong)(rawData?.Length ?? 0);
                        break;
                    case 14: // CF_ENHMETAFILE
                        rawData = FormatConverters.EnhMetafile_RawData_FromHandle(hData);
                        dataSize = (ulong)(rawData?.Length ?? 0);
                        break;
                    case 15: // CF_HDROP
                        rawData = FormatConverters.CF_HDROP_RawData_FromHandle(hData);
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
                                Console.WriteLine($"Error processing format {formatId}: {ex.Message}");
                            }
                            finally
                            {
                                NativeMethods.GlobalUnlock(hData);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"GlobalLock returned null for format {formatId}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing format {formatId}: {ex.Message}");
                copySuccess = false;
            }
            if (retryMode == false)
            {
                var item = new ClipboardItem
                {
                    FormatName = formatName,
                    FormatId = formatId,
                    Handle = hData,
                    DataSize = dataSize,
                    RawData = rawData,
                    ProcessedData = null,
                    ErrorReason = errorString,
                    ErrorDiagnosisReport = diagnosisReport,
                    OriginalIndex = originalIndex
                };
                clipboardItems.Add(item);
            }
            else
            {
                // If the retry was a success, update the existing item
                // Find the item in the list
                var item = clipboardItems.FirstOrDefault(item => item.FormatId == formatId);
                if (item != null)
                {
                    item.RawData = rawData;
                    item.DataSize = dataSize;
                    item.ErrorReason = errorString;
                    item.ErrorDiagnosisReport = diagnosisReport;
                }
            }

            return copySuccess;
        } // End of CopyIndividualClipboardFormat

        public static bool RequestDelayedRendering(IntPtr windowHandle, uint format)
        {
            IntPtr hWnd = windowHandle;
            //IntPtr hWnd = NativeMethods.FindWindow(null, windowName);

            if (hWnd == IntPtr.Zero)
            {
                throw new Exception("Window not found");
            }

            IntPtr result = NativeMethods.SendMessage(hWnd, NativeMethods.WM_RENDERFORMAT, (IntPtr)format, IntPtr.Zero);

            // Wait a bit for the application to process the message. The user can always manually refresh if it doesn't work
            System.Threading.Thread.Sleep(50);

            // Check if the application is still processing
            if (result == IntPtr.Zero)
            {
                // The application is still processing
                return false;
            }

            // The application has completed processing
            return true;
        }

        public static string GetWin32ErrorMessage(int? inputError)
        {

            int errorCode;
            if (inputError == null)
            {
                return "[Unknown Error]";
            }
            else
            {
                errorCode = (int)inputError;
            }

            const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
            const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;

            StringBuilder sb = new StringBuilder(256);
            int length = NativeMethods.FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                errorCode,
                0, // Use system's current language
                sb,
                sb.Capacity,
                IntPtr.Zero
            );

            if (length == 0)
            {
                return $"Unknown error (0x{errorCode:X})";
            }

            return sb.ToString().Trim();
        }

        private void ProcessClipboardData()
        {
            dataGridViewClipboard.Rows.Clear();

            foreach (var item in clipboardItems)
            {
                byte[]? processedData = null;
                ClipDataObject? processedObject = null;

                // Data info list contains metadata about the data. First item will show in the data info column, all will show in the text box in object/struct view mode
                List<string> dataInfoList = new List<string>();

                // If there is data, process it and get the data info
                if (item?.RawData != null && item.RawData.Length > 0)
                {
                    (dataInfoList, processedData, processedObject) = SetDataInfo(formatName: item.FormatName, rawData: item.RawData);
                }
                // If there is no data, and there is an error message
                else if (item != null && !string.IsNullOrEmpty(item.ErrorReason))
                {
                    dataInfoList.Add(item.ErrorReason ?? string.Empty);
                }
                // If the data is null
                else if (item?.RawData == null)
                {
                    dataInfoList.Add("[null]");
                }
                // If the data isn't null but still empty
                else if (item.RawData.Length == 0)
                {
                    dataInfoList.Add("[Empty]");
                }

                if (item == null) // It should never be null here, but just in case of side effects or something
                {
                    continue;
                }

                item.ProcessedData = processedData; // Update the processed data in the selectedItem
                item.DataInfoList = dataInfoList; // Update the data info in the selectedItem

                // Determine format type. If it's below 0xC0000 it's a standard format type.
                // See here for details about the specific ranges: https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats
                string formatType;
                if (item.FormatId == 0x0082 || item.FormatId == 0x008E || item.FormatId == 0x0083 || item.FormatId == 0x0081)
                {
                    formatType = "Standard / Display";
                }
                else if (item.FormatId >= 0x0200 && item.FormatId <= 0x02FF) // 512 - 767
                {
                    formatType = "Private";
                }
                else if (item.FormatId >= 0x0300 && item.FormatId <= 0x03FF) // 768 - 1023
                {
                    formatType = "Global GDI Object";
                }
                else if (item.FormatId >= 0xC000 && item.FormatId <= 0xFFFF) // 49152 - 65535
                {
                    formatType = "Registered";
                }
                else if (item.FormatId < 0xC000) // Otherwise under 49152
                {
                    formatType = "Standard";
                }
                else
                {
                    formatType = "Unknown";
                }

                if (item == null) // It should never be null here, but just in case of side effects or something
                {
                    continue;
                }

                // All synthesized formats are standard so just override the type if so
                if (item.AssumedSynthesized)
                {
                    formatType = "Synthesized";
                }

                item.FormatType = formatType; // Update the format type in the selectedItem
                item.ClipDataObject = processedObject; // Update the clipDataObject in the selectedItem, even if it's null

                UpdateClipboardItemsGridViewWith_AdditionalItem(formatItem: item);
            }
        }

        public static DiagnosticsInfo DiagnoseClipboardState(uint format, string formatName = "")
        {
            DiagnosticsInfo diagnostics = new DiagnosticsInfo();
            StringBuilder diagnosisString = new StringBuilder();
            int currentError;
            NativeMethods.SetLastErrorEx(0, 0); // Clear last error

            diagnosisString.AppendLine("------------------------------------------------------");

            if (!string.IsNullOrEmpty(formatName))
            {
                diagnosisString.AppendLine($"Diagnosing clipboard state for format: {formatName} ({format})");
            }
            else
            {
                diagnosisString.AppendLine($"Diagnosing clipboard state for format: {format}");
            }

            // Check if the format is available
            bool isFormatAvailable = NativeMethods.IsClipboardFormatAvailable(format);
            diagnosisString.AppendLine($"Is format available: {isFormatAvailable}");
            diagnostics.IsFormatAvailable = isFormatAvailable;

            // Clipboard should already be opened by the caller

            // Set alias for NativeMethods.FormatMessage
            static string ErrMsg(int errorCode)
            {
                return GetWin32ErrorMessage(errorCode);
            }

            try
            {
                // Get clipboard owner
                IntPtr hOwner = NativeMethods.GetClipboardOwner();
                diagnosisString.AppendLine($"Clipboard owner handle: 0x{hOwner.ToInt64():X}");
                diagnostics.OwnerHandle = hOwner;

                if (hOwner != IntPtr.Zero)
                {
                    StringBuilder className = new StringBuilder(256);
                    NativeMethods.GetClassName(hOwner, className, className.Capacity);
                    diagnosisString.AppendLine($"Clipboard owner class: {className}");
                    currentError = Marshal.GetLastWin32Error();
                    diagnostics.WindowClass = className.ToString();
                    if (currentError != 0)
                    {
                        diagnosisString.AppendLine($"GetClassName failed. Error: {currentError} | {ErrMsg(currentError)}");
                    }
                    NativeMethods.SetLastErrorEx(0, 0); // Clear last error

                    StringBuilder windowText = new StringBuilder(256);
                    NativeMethods.GetWindowText(hOwner, windowText, windowText.Capacity);
                    diagnosisString.AppendLine($"Clipboard owner window text: {windowText}");
                    diagnostics.WindowText = windowText.ToString();
                    currentError = Marshal.GetLastWin32Error();
                    if (currentError != 0)
                    {
                        diagnosisString.AppendLine($"GetWindowText failed. Error: {currentError} | {ErrMsg(currentError)}");
                    }
                    NativeMethods.SetLastErrorEx(0, 0); // Clear last error

                    // Get process ID
                    NativeMethods.GetWindowThreadProcessId(hOwner, out uint processId);
                    diagnosisString.AppendLine($"Clipboard owner process ID: {processId}");
                    diagnostics.OwnerProcessID = processId;
                    currentError = Marshal.GetLastWin32Error();
                    if (currentError != 0)
                    {
                        diagnosisString.AppendLine($"GetWindowThreadProcessId failed. Error: {currentError} | {ErrMsg(currentError)}");
                    }
                    NativeMethods.SetLastErrorEx(0, 0); // Clear last error

                    // Get process name
                    try
                    {
                        using (Process process = Process.GetProcessById((int)processId))
                        {
                            diagnosisString.AppendLine($"Clipboard owner process name: {process.ProcessName}");
                            diagnostics.OwnerProcessName = process.ProcessName;
                        }
                    }
                    catch (ArgumentException)
                    {
                        diagnosisString.AppendLine("Failed to get process name. The process may have ended.");
                    }
                }
                else
                {
                    diagnosisString.AppendLine("Clipboard owner handle is null. No owner.");
                }

                // Get clipboard sequence number
                uint sequenceNumber = NativeMethods.GetClipboardSequenceNumber();
                diagnosisString.AppendLine($"Clipboard sequence number: {sequenceNumber}");
                diagnostics.ClipboardSequenceNumber = sequenceNumber;
                currentError = Marshal.GetLastWin32Error();
                if (currentError != 0)
                {
                    diagnosisString.AppendLine($"GetClipboardSequenceNumber failed. Error: {currentError} | {ErrMsg(currentError)}");
                }
                NativeMethods.SetLastErrorEx(0, 0); // Clear last error

                // Get open clipboard window
                IntPtr hOpenWindow = NativeMethods.GetOpenClipboardWindow();
                diagnosisString.AppendLine($"Open clipboard window handle: 0x{hOpenWindow.ToInt64():X}");
                diagnostics.OwnerWindowHandle = hOpenWindow;
                currentError = Marshal.GetLastWin32Error();
                if (currentError != 0)
                {
                    diagnosisString.AppendLine($"GetOpenClipboardWindow failed. Error: {currentError} | {ErrMsg(currentError)}");
                }
                NativeMethods.SetLastErrorEx(0, 0); // Clear last error

                // Attempt to get clipboard data
                IntPtr hData = NativeMethods.GetClipboardData(format);
                diagnosisString.AppendLine($"GetClipboardData result: 0x{hData.ToInt64():X}");
                currentError = Marshal.GetLastWin32Error();
                if (currentError != 0)
                {
                    diagnosisString.AppendLine($"GetClipboardData failed. Error: {currentError} | {ErrMsg(currentError)}");
                }

            }
            catch (Exception ex)
            {
                diagnosisString.AppendLine($"An exception occurred while diagnosing: {ex.Message}");
            }
            finally
            {
                //NativeMethods.CloseClipboard(); // CLipboard will be closed elsewhere
            }

            diagnosisString.AppendLine("------------------------------------------------------");

            string finalResult = diagnosisString.ToString();
            diagnostics.ReportString = finalResult;

            return diagnostics;
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

        internal class ClipboardFormatData
        {
            public uint Format { get; set; }
            public IntPtr Data { get; set; }
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

                if (selectedRow.Cells[colName.HandleType].Value.ToString() == "Custom")
                {
                    labelCustomFormatNameID.Visible = true;
                }
                else
                {
                    labelCustomFormatNameID.Visible = false;
                }
            }
        }


        private void DisplayClipboardData(ClipboardItem? item)
        {
            if (item == null || item.RawData == null)
            {
                richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
                richTextBoxContents.Text = "Data not available";
                richTextBoxContents.ForeColor = Color.Red;
                if (item?.ErrorDiagnosisReport != null && item.ErrorDiagnosisReport.ReportString != null)
                {
                    richTextBoxContents.Text += "\n\n" + "   Info about error retrieving clipboard item:" + "\n" + item.ErrorDiagnosisReport.ReportString;
                    richTextBoxContents.ForeColor = Color.DarkRed;
                }
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
                    richTextBoxContents.Text = TryParseText(item.RawData, maxLength: 0, prefixEncodingType: false);
                    richTextBoxContents.ReadOnly = true;
                    richTextBoxContents.BackColor = SystemColors.ControlLight;
                    break;

                case 1: // Hex view mode
                    // Show hex data in the left panel text box
                    richTextBoxContents.Text = BitConverter.ToString(item.RawData).Replace("-", " ");
                    richTextBoxContents.ReadOnly = true;
                    // Set the background color to gray to indicate read-only
                    richTextBoxContents.BackColor = SystemColors.ControlLight;
                    UpdatePlaintextFromHexView();
                    break;

                case 2: // Hex (Editable) view mode
                    richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
                    richTextBoxContents.Text = BitConverter.ToString(item.RawData).Replace("-", " ");
                    richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;
                    // Set the background color to white to indicate editable
                    richTextBoxContents.BackColor = SystemColors.Window;

                    richTextBoxContents.ReadOnly = false;
                    UpdatePlaintextFromHexView();
                    break;
                case 3: // Object / Struct View
                    richTextBoxContents.TextChanged -= richTextBoxContents_TextChanged;
                    richTextBoxContents.Text = FormatStructurePrinter.GetDataStringForTextbox(formatName: Utils.GetClipboardFormatName(item.FormatId), fullItem: item);
                    richTextBoxContents.TextChanged += richTextBoxContents_TextChanged;
                    richTextBoxContents.BackColor = SystemColors.ControlLight;

                    richTextBoxContents.ReadOnly = true;
                    break;

                default:
                    richTextBoxContents.Text = "Unknown view mode";
                    break;
            }
        }

        // Get data in the datagridview of the selected item for a particular column
        private string? GetSelectedDataFromDataGridView(string columnName)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                int selectedRowIndex = dataGridViewClipboard.SelectedRows[0].Index; // Just get the first selected row even if there are multiple
                return dataGridViewClipboard.Rows[selectedRowIndex].Cells[columnName].Value.ToString();
            }
            return null;
        }

        private void UpdatePlaintextFromHexView()
        {
            // Set encoding mode based on dropdown
            Encoding encodingToUse;
            if (dropdownHexToTextEncoding.SelectedIndex == 0) // UTF-8
            {
                encodingToUse = Encoding.UTF8;
            }
            else if (dropdownHexToTextEncoding.SelectedIndex == 1) // UTF-16 LE (Unicode
            {
                encodingToUse = Encoding.Unicode;
            }
            else if (dropdownContentsViewMode.SelectedIndex == 2) // UTF-16 BE
            {
                encodingToUse = Encoding.BigEndianUnicode;
            }
            else if (dropdownHexToTextEncoding.SelectedIndex == 3) // UTF-32 LE
            {
                encodingToUse = Encoding.UTF32;
            }
            else if (dropdownHexToTextEncoding.SelectedIndex == 4) // UTF-32 Big Endian
            {
                encodingToUse = Encoding.GetEncoding(12001);
            }
            else if (dropdownHexToTextEncoding.SelectedIndex == 5) // Windows-1252
            {
                encodingToUse = Encoding.GetEncoding(1252);
            }
            // System Default
            else if (dropdownHexToTextEncoding.SelectedIndex == 6) // Default
            {
                encodingToUse = Encoding.Default;
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
                // Set background color to white to indicate editable
                richTextBox_HexPlaintext.BackColor = SystemColors.Window;
            }
            else
            {
                plaintext = ReplaceEscapeWithChar(plaintextRaw); // Remove null characters
                richTextBox_HexPlaintext.BackColor = SystemColors.ControlLight;
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


        private bool SaveClipboardData()
        {
            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                Console.WriteLine("Failed to open clipboard.");
                MessageBox.Show("Failed to open clipboard.");
                return false;
            }

            List<string> erroredFormats = new List<string>();

            try
            {
                NativeMethods.EmptyClipboard();
                foreach (var item in editedClipboardItems)
                {
                    if (item.PendingRemoval == true)
                    {
                        continue; // Skip this format if it's in the exclusion list
                    }

                    IntPtr hGlobal;

                    // Try to get the handle for the data
                    try
                    {
                        // Special handling for certain filetypes if necessary
                        switch (item.FormatId)
                        {
                            case 2: // CF_BITMAP
                                using (MemoryStream ms = new MemoryStream(item.RawData))
                                using (Bitmap bmp = new Bitmap(ms))
                                {
                                    hGlobal = FormatConverters.Bitmap_hBitmapHandle_FromHandle(bmp.GetHbitmap());
                                }
                                break;
                            case 3: // CF_METAFILEPICT
                                hGlobal = FormatConverters.MetafilePict_Handle_FromRawData(item.RawData);
                                break;
                            case 9: // CF_PALETTE
                                hGlobal = FormatConverters.CF_PALETTE_Handle_FromRawData(item.RawData);
                                break;
                            case 14: // CF_ENHMETAFILE
                                hGlobal = FormatConverters.EnhMetafile_Handle_FromRawData(item.RawData);
                                break;
                            case 15: // CF_HDROP
                                hGlobal = FormatConverters.CF_HDROP_Handle_FromRawData(item.RawData);
                                break;
                            case 8: // CF_DIB
                            case 17: // CF_DIBV5
                                hGlobal = FormatConverters.BitmapDIB_hGlobalHandle_FromHandle(FormatConverters.AllocateGeneralHandle_FromRawData(item.RawData));
                                break;

                            // Default handling for all other formats
                            default:
                                hGlobal = FormatConverters.AllocateGeneralHandle_FromRawData(item.RawData);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = ex.Message.Replace("\n", "  ").Replace("\r", "");
                        Console.WriteLine($"Error allocating memory for format {item.FormatId}: {message}");
                        erroredFormats.Add($"ID: {item.FormatId}\tName: {item.FormatName}\n\t > Error: {message}");
                        continue;
                    }

                    // Try to set the clipboard data using the handle
                    try
                    {
                        if (item.PendingCustomAddition == true)
                        {
                            if (hGlobal != IntPtr.Zero)
                            {
                                IntPtr result = SetCustomFormatToClipboard(item.FormatId, item.FormatName, hGlobal);
                                if (result == IntPtr.Zero)
                                {
                                    erroredFormats.Add($"ID: {item.FormatId}\tName: {item.FormatName}");
                                }
                            }
                            else if (item.RawData == null || !item.RawData.Any()) // If the original data was null or empty then assume it's intentional so add it
                            {
                                IntPtr result = SetCustomFormatToClipboard(item.FormatId, item.FormatName, IntPtr.Zero);
                            }
                            else
                            {
                                Console.WriteLine($"Failed to allocate memory for custom format: {item.FormatId}");
                                erroredFormats.Add($"ID: {item.FormatId}    Name: {item.FormatName}");
                            }
                        }
                        else if (hGlobal != IntPtr.Zero)
                        {
                            if (NativeMethods.SetClipboardData(item.FormatId, hGlobal) == IntPtr.Zero)
                            {
                                NativeMethods.GlobalFree(hGlobal);
                                Console.WriteLine($"Failed to set clipboard data for format: {item.FormatId}");
                                erroredFormats.Add($"ID: {item.FormatId}\tName: {item.FormatName}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Failed to allocate memory for format: {item.FormatId}");
                            erroredFormats.Add($"ID: {item.FormatId}\tName: {item.FormatName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = ex.Message.Replace("\n", "  ").Replace("\r", "");
                        Console.WriteLine($"Error setting clipboard data for format {item.FormatId}: {ex.Message}");
                        erroredFormats.Add($"ID: {item.FormatId}\tName: {item.FormatName}\n\t{message}");
                        continue;
                    }
                    
                } // End foreach loop

                if (erroredFormats.Count > 0)
                {
                    string erroredFormatsString = string.Join("\n", erroredFormats);
                    MessageBox.Show($"Errors occurred while saving clipboard data for one or more formats:\n\n{erroredFormatsString}");
                }
                else
                {
                    MessageBox.Show("Clipboard data saved.");
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

        private IntPtr SetCustomFormatToClipboard(uint formatId, string formatName, IntPtr handle)
        {
            bool registerCustomName;
            IntPtr result = IntPtr.Zero;
            uint formatIDToUse;

            if (formatId == 0)
            {
                registerCustomName = true;
            }
            else
            {
                registerCustomName = false;
            }

            if (registerCustomName)
            {
                // Register the custom format
                formatIDToUse = NativeMethods.RegisterClipboardFormat(formatName);
                result = NativeMethods.SetClipboardData(formatIDToUse, handle);
            }
            else
            {
                // Register the custom format
                formatIDToUse = formatId;
                result = NativeMethods.SetClipboardData(formatIDToUse, handle);
            }
            

            if (result == IntPtr.Zero)
            {
                Console.WriteLine($"Failed to set clipboard data for custom format: {formatName} ({formatIDToUse})");
                NativeMethods.GlobalFree(handle);
            }

            return result;
        }

        private void UpdateSplitterPosition_FitDataGrid(bool force = false)
        {
            DataGridView dgv = dataGridViewClipboard;

            // Set the splitter distance to fit the datagridview total height of visible cells
            // Row height doesn't include cell borders so add 1 pixel each to the height
            int newSize = dgv.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + dgv.ColumnHeadersHeight + dgv.Rows.GetRowCount(DataGridViewElementStates.Visible);

            int idealMaxSize = (int)Math.Round((decimal)splitContainerMain.Height * (decimal)0.6);
            int trueMaxSize = splitContainerMain.Height - CompensateDPI(75);
            newSize += CompensateDPI(2); // Add a little extra space
            // Don't exceed 60% of the splitter panel
            if (newSize > idealMaxSize)
            {
                if (force == false)
                {
                    newSize = idealMaxSize;
                }
                else if (newSize > trueMaxSize)
                {
                    newSize = trueMaxSize;
                }
            }

            splitContainerMain.SplitterDistance = newSize;
        }

        private void UpdateEditControlsVisibility_AndPendingGridAppearance(ClipboardItem? selectedItem = null, ClipboardItem? selectedEditedItem = null)
        {
            //if (selectedItem == null)
            //{
            //    selectedItem = GetSelectedClipboardItemObject(returnEditedItemVersion: false);
            //}
            if (selectedEditedItem == null)
            {
                selectedEditedItem = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
            }

            // Visibility updates to make regardless of view mode and selected item
            if (anyPendingChanges)
            {
                labelPendingChanges.Visible = true;
            }
            else
            {
                labelPendingChanges.Visible = false;
                // Reset all default row colors to black
                foreach (DataGridViewRow row in dataGridViewClipboard.Rows)
                {
                    row.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                    row.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
                    SetDataGridRowFontStrikeout(dataGridViewClipboard, row, false);
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


            // --------------- CONDITIONAL CELL COLORING AND STYLING ---------------

            // Color based on fixed values. Only for non-custom formats, because that will be handled later using the editedClipboardItems list
            foreach (DataGridViewRow row in dataGridViewClipboard.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    string columnName = dataGridViewClipboard.Columns[cell.ColumnIndex].Name;
                    object cellValue = cell.Value;
                    string cellValueString = cellValue?.ToString() ?? string.Empty;

                    // Reset the cell style to inherit from the row
                    cell.Style = new DataGridViewCellStyle();

                    if (columnName == colName.DataInfo) // Data Info column
                    {
                        // Set text color to gray for empty data info
                        if (string.IsNullOrEmpty(cellValueString) ||
                            cellValueString == MyStrings.DataNotApplicable || // If the data info is "N/A"
                            cellValueString.ToLower() == MyStrings.DataNull) // If the data info is "[null]"
                        {
                            cell.Style.ForeColor = Color.Gray;
                        }
                        // Set text color to dark red for errors
                        else if (cellValueString.Contains("Error"))
                        {
                            cell.Style.ForeColor = Color.DarkRed;
                        }
                    }

                    if (columnName == colName.Index) // Index column
                    {
                        cell.Style.ForeColor = Color.Gray;
                    }
                }
            }

            // For any items in editedClipboardItems that has pending changes, set the default row color
            foreach (var editedItem in editedClipboardItems)
            {
                int rowIndex;
                // Update the row text appearance depending on pending operations
                if (editedItem.PendingRemoval)
                {
                    rowIndex = dataGridViewClipboard.Rows.Cast<DataGridViewRow>().ToList().FindIndex(r => r.Cells[colName.UniqueID].Value.ToString() == editedItem.UniqueID.ToString());
                    if (rowIndex >= 0)
                    {
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Red;
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.SelectionForeColor = Color.Yellow;
                        SetDataGridRowFontStrikeout(dataGridViewClipboard, dataGridViewClipboard.Rows[rowIndex], true);
                    }
                }
                else if (editedItem.PendingCustomAddition)
                {
                    rowIndex = dataGridViewClipboard.Rows.Cast<DataGridViewRow>().ToList().FindIndex(r => r.Cells[colName.UniqueID].Value.ToString() == editedItem.UniqueID.ToString());

                    if (rowIndex >= 0)
                    {
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Green;
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.SelectionForeColor = Color.Yellow;

                        // If the format id column is 0, the cell to gray
                        if (editedItem.FormatId.ToString() == MyStrings.DefaultCustomFormatID)
                        {
                            dataGridViewClipboard.Rows[rowIndex].Cells[colName.FormatId].Style.ForeColor = Color.Gray;
                        }
                        // Using an else-if because if the custom format ID is default, the format name will be applied even if it is also default
                        else if (editedItem.FormatName == MyStrings.DefaultCustomFormatName) 
                        {
                            dataGridViewClipboard.Rows[rowIndex].Cells[colName.FormatName].Style.ForeColor = Color.Gray;
                        }
                    }
                }
                else if (editedItem.HasPendingEdit)
                {
                    rowIndex = dataGridViewClipboard.Rows.Cast<DataGridViewRow>().ToList().FindIndex(r => r.Cells[colName.UniqueID].Value.ToString() == editedItem.UniqueID.ToString());
                    if (rowIndex >= 0)
                    {
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Red;
                        dataGridViewClipboard.Rows[rowIndex].DefaultCellStyle.SelectionForeColor = Color.Yellow;
                    }
                }
            }

            // ---------------------------------------------------------------------------

            // Beyond here, we need a selected item. If there isn't one, set some buttons that require a selectedItem to be disabled
            if (selectedEditedItem == null)
            {
                buttonResetEdit.Enabled = false;
                buttonApplyEdit.Enabled = false;
                menuEdit_CopyEditedHexAsText.Enabled = false;
                return;
            }

            // Updates based on selected selectedItem only, regardless of view mode
            if (selectedEditedItem.HasPendingEdit || selectedEditedItem.PendingRemoval)
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

            // If the plaintext editing checkbox is unchecked, disable editing of the text
            if (!checkBoxPlainTextEditing.Checked)
            {
                richTextBox_HexPlaintext.ReadOnly = true;
                // Change the color to gray to indicate it's not editable
                richTextBox_HexPlaintext.BackColor = SystemColors.ControlLight;
            }
            else
            {
                richTextBox_HexPlaintext.ReadOnly = false;
                richTextBox_HexPlaintext.BackColor = SystemColors.Window;
            }

        }

        // Enables or Disables font strikeout for text of a given row, while preserving the otherwise existing font style
        private void SetDataGridRowFontStrikeout(DataGridView dgv, DataGridViewRow row, bool strikeout)
        {
            if (dgv == null || row == null) return;

            // Start with the DataGridView's DefaultCellStyle font
            Font baseFont = dgv.DefaultCellStyle.Font;

            // If RowsDefaultCellStyle has a font, use that instead
            if (dgv.RowsDefaultCellStyle.Font != null)
                baseFont = dgv.RowsDefaultCellStyle.Font;

            // If the specific row has a font, use that
            if (row.DefaultCellStyle.Font != null)
                baseFont = row.DefaultCellStyle.Font;

            // If we somehow still don't have a font, exit
            if (baseFont == null) return;

            // Determine the new font style
            FontStyle newStyle = baseFont.Style & ~FontStyle.Strikeout;
            if (strikeout)
                newStyle |= FontStyle.Strikeout;

            // Create the new font
            Font newFont = new Font(baseFont, newStyle);

            // Create a new style based on the existing row style
            DataGridViewCellStyle newCellStyle = row.DefaultCellStyle.Clone();
            newCellStyle.Font = newFont;

            // Apply the new style to the row
            row.DefaultCellStyle = newCellStyle;

            // Optionally, apply to individual cells if needed
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.Style = newCellStyle;
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

        private ClipboardItem? GetSelectedClipboardItemObject(bool returnEditedItemVersion)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.SelectedRows[0];
                if (Guid.TryParse(selectedRow.Cells[colName.UniqueID].Value.ToString(), out Guid uniqueID))
                {
                    if (returnEditedItemVersion)
                    {
                        return editedClipboardItems.Find(i => i.UniqueID == uniqueID);
                    }
                    else
                    {
                        //MessageBox.Show("There is no original data for this item.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return clipboardItems.Find(i => i.UniqueID == uniqueID);
                    }
                }
            }
            return null;
        }

        // Updates selected clipboard selectedItem in editedClipboardItems list. Does not update the actual clipboard.
        private void UpdateEditedClipboardItem(Guid uniqueID, byte[]? rawData, bool setPendingEdit = true, bool setPendingRemoval = false)
        {
            foreach (ClipboardItem item in editedClipboardItems)
            {
                if (item.UniqueID == uniqueID)
                {
                    item.RawData = rawData;
                    item.DataSize = (ulong)(rawData?.Length ?? 0);
                    item.HasPendingEdit = setPendingEdit;
                    item.PendingRemoval = setPendingRemoval; // If the user applies an edit, presumably they don't want to remove it anymore
                    return;
                }
            }
        }

        private void MarkIndividualClipboardItemForRemoval(Guid uniqueID)
        {
            foreach (ClipboardItem item in editedClipboardItems)
            {
                if (item.UniqueID == uniqueID)
                {
                    item.PendingRemoval = true;
                    anyPendingChanges = true;
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
                List<string> headerRow = new List<string>();
                foreach (DataGridViewColumn col in dataGridViewClipboard.Columns)
                {
                    // Ignore the dummy column
                    if (col.Name != "Index")
                    {
                        headerRow.Add(col.HeaderText);
                    }
                }
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
                List<string> rowCells = new List<string>();
                foreach (DataGridViewCell cell in row.Cells)
                {
                    // Ignore the dummy column
                    if (cell.OwningColumn.Name != "Index")
                    {
                        rowCells.Add(cell.Value.ToString());
                    }
                }

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
            MenuItem? optionsMenuItem = null;
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
                MenuItem? tableCopyingModeMenuItem = null;
                foreach (MenuItem subMenuItem in optionsMenuItem.MenuItems)
                {
                    if (subMenuItem?.Text == "Table Copying Mode")
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

            string rawSelectedTextSection = text.Substring(start, length);
            string cleanedText = rawSelectedTextSection.Replace(" ", "");

            // Choose bytesize baseed on dropdown encoding
            int byteSize;
            switch(dropdownHexToTextEncoding.SelectedIndex)
            {
                case 0: // UTF-8
                    byteSize = 2;
                    break;
                case 1: // UTF-16 LE
                case 2: // UTF-16 BE
                    byteSize = 4;
                    break;
                case 3: // UTF-32 LE
                case 4: // UTF-32 BE
                    byteSize = 8;
                    break;
                default:
                    byteSize = 2;
                    break;
            }


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
                    else if (byteSize == 8)
                    {
                        if (uint.TryParse(byteStr, System.Globalization.NumberStyles.HexNumber, null, out uint ui))
                        {
                            ProcessUtf32(ui);
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

            void ProcessUtf32(uint ui)
            {
                if (editMode)
                {
                    switch (ui)
                    {
                        case 0x00000000: // \0
                        case 0x00000007: // \a
                        case 0x00000008: // \b
                        case 0x0000000C: // \f
                        case 0x0000000A: // \n
                        case 0x0000000D: // \r
                        case 0x00000009: // \t
                        case 0x0000000B: // \v
                            plaintextLength += 8; // These are represented as four characters in edit mode
                            break;
                        default:
                            plaintextLength += 4; // Normal character
                            break;

                    }
                }
                    
            }

            // All lengths are multiplied by two so that we can divide by two now and account for UTF-16
            // Otherwise we would have had to use 0.5 for the UTF-16 case
            // Actually that was for a previous version, might not be necessary at the moment
            return plaintextLength / 2; 
        }

        private void SaveBinaryFile()
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem? itemToExport = GetSelectedClipboardItemObject(returnEditedItemVersion: false);

            if (itemToExport == null || itemToExport.RawData == null || itemToExport.RawData.Length == 0)
            {
                MessageBox.Show("Format item contains no data to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string nameStem = itemToExport.FormatName;

            // If it's a DIBV5 format, convert it to a bitmap
            if (itemToExport.FormatId == 17)
            {
                Bitmap bitmap = FormatConverters.BitmapFile_From_CF_DIBV5_RawData(itemToExport.RawData);

                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);

                }
                return;
            }
            else if (itemToExport.FormatId == 8) // CF_DIB
            {
                Bitmap bitmap = FormatConverters.BitmapFile_From_CF_DIB_RawData(itemToExport.RawData);
                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);
                }
                return;
            }
            else if (itemToExport.FormatId == 2) // CF_BITMAP
            {
                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    using (MemoryStream ms = new MemoryStream(itemToExport.RawData))
                    {
                        using (Bitmap bitmap = new Bitmap(ms))
                        {
                            bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);

                        }
                    }
                }
                return;
            }
            else if (itemToExport.FormatName == "HTML Format")
            {
                string inputString = Encoding.UTF8.GetString(itemToExport.RawData);
                string? outputString = FormatConverters.ConvertHtmlFormat(inputString);

                if (outputString == null)
                {
                    return; // Error message box will be shown in the function itself with error
                }

                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "html", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialogResult.FileName, outputString);
                }
                return;
            }

            // --------- For generic binary data ------------

            string fileExt = "dat"; // Default extension if not in the list of known formats

            // Check the dictionary for known binary file associations for which to save directly as files with given extensions
            if (FormatInfoHardcoded.KnownBinaryExtensionAssociations.TryGetValue(itemToExport.FormatName.ToLower(), out string ext))
            {
                fileExt = ext;
            }

            // Sanitize the name stem
            nameStem = nameStem.Replace(" ", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_")
                .Replace("*", "_")
                .Replace("?", "_")
                .Replace("\"", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("|", "_");

            SaveFileDialog saveRawFileDialogResult = SaveFileDialog(extension: fileExt, defaultFileNameStem: nameStem);
            if (saveRawFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(saveRawFileDialogResult.FileName, itemToExport.RawData);
            }
        }

        public bool ValidateCustomFormats()
        {
            bool okToProceed = false ;
            bool allValidRanges = true;
            bool customFormatWithinStandardRange = false;
            bool customFormatFallsOnReservedRange = false; // If it is a multiple of 0x100 (256), Windows might not register it

            // Check that all custom format IDs are within the valid range for registered formats         
            foreach (ClipboardItem item in editedClipboardItems)
            {
                bool validRange = false;
                if (item.FormatType == MyStrings.DefaultCustomFormatType) 
                {
                    if (item.FormatId == 0
                        || (item.FormatId >= MyVals.RegisteredFormatMinID && item.FormatId <= MyVals.RegisteredFormatMaxID)
                        )
                    {
                        validRange = true;
                    }
                    // Check if it's at least within the standard formats range even if these shouldn't be used
                    else if (item.FormatId > 0 && item.FormatId <= MyVals.RegisteredFormatMinID)
                    {
                        customFormatWithinStandardRange = true;
                    }

                    // Check if it's a multiple of 0x100 it might not work, so warn the user even if it's otherwise valid
                    if ((item.FormatId % 0x100 == 0) && item.FormatId != 0)
                    {
                        customFormatFallsOnReservedRange = true;
                    }
                }
                else 
                {
                    // Reaching here means it's not a new custom format
                    validRange = true;
                }
                
                if (!validRange) // Ensures one-way marking as invalid
                {
                    allValidRanges = false;
                }
            }

            // Check if any custom formats match a non-custom format's name
            bool noConflictingCustomNames = true;
            bool noConflictingCustomIDs = true;
            List<ClipboardItem> customItems = editedClipboardItems.Where(i => i.FormatType == MyStrings.DefaultCustomFormatType).ToList();
            List<ClipboardItem> regularItems = editedClipboardItems.Where(i => i.FormatType != MyStrings.DefaultCustomFormatType).ToList();

            foreach (ClipboardItem customItem in customItems)
            {
                foreach (ClipboardItem regularItem in regularItems)
                {
                    if (regularItem.FormatName == customItem.FormatName)
                    {
                        // Allow certain exceptions
                        if (!regularItem.PendingRemoval && !customItem.PendingRemoval)
                        {
                            noConflictingCustomNames = false;
                            break;
                        }
                    }

                    if (regularItem.FormatId == customItem.FormatId)
                    {
                        // Allow certain exceptions
                        if (!regularItem.PendingRemoval || !customItem.PendingRemoval)
                        {
                            noConflictingCustomIDs = false;
                            break;
                        }
                    }
                }
                if (!noConflictingCustomNames || !noConflictingCustomIDs)
                {
                    break;
                }
            }

            bool customMutualsAreUnique = true;
            bool specialCaseBothFormatIDsZero = false;
            // Compare custom format IDs to other custom format IDs
            foreach (ClipboardItem customItem in customItems)
            {
                foreach (ClipboardItem otherCustomItem in customItems)
                {
                    // Actual start of checking
                    if (customItem.UniqueID != otherCustomItem.UniqueID) // Skip comparing the same item
                    {
                        if ((customItem.FormatName == otherCustomItem.FormatName) && (customItem.FormatId == otherCustomItem.FormatId)) 
                        {
                            // Allow certain exceptions
                            if (!customItem.PendingRemoval || !otherCustomItem.PendingRemoval) // If one is marked for deletion, it's fine
                            {
                                customMutualsAreUnique = false;
                                if (customItem.FormatId == 0 && otherCustomItem.FormatId == 0)
                                {
                                    specialCaseBothFormatIDsZero = true;
                                }
                                break;
                            }
                        }
                    }
                }
                if (!customMutualsAreUnique)
                {
                    break;
                }
            }

            if (customFormatFallsOnReservedRange)
            {
                MessageBox.Show("Warning: Format IDs that are multiples of 256 (Aka 0x100) might not be registered by Windows.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (allValidRanges && noConflictingCustomIDs && noConflictingCustomNames && customMutualsAreUnique)
            {
                okToProceed = true;
            }
            else if (!allValidRanges)
            {
                int minID = (int)MyVals.RegisteredFormatMinID;
                int maxID = (int)MyVals.RegisteredFormatMaxID;
                if (customFormatWithinStandardRange)
                {
                    // Prompt the user if they want to proceed
                    DialogResult result = MessageBox.Show(
                        $"Warning: Custom format IDs technically should be between {minID} and {maxID} (the valid registered format range), to assign a name.\n\n" +
                        $"You can still add it to the clipboard below {minID} but that isn't normal and it won't have a name.\n\n" +
                        "Do you want to proceed anyway?",
                        "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    okToProceed = (result == DialogResult.Yes);
                }
                else
                {
                    MessageBox.Show($"Custom format IDs SHOULD be between {minID} and {maxID} (the valid registered format range), and MUST be between 1 and {maxID}.", "Invalid Custom Format IDs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!noConflictingCustomNames)
            {
                MessageBox.Show("Custom format names must not match any existing format names (unless one or the other is marked for deletion).", "Conflicting Format Names", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!noConflictingCustomIDs)
            {
                MessageBox.Show("Custom format IDs must not match any existing format IDs (unless one or the other is marked for deletion).", "Conflicting Format IDs", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!customMutualsAreUnique && specialCaseBothFormatIDsZero)
            {
                MessageBox.Show("Custom format Name/ID pairs must be unique even if they both use a zero placeholder for the format ID number. Because when registering format IDs, names are used.", "Duplicate Custom Formats", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!customMutualsAreUnique)
            {
                MessageBox.Show("Custom format Name/ID pairs must be unique.", "Duplicate Custom Formats", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("An unknown error occurred while validating custom formats.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return okToProceed;
        }

        
    } // ---------------------------------------------------------------------------------------------------
    // --------------------------------------- End of MainForm Class ---------------------------------------
    // -----------------------------------------------------------------------------------------------------

    public enum WhichPanelResize
    {
        None,
        Bottom,
        Top
    }

    // ------ Will probably look for a more direct way for this later but this will do for now ------
    public static class colName
    {
        public const string UniqueID = "UniqueID";
        public const string Index = "Index";
        public const string FormatName = "FormatName";
        public const string FormatId = "FormatId";
        public const string HandleType = "HandleType";
        public const string DataSize = "DataSize";
        public const string DataInfo = "DataInfo";
        public const string TextPreview = "TextPreview";
    }

    public static class MyStrings
    {
        public const string DefaultCustomFormatName = "Custom Format";
        public const string CustomPendingData = "[Pending: Added By You]";
        public const string DefaultCustomFormatID = "0";
        public const string DefaultCustomFormatType = "Custom";
        public const string DataNotApplicable = "N/A";
        public const string DataNull = "[null]";
    }

    public static class MyVals
    {
        public const uint RegisteredFormatMinID = 0xC000;
        public const uint RegisteredFormatMaxID = 0xFFFF;
    }

    // ---------------------------------------------------------------------------------------------------

    // ----------------------------------- Object Definitions---------------------------------------------------

    public class DiagnosticsInfo
    {
        // Clipboard owner
        public string Owner { get; set; } = "";
        public string OwnerProcessName { get; set; } = "";
        public uint OwnerProcessID { get; set; }
        public string OwnerProcessPath { get; set; } = "";
        public uint ClipboardSequenceNumber { get; set; }
        public IntPtr OwnerHandle { get; set; }
        public IntPtr OwnerWindowHandle { get; set; }
        public bool IsFormatAvailable { get; set; }
        public string WindowText { get; set; } = "";
        public string WindowClass { get; set; } = "";
        public string ReportString { get; set; } = "";

    }

    public class ClipDataObject
    {
        private IClipboardFormat? _objectData = null;
        public IClipboardFormat? ObjectData
        {
            get => _objectData;
            set
            {
                _objectData = value;
                _structName = ObjectData?.StructName();
            }
        }

        // Struct name will be gotten automatically via class method if possible and it wasn't set manually
        private string? _structName = null;
        public string? StructName
        {
            get => _structName;
            set
            {
                _structName = value;
                if (_structName == null && ObjectData != null)
                {
                    _structName = ObjectData.StructName();
                }
            }
        }

        public IEnumerable<string>? PropertyNames
        {
            get
            {
                if (ObjectData == null)
                {
                    return null;
                }

                // If it's a collection and therefore no actual property names
                if (ObjectData is IEnumerable enumerable)
                {
                    return []; // Same as:  Enumerable.Empty<string>()
                }
                else
                {
                    return ObjectData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);
                }
            }
        }

        public object? GetPropertyValue(string propertyName)
        {
            if (ObjectData == null)
                return null;

            // Check if it's an enum first, because it will otherwise get treated as a nested object
            // If enumerable, return the object itself, not a string, so we can further process it
            if (ObjectData.GetType().GetProperty(propertyName)?.PropertyType.IsEnum == true)
            {
                return ObjectData.GetType().GetProperty(propertyName).GetValue(ObjectData);
            }

            // Check for display replacements. If there is one, use that instead of the actual value
            if (ObjectData.DataDisplayReplacements()?.TryGetValue(propertyName, out string replacementValue) == true)
            {
                return replacementValue;
            }

            PropertyInfo propInfo = ObjectData.GetType().GetProperty(propertyName);
            try
            {
                return propInfo?.GetValue(ObjectData);
            }
            catch (TargetParameterCountException)
            {
                return null;
            }
        }

        private void BuildString(StringBuilder sb, string indent)
        {
            if (PropertyNames == null)
            {
                return;
            }
            foreach (var propertyName in PropertyNames)
            {
                var propertyValue = GetPropertyValue(propertyName);
                if (propertyValue is ClipDataObject nestedObject)
                {
                    sb.AppendLine($"{indent}{propertyName}:");
                    nestedObject.BuildString(sb, indent + "    ");
                }
                else if (propertyValue is IList<ClipDataObject> nestedList)
                {
                    sb.AppendLine($"{indent}{propertyName}:");
                    foreach (var item in nestedList)
                    {
                        sb.AppendLine($"{indent}    - ");
                        item.BuildString(sb, indent + "        ");
                    }
                }
                else
                {
                    sb.AppendLine($"{indent}{propertyName}: {propertyValue}");
                }
            }
        }


    } // ------ End of ClipDataObject class definition

    public class ClipboardItem : ICloneable
    {
        // Using a unique ID for each object instance just in case it's needed for tracking
        // Like an edited item where the ID or name might change
        public Guid UniqueID { get; init; } = Guid.NewGuid();

        public string FormatName { get; set; } = "";
        public uint FormatId { get; set; }
        public IntPtr Handle { get; set; }
        public ulong DataSize { get; set; }
        public byte[]? ProcessedData { get; set; }
        public byte[]? RawData { get; set; }
        public bool AssumedSynthesized { get; set; }
        public List<string> DataInfoList { get; set; } = new List<string>();
        public string DataInfoLinesString => string.Join("\n", DataInfoList ?? new List<string>());
        public bool HasPendingEdit { get; set; } = false;
        public bool PendingRemoval { get; set; } = false;
        public bool PendingCustomAddition { get; set; } = false;
        public string FormatType { get; set; } = "Unknown";
        public string? ErrorReason { get; set; } = null;
        public DiagnosticsInfo? ErrorDiagnosisReport { get; set; }
        public int OriginalIndex { get; set; } = -1;
        public ClipDataObject? ClipDataObject { get; set; } = null ;
        

        public object Clone()
        {
            var clone = new ClipboardItem
            {
                FormatName = this.FormatName,
                FormatId = this.FormatId,
                Handle = this.Handle,
                DataSize = this.DataSize,
                ProcessedData = (byte[]?)this.ProcessedData?.Clone(),
                RawData = (byte[]?)this.RawData?.Clone(),
                AssumedSynthesized = this.AssumedSynthesized,
                DataInfoList = new List<string>(this.DataInfoList ?? new List<string>()),
                HasPendingEdit = false,
                PendingRemoval = this.PendingRemoval,
                PendingCustomAddition = this.PendingCustomAddition,
                FormatType = this.FormatType,
                ErrorReason = this.ErrorReason,
                ErrorDiagnosisReport = this.ErrorDiagnosisReport,
                OriginalIndex = this.OriginalIndex,
                ClipDataObject = this.ClipDataObject != null
                ? new ClipDataObject // If it 's not null, clone it
                {
                    StructName = this.ClipDataObject.StructName,
                    ObjectData = this.ClipDataObject.ObjectData
                }
                : null // If it's null, set to null
            };

            // Manually set the UniqueID to match the original
            typeof(ClipboardItem).GetProperty(nameof(UniqueID)).SetValue(clone, this.UniqueID);

            return clone;
        }

    } // ------  End of ClipboardItem class definition

}