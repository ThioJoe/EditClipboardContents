using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression


// Notes:
// This file contains struct definitions for various clipboard formats. The definitions are based on the official Microsoft documentation.
// But it also contains classes that mirror the structs, which may contain lists in place of arrays and other differences to make them easier to parse
// The actual structs are used with Marshal to read the data from the clipboard. The classes are used to store the data in a more readable format as an object
// Structs are really only used for certain standard clipboard formats, since those formats often are just pointers to the struct, and Marsshal requires a struct to copy the data out
//   Then the class version can be used to process those too. Some don't require the struct to get the data out, so the class is used directly

namespace EditClipboardItems
{
    // Win32 API Types defined explicitly to avoid confusion and ensure compatibility with Win32 API, and it matches with documentation
    // See: https://learn.microsoft.com/en-us/windows/win32/winprog/windows-data-types
    using BOOL = System.Int32;          // 4 Bytes
    using LONG = System.Int32;          // 4 Bytes
    using DWORD = System.UInt32;        // 4 Bytes, aka uint, uint32
    using WORD = System.UInt16;         // 2 Bytes
    using BYTE = System.Byte;           // 1 Byte
    using FXPT2DOT30 = System.Int32;    // 4 Bytes
    using LPVOID = System.IntPtr;       // Handle to any type
    using HMETAFILE = System.IntPtr;    // Handle to metafile
    using CHAR = System.Byte;           // 1 Byte
    using USHORT = System.UInt16;       // 2 Bytes
    using static System.Net.WebRequestMethods;


    public static class ClipboardFormats
    {
        public interface IClipboardFormat
        {
            (string, string) GetDocumentationUrl();
            string StructName();
            string[] ItemsNotToPrint();
            void SetCacheStructObjectDisplayInfo(string structInfo);
            string GetCacheStructObjectDisplayInfo();
            IEnumerable<(string Name, object Value, Type Type, int? ArraySize)> EnumeratePropertiesWithType();
        }

        public abstract class ClipboardFormatBase : IClipboardFormat
        {
            // Private field to store the struct name
            private readonly string _structName;

            // Private field to store the cached struct display info
            private string _cachedStructDisplayInfo;

            // Default implementation for StructName
            public virtual string StructName() => _structName;

            // Common methods apply to all classes of the type
            public virtual (string, string) GetDocumentationUrl()
            {
                return (_structName, StructDocsLinks[_structName]);
            }

            public abstract string[] ItemsNotToPrint(); // Things that are too big or not useful to print, like binary data

            // Method to cache the display info of the struct object
            public void SetCacheStructObjectDisplayInfo(string structInfo)
            {
                _cachedStructDisplayInfo = structInfo;
            }

            // Method to retrieve the cached display info of the struct object
            public string GetCacheStructObjectDisplayInfo()
            {
                return _cachedStructDisplayInfo ?? string.Empty;
            }

            // Gets the type, and if it's a collection (like an array), the size of the collection as well
            public virtual IEnumerable<(string Name, object Value, Type Type, int? ArraySize)> EnumeratePropertiesWithType()
            {
                var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    var value = property.GetValue(this);
                    var type = property.PropertyType;
                    int? arraySize = null;

                    if (value is Array array)
                    {
                        arraySize = array.Length;
                    }

                    yield return (property.Name, value, type, arraySize);
                }
            }
        }

        // Static helper methods
        public static (string, string) GetDocumentationUrl<T>() where T : IClipboardFormat, new()
        {
            return new T().GetDocumentationUrl();
        }

        public static string StructName<T>() where T : IClipboardFormat, new()
        {
            return new T().StructName();
        }

        public static string[] GetVariableSizedItems<T>() where T : IClipboardFormat, new()
        {
            return new T().ItemsNotToPrint();
        }

        public class BITMAP_OBJ : ClipboardFormatBase
        {
            public LONG bmType { get; set; }
            public LONG bmWidth { get; set; }
            public LONG bmHeight { get; set; }
            public LONG bmWidthBytes { get; set; }
            public WORD bmPlanes { get; set; }
            public WORD bmBitsPixel { get; set; }
            public LPVOID bmBits { get; set; }

            private string _structName = "BITMAP";

            public override string[] ItemsNotToPrint()
            {
                return new string[] { "bmBits" };
            }
        }

