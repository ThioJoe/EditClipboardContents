using EditClipboardItems;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dataGridViewClipboard.MouseWheel += dataGridViewClipboard_MouseWheel;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RefreshClipboardItems();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Don't change tool locations if the window is minimized
            if (this.WindowState != FormWindowState.Minimized)
            {
                UpdateToolLocations();
            }
        }

        private void dataGridViewClipboard_MouseWheel(object sender, MouseEventArgs e)
        {
            if (((HandledMouseEventArgs)e).Handled == true)
            {
                return;
            }

            // Determine direction: -1 for up, 1 for down
            int direction = e.Delta > 0 ? -1 : 1;

            // Get current selected row index
            int currentIndex = dataGridViewClipboard.CurrentCell?.RowIndex ?? -1;

            if (currentIndex != -1)
            {
                // Calculate new index
                int newIndex = currentIndex + direction;

                // Ensure new index is within bounds
                int rowCount = dataGridViewClipboard.Rows.Count;
                if (newIndex < 0)
                {
                    newIndex = 0;
                }
                else if (newIndex >= rowCount)
                {
                    newIndex = rowCount - 1;
                }

                // If the index has changed, update selection
                if (newIndex != currentIndex)
                {
                    dataGridViewClipboard.ClearSelection();
                    dataGridViewClipboard.Rows[newIndex].Selected = true;
                    dataGridViewClipboard.CurrentCell = dataGridViewClipboard.Rows[newIndex].Cells[0];

                    // Ensure the selected row is visible
                    dataGridViewClipboard.FirstDisplayedScrollingRowIndex = newIndex;
                }

                ChangeCellFocus(newIndex);

            }

            // Mark as handled because the event might get fired multiple times per scroll
            ((HandledMouseEventArgs)e).Handled = true;
        }

        private void menuHelp_About_Click(object sender, EventArgs e)
        {
            // Show message box
            MessageBox.Show("Edit Clipboard Contents\n\n" +
                "Version: " + versionString + "\n\n" +
                "Author: ThioJoe" +
                "   (https://github.com/ThioJoe)",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Open the link in the default browser
        private void richTextBoxContents_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                try
                {
                    System.Diagnostics.Process.Start(e.LinkText);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Show tooltip near the cursor when clicked without Ctrl
                Point cursorPos = richTextBoxContents.PointToClient(Cursor.Position);
                toolTip1.Show("Ctrl + Click To Open Link", richTextBoxContents, cursorPos.X + 10, cursorPos.Y + 10, 2000);
            }
        }

        private void dataGridViewClipboard_KeyDown(object sender, KeyEventArgs e)
        {
            // If the user presses Ctrl+C, copy the selected rows to the clipboard
            if (e.Control && e.KeyCode == Keys.C)
            {
                e.Handled = true;  // Prevents the default copy operation
                copyTableRows(copyEntireTable: null); // Null means entire table will be copied if no rows are selected, otherwise just selected rows
            }
        }

        private void copyRowDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyTableRows(copyEntireTable: false);
        }

        private void copyCellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Get the contents of the selected cell
            string cellContents = dataGridViewClipboard.CurrentCell.Value.ToString();
            // Copy the cell contents to the clipboard
            Clipboard.SetText(cellContents);
        }

        private void copySelectedRowsNoHeaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyTableRows(copyEntireTable: false, forceNoHeader: true);
        }

        private void dataGridViewClipboard_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                bool isClickedRowSelected = false;

                // Check if the clicked row is part of the current selection
                foreach (DataGridViewRow row in dataGridViewClipboard.SelectedRows)
                {
                    if (row.Index == e.RowIndex)
                    {
                        isClickedRowSelected = true;
                        break;
                    }
                }

                // If the clicked row is not part of the current selection, clear the selection and re-set the clicked row as the only selected row
                if (!isClickedRowSelected)
                {
                    dataGridViewClipboard.ClearSelection();
                    dataGridViewClipboard.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
                    // Change the cell focus
                    ChangeCellFocus(rowIndex: e.RowIndex, cellIndex: e.ColumnIndex);
                }
            }
        }

        private void contextMenuStrip_dataGridView_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void splitterContainer_InnerTextBoxes_SplitterMoved(object sender, SplitterEventArgs e)
        {
            UpdateToolLocations();
        }

        private void dropdownHexToTextEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePlaintextFromHexView();
        }

        private void richTextBoxContents_TextChanged(object sender, EventArgs e)
        {
            // Only update if in edit mode
            if (dropdownContentsViewMode.SelectedIndex == 2)
            {
                UpdatePlaintextFromHexView();
            }

        }

        private void richTextBox_HexPlaintext_TextChanged(object sender, EventArgs e)
        {
            // Only bother if in edit mode
            if (dropdownContentsViewMode.SelectedIndex == 2)
            {
                UpdateHexViewChanges();

            }
        }

        private void checkBoxPlainTextEditing_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePlaintextFromHexView();
        }

        private void richTextBoxContents_SelectionChanged(object sender, EventArgs e)
        {
            // Get the length of the selection
            int selectionLength = richTextBoxContents.SelectionLength;
            if (selectionLength == 0) // Probably just a click, not even a selection
            {
                return;
            }

            if (dropdownContentsViewMode.SelectedIndex == 2 || dropdownContentsViewMode.SelectedIndex == 1)
            {
                //RoundSelection();
                SyncHexToPlaintext();
            }
        }


        private void richTextBox_HexPlaintext_SelectionChanged(object sender, EventArgs e)
        {
            // Get the length of the selection
            int selectionLength = richTextBox_HexPlaintext.SelectionLength;
            if (selectionLength == 0) // Probably just a click, not even a selection
            {
                return;
            }

            if (dropdownContentsViewMode.SelectedIndex == 2 || dropdownContentsViewMode.SelectedIndex == 1)
            {
                SyncPlaintextToHex();
            }
        }

        private void menuOptions_IncludeRowHeaders_Click(object sender, EventArgs e)
        {
            // Toggle the check based on the current state
            menuOptions_IncludeRowHeaders.Checked = !menuOptions_IncludeRowHeaders.Checked;
        }

        // ---------------------- Table Copy Formatting Options ----------------------
        private void menuOptions_TabSeparation_Click(object sender, EventArgs e)
        {
            // Use pattern matchin to get the text of the clicked item and pass it in to the function automatically
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem.Text);
            }
        }

        private void menuOptions_CommaSeparation_Click(object sender, EventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem.Text);
            }
        }

        private void menuOptions_PreFormatted_Click(object sender, EventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem.Text);
            }
        }

        private void menuItemShowLargeHex_Click(object sender, EventArgs e)
        {
            // Toggle the check based on the current state
            menuOptions_ShowLargeHex.Checked = !menuOptions_ShowLargeHex.Checked;
        }

        // Give focus to control when mouse enters
        private void dataGridViewClipboard_MouseEnter(object sender, EventArgs e)
        {
            dataGridViewClipboard.Focus();
        }

        private void buttonResetEdit_Click(object sender, EventArgs e)
        {
            // Get the original item's data and apply it to the edited item
            UpdateEditedClipboardItem((int)GetSelectedClipboardItemObject().FormatId, GetSelectedClipboardItemObject().RawData, setPending: false);

            // Check if any edited items have pending changes, and update the pending changes label if necessary
            hasPendingChanges = editedClipboardItems.Any(i => i.HasPendingEdit);

            // Update the view
            DisplayClipboardData(GetSelectedClipboardItemObject());
            UpdateEditControlsVisibility();

        }

        private void dataGridViewClipboard_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                // Assume focus of the first selected row if multiple are selected
                ChangeCellFocus(dataGridViewClipboard.SelectedRows[0].Index);

                // Enable menu buttons that require a selectedItem
                menuEdit_CopySelectedRows.Enabled = true;
                menuFile_ExportSelectedAsRawHex.Enabled = true;
                menuFile_ExportSelectedStruct.Enabled = true;
                menuFile_ExportSelectedAsFile.Enabled = true;
            }
            else
            {
                richTextBoxContents.Clear();

                // Disable menu buttons that require a selectedItem
                menuEdit_CopySelectedRows.Enabled = false;
                menuFile_ExportSelectedAsRawHex.Enabled = false;
                menuFile_ExportSelectedStruct.Enabled = false;
                menuFile_ExportSelectedAsFile.Enabled = false;
            }

            // If the auto selection checkbox is checked, decide which view mode to use based on item data
            if (checkBoxAutoViewMode.Checked)
            {
                // Get the selectedItem object
                ClipboardItem item = GetSelectedClipboardItemObject();

                // If there 
            }

        }

        private void menuEdit_CopyHexAsText_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToCopy = GetSelectedClipboardItemObject();
            if (itemToCopy == null)
            {
                return;
            }
            // Get the hex information that would be displayed in the hex view
            string data = BitConverter.ToString(itemToCopy.RawData).Replace("-", " ");

            // Copy the hex information to the clipboard
            Clipboard.SetText(data);
        }

        private void menuEdit_CopyObjectInfoAsText_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToCopy = GetSelectedClipboardItemObject();
            if (itemToCopy == null)
            {
                return;
            }
            // Get the struct / object info that would be displayed in object view of rich text box and copy it to clipboard
            string data = FormatInspector.CreateFormatDataStringForTextbox(formatName: GetClipboardFormatName(itemToCopy.FormatId), data: itemToCopy.RawData, fullItem: itemToCopy);
            Clipboard.SetText(data);
        }

        private void menuEdit_CopyEditedHexAsText_Click(object sender, EventArgs e)
        {
            // Get the edited clipboard selectedItem and its info
            ClipboardItem itemToCopy = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
            if (itemToCopy == null)
            {
                return;
            }

            // Get the hex information that would be displayed in the hex view and copy it to clipboard
            string data = BitConverter.ToString(itemToCopy.RawData).Replace("-", " ");
            Clipboard.SetText(data);
        }

        private void menuEdit_CopySelectedRows_Click(object sender, EventArgs e)
        {
            // If no rows are selected, do nothing
            if (dataGridViewClipboard.SelectedRows.Count == 0)
            {
                return;
            }
            copyTableRows(copyEntireTable: false);
        }

        private void menuEdit_CopyEntireTable_Click(object sender, EventArgs e)
        {
            copyTableRows(copyEntireTable: true);
        }

        // Converts the hex string in the hex view to a byte array and updates the clipboard selectedItem in editedClipboardItems
        private void buttonApplyEdit_Click(object sender, EventArgs e)
        {
            // Get the hex string from the hex view
            string hexString = Regex.Replace(richTextBoxContents.Text, @"\s", "");

            // Ensure valid number of characters and the text is valid Hex
            if (hexString.Length % 2 != 0)
            {
                MessageBox.Show($"Invalid hex data. There must be an even number of hex characters (spaces and whitespace are ignored).\n\nInput length was: {hexString.Length}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check for invalid characters
            Match invalidMatch = Regex.Match(hexString, @"[^0-9a-fA-F]");
            if (invalidMatch.Success)
            {
                string invalidChars = string.Join(", ", hexString.Where(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))).Distinct());
                MessageBox.Show($"Invalid hex data. Please ensure the text box only contains valid hex characters (0-9, A-F).\n\nInvalid characters found: {invalidChars}\n\n(Spaces and whitespace are automatically ignored)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] rawDataFromTextbox = Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
            // Get the format ID of the selected clipboard selectedItem
            int formatId = (int)GetSelectedClipboardItemObject().FormatId;

            // Check if the edited data is actually different from the original data, apply the change and set hasPendingChanges accordingly
            if (!GetSelectedClipboardItemObject().RawData.SequenceEqual(rawDataFromTextbox))
            {
                UpdateEditedClipboardItem(formatId, rawDataFromTextbox);
                hasPendingChanges = true;
            }
            else
            {
                // Don't change hasPendingChanges to false because there might be other items with pending changes
            }

            UpdateEditControlsVisibility();
        }

        private void toolStripButtonSaveEdited_Click(object sender, EventArgs e)
        {
            SaveClipboardData(formatsToExclude: null);
            RefreshClipboardItems();
            hasPendingChanges = false;
            UpdateEditControlsVisibility();
        }

        private void menuFile_ExportSelectedAsRawHex_Click(object sender, EventArgs e)
        {
            ClipboardItem itemToExport = GetSelectedClipboardItemObject();
            if (itemToExport == null)
            {
                return;
            }

            string nameStem = itemToExport.FormatName + "_RawHex";
            SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "txt", defaultFileNameStem: nameStem);
            if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                // Get the hex information
                string data = BitConverter.ToString(itemToExport.RawData).Replace("-", " ");
                // Save the data to a file
                File.WriteAllText(saveFileDialogResult.FileName, data);
            }
        }

        private void menuFile_ExportSelectedStruct_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToExport = GetSelectedClipboardItemObject();
            if (itemToExport == null)
            {
                return;
            }
            string nameStem = itemToExport.FormatName + "_StructInfo";
            SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "txt", defaultFileNameStem: nameStem);
            if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                // Get the hex information
                string data = FormatInspector.CreateFormatDataStringForTextbox(formatName: GetClipboardFormatName(itemToExport.FormatId), data: itemToExport.RawData, fullItem: itemToExport);
                // TO DO - Export details of each object in the struct

                // Save the data to a file
                File.WriteAllText(saveFileDialogResult.FileName, data);
            }
        }

        private void menuFile_ExportSelectedAsFile_Click(object sender, EventArgs e)
        {
            toolStripButtonExportSelected_Click(null, null);
        }

        private void toolStripButtonExportSelected_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem itemToExport = GetSelectedClipboardItemObject();

            if (itemToExport == null)
            {
                return;
            }
            string nameStem = itemToExport.FormatName;

            // If it's a DIBV5 format, convert it to a bitmap
            if (itemToExport.FormatId == 17)
            {
                Bitmap bitmap = FormatHandleTranslators.BitmapFile_From_CF_DIBV5_RawData(itemToExport.RawData);

                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);
                    return;
                }
            }
            else if (itemToExport.FormatId == 8) // CF_DIB
            {
                Bitmap bitmap = FormatHandleTranslators.BitmapFile_From_CF_DIB_RawData(itemToExport.RawData);
                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);
                    return;
                }
            }
            else if (itemToExport.FormatId == 2) // CF_BITMAP
            {
                SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "bmp", defaultFileNameStem: nameStem);
                if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
                {
                    using (MemoryStream ms = new MemoryStream(itemToExport.RawData))
                    {
                        using (Bitmap bitmap = new Bitmap(ms))
                        {
                            bitmap.Save(saveFileDialogResult.FileName, ImageFormat.Bmp);
                            return;
                        }
                    }
                }
            }

            string[] knownFormatExtensions = new string[] { "PNG" };
            string fileExt = "dat"; // Default extension if not in the list of known formats

            // Just export the raw data as a file. If it's in the list of known formats where the raw data is the actual file data, and the extension matches the format name, use that extension
            if (knownFormatExtensions.Contains(nameStem.ToUpper()))
            {
                fileExt = nameStem;
                nameStem = "Clipboard";
            }

            SaveFileDialog saveRawFileDialogResult = SaveFileDialog(extension: fileExt, defaultFileNameStem: nameStem);
            if (saveRawFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(saveRawFileDialogResult.FileName, itemToExport.RawData);
            }
        }

        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            int selectedFormatId = -1;
            // New scope, only need item for this operation
            {
                ClipboardItem item = GetSelectedClipboardItemObject();
                if (item != null)
                {
                    selectedFormatId = (int)item.FormatId;
                }
            }

            RefreshClipboardItems();
            hasPendingChanges = false;

            // If the new clipboard data contains the same format as the previously selected item, re-select it
            if (selectedFormatId > 0 && clipboardItems != null && clipboardItems.Any(ci => ci.FormatId == selectedFormatId))
            {
                // If format id is still in the new clipboard, select it
                int rowIndex = dataGridViewClipboard.Rows.Cast<DataGridViewRow>().ToList().FindIndex(r => r.Cells["FormatId"].Value.ToString() == selectedFormatId.ToString());
                if (rowIndex >= 0)
                {
                    dataGridViewClipboard.Rows[rowIndex].Selected = true;
                    dataGridViewClipboard.FirstDisplayedScrollingRowIndex = rowIndex;
                }
            }
            UpdateEditControlsVisibility();
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                // Make a list of selected row format IDs
                List<uint> selectedFormatIds = new List<uint>();
                foreach (DataGridViewRow selectedRow in dataGridViewClipboard.SelectedRows)
                {
                    if (uint.TryParse(selectedRow.Cells["FormatId"].Value.ToString(), out uint formatIdToRemove))
                    {
                        selectedFormatIds.Add(formatIdToRemove);
                    }
                }

                if (selectedFormatIds.Count == 0)
                {
                    MessageBox.Show("No valid format IDs selected.");
                    return;
                }

                //if (RemoveClipboardFormat(formatsToExclude: selectedFormatIds))
                if (SaveClipboardData(formatsToExclude: selectedFormatIds))
                {
                    //MessageBox.Show($"Format {formatIdToRemove} removed successfully.");
                }
                else
                {
                    MessageBox.Show($"Failed to remove format {selectedFormatIds}.");
                }
                RefreshClipboardItems();
            }
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // Resize processedData grid view to fit the form window
            UpdateToolLocations();
        }

        private void dropdownContentsViewMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Indexes:
            // 0: Text
            // 1: Hex
            // 2: Hex (Editable)
            // 3: Object / Struct View

            // Show buttons and labels for edited mode
            UpdateEditControlsVisibility();

            // For object view mode and text view mode, enable auto highlighting URLs in the text box
            if (dropdownContentsViewMode.SelectedIndex == 0 || dropdownContentsViewMode.SelectedIndex == 3)
            {
                richTextBoxContents.DetectUrls = true;
            }
            else
            {
                richTextBoxContents.DetectUrls = false;
            }

            ClipboardItem item = GetSelectedClipboardItemObject();
            if (item == null)
            {
                return;
            }

            DisplayClipboardData(item);
        }

        private void dataGridViewClipboard_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            ChangeCellFocus(e.RowIndex);
            UpdateEditControlsVisibility();
        }
    }
}
