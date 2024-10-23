using EditClipboardContents;
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
using static EditClipboardContents.ClipboardFormats;
using System.Windows.Forms;
using System.Xml.Linq;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0066 // Disable message about switch case expression
// Nullable reference types
#nullable enable

namespace EditClipboardContents
{
    public static partial class FormatConverters
    {
        public static IntPtr AllocateGeneralHandle_FromRawData(byte[]? data)
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
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Error in AllocateGeneralHandle_FromRawData: {ex.Message}");
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
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error while trying to copy HBITMAP: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return IntPtr.Zero;
            }

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

        public static byte[]? EnhMetafile_RawData_FromHandle(IntPtr hEnhMetaFile)
        {
            if (hEnhMetaFile == IntPtr.Zero)
            {
                return null;
            }

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

        public static byte[]? MetafilePict_RawData_FromHandle(IntPtr hMetafilePict)
        {
            IntPtr pMetafilePict = NativeMethods.GlobalLock(hMetafilePict);
            if (pMetafilePict != IntPtr.Zero)
            {
                try
                {
                    METAFILEPICT mfp = (METAFILEPICT)Marshal.PtrToStructure(pMetafilePict, typeof(METAFILEPICT));
                    uint metafileSize = NativeMethods.GetMetaFileBitsEx(mfp.hMF, 0, null);
                    if (metafileSize > 0)
                    {
                        byte[] metafileData = new byte[metafileSize];
                        if (NativeMethods.GetMetaFileBitsEx(mfp.hMF, metafileSize, metafileData) == metafileSize)
                        {
                            int metaFilePictHeaderSize = Marshal.SizeOf(typeof(METAFILEPICT)) - IntPtr.Size; // Subtract IntPtr.Size we'll replace with the actual metafile data
                            byte[] fullData = new byte[metaFilePictHeaderSize + metafileSize];
                            Marshal.Copy(pMetafilePict, fullData, 0, metaFilePictHeaderSize);
                            Buffer.BlockCopy(metafileData, 0, fullData, metaFilePictHeaderSize, (int)metafileSize);
                            return fullData;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while trying to read METAFILEPICT from memory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    NativeMethods.GlobalUnlock(hMetafilePict);
                }
            }
            return null;
        }

        public static byte[]? CF_HDROP_RawData_FromHandle(IntPtr hDrop)
        {
            // Lock the global memory object to access the data
            IntPtr pDropFiles = NativeMethods.GlobalLock(hDrop);
            if (pDropFiles == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                // Get the size of the global memory object
                int globalSize = (int)NativeMethods.GlobalSize(hDrop);

                // Marshal the DROPFILES_OBJ structure from the memory
                DROPFILES dropFiles = Marshal.PtrToStructure<DROPFILES>(pDropFiles);

                // Ensure dropFiles.pFiles is within a reasonable range. pFiles defines the offset to the file names, so it should be less than the size of the global memory object.
                if (dropFiles.pFiles <= 0 || dropFiles.pFiles > globalSize)
                {
                    Console.WriteLine($"Invalid pFiles value: {dropFiles.pFiles}");
                    return null;
                }

                // Ensure the memory we're trying to access is within the bounds of the allocated global memory
                int dropFilesStructSize = Marshal.SizeOf<DROPFILES>();
                if (dropFilesStructSize + dropFiles.pFiles > globalSize)
                {
                    Console.WriteLine("Attempting to access memory beyond the allocated global memory");
                    return null;
                }

                byte[] managedArray = new byte[dropFiles.pFiles];

                // Now we can safely copy the memory
                Marshal.Copy(pDropFiles, managedArray, 0, (int)dropFiles.pFiles); // Need to cast Dword to int32 because DWORD is uint32 and Marshal.Copy only takes int32 as the length parameter

                // Determine if the file names are Unicode
                bool isUnicode = dropFiles.fWide != 0;

                // Get the number of files
                uint fileCount = NativeMethods.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);

                // Prepare to calculate the total size
                List<byte> rawDataList = new List<byte>();

                // Get the size of the DROPFILES_OBJ structure
                int dropFilesSize = Marshal.SizeOf(typeof(DROPFILES));

                // Copy the DROPFILES_OBJ structure to rawDataList
                byte[] dropFilesBytes = new byte[dropFilesSize];
                Marshal.Copy(pDropFiles, dropFilesBytes, 0, dropFilesSize);
                rawDataList.AddRange(dropFilesBytes);

                // For each file, get its path and add to rawDataList
                for (uint i = 0; i < fileCount; i++)
                {
                    uint pathLength = NativeMethods.DragQueryFile(hDrop, i, null, 0) + 1; // +1 for null terminator

                    if (isUnicode)
                    {
                        // Unicode
                        StringBuilder path = new StringBuilder((int)pathLength);
                        NativeMethods.DragQueryFile(hDrop, i, path, pathLength);

                        byte[] pathBytes = Encoding.Unicode.GetBytes(path.ToString());
                        rawDataList.AddRange(pathBytes);
                        // Add null terminator (2 bytes for Unicode)
                        rawDataList.AddRange(new byte[] { 0, 0 });
                    }
                    else
                    {
                        // ANSI
                        StringBuilder path = new StringBuilder((int)pathLength);
                        NativeMethods.DragQueryFileA(hDrop, i, path, pathLength);

                        byte[] pathBytes = Encoding.Default.GetBytes(path.ToString());
                        rawDataList.AddRange(pathBytes);
                        // Add null terminator
                        rawDataList.Add(0);
                    }
                }

                // Add final null terminator
                if (isUnicode)
                {
                    rawDataList.AddRange(new byte[] { 0, 0 });
                }
                else
                {
                    rawDataList.Add(0);
                }

                // Convert the rawDataList to a byte array
                return rawDataList.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CF_HDROP_RawData_FromHandle: {ex.Message}");
                MessageBox.Show($"Error in CF_HDROP_RawData_FromHandle: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            finally
            {
                // Always unlock the global memory object
                NativeMethods.GlobalUnlock(hDrop);
            }
        }



        public static IntPtr CF_HDROP_Handle_FromRawData(byte[]? rawData)
        {
            if (rawData == null || rawData.Length < Marshal.SizeOf(typeof(DROPFILES)))
            {
                return IntPtr.Zero;
            }

            // Lock rawData for pinning in memory
            GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                // Get pointer to the DROPFILES_OBJ structure in rawData
                IntPtr pRawData = handle.AddrOfPinnedObject();

                // Marshal the DROPFILES_OBJ structure from rawData
                DROPFILES dropFiles = Marshal.PtrToStructure<DROPFILES>(pRawData);

                // The file names start after the DROPFILES_OBJ structure
                int fileListOffset = (int)dropFiles.pFiles;

                // Calculate the length of the file names (rest of the rawData)
                int fileNamesLength = rawData.Length - fileListOffset;

                // Allocate global memory for the new clipboard data
                IntPtr hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, (UIntPtr)rawData.Length);
                if (hGlobal == IntPtr.Zero)
                    return IntPtr.Zero;

                IntPtr pGlobal = NativeMethods.GlobalLock(hGlobal);
                if (pGlobal == IntPtr.Zero)
                {
                    NativeMethods.GlobalFree(hGlobal);
                    return IntPtr.Zero;
                }

                try
                {
                    // Copy the DROPFILES_OBJ structure to the global memory
                    Marshal.StructureToPtr(dropFiles, pGlobal, false);

                    // Copy the file names to the global memory
                    Marshal.Copy(rawData, fileListOffset, IntPtr.Add(pGlobal, fileListOffset), fileNamesLength);
                }
                finally
                {
                    NativeMethods.GlobalUnlock(hGlobal);
                }

                return hGlobal;
            }
            finally
            {
                handle.Free();
            }
        }

        public const uint GMEM_MOVEABLE = 0x0002; // Delete this later

        public static IntPtr MetafilePict_Handle_FromRawData(byte[]? rawData)
        {
            if (rawData == null || rawData.Length <= Marshal.SizeOf<METAFILEPICT>())
                return IntPtr.Zero;

            IntPtr hGlobalMetafilePict = IntPtr.Zero;
            IntPtr hMetafile = IntPtr.Zero;
            IntPtr pMetafile = IntPtr.Zero;
            IntPtr pMetafilePict = IntPtr.Zero;
            IntPtr hActualMetafile = IntPtr.Zero;

            try
            {
                // First allocate and create the metafile
                int headerSize = Marshal.SizeOf<METAFILEPICT>() - IntPtr.Size;
                int metafileDataSize = rawData.Length - headerSize;
                byte[] metaFileData = new byte[metafileDataSize];

                // Copy the metafile data portion
                Array.Copy(rawData, headerSize, metaFileData, 0, metafileDataSize);

                // Allocate memory for the metafile bits
                hMetafile = NativeMethods.GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)metafileDataSize);
                if (hMetafile == IntPtr.Zero)
                    throw new OutOfMemoryException("Failed to allocate memory for metafile.");

                // Lock and copy the metafile bits
                pMetafile = NativeMethods.GlobalLock(hMetafile);
                if (pMetafile == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to lock metafile memory.");

                Marshal.Copy(metaFileData, 0, pMetafile, metafileDataSize);

                // Create the actual metafile while memory is still locked
                hActualMetafile = NativeMethods.SetMetaFileBitsEx((uint)metafileDataSize, pMetafile);
                if (hActualMetafile == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    string errorMessage = Utils.GetWin32ErrorMessage(error);
                    throw new InvalidOperationException($"Failed to create metafile from bits. Error {error} - {errorMessage}");
                }

                // Now create the METAFILEPICT structure
                hGlobalMetafilePict = NativeMethods.GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)Marshal.SizeOf<METAFILEPICT>());
                if (hGlobalMetafilePict == IntPtr.Zero)
                    throw new OutOfMemoryException("Failed to allocate memory for METAFILEPICT.");

                pMetafilePict = NativeMethods.GlobalLock(hGlobalMetafilePict);
                if (pMetafilePict == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to lock METAFILEPICT memory.");

                // Create and copy the METAFILEPICT structure
                METAFILEPICT mfp = new METAFILEPICT
                {
                    mm = BitConverter.ToInt32(rawData, Marshal.OffsetOf<METAFILEPICT>(nameof(METAFILEPICT.mm)).ToInt32()),
                    xExt = BitConverter.ToInt32(rawData, Marshal.OffsetOf<METAFILEPICT>(nameof(METAFILEPICT.xExt)).ToInt32()),
                    yExt = BitConverter.ToInt32(rawData, Marshal.OffsetOf<METAFILEPICT>(nameof(METAFILEPICT.yExt)).ToInt32()),
                    hMF = hActualMetafile
                };

                Marshal.StructureToPtr(mfp, pMetafilePict, false);

                // Transfer ownership of hActualMetafile to the METAFILEPICT structure
                hActualMetafile = IntPtr.Zero;

                return hGlobalMetafilePict;
            }
            catch (Exception ex)
            {
                // Clean up on failure
                if (hActualMetafile != IntPtr.Zero)
                    NativeMethods.DeleteMetaFile(hActualMetafile);
                if (hGlobalMetafilePict != IntPtr.Zero)
                    NativeMethods.GlobalFree(hGlobalMetafilePict);
                Console.WriteLine($"Error in MetafilePict_Handle_FromRawData: {ex.Message}");
                throw;
            }
            finally
            {
                // Clean up temporary resources
                if (pMetafilePict != IntPtr.Zero)
                    NativeMethods.GlobalUnlock(hGlobalMetafilePict);

                if (pMetafile != IntPtr.Zero)
                    NativeMethods.GlobalUnlock(hMetafile);

                if (hMetafile != IntPtr.Zero)
                    NativeMethods.GlobalFree(hMetafile);
            }
        }

