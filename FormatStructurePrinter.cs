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
// Nullable reference types
#nullable enable

namespace EditClipboardContents
{
    public static class FormatStructurePrinter
    {
        public static string GetDataStringForTextbox(string formatName, ClipboardItem? fullItem)
        {
            string displayText;

            if (fullItem != null && fullItem.ClipDataObject != null)
            {
                displayText = fullItem.ClipDataObject.GetCacheStructObjectDisplayInfo();
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

                fullItem.ClipDataObject.SetCacheStructObjectDisplayInfo(displayText);
                return displayText;
            }
        }

        public static string CreateDataString(string formatName, ClipboardItem? fullItem)
        {
            bool anyFormatInfoAvailable = false;

            string indent = "   ";
            string originalIndent = indent; // Save the original indent for later, otherwise it will keep doubling in recursive functions

            StringBuilder dataInfoString = new StringBuilder();
            dataInfoString.AppendLine($"Format: {formatName}");

            if (FormatInfoHardcoded.FormatDescriptions.TryGetValue(formatName, out string formatDescription))
            {
                dataInfoString.AppendLine($"Description: {formatDescription}");
                anyFormatInfoAvailable = true;
            }

            // Add URL Link if it exists by dictionary lookup
            if (FormatInfoHardcoded.FormatDocsLinks.TryGetValue(formatName, out string docURL))
            {
                dataInfoString.AppendLine($"Details: " + FormatInfoHardcoded.FormatDocsLinks[formatName]);
                anyFormatInfoAvailable = true;
            }

            if (fullItem?.DataInfoList.Count > 0 && !string.IsNullOrEmpty(fullItem.DataInfoList[0]))
            {

                dataInfoString.AppendLine($"\nData Info:");
                // Add each selectedItem in DataInfoList to the result indented
                foreach (string dataInfoItem in fullItem.DataInfoList)
                {
                    // Replace newlines with newline plus same indent
                    dataInfoString.AppendLine($"{indent}{dataInfoItem}".Replace("\n", $"\n{indent}"));
                }
                anyFormatInfoAvailable = true;
            }

            // If there's no full item or object data, we'll still check if there is any data info
            if (fullItem == null || (fullItem.ClipDataObject == null && fullItem.ClipEnumObject == null))
            {
                if (!anyFormatInfoAvailable)
                {
                    return $"{indent}Unknown format: {formatName}";
                }
            }

            // ----------------- If there is a full item and object data -----------------

            StringBuilder structInfoString = new StringBuilder();

            if (fullItem?.ClipDataObject != null)
            {
                // Documentation links for the struct and its members
                Dictionary<string, string> structDocs = FormatInfoHardcoded.GetDocumentationUrls_ForEntireObject(fullItem.ClipDataObject);
                if (structDocs.Count > 0)
                {
                    dataInfoString.AppendLine($"\nStruct Documentation:");
                    foreach (var doc in structDocs)
                    {
                        dataInfoString.AppendLine($"{indent}{doc.Key}: {doc.Value}");
                    }
                }

                structInfoString.AppendLine($"\nStruct Info:");
                RecursivePrintClipDataObject(fullItem.ClipDataObject, indent);
            }
            else if (fullItem?.ClipEnumObject != null)
            {
                // Documentation links for the enum. In this case there will be only one
                var enumTypeStructName = fullItem.ClipEnumObject.GetType().GetStructName();
                if (enumTypeStructName != null && enumTypeStructName != "")
                {
                    if (FormatInfoHardcoded.StructDocsLinks.ContainsKey(enumTypeStructName))
                    {
                        string structDocsURL = FormatInfoHardcoded.StructDocsLinks[enumTypeStructName];
                        dataInfoString.AppendLine($"\nStruct Documentation:");
                        dataInfoString.AppendLine($"{indent}{enumTypeStructName}: {structDocsURL}");
                    }
                }
                // Print the enum values
                Dictionary<string, string> flagsDict = fullItem.ClipEnumObject.GetFlagDescriptionDictionary();
                if (flagsDict.Count > 0)
                {
                    structInfoString.AppendLine($"\nActive Enum Values/Flags:");
                    foreach (var flag in flagsDict)
                    {
                        structInfoString.AppendLine($"{indent}{flag.Key}: {flag.Value}");
                    }
                }

            }

            // Final result
            StringBuilder alignedStructInfo = TabAligner(structInfoString);

            // Add together datainfostring and alignedstructinfo
            StringBuilder finalResult = new StringBuilder();
            finalResult.AppendLine(dataInfoString.ToString());
            finalResult.AppendLine(alignedStructInfo.ToString());

            return finalResult.ToString();


            // -------------------- LOCAL FUNCTIONS --------------------

            void RecursivePrintClipDataObject(IClipboardFormat? obj, string indent, int depth = 0)
            {
                if (obj == null)
                {
                    structInfoString.AppendLine($"{indent}Max depth reached or object is null");
                    return;
                }
                if (depth > 100)
                {
                    structInfoString.AppendLine($"{indent}Max depth of 100 reached");
                    return;
                }

                var replacements = obj.DataDisplayReplacements();

                foreach (var (propertyName, _, propertyType, arraySize) in obj.EnumerateProperties(getValues: false))

                {
                    // Print replacement data if it is given
                    if (replacements.TryGetValue(propertyName, out string replacementValue))
                    {
                        structInfoString.AppendLine($"{indent}{propertyName}: {replacementValue}");
                        continue;
                    }

                    // All of these first ones are for nested objects and collections and just recurse without printing the property name (except enums)
                    if (typeof(IClipboardFormat).IsAssignableFrom(propertyType))
                    {
                        var nestedObj = obj.GetType().GetProperty(propertyName).GetValue(obj) as IClipboardFormat;
                        structInfoString.AppendLine($"{indent}{propertyName}:"); // Header label
                        RecursivePrintClipDataObject(nestedObj, indent + originalIndent, depth + 1);
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string)) // List
                    {
                        var nestedObj = obj.GetType().GetProperty(propertyName).GetValue(obj);
                        structInfoString.AppendLine($"{indent}{propertyName}:"); // Header label
                        RecursivePrintCollection(nestedObj, indent + originalIndent, depth: depth+1);
                    }
                    else if (propertyType.IsEnum)
                    {
                        structInfoString.AppendLine($"{indent}{propertyName}: {obj.GetType().GetProperty(propertyName).GetValue(obj)}");
                    }
                    else if (propertyType.IsArray) // Array
                    {
                        Array? array = obj.GetType().GetProperty(propertyName).GetValue(obj) as Array;
                        if (array != null && array.Length > 0 )
                        {
                            RecursivePrintArray(array, indent + originalIndent, depth: depth + 1);
                        }
                    }

                    // This finally ends up printing the property name and value for non-nested objects
                    else
                    {
                        // For non-collection types, we might still want to get the value
                        var value = obj.GetType().GetProperty(propertyName).GetValue(obj);
                        string valueToDisplay = GetValueString(value);
                        structInfoString.AppendLine($"{indent}{propertyName}: {valueToDisplay}");
                    }
                }
            }


