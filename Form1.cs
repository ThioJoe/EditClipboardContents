using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;

namespace ClipboardManager
{
    public partial class Form1 : Form
    {
        

        private List<ClipboardItem> clipboardItems = new List<ClipboardItem>();

        private StreamWriter logFile;


        public Form1()
        {
            InitializeComponent();
            InitializeLogging();
            InitializeDataGridView();
            UpdateToolLocations();
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
            dataGridViewClipboard.Columns.Add("Data", "Data");

            // Set AutoSizeMode for each column individually
            
            dataGridViewClipboard.Columns["FormatId"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewClipboard.Columns["HandleType"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewClipboard.Columns["DataSize"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewClipboard.Columns["Data"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

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

        //Function to fit data grid view to the form window
        private void UpdateToolLocations()
        {
            int titlebarAccomodate = 40;
            int splitterBorderAccomodate = 5;
            int bottomBuffer = 35; // Adjust this value to set the desired buffer size
            
            int splitterPanelsBottomPosition = this.Height - toolStrip1.Height - titlebarAccomodate;

            // Resize splitContainer1 to fit the form
            splitContainer1.Width = this.Width - 32;
            splitContainer1.Height = splitterPanelsBottomPosition - bottomBuffer;

            // Resize data grid within panel to match panel size
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

            clipboardItems.Clear();
            dataGridViewClipboard.Rows.Clear();

            Console.WriteLine("Attempting to open clipboard");
            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                Console.WriteLine("Failed to open clipboard");
                MessageBox.Show("Failed to open clipboard.");
                return;
            }

            try
            {
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
                    string dataInfo = "Not available";

                    try
                    {
                        switch (format)
                        {
                            case 2: // CF_BITMAP
                                Console.WriteLine("Processing CF_BITMAP");
                                using (Bitmap bmp = Bitmap.FromHbitmap(hData))
                                {
                                    dataSize = (ulong)(bmp.Width * bmp.Height * (Image.GetPixelFormatSize(bmp.PixelFormat) / 8));
                                    dataInfo = $"Bitmap: {bmp.Width}x{bmp.Height}, {bmp.PixelFormat}";
                                }
                                break;

                            case 1:  // CF_TEXT
                            case 13: // CF_UNICODETEXT
                                Console.WriteLine($"Processing text format: {(format == 1 ? "CF_TEXT" : "CF_UNICODETEXT")}");
                                dataSize = (ulong)NativeMethods.GlobalSize(hData).ToUInt64();
                                IntPtr pData = NativeMethods.GlobalLock(hData);
                                if (pData != IntPtr.Zero)
                                {
                                    try
                                    {
                                        dataInfo = format == 1 ?
                                            Marshal.PtrToStringAnsi(pData) :
                                            Marshal.PtrToStringUni(pData);
                                        dataInfo = dataInfo.Length > 50 ? dataInfo.Substring(0, 50) + "..." : dataInfo;
                                    }
                                    finally
                                    {
                                        NativeMethods.GlobalUnlock(hData);
                                    }
                                }
                                break;

                            case 15: // CF_HDROP
                                Console.WriteLine("Processing CF_HDROP");
                                dataSize = (ulong)NativeMethods.GlobalSize(hData).ToUInt64();
                                uint fileCount = NativeMethods.DragQueryFile(hData, 0xFFFFFFFF, null, 0);
                                dataInfo = $"File Drop: {fileCount} file(s)";
                                break;

                            default:
                                Console.WriteLine($"Processing unknown format: {format}");
                                dataSize = (ulong)NativeMethods.GlobalSize(hData).ToUInt64();
                                dataInfo = $"Data size: {dataSize} bytes";
                                break;
                        }

                        Console.WriteLine($"Processed format: {format}, Size: {dataSize}, Info: {dataInfo}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing format {format}: {ex.Message}");
                    }

                    var item = new ClipboardItem
                    {
                        FormatName = formatName,
                        FormatId = format,
                        HandleType = "Handle",
                        DataSize = dataSize,
                        Data = null
                    };

                    clipboardItems.Add(item);
                    UpdateClipboardItems(formatName, format.ToString(), "Handle", dataSize.ToString(), dataInfo);
                }
            }
            finally
            {
                Console.WriteLine("Closing clipboard");
                NativeMethods.CloseClipboard();
            }

            Console.WriteLine("RefreshClipboardItems completed");
        }


        // Update data grid view with clipboard contents during refresh
        private void UpdateClipboardItems(string formatName, string formatID, string handleType, string dataSize, string dataPreview)
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
            // Resize data grid view to fit the form window
            UpdateToolLocations();
        }
    }

    internal class ClipboardItem
    {
        public string FormatName { get; set; }
        public uint FormatId { get; set; }
        public string HandleType { get; set; }
        public ulong DataSize { get; set; }  // Changed from uint to ulong
        public byte[] Data { get; set; }
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



        public const uint GMEM_MOVEABLE = 0x0002;
    }

}