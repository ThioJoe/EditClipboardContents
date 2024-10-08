﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression

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


    public static class ClipboardFormats
    {
        public class BITMAP
        {
            public LONG bmType { get; set; }
            public LONG bmWidth { get; set; }
            public LONG bmHeight { get; set; }
            public LONG bmWidthBytes { get; set; }
            public WORD bmPlanes { get; set; }
            public WORD bmBitsPixel { get; set; }
            public LPVOID bmBits { get; set; }
        }

        public class BITMAPV5HEADER
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
            public LOGCOLORSPACEA bV5CSType { get; set; }
            public CIEXYZTRIPLE bV5Endpoints { get; set; }
            public DWORD bV5GammaRed { get; set; }
            public DWORD bV5GammaGreen { get; set; }
            public DWORD bV5GammaBlue { get; set; }
            public DWORD bV5Intent { get; set; }
            public DWORD bV5ProfileData { get; set; }
            public DWORD bV5ProfileSize { get; set; }
            public DWORD bV5Reserved { get; set; }
        }

        public enum bV5Compression : uint // DWORD
        {
            BI_RGB       = 0x0000,
            BI_RLE8      = 0x0001,
            BI_RLE4      = 0x0002,
            BI_BITFIELDS = 0x0003,
            BI_JPEG      = 0x0004,
            BI_PNG       = 0x0005,
            BI_CMYK      = 0x000B,
            BI_CMYKRLE8  = 0x000C,
            BI_CMYKRLE4  = 0x000D
        }

        public class BITMAPINFOHEADER
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
        }

        public class RGBQUAD
        {
            public BYTE rgbBlue { get; set; }
            public BYTE rgbGreen { get; set; }
            public BYTE rgbRed { get; set; }
            public BYTE rgbReserved { get; set; }
        }

        public class BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader { get; set; }
            public List<RGBQUAD> bmiColors { get; set; }
        }

        public class METAFILEPICT
        {
            public LONG mm { get; set; }
            public LONG xExt { get; set; }
            public LONG yExt { get; set; }
            public HMETAFILE hMF { get; set; }
        }

        public class CIEXYZ
        {
            public FXPT2DOT30 ciexyzX { get; set; }
            public FXPT2DOT30 ciexyzY { get; set; }
            public FXPT2DOT30 ciexyzZ { get; set; }
        }

        public class CIEXYZTRIPLE
        {
            public CIEXYZ ciexyzRed { get; set; }
            public CIEXYZ ciexyzGreen { get; set; }
            public CIEXYZ ciexyzBlue { get; set; }
        }

        public class DROPFILES
        {
            public DWORD pFiles { get; set; }
            public POINT pt { get; set; }
            public BOOL fNC { get; set; }
            public BOOL fWide { get; set; }
        }

        public class POINT
        {
            public LONG x { get; set; }
            public LONG y { get; set; }
        }

        public class PALETTEENTRY
        {
            public BYTE peRed { get; set; }
            public BYTE peGreen { get; set; }
            public BYTE peBlue { get; set; }
            public BYTE peFlags { get; set; }
        }

        public class LOGPALETTE
        {
            public WORD palVersion { get; set; }
            public WORD palNumEntries { get; set; }
            public List<PALETTEENTRY> palPalEntry { get; set; }
        }
        // public enum?
        public class LOGCOLORSPACEA
        {
            public DWORD lcsSignature { get; set; }
            public DWORD lcsVersion { get; set; }
            public DWORD lcsSize { get; set; }
            public LCSCSTYPE lcsCSType { get; set; }
            public LCSGAMUTMATCH lcsIntent { get; set; }
            public CIEXYZTRIPLE lcsEndpoints { get; set; }
            public DWORD lcsGammaRed { get; set; }
            public DWORD lcsGammaGreen { get; set; }
            public DWORD lcsGammaBlue { get; set; }
            public List<CHAR> lcsFilename { get; set; } // Max path is 260 usually. Originally defined as lcsFilename[MAX_PATH], null terminated string
        }
        
        public enum LCSCSTYPE : uint
        {
            // Can be one of the following values
            LCS_CALIBRATED_RGB      = 0x00000000,
            LCS_sRGB                = 0x73524742,
            LCS_WINDOWS_COLOR_SPACE = 0x57696E20
        }

        public enum LCSGAMUTMATCH : uint
        {
            // Can be one of the following values
            LCS_GM_ABS_COLORIMETRIC =   0x00000008,
            LCS_GM_BUSINESS         =   0x00000001,
            LCS_GM_GRAPHICS         =   0x00000002,
            LCS_GM_IMAGES           =   0x00000004
        }

        public static string EnumLookup(Type enumType, uint value)
        {
            return Enum.GetName(enumType, value);
        }

        public static T BytesToObject<T>(byte[] data) where T : new()
        {
            int offset = 0;
            return (T)ReadValue(typeof(T), data, ref offset);
        }

        private static object ReadValue(Type type, byte[] data, ref int offset)
        {
            if (type == typeof(BYTE))
            {
                offset += sizeof(BYTE);
                return data[offset];
            }
            else if (type == typeof(CHAR))
            {
                offset += sizeof(CHAR);
                return (CHAR)data[offset];
            }
            else if (type == typeof(WORD))
            {
                WORD value = BitConverter.ToUInt16(data, offset);
                offset += sizeof(WORD);
                return value;
            }
            else if (type == typeof(DWORD))
            {
                DWORD value = BitConverter.ToUInt32(data, offset);
                offset += sizeof(DWORD);
                return value;
            }
            else if (type == typeof(LONG))
            {
                LONG value = BitConverter.ToInt32(data, offset);
                offset += sizeof(LONG);
                return value;
            }
            else if (type == typeof(BOOL))
            {
                BOOL value = BitConverter.ToInt32(data, offset);
                offset += sizeof(BOOL);
                return value;
            }
            else if (type == typeof(LPVOID))
            {
                LPVOID value = (IntPtr)BitConverter.ToInt64(data, offset);
                offset += IntPtr.Size;
                return value;
            }
            else if (type == typeof(FXPT2DOT30))
            {
                FXPT2DOT30 value = BitConverter.ToInt32(data, offset);
                offset += sizeof(FXPT2DOT30);
                return value;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = type.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType);

                int listSize = 0;
                // Get the total size of the types in the list
                foreach (var property in elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanWrite)
                    {
                        listSize += Marshal.SizeOf(property.PropertyType);
                    }
                }

                // If it's an empty list return it or else it will throw an exception for some reason
                if (listSize == 0)
                {
                    return list;
                }

                // Instead of reading a count, we'll read elements until we reach the end of the data
                while (offset + listSize < data.Length)
                {
                    list.Add(ReadValue(elementType, data, ref offset));
                }
                return list;
            }
            else if (type.IsClass)
            {
                object obj = Activator.CreateInstance(type);
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanWrite)
                    {
                        object value = ReadValue(property.PropertyType, data, ref offset);
                        property.SetValue(obj, value);
                    }
                }
                return obj;
            }
            // if it's uint based enum
            else if (type.IsEnum && Enum.GetUnderlyingType(type) == typeof(uint))
            {
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
        public static Dictionary<string, string> StructDocsLinks = new Dictionary<string, string>
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
            { "PALETTEENTRY", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-paletteentry" },
            { "LOGPALETTE", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-logpalette" },
            { "LOGCOLORSPACEA", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-logcolorspacea" },
            { "LCSCSTYPE", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/eb4bbd50-b3ce-4917-895c-be31f214797f" },
            { "LCSGAMUTMATCH", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/9fec0834-607d-427d-abd5-ab240fb0db38" },
            { "bV5Compression", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/4e588f70-bd92-4a6f-b77f-35d0feaf7a57" }
        };

        // Dictionary containing the names of the types of structs as keys and any variable sized item properties or handles as values
        public static Dictionary<string, string[]> VariableSizedItems = new Dictionary<string, string[]>
        {
            { "BITMAP", new string[] { "bmBits" } },                // Pointer
            { "BITMAPV5HEADER", new string[] { null } },            // Contains sub-items but they are all fixed
            { "BITMAPINFOHEADER", new string[] { null } },          // All fixed data
            { "BITMAPINFO", new string[] { "bmiColors" } },         // Dynamically sized array of RGBQUAD with image data
            { "METAFILEPICT", new string[] { "hMF" } },             // Pointer to HMETAFILE
            { "CIEXYZTRIPLE", new string[] { null } },              // Contains sub-items but they are all fixed
            { "DROPFILES", new string[] { "pt" } },                 // Pointer
            { "LOGPALETTE", new string[] { "palPalEntry" } }        // Dynamically sized array of PALETTEENTRY
        };

    }

}