        public class BITMAPV5HEADER_OBJ : ClipboardFormatBase
        {
            public DWORD bV5Size { get; set; }
            public LONG bV5Width { get; set; }
            public LONG bV5Height { get; set; }
            public WORD bV5Planes { get; set; }
            public WORD bV5BitCount { get; set; }
            public bV5Compression bV5Compression { get; set; }
            public DWORD bV5SizeImage { get; set; }
            public LONG bV5XPelsPerMeter { get; set; }
            public LONG bV5YPelsPerMeter { get; set; }
            public DWORD bV5ClrUsed { get; set; }
            public DWORD bV5ClrImportant { get; set; }
            public DWORD bV5RedMask { get; set; }
            public DWORD bV5GreenMask { get; set; }
            public DWORD bV5BlueMask { get; set; }
            public DWORD bV5AlphaMask { get; set; }
            public LOGCOLORSPACEA_OBJ bV5CSType { get; set; }
            public CIEXYZTRIPLE_OBJ bV5Endpoints { get; set; }
            public DWORD bV5GammaRed { get; set; }
            public DWORD bV5GammaGreen { get; set; }
            public DWORD bV5GammaBlue { get; set; }
            public DWORD bV5Intent { get; set; }
            public DWORD bV5ProfileData { get; set; }
            public DWORD bV5ProfileSize { get; set; }
            public DWORD bV5Reserved { get; set; }

            private readonly string _structName = "BITMAPV5HEADER";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public enum bV5Compression : uint // DWORD
        {
            BI_RGB = 0x0000,
            BI_RLE8 = 0x0001,
            BI_RLE4 = 0x0002,
            BI_BITFIELDS = 0x0003,
            BI_JPEG = 0x0004,
            BI_PNG = 0x0005,
            BI_CMYK = 0x000B,
            BI_CMYKRLE8 = 0x000C,
            BI_CMYKRLE4 = 0x000D
        }

        public class BITMAPINFOHEADER_OBJ : ClipboardFormatBase
        {
            public DWORD biSize { get; set; }
            public LONG biWidth { get; set; }
            public LONG biHeight { get; set; }
            public WORD biPlanes { get; set; }
            public WORD biBitCount { get; set; }
            public DWORD biCompression { get; set; }
            public DWORD biSizeImage { get; set; }
            public LONG biXPelsPerMeter { get; set; }
            public LONG biYPelsPerMeter { get; set; }
            public DWORD biClrUsed { get; set; }
            public DWORD biClrImportant { get; set; }

