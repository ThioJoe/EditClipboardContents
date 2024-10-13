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
        public static string GetDataStringForTextbox(string formatName, byte[] data, ClipboardItem fullItem, string indent = "")
        {
            string displayText;

            if (fullItem != null && fullItem.ClipDataObject != null && fullItem.ClipDataObject.ObjectData != null)
            {
                displayText = fullItem.ClipDataObject.ObjectData.GetCacheStructObjectDisplayInfo();
            }
            else
            {
                displayText = CreateDataString(formatName, data, fullItem, indent);
                return displayText;
            }

            // At this point we know the data object exists - Check if the fullItem has the data info cached in its data object first
            if (!string.IsNullOrEmpty(displayText))
            {
                return displayText;
            }
            // Otherwise put it in the cache after generating
            else
            {
                displayText = CreateDataString(formatName, data, fullItem, indent);

                fullItem.ClipDataObject.ObjectData.SetCacheStructObjectDisplayInfo(displayText);
                return displayText;
            }
        }

        public static string CreateDataString(string formatName, byte[] data, ClipboardItem fullItem, string indent = "")
        {
            bool anyFormatInfoAvailable = false;

            StringBuilder result = new StringBuilder();
            result.AppendLine($"{indent}Format: {formatName}");

            if (FormatDescriptions.TryGetValue(formatName, out string formatDescription))
            {
                result.AppendLine($"{indent}Description: {formatDescription}");
                anyFormatInfoAvailable = true;
            }

            // Add URL Link if it exists by dictionary lookup
            if (ClipboardFormats.FormatDocsLinks.TryGetValue(formatName, out string docURL))
            {
                result.AppendLine($"{indent}Details: " + ClipboardFormats.FormatDocsLinks[formatName]);
                anyFormatInfoAvailable = true;
            }

            if (fullItem.DataInfoList.Count > 0 && !string.IsNullOrEmpty(fullItem.DataInfoList[0]))
            {
                indent = "  ";
                result.AppendLine($"\nData Info:");
                // Add each selectedItem in DataInfoList to the result indented
                foreach (string dataInfoItem in fullItem.DataInfoList)
                {
                    result.AppendLine($"{indent}{dataInfoItem}");
                }
                anyFormatInfoAvailable = true;
            }

            // If there's no full item or object data, we'll still check if there is any data info
            if (fullItem == null || fullItem.ClipDataObject == null || fullItem.ClipDataObject.ObjectData == null)
            {
                if (!anyFormatInfoAvailable)
                {
                    return $"{indent}Unknown format: {formatName}";
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
                            RecursivePrintClipDataObject(clipDataObject, indent + indent);
                        }
                        else if(item is IEnumerable nestedEnumerable)
                        {
                            RecursivePrintCollection(nestedEnumerable, indent + indent);
                        }
                        else if (item is Array nestedArray)
                        {
                            RecursivePrintArray(nestedArray, indent + indent);
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

            void RecursivePrintArray(Array array, string indent)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    object item = array.GetValue(i);
                    if (item is ClipDataObject clipDataObject)
                    {
                        result.AppendLine($"{indent}{i}:");
                        RecursivePrintClipDataObject(clipDataObject, indent + indent);
                    }
                    else if (item is IEnumerable nestedEnumerable)
                    {
                        RecursivePrintCollection(nestedEnumerable, indent + indent);
                    }
                    else if (item is Array nestedArray)
                    {
                        RecursivePrintArray(nestedArray, indent + indent);
                    }
                    else
                    {
                        result.AppendLine($"{indent}{item}");
                    }
                    result.AppendLine("");
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
                            RecursivePrintClipDataObject(nestedObject, indent + indent);
                        }
                        else if (propertyValue is IEnumerable enumerable && !(propertyValue is string))
                        {
                            result.AppendLine($"{indent}{propertyName}:");
                            RecursivePrintCollection(enumerable, indent + indent);
                        }
                        else if (propertyValue is Array array)
                        {
                            result.AppendLine($"{indent}{propertyName}:");
                            RecursivePrintArray(array, indent + indent);
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
                // Documentation links for the struct and its members
                Dictionary<string, string> structDocs = ClipboardFormats.GetDocumentationUrls_ForEntireObject(fullItem.ClipDataObject.ObjectData);
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
                        InspectStruct(field.FieldType, data, ref result, indent + indent, ref offset);
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
