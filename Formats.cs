using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about inputArray initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression
// Nullable reference types
#nullable enable


// Notes:
// This file contains struct definitions for various clipboard formats. The definitions are based on the official Microsoft documentation.
// But it also contains classes that mirror the structs, which may contain lists in place of arrays and other differences to make them easier to parse
// The actual structs are used with Marshal to read the data from the clipboard. The classes are used to store the data in a more readable format as an object
// Structs are really only used for certain standard clipboard formats, since those formats often are just pointers to the struct, and Marsshal requires a struct to copy the data out
//   Then the class version can be used to process those too. Some don't require the struct to get the data out, so the class is used directly

namespace EditClipboardContents
{
    // Win32 API Types defined explicitly to avoid confusion and ensure compatibility with Win32 API, and it matches with documentation
    // See: https://learn.microsoft.com/en-us/windows/win32/winprog/windows-data-types
    using BOOL = System.Int32;          // 4 Bytes
    using LONG = System.Int32;          // 4 Bytes
    using DWORD = System.UInt32;        // 4 Bytes, aka uint, uint32
    using WORD = System.UInt16;         // 2 Bytes
    using BYTE = System.Byte;           // 1 Byte
    using FXPT2DOT30 = System.Int32;    // 4 Bytes , aka LONG
    using LPVOID = System.IntPtr;       // Handle to any type
    using HMETAFILE = System.IntPtr;    // Handle to metafile
    using CHAR = System.Byte;           // 1 Byte
    using WCHAR = System.Char;        // 2 Bytes
    using USHORT = System.UInt16;       // 2 Bytes
    using UINT32 = System.UInt32;       // 4 Bytes
    using INT16 = System.Int16;         // 2 Bytes
    using UINT = System.UInt32;         // 4 Bytes
    using static System.Net.WebRequestMethods;


    public static class ClipboardFormats
    {
        public interface IClipboardFormat
        {
            string? GetDocumentationUrl();
            string? StructName();
            Dictionary<string, string> DataDisplayReplacements();
            List<string> PropertiesNoProcess();
            void SetCacheStructObjectDisplayInfo(string structInfo);
            string GetCacheStructObjectDisplayInfo();
            IEnumerable<(string Name, object? Value, Type Type, int? ArraySize)> EnumerateProperties(bool getValues = false);
            bool FillEmptyArrayWithRemainingBytes();
            int MaxStringLength();
        }

        public abstract class ClipboardFormatBase : IClipboardFormat
        {
            // Protected method to be implemented by derived classes. But if it's en enum then check for StructNameAttribute
            protected virtual string? GetStructName()
            {
                Type type = this.GetType();
                if (type.IsEnum)
                {
                    var attr = type.GetCustomAttribute<StructNameAttribute>();
                    if (attr != null)
                        return attr.Name;
                    else
                        return null;
                }
                // If it's not an enum or doesn't have the attribute, derived classes should override this
                throw new NotImplementedException(
                    $"Type {type.Name} must either be an enum with StructNameAttribute or override GetStructName()");
            }

            // Public method to access the struct name
            public string? StructName() => GetStructName();

            // MaxStringLength method
            public virtual int MaxStringLength() => 0;

            // Private field to store the cached struct display info
            private string? _cachedStructDisplayInfo;

            // Default implementation for FillEmptyArrayWithRemainingBytes - If the last array should be filled with bytes. Defaults to false
            public virtual bool FillEmptyArrayWithRemainingBytes() => false;

            // Common methods apply to all classes of the type
            public virtual string? GetDocumentationUrl()
            {
                string? structName = StructName();
                if (structName == null || !FormatInfoHardcoded.StructDocsLinks.ContainsKey(structName))
                {
                    return null;
                }
                else
                {
                    return FormatInfoHardcoded.StructDocsLinks[structName];
                }
            }

            // Default implementation for DataDisplayReplacements - Things that need to be replaced or pre-processed before displaying
            public virtual Dictionary<string, string> DataDisplayReplacements() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Properties to not even process into the object because it won't be used at all
            public virtual List<string> PropertiesNoProcess() => new List<string>();

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

            public virtual IEnumerable<(string Name, object? Value, Type Type, int? ArraySize)> EnumerateProperties(bool getValues = false)
            {
                var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    var type = property.PropertyType;
                    object? value = null;
                    int? arraySize = null;

                    if (getValues || typeof(ICollection).IsAssignableFrom(type) || type.IsArray)
                    {
                        try
                        {
                            value = property.GetValue(this);

                            if (value is ICollection collection)
                            {
                                arraySize = collection.Count;
                            }
                            else if (value is Array array)
                            {
                                arraySize = array.Length;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error getting value for property {property.Name}: {ex.Message}");
                            // Continue to the next property if there's an error
                            continue;
                        }
                    }

                    // If getValues is false, we always return null for the Value
                    yield return (property.Name, getValues ? value : null, type, arraySize);
                }
            }

        }

        // Static helper methods to be able to object info without creating an object
        public static string? GetDocumentationUrl<T>() where T : IClipboardFormat, new()
        {
            return new T().GetDocumentationUrl();
        }

        public static string StructName<T>() where T : IClipboardFormat, new()
        {
            return new T().StructName();
        }

        public static Dictionary<string, string> GetVariableSizedItems<T>() where T : IClipboardFormat, new()
        {
            return new T().DataDisplayReplacements();
        }

        public static List<string> PropertiesNoProcess<T>() where T : IClipboardFormat, new()
        {
            return new T().PropertiesNoProcess();
        }

