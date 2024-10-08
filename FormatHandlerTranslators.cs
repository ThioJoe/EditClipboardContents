using ClipboardManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// My classes
using static EditClipboardItems.ClipboardFormats;

namespace EditClipboardItems
{
    public static partial class FormatHandlerTranslators
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

        //------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
