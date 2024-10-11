using ClipboardManager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EditClipboardItems.ClipboardFormats;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0090 // Disable messages about New expression simplification

namespace EditClipboardItems
{
    public static class FormatInspector
    {
        public class FormatInfo
        {
            public uint Value { get; set; }
            public string Kind { get; set; }
            public string HandleOutput { get; set; }
            public Type StructType { get; set; }
        }

        private static readonly Dictionary<string, FormatInfo> FormatDictionary = new Dictionary<string, FormatInfo>
        {
        // "Kinds" of formats:
        //  - typedef: A simple typedef, like CF_TEXT or CF_BITMAP
        //  - struct: A complex structure, like CF_DIB or CF_METAFILEPICT
        //  - data: A simple data format, like CF_OEMTEXT or CF_WAVE

        {"CF_BITMAP", new FormatInfo {Value = 2, Kind = "typedef", HandleOutput = "HBITMAP"}},
        {"CF_DIB", new FormatInfo {Value = 8, Kind = "struct", HandleOutput = "BITMAPINFO followed by bitmap bits", StructType = typeof(BITMAPINFO)}},
        {"CF_DIBV5", new FormatInfo {Value = 17, Kind = "struct", HandleOutput = "BITMAPV5HEADER followed by color space info and bitmap bits", StructType = typeof(BITMAPV5HEADER)}},
        {"CF_DIF", new FormatInfo {Value = 5, Kind = "data", HandleOutput = "Software Arts' Data Interchange Format"}},
        {"CF_DSPBITMAP", new FormatInfo {Value = 0x0082, Kind = "data", HandleOutput = "Bitmap display data"}},
        {"CF_DSPENHMETAFILE", new FormatInfo {Value = 0x008E, Kind = "data", HandleOutput = "Enhanced metafile display data"}},
        {"CF_DSPMETAFILEPICT", new FormatInfo {Value = 0x0083, Kind = "data", HandleOutput = "Metafile picture display data"}},
        {"CF_DSPTEXT", new FormatInfo {Value = 0x0081, Kind = "data", HandleOutput = "Text display data"}},
        {"CF_ENHMETAFILE", new FormatInfo {Value = 14, Kind = "typedef", HandleOutput = "HENHMETAFILE"}},
        {"CF_GDIOBJFIRST", new FormatInfo {Value = 0x0300, Kind = "data", HandleOutput = "Start of range of integers for application-defined GDI object formats"}},
        {"CF_GDIOBJLAST", new FormatInfo {Value = 0x03FF, Kind = "data", HandleOutput = "End of range of integers for application-defined GDI object formats"}},
        {"CF_HDROP", new FormatInfo {Value = 15, Kind = "struct", HandleOutput = "HDROP (list of files)", StructType = typeof(DROPFILES)}},
        {"CF_LOCALE", new FormatInfo {Value = 16, Kind = "data", HandleOutput = "LCID (locale identifier)"}},
        {"CF_METAFILEPICT", new FormatInfo {Value = 3, Kind = "struct", HandleOutput = "METAFILEPICT", StructType = typeof(METAFILEPICT)}},
        {"CF_OEMTEXT", new FormatInfo {Value = 7, Kind = "data", HandleOutput = "Text in OEM character set"}},
        {"CF_OWNERDISPLAY", new FormatInfo {Value = 0x0080, Kind = "data", HandleOutput = "Owner-display format data"}},
        {"CF_PALETTE", new FormatInfo {Value = 9, Kind = "typedef", HandleOutput = "HPALETTE"}},
        {"CF_PENDATA", new FormatInfo {Value = 10, Kind = "data", HandleOutput = "Pen computing extension data"}},
        {"CF_PRIVATEFIRST", new FormatInfo {Value = 0x0200, Kind = "data", HandleOutput = "Start of range of integers for private clipboard formats"}},
        {"CF_PRIVATELAST", new FormatInfo {Value = 0x02FF, Kind = "data", HandleOutput = "End of range of integers for private clipboard formats"}},
        {"CF_RIFF", new FormatInfo {Value = 11, Kind = "data", HandleOutput = "Complex audio data, can be represented in a CF_WAVE standard wave format."}},
        {"CF_SYLK", new FormatInfo {Value = 4, Kind = "data", HandleOutput = "Microsoft Symbolic Link format (SYLK)"}},
        {"CF_TEXT", new FormatInfo {Value = 1, Kind = "data", HandleOutput = "ANSI text"}},
        {"CF_TIFF", new FormatInfo {Value = 6, Kind = "data", HandleOutput = "Tagged-image file format"}},
        {"CF_UNICODETEXT", new FormatInfo {Value = 13, Kind = "data", HandleOutput = "Unicode text"}},
        {"CF_WAVE", new FormatInfo {Value = 12, Kind = "data", HandleOutput = "Standard wave format audio data"}},
        {"FileGroupDescriptorW", new FormatInfo {Value = 49275, Kind = "struct", HandleOutput = "Describes the properties of a file that is being copied."}},
        };