            private readonly string _structName = "BITMAPINFOHEADER";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class RGBQUAD_OBJ : ClipboardFormatBase
        {
            public BYTE rgbBlue { get; set; }
            public BYTE rgbGreen { get; set; }
            public BYTE rgbRed { get; set; }
            public BYTE rgbReserved { get; set; }

            private readonly string _structName = "RGBQUAD";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class BITMAPINFO_OBJ : ClipboardFormatBase
        {
            public BITMAPINFOHEADER_OBJ bmiHeader { get; set; }
            public List<RGBQUAD_OBJ> bmiColors { get; set; }

            private readonly string _structName = "BITMAPINFO";

            public override string[] ItemsNotToPrint()
            {
                return new string[] { "bmiColors" };
            }
        }

        public class METAFILEPICT_OBJ : ClipboardFormatBase
        {
            public LONG mm { get; set; }
            public LONG xExt { get; set; }
            public LONG yExt { get; set; }
            public HMETAFILE hMF { get; set; }

            private readonly string _structName = "METAFILEPICT";

            public override string[] ItemsNotToPrint()
            {
                return new string[] { "hMF" };
            }
        }

        public class CIEXYZ_OBJ : ClipboardFormatBase
        {
            public FXPT2DOT30 ciexyzX { get; set; }
            public FXPT2DOT30 ciexyzY { get; set; }
            public FXPT2DOT30 ciexyzZ { get; set; }
            private readonly string _structName = "CIEXYZ";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class CIEXYZTRIPLE_OBJ : ClipboardFormatBase
        {
            public CIEXYZ_OBJ ciexyzRed { get; set; }
            public CIEXYZ_OBJ ciexyzGreen { get; set; }
            public CIEXYZ_OBJ ciexyzBlue { get; set; }

            private readonly string _structName = "CIEXYZTRIPLE";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class DROPFILES_OBJ : ClipboardFormatBase
        {
            public DWORD pFiles { get; set; }
            public POINT_OBJ pt { get; set; }
            public BOOL fNC { get; set; }
            public BOOL fWide { get; set; }

            // Method for total size
            public int GetSize()
            {
                return Marshal.SizeOf(this);
            }

            private readonly string _structName = "DROPFILES";

            public override string[] ItemsNotToPrint()
            {
                return new string[] { "pt" };
            }
        }

        public class POINT_OBJ : ClipboardFormatBase
        {
            public LONG x { get; set; }
            public LONG y { get; set; }

            private readonly string _structName = "POINT";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class PALETTEENTRY_OBJ : ClipboardFormatBase
        {
            public BYTE peRed { get; set; }
            public BYTE peGreen { get; set; }
            public BYTE peBlue { get; set; }
            public BYTE peFlags { get; set; }

            private readonly string _structName = "PALETTEENTRY";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class LOGPALETTE_OBJ : ClipboardFormatBase
        {
            public WORD palVersion { get; set; }
            public WORD palNumEntries { get; set; }
            public List<PALETTEENTRY_OBJ> palPalEntry { get; set; }

            private readonly string _structName = "LOGPALETTE";

            public override string[] ItemsNotToPrint()
            {
                return new string[] { "palPalEntry" };
            }
        }

        public class LOGCOLORSPACEA_OBJ : ClipboardFormatBase
        {
            public DWORD lcsSignature { get; set; }
            public DWORD lcsVersion { get; set; }
            public DWORD lcsSize { get; set; }
            public LCSCSTYPE lcsCSType { get; set; }
            public LCSGAMUTMATCH lcsIntent { get; set; }
            public CIEXYZTRIPLE_OBJ lcsEndpoints { get; set; }
            public DWORD lcsGammaRed { get; set; }
            public DWORD lcsGammaGreen { get; set; }
            public DWORD lcsGammaBlue { get; set; }
            public string lcsFilename { get; set; }

            private readonly string _structName = "LOGCOLORSPACEA";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
            public static int MaxStringLength()
            {
                return 260;
            }
        }

        public enum LCSCSTYPE : uint // DWORD
        {
            // Can be one of the following values
            LCS_CALIBRATED_RGB = 0x00000000,
            LCS_sRGB = 0x73524742,
            LCS_WINDOWS_COLOR_SPACE = 0x57696E20
            }

        public enum LCSGAMUTMATCH : uint // DWORD
        {
            // Can be one of the following values
            LCS_GM_ABS_COLORIMETRIC = 0x00000008,
            LCS_GM_BUSINESS = 0x00000001,
            LCS_GM_GRAPHICS = 0x00000002,
            LCS_GM_IMAGES = 0x00000004
        }

        public class FILEGROUPDESCRIPTORW_OBJ : ClipboardFormatBase
        {
            public DWORD cItems { get; set; }
            public List<FILEDESCRIPTOR_OBJ> fgd { get; set; }

            private readonly string _structName = "FILEGROUPDESCRIPTORW";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class FILEDESCRIPTOR_OBJ : ClipboardFormatBase
        {
            public DWORD dwFlags { get; set; }
            public CLSID_OBJ clsid { get; set; }
            public SIZEL_OBJ sizel { get; set; }
            public POINTL_OBJ point { get; set; }
            public DWORD dwFileAttributes { get; set; }
            public FILETIME_OBJ ftCreationTime { get; set; }
            public FILETIME_OBJ ftLastAccessTime { get; set; }
            public FILETIME_OBJ ftLastWriteTime { get; set; }
            public DWORD nFileSizeHigh { get; set; }
            public DWORD nFileSizeLow { get; set; }
            public string cFileName { get; set; }

            public static int MetaDataOnlySize()
            {
                return 4 + 16 + 8 + 8 + 4 + 8 + 8 + 8 + 4 + 4;
            }
            public static int MaxStringLength()
            {
                return 260;
            }

            private readonly string _structName = "FILEDESCRIPTORW";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }

        }

        public class CLSID_OBJ : ClipboardFormatBase
        {
            public DWORD Data1 { get; set; }
            public WORD Data2 { get; set; }
            public WORD Data3 { get; set; }
            public double Data4 { get; set; } // 8 bytes

            // Method for total size
            public static int GetSize()
            {
                return 16;
            }

            private readonly string _structName = "CLSID";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class POINTL_OBJ : ClipboardFormatBase
        {
            public LONG x { get; set; }
            public LONG y { get; set; }

            private readonly string _structName = "POINTL";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class SIZEL_OBJ : ClipboardFormatBase
        {
            public DWORD cx { get; set; }
            public DWORD cy { get; set; }

            private readonly string _structName = "SIZEL";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class FILETIME_OBJ : ClipboardFormatBase
        {
            public DWORD dwLowDateTime { get; set; }
            public DWORD dwHighDateTime { get; set; }

            private readonly string _structName = "FILETIME";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class CIDA_OBJ : ClipboardFormatBase
        {
            private uint _cidl;
            private uint[] _aoffset;

            // Automatically updates the size of aoffset when cidl is set because it is dependent on it
            public uint cidl
            {
                get => _cidl;
                set
                {
                    _cidl = value;
                    _aoffset = new uint[_cidl + 1];
                }
            }
            // Still allow setting aoffset directly so we can put values into it
            public uint[] aoffset
            {
                get => _aoffset;
                set => _aoffset = value;
            }

            public ITEMIDLIST_OBJ[] ITEMIDLIST { get; set; } = new ITEMIDLIST_OBJ[0];

            private readonly string _structName = "CIDA";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class ITEMIDLIST_OBJ : ClipboardFormatBase
        {
            public SHITEMID_OBJ mkid { get; set; }

            private readonly string _structName = "ITEMIDLIST";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        public class SHITEMID_OBJ : ClipboardFormatBase
        {
            private USHORT _cb; // Size of the structure in bytes, including the cb field itself
            private byte[] _abID; // The actual data

            public uint cb
            {
                get => _cb;
                set
                {
                    _cb = (USHORT)value;
                    _abID = new byte[_cb - sizeof(USHORT)];
                }
            }

            // Still allow setting aoffset directly so we can put values into it
            public byte[] abID
            {
                get => _abID;
                set => _abID = value;
            }

            private readonly string _structName = "SHITEMID";

            public override string[] ItemsNotToPrint()
            {
                return new string[0];
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------
        // --------------------------------------------------- Struct definitions ---------------------------------------------------
        // --------------------------------------------------------------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public LONG bmType;
            public LONG bmWidth;
            public LONG bmHeight;
            public LONG bmWidthBytes;
            public WORD bmPlanes;
            public WORD bmBitsPixel;
            public LPVOID bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPV5HEADER
        {
            public DWORD bV5Size;
            public LONG bV5Width;
            public LONG bV5Height;
            public WORD bV5Planes;
            public WORD bV5BitCount;
            public DWORD bV5Compression;
            public DWORD bV5SizeImage;
            public LONG bV5XPelsPerMeter;
            public LONG bV5YPelsPerMeter;
            public DWORD bV5ClrUsed;
            public DWORD bV5ClrImportant;
            public DWORD bV5RedMask;
            public DWORD bV5GreenMask;
            public DWORD bV5BlueMask;
            public DWORD bV5AlphaMask;
            public LOGCOLORSPACEA bV5CSType;
            public CIEXYZTRIPLE bV5Endpoints;
            public DWORD bV5GammaRed;
            public DWORD bV5GammaGreen;
            public DWORD bV5GammaBlue;
            public DWORD bV5Intent;
            public DWORD bV5ProfileData;
            public DWORD bV5ProfileSize;
            public DWORD bV5Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public DWORD biSize;
            public LONG biWidth;
            public LONG biHeight;
            public WORD biPlanes;
            public WORD biBitCount;
            public DWORD biCompression;
            public DWORD biSizeImage;
            public LONG biXPelsPerMeter;
            public LONG biYPelsPerMeter;
            public DWORD biClrUsed;
            public DWORD biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RGBQUAD
        {
            public BYTE rgbBlue;
            public BYTE rgbGreen;
            public BYTE rgbRed;
            public BYTE rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public RGBQUAD[] bmiColors;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct METAFILEPICT
        {
            public LONG mm;
            public LONG xExt;
            public LONG yExt;
            public HMETAFILE hMF;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIEXYZ
        {
            public FXPT2DOT30 ciexyzX;
            public FXPT2DOT30 ciexyzY;
            public FXPT2DOT30 ciexyzZ;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIEXYZTRIPLE
        {
            public CIEXYZ ciexyzRed;
            public CIEXYZ ciexyzGreen;
            public CIEXYZ ciexyzBlue;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DROPFILES
        {
            public DWORD pFiles;
            public POINT pt;
            public BOOL fNC;
            public BOOL fWide;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public LONG x;
            public LONG y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PALETTEENTRY
        {
            public BYTE peRed;
            public BYTE peGreen;
            public BYTE peBlue;
            public BYTE peFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LOGPALETTE
        {
            public WORD palVersion;
            public WORD palNumEntries;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public PALETTEENTRY[] palPalEntry;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LOGCOLORSPACEA
        {
            public DWORD lcsSignature;
            public DWORD lcsVersion;
            public DWORD lcsSize;
            public DWORD lcsCSType;
            public DWORD lcsIntent;
            public CIEXYZTRIPLE lcsEndpoints;
            public DWORD lcsGammaRed;
            public DWORD lcsGammaGreen;
            public DWORD lcsGammaBlue;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
            public CHAR[] lcsFilename;
        }

        // --------------------------------------------------- Helper methods ---------------------------------------------------

        public const int MAX_PATH = 260;

        public static string EnumLookup(Type enumType, uint value)
        {
            return Enum.GetName(enumType, value);
        }

        public static T BytesToObject<T>(byte[] data) where T : new()
        {
            int offset = 0;
            return (T)ReadValue(typeof(T), data, ref offset);
        }

        private static object ReadValue(Type type, byte[] data, ref int offset, Type callingClass = null, int collectionSize = -1)
        {
            int remainingBytes = data.Length - offset;

            if (type == typeof(BYTE))
            {
                if (remainingBytes < sizeof(BYTE))
                    throw new ArgumentException("Not enough data to read BYTE");
                byte value = data[offset];
                offset += sizeof(BYTE);
                return value;
            }
            else if (type == typeof(CHAR))
            {
                if (remainingBytes < sizeof(CHAR))
                    throw new ArgumentException("Not enough data to read CHAR");
                char value = (char)data[offset];
                offset += sizeof(CHAR);
                return value;
            }
            else if (type == typeof(WORD)) // 2 bytes - Ushort, UInt16
            {
                if (remainingBytes < sizeof(WORD))
                    throw new ArgumentException("Not enough data to read WORD");
                WORD value = BitConverter.ToUInt16(data, offset);
                offset += sizeof(WORD);
                return value;
            }
            else if (type == typeof(DWORD))
            {
                if (remainingBytes < sizeof(DWORD))
                    throw new ArgumentException("Not enough data to read DWORD / uint");
                DWORD value = BitConverter.ToUInt32(data, offset);
                offset += sizeof(DWORD);
                return value;
            }
            else if (type == typeof(LONG))
            {
                if (remainingBytes < sizeof(LONG))
                    throw new ArgumentException("Not enough data to read LONG");
                LONG value = BitConverter.ToInt32(data, offset);
                offset += sizeof(LONG);
                return value;
            }
            else if (type == typeof(BOOL))
            {
                if (remainingBytes < sizeof(BOOL))
                    throw new ArgumentException("Not enough data to read BOOL");
                BOOL value = BitConverter.ToInt32(data, offset);
                offset += sizeof(BOOL);
                return value;
            }
            else if (type == typeof(double))
            {
                if (remainingBytes < sizeof(double))
                    throw new ArgumentException("Not enough data to read double");
                double value = BitConverter.ToDouble(data, offset);
                offset += sizeof(double);
                return value;
            }
            else if (type == typeof(LPVOID))
            {
                int size = IntPtr.Size;
                if (remainingBytes < size)
                    throw new ArgumentException("Not enough data to read LPVOID");
                IntPtr value;
                if (size == 4)
                {
                    value = (IntPtr)BitConverter.ToInt32(data, offset);
                }
                else
                {
                    value = (IntPtr)BitConverter.ToInt64(data, offset);
                }
                offset += size;
                return value;
            }
            else if (type == typeof(FXPT2DOT30))
            {
                if (remainingBytes < sizeof(FXPT2DOT30))
                    throw new ArgumentException("Not enough data to read FXPT2DOT30");
                FXPT2DOT30 value = BitConverter.ToInt32(data, offset);
                offset += sizeof(FXPT2DOT30);
                return value;
            }
            else if (type == typeof(string))
            {
                if (remainingBytes <= 0)
                    throw new ArgumentException("Not enough data to read string");

                int maxStringLength = MAX_PATH;

                // Try to get MaxStringLength from the declaring type of the calling method
                if (callingClass != null)
                {
                    var declaringType = callingClass.DeclaringType;
                    var maxStringLengthMethod = declaringType?.GetMethod("MaxStringLength", BindingFlags.Public | BindingFlags.Static);
                    if (maxStringLengthMethod != null)
                    {
                        maxStringLength = (int)maxStringLengthMethod.Invoke(null, null);
                    }
                }

                string value = Encoding.Unicode.GetString(data, offset, Math.Min(maxStringLength * 2, remainingBytes));
                int terminatorIndex = value.IndexOf('\0');

                // Only return the string up to the null terminator.
                value = terminatorIndex >= 0 ? value.Substring(0, terminatorIndex) : value;
                // Decode to UTF-8 to remove any null characters in between the string, then remove any remaining null characters
                value = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value));
                value = value.Replace("\0", "");

                offset += maxStringLength * 2; // Still increment till the end of the allocated space
                return value;
            }
            // For arrays
            else if (type.IsArray)
            {
                // If it's a known size, we can recurse through it that many times
                if (collectionSize > 0)
                {
                    var elementType = type.GetElementType();
                    var array = Array.CreateInstance(elementType, collectionSize);
                    for (int i = 0; i < collectionSize; i++)
                    {
                        array.SetValue(ReadValue(elementType, data, ref offset), i);
                    }
                    return array;
                }
                // If it's a variable size, we will iterate through based on primitive type 
                else
                {
                    var elementType = type.GetElementType();
                    var list = new List<object>();
                    while (remainingBytes > 0)
                    {
                        try
                        {
                            object element = ReadValue(elementType, data, ref offset);
                            list.Add(element);
                            remainingBytes = data.Length - offset;
                        }
                        catch (ArgumentException)
                        {
                            // We've reached the end of the data or can't read another element
                            break;
                        }
                    }
                    return list.ToArray();
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType);

                // Read elements until we run out of data
                while (remainingBytes > 0)
                {
                    try
                    {
                        object element = ReadValue(elementType, data, ref offset);
                        list.Add(element);
                        remainingBytes = data.Length - offset;
                    }
                    catch (ArgumentException)
                    {
                        // We've reached the end of the data or can't read another element
                        break;
                    }
                }
                return list;
            }
            else if (type.IsClass)
            {
                object obj = Activator.CreateInstance(type);
                int collectionSizeToPassIn = -1;

                foreach (var (propertyName, propertyValue, propertyType, arraySize) in ((IClipboardFormat)obj).EnumeratePropertiesWithType())
                {
                    if (remainingBytes <= 0)
                        break;  // Stop reading if we've reached the end of the data

                    //if (propertyValue == null)
                    //    continue;

                    try
                    {
                        Type typeToUse = propertyType;
                        if (arraySize.HasValue)
                        {
                            if (arraySize.Value > 0)
                            {
                                collectionSizeToPassIn = arraySize ?? -1; // It shouldn't be null here because of if statement, but compiler requires null check
                            }
                            else
                            {
                                continue; // Skip this property if the array size is 0. It's probably a placeholder to add processed data later
                            }
                        }

                        object value = ReadValue(typeToUse, data, ref offset, collectionSize: collectionSizeToPassIn);
                        type.GetProperty(propertyName).SetValue(obj, value);
                        remainingBytes = data.Length - offset;
                    }
                    catch (ArgumentException)
                    {
                        // We've reached the end of the data or can't read this property
                        break;
                    }
                }

                return obj;
            }
            else if (type.IsEnum && Enum.GetUnderlyingType(type) == typeof(uint))
            {
                if (remainingBytes < sizeof(uint))
                    throw new ArgumentException("Not enough data to read enum");
                uint value = BitConverter.ToUInt32(data, offset);
                offset += sizeof(uint);
                return Enum.ToObject(type, value);
            }
            else
            {
                throw new NotSupportedException($"Type {type} is not supported.");
            }
        }

        // Dictionary containing names of structs as keys and links to microsoft articles about them
        public static readonly Dictionary<string, string> StructDocsLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "BITMAP", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmap" },
            { "BITMAPV5HEADER", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapv5header" },
            { "BITMAPINFOHEADER", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader" },
            { "RGBQUAD", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-rgbquad" },
            { "BITMAPINFO", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfo" },
            { "METAFILEPICT", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-metafilepict" },
            { "CIEXYZ", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-ciexyz" },
            { "CIEXYZTRIPLE", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-ciexyztriple" },
            { "DROPFILES", "https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ns-shlobj_core-dropfiles" },
            { "POINT", "https://learn.microsoft.com/en-us/windows/win32/api/windef/ns-windef-point" },
            { "PALETTEENTRY", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-paletteentry" },
            { "LOGPALETTE", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-logpalette" },
            { "LOGCOLORSPACEA", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-logcolorspacea" },
            { "LCSCSTYPE", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/eb4bbd50-b3ce-4917-895c-be31f214797f" },
            { "LCSGAMUTMATCH", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/9fec0834-607d-427d-abd5-ab240fb0db38" },
            { "bV5Compression", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/4e588f70-bd92-4a6f-b77f-35d0feaf7a57" },
            { "FILEDESCRIPTORW", "https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ns-shlobj_core-filedescriptorw" },
            { "FILEGROUPDESCRIPTORW", "https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ns-shlobj_core-filegroupdescriptorw" },
            { "FILETIME", "https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-filetime" },
            { "POINTL", "https://learn.microsoft.com/en-us/windows/win32/api/windef/ns-windef-pointl" },
            { "SIZEL", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/17b541c5-f8ee-4111-b1f2-012128f35871" },
            { "CLSID", "https://learn.microsoft.com/en-us/windows/win32/api/guiddef/ns-guiddef-guid" },
            { "CIDA", "https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ns-shlobj_core-cida" }
        };

        // Dictionary for docs to non-standard registered formats other than structs
        public static readonly Dictionary<string, string> FormatDocsLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "HTML Format", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/html-clipboard-format" },
            { "CanIncludeInClipboardHistory", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats" },
            { "CanUploadToCloudClipboard", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats" },
            { "ExcludeClipboardContentFromMonitorProcessing", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats" }
        };

        public static readonly Dictionary<string, string> KnownBinaryExtensionAssociations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) // Case insensitive
        {
            // Key is the format name (lower case), value is the file extension 
            { "png", "png" },
            { "csv", "csv" },
            { "biff12", "xlsb" },
            { "biff8", "xls" },
            { "biff5", "xls" },
            { "cf_sylk", "slk" },
            { "rich text format", "rtf" },
            { "jfif", "jpg" },
            { "text", "txt" },
            { "gif", "gif" },
            { "image/svg+xml", "svg" },
            { "cf_dif", "dif" },
            { "xml spreadsheet", "xml" },
            { "text/html", "html" }

        };

        // Helper function to get documentation URLs for a class and it's sub-classes using DocumentationUrl() method of each
        // Iterates them and puts them into list. Parameter is the object itself. Recursive.
        public static Dictionary<string, string> GetDocumentationUrls(object obj)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            if (obj == null)
                return results;

            Type type = obj.GetType();

            // Only proceed if the object is a class or an enum
            if (!type.IsClass && !type.IsEnum)
                return results;

            // Check if the type has a GetDocumentationUrl method
            var docUrlMethod = type.GetMethod("GetDocumentationUrl", BindingFlags.Public | BindingFlags.Static);
            if (docUrlMethod != null)
            {
                try
                {
                    var result = docUrlMethod.Invoke(null, null);
                    if (result is ValueTuple<string, string> tuple)
                    {
                        results[tuple.Item1] = tuple.Item2;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error invoking GetDocumentationUrl method: {ex.Message}");
                }
            }

            // If it's an enum, we're done
            if (type.IsEnum)
                return results;

            // For classes, process their properties
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead)
                    continue;

                object value = null;
                try
                {
                    // Check if the property is indexed
                    if (property.GetIndexParameters().Length > 0)
                    {
                        // Skip indexed properties
                        continue;
                    }

                    value = property.GetValue(obj);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting value for property {property.Name}: {ex.Message}");
                    continue;
                }

                if (value == null)
                    continue;

                Type propertyType = value.GetType();

                if (propertyType.IsClass || propertyType.IsEnum)
                {
                    if (value is IEnumerable<object> collection)
                    {
                        foreach (var item in collection)
                        {
                            foreach (var kvp in GetDocumentationUrls(item))
                            {
                                results[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    else
                    {
                        // Ensure it's not a primitive or else we'll get stuck in an infinite loop
                        if (!propertyType.IsPrimitive)
                        {
                            foreach (var kvp in GetDocumentationUrls(value))
                            {
                                results[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }

            return results;
        }

    }

}