            void RecursivePrintCollection(object obj, string indent, int depth)
            {
                if (obj is IEnumerable enumerable &&  obj is not string) // not a string
                {
                    int index = 1;
                    foreach (var item in enumerable)
                    {
                        if (item is IClipboardFormat formatObject)
                        {
                            structInfoString.AppendLine($"{indent}{index}:");
                            RecursivePrintClipDataObject(formatObject, indent + originalIndent, depth: depth + 1);
                        }
                        else if(item is IEnumerable nestedEnumerable)
                        {
                            RecursivePrintCollection(nestedEnumerable, indent + originalIndent, depth: depth + 1);
                        }
                        else if (item is Array nestedArray)
                        {
                            RecursivePrintArray(nestedArray, indent + originalIndent, depth: depth + 1);
                        }
                        else
                        {
                            structInfoString.AppendLine($"{indent}{item}");
                        }
                        structInfoString.AppendLine("");
                        index++;
                    }
                }
            }


            void RecursivePrintArray(Array array, string indent, int depth)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    object item = array.GetValue(i);
                    if (item is IClipboardFormat formatObject)
                    {
                        structInfoString.AppendLine($"{indent}{i}:");
                        RecursivePrintClipDataObject(formatObject, indent + originalIndent, depth: depth + 1);
                    }
                    else if (item is IEnumerable nestedEnumerable)
                    {
                        RecursivePrintCollection(nestedEnumerable, indent + originalIndent, depth: depth + 1);
                    }
                    else if (item is Array nestedArray)
                    {
                        RecursivePrintArray(nestedArray, indent + originalIndent, depth: depth + 1);
                    }
                    else
                    {
                        structInfoString.AppendLine($"{indent}{item}");
                    }
                    structInfoString.AppendLine("");
                }
            }


        } // ----------------- END OF CreateDataString -----------------

        private static string GetValueString(object value, bool hexOnly = false, bool decimalOnly = false)
        {
            if (value == null)
                return "null";

            Type valueType = value.GetType();

            // For pointers never print the decimal version
            if (valueType == typeof(IntPtr) || valueType == typeof(UIntPtr))
            {
                return Utils.AsHexString(value);
            }

            // For nested structs, we'll return a placeholder
            if (valueType.IsValueType && !valueType.IsPrimitive && valueType != typeof(IntPtr))
            {
                
                return $"[{valueType.Name}]";
            }

            if (hexOnly)
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
            else if (decimalOnly)
            {
                return value.ToString();
            }
            
            // Default to both
            string finalString = value.ToString();
            string hexString = Utils.AsHexString(value);

            if (!string.IsNullOrEmpty(hexString))
            {
                finalString = $"{finalString}\t({hexString})";
            }
            return finalString;
        }

        //// Attemps to add additional tabs where necessary so items at the same level are aligned
        //private static StringBuilder TabAligner(StringBuilder inputString)
        //{
        //    // For the inputted string builder, we can get the length of the text between the starting whitespace and the first tab which comes before the hex value we want to align
        //    // We'll do this for each line until the number of preceding spaces changes, then we know we're at a new level
        //    // When we have all the items in a group, we'll find the longest length and add tabs to the other lines such that the hex values all aign with that longest value's tabbed hex value
        //}

        private static StringBuilder TabAligner(StringBuilder inputString)
        {
            // Replace tabs with single spaces and trim endspace
            string input = inputString.ToString().Replace("\t", " ");
            input = input.TrimEnd();

            // Split input into lines
            var lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // List to store groups
            var groups = new List<Group>();

            int? currentIndentLevel = null;
            Group? currentGroup = null;

            foreach (var line in lines)
            {
                int leadingSpacesCount = line.TakeWhile(char.IsWhiteSpace).Count();
                string leadingSpaces = line.Substring(0, leadingSpacesCount);

                // Check if indentation level changes
                if (currentIndentLevel != leadingSpacesCount)
                {
                    currentIndentLevel = leadingSpacesCount;
                    currentGroup = new Group
                    {
                        Lines = new List<string>(),
                        IndentLevel = leadingSpacesCount,
                        MaxPropertyNameLength = 0,
                        MaxDecimalValueLength = 0
                    };
                    groups.Add(currentGroup);
                }

                currentGroup?.Lines.Add(line);
            }

            // Now, for each group, find max lengths
            foreach (var group in groups)
            {
                foreach (var line in group.Lines)
                {
                    string trimmedLine = line.TrimStart();

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;

                    int colonIndex = trimmedLine.IndexOf(':');

                    if (colonIndex == -1)
                    {
                        // Line doesn't have a colon, skip it
                        continue;
                    }

                    // Parse property name and rest of line
                    string propertyName = trimmedLine.Substring(0, colonIndex);
                    string rest = trimmedLine.Substring(colonIndex + 1).Trim();

                    // For value, split on '(' to separate decimal value and hex value
                    string decimalValue = rest;
                    int parenIndex = rest.IndexOf('(');
                    // If there's no hex value, we don't care about the length of the decimal value, so only do this if there is a hex value
                    if (parenIndex != -1)
                    {
                        decimalValue = rest.Substring(0, parenIndex).Trim();

                        if (decimalValue.Length > group.MaxDecimalValueLength)
                        {
                            group.MaxDecimalValueLength = decimalValue.Length;
                        }
                    }

                    // Update max lengths
                    if (propertyName.Length > group.MaxPropertyNameLength)
                    {
                        group.MaxPropertyNameLength = propertyName.Length;
                    }

                }
            }

            // Now, build the output
            var outputStringBuilder = new StringBuilder();

            foreach (var group in groups)
            {
                foreach (var line in group.Lines)
                {
                    string trimmedLine = line.TrimStart();
                    string leadingSpaces = line.Substring(0, line.Length - trimmedLine.Length);

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        outputStringBuilder.AppendLine(line);
                        continue;
                    }

                    int colonIndex = trimmedLine.IndexOf(':');

                    if (colonIndex == -1)
                    {
                        // Line doesn't have a colon, output as is
                        outputStringBuilder.AppendLine(line);
                        continue;
                    }

                    // Parse property name and rest of line
                    string propertyName = trimmedLine.Substring(0, colonIndex);
                    string rest = trimmedLine.Substring(colonIndex + 1).Trim();

                    // For value, split on '(' to separate decimal value and hex value
                    string decimalValue = rest;
                    string hexValue = "";

                    int parenIndex = rest.IndexOf('(');
                    if (parenIndex != -1)
                    {
                        decimalValue = rest.Substring(0, parenIndex).Trim();
                        hexValue = rest.Substring(parenIndex).Trim();
                    }

                    // Format the line
                    string formattedLine = $"{leadingSpaces}{propertyName.PadRight(group.MaxPropertyNameLength)}: {decimalValue.PadRight(group.MaxDecimalValueLength)}";

                    if (!string.IsNullOrEmpty(hexValue))
                    {
                        formattedLine += $" {hexValue}";
                    }

                    outputStringBuilder.AppendLine(formattedLine);
                }
            }

            return outputStringBuilder;
        }

        class Group
        {
            public int IndentLevel { get; set; }
            public List<string> Lines { get; set; } = new List<string>();
            public int MaxPropertyNameLength { get; set; }
            public int MaxDecimalValueLength { get; set; }
        }


    } // ----------------- END OF FormatStructurePrinter -----------------
}