        public static bool FillEmptyArrayWithRemainingBytes<T>() where T : IClipboardFormat, new()
        {
            return new T().FillEmptyArrayWithRemainingBytes();
        }

        public static int MaxStringLength<T>() where T : IClipboardFormat, new()
        {
            return new T().MaxStringLength();
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

            protected override string GetStructName() => "BITMAP";
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
            public LOGCOLORSPACEW_OBJ bV5CSType { get; set; } = new LOGCOLORSPACEW_OBJ();
            public CIEXYZTRIPLE_OBJ bV5Endpoints { get; set; } = new CIEXYZTRIPLE_OBJ();
            public DWORD bV5GammaRed { get; set; }
            public DWORD bV5GammaGreen { get; set; }
            public DWORD bV5GammaBlue { get; set; }
            public DWORD bV5Intent { get; set; }
            public DWORD bV5ProfileData { get; set; }
            public DWORD bV5ProfileSize { get; set; }
            public DWORD bV5Reserved { get; set; }

            protected override string GetStructName() => "BITMAPV5HEADER";
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

            protected override string GetStructName() => "BITMAPINFOHEADER";
        }

        public class RGBQUAD_OBJ : ClipboardFormatBase
        {
            public BYTE rgbBlue { get; set; }
            public BYTE rgbGreen { get; set; }
            public BYTE rgbRed { get; set; }
            public BYTE rgbReserved { get; set; }

            protected override string GetStructName() => "RGBQUAD";
        }

        public class BITMAPINFO_OBJ : ClipboardFormatBase
        {
            public BITMAPINFOHEADER_OBJ bmiHeader { get; set; } = new BITMAPINFOHEADER_OBJ();
            public List<RGBQUAD_OBJ> bmiColors { get; set; } = [];

            protected override string GetStructName() => "BITMAPINFO";

