﻿using ClipboardManager;
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

                case "CF_UNICODETEXT": // 13 - CF_UNICODETEXT
                    string unicodeText = Encoding.Unicode.GetString(rawData);
                    int unicodeTextLength = unicodeText.Length;
                    dataInfoList.Add($"{unicodeTextLength} Chars (Unicode)");
                    dataInfoList.Add($"Encoding: Unicode (UTF-16)");
                    dataInfoList.Add($"Character Count: {unicodeTextLength}");
                    dataInfoList.Add($"Byte Count: {rawData.Length}");

                    processedData = Encoding.Unicode.GetBytes(unicodeText);
                    break;

                case "CF_BITMAP": // 2 - CF_BITMAP
                    using (MemoryStream ms = new MemoryStream(rawData))
                    {
                        using (Bitmap bmp = new Bitmap(ms))
                        {
                            // Setting the contents of the data info list explicitly instead of using Add. It could be done the other way too.
                            dataInfoList = new List<string>
                            {
                                $"{bmp.Width}x{bmp.Height}, {bmp.PixelFormat}",
                                $"Size: {bmp.Width}x{bmp.Height}",
                                $"Image Format: {bmp.PixelFormat}"
                            };
                        }
                    }
                    break;

                case "CF_DIB":   // 8  - CF_DIB
                    var bitmapProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.BITMAPINFO>(rawData);
                    int width = bitmapProcessed.bmiHeader.biWidth;
                    int height = bitmapProcessed.bmiHeader.biHeight;
                    int bitCount = bitmapProcessed.bmiHeader.biBitCount;
                    dataInfoList.Add($"{width}x{height}, {bitCount} bpp");

                    processedObject = new ClipDataObject
                    {
                        ObjectData = bitmapProcessed,
                        StructName = "BITMAPINFO"
                    };

                    break;

                case "CF_DIBV5": // 17 - CF_DIBV5
                    var bitmapInfoV5Processed = ClipboardFormats.BytesToObject<ClipboardFormats.BITMAPV5HEADER>(rawData);
                    dataInfoList.Add($"{bitmapInfoV5Processed.bV5Width}x{bitmapInfoV5Processed.bV5Height}, {bitmapInfoV5Processed.bV5BitCount} bpp");


                    processedObject = new ClipDataObject
                    {
                        ObjectData = bitmapInfoV5Processed,
                        StructName = "BITMAPV5HEADER"
                    };

                    break;

                case "CF_HDROP": // 15 - CF_HDROP
                    {
                        GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
                        try
                        {
                            IntPtr pData = handle.AddrOfPinnedObject();

                            // Read the DROPFILES structure
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

                        var dropFilesProcessed = ClipboardFormats.BytesToObject<ClipboardFormats.DROPFILES>(rawData);
                        processedObject = new ClipDataObject
                        {
                            ObjectData = dropFilesProcessed,
                            StructName = "DROPFILES"
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

                    dataInfoList.Add("Details: https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats");
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

                    dataInfoList.Add("Details: https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats");
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

            return (dataInfoList, processedData, processedObject);

        }
    }
}
