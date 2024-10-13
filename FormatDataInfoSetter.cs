using ClipboardManager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

// My Classes
using EditClipboardItems;
using System.ComponentModel;
using static EditClipboardItems.ClipboardFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression

namespace ClipboardManager
{
    public partial class MainForm : Form
    {
        // -------------------------------------- Set Data Info ---------------------------------------------------
        private static (List<string>, byte[], ClipDataObject) SetDataInfo(string formatName, byte[] rawData)
        {
            List<string> dataInfoList = new List<string>();
            byte[] processedData = rawData;
            ClipDataObject processedObject = null;

            switch (formatName) // Process based on format name because format ID can be different for non-standard (registered) formats
            {
                case "CF_TEXT": // 1 - CF_TEXT
                                // Use Windows-1252 encoding (commonly referred to as ANSI in Windows)
                    Encoding ansiEncoding = Encoding.GetEncoding(1252);

                    // Convert bytes to string, stopping at the first null character
                    string text = "";
                    for (int i = 0; i < rawData.Length; i++)
                    {
                        if (rawData[i] == 0) break; // Stop at null terminator
                        text += (char)rawData[i];
                    }
                    processedData = ansiEncoding.GetBytes(text);
                    //-----------------------------------------
                    string ansiText = Encoding.Default.GetString(processedData);
                    dataInfoList.Add($"{ansiText.Length} Chars (ANSI)");
                    dataInfoList.Add($"Encoding: ANSI");
                    dataInfoList.Add($"Chars: {ansiText.Length}");
                    break;              

                case "CF_BITMAP": // 2 - CF_BITMAP
                    BITMAP_OBJ CF_bitmapProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.BITMAP_OBJ>(rawData);
                    
                    //dataInfoList.Add($"{CF_bitmapProcessed.bmWidth}x{CF_bitmapProcessed.bmHeight}, {CF_bitmapProcessed.bmBitsPixel} bpp");
                    using (MemoryStream ms = new MemoryStream(rawData))
                    {
                        byte[] msbytes = ms.ToArray();
                        using (Bitmap bmp = new Bitmap(ms))
                        {
                            byte[] bmpBytes = ms.ToArray();
                            // Setting the contents of the data info list explicitly instead of using Add. It could be done the other way too.
                            dataInfoList = new List<string>
                            {
                                $"{bmp.Width}x{bmp.Height}, {bmp.PixelFormat}",
                                $"Size: {bmp.Width}x{bmp.Height}",
                                $"Image Format: {bmp.PixelFormat}"
                            };
                        }

                        processedObject = new ClipDataObject
                        {
                            ObjectData = CF_bitmapProcessed
                        };

                    }
                    break;

                case "CF_METAFILEPICT": // 3
                    METAFILEPICT_OBJ metafilePictProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.METAFILEPICT_OBJ>(rawData);
                    dataInfoList.Add($"Mode: {metafilePictProcessed.mm}");

                    processedObject = new ClipDataObject
                    {
                        ObjectData = metafilePictProcessed
                    };
                    break;

                case "CF_SYLK": // 4 - CF_SYLK
                    dataInfoList.Add("Microsoft Symbolic Link format");
                    break;

                case "CF_DIF": // 5 - CF_DIF
                    dataInfoList.Add("Software Arts Data Interchange Format");
                    break;

                case "CF_TIFF": // 6 - CF_TIFF
                    dataInfoList.Add("Tagged Image File Format");
                    break;

                case "CF_DIB":   // 8  - CF_DIB
                    BITMAPINFO_OBJ bitmapProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.BITMAPINFO_OBJ>(rawData);
                    int width = bitmapProcessed.bmiHeader.biWidth;
                    int height = bitmapProcessed.bmiHeader.biHeight;
                    int bitCount = bitmapProcessed.bmiHeader.biBitCount;
                    dataInfoList.Add($"{width}x{height}, {bitCount} bpp");

                    processedObject = new ClipDataObject
                    {
                        ObjectData = bitmapProcessed
                    };

                    break;

                case "CF_PALETTE": // 9 - CF_PALETTE
                    LOGPALETTE_OBJ paletteProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.LOGPALETTE_OBJ>(rawData);
                    int paletteEntries = paletteProcessed.palNumEntries;
                    dataInfoList.Add($"{paletteEntries} Entries");
                    dataInfoList.Add($"Version: {paletteProcessed.palVersion}");
                    dataInfoList.Add($"Flags: {paletteProcessed.palPalEntry}");
                    dataInfoList.Add($"Color Count: {paletteEntries}");

                    processedObject = new ClipDataObject
                    {
                        ObjectData = paletteProcessed
                    };
                    break;

                case "CF_PENDATA": // 10 - CF_PENDATA
                    dataInfoList.Add("Windows Pen Computing data");
                    dataInfoList.Add("Data for the pen extensions to the Microsoft Windows for Pen Computing.");
                    break;

                case "CF_RIFF": // 11 - CF_RIFF
                    dataInfoList.Add("Wave format audio");
                    dataInfoList.Add("Represents audio data more complex than can be represented in a CF_WAVE standard wave format.");
                    break;

                case "CF_WAVE": // 12 - CF_WAVE
                    dataInfoList.Add("Standard wave format audio");
                    dataInfoList.Add("Represents audio data in one of the standard wave formats, such as 11 kHz or 22 kHz PCM.");
                    break;

                case "CF_UNICODETEXT": // 13 - CF_UNICODETEXT
                    string unicodeText = Encoding.Unicode.GetString(rawData);
                    int unicodeTextLength = unicodeText.Length;
                    dataInfoList.Add($"{unicodeTextLength} Chars (Unicode)");
                    dataInfoList.Add($"Encoding: Unicode (UTF-16)");
                    dataInfoList.Add($"Character Count: {unicodeTextLength}");
                    dataInfoList.Add($"Byte Count: {rawData.Length}");

                    processedData = Encoding.Unicode.GetBytes(unicodeText);
                    break;

                case "CF_HDROP": // 15 - CF_HDROP
                    {
                        GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
                        try
                        {
                            IntPtr pData = handle.AddrOfPinnedObject();

                            // Read the DROPFILES_OBJ structure
                            ClipboardFormats.DROPFILES dropFiles = Marshal.PtrToStructure<ClipboardFormats.DROPFILES>(pData);

                            // Determine if file names are Unicode
                            bool isUnicode = dropFiles.fWide != 0;
                            Encoding encodingType;
                            if (isUnicode)
                            {
                                encodingType = Encoding.Unicode;
                            }
                            else
                            {
                                encodingType = Encoding.Default;
                            }

                            // Get the offset to the file list
                            int fileListOffset = (int)dropFiles.pFiles;

                            // Read the file names from rawData starting at fileListOffset
                            List<string> fileNames = new List<string>();
                            if (fileListOffset < rawData.Length)
                            {
                                int bytesCount = rawData.Length - fileListOffset;
                                byte[] fileListBytes = new byte[bytesCount];
                                Array.Copy(rawData, fileListOffset, fileListBytes, 0, bytesCount);


                                // Convert to string
                                string fileListString = encodingType.GetString(fileListBytes);

                                // Split on null character
                                string[] files = fileListString.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                                fileNames.AddRange(files);
                            }
                            // Add the file count and file paths to dataInfoList
                            dataInfoList.Add($"File Drop: {fileNames.Count} file(s)");
                            dataInfoList.AddRange(fileNames);
                        }
                        finally
                        {
                            handle.Free();
                        }

                        DROPFILES_OBJ dropFilesProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.DROPFILES_OBJ>(rawData);
                        processedObject = new ClipDataObject
                        {
                            ObjectData = dropFilesProcessed
                        };

                        break;
                    }

                case "CF_LOCALE": // 16 - CF_LOCALE
                    string dataInfo = "Invalid CF_LOCALE data"; // Default to invalid data
                    if (rawData.Length >= 4)
                    {
                        int lcid = BitConverter.ToInt32(rawData, 0);
                        try
                        {
                            CultureInfo culture = new CultureInfo(lcid);
                            dataInfo = $"Locale: {culture.Name} (LCID: {lcid})";
                        }
                        catch (CultureNotFoundException)
                        {
                            dataInfo = $"Unknown Locale (LCID: {lcid})";
                        }
                    }
                    dataInfoList.Add(dataInfo);
                    break;

                case "CF_DIBV5": // 17 - CF_DIBV5
                    BITMAPV5HEADER_OBJ bitmapInfoV5Processed = ClipboardFormats.BytesToObject<ClipboardFormats.BITMAPV5HEADER_OBJ>(rawData);
                    dataInfoList.Add($"{bitmapInfoV5Processed.bV5Width}x{bitmapInfoV5Processed.bV5Height}, {bitmapInfoV5Processed.bV5BitCount} bpp");

                    processedObject = new ClipDataObject
                    {
                        ObjectData = bitmapInfoV5Processed
                    };

                    break;

                // ------------------- Non-Standard Clipboard Formats -------------------

                case "FileGroupDescriptorW": 
                    FILEGROUPDESCRIPTORW_OBJ fileGroupDescriptorWProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.FILEGROUPDESCRIPTORW_OBJ>(rawData);
                    int fileCount = (int)fileGroupDescriptorWProcessed.cItems;
                    dataInfoList.Add($"File Count: {fileCount}");

                    processedObject = new ClipDataObject
                    {
                        ObjectData = fileGroupDescriptorWProcessed
                    };
                    break;

                case "Shell IDList Array":
                    CIDA_OBJ cidaProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.CIDA_OBJ>(rawData);
                    int itemCount = (int)cidaProcessed.cidl;
                    dataInfoList.Add($"Item Count: {itemCount}");

                    // Using the offset location entries in the aoffset array, get the PIDLs from the rawdata
                    ITEMIDLIST_OBJ[] pidlList = new ITEMIDLIST_OBJ[itemCount];

                    int nextOffset = 0;
                    for (int i = 0; i < itemCount; i++)
                    {
                        int offset = (int)cidaProcessed.aoffset[i];
                        if (i < itemCount - 1)
                        {
                            nextOffset = (int)cidaProcessed.aoffset[i + 1];
                        }
                        else
                        {
                            nextOffset = rawData.Length;
                        }

                        if (offset < rawData.Length)
                        {
                            int length = nextOffset - offset;
                            byte[] individualPIDLBytes = new byte[length];
                            //Array.Copy(rawData, individualPIDLBytes, length);
                            Array.Copy(rawData, offset, individualPIDLBytes, 0, length);
                            ITEMIDLIST_OBJ itemIDProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.ITEMIDLIST_OBJ>(individualPIDLBytes);
                            pidlList[i] = itemIDProcessed;

                        }
                    }

                    // Add property to cidaProcessed called ITEMIDLIST with the list of ITEMIDLIST_OBJ objects
                    cidaProcessed.ITEMIDLIST = pidlList;

                    processedObject = new ClipDataObject
                    {
                        ObjectData = cidaProcessed
                    };
                    break;

                // Excel Related Formats
                case "Biff5":
                    dataInfoList.Add("Excel 5.0/95 Binary File");
                    break;

                case "Biff8":
                    dataInfoList.Add("Excel 97-2003 Binary File");
                    break;

                case "Biff12":
                    dataInfoList.Add("Excel 2007 Binary File");
                    break;

                case "HTML Format":
                    dataInfoList.Add("HTML Format");
                    break;


                // ------------------- Cloud Clipboard Formats -------------------
                // See: See: https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats
                case "ExcludeClipboardContentFromMonitorProcessing": // It says "place any data on the clipboard in this format..." -- Assuming that means it applies as long as it exists even if value is null
                    dataInfoList.Add("Disables Clipboard History & Sync");
                    dataInfoList.Add("The existance of this format in the current clipboard prevents all other formats from both being synced to the cloud or included in the clipboard history."); // (Not sure if this is accurate, but it seems likely
                    break;

                case "CanIncludeInClipboardHistory": // DWORD - Value of zero prevents all formats from being added to history, value of 1 explicitly requests all formats to be added to history
                    if (rawData != null && rawData.Length == 4)
                    {
                        if (BitConverter.ToInt32(rawData, 0) == 0)
                        {
                            dataInfoList.Add("Disables Clipboard History");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 0 to prevent the data from being added to the clipboard history.");
                        }
                        else if (BitConverter.ToInt32(rawData, 0) == 1)
                        {
                            dataInfoList.Add("Explicitly Allows Clipboard History");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 1 to explicitly request that the data be added to the clipboard history.");
                        }
                        else
                        {
                            dataInfoList.Add("Unknown value");
                            dataInfoList.Add("The value of this format should either be 1 or zero, but it is neither. There could be a new feature or a problem.");
                        }
                    }
                    else
                    {
                        dataInfoList.Add("Unknown value");
                        dataInfoList.Add("The value of this format should be a DWORD (4 bytes), but it is not. There could be a new feature or a problem.");
                    }

                    break;

                case "CanUploadToCloudClipboard": // DWORD - Value of zero prevents all formats from being synced to other devices, value of 1 explicitly requests all formats to be synced to other devices
                    if (rawData != null && rawData.Length == 4)
                    {
                        if (BitConverter.ToInt32(rawData, 0) == 0)
                        {
                            dataInfoList.Add("Disables Cloud Sync");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 0 to prevent the data from being synced to other devices.");
                        }
                        else if (BitConverter.ToInt32(rawData, 0) == 1)
                        {
                            dataInfoList.Add("Explicitly Allows Cloud Sync");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 1 to explicitly request that the data be synced to other devices.");
                        }
                        else
                        {
                            dataInfoList.Add("Unknown value");
                            dataInfoList.Add("The value of this format should either be 1 or zero, but it is neither. There could be a new feature or a problem.");
                        }
                    }
                    else
                    {
                        dataInfoList.Add("Unknown value");
                        dataInfoList.Add("The value of this format should be a DWORD (4 bytes), but it is not. There could be a new feature or a problem.");
                    }

                    break;

                // --------------- End Cloud Formats -----------------

                default:

                    if (rawData == null)
                    {
                        dataInfoList.Add("[null]");
                    }
                    else
                    {
                        dataInfoList.Add("");
                    }
                    break;

            } // End switch (formatName)

            // Add URL Link if it exists by dictionary lookup
            if (ClipboardFormats.FormatDocsLinks.TryGetValue(formatName, out string docURL))
            {
                dataInfoList.Add("Details: " + ClipboardFormats.FormatDocsLinks[formatName]);
            }

            return (dataInfoList, processedData, processedObject);

        }
    }
}
