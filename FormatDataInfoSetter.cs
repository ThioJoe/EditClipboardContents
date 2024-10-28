using EditClipboardContents;
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
using System.ComponentModel;
using static EditClipboardContents.ClipboardFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Drawing.Imaging;

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
    public partial class MainForm : Form
    {
        // -------------------------------------- Set Data Info ---------------------------------------------------
        private static (List<string>, ViewMode, byte[], IClipboardFormat?, Enum?, FormatAnalysis) SetDataInfo(string formatName, byte[] rawData)
        {
            List<string> dataInfoList = new List<string>();
            byte[] processedData = rawData;
            IClipboardFormat? processedObject = null;
            ViewMode preferredDisplayMode = ViewMode.None;
            Enum? processedEnum = null;
            FormatAnalysis formatAnalysis = new FormatAnalysis();

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
                    preferredDisplayMode = ViewMode.Text;
                    break;

                case "CF_BITMAP": // 2 - CF_BITMAP
                    BITMAP_OBJ CF_bitmapProcessed = BytesToObject<BITMAP_OBJ>(rawData);
                    dataInfoList.Add($"{CF_bitmapProcessed.bmWidth}x{CF_bitmapProcessed.bmHeight}, {CF_bitmapProcessed.bmBitsPixel} bpp");
                    dataInfoList.Add($"Size: {CF_bitmapProcessed.bmWidth}x{CF_bitmapProcessed.bmHeight}");

                    processedObject = CF_bitmapProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;

                case "CF_METAFILEPICT": // 3
                    METAFILEPICT_OBJ metafilePictProcessed = BytesToObject<METAFILEPICT_OBJ>(rawData);

                    int metaFilePictOffset = Marshal.OffsetOf<METAFILEPICT>(nameof(METAFILEPICT.hMF)).ToInt32();
                    byte[] justMetaFileData = new byte[rawData.Length - metaFilePictOffset];
                    Array.Copy(rawData, metaFilePictOffset, justMetaFileData, 0, justMetaFileData.Length);
                    METAFILE_OBJ metaFile = BytesToObject<METAFILE_OBJ>(justMetaFileData);

                    // Get the offset where METARECORDS starts in the Metafile. (After the METAHEADER)
                    int metaRecordOffsetInMetaFile =
                        sizeof(UInt16) +  // mtType (enum is stored as WORD/UInt16)
                        sizeof(UInt16) +  // mtHeaderSize (WORD)
                        sizeof(UInt16) +  // mtVersion (WORD)
                        sizeof(UInt32) +  // mtSize (DWORD)
                        sizeof(UInt16) +  // mtNoObjects (WORD)
                        sizeof(UInt32) +  // mtMaxRecord (DWORD)
                        sizeof(UInt16);   // mtNoParameters (WORD)

                    byte[] justMetaRecordsData = new byte[justMetaFileData.Length - metaRecordOffsetInMetaFile];
                    Array.Copy(justMetaFileData, metaRecordOffsetInMetaFile, justMetaRecordsData, 0, justMetaRecordsData.Length);

                    // Now will use justMetaRecordsData to get the meta records. Will need to loop through and get the first DWORD to get the size then create the metarecord object
                    List<METARECORD_OBJ> allMetaRecords = [];
                    int recordOffset = 0;
                    while (recordOffset < justMetaRecordsData.Length)
                    {
                        // Get the DWORD of the record size at the offset
                        byte[] recordSizeBytesValueHolder = new byte[sizeof(UInt32)];
                        Array.Copy(justMetaRecordsData, recordOffset, recordSizeBytesValueHolder, 0, sizeof(UInt32));
                        UInt32 recordSizeInWords = BitConverter.ToUInt32(recordSizeBytesValueHolder, 0); // Size is in words, so multiply by 2 to get bytes
                        uint recordSizeInBytes = recordSizeInWords * 2;
                        byte[] currentRecord = new byte[recordSizeInBytes];
                        Array.Copy(justMetaRecordsData, recordOffset, currentRecord, 0, recordSizeInBytes);

                        METARECORD_OBJ recordObj = new METARECORD_OBJ((UInt32)recordSizeInBytes, currentRecord);
                        allMetaRecords.Add(recordObj);
                        recordOffset += (int)recordSizeInBytes;
                    }

                    // Convert the list to an array
                    METARECORD_OBJ[] allMetaRecordsArray = allMetaRecords.ToArray();

                    // Put in the processed nested objects
                    //metaFile.METARECORD = allMetaRecords;
                    metaFile.METARECORD = allMetaRecordsArray;
                    metafilePictProcessed.hMF = metaFile;

                    dataInfoList.Add($"{allMetaRecordsArray.Length} Records, Mode: {metafilePictProcessed.mm}");

                    processedObject = metafilePictProcessed;
                    preferredDisplayMode = ViewMode.Object;
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
                {
                    BITMAPINFO_OBJ bitmapProcessed = BytesToObject<BITMAPINFO_OBJ>(rawData);
                    int width = bitmapProcessed.bmiHeader.biWidth;
                    int height = bitmapProcessed.bmiHeader.biHeight;
                    int bitCount = bitmapProcessed.bmiHeader.biBitCount;
                    dataInfoList.Add($"{width}x{height}, {bitCount} bpp");

                    processedObject = bitmapProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;
                }

                case "CF_PALETTE": // 9 - CF_PALETTE
                {
                    LOGPALETTE_OBJ paletteProcessed = BytesToObject<LOGPALETTE_OBJ>(rawData);
                    int paletteEntries = paletteProcessed.palNumEntries;
                    dataInfoList.Add($"{paletteEntries} Palette Entries");
                    dataInfoList.Add($"Version: {Utils.AsHexString(paletteProcessed.palVersion)}");

                    processedObject = paletteProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;
                }

                case "CF_PENDATA": // 10 - CF_PENDATA
                    dataInfoList.Add("Windows Pen Computing data");
                    break;

                case "CF_RIFF": // 11 - CF_RIFF
                    dataInfoList.Add("Wave format audio");
                    preferredDisplayMode = ViewMode.Hex;
                    break;

                case "CF_WAVE": // 12 - CF_WAVE
                    dataInfoList.Add("Standard wave format audio");
                    preferredDisplayMode = ViewMode.Hex;
                    break;

                case "CF_UNICODETEXT": // 13 - CF_UNICODETEXT
                {
                    string unicodeText = Encoding.Unicode.GetString(rawData);
                    int unicodeTextLength = unicodeText.Length;
                    dataInfoList.Add($"{unicodeTextLength} Chars (Unicode)");
                    dataInfoList.Add($"Encoding: Unicode (UTF-16)");
                    dataInfoList.Add($"Character Count: {unicodeTextLength}");
                    dataInfoList.Add($"Byte Count: {rawData.Length}");

                    processedData = Encoding.Unicode.GetBytes(unicodeText);
                    preferredDisplayMode = ViewMode.Text;
                        break;
                }   

                case "CF_ENHMETAFILE": // 14
                {
                    ENHMETAFILE_OBJ enhMetafile = BytesToObject<ENHMETAFILE_OBJ>(rawData);

                    int enhMetaRecordOffset =
                        sizeof(UInt32) +     // iType (EMF_RecordType enum as DWORD)
                        sizeof(UInt32) +     // nSize (DWORD)
                        4 * sizeof(Int32) +  // rclBounds (RECTL_OBJ - 16 bytes)
                        4 * sizeof(Int32) +  // rclFrame (RECTL_OBJ - 16 bytes)
                        sizeof(UInt32) +     // dSignature (DWORD)
                        sizeof(UInt32) +     // nVersion (DWORD)
                        sizeof(UInt32) +     // nBytes (DWORD)
                        sizeof(UInt32) +     // nRecords (DWORD)
                        sizeof(UInt16) +     // nHandles (WORD)
                        sizeof(UInt16) +     // sReserved (WORD)
                        sizeof(UInt32) +     // nDescription (DWORD)
                        sizeof(UInt32) +     // offDescription (DWORD)
                        sizeof(UInt32) +     // nPalEntries (DWORD)
                        2 * sizeof(Int32) + // szlDevice (SIZEL_OBJ - 8 bytes)
                        2 * sizeof(Int32) + // szlMillimeters (SIZEL_OBJ - 8 bytes)
                        sizeof(UInt32) +     // cbPixelFormat (DWORD)
                        sizeof(UInt32) +     // offPixelFormat (DWORD)
                        sizeof(UInt32) +     // bOpenGL (DWORD)
                        2 * sizeof(Int32);  // szlMicrometers (SIZEL_OBJ - 8 bytes)

                    byte[] justEnhMetaFileData = rawData;
                    byte[] justEnhRecordsData = new byte[justEnhMetaFileData.Length - enhMetaRecordOffset];
                    Array.Copy(justEnhMetaFileData, enhMetaRecordOffset, justEnhRecordsData, 0, justEnhRecordsData.Length);

                    // Now will use justEnhRecordsData to get the records. Will need to loop through and get the 2nd DWORD to get the size then create the record object
                    List<ENHMETARECORD_OBJ> allEnhMetaRecords = [];
                    int enhRecordOffset = 0;
                    while (enhRecordOffset < justEnhRecordsData.Length)
                    {
                        // Get the DWORD of the record size at the offset
                        byte[] recordSizeBytesValueHolder = new byte[sizeof(UInt32)];
                        int localOffsetOfSize = sizeof(UInt32);
                        Array.Copy(justEnhRecordsData, (enhRecordOffset + localOffsetOfSize), recordSizeBytesValueHolder, 0, sizeof(UInt32));

                        UInt32 recordSizeInBytes = BitConverter.ToUInt32(recordSizeBytesValueHolder, 0); // For enhanced metafile, size is in bytes (not words)
                        byte[] currentRecord = new byte[recordSizeInBytes];
                        Array.Copy(justEnhRecordsData, enhRecordOffset, currentRecord, 0, recordSizeInBytes);

                        ENHMETARECORD_OBJ recordObj = new ENHMETARECORD_OBJ(recordSizeInBytes, currentRecord);
                        allEnhMetaRecords.Add(recordObj);

                        enhRecordOffset += (int)recordSizeInBytes;
                    }

                    // Convert the list to an array
                    ENHMETARECORD_OBJ[] allEnhMetaRecordsArray = allEnhMetaRecords.ToArray();
                    enhMetafile.ENHMETARECORD = allEnhMetaRecordsArray;

                    dataInfoList.Add($"{allEnhMetaRecordsArray.Length} Records");

                        processedObject = enhMetafile;
                    preferredDisplayMode = ViewMode.Object;

                    break;
                }
                case "CF_HDROP": // 15 - CF_HDROP
                {
                    GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
                    try
                    {
                        IntPtr pData = handle.AddrOfPinnedObject();

                        // Read the DROPFILES_OBJ structure
                        DROPFILES dropFiles = Marshal.PtrToStructure<DROPFILES>(pData);

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

                    DROPFILES_OBJ dropFilesProcessed = BytesToObject<DROPFILES_OBJ>(rawData);

                    processedObject = dropFilesProcessed;
                    preferredDisplayMode = ViewMode.Object;

                    break;
                }

                case "CF_LOCALE": // 16 - CF_LOCALE
                {
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
                        catch (ArgumentException)
                        {
                            dataInfo = $"Invalid LCID: {lcid}";
                        }
                        catch (Exception ex)
                        {
                            dataInfo = $"Error: {ex.Message}";
                        }
                    }
                    dataInfoList.Add(dataInfo);
                    preferredDisplayMode = ViewMode.Object;
                    break;
                }
                case "CF_DIBV5": // 17 - CF_DIBV5
                    BITMAPV5HEADER_OBJ bitmapInfoV5Processed = BytesToObject<BITMAPV5HEADER_OBJ>(rawData);
                    dataInfoList.Add($"{bitmapInfoV5Processed.bV5Width}x{bitmapInfoV5Processed.bV5Height}, {bitmapInfoV5Processed.bV5BitCount} bpp");

                    processedObject = bitmapInfoV5Processed;
                    preferredDisplayMode = ViewMode.Object;

                    break;

                // ------------------- Non-Standard Windows Shell Clipboard Formats -------------------

                case "FileGroupDescriptorW":
                    FILEGROUPDESCRIPTORW_OBJ fileGroupDescriptorWProcessed = BytesToObject<FILEGROUPDESCRIPTORW_OBJ>(rawData);
                    int fileCount = (int)fileGroupDescriptorWProcessed.cItems;
                    dataInfoList.Add($"File Count: {fileCount}");

                    processedObject = fileGroupDescriptorWProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;

                case "Autoplay Enumerated IDList Array":
                case "Shell IDList Array":
                {
                    CIDA_OBJ cidaProcessed = BytesToObject<CIDA_OBJ>(rawData);
                    int itemCount = (int)cidaProcessed.cidl;
                    dataInfoList.Add($"Item Count: {itemCount}");

                    // Using the offset location entries in the aoffset array, get the PIDLs from the rawdata
                    ITEMIDLIST_OBJ[] pidlList = new ITEMIDLIST_OBJ[itemCount];

                    int nextOffset;
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
                            ITEMIDLIST_OBJ itemIDProcessed = BytesToObject<ITEMIDLIST_OBJ>(individualPIDLBytes);
                            pidlList[i] = itemIDProcessed;

                        }
                    }
                    
                    // Add property to cidaProcessed called ITEMIDLIST with the list of ITEMIDLIST_OBJ objects
                    cidaProcessed.ITEMIDLIST = pidlList;

                    processedObject = cidaProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;
                }
                case "Shell Object Offsets":
                    POINT_OBJ ShellObjOffsetProcessed = BytesToObject<POINT_OBJ>(rawData);
                    dataInfoList.Add($"X: {ShellObjOffsetProcessed.x}, Y: {ShellObjOffsetProcessed.y}");
                    processedObject = ShellObjOffsetProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;

                case "AsyncFlag":
                    BOOL AsyncBoolVal = BytesToObject<BOOL>(rawData);
                    dataInfoList.Add($"Boolean: {AsyncBoolVal}");
                    dataInfoList.Add("Possibly has to do with telling Explorer whether to paste in the background.");
                    preferredDisplayMode = ViewMode.Object;
                    processedEnum = AsyncBoolVal;
                    break;
                
                // Formats that use BOOL enum - With no special notes
                case "UIDisplayed":
                case "DisableDragText":
                case "InShellDragLoop":
                case "IsComputingImage":
                case "IsShowingLayered":
                case "IsShowingText":
                case "UsingDefaultDragImage":  
                    BOOL UIBoolVal = BytesToObject<BOOL>(rawData);
                    dataInfoList.Add($"Boolean: {UIBoolVal}");
                    preferredDisplayMode = ViewMode.Object;
                    processedEnum = UIBoolVal;
                    break;

                case "DataObjectAttributes":
                case "DataObjectAttributesRequiringElevation":
                    DataObjectAttributes_Obj dataObjectAttributesProcessed = BytesToObject<DataObjectAttributes_Obj>(rawData);
                    processedObject = dataObjectAttributesProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;

                // Foramts that use DROPEFFECT enum
                case "Performed DropEffect":
                case "Logical Performed DropEffect":
                case "Paste Succeeded":
                case "Preferred DropEffect":
                {
                    DROPEFFECT preferredDropEffectProcessed = BytesToObject<DROPEFFECT>(rawData);
                    Dictionary<string, string> flagsDict = preferredDropEffectProcessed.GetFlagDescriptionDictionary();
                    if (flagsDict.Count > 0)
                        dataInfoList.Add($"Drop Effect: {flagsDict.Count} Flags");
                    else
                        dataInfoList.Add("No flags set");

                    processedEnum = preferredDropEffectProcessed;
                    preferredDisplayMode = ViewMode.Object;
                        break;
                }

                case "DataObject":
                    IntPtr hwndOwner = rawData.Length == 8 // Apparently the handle depends on the owner app's bitness
                        ? new IntPtr(BitConverter.ToInt64(rawData, 0))
                        : new IntPtr(BitConverter.ToInt32(rawData, 0));

                    string hexStringHandle = Utils.AutoHexString(hwndOwner, truncate: true);
                    dataInfoList.Add($"HWND: {hwndOwner}{hexStringHandle}");
                    dataInfoList.Add("This is the HWND of the window that owns the data object.");
                    if (hwndOwner == IntPtr.Zero) { dataInfoList.Add("A null handle could indicate the owning app has been closed or didn't use a DataObject."); }
                    dataInfoList.Add("See: https://learn.microsoft.com/en-us/office/vba/language/concepts/forms/what-is-the-difference-between-the-dataobject-and-the-clipboard");
                    preferredDisplayMode = ViewMode.Object;
                    break;

                case "DropDescription":
                    DROPDESCRIPTION_OBJ dropDescriptionProcessed = BytesToObject<DROPDESCRIPTION_OBJ>(rawData);
                    dataInfoList.Add($"Type: {dropDescriptionProcessed.type}");
                    processedObject = dropDescriptionProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;

                case "Link Source Descriptor":
                case "Object Descriptor":
                    OBJECTDESCRIPTOR_OBJ objectDescriptorProcessed = BytesToObject<OBJECTDESCRIPTOR_OBJ>(rawData);
                    // Convert CLSID data to string
                    string clsidString = Utils.FormatCLSID(objectDescriptorProcessed.clsid);
                    Dictionary<string, string> OLEStatus = objectDescriptorProcessed.dwStatus.GetFlagDescriptionDictionary();
                    string firstStatus = OLEStatus.Count > 0 ? OLEStatus.First().Key : "No Status Flag";

                    dataInfoList.Add($"Flag: {firstStatus}");
                    dataInfoList.Add($"Class ID (CLSID): {clsidString}");

                    processedObject = objectDescriptorProcessed;
                    preferredDisplayMode = ViewMode.Object;
                    break;

                case "Ole Private Data":
                    {
                        int headerSize = Marshal.SizeOf(typeof(_OLEPrivateDataHeader));
                        byte[] headerData = new byte[headerSize];
                        Array.Copy(rawData, headerData, headerSize);

                        _OLEPrivateDataHeader? header = Utils.GetStructFromData<_OLEPrivateDataHeader>(rawData);
                        if (header == null)
                        {
                            dataInfoList.Add("Error processing OLE Private Data Header");
                            break;
                        }
                        uint arrayCount = header?.count ?? 0;

                        // Create OLE_PRIVATE_DATA_OBJ where entries array is of size arrayCount
                        OLE_PRIVATE_DATA_OBJ oledPrivateDataSized = new OLE_PRIVATE_DATA_OBJ(arrayCount);
                        OLE_PRIVATE_DATA_OBJ oledPrivateDataProcessed = BytesToObjectInstance<OLE_PRIVATE_DATA_OBJ>(oledPrivateDataSized, rawData);

                        //if (oledPrivateDataProcessed != null)
                        //{
                        //    // Go through and process all the entries - Get ptd pointer and process the DVTARGETDEVICE object
                        //    foreach (OLE_ENTRY_OBJ entry in oledPrivateDataSized.entries)
                        //    {
                        //        if (entry.fmtetc?.ptd != IntPtr.Zero)
                        //        {
                        //            byte[]? DVTARGETDEVICE_data = Utils.GetDataFromStructHandle<DVTARGETDEVICE>(entry.fmtetc?.ptd);
                        //            if (DVTARGETDEVICE_data != null && DVTARGETDEVICE_data.Length > 0)
                        //            {
                        //                try
                        //                {
                        //                    DVTARGETDEVICE_OBJ DVTARGETDEVICE_processed = BytesToObject<DVTARGETDEVICE_OBJ>(DVTARGETDEVICE_data);
                        //                    entry.fmtetc.DVTARGETDEVICE = DVTARGETDEVICE_processed;
                        //                }
                        //                catch (Exception ex)
                        //                {
                        //                    dataInfoList.Add($"Error processing DVTARGETDEVICE: {ex.Message}");
                        //                }
                        //            }
                        //        }
                        //    }
                        //}

                        dataInfoList.Add($"{arrayCount} entries");

                        processedObject = oledPrivateDataProcessed;
                        preferredDisplayMode = ViewMode.Object;
                        break;
                    }


                //case // Whatever uses the FORMATETC struct
                //    FORMATETC ? formatEtcStructData = Utils.GetStructFromData<FORMATETC>(rawData);
                //    // Get offset of DVTARGETDEVICE
                //    int offsetToHandle = Marshal.OffsetOf<FORMATETC>(nameof(FORMATETC.ptd)).ToInt32();

                //    RedirectDataInfo? redirectDataInfo = null;
                //    if (formatEtcStructData != null)
                //    {
                //        // Get the DVTARGETDEVICE object
                //        byte[]? DVTARGETDEVICE_data = Utils.GetDataFromStructHandle<DVTARGETDEVICE>(formatEtcStructData?.ptd);

                //        if (DVTARGETDEVICE_data != null && DVTARGETDEVICE_data.Length > 0)
                //        {
                //            try
                //            {
                //                DVTARGETDEVICE_OBJ DVTARGETDEVICE_processed = BytesToObject<DVTARGETDEVICE_OBJ>(DVTARGETDEVICE_data);
                //                // Get the FORMATETC object and pass in the DVTARGETDEVICE object as a redirect when at the DVTargetDevice pointer field
                //                redirectDataInfo = new RedirectDataInfo
                //                {
                //                    OffsetToRedirect = offsetToHandle,
                //                    OriginalRedirectedType = typeof(IntPtr),
                //                    ObjectToReplaceWith = DVTARGETDEVICE_processed
                //                };
                //            }
                //            catch (Exception ex)
                //            {
                //                dataInfoList.Add($"Error processing DVTARGETDEVICE: {ex.Message}");
                //                redirectDataInfo = null;
                //            }

                //        }
                //    }

                //    FORMATETC_OBJ formatEtcProcessed = BytesToObject<FORMATETC_OBJ>(rawData, redirectInfo: redirectDataInfo);
                //    processedObject = formatEtcProcessed;

                //    break;

                // ------------------- Other Non-Standard Clipboard Formats -------------------

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

                // ---------------------

                case "HTML Format":
                    dataInfoList.Add("HTML Format");
                    preferredDisplayMode = ViewMode.Text;
                    break;

                case "PNG":
                case "image/png":
                {
                    using (var ms = new MemoryStream(rawData))
                    using (var img = Image.FromStream(ms))
                    {
                        int width = img.Width;
                        int height = img.Height;
                        dataInfoList.Add($"{width}x{height} PNG");
                    }
                    break;
                }

                // ------------------- Cloud Clipboard Formats -------------------
                // See: See: https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#cloud-clipboard-and-clipboard-history-formats
                case "ExcludeClipboardContentFromMonitorProcessing": // It says "place any data on the clipboard in this format..." -- Assuming that means it applies as long as it exists even if value is null
                    dataInfoList.Add("Disables Clipboard History & Sync");
                    dataInfoList.Add("The existance of this format in the current clipboard prevents all other formats from both being synced to the cloud or included in the clipboard history."); // (Not sure if this is accurate, but it seems likely
                    preferredDisplayMode = ViewMode.Object;
                    break;

                case "CanIncludeInClipboardHistory": // DWORD - Value of zero prevents all formats from being added to history, value of 1 explicitly requests all formats to be added to history
                    if (rawData != null && rawData.Length == 4)
                    {
                        BOOL canIncludeInClipboardHistory_bool = BytesToObject<BOOL>(rawData);
                        if (canIncludeInClipboardHistory_bool == BOOL.FALSE)
                        {
                            dataInfoList.Add("Disables Clipboard History");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 0 to prevent the data from being added to the clipboard history.");
                            processedEnum = canIncludeInClipboardHistory_bool;
                        }
                        else if (canIncludeInClipboardHistory_bool == BOOL.TRUE)
                        {
                            dataInfoList.Add("Explicitly Allows Clipboard History");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 1 to explicitly request that the data be added to the clipboard history.");
                            processedEnum = canIncludeInClipboardHistory_bool;
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
                    preferredDisplayMode = ViewMode.Object;
                    
                    break;

                case "CanUploadToCloudClipboard": // DWORD - Value of zero prevents all formats from being synced to other devices, value of 1 explicitly requests all formats to be synced to other devices
                    if (rawData != null && rawData.Length == 4)
                    {
                        BOOL canUploadToCloudClipboard_bool = BytesToObject<BOOL>(rawData);
                        if (canUploadToCloudClipboard_bool == BOOL.FALSE)
                        {
                            dataInfoList.Add("Disables Cloud Sync");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 0 to prevent the data from being synced to other devices.");
                            processedEnum = canUploadToCloudClipboard_bool;
                        }
                        else if (canUploadToCloudClipboard_bool == BOOL.TRUE)
                        {
                            dataInfoList.Add("Explicitly Allows Cloud Sync");
                            dataInfoList.Add("Applications add this format to the clipboard with a value of 1 to explicitly request that the data be synced to other devices.");
                            processedEnum = canUploadToCloudClipboard_bool;
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
                    preferredDisplayMode = ViewMode.Object;
                    break;

                // --------------- End Cloud Formats -----------------

                default:

                    if (rawData == null)
                    {
                        dataInfoList.Add(MyStrings.DataNull);
                    }
                    else
                    {
                        dataInfoList.Add("");
                    }
                    break;

            } // ------------------------------------------ End switch (formatName) ------------------------------------------

            // Do format analysis operations
            List<string> possibleExtensions = new List<string>();
            FileSignature? resultSignature = null;
            FileSignatureParser parser = new FileSignatureParser();
            IEnumerable<string> mimeExtensions = Utils.GetExtensions(formatName);

            if (FormatInfoHardcoded.KnownBinaryExtensionAssociations.TryGetValue(formatName, out string? extension))
            {
                formatAnalysis.KnownFileExtension = extension;
            }

            // Look up the format name in the file signatures and mime types
            if (rawData != null && rawData.Length > 0)
            {
                resultSignature = parser.CheckSignatureMatch(rawData);
                if (resultSignature != null)
                {
                    possibleExtensions.AddRange(resultSignature.Extensions);
                    formatAnalysis.PossibleFileExtensions = possibleExtensions;
                    formatAnalysis.FileTypeDescription = resultSignature.Description;
                }
                else if (mimeExtensions != null)
                {
                    possibleExtensions.AddRange(mimeExtensions);
                    formatAnalysis.PossibleFileExtensions = possibleExtensions;
                }
            }

            return (dataInfoList, preferredDisplayMode, processedData, processedObject, processedEnum, formatAnalysis);

        }
    }
}