        public static IntPtr EnhMetafile_Handle_FromRawData(byte[]? rawData)
        {
            if (rawData == null || rawData.Length == 0)
            {
                return IntPtr.Zero;
            }
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
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                BITMAPV5HEADER bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(BITMAPV5HEADER));
                int width = Math.Abs(bmi.bV5Width);  // Ensure positive width
                int height = Math.Abs(bmi.bV5Height); // Ensure positive height
                int headerSize = (int)Math.Min(bmi.bV5Size, (uint)Marshal.SizeOf(typeof(BITMAPV5HEADER)));
                PixelFormat pixelFormat;
                int paletteSize = 0;

                switch (bmi.bV5BitCount)
                {
                    case 1:
                        pixelFormat = PixelFormat.Format1bppIndexed;
                        paletteSize = 2 * Marshal.SizeOf(typeof(RGBQUAD));
                        break;
                    case 8:
                        pixelFormat = PixelFormat.Format8bppIndexed;
                        paletteSize = 256 * Marshal.SizeOf(typeof(RGBQUAD));
                        break;
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
                IntPtr scan0 = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + bmi.bV5Size + paletteSize);

                if (!isTopDown)
                {
                    scan0 = new IntPtr(scan0.ToInt64() + (height - 1) * stride);
                    stride = -stride;
                }

                Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);

                if (pixelFormat == PixelFormat.Format8bppIndexed)
                {
                    ColorPalette palette = bitmap.Palette;
                    IntPtr palettePtr = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + bmi.bV5Size);

                    for (int i = 0; i < 256; i++)
                    {
                        RGBQUAD colorQuad = (RGBQUAD)Marshal.PtrToStructure(new IntPtr(palettePtr.ToInt64() + i * Marshal.SizeOf(typeof(RGBQUAD))), typeof(RGBQUAD));
                        palette.Entries[i] = Color.FromArgb(colorQuad.rgbRed, colorQuad.rgbGreen, colorQuad.rgbBlue);
                    }

                    bitmap.Palette = palette;
                }

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
                int paletteSize = 0;

                switch (bmi.bmiHeader.biBitCount)
                {
                    case 8:
                        pixelFormat = PixelFormat.Format8bppIndexed;
                        paletteSize = 256 * Marshal.SizeOf(typeof(RGBQUAD));
                        break;
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
                IntPtr scan0 = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + Marshal.SizeOf(typeof(BITMAPINFOHEADER)) + paletteSize);

                if (bmi.bmiHeader.biHeight > 0) // Top-up DIB
                {
                    scan0 = new IntPtr(scan0.ToInt64() + (height - 1) * stride);
                    stride = -stride;
                }

                Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);

                if (pixelFormat == PixelFormat.Format8bppIndexed)
                {
                    ColorPalette palette = bitmap.Palette;
                    IntPtr palettePtr = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + Marshal.SizeOf(typeof(BITMAPINFOHEADER)));

                    for (int i = 0; i < 256; i++)
                    {
                        RGBQUAD colorQuad = (RGBQUAD)Marshal.PtrToStructure(new IntPtr(palettePtr.ToInt64() + i * Marshal.SizeOf(typeof(RGBQUAD))), typeof(RGBQUAD));
                        palette.Entries[i] = Color.FromArgb(colorQuad.rgbRed, colorQuad.rgbGreen, colorQuad.rgbBlue);
                    }

                    bitmap.Palette = palette;
                }

                // Create a new bitmap to return, because the original one is tied to the pinned memory
                Bitmap result = new Bitmap(bitmap);
                return result;
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[]? DIBits_From_HBitmap(IntPtr hBitmap)
        {
            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));

            IntPtr hDC = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
            IntPtr hOldBitmap = NativeMethods.SelectObject(hDC, hBitmap);

            try
            {
                // Get the bitmap information
                NativeMethods.GetDIBits(hDC, hBitmap, 0, 0, null, ref bmi, (uint)ColorUsage.DIB_RGB_COLORS);

                // Allocate the buffer for the bits
                int imageSize = (int)bmi.bmiHeader.biSizeImage;
                byte[] bits = new byte[imageSize];

                // Get the actual bitmap data
                if (NativeMethods.GetDIBits(hDC, hBitmap, 0, (uint)bmi.bmiHeader.biHeight, bits, ref bmi, (uint)ColorUsage.DIB_RGB_COLORS) == 0)
                {
                    throw new Exception("Failed to get the bitmap bits.");
                }

                return bits;
            }
            finally
            {
                NativeMethods.SelectObject(hDC, hOldBitmap);
                NativeMethods.DeleteDC(hDC);
            }
        }

        
        public static IntPtr CF_BITMAP_Handle_FromRawData(byte[] rawData)
        {
            if (rawData == null)
            {
                return IntPtr.Zero;
            }

            // Get the offset of bmBits in the BITMAP structure
            int bmBitsOffset = Marshal.OffsetOf<BITMAP>("bmBits").ToInt32();

            if (rawData.Length <= bmBitsOffset)
            {
                return IntPtr.Zero;
            }

            // Extract the BITMAP_HEADER from rawData
            byte[] bitmapHeaderData = new byte[bmBitsOffset];
            Array.Copy(rawData, 0, bitmapHeaderData, 0, bmBitsOffset);

            _BitmapHeader bitmapHeader;
            GCHandle headerHandle = GCHandle.Alloc(bitmapHeaderData, GCHandleType.Pinned);
            try
            {
                bitmapHeader = Marshal.PtrToStructure<_BitmapHeader>(headerHandle.AddrOfPinnedObject());
            }
            finally
            {
                headerHandle.Free();
            }

            // Extract the bitmap bits from rawData
            int bitsSize = rawData.Length - bmBitsOffset;
            byte[] bitmapBits = new byte[bitsSize];
            Array.Copy(rawData, bmBitsOffset, bitmapBits, 0, bitsSize);

            // Calculate the stride (number of bytes per scanline)
            int bytesPerPixel = (bitmapHeader.bmBitsPixel / 8);
            int stride = bitmapHeader.bmWidth * bytesPerPixel;

            // Adjust stride for padding to a multiple of 4 bytes
            int padding = (4 - (stride % 4)) % 4;
            int scanlineSize = stride + padding;

            // Create a new array to hold the flipped bitmap bits. The bits are stored bottom-up, so we need to flip them or else it will be upside down.
            byte[] flippedBitmapBits = new byte[bitmapBits.Length];

            for (int y = 0; y < bitmapHeader.bmHeight; y++)
            {
                int sourceIndex = y * scanlineSize;
                int destIndex = (bitmapHeader.bmHeight - 1 - y) * scanlineSize;

                Array.Copy(bitmapBits, sourceIndex, flippedBitmapBits, destIndex, scanlineSize);
            }

            // Pin the flipped bitmap bits in memory
            GCHandle bitsHandle = GCHandle.Alloc(flippedBitmapBits, GCHandleType.Pinned);

            // Create the HBITMAP using the BITMAP_HEADER and flipped bitmap bits
            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                hBitmap = NativeMethods.CreateBitmap(
                    bitmapHeader.bmWidth,
                    bitmapHeader.bmHeight,
                    bitmapHeader.bmPlanes,
                    bitmapHeader.bmBitsPixel,
                    bitsHandle.AddrOfPinnedObject()
                );
            }
            finally
            {
                bitsHandle.Free();
            }

            return hBitmap;
        }

        public static byte[]? BITMAP_RawData_FromHandle(IntPtr hBitmap)
        {
            if (hBitmap == IntPtr.Zero)
            {
                return null;
            }

            int bitmapSizeResult = NativeMethods.GetObject(hBitmap, 0, IntPtr.Zero); // Get the size of the BITMAP object
            if (bitmapSizeResult == 0)
            {
                return null;
            }

            IntPtr pBitmap = Marshal.AllocHGlobal(bitmapSizeResult); // Allocate memory for GetObject to put BITMAP struct into
            try
            {
                int result = NativeMethods.GetObject(hBitmap, bitmapSizeResult, pBitmap);
                if (result == 0)
                {
                    return null;
                }

                byte[] rawData = new byte[bitmapSizeResult];
                Marshal.Copy(pBitmap, rawData, 0, bitmapSizeResult);

                // Get the the pointer from the bits at the end of the BITMAP struct
                if (Marshal.SizeOf(typeof(BITMAP)) <= bitmapSizeResult)
                {
                    BITMAP bitmap = Marshal.PtrToStructure<BITMAP>(pBitmap);
                    // If the pointer is included in the struct, use that to get the actual bitmap data
                    if (bitmap.bmBits != IntPtr.Zero)
                    {
                        int bitsSize = (int)bitmap.bmWidthBytes * bitmap.bmHeight;
                        byte[] bits = new byte[bitsSize];
                        Marshal.Copy(bitmap.bmBits, bits, 0, bitsSize);
                    }
                    // Otherwise separately call GetDIBits to get the bitmap data and append to rawdata if received
                    else
                    {
                        byte[]? rawImageBitsOnly = FormatConverters.DIBits_From_HBitmap(hBitmap);
                        if (rawImageBitsOnly != null)
                        {
                            // Get the index of bmbits in the BITMAP struct
                            int bmBitsIndex = Marshal.OffsetOf<BITMAP>("bmBits").ToInt32();
                            // Copy the BITMAP struct to a new array
                            byte[] newRawData = new byte[bmBitsIndex + rawImageBitsOnly.Length];
                            Array.Copy(rawData, newRawData, bmBitsIndex);
                            Array.Copy(rawImageBitsOnly, 0, newRawData, bmBitsIndex, rawImageBitsOnly.Length);
                            rawData = newRawData;

                            //byte[] newRawData = new byte[rawData.Length + rawImageBitsOnly.Length];
                            //Array.Copy(rawData, newRawData, rawData.Length);
                            //Array.Copy(rawImageBitsOnly, 0, newRawData, rawData.Length, rawImageBitsOnly.Length);
                            //rawData = newRawData;
                        }
                    }
                }

                return rawData;
            }
            finally
            {
                Marshal.FreeHGlobal(pBitmap);
            }

        }
        public static byte[]? CF_PALETTE_RawData_FromHandle(IntPtr hPalette)
        {
            if (hPalette == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                IntPtr paletteEntryCountHandle = Marshal.AllocHGlobal(2);
                try
                {
                    int result = NativeMethods.GetObject(hPalette, 2, paletteEntryCountHandle);
                    if (result != 2)
                    {
                        return null;
                    }

                    ushort entryCount = (ushort)Marshal.ReadInt16(paletteEntryCountHandle);
                    int logPaletteHeaderSize = Marshal.SizeOf(typeof(_LogPaletteHeader)); // Size of palVersion and palNumEntries, doesn't change

                    int logPaletteSize = logPaletteHeaderSize + (Marshal.SizeOf<PALETTEENTRY>() * entryCount); // Subtract 1 because the standard LOGPALETTE struct already has one PALETTEENTRY
                    IntPtr pLogPalette = Marshal.AllocHGlobal(logPaletteSize);
                    try
                    {
                        Marshal.WriteInt16(pLogPalette, 0, 0x300);  // palVersion
                        Marshal.WriteInt16(pLogPalette, 2, (short)entryCount);  // palNumEntries

                        // For the last argument of GetPaletteEntries, point it to the start of the PALETTEENTRY array, not the entire handle
                        uint entriesRetrieved = NativeMethods.GetPaletteEntries(hPalette, 0, (uint)entryCount, pLogPalette + 4); 
                        if (entriesRetrieved != entryCount)
                        {
                            return null;
                        }

                        byte[] rawData = new byte[logPaletteSize];
                        Marshal.Copy(pLogPalette, rawData, 0, logPaletteSize);

                        return rawData;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pLogPalette);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error in CF_PALETTE_RawData_FromHandle: {ex.Message}");
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(paletteEntryCountHandle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CF_PALETTE_RawData_FromHandle: {ex.Message}");
                return null;
            }
        }

        public static IntPtr CF_PALETTE_Handle_FromRawData(byte[]? rawData)
        {
            if (rawData == null || rawData.Length < 4) // Ensure it's at least the size of the LOGPALETTE header info before the PALETTEENTRY array
            {
                return IntPtr.Zero;
            }

            int headerSize = Marshal.SizeOf<_LogPaletteHeader>();

            // Determine the number of entries in the palette
            ushort numEntries = BitConverter.ToUInt16(rawData, 2);
            int logPaletteSize = headerSize + (Marshal.SizeOf<PALETTEENTRY>() * numEntries);
            LOGPALETTE logPalette = new LOGPALETTE((ushort)numEntries);
            int rawByteIndex = 4; // Skip the first 4 bytes, which are the palVersion and palNumEntries
            for (int i = 0; i < numEntries; i++)
            {
                byte b1 = rawData[rawByteIndex];
                byte b2 = rawData[rawByteIndex + 1];
                byte b3 = rawData[rawByteIndex + 2];
                byte b4 = rawData[rawByteIndex + 3];
                rawByteIndex += 4;

                logPalette.palPalEntry[i] = new PALETTEENTRY
                {
                    peRed = b1,
                    peGreen = b2,
                    peBlue = b3,
                    peFlags = b4
                };
            }

            // Marshal the LOGPALETTE struct to a handle
            int entrySize = Marshal.SizeOf<PALETTEENTRY>();
            int totalSize = headerSize + (entrySize * logPalette.palNumEntries);

            IntPtr logPalettePtr = Marshal.AllocHGlobal(totalSize);

            try
            {
                // Write palVersion and palNumEntries
                Marshal.WriteInt16(logPalettePtr, 0, (short)logPalette.palVersion);
                Marshal.WriteInt16(logPalettePtr, 2, (short)logPalette.palNumEntries);

                // Write palette entries
                for (int i = 0; i < logPalette.palNumEntries; i++)
                {
                    IntPtr entryPtr = logPalettePtr + headerSize + (i * entrySize);
                    Marshal.StructureToPtr(logPalette.palPalEntry[i], entryPtr, false);
                }
            }
            catch
            {
                Marshal.FreeHGlobal(logPalettePtr);
                return IntPtr.Zero;
            }

            if (logPalettePtr == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            // Now we have a handle to LOGPALETTE we can use to create the HPALLETE handle
            try
            {
                IntPtr hPalette = NativeMethods.CreatePalette(logPalettePtr);
                if (hPalette != IntPtr.Zero)
                {
                    return hPalette;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(logPalettePtr);
            }

            // If we reach here, something went wrong
            return IntPtr.Zero;
        }

        public static string? ConvertHtmlFormat(string htmlFormatText)
        {
            try
            {
                // Ensure the input is UTF-8 encoded
                byte[] bytes = Encoding.UTF8.GetBytes(htmlFormatText);
                string utf8String = Encoding.UTF8.GetString(bytes);

                // Parse header information
                var headerLines = utf8String.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int startHtml = -1, endHtml = -1, startFragment = -1, endFragment = -1;

                foreach (var line in headerLines)
                {
                    if (line.StartsWith("StartHTML:")) int.TryParse(line.Substring(10), out startHtml);
                    else if (line.StartsWith("EndHTML:")) int.TryParse(line.Substring(8), out endHtml);
                    else if (line.StartsWith("StartFragment:")) int.TryParse(line.Substring(15), out startFragment);
                    else if (line.StartsWith("EndFragment:")) int.TryParse(line.Substring(13), out endFragment);

                    if (startHtml != -1 && endHtml != -1 && startFragment != -1 && endFragment != -1)
                        break;
                }

                if (startHtml == -1 || endHtml == -1 || startFragment == -1 || endFragment == -1)
                {
                    throw new ArgumentException("Invalid HTML Format: Missing required header information.");
                }

                // Adjust end indices if they exceed the string length
                endHtml = Math.Min(endHtml, utf8String.Length);
                endFragment = Math.Min(endFragment, utf8String.Length);

                // Validate indices
                if (startHtml < 0 || startHtml >= utf8String.Length ||
                    endHtml <= startHtml || endHtml > utf8String.Length ||
                    startFragment < startHtml || startFragment >= utf8String.Length ||
                    endFragment <= startFragment || endFragment > utf8String.Length)
                {
                    throw new ArgumentException($"Invalid HTML Format: Index out of range after adjustment. StartHTML: {startHtml}, EndHTML: {endHtml}, StartFragment: {startFragment}, EndFragment: {endFragment}, StringLength: {utf8String.Length}");
                }

                // Extract the HTML content
                string htmlContent = utf8String.Substring(startHtml, endHtml - startHtml);

                // Find fragment markers
                int fragmentStartIndex = htmlContent.IndexOf("<!--StartFragment-->");
                int fragmentEndIndex = htmlContent.IndexOf("<!--EndFragment-->");

                if (fragmentStartIndex == -1 || fragmentEndIndex == -1)
                {
                    // If markers are not found, use the entire HTML content
                    fragmentStartIndex = 0;
                    fragmentEndIndex = htmlContent.Length;
                }
                else
                {
                    fragmentStartIndex += "<!--StartFragment-->".Length;
                }

                // Extract the fragment
                string fragment = htmlContent.Substring(fragmentStartIndex, fragmentEndIndex - fragmentStartIndex);

                // Clean up the fragment
                fragment = fragment.Trim();

                // Construct the final HTML
                var htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<!DOCTYPE html>");
                htmlBuilder.AppendLine("<html>");
                htmlBuilder.AppendLine("<head>");
                htmlBuilder.AppendLine("    <meta charset=\"utf-8\">");
                htmlBuilder.AppendLine("    <title>Converted HTML</title>");
                htmlBuilder.AppendLine("</head>");
                htmlBuilder.AppendLine("<body>");
                htmlBuilder.AppendLine(fragment);
                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");

                return htmlBuilder.ToString();
            }
            catch (Exception ex)
            {
                //throw new Exception($"Error converting HTML Format: {ex.Message}\nInput string length: {htmlFormatText?.Length}\nFirst 100 chars: {htmlFormatText?.Substring(0, Math.Min(100, htmlFormatText?.Length ?? 0))}", ex);
                MessageBox.Show($"Error converting HTML Format: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
