using Microsoft.SqlServer.Management.HadrData;
using System;
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

} // ----------------- End of class -----------------
} // ----------------- End of namespace -----------------