        public static string CreateFormatDataStringForTextbox(string formatName, byte[] data, ClipboardItem fullItem, string indent = "")
        {
            if (!FormatDictionary.TryGetValue(formatName, out FormatInfo formatInfo))
            {
                // If there is data info, we'll show that
                if (fullItem.DataInfoList.Count > 0 && !string.IsNullOrEmpty(fullItem.DataInfoList[0]))
                {
                    indent = "    ";
                    string stringToPrint = "Data Info: ";
                    foreach (string dataInfoItem in fullItem.DataInfoList)
                    {
                        stringToPrint += $"\n{indent}• " + dataInfoItem;
                    }
                    return stringToPrint;
                }
                else
                {
                    return $"{indent}Unknown format: {formatName}";
                }
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine($"{indent}Format: {formatName}");
            result.AppendLine($"{indent}Format ID: {formatInfo.Value}");
            //result.AppendLine($"{indent}Kind: {formatInfo.Kind}");
            result.AppendLine($"{indent}Handle Output: {formatInfo.HandleOutput}");

            if (fullItem.DataInfoList.Count > 0 && !string.IsNullOrEmpty(fullItem.DataInfoList[0]))
            {
                indent = "    ";
                result.AppendLine($"\nData Info:");
                // Add each selectedItem in DataInfoList to the result indented
                foreach (string dataInfoItem in fullItem.DataInfoList)
                {
                    result.AppendLine($"{indent}  {dataInfoItem}");
                }
            }

            void RecursivePrintCollection(object obj, string indent)
            {
                if (obj is IEnumerable enumerable &&  !(obj is string)) // not a string
                {
                    int index = 1;
                    foreach (var item in enumerable)
                    {
                        if (item is ClipDataObject clipDataObject)
                        {
                            result.AppendLine($"{indent}{index}:");
                            RecursivePrintClipDataObject(clipDataObject, indent + "  ");
                        }
                        else if(item is IEnumerable nestedEnumerable)
                        {
                            RecursivePrintCollection(nestedEnumerable, indent + "  ");
                        }
                        else
                        {
                            result.AppendLine($"{indent}{item}");
                        }
                        result.AppendLine("");
                        index++;
                    }
                }
            }


            void RecursivePrintClipDataObject(ClipDataObject obj, string indent)
            {
                if (obj.ObjectData == null)
                {
                    result.AppendLine($"{indent}ObjectData is null");
                    return;
                }

                // Get the property names if there areny. If ObjectData is null, it will return null. If it's a collection, it returns an empty list
                IEnumerable<string> propertyNames = obj.PropertyNames;

                if (propertyNames.Any()) // If there are items in it
                {
                    foreach (var propertyName in propertyNames)
                    {
                        object propertyValue = obj.GetPropertyValue(propertyName);

                        if (propertyValue is ClipDataObject nestedObject)
                        {
                            result.AppendLine($"{indent}{propertyName}:");
                            RecursivePrintClipDataObject(nestedObject, indent + "    ");
                        }
                        else if (propertyValue is IEnumerable enumerable && !(propertyValue is string))
                        {
                            result.AppendLine($"{indent}{propertyName}:");
                            RecursivePrintCollection(enumerable, indent + "    ");
                        }
                        else
                        {
                            // Try to also get hex version of the value
                            if (propertyValue != null && propertyValue.GetType().IsPrimitive)
                            {
                                string hexValue = GetValueString(propertyValue, asHex: true);
                                if (!string.IsNullOrEmpty(hexValue))
                                {
                                    // Updates the property value to include hex value, otherwise it will just include the decimal value
                                    propertyValue = $"{propertyValue} ({hexValue})";
                                }
                            }
                            result.AppendLine($"{indent}{propertyName}: {propertyValue}");
                        }
                    }
                }
                // This means it's a collection if it was empty, so just go through as indexes
                else
                {
                    RecursivePrintCollection(obj.ObjectData, indent); // This will print out the collection (if it's a collection
                }
            }
                




            if (fullItem.ClipDataObject != null)
            {
                indent = "    ";

                // Documentation links for the struct and its members
                Dictionary<string, string> structDocs = ClipboardFormats.GetDocumentationUrls(fullItem.ClipDataObject.ObjectData);
                if (structDocs.Count > 0)
                {
                    result.AppendLine($"\nStruct Documentation:");
                    foreach (var doc in structDocs)
                    {
                        result.AppendLine($"{indent}{doc.Key}: {doc.Value}");
                    }
                }

                result.AppendLine($"\nStruct Info:");
                RecursivePrintClipDataObject(fullItem.ClipDataObject, indent);
            }
            else
            {
                //result.AppendLine($"\nAsDataObject is not available for this item.");
            }

            return result.ToString();
        }

        private static string GetFormattedValue(object value)
        {
            if (value == null)
                return "null";

            if (value.GetType().IsPrimitive)
            {
                string hexValue = GetValueString(value, asHex: true);
                return !string.IsNullOrEmpty(hexValue) ? $"{value} ({hexValue})" : value.ToString();
            }

            return value.ToString();
        }

        private static string GetValueString(object value, bool asHex = false)
        {
            if (value == null)
                return "null";

            if (value is IntPtr ptr)
            {
                return $"0x{ptr.ToInt64():X}";
            }

            Type valueType = value.GetType();
            if (valueType.IsValueType && !valueType.IsPrimitive && valueType != typeof(IntPtr))
            {
                // For nested structs, we'll return a placeholder
                return $"[{valueType.Name}]";
            }

            if (asHex)
            {
                if (valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(short) || valueType == typeof(byte))
                {
                    return string.Format("0x{0:X}", value); // Hexadecimal (0x1234ABCD)
                }
                else
                {
                    // Return empty string or handle other types as needed
                    return "";
                }
            }

            return value.ToString();
        }



        private static void InspectStruct(Type structType, byte[] data, ref StringBuilder result, string indent, ref int offset)
        {
            var fields = structType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                result.AppendLine($"{indent}{field.FieldType.Name} {field.Name}");

                if (offset < data.Length)
                {
                    if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && field.FieldType != typeof(IntPtr))
                    {
                        // Nested struct
                        result.AppendLine($"{indent}  Value:");
                        InspectStruct(field.FieldType, data, ref result, indent + "    ", ref offset);
                    }
                    else if (field.FieldType.IsArray)
                    {
                        // Array field (like RGBQUAD_OBJ[])
                        result.AppendLine($"{indent}  Value: [Array of {field.FieldType.GetElementType().Name}]");
                        // Note: We don't process array contents here as the length is unknown
                    }
                    else
                    {
                        object fieldValue = ReadValueFromBytes(data, ref offset, field.FieldType);
                        string valueStr = GetValueString(fieldValue);
                        result.AppendLine($"{indent}  Value: {valueStr}");

                        // Try getting hex value as well. But not if the returned string is empty
                        string valueStrHex = GetValueString(fieldValue, asHex: true);
                        if (!string.IsNullOrEmpty(valueStrHex))
                        {
                            result.AppendLine($"{indent}  Value (hex): {valueStrHex}");
                        }
                    }
                }
                else
                {
                    result.AppendLine($"{indent}  Value: [Data not available]");
                }
            }
        }


