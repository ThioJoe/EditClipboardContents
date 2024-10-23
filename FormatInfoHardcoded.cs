using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static EditClipboardContents.ClipboardFormats;
using static System.Net.WebRequestMethods;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression
// Nullable reference types
#nullable enable

namespace EditClipboardContents
{
    public static class FormatInfoHardcoded
    {

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
            { "LOGCOLORSPACEW", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-logcolorspacew" },
            { "LCSCSTYPE", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/eb4bbd50-b3ce-4917-895c-be31f214797f" },
            { "LCSGAMUTMATCH", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/9fec0834-607d-427d-abd5-ab240fb0db38" },
            { "bV5Compression", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/4e588f70-bd92-4a6f-b77f-35d0feaf7a57" },
            { "FILEDESCRIPTORW", "https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ns-shlobj_core-filedescriptorw" },
            { "FILEGROUPDESCRIPTORW", "https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ns-shlobj_core-filegroupdescriptorw" },
            { "FILETIME", "https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-filetime" },
            { "POINTL", "https://learn.microsoft.com/en-us/windows/win32/api/windef/ns-windef-pointl" },
            { "SIZEL", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/17b541c5-f8ee-4111-b1f2-012128f35871" },
            { "CLSID", "https://learn.microsoft.com/en-us/windows/win32/api/guiddef/ns-guiddef-guid" },
            { "CIDA", "https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/ns-shlobj_core-cida" },
            { "ITEMIDLIST", "https://learn.microsoft.com/en-us/windows/win32/api/shtypes/ns-shtypes-itemidlist" },
            { "SHITEMID", "https://learn.microsoft.com/en-us/windows/win32/api/shtypes/ns-shtypes-shitemid" },
            { "METAHEADER", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-metaheader" },
            { "ENHMETAHEADER", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-enhmetaheader" },
            { "METARECORD", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-metarecord" },
            { "ENHMETARECORD", "https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-enhmetarecord" },
            { "RECTL", "https://learn.microsoft.com/en-us/windows/win32/api/windef/ns-windef-rectl" },
            { "META_PLACEABLE", "https://learn.microsoft.com/en-us/windows/win32/api/gdiplusmetaheader/ns-gdiplusmetaheader-wmfplaceablefileheader" },
            { "PWMFRect16", "https://learn.microsoft.com/en-us/windows/win32/api/gdiplusmetaheader/ns-gdiplusmetaheader-pwmfrect16" },
            { "MS-WMF", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/ba5458c6-e885-41e6-b5d7-d54ef9e1065f" },
            { "MS-EMF" , "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-emf/daaf9447-0c47-446e-b72e-ac6bd7a2e8f1"},
            { "MetafileType", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/2d09c51e-062b-4d9b-94c4-6ffd0e12dfb6" },
            { "RecordType (WMF)", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/77db8158-96df-4656-a36c-3066de3d5f59" },
            { "RecordType (EMF)", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-emf/1eec80ba-799b-4784-a9ac-91597d590ae1" },
            { "SFGAO", "https://learn.microsoft.com/en-us/windows/win32/shell/sfgao" },
            { "DROPEFFECT", "https://learn.microsoft.com/en-us/windows/win32/com/dropeffect-constants" },
            { "ColorUsage", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/30403797-a408-40ca-b024-dd8a1acb39be" },
            { "LogicalColorSpace ", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/eb4bbd50-b3ce-4917-895c-be31f214797f" },
            { "GamutMappingIntent", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/9fec0834-607d-427d-abd5-ab240fb0db38" },
            { "MetafileType ", "https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wmf/2d09c51e-062b-4d9b-94c4-6ffd0e12dfb6" },
            
        };

        // Dictionary for docs to non-standard registered formats other than structs
        public static readonly Dictionary<string, string> FormatDocsLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "HTML Format", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/html-clipboard-format" },
            { "CanIncludeInClipboardHistory", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats" },
            { "CanUploadToCloudClipboard", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats" },
            { "ExcludeClipboardContentFromMonitorProcessing", "https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats" }
        };

        public static readonly Dictionary<string, string> FormatDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"CF_BITMAP", "Handle to HBITMAP"},
            {"CF_DIB", "BITMAPINFO followed by bitmap bits"},
            {"CF_DIBV5", "BITMAPV5HEADER followed by color space info and bitmap bits"},
            {"CF_DIF", "Software Arts' Data Interchange Format"},
            {"CF_DSPBITMAP", "Bitmap display data"},
            {"CF_DSPENHMETAFILE", "Enhanced metafile display data"},
            {"CF_DSPMETAFILEPICT", "Metafile picture display data"},
            {"CF_DSPTEXT", "Text display data"},
            {"CF_ENHMETAFILE", "A handle to an enhanced metafile (HENHMETAFILE)"},
            {"CF_GDIOBJFIRST", "Start of range of integers for application-defined GDI object formats"},
            {"CF_GDIOBJLAST", "End of range of integers for application-defined GDI object formats"},
            {"CF_HDROP", "HDROP (list of files)"},
            {"CF_LOCALE", "LCID (locale identifier)"},
            {"CF_METAFILEPICT", "METAFILEPICT"},
            {"CF_OEMTEXT", "Text in OEM character set"},
            {"CF_OWNERDISPLAY", "Owner-display format data"},
            {"CF_PALETTE", "Handle to HPALETTE"},
            {"CF_PENDATA", "Data for the pen extensions to the Microsoft Windows for Pen Computing."},
            {"CF_PRIVATEFIRST", "Start of range of integers for private clipboard formats"},
            {"CF_PRIVATELAST", "End of range of integers for private clipboard formats"},
            {"CF_RIFF", "Represents audio data more complex than can be represented in a CF_WAVE standard wave format."},
            {"CF_SYLK", "Microsoft Symbolic Link format (SYLK)"},
            {"CF_TEXT", "ANSI text"},
            {"CF_TIFF", "Tagged-image file format"},
            {"CF_UNICODETEXT", "Unicode text"},
            {"CF_WAVE", "Represents audio data in one of the standard wave formats, such as 11 kHz or 22 kHz PCM."},
            {"FileGroupDescriptorW", "Describes the properties of a file that is being copied."},
        };

        public static readonly Dictionary<string, string> KnownBinaryExtensionAssociations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) // Case insensitive
        {
            // Key is the formatObject name (lower case), value is the file extension 
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
            { "image/png", "png" },
            { "cf_dif", "dif" },
            { "xml spreadsheet", "xml" },
            { "text/html", "html" },
            { "html format", "html" },
            { "CF_DIBV5", "bmp" },
            { "CF_DIB", "bmp" },
            { "CF_BITMAP", "bmp" },
            { "image/x-xcf", "xcf" }
        };

        public static object? CheckIfProblematicValue(PropertyInfo property, object obj)
        {
            try
            {
                // Check if the property is indexed - Skip if it is
                if (property.GetIndexParameters().Length > 0)
                {
                    return null;
                }
                if (!property.CanRead)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting value for property {property.Name}: {ex.Message}");
                return null;
            }
            object value = property.GetValue(obj);

            return value;
        }


        // Helper function to get documentation URLs for a class and it's sub-classes using DocumentationUrl() method of each
        // Iterates them and puts them into list. Parameter is the object itself. Recursive.
        public static Dictionary<string, string> GetDocumentationUrls_ForEntireObject(IClipboardFormat? obj)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            if (obj == null)
                return results;

            // Get documentation URL for the current outer object
            string structName = obj.StructName();
            string? currentObjDocUrl = obj.GetDocumentationUrl();

            if (currentObjDocUrl != null && !string.IsNullOrEmpty(currentObjDocUrl)) // Compiler was giving warning when just using IsNullOrEmpty so added null check
            {
                results[structName] = currentObjDocUrl;
            }

            // For classes, process their properties
            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // ----------------------------- Local Function ---------------------------------------
                static object? CheckIfProblematicValue(PropertyInfo property, object obj)
                {
                    try
                    {
                        // Check if the property is indexed - Skip if it is
                        if (property.GetIndexParameters().Length > 0)
                        {
                            return null;
                        }
                        if (!property.CanRead)
                        {
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting value for property {property.Name}: {ex.Message}");
                        return null;
                    }
                    object value = property.GetValue(obj);

                    return value;
                }
                // ------------------------------------------------------------------------------------

                object? value = CheckIfProblematicValue(property, obj);

                if (value == null)
                    continue;

                // Got the value, now check if it's a class or a collection

                Type propertyType = value.GetType();
                Dictionary<string, string> propertyResults = new Dictionary<string, string>();

                if (propertyType.IsPrimitive || propertyType == typeof(string))
                {
                    continue;
                }
                else if (value is IClipboardFormat formatObject)
                {
                    propertyResults = GetDocumentationUrls_ForEntireObject(formatObject);
                }
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    if (value is IEnumerable<object> collection)
                    {
                        propertyResults = RecurseThroughCollection(collection);
                    }
                }
                else if (propertyType.IsArray)
                {
                    propertyResults = RecurseThroughArray(value);
                }
                // If it's an enum, check if we added a StructNameAttribute to it, and use that
                else if (propertyType.IsEnum) // Can add 
                {
                    // Check if it has a StructName attribute on the enum
                    //var structNameAttribute = propertyType.GetCustomAttribute<StructNameAttribute>();
                    //if (structNameAttribute != null)
                    //{
                    //    propertyResults[propertyType.Name] = structNameAttribute.StructName;
                    //}
                    continue;
                }

                // Add the results to the main dictionary. If a key already exists then don't add it
                foreach (var kvp in propertyResults)
                {
                    if (!results.ContainsKey(kvp.Key))
                    {
                        results[kvp.Key] = kvp.Value;
                    }
                }
            }
            return results;
        }

        private static Dictionary<string, string> RecurseThroughArray(object inputArray)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            Type propertyType = inputArray.GetType();

            if (inputArray == null)
                return results;

            if (propertyType.IsArray)
            {
                foreach (var item in (Array)inputArray)
                {
                    if (item == null)
                        continue;

                    Dictionary<string, string> itemResults = new Dictionary<string, string>();

                    if (item is IClipboardFormat formatObject)
                    {
                        itemResults = GetDocumentationUrls_ForEntireObject(formatObject);
                        foreach (var kvp in itemResults)
                        {
                            if (!results.ContainsKey(kvp.Key))
                                results[kvp.Key] = kvp.Value;
                        }
                    }
                    else if (item is string)
                    {
                        // Skip strings
                    }
                    else if (item is IEnumerable nestedCollection && item is not string)
                    {
                        itemResults = RecurseThroughCollection(nestedCollection);
                        foreach (var kvp in itemResults)
                        {
                            if (!results.ContainsKey(kvp.Key))
                                results[kvp.Key] = kvp.Value;
                        }
                    }
                    // If it's another array
                    else if (item.GetType().IsArray)
                    {
                        itemResults = RecurseThroughArray(item);
                        foreach (var kvp in itemResults)
                        {
                            if (!results.ContainsKey(kvp.Key))
                                results[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            return results;
        }


        private static Dictionary<string, string> RecurseThroughCollection(IEnumerable collection)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (var item in collection)
            {
                if (item == null)
                    continue;

                Dictionary<string, string> itemResults = new Dictionary<string, string>();

                if (item is IClipboardFormat formatObject)
                {
                    itemResults = GetDocumentationUrls_ForEntireObject(formatObject);
                    foreach (var kvp in itemResults)
                    {
                        if (!results.ContainsKey(kvp.Key))
                            results[kvp.Key] = kvp.Value;
                    }
                }
                else if (item is IEnumerable nestedCollection && item is not string)
                {
                    itemResults = RecurseThroughCollection(nestedCollection);
                    foreach (var kvp in itemResults)
                    {
                        if (!results.ContainsKey(kvp.Key))
                            results[kvp.Key] = kvp.Value;
                    }
                }
            }
            return results;
        }
    }
}