            public override Dictionary<string, string> DataDisplayReplacements()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "bmiColors", "[Color data bytes]" }
                };
            }

            public override List<string> PropertiesNoProcess()
            {
                return ["bmiColors"];
            }
        }

        public class METAFILEPICT_OBJ : ClipboardFormatBase
        {
            public LONG mm { get; set; }
            public LONG xExt { get; set; }
            public LONG yExt { get; set; }
            //public byte[] hMF { get; set; } = []; // Handle to metafile. Will process as METAFILE_OBJ later separately
            public METAFILE_OBJ hMF { get; set; } = new METAFILE_OBJ();

            protected override string GetStructName() => "METAFILEPICT";

            //public override Dictionary<string, string> DataDisplayReplacements()
            //{
            //    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            //    {
            //        { "hMF", "[Handle to metafile]" }
            //    };
            //}

            public override bool FillEmptyArrayWithRemainingBytes() => true;
        }

        public class CIEXYZ_OBJ : ClipboardFormatBase
        {
            public FXPT2DOT30 ciexyzX { get; set; }
            public FXPT2DOT30 ciexyzY { get; set; }
            public FXPT2DOT30 ciexyzZ { get; set; }
            protected override string GetStructName() => "CIEXYZ";
        }

        public class CIEXYZTRIPLE_OBJ : ClipboardFormatBase
        {
            public CIEXYZ_OBJ ciexyzRed { get; set; } = new CIEXYZ_OBJ();
            public CIEXYZ_OBJ ciexyzGreen { get; set; } = new CIEXYZ_OBJ();
            public CIEXYZ_OBJ ciexyzBlue { get; set; } = new CIEXYZ_OBJ();

            protected override string GetStructName() => "CIEXYZTRIPLE";
        }

        public class DROPFILES_OBJ : ClipboardFormatBase
        {
            public DWORD pFiles { get; set; }
            public POINT_OBJ pt { get; set; } = new POINT_OBJ();
            public BOOL fNC { get; set; }
            public BOOL fWide { get; set; }

            // Method for total size
            public int GetSize()
            {
                return Marshal.SizeOf(this);
            }

            protected override string GetStructName() => "DROPFILES";

            public override Dictionary<string, string> DataDisplayReplacements()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "pt", "[Memory Handle]" }
                };
            }

        }

        public class POINT_OBJ : ClipboardFormatBase
        {
            public LONG x { get; set; }
            public LONG y { get; set; }

            protected override string GetStructName() => "POINT";
        }

        public class PALETTEENTRY_OBJ : ClipboardFormatBase
        {
            public BYTE peRed { get; set; }
            public BYTE peGreen { get; set; }
            public BYTE peBlue { get; set; }
            public BYTE peFlags { get; set; }

            protected override string GetStructName() => "PALETTEENTRY";
        }

        public class LOGPALETTE_OBJ : ClipboardFormatBase
        {
            private WORD _palVersion { get; set; }
            private WORD _palNumEntries { get; set; }
            //private List<PALETTEENTRY_OBJ> _palPalEntry { get; set; } = [];
            private PALETTEENTRY_OBJ[] _palPalEntry { get; set; } = [];

            public WORD palVersion
            {
                get => _palVersion;
                set => _palVersion = value;
            }

            public WORD palNumEntries
            {
                get => _palNumEntries;
                set
                {
                    _palNumEntries = value;
                    //_palPalEntry = new List<PALETTEENTRY_OBJ>(_palNumEntries);
                    _palPalEntry = new PALETTEENTRY_OBJ[_palNumEntries];
                }
            }

            //public List<PALETTEENTRY_OBJ> palPalEntry
            public PALETTEENTRY_OBJ[] palPalEntry
            {
                get => _palPalEntry;
                set => _palPalEntry = value;
            }

            protected override string GetStructName() => "LOGPALETTE";

            public override Dictionary<string, string> DataDisplayReplacements()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "palPalEntry", "[Color Data Bytes]" }
                };
            }
        }

        public class LOGCOLORSPACEW_OBJ : ClipboardFormatBase
        {
            public DWORD lcsSignature { get; set; }
            public DWORD lcsVersion { get; set; }
            public DWORD lcsSize { get; set; }
            public LCSCSTYPE lcsCSType { get; set; }
            public LCSGAMUTMATCH lcsIntent { get; set; }
            public CIEXYZTRIPLE_OBJ lcsEndpoints { get; set; } = new CIEXYZTRIPLE_OBJ();
            public DWORD lcsGammaRed { get; set; }
            public DWORD lcsGammaGreen { get; set; }
            public DWORD lcsGammaBlue { get; set; }
            public string lcsFilename { get; set; } = string.Empty;

            protected override string GetStructName() => "LOGCOLORSPACEW";
            public override int MaxStringLength() => MAX_PATH;
        }



        public class FILEGROUPDESCRIPTORW_OBJ : ClipboardFormatBase
        {
            public DWORD cItems { get; set; }
            public List<FILEDESCRIPTOR_OBJ> fgd { get; set; } = [];

            protected override string GetStructName() => "FILEGROUPDESCRIPTORW";
        }

        public class FILEDESCRIPTOR_OBJ : ClipboardFormatBase
        {
            public DWORD dwFlags { get; set; }
            public CLSID_OBJ clsid { get; set; } = new CLSID_OBJ();
            public SIZEL_OBJ sizel { get; set; } = new SIZEL_OBJ();
            public POINTL_OBJ point { get; set; } = new POINTL_OBJ();
            public DWORD dwFileAttributes { get; set; }
            public FILETIME_OBJ ftCreationTime { get; set; } = new FILETIME_OBJ();
            public FILETIME_OBJ ftLastAccessTime { get; set; } = new FILETIME_OBJ();
            public FILETIME_OBJ ftLastWriteTime { get; set; } = new FILETIME_OBJ();
            public DWORD nFileSizeHigh { get; set; }
            public DWORD nFileSizeLow { get; set; }
            public string cFileName { get; set; } = string.Empty;

            public static int MetaDataOnlySize()
            {
                return 4 + 16 + 8 + 8 + 4 + 8 + 8 + 8 + 4 + 4;
            }
            public override int MaxStringLength() => MAX_PATH;

            protected override string GetStructName() => "FILEDESCRIPTORW";

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

            protected override string GetStructName() => "CLSID";
        }

        public class POINTL_OBJ : ClipboardFormatBase
        {
            public LONG x { get; set; }
            public LONG y { get; set; }

            protected override string GetStructName() => "POINTL";
        }

        public class SIZEL_OBJ : ClipboardFormatBase
        {
            public DWORD cx { get; set; }
            public DWORD cy { get; set; }

            protected override string GetStructName() => "SIZEL";
        }

        public class FILETIME_OBJ : ClipboardFormatBase
        {
            public DWORD dwLowDateTime { get; set; }
            public DWORD dwHighDateTime { get; set; }

            protected override string GetStructName() => "FILETIME";
        }

        public class CIDA_OBJ : ClipboardFormatBase
        {
            private uint _cidl;
            private uint[] _aoffset = [];
            private ITEMIDLIST_OBJ[] _ITEMIDLIST = [];

            // Automatically updates the size of aoffset when cidl is set because it is dependent on it
            public uint cidl
            {
                get => _cidl;
                set
                {
                    _cidl = value;
                    _aoffset = new uint[_cidl + 1];
                    _ITEMIDLIST = []; // Initialize to empty array since we are manually going to fill it later with separate processing
                }
            }
            // Still allow setting aoffset directly so we can put values into it
            public uint[] aoffset
            {
                get => _aoffset;
                set => _aoffset = value;
            }

            public ITEMIDLIST_OBJ[] ITEMIDLIST
            {
                get => _ITEMIDLIST;
                set => _ITEMIDLIST = value;
            }

            protected override string GetStructName() => "CIDA";

            public override Dictionary<string, string> DataDisplayReplacements()
            {
                try
                {
                    string aoffsetString = string.Join(", ", _aoffset.Select(x => x.ToString()));
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "aoffset", $"[{aoffsetString}]" },
                    };
                }
                catch
                {
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "aoffset", $"[Data Not Available]" },
                    };
                }
            }
        }

        public class ITEMIDLIST_OBJ : ClipboardFormatBase
        {
            public SHITEMID_OBJ mkid { get; set; } = new SHITEMID_OBJ();

            protected override string GetStructName() => "ITEMIDLIST";
        }

        public class SHITEMID_OBJ : ClipboardFormatBase
        {
            private USHORT _cb; // Size of the structure in bytes, including the cb field itself
            private byte[] _abID = []; // The actual data

            public uint cb
            {
                get => _cb;
                set
                {
                    _cb = (USHORT)value;
                    _abID = new byte[_cb - sizeof(USHORT)];
                }
            }
            public byte[] abID
            {
                get => _abID;
                set => _abID = value;
            }

            // Method to decode the abID into a string
            public string abIDString()
            {
                string byteString = BitConverter.ToString(_abID).Replace("-", "");
                return byteString;
            }

            public override Dictionary<string, string> DataDisplayReplacements()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "abID", abIDString() }
                };
            }

            protected override string GetStructName() => "SHITEMID";
        }

        public class METAFILE_OBJ : ClipboardFormatBase
        {
            public METAHEADER_OBJ METAHEADER { get; set; } = new METAHEADER_OBJ();
            private METARECORD_OBJ[] _METARECORD { get; set; } = []; // Last record must be a META_EOF which is 0x0000
            //private byte[] _rawRecordsData { get; set; } = new byte[0]; // Raw data of all records

            public override bool FillEmptyArrayWithRemainingBytes() => false;

            public METARECORD_OBJ[] METARECORD
            {
                get => _METARECORD;
                set => _METARECORD = value;
            }
            protected override string GetStructName() => "MS-WMF";
        }

        public class METARECORD_OBJ: ClipboardFormatBase
        {
            public DWORD rdSize { get; set; }
            public WMF_RecordType rdFunction { get; set; }
            public WORD[] rdParm { get; set; } = [];
            // ----------------------------------------------------------
            public METARECORD_OBJ(UInt32 rdSizeInput, byte[] rawBytes)
            {
                rdSize = rdSizeInput;
                rdFunction = GetFunctionValue(rawBytes);
                rdParm = new WORD[rdSizeInput - sizeof(DWORD) - sizeof(WORD)]; // Assign the remaining bytes to rdParm
            }
            // ----------------------------------------------------------
            private WMF_RecordType GetFunctionValue(byte[] rawBytes)
            {
                // Get the two bytes that are after the first DWORD
                byte[] functionBytes = new byte[2];
                Array.Copy(rawBytes, 4, functionBytes, 0, 2);
                WORD function = BitConverter.ToUInt16(functionBytes, 0);
                return (WMF_RecordType)function;
            }

            protected override string GetStructName() => "METARECORD";
            public override Dictionary<string, string> DataDisplayReplacements()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "rdParm", $"[Parameter Data]" }
                };
            }
        }

        
        public class METAHEADER_OBJ : ClipboardFormatBase
        {
            public MetaFileType mtType { get; set; }
            public WORD mtHeaderSize { get; set; }
            public WORD mtVersion { get; set; }
            public DWORD mtSize { get; set; }
            public WORD mtNoObjects { get; set; }
            public DWORD mtMaxRecord { get; set; }
            public WORD mtNoParameters { get; set; }

            protected override string GetStructName() => "METAHEADER";
        }

        public class ENHMETAFILE_OBJ : ClipboardFormatBase
        {
            public ENHMETAHEADER_OBJ ENHMETAHEADER { get; set; } = new ENHMETAHEADER_OBJ();
            public ENHMETARECORD_OBJ[] ENHMETARECORD { get; set; } = [];
            // Last record must be a EMR_EOF which is 0x0000
            protected override string GetStructName() => "MS-EMF";

            public override bool FillEmptyArrayWithRemainingBytes() => false;

            //public override Dictionary<string, string> DataDisplayReplacements()
            //{
            //    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            //    {
            //        { "ENHMETARECORD", "[Enhanced Metafile Record Data]" }
            //    };
            //}
        }


        public class ENHMETAHEADER_OBJ : ClipboardFormatBase
        {
            public EMF_RecordType iType { get; set; } // Aka RecordType, 4 bytes DWORD
            public DWORD nSize { get; set; }
            public RECTL_OBJ rclBounds { get; set; } = new RECTL_OBJ(); // 16 Bytes
            public RECTL_OBJ rclFrame { get; set; } = new RECTL_OBJ();
            public DWORD dSignature { get; set; }
            public DWORD nVersion { get; set; }
            public DWORD nBytes { get; set; }
            public DWORD nRecords { get; set; }
            public WORD nHandles { get; set; }
            public WORD sReserved { get; set; }
            public DWORD nDescription { get; set; }
            public DWORD offDescription { get; set; }
            public DWORD nPalEntries { get; set; }
            public SIZEL_OBJ szlDevice { get; set; } = new SIZEL_OBJ();
            public SIZEL_OBJ szlMillimeters { get; set; } = new SIZEL_OBJ();
            public DWORD cbPixelFormat { get; set; }
            public DWORD offPixelFormat { get; set; }
            public DWORD bOpenGL { get; set; }
            public SIZEL_OBJ szlMicrometers { get; set; } = new SIZEL_OBJ();

            protected override string GetStructName() => "ENHMETAHEADER";
        }


        public class ENHMETARECORD_OBJ : ClipboardFormatBase
        {
            public EMF_RecordType iType { get; set; } // DWORD
            public DWORD nSize { get; set; }
            public DWORD[] dParm { get; set; } = [];
            // ----------------------------------------------------------
            public ENHMETARECORD_OBJ(UInt32 nSizeInput, byte[] rawBytes)
            {
                iType = GetFunctionValue(rawBytes);
                //nSize = GetnSize(rawBytes);
                nSize = nSizeInput;
                dParm = new DWORD[nSizeInput - (sizeof(DWORD)*2)];
            }
            // ----------------------------------------------------------
            private EMF_RecordType GetFunctionValue(byte[] rawBytes)
            {
                // Get the first DWORD
                byte[] functionBytes = new byte[sizeof(DWORD)];
                Array.Copy(sourceArray: rawBytes, sourceIndex: 0, destinationArray: functionBytes, destinationIndex: 0, length: sizeof(DWORD));
                DWORD function = BitConverter.ToUInt32(functionBytes, 0);
                return (EMF_RecordType)function;
            }

            private DWORD GetnSize(byte[] rawBytes)
            {
                // Get the DWORD after the first DWORD
                byte[] sizeCountBytes = new byte[sizeof(DWORD)];
                Array.Copy(sourceArray: rawBytes, sourceIndex: sizeof(DWORD), destinationArray: sizeCountBytes, destinationIndex: 0, length: sizeof(DWORD));
                DWORD size = BitConverter.ToUInt32(sizeCountBytes, 0);
                return size;
            }

            protected override string GetStructName() => "ENHMETARECORD";
            public override Dictionary<string, string> DataDisplayReplacements()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "dParm", $"[Parameter Data]" }
                };
            }
        }

        public class RECTL_OBJ : ClipboardFormatBase // 16 bytes
        {
            public LONG left { get; set; }
            public LONG top { get; set; }
            public LONG right { get; set; }
            public LONG bottom { get; set; }

            protected override string GetStructName() => "RECTL";
        }

        public class META_PLACEABLE_OBJ : ClipboardFormatBase
        {
            public UINT32 Key { get; set; }
            public INT16 Hmf { get; set; } // Handle to metafile
            public PWMFRect16_OBJ BoundingBox { get; set; } = new PWMFRect16_OBJ();
            public INT16 Inch { get; set; }
            public UINT32 Reserved { get; set; }
            public INT16 Checksum { get; set; }

            protected override string GetStructName() => "META_PLACEABLE";
        }

        public class PWMFRect16_OBJ : ClipboardFormatBase
        {
            public INT16 Left { get; set; }
            public INT16 Top { get; set; }
            public INT16 Right { get; set; }
            public INT16 Bottom { get; set; }

            protected override string GetStructName() => "PWMFRect16";
        }

        public class DataObjectAttributes_Obj : ClipboardFormatBase
        {
            public SFGAO dwRequested { get; set; }  // Bitmask of attributes that were requested
            public SFGAO dwReceived { get; set; }   // Bitmask of actual attributes received from GetAttributesOf
            public UINT cItems { get; set; }        // Count of items in the data object

            protected override string GetStructName() => "DataObjectAttributes";
        }

        // --------------------------------------------------------------------------------------------------------------------------
        // --------------------------------------------------- Enum Definitions -----------------------------------------------------
        // --------------------------------------------------------------------------------------------------------------------------

        [StructName("LogicalColorSpace")]
        public enum LCSCSTYPE : uint // DWORD
        {
            // Can be one of the following values
            LCS_CALIBRATED_RGB = 0x00000000,
            LCS_sRGB = 0x73524742,
            LCS_WINDOWS_COLOR_SPACE = 0x57696E20
        }

        [StructName("GamutMappingIntent")]
        public enum LCSGAMUTMATCH : uint // DWORD
        {
            // Can be one of the following values
            LCS_GM_ABS_COLORIMETRIC = 0x00000008,
            LCS_GM_BUSINESS = 0x00000001,
            LCS_GM_GRAPHICS = 0x00000002,
            LCS_GM_IMAGES = 0x00000004
        }

        [StructName("bV5Compression")]
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

        [StructName("MetafileType")]
        public enum MetaFileType : WORD
        {
            MEMORYMETAFILE = 0x0001, // Metafile is stored in memory
            DISKMETAFILE = 0x0002 // Metafile is stored on disk
        }

        [StructName("RecordType (WMF)")]
        public enum WMF_RecordType: WORD
        {
            META_EOF = 0x0000,
            META_REALIZEPALETTE = 0x0035,
            META_SETPALENTRIES = 0x0037,
            META_SETBKMODE = 0x0102,
            META_SETMAPMODE = 0x0103,
            META_SETROP2 = 0x0104,
            META_SETRELABS = 0x0105,
            META_SETPOLYFILLMODE = 0x0106,
            META_SETSTRETCHBLTMODE = 0x0107,
            META_SETTEXTCHAREXTRA = 0x0108,
            META_RESTOREDC = 0x0127,
            META_RESIZEPALETTE = 0x0139,
            META_DIBCREATEPATTERNBRUSH = 0x0142,
            META_SETLAYOUT = 0x0149,
            META_SETBKCOLOR = 0x0201,
            META_SETTEXTCOLOR = 0x0209,
            META_OFFSETVIEWPORTORG = 0x0211,
            META_LINETO = 0x0213,
            META_MOVETO = 0x0214,
            META_OFFSETCLIPRGN = 0x0220,
            META_FILLREGION = 0x0228,
            META_SETMAPPERFLAGS = 0x0231,
            META_SELECTPALETTE = 0x0234,
            META_POLYGON = 0x0324,
            META_POLYLINE = 0x0325,
            META_SETTEXTJUSTIFICATION = 0x020A,
            META_SETWINDOWORG = 0x020B,
            META_SETWINDOWEXT = 0x020C,
            META_SETVIEWPORTORG = 0x020D,
            META_SETVIEWPORTEXT = 0x020E,
            META_OFFSETWINDOWORG = 0x020F,
            META_SCALEWINDOWEXT = 0x0410,
            META_SCALEVIEWPORTEXT = 0x0412,
            META_EXCLUDECLIPRECT = 0x0415,
            META_INTERSECTCLIPRECT = 0x0416,
            META_ELLIPSE = 0x0418,
            META_FLOODFILL = 0x0419,
            META_FRAMEREGION = 0x0429,
            META_ANIMATEPALETTE = 0x0436,
            META_TEXTOUT = 0x0521,
            META_POLYPOLYGON = 0x0538,
            META_EXTFLOODFILL = 0x0548,
            META_RECTANGLE = 0x041B,
            META_SETPIXEL = 0x041F,
            META_ROUNDRECT = 0x061C,
            META_PATBLT = 0x061D,
            META_SAVEDC = 0x001E,
            META_PIE = 0x081A,
            META_STRETCHBLT = 0x0B23,
            META_ESCAPE = 0x0626,
            META_INVERTREGION = 0x012A,
            META_PAINTREGION = 0x012B,
            META_SELECTCLIPREGION = 0x012C,
            META_SELECTOBJECT = 0x012D,
            META_SETTEXTALIGN = 0x012E,
            META_ARC = 0x0817,
            META_CHORD = 0x0830,
            META_BITBLT = 0x0922,
            META_EXTTEXTOUT = 0x0a32,
            META_SETDIBTODEV = 0x0d33,
            META_DIBBITBLT = 0x0940,
            META_DIBSTRETCHBLT = 0x0b41,
            META_STRETCHDIB = 0x0f43,
            META_DELETEOBJECT = 0x01f0,
            META_CREATEPALETTE = 0x00f7,
            META_CREATEPATTERNBRUSH = 0x01F9,
            META_CREATEPENINDIRECT = 0x02FA,
            META_CREATEFONTINDIRECT = 0x02FB,
            META_CREATEBRUSHINDIRECT = 0x02FC,
            META_CREATEREGION = 0x06FF
        }

        [StructName("RecordType (EMF)")]
        public enum EMF_RecordType: DWORD // 4 bytes
        {
            EMR_HEADER = 0x00000001,
            EMR_POLYBEZIER = 0x00000002,
            EMR_POLYGON = 0x00000003,
            EMR_POLYLINE = 0x00000004,
            EMR_POLYBEZIERTO = 0x00000005,
            EMR_POLYLINETO = 0x00000006,
            EMR_POLYPOLYLINE = 0x00000007,
            EMR_POLYPOLYGON = 0x00000008,
            EMR_SETWINDOWEXTEX = 0x00000009,
            EMR_SETWINDOWORGEX = 0x0000000A,
            EMR_SETVIEWPORTEXTEX = 0x0000000B,
            EMR_SETVIEWPORTORGEX = 0x0000000C,
            EMR_SETBRUSHORGEX = 0x0000000D,
            EMR_EOF = 0x0000000E,
            EMR_SETPIXELV = 0x0000000F,
            EMR_SETMAPPERFLAGS = 0x00000010,
            EMR_SETMAPMODE = 0x00000011,
            EMR_SETBKMODE = 0x00000012,
            EMR_SETPOLYFILLMODE = 0x00000013,
            EMR_SETROP2 = 0x00000014,
            EMR_SETSTRETCHBLTMODE = 0x00000015,
            EMR_SETTEXTALIGN = 0x00000016,
            EMR_SETCOLORADJUSTMENT = 0x00000017,
            EMR_SETTEXTCOLOR = 0x00000018,
            EMR_SETBKCOLOR = 0x00000019,
            EMR_OFFSETCLIPRGN = 0x0000001A,
            EMR_MOVETOEX = 0x0000001B,
            EMR_SETMETARGN = 0x0000001C,
            EMR_EXCLUDECLIPRECT = 0x0000001D,
            EMR_INTERSECTCLIPRECT = 0x0000001E,
            EMR_SCALEVIEWPORTEXTEX = 0x0000001F,
            EMR_SCALEWINDOWEXTEX = 0x00000020,
            EMR_SAVEDC = 0x00000021,
            EMR_RESTOREDC = 0x00000022,
            EMR_SETWORLDTRANSFORM = 0x00000023,
            EMR_MODIFYWORLDTRANSFORM = 0x00000024,
            EMR_SELECTOBJECT = 0x00000025,
            EMR_CREATEPEN = 0x00000026,
            EMR_CREATEBRUSHINDIRECT = 0x00000027,
            EMR_DELETEOBJECT = 0x00000028,
            EMR_ANGLEARC = 0x00000029,
            EMR_ELLIPSE = 0x0000002A,
            EMR_RECTANGLE = 0x0000002B,
            EMR_ROUNDRECT = 0x0000002C,
            EMR_ARC = 0x0000002D,
            EMR_CHORD = 0x0000002E,
            EMR_PIE = 0x0000002F,
            EMR_SELECTPALETTE = 0x00000030,
            EMR_CREATEPALETTE = 0x00000031,
            EMR_SETPALETTEENTRIES = 0x00000032,
            EMR_RESIZEPALETTE = 0x00000033,
            EMR_REALIZEPALETTE = 0x00000034,
            EMR_EXTFLOODFILL = 0x00000035,
            EMR_LINETO = 0x00000036,
            EMR_ARCTO = 0x00000037,
            EMR_POLYDRAW = 0x00000038,
            EMR_SETARCDIRECTION = 0x00000039,
            EMR_SETMITERLIMIT = 0x0000003A,
            EMR_BEGINPATH = 0x0000003B,
            EMR_ENDPATH = 0x0000003C,
            EMR_CLOSEFIGURE = 0x0000003D,
            EMR_FILLPATH = 0x0000003E,
            EMR_STROKEANDFILLPATH = 0x0000003F,
            EMR_STROKEPATH = 0x00000040,
            EMR_FLATTENPATH = 0x00000041,
            EMR_WIDENPATH = 0x00000042,
            EMR_SELECTCLIPPATH = 0x00000043,
            EMR_ABORTPATH = 0x00000044,
            EMR_COMMENT = 0x00000046,
            EMR_FILLRGN = 0x00000047,
            EMR_FRAMERGN = 0x00000048,
            EMR_INVERTRGN = 0x00000049,
            EMR_PAINTRGN = 0x0000004A,
            EMR_EXTSELECTCLIPRGN = 0x0000004B,
            EMR_BITBLT = 0x0000004C,
            EMR_STRETCHBLT = 0x0000004D,
            EMR_MASKBLT = 0x0000004E,
            EMR_PLGBLT = 0x0000004F,
            EMR_SETDIBITSTODEVICE = 0x00000050,
            EMR_STRETCHDIBITS = 0x00000051,
            EMR_EXTCREATEFONTINDIRECTW = 0x00000052,
            EMR_EXTTEXTOUTA = 0x00000053,
            EMR_EXTTEXTOUTW = 0x00000054,
            EMR_POLYBEZIER16 = 0x00000055,
            EMR_POLYGON16 = 0x00000056,
            EMR_POLYLINE16 = 0x00000057,
            EMR_POLYBEZIERTO16 = 0x00000058,
            EMR_POLYLINETO16 = 0x00000059,
            EMR_POLYPOLYLINE16 = 0x0000005A,
            EMR_POLYPOLYGON16 = 0x0000005B,
            EMR_POLYDRAW16 = 0x0000005C,
            EMR_CREATEMONOBRUSH = 0x0000005D,
            EMR_CREATEDIBPATTERNBRUSHPT = 0x0000005E,
            EMR_EXTCREATEPEN = 0x0000005F,
            EMR_POLYTEXTOUTA = 0x00000060,
            EMR_POLYTEXTOUTW = 0x00000061,
            EMR_SETICMMODE = 0x00000062,
            EMR_CREATECOLORSPACE = 0x00000063,
            EMR_SETCOLORSPACE = 0x00000064,
            EMR_DELETECOLORSPACE = 0x00000065,
            EMR_GLSRECORD = 0x00000066,
            EMR_GLSBOUNDEDRECORD = 0x00000067,
            EMR_PIXELFORMAT = 0x00000068,
            EMR_DRAWESCAPE = 0x00000069,
            EMR_EXTESCAPE = 0x0000006A,
            EMR_SMALLTEXTOUT = 0x0000006C,
            EMR_FORCEUFIMAPPING = 0x0000006D,
            EMR_NAMEDESCAPE = 0x0000006E,
            EMR_COLORCORRECTPALETTE = 0x0000006F,
            EMR_SETICMPROFILEA = 0x00000070,
            EMR_SETICMPROFILEW = 0x00000071,
            EMR_ALPHABLEND = 0x00000072,
            EMR_SETLAYOUT = 0x00000073,
            EMR_TRANSPARENTBLT = 0x00000074,
            EMR_GRADIENTFILL = 0x00000076,
            EMR_SETLINKEDUFIS = 0x00000077,
            EMR_SETTEXTJUSTIFICATION = 0x00000078,
            EMR_COLORMATCHTOTARGETW = 0x00000079,
            EMR_CREATECOLORSPACEW = 0x0000007A
        }

        [StructName("ColorUsage")]
        public enum ColorUsage : UINT
        {
            DIB_RGB_COLORS = 0x0000,
            DIB_PAL_COLORS = 0x0001,
            DIB_PAL_INDICES = 0x0002
        }

        [StructName("SFGAO")]
        [Flags] // These are a bitfield of flags
        public enum SFGAO: DWORD
        {
            SFGAO_CANCOPY = 0x00000001,
            SFGAO_CANMOVE = 0x00000002,
            SFGAO_CANLINK = 0x00000004,
            SFGAO_STORAGE = 0x00000008,
            SFGAO_CANRENAME = 0x00000010,
            SFGAO_CANDELETE = 0x00000020,
            SFGAO_HASPROPSHEET = 0x00000040,
            SFGAO_DROPTARGET = 0x00000100,
            SFGAO_CAPABILITYMASK = 0x00000177,
            SFGAO_SYSTEM = 0x00001000,
            SFGAO_ENCRYPTED = 0x00002000,
            SFGAO_ISSLOW = 0x00004000,
            SFGAO_GHOSTED = 0x00008000,
            SFGAO_LINK = 0x00010000,
            SFGAO_SHARE = 0x00020000,
            SFGAO_READONLY = 0x00040000,
            SFGAO_HIDDEN = 0x00080000,
            SFGAO_DISPLAYATTRMASK = 0x000FC000,
            SFGAO_NONENUMERATED = 0x00100000,
            SFGAO_NEWCONTENT = 0x00200000,
            //SFGAO_CANMONIKER,
            //SFGAO_HASSTORAGE,
            SFGAO_STREAM = 0x00400000,
            SFGAO_STORAGEANCESTOR = 0x00800000,
            SFGAO_VALIDATE = 0x01000000,
            SFGAO_REMOVABLE = 0x02000000,
            SFGAO_COMPRESSED = 0x04000000,
            SFGAO_BROWSABLE = 0x08000000,
            SFGAO_FILESYSANCESTOR = 0x10000000,
            SFGAO_FOLDER = 0x20000000,
            SFGAO_FILESYSTEM = 0x40000000,
            SFGAO_STORAGECAPMASK = 0x70C50008,
            SFGAO_HASSUBFOLDER = 0x80000000,
            SFGAO_CONTENTSMASK = 0x80000000,
            SFGAO_PKEYSFGAOMASK = 0x81044000,
        }

        [StructName("BOOL")]
        public enum WINDOWS_BOOL : UInt32
        {
            FALSE = 0x00000000,
            TRUE = 0x00000001
        }
        [StructName("DROPEFFECT")]
        [Flags]
        public enum DROPEFFECT : DWORD
        {
            [Description("Drop target cannot accept the data.")]
            DROPEFFECT_NONE = 0,
            [Description("Drop results in a copy.")]
            DROPEFFECT_COPY = 1,
            [Description("Drop results in a move.")]
            DROPEFFECT_MOVE = 2,
            [Description("Drop results in a link to the original data.")]
            DROPEFFECT_LINK = 4,
            [Description("Scrolling is about to start or is currently occurring in the target. This value is used in addition to the other values.")]
            DROPEFFECT_SCROLL = 0x80000000
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
            public LOGCOLORSPACEW bV5CSType;
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
        public struct _MetaFilePictHeader
        {
            public LONG mm;
            public LONG xExt;
            public LONG yExt;
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
        public struct METAHEADER
        {
            public MetaFileType mtType;
            public WORD mtHeaderSize;
            public WORD mtVersion;
            public DWORD mtSize;
            public WORD mtNoObjects;
            public DWORD mtMaxRecord;
            public WORD mtNoParameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _MetaFile
        {
            public METAHEADER mtHeader;
            public METARECORD mtRecords;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct METARECORD
        {
            public DWORD rdSize;
            public WORD rdFunction;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public WORD[] rdParm;
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

        // Shouldn't make a difference since it will be size 4 anyway, but just in case will use this with marshal sizeof
        [StructLayout(LayoutKind.Sequential)]
        public struct _LogPaletteHeader
        {
            public WORD palVersion;
            public WORD palNumEntries;
        }

        // Using class instead of struct because of the array size
        public class LOGPALETTE
        {
            public WORD palVersion;
            public WORD palNumEntries;
            public PALETTEENTRY[] palPalEntry;

            public LOGPALETTE(ushort numEntries)
            {
                palVersion = 0x300;
                palNumEntries = numEntries;
                palPalEntry = new PALETTEENTRY[numEntries];
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LOGCOLORSPACEW
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
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_PATH)]
            public WCHAR[] lcsFilename;
        }

        // --------------------------------------------------- Helper methods ---------------------------------------------------

        public const int MAX_PATH = 260;

        // Define StructName for enums to be used when looking up URLs and such
        [AttributeUsage(AttributeTargets.Enum)]
        public class StructNameAttribute : Attribute
        {
            public string Name { get; }

            public StructNameAttribute(string name)
            {
                Name = name;
            }
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

        private static object ReadValue(Type type, byte[] data, ref int offset, Type? callingClass = null, int collectionSize = -1)
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
            else if (type == typeof(Int16))
            {
                if (remainingBytes < sizeof(Int16))
                    throw new ArgumentException("Not enough data to read Int16");
                Int16 value = BitConverter.ToInt16(data, offset);
                offset += sizeof(Int16);
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

                // The 'max' string length often is a fixed size property. It means the max size that it can store, not the actual size of the string.
                int maxStringLength;

                if (collectionSize > 0)
                {
                    maxStringLength = collectionSize;
                }
                else if (Activator.CreateInstance(callingClass) is IClipboardFormat stringParentType)
                {
                    maxStringLength = stringParentType.MaxStringLength();
                }
                else
                {
                    maxStringLength = 0;
                }

                // Get the bytes for the string
                int byteLength = Math.Min(maxStringLength * 2, remainingBytes);
                byte[] stringBytes = new byte[byteLength];
                Array.Copy(data, offset, stringBytes, 0, byteLength);

                // Assuming UTF-16 Unicode encoding as standard for C# and Windows
                string value = Encoding.Unicode.GetString(stringBytes);
                int terminatorIndex = value.IndexOf('\0');

                // Only return the string up to the null terminator.
                value = terminatorIndex >= 0 ? value.Substring(0, terminatorIndex) : value;
                // Decode to UTF-8 to remove any null characters in between the string, then remove any remaining null characters
                // Probably really roundabout way to do this but whatever for now
                value = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value));
                value = value.Replace("\0", "");

                //string hexStringVersion = BitConverter.ToString(stringBytes).Replace("-", " ");

                offset += byteLength; // Still increment till the end of the allocated space
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
            else if (Activator.CreateInstance(type) is IClipboardFormat obj)
            {
                //object obj = Activator.CreateInstance(type);
                var propertiesNoProcess = obj.PropertiesNoProcess();
                var properties = obj.EnumerateProperties(getValues: false);

                foreach (var property in properties)
                {
                    string propertyName = property.Name;
                    //var _ = property.Value;
                    Type propertyType = property.Type;
                    BOOL? arraySize = property.ArraySize;

                    if (remainingBytes <= 0)
                        break;  // Stop reading if we've reached the end of the data

                    if (propertiesNoProcess.Contains(propertyName))
                        continue;  // Skip properties that are in the replacement dictionary

                    try
                    {
                        Type typeToUse = propertyType;
                        int collectionSizeToPassIn = -1;

                        if (arraySize.HasValue)
                        {
                            if (arraySize.Value > 0)
                            {
                                collectionSizeToPassIn = arraySize.Value;
                            }
                            else if (obj.FillEmptyArrayWithRemainingBytes() == true)
                            {
                                collectionSizeToPassIn = remainingBytes;
                            }
                            else
                            {
                                continue; // Skip this property if the array size is 0 and not set to fill the rest. It's probably a placeholder to add processed data later
                            }
                        }

                        object value = ReadValue(typeToUse, data, ref offset, callingClass: type, collectionSize: collectionSizeToPassIn);
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
            else if (type.IsEnum)
            {
                if (remainingBytes < Marshal.SizeOf(Enum.GetUnderlyingType(type)))
                    throw new ArgumentException("Not enough data to read enum");
                object value = Enum.ToObject(type, ReadValue(Enum.GetUnderlyingType(type), data, ref offset));
                return value;
            }
            else
            {
                throw new NotSupportedException($"Type {type} is not supported.");
            }
        }
    } // ------------------------------------------------------------------------------------------------------------------------------------

}