        private static object ReadValueFromBytes(byte[] data, ref int offset, Type fieldType)
        {
            if (fieldType == typeof(byte))
            {
                return data[offset++];
            }
            else if (fieldType == typeof(short) || fieldType == typeof(ushort))
            {
                var value = BitConverter.ToInt16(data, offset);
                offset += 2;
                return value;
            }
            else if (fieldType == typeof(int) || fieldType == typeof(uint))
            {
                var value = BitConverter.ToInt32(data, offset);
                offset += 4;
                return value;
            }
            else if (fieldType == typeof(long) || fieldType == typeof(ulong))
            {
                var value = BitConverter.ToInt64(data, offset);
                offset += 8;
                return value;
            }
            else if (fieldType == typeof(float))
            {
                var value = BitConverter.ToSingle(data, offset);
                offset += 4;
                return value;
            }
            else if (fieldType == typeof(double))
            {
                var value = BitConverter.ToDouble(data, offset);
                offset += 8;
                return value;
            }
            else if (fieldType == typeof(bool))
            {
                var value = BitConverter.ToBoolean(data, offset);
                offset += 1;
                return value;
            }
            else if (fieldType == typeof(char))
            {
                var value = BitConverter.ToChar(data, offset);
                offset += 2;
                return value;
            }
            else if (fieldType == typeof(IntPtr) || fieldType == typeof(UIntPtr))
            {
                var size = IntPtr.Size;
                var value = size == 4 ? BitConverter.ToInt32(data, offset) : BitConverter.ToInt64(data, offset);
                offset += size;
                return new IntPtr(value);
            }
            else
            {
                // For complex types, we'll just return the type name
                return $"[{fieldType.Name}]";
            }
        }

    }
}
