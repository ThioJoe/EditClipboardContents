using EditClipboardContents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EditClipboardContents.ClipboardFormats;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression
#pragma warning disable IDE0090 // Disable messages about New expression simplification

namespace EditClipboardContents
{
    public static class FormatStructurePrinter
    {
        public static string GetDataStringForTextbox(string formatName, ClipboardItem fullItem)
        {
            string displayText;

            if (fullItem != null && fullItem.ClipDataObject != null && fullItem.ClipDataObject.ObjectData != null)
            {
                displayText = fullItem.ClipDataObject.ObjectData.GetCacheStructObjectDisplayInfo();
            }
            else
            {
                displayText = CreateDataString(formatName, fullItem);
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
                displayText = CreateDataString(formatName, fullItem);

                fullItem.ClipDataObject.ObjectData.SetCacheStructObjectDisplayInfo(displayText);
                return displayText;
            }
        }

        public static string CreateDataString(string formatName, ClipboardItem fullItem)
        {
            bool anyFormatInfoAvailable = false;

            string indent = "   ";
            string originalIndent = indent; // Save the original indent for later, otherwise it will keep doubling in recursive functions

            StringBuilder result = new StringBuilder();
            result.AppendLine($"Format: {formatName}");

            if (FormatInfoHardcoded.FormatDescriptions.TryGetValue(formatName, out string formatDescription))
            {
                result.AppendLine($"Description: {formatDescription}");
                anyFormatInfoAvailable = true;
            }

            // Add URL Link if it exists by dictionary lookup
            if (FormatInfoHardcoded.FormatDocsLinks.TryGetValue(formatName, out string docURL))
            {
                result.AppendLine($"Details: " + FormatInfoHardcoded.FormatDocsLinks[formatName]);
                anyFormatInfoAvailable = true;
            }

            if (fullItem.DataInfoList.Count > 0 && !string.IsNullOrEmpty(fullItem.DataInfoList[0]))
            {
                
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

            // ----------------- If there is a full item and object data -----------------

            if (fullItem.ClipDataObject != null)
            {
                // Documentation links for the struct and its members
                Dictionary<string, string> structDocs = FormatInfoHardcoded.GetDocumentationUrls_ForEntireObject(fullItem.ClipDataObject.ObjectData);
                if (structDocs.Count > 0)
                {
                    result.AppendLine($"\nStruct Documentation:");
                    foreach (var doc in structDocs)
                    {
                        result.AppendLine($"{indent}{doc.Key}: {doc.Value}");
                    }
                }

                result.AppendLine($"\nStruct Info:");
                RecursivePrintClipDataObject(fullItem.ClipDataObject.ObjectData, indent);
            }

            return result.ToString();


            // -------------------- LOCAL FUNCTIONS --------------------

            void RecursivePrintClipDataObject(IClipboardFormat obj, string indent, int depth = 0)
            {
                if (obj == null || depth > 100)
                {
                    result.AppendLine($"{indent}Max depth reached or object is null");
                    return;
                }

                var replacements = obj.DataDisplayReplacements();

                foreach (var (propertyName, _, propertyType, arraySize) in obj.EnumerateProperties(getValues: false))

                {
                    if (replacements.TryGetValue(propertyName, out string replacementValue))
                    {
                        result.AppendLine($"{indent}{propertyName}: {replacementValue}");
                        continue;
                    }

                    if (typeof(IClipboardFormat).IsAssignableFrom(propertyType))
                    {
                        var nestedObj = obj.GetType().GetProperty(propertyName).GetValue(obj) as IClipboardFormat;
                        RecursivePrintClipDataObject(nestedObj, indent + originalIndent, depth + 1);
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
                    {
                        result.AppendLine($"{indent}{propertyName}: [Collection of type {propertyType.Name} with {arraySize?.ToString() ?? "unknown"} items]");
                    }
                    else if (propertyType.IsEnum)
                    {
                        result.AppendLine($"{indent}{propertyName}: {obj.GetType().GetProperty(propertyName).GetValue(obj)}");
                    }
                    else
                    {
                        // For non-collection types, we might still want to get the value
                        var value = obj.GetType().GetProperty(propertyName).GetValue(obj);
                        string valueToDisplay = GetValueString(value);
                        result.AppendLine($"{indent}{propertyName}: {valueToDisplay}");
                    }
                }
            }


            void RecursivePrintCollection(object obj, string indent)
            {
                if (obj is IEnumerable enumerable &&  obj is not string) // not a string
                {
                    int index = 1;
                    foreach (var item in enumerable)
                    {
                        if (item is IClipboardFormat formatObject)
                        {
                            result.AppendLine($"{indent}{index}:");
                            RecursivePrintClipDataObject(formatObject, indent + originalIndent);
                        }
                        else if(item is IEnumerable nestedEnumerable)
                        {
                            RecursivePrintCollection(nestedEnumerable, indent + originalIndent);
                        }
                        else if (item is Array nestedArray)
                        {
                            RecursivePrintArray(nestedArray, indent + originalIndent);
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
                    if (item is IClipboardFormat formatObject)
                    {
                        result.AppendLine($"{indent}{i}:");
                        RecursivePrintClipDataObject(formatObject, indent + indent);
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


        } // ----------------- END OF CreateDataString -----------------

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

    }
}
