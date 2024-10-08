using ClipboardManager;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// My classes
using static EditClipboardItems.ClipboardFormats;

namespace EditClipboardItems
{
    public static partial class FormatHandleTranslators
    {
        public static IntPtr AllocateGeneralHandle_FromRawData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Console.WriteLine("No data to allocate and copy");
                return IntPtr.Zero;
            }

            IntPtr hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)data.Length);
            if (hGlobal != IntPtr.Zero)
            {
                IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                if (pGlobal != IntPtr.Zero)
                {
                    try
                    {
                        Marshal.Copy(data, 0, pGlobal, data.Length);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(hGlobal);
                    }
                }
                else
                {
                    NativeMethods.GlobalFree(hGlobal);
                    hGlobal = IntPtr.Zero;
                    Console.WriteLine("Failed to lock memory");
                }
            }
            else
            {
                Console.WriteLine("Failed to allocate memory");
            }
            return hGlobal;
        }

        public static IntPtr Bitmap_hBitmapHandle_FromHandle(IntPtr hBitmap)
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

        public static IntPtr BitmapDIB_hGlobalHandle_FromHandle(IntPtr hDib)
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

        public static byte[] EnhMetafile_RawData_FromHandle(IntPtr hEnhMetaFile)
        {
            uint size = NativeMethods.GetEnhMetaFileBits(hEnhMetaFile, 0, null);
            if (size > 0)
            {
                byte[] data = new byte[size];
                if (NativeMethods.GetEnhMetaFileBits(hEnhMetaFile, size, data) == size)
                {
                    return data;
                }
            }
            return null;
        }

        public static byte[] MetafilePict_RawData_FromHandle(IntPtr hMetafilePict)
        {
            IntPtr pMetafilePict = NativeMethods.GlobalLock(hMetafilePict);
            if (pMetafilePict != IntPtr.Zero)
            {
                try
                {
                    METAFILEPICT mfp = (METAFILEPICT)Marshal.PtrToStructure(pMetafilePict, typeof(METAFILEPICT));
                    int metafileSize = NativeMethods.GetMetaFileBitsEx(mfp.hMF, 0, null);
                    if (metafileSize > 0)
                    {
                        byte[] metafileData = new byte[metafileSize];
                        if (NativeMethods.GetMetaFileBitsEx(mfp.hMF, metafileSize, metafileData) == metafileSize)
                        {
                            byte[] fullData = new byte[Marshal.SizeOf(typeof(METAFILEPICT)) + metafileSize];
                            Marshal.Copy(pMetafilePict, fullData, 0, Marshal.SizeOf(typeof(METAFILEPICT)));
                            Buffer.BlockCopy(metafileData, 0, fullData, Marshal.SizeOf(typeof(METAFILEPICT)), metafileSize);
                            return fullData;
                        }
                    }
                }
                finally
                {
                    NativeMethods.GlobalUnlock(hMetafilePict);
                }
            }
            return null;
        }

        public static IntPtr MetafilePict_Handle_FromRawData(byte[] rawData)
        {
            IntPtr hGlobal = AllocateGeneralHandle_FromRawData(rawData);
            if (hGlobal != IntPtr.Zero)
            {
                IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                if (pGlobal != IntPtr.Zero)
                {
                    try
                    {
                        METAFILEPICT mfp = (METAFILEPICT)Marshal.PtrToStructure(pGlobal, typeof(METAFILEPICT));
                        IntPtr hMetafileCopy = NativeMethods.CopyMetaFile(mfp.hMF, null);
                        if (hMetafileCopy != IntPtr.Zero)
                        {
                            mfp.hMF = hMetafileCopy;
                            Marshal.StructureToPtr(mfp, pGlobal, false);
                            return hGlobal;
                        }
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(hGlobal);
                    }
                }
                NativeMethods.GlobalFree(hGlobal);
            }
            return IntPtr.Zero;
        }

        public static IntPtr EnhMetafile_Handle_FromRawData(byte[] rawData)
        {
            using (MemoryStream ms = new MemoryStream(rawData))
            {
                IntPtr hemf = NativeMethods.SetEnhMetaFileBits((uint)rawData.Length, rawData);
                if (hemf != IntPtr.Zero)
                {
                    IntPtr hemfCopy = NativeMethods.CopyEnhMetaFile(hemf, null);
                    NativeMethods.DeleteEnhMetaFile(hemf);
                    return hemfCopy;
                }
            }
            return IntPtr.Zero;
        }

        public static Bitmap BitmapFile_From_CF_DIBV5_RawData(byte[] data)
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

        public static Bitmap BitmapFile_From_CF_DIB_RawData(byte[] data)
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

        //------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
