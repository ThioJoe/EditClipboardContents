using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;

namespace ClipboardManager
{
    public partial class Form1 : Form
    {
        private int bottomBuffer = 50; // Adjust this value to set the desired buffer size

        private List<ClipboardItem> clipboardItems = new List<ClipboardItem>();

        public Form1()
        {
            InitializeComponent();
            InitializeDataGridView();
            UpdateToolLocations();
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
            int trueBottomPos = this.Height - 55;
            // Resize dataGridViewClipboard to fit the form
            dataGridViewClipboard.Width = this.Width - 40;
            dataGridViewClipboard.Height = trueBottomPos - bottomBuffer; //55 To account for window title bar

            // Keep buttons at the bottom of the form
            btnRefresh.Location = new System.Drawing.Point(18, trueBottomPos - 20);
            btnDelete.Location = new System.Drawing.Point(100, trueBottomPos - 20);
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
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridViewClipboard.SelectedRows[0];
                if (uint.TryParse(selectedRow.Cells["FormatId"].Value.ToString(), out uint formatIdToRemove))
                {
                    LogClipboardContents("Clipboard contents before removal:");
                    if (RemoveClipboardFormat(formatIdToRemove))
                    {
                        LogClipboardContents("Clipboard contents after removal:");
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

        private void RewriteClipboard()
        {
            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                MessageBox.Show("Failed to open clipboard.");
                return;
            }

            try
            {
                NativeMethods.EmptyClipboard();

                foreach (var item in clipboardItems)
                {
                    IntPtr hGlobal = IntPtr.Zero;
                    try
                    {
                        uint size = (uint)item.Data.Length;
                        hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)size);
                        if (hGlobal == IntPtr.Zero)
                        {
                            throw new OutOfMemoryException("GlobalAlloc failed");
                        }

                        IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                        if (pGlobal == IntPtr.Zero)
                        {
                            throw new InvalidOperationException("GlobalLock failed");
                        }

                        Marshal.Copy(item.Data, 0, pGlobal, (int)size);
                        NativeMethods.GlobalUnlock(hGlobal);

                        IntPtr setDataResult = NativeMethods.SetClipboardData(item.FormatId, hGlobal);
                        if (setDataResult == IntPtr.Zero)
                        {
                            throw new InvalidOperationException("SetClipboardData failed");
                        }

                        // Ownership of the memory has been transferred to the system
                        hGlobal = IntPtr.Zero;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error setting clipboard data: {ex.Message}");
                    }
                    finally
                    {
                        if (hGlobal != IntPtr.Zero)
                        {
                            NativeMethods.GlobalFree(hGlobal);
                        }
                    }
                }
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
        }

        private bool ForceRemoveClipboardFormat(uint formatToRemove)
        {
            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                MessageBox.Show("Failed to open clipboard.");
                return false;
            }

            try
            {
                List<uint> formatsToKeep = new List<uint>();
                Dictionary<uint, IntPtr> dataToKeep = new Dictionary<uint, IntPtr>();

                uint format = 0;
                while ((format = NativeMethods.EnumClipboardFormats(format)) != 0)
                {
                    if (format != formatToRemove)
                    {
                        formatsToKeep.Add(format);
                    }
                }

                foreach (uint fmt in formatsToKeep)
                {
                    IntPtr hData = NativeMethods.GetClipboardData(fmt);
                    if (hData != IntPtr.Zero)
                    {
                        UIntPtr size = NativeMethods.GlobalSize(hData);
                        IntPtr hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, size);
                        if (hGlobal != IntPtr.Zero)
                        {
                            IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                            IntPtr pData = NativeMethods.GlobalLock(hData);
                            if (pGlobal != IntPtr.Zero && pData != IntPtr.Zero)
                            {
                                NativeMethods.CopyMemory(pGlobal, pData, size);
                                NativeMethods.GlobalUnlock(hData);
                                NativeMethods.GlobalUnlock(hGlobal);
                                dataToKeep[fmt] = hGlobal;
                            }
                            else
                            {
                                NativeMethods.GlobalFree(hGlobal);
                            }
                        }
                    }
                }

                NativeMethods.EmptyClipboard();

                foreach (var kvp in dataToKeep)
                {
                    NativeMethods.SetClipboardData(kvp.Key, kvp.Value);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing clipboard format: {ex.Message}");
                return false;
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
        }

        private void LogClipboardContents(string message)
        {
            Console.WriteLine(message);
            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                Console.WriteLine("Failed to open clipboard.");
                return;
            }

            try
            {
                uint format = 0;
                while ((format = NativeMethods.EnumClipboardFormats(format)) != 0)
                {
                    Console.WriteLine(GetFormatInfo(format));
                }
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
            Console.WriteLine();
        }
        private void RefreshClipboardItems()
        {
            clipboardItems.Clear();
            dataGridViewClipboard.Rows.Clear();

            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                MessageBox.Show("Failed to open clipboard.");
                return;
            }

            try
            {
                uint format = 0;
                while ((format = NativeMethods.EnumClipboardFormats(format)) != 0)
                {
                    string formatName = GetClipboardFormatName(format);
                    IntPtr hData = NativeMethods.GetClipboardData(format);
                    UIntPtr dataSize = hData != IntPtr.Zero ? NativeMethods.GlobalSize(hData) : UIntPtr.Zero;
                    string handleType = hData != IntPtr.Zero ? "Memory" : "N/A";
                    byte[] data = RetrieveClipboardData(format, hData, dataSize);

                    var item = new ClipboardItem
                    {
                        FormatName = formatName,
                        FormatId = format,
                        HandleType = handleType,
                        DataSize = dataSize.ToUInt32(),
                        Data = data
                    };

                    clipboardItems.Add(item);

                    string dataPreview = GetDataPreview(item);
                    UpdateClipboardItems(formatName, format.ToString(), handleType, dataSize.ToString(), dataPreview);
                }
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
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
                default:
                    StringBuilder formatName = new StringBuilder(512);
                    NativeMethods.GetClipboardFormatName(format, formatName, formatName.Capacity);
                    return formatName.ToString();
            }
        }

