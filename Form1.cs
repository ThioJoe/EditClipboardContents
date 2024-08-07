using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Linq;

namespace ClipboardManager
{
    public partial class Form1 : Form
    {
        private List<ClipboardItem> clipboardItems = new List<ClipboardItem>();

        private StreamWriter logFile;

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


        public Form1()
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

            // Set AutoSizeMode for each column individually
            
            dataGridViewClipboard.Columns["FormatId"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewClipboard.Columns["HandleType"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewClipboard.Columns["DataSize"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewClipboard.Columns["DataInfo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dataGridViewClipboard.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewClipboard.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Reisize auto
            dataGridViewClipboard.Columns["FormatName"].Resizable = DataGridViewTriState.True;
            dataGridViewClipboard.Columns["FormatName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;


            // Hide the row headers (the leftmost column)
            dataGridViewClipboard.RowHeadersVisible = false;
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
            int bottomBuffer = 35; // Adjust this value to set the desired buffer size
            
            int splitterPanelsBottomPosition = this.Height - toolStrip1.Height - titlebarAccomodate;

            // Resize splitContainer1 to fit the form
            splitContainer1.Width = this.Width - 32;
            splitContainer1.Height = splitterPanelsBottomPosition - bottomBuffer;

            // Resize processedData grid within panel to match panel size
            dataGridViewClipboard.Width = splitContainer1.Panel1.Width - splitterBorderAccomodate;
            dataGridViewClipboard.Height = splitContainer1.Panel1.Height - splitterBorderAccomodate;
            richTextBoxContents.Width = splitContainer1.Panel2.Width - splitterBorderAccomodate;
            richTextBoxContents.Height = splitContainer1.Panel2.Height - splitterBorderAccomodate;

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

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshClipboardItems();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {

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
                DetermineSynthesizedFormats();
            }
            finally
            {
                Console.WriteLine("Closing clipboard");
                NativeMethods.CloseClipboard();
            }

            ProcessClipboardData();
            Console.WriteLine("RefreshClipboardItems completed");
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing format {format}: {ex.Message}");
                }

                var item = new ClipboardItem
                {
                    FormatName = formatName,
                    FormatId = format,
                    Handle = hData, // Set the handle
                    DataSize = dataSize,
                    RawData = rawData,
                    Data = rawData // Initially, set Data to rawData
                };

                clipboardItems.Add(item);
            }
        }


        private void ProcessClipboardData()
        {
            dataGridViewClipboard.Rows.Clear();

            foreach (var item in clipboardItems)
            {
                string dataInfo = "N/A";
                byte[] processedData = item.RawData;

                switch (item.FormatId)
                {
                    case 1: // CF_TEXT
                        Console.WriteLine("Processing CF_TEXT");
                        string asciiText = Encoding.ASCII.GetString(item.RawData);
                        int asciiTextLength = asciiText.Length;
                        dataInfo = $"Encoding: ASCII, Chars: {asciiTextLength}";
                        processedData = Encoding.ASCII.GetBytes(asciiText);
                        break;

                    case 13: // CF_UNICODETEXT
                        Console.WriteLine("Processing CF_UNICODETEXT");
                        string unicodeText = Encoding.Unicode.GetString(item.RawData);
                        int unicodeTextLength = unicodeText.Length;
                        dataInfo = $"Encoding: Unicode, Chars: {unicodeTextLength}";
                        processedData = Encoding.Unicode.GetBytes(unicodeText);
                        break;

                    case 2: // CF_BITMAP
                        Console.WriteLine("Processing CF_BITMAP");
                        dataInfo = ProcessBitmap(item.Handle, out processedData);
                        item.DataSize = (ulong)processedData.Length;
                        break;

                    case 8: // CF_DIB
                    case 17: // CF_DIBV5
                        Console.WriteLine($"Processing bitmap format: {(item.FormatId == 8 ? "CF_DIB" : "CF_DIBV5")}");
                        dataInfo = $"{item.FormatName}, Size: {item.DataSize} bytes";
                        break;

                    case 15: // CF_HDROP
                        Console.WriteLine("Processing CF_HDROP");
                        uint fileCount = NativeMethods.DragQueryFile(item.Handle, 0xFFFFFFFF, null, 0);
                        StringBuilder fileNames = new StringBuilder();
                        for (uint i = 0; i < fileCount; i++)
                        {
                            StringBuilder fileName = new StringBuilder(260);
                            NativeMethods.DragQueryFile(item.Handle, i, fileName, (uint)fileName.Capacity);
                            fileNames.AppendLine(fileName.ToString());
                        }
                        dataInfo = $"File Drop: {fileCount} file(s)";
                        break;

                    // Add more cases for other formats as needed...

                    default:
                        Console.WriteLine($"Processing unknown format: {item.FormatId}");
                        dataInfo = "N/A";
                        break;
                }

                item.Data = processedData; // Update the processed data in the item
                string handleType = item.AssumedSynthesized ? "Synthesized" : "Standard"; // Determine handle type

                UpdateClipboardItemsGridView(item.FormatName, item.FormatId.ToString(), handleType, item.DataSize.ToString(), dataInfo);
            }
        }

        private void DetermineSynthesizedFormats()
        {
            List<uint> formatOrder = clipboardItems.Select(item => item.FormatId).ToList();
            HashSet<uint> synthesizeTargets = new HashSet<uint>(SynthesizedFormatsMap.SelectMany(kvp => kvp.Value));

            for (int i = formatOrder.Count - 1; i >= 0; i--)
            {
                uint currentFormat = formatOrder[i];

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




        private bool IsSynthesizedFormat(uint previousFormat, uint currentFormat)
        {
            if (SynthesizedFormatsMap.ContainsKey(previousFormat))
            {
                return SynthesizedFormatsMap[previousFormat].Contains(currentFormat);
            }
            return false;
        }

        private string ProcessBitmap(IntPtr hBitmap, out byte[] bitmapData)
        {
            using (Bitmap bmp = Bitmap.FromHbitmap(hBitmap))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    bitmapData = ms.ToArray();
                    return $"Bitmap: {bmp.Width}x{bmp.Height}, {bmp.PixelFormat}";
                }
            }
        }



        // Update processedData grid view with clipboard contents during refresh
        private void UpdateClipboardItemsGridView(string formatName, string formatID, string handleType, string dataSize, string dataPreview)
        {
            dataGridViewClipboard.Rows.Add(formatName, formatID, handleType, dataSize, dataPreview);

            // Set column widths
            dataGridViewClipboard.Columns["FormatName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            int originalWidth = (int)dataGridViewClipboard.Columns["FormatName"].Width;
            dataGridViewClipboard.Columns["FormatName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dataGridViewClipboard.Columns["FormatName"].Width = originalWidth + 15;
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
                default: return $"Unknown Format ({format})";
            }
        }

        private void dataGridViewClipboard_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void dataGridViewClipboard_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.Rows[e.RowIndex];
                if (uint.TryParse(selectedRow.Cells["FormatId"].Value.ToString(), out uint formatId))
                {
                    ClipboardItem item = clipboardItems.Find(i => i.FormatId == formatId);
                    if (item != null)
                    {
                        richTextBoxContents.Clear();
                        DisplayClipboardData(item);
                    }
                }
            }
        }


        private void DisplayClipboardData(ClipboardItem item)
        {
            if (item == null || item.RawData == null)
            {
                richTextBoxContents.Text = "Data not available";
                return;
            }

            switch (dropdownContentsViewMode.SelectedIndex)
            {
                case 0: // Text view mode
                    richTextBoxContents.Text = Encoding.UTF8.GetString(item.RawData);
                    break;

                case 1: // Hex view mode
                    richTextBoxContents.Text = BitConverter.ToString(item.RawData).Replace("-", " ");
                    break;

                default:
                    richTextBoxContents.Text = "Unknown view mode";
                    break;
            }
        }


        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            RefreshClipboardItems();
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
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.SelectedRows[0];
                if (uint.TryParse(selectedRow.Cells["FormatId"].Value.ToString(), out uint formatId))
                {
                    ClipboardItem item = clipboardItems.Find(i => i.FormatId == formatId);
                    if (item != null)
                    {
                        DisplayClipboardData(item);
                    }
                }
            }
        }
    }

    internal class ClipboardItem
    {
        public string FormatName { get; set; }
        public uint FormatId { get; set; }
        public IntPtr Handle { get; set; }
        public ulong DataSize { get; set; } // Changed from uint to ulong
        public byte[] Data { get; set; }
        public byte[] RawData { get; set; }
        public bool AssumedSynthesized { get; set; } // New property to indicate if the format is assumed to be synthesized
    }






    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public IntPtr bmBits;
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

}