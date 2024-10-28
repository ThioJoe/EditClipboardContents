using Microsoft.SqlServer.Management.HadrData;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using static EditClipboardContents.ClipboardFormats;
using System.Web;

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

namespace EditClipboardContents
{
    public static class Utils
    {
        public static bool ListOfArraysNoDuplicates(List<string[]> inputArrayList)
        {
            bool ArrayMatch(string[] x, string[] y)
            {
                if (x == null || y == null || x.Length != 2 || y.Length != 2)
                    return false;
                return x[0] == y[0] && x[1] == y[1];
            }

            for (int i = 0; i < inputArrayList.Count; i++)
            {
                for (int j = i + 1; j < inputArrayList.Count; j++)
                {
                    if (ArrayMatch(inputArrayList[i], inputArrayList[j]))
                        return false;
                }
            }

            return true;
        }
        
        public static Dictionary<uint,string>? GetAllPossibleRegisteredFormatNames()
        {
            try
            {
                // Use range 0xC000 to 0xFFFF to get all possible format names in the registered name range
                Dictionary<uint, string> allFormatNames = new Dictionary<uint, string>();
                for (uint i = 0xC000; i <= 0xFFFF; i++)
                {
                    StringBuilder formatName = new StringBuilder(256);
                    if (NativeMethods.GetClipboardFormatName(i, formatName, formatName.Capacity) != 0) // it returns 0 if it fails, so just move on
                    {
                        allFormatNames.Add(i, formatName.ToString());
                    }
                }
                return allFormatNames;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetAllPossibleRegisteredFormatNames: " + ex.Message);
                MessageBox.Show($"Something went wrong trying to fetch list of registered formats. Error: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static uint GetClipboardFormatIdFromName(string formatName, bool caseSensitive = true)
        {
            uint formatId = GetStandardFormatIdFromName(formatName);

            if (formatId != 0) // If it's a standard format, return it
            {
                return formatId;
            }

            // Get map where key is the ID and value is the name
            Dictionary<uint, string>? nameIDMap = Utils.GetAllPossibleRegisteredFormatNames();

            if (nameIDMap != null && nameIDMap.ContainsValue(formatName)) // Prefer exact match
            {
                formatId = nameIDMap.FirstOrDefault(x => x.Value == formatName).Key;
            }
            else if (nameIDMap != null && !caseSensitive) // If case insensitive, try fallback to case insensitive match if specified
            {
                formatId = nameIDMap.FirstOrDefault(x => x.Value.ToLower() == formatName.ToLower()).Key;
            }

            return formatId;
        }

        public static string GetClipboardFormatNameFromId(uint format)
        {
            // Ensure the format ID is within the range of registered clipboard formats.  The windows api method does not work on standard formats, it will just return 0.
            // See:https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerclipboardformata
            if (format > 0xFFFF || format < 0xC000)
            {
                return GetStandardFormatNameFromId(format);
            }

            // Define a sufficient buffer size
            StringBuilder formatName = new StringBuilder(256);
            int result = NativeMethods.GetClipboardFormatNameA(format, formatName, formatName.Capacity); // This will return 0 for standard formats

            if (result > 0)
            {
                return formatName.ToString();
            }
            else
            {
                return GetStandardFormatNameFromId(format);
            }
        }

        private static string GetStandardFormatNameFromId(uint format)
        {
            switch (format)
            {
                case 1: return "CF_TEXT";
                case 2: return "CF_BITMAP";
                case 3: return "CF_METAFILEPICT";
                case 4: return "CF_SYLK";
                case 5: return "CF_DIF";
                case 6: return "CF_TIFF";
                case 7: return "CF_OEMTEXT";
                case 8: return "CF_DIB";
                case 9: return "CF_PALETTE";
                case 10: return "CF_PENDATA";
                case 11: return "CF_RIFF";
                case 12: return "CF_WAVE";
                case 13: return "CF_UNICODETEXT";
                case 14: return "CF_ENHMETAFILE";
                case 15: return "CF_HDROP";
                case 16: return "CF_LOCALE";
                case 17: return "CF_DIBV5";
                case 0x0080: return "CF_OWNERDISPLAY";
                case 0x0081: return "CF_DSPTEXT";
                case 0x0082: return "CF_DSPBITMAP";
                case 0x0083: return "CF_DSPMETAFILEPICT";
                case 0x008E: return "CF_DSPENHMETAFILE";
            }

            if (format >= 0x0200 && format <= 0x02FF)
            {
                return $"CF_PRIVATEFIRST-CF_PRIVATELAST ({format:X4})";
            }

            if (format >= 0x0300 && format <= 0x03FF)
            {
                return $"CF_GDIOBJFIRST-CF_GDIOBJLAST ({format:X4})";
            }

            return $"Unknown Format ({format:X4})";
        }

        public static uint GetStandardFormatIdFromName(string formatName)
        {
            formatName = formatName.ToUpper();
            switch (formatName)
            {
                case "CF_TEXT": return 1;
                case "CF_BITMAP": return 2;
                case "CF_METAFILEPICT": return 3;
                case "CF_SYLK": return 4;
                case "CF_DIF": return 5;
                case "CF_TIFF": return 6;
                case "CF_OEMTEXT": return 7;
                case "CF_DIB": return 8;
                case "CF_PALETTE": return 9;
                case "CF_PENDATA": return 10;
                case "CF_RIFF": return 11;
                case "CF_WAVE": return 12;
                case "CF_UNICODETEXT": return 13;
                case "CF_ENHMETAFILE": return 14;
                case "CF_HDROP": return 15;
                case "CF_LOCALE": return 16;
                case "CF_DIBV5": return 17;
                case "CF_OWNERDISPLAY": return 0x0080;
                case "CF_DSPTEXT": return 0x0081;
                case "CF_DSPBITMAP": return 0x0082;
                case "CF_DSPMETAFILEPICT": return 0x0083;
                case "CF_DSPENHMETAFILE": return 0x008E;
            }
            if (formatName.StartsWith("CF_PRIVATEFIRST") || formatName.StartsWith("CF_PRIVATELAST"))
            {
                return Convert.ToUInt32(formatName.Substring(1, 4), 16);
            }
            if (formatName.StartsWith("CF_GDIOBJFIRST") || formatName.StartsWith("CF_GDIOBJLAST"))
            {
                return Convert.ToUInt32(formatName.Substring(1, 4), 16);
            }
            return 0;
        }

        public static SortableBindingList<ClipboardItem> SortSortableBindingList(SortableBindingList<ClipboardItem> inputList, string propertyName, ListSortDirection direction)
        {
            PropertyDescriptor prop = TypeDescriptor.GetProperties(typeof(ClipboardItem))[propertyName];

            if (prop != null)
            {
                ((IBindingList)inputList).ApplySort(prop, ListSortDirection.Ascending);
            }
            else
            {
                Console.WriteLine("Property not found: " + propertyName);
                throw new Exception("Property not found: " + propertyName);
            }

            return inputList; // TESTING - CHANGE THIS
        }

        public static DialogResult ShowInputDialog(Form owner, ref string input, string instructions, float scale = 1.0f)
        {
            int dpi(int value) => MainForm.CompensateDPIStatic(value); // Alias for DPI compensation
                                                                       // Base dimensions
            int baseWidth = dpi(300);
            int baseHeight = dpi(130);
            int basePadding = dpi(20);
            int baseButtonWidth = dpi(75);
            int baseButtonHeight = dpi(23);
            int baseTextBoxHeight = dpi(23);
            int baseLabelHeight = dpi(20);
            int baseTopSpacing = dpi(20);
            int baseLabelToTextBoxSpacing = dpi(10);
            int baseButtonToBottomSpacing = dpi(20);
            int baseFontSize = 10;
            // Scaled dimensions
            int scaledWidth = (int)(baseWidth * scale);
            int scaledHeight = (int)(baseHeight * scale);
            int scaledPadding = (int)(basePadding * scale);
            int scaledButtonWidth = (int)(baseButtonWidth * scale);
            int scaledButtonHeight = (int)(baseButtonHeight * scale);
            int scaledTextBoxHeight = (int)(baseTextBoxHeight * scale);
            int scaledLabelHeight = (int)(baseLabelHeight * scale);
            int scaledTopSpacing = (int)(baseTopSpacing * scale);
            int scaledLabelToTextBoxSpacing = (int)(baseLabelToTextBoxSpacing * scale);
            int scaledButtonToBottomSpacing = (int)(baseButtonToBottomSpacing * scale);
            int scaledFontSize = (int)(baseFontSize * scale);
            // Form
            Form inputBox = new Form();
            inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputBox.ClientSize = new Size(scaledWidth, scaledHeight);
            inputBox.Text = "Name";
            inputBox.StartPosition = FormStartPosition.Manual;

            // Center the inputBox relative to the owner form
            inputBox.Location = new Point(
                owner.Location.X + (owner.Width - inputBox.Width) / 2,
                owner.Location.Y + (owner.Height - inputBox.Height) / 2
            );

            // Instructions Label
            Label instructionsLabel = new Label();
            instructionsLabel.AutoSize = true;
            instructionsLabel.MaximumSize = new Size(scaledWidth - 2 * scaledPadding, 0);
            instructionsLabel.Location = new Point(scaledPadding, scaledTopSpacing);
            instructionsLabel.Text = instructions;
            instructionsLabel.Font = new Font(instructionsLabel.Font.FontFamily, scaledFontSize);
            inputBox.Controls.Add(instructionsLabel);
            // TextBox
            TextBox textBox = new TextBox();
            textBox.Size = new Size(scaledWidth - 2 * scaledPadding, scaledTextBoxHeight);
            textBox.Location = new Point(scaledPadding, instructionsLabel.Bottom + scaledLabelToTextBoxSpacing);
            textBox.Text = input;
            textBox.Font = new Font(textBox.Font.FontFamily, scaledFontSize);
            inputBox.Controls.Add(textBox);
            // Calculate button positions
            int totalButtonWidth = 2 * scaledButtonWidth + scaledPadding;
            int buttonStartX = (scaledWidth - totalButtonWidth) / 2;
            int buttonY = scaledHeight - scaledButtonHeight - scaledButtonToBottomSpacing;
            // OK Button
            Button okButton = new Button();
            okButton.DialogResult = DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new Size(scaledButtonWidth, scaledButtonHeight);
            okButton.Text = "&OK";
            okButton.Location = new Point(buttonStartX, buttonY);
            okButton.Font = new Font(okButton.Font.FontFamily, scaledFontSize);
            inputBox.Controls.Add(okButton);
            // Cancel Button
            Button cancelButton = new Button();
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(scaledButtonWidth, scaledButtonHeight);
            cancelButton.Text = "&Cancel";
            cancelButton.Location = new Point(buttonStartX + scaledButtonWidth + scaledPadding, buttonY);
            cancelButton.Font = new Font(cancelButton.Font.FontFamily, scaledFontSize);
            inputBox.Controls.Add(cancelButton);
            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            // Show the dialog as a modal dialog and return the result
            DialogResult result = inputBox.ShowDialog(owner);
            input = textBox.Text;
            return result;
        }

        public static string SanitizeFilename(string filename)
        {
            // Remove invalid characters
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"[{0}]", invalidChars);
            filename = Regex.Replace(filename, invalidReStr, "");

            // Truncate filename if it's too long
            if (filename.Length > 255)
                filename = filename.Substring(0, 255);

            return filename;
        }

        // Take in any kind of integer and return it as a proper length hex string
        public static string AsHexString(this object integerValue)
        {
            Type type = integerValue.GetType();

            if (integerValue is UIntPtr uintPtr)
            {
                if (UIntPtr.Size == 4)
                    return $"0x{uintPtr.ToUInt32():X8}";
                if (UIntPtr.Size == 8)
                    return $"0x{uintPtr.ToUInt64():X16}";
            }
            if (integerValue is IntPtr intPtr)
            {
                if (IntPtr.Size == 4)
                    return $"0x{(uint)intPtr.ToInt32():X8}";
                if (IntPtr.Size == 8)
                    return $"0x{(ulong)intPtr.ToInt64():X16}";
            }

            // Check if the integer is not zero and return it as a hex string
            if (type == typeof(byte) && (byte)integerValue != 0)
                return $"0x{(byte)integerValue:X2}";
            if (type == typeof(sbyte) && (sbyte)integerValue != 0)
                return $"0x{((byte)((sbyte)integerValue)):X2}";
            if (type == typeof(ushort) && (ushort)integerValue != 0)
                return $"0x{(ushort)integerValue:X4}";
            if (type == typeof(short) && (short)integerValue != 0)
                return $"0x{(ushort)(short)integerValue:X4}";
            if (type == typeof(uint) && (uint)integerValue != 0)
                return $"0x{(uint)integerValue:X8}";
            if (type == typeof(int) && (int)integerValue != 0)
                return $"0x{(uint)(int)integerValue:X8}";
            if (type == typeof(ulong) && (ulong)integerValue != 0)
                return $"0x{(ulong)integerValue:X16}";
            if (type == typeof(long) && (long)integerValue != 0)
                return $"0x{(ulong)(long)integerValue:X16}";

            return "";
        }

        // Builds on AsHexString to add a space and the integer value in parentheses if it's not zero. Otherwise returns nothing
        public static string AutoHexString(this object integerValue, bool truncate = false)
        {
            if (integerValue is IntPtr intPtr && intPtr == IntPtr.Zero && truncate)
            {
                return "0x0 (Null)";
            }

            if (integerValue is UIntPtr uintPtr && uintPtr == UIntPtr.Zero && truncate)
            {
                return "0x0 (Null)";
            }

            string hexValue = Utils.AsHexString(integerValue);
            if (string.IsNullOrEmpty(hexValue))
                return string.Empty;

            if (truncate && hexValue.Length > 2)  // Check if we have more than just "0x"
            {
                // Remove "0x", trim leading zeros, then add "0x" back
                string numberPart = hexValue.Substring(2).TrimStart('0');
                // If it was all zeros, keep one
                hexValue = "0x" + (numberPart.Length == 0 ? "0" : numberPart);
            }

            return $" ({hexValue})";
        }

        public static string GetWin32ErrorMessage(int? inputError)
        {
            int errorCode;
            if (inputError == null)
            {
                return "[Unknown Error]";
            }
            else
            {
                errorCode = (int)inputError;
            }

            const int FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
            const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;

            StringBuilder sb = new StringBuilder(256);
            int length = NativeMethods.FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero,
                errorCode,
                0, // Use system's current language
                sb,
                sb.Capacity,
                IntPtr.Zero
            );

            if (length == 0)
            {
                return $"Unknown error (0x{errorCode:X})";
            }

            return sb.ToString().Trim();
        }

        public static string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        public static byte[]? GetDataFromStructHandle<T>(IntPtr? inputHandle) where T : struct
        {
            if (inputHandle == IntPtr.Zero || inputHandle is not IntPtr handle)
            {
                return null;
            }

            int size = Marshal.SizeOf<T>();
            byte[] data = new byte[size];
            Marshal.Copy(handle, data, 0, size);
            return data;
        }

        public static T? GetStructFromData<T>(byte[] data) where T : struct
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            if (handle.AddrOfPinnedObject() == IntPtr.Zero)
            {
                handle.Free();
                return null;
            }

            T result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return result;
        }
        public static string FormatCLSID(CLSID_OBJ clsid)
        {
            return $"{clsid.Data1:X8}-{clsid.Data2:X4}-{clsid.Data3:X4}-{clsid.Data4[0]:X2}{clsid.Data4[1]:X2}-{clsid.Data4[2]:X2}{clsid.Data4[3]:X2}{clsid.Data4[4]:X2}{clsid.Data4[5]:X2}{clsid.Data4[6]:X2}{clsid.Data4[7]:X2}";
        }

        public static IEnumerable<string> GetExtensions(this string mimeType)
        {
            var mappingDictionaryField = typeof(MimeMapping).GetField("_mappingDictionary", BindingFlags.NonPublic | BindingFlags.Static);
            if (mappingDictionaryField == null)
                return new string[0];

            var mappingDictionary = mappingDictionaryField.GetValue(null);
            if (mappingDictionary == null)
                return new string[0];

            var dictionaryType = mappingDictionary.GetType().BaseType;
            if (dictionaryType == null)
                return new string[0];

            var mappingField = dictionaryType.GetField("_mappings", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mappingField == null)
                return new string[0];

            var mapping = mappingField.GetValue(mappingDictionary) as IDictionary<string, string>;

            // Check if the mappings are already populated
            if (mapping == null || mapping.Count == 0)
            {
                var populateMethod = dictionaryType.GetMethod("PopulateMappings", BindingFlags.NonPublic | BindingFlags.Instance);
                if (populateMethod != null)
                {
                    try
                    {
                        populateMethod.Invoke(mappingDictionary, null);
                    }
                    catch (ArgumentException)
                    {
                        // Ignore the exception if it occurs because mappings are already populated
                    }
                }
                mapping = mappingField.GetValue(mappingDictionary) as IDictionary<string, string>;
                if (mapping == null)
                    return new string[0];
            }

            var extensions = mapping.Where(x =>
                string.Equals(x.Value, mimeType, StringComparison.OrdinalIgnoreCase)) // Case insensitive comparison
                      .Select(x => x.Key);
            return extensions;
        }

        // Show the tooltip for a control if there is one
        public static void ShowToolTip(object sender, System.Windows.Forms.ToolTip toolTipTouse)
        {
            if (sender is Label label)
            {
                toolTipTouse.Show(toolTipTouse.GetToolTip(label), label);
            }
        }


    } // ----------------- End of class -----------------
} // ----------------- End of namespace -----------------