        private byte[] RetrieveClipboardData(uint format, IntPtr hData, UIntPtr dataSize)
        {
            if (hData == IntPtr.Zero)
                return null;

            try
            {
                IntPtr ptr = NativeMethods.GlobalLock(hData);
                if (ptr != IntPtr.Zero)
                {
                    try
                    {
                        byte[] data = new byte[dataSize.ToUInt32()];
                        Marshal.Copy(ptr, data, 0, (int)dataSize.ToUInt32());
                        return data;
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(hData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving data: {ex.Message}");
            }

            return null;
        }

        private string GetDataPreview(ClipboardItem item)
        {
            if (item.Data == null)
                return "N/A";

            switch (item.FormatId)
            {
                case 1: // CF_TEXT
                case 7: // CF_OEMTEXT
                    return Encoding.Default.GetString(item.Data);
                case 13: // CF_UNICODETEXT
                    return Encoding.Unicode.GetString(item.Data);
                default:
                    return $"(Binary data: {item.DataSize} bytes)";
            }
        }

        private bool RemoveClipboardFormat(uint formatToRemove)
        {
            if (!NativeMethods.OpenClipboard(this.Handle))
            {
                MessageBox.Show("Failed to open clipboard.");
                return false;
            }

            try
            {
                List<ClipboardFormatData> formatsToKeep = new List<ClipboardFormatData>();

                uint format = 0;
                while ((format = NativeMethods.EnumClipboardFormats(format)) != 0)
                {
                    if (format != formatToRemove)
                    {
                        IntPtr hData = NativeMethods.GetClipboardData(format);
                        if (hData != IntPtr.Zero)
                        {
                            UIntPtr size = NativeMethods.GlobalSize(hData);
                            IntPtr hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, size);
                            if (hGlobal != IntPtr.Zero)
                            {
                                IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                                IntPtr pData = NativeMethods.GlobalLock(hData);
                                if (pGlobal != IntPtr.Zero && pData != IntPtr.Zero)
                                {
                                    NativeMethods.CopyMemory(pGlobal, pData, size);
                                    NativeMethods.GlobalUnlock(hData);
                                    NativeMethods.GlobalUnlock(hGlobal);
                                    formatsToKeep.Add(new ClipboardFormatData { Format = format, Data = hGlobal });
                                }
                                else
                                {
                                    NativeMethods.GlobalFree(hGlobal);
                                }
                            }
                        }
                    }
                }

                NativeMethods.EmptyClipboard();

                foreach (var item in formatsToKeep)
                {
                    NativeMethods.SetClipboardData(item.Format, item.Data);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing clipboard format: {ex.Message}");
                return false;
            }
            finally
            {
                NativeMethods.CloseClipboard();
            }
        }

        internal class ClipboardFormatData
        {
            public uint Format { get; set; }
            public IntPtr Data { get; set; }
        }
        private string GetFormatInfo(uint format)
        {
            StringBuilder formatName = new StringBuilder(512);
            NativeMethods.GetClipboardFormatName(format, formatName, formatName.Capacity);
            string name = formatName.ToString();

            if (string.IsNullOrEmpty(name))
            {
                name = GetStandardFormatName(format);
            }

            IntPtr hData = NativeMethods.GetClipboardData(format);
            string size = hData != IntPtr.Zero ? NativeMethods.GlobalSize(hData).ToString() : "N/A";

            return $"Format: {format}, Name: {name}, Size: {size}";
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
                default: return "Unknown";
            }
        }

        private void dataGridViewClipboard_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }

    internal class ClipboardItem
    {
        public string FormatName { get; set; }
        public uint FormatId { get; set; }
        public string HandleType { get; set; }
        public uint DataSize { get; set; }
        public byte[] Data { get; set; }
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


        public const uint GMEM_MOVEABLE = 0x0002;
    }

}