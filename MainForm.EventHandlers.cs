using EditClipboardContents;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static EditClipboardContents.ClipboardFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about collection initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression
#pragma warning disable IDE0017
// Nullable reference types
#nullable enable

namespace EditClipboardContents
{
    public partial class MainForm : Form
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dataGridViewClipboard.MouseWheel += dataGridViewClipboard_MouseWheel;
        }

        // Form has finished loading and is about to be displayed, but not yet visible
        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }

        // Form is now visible
        private void MainForm_Shown(object sender, EventArgs e)
        {
            // Use BeginInvoke to ensure the form is fully rendered
            // Don't put anything outside of BeginInvoke that requires the form to be fully rendered, it will actually run first
            this.BeginInvoke(new Action(() =>
            {
                //ShowLoadingIndicator(true);
                RefreshClipboardItems();
                //ShowLoadingIndicator(false);
                //UpdateSplitterPosition_FitDataGrid(); // Occurs in RefreshClipboardItems
            }));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (isResizing) return; // Prevent re-entry
            isResizing = true;

            try
            {
                // Your existing code...
                WhichPanelResize splitAnchor;
                int maxSize = (int)Math.Round((decimal)splitContainerMain.Height * (decimal)0.6);

                DataGridView dgv = dataGridViewClipboard;
                int cellsTotalHeight = dgv.Rows.GetRowsHeight(DataGridViewElementStates.Visible);// + dgv.ColumnHeadersHeight + dgv.Rows.GetRowCount(DataGridViewElementStates.Visible);

                if ((dataGridViewClipboard.DisplayedRowCount(includePartialRow: false)) >= dataGridViewClipboard.Rows.Count && cellsTotalHeight <= maxSize)
                {
                    splitAnchor = WhichPanelResize.Bottom;
                }
                else
                {
                    splitAnchor = WhichPanelResize.Top;
                }

                if (this.WindowState != FormWindowState.Minimized)
                {
                    UpdateToolLocations(splitAnchor: splitAnchor);
                }

                previousWindowHeight = this.Height;
                previousSplitterDistance = splitContainerMain.SplitterDistance;
            }
            finally
            {
                isResizing = false;
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
                    SelectRowByRowIndex(newIndex);
                }
            }

            // Mark as handled because the event might get fired multiple times per scroll
            ((HandledMouseEventArgs)e).Handled = true;
        }

        private void SelectRowByRowIndex(int newIndex, int focusedCellIndex = -1)
        {
            if (newIndex >= dataGridViewClipboard.Rows.Count)
            {
                return;
            }

            // Use the currently focused cell index if none is provided, or default to zero if there is no focused cell
            if (focusedCellIndex == -1)
            {
                focusedCellIndex = dataGridViewClipboard.CurrentCell?.ColumnIndex ?? 0;
            }

            dataGridViewClipboard.ClearSelection();
            dataGridViewClipboard.Rows[newIndex].Selected = true;
            dataGridViewClipboard.CurrentCell = dataGridViewClipboard.Rows[newIndex].Cells[focusedCellIndex];

            // Scroll to the new index, but only if it's not already visible
            if (newIndex < dataGridViewClipboard.FirstDisplayedScrollingRowIndex
                || newIndex >= dataGridViewClipboard.FirstDisplayedScrollingRowIndex + dataGridViewClipboard.DisplayedRowCount(false))
            {
                dataGridViewClipboard.FirstDisplayedScrollingRowIndex = newIndex;
            }

            ChangeCellFocusAndDisplayCorrespondingData(newIndex, focusedCellIndex);
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
                copyTableRows(copyAllRows: null); // Null means entire table will be copied if no rows are selected, otherwise just selected rows
            }
        }

        private void copyRowDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyTableRows(copyAllRows: false);
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
            copyTableRows(copyAllRows: false, forceNoHeader: true);
        }

        // Used for Right Click Context Menu
        private void dataGridViewClipboard_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            void headerOptionsVisibility(bool visible)
            {
                contextMenu_copyColumn.Visible = visible;
                contextMenu_copyColumnNoHeader.Visible = visible;
            }
            void cellOptionsVisibility(bool visible)
            {
                contextMenu_copySingleCell.Visible = visible;
                contextMenu_copySelectedCurrentColumnOnly.Visible = visible;
                contextMenu_copySelectedRows.Visible = visible;
                contextMenu_copySelectedRowsNoHeader.Visible = visible;
            }
            // -----------------------------------------------------------------------------------

            if (recentRightClickedCell == null)
            {
                recentRightClickedCell = new RecentRightClickedCell();
            }

            if (e.Button == MouseButtons.Right)
            {
                // Note cell row and column that was right clicked
                recentRightClickedCell.RowIndex = e.RowIndex;
                recentRightClickedCell.ColumnIndex = e.ColumnIndex;

                // Check if the clicked row is part of the current selection
                bool isClickedRowSelected = false;
                foreach (DataGridViewRow row in dataGridViewClipboard.SelectedRows)
                {
                    if (row.Index == e.RowIndex)
                    {
                        isClickedRowSelected = true;
                        break;
                    }
                }

                // If right click target is a header, show specific options
                if (e.RowIndex == -1)
                {
                    headerOptionsVisibility(visible: true);
                    cellOptionsVisibility(visible: false);
                }
                else
                {
                    // Baseline visibility, adjust specifics next
                    headerOptionsVisibility(visible: false);
                    cellOptionsVisibility(visible: true);

                    // If more than one row is selected, hide the "Copy Single Cell" option and display the Copy Column button, and vice versa
                    if (dataGridViewClipboard.SelectedRows.Count > 1)
                    {
                        contextMenu_copySingleCell.Visible = false;
                        contextMenu_copySelectedCurrentColumnOnly.Visible = true;
                    }
                    else
                    {
                        contextMenu_copySingleCell.Visible = true;
                        contextMenu_copySelectedCurrentColumnOnly.Visible = false;
                    }

                    // If the clicked row is not part of the current selection, clear the selection and re-set the clicked row as the only selected row
                    if (!isClickedRowSelected)
                    {
                        dataGridViewClipboard.ClearSelection();
                        dataGridViewClipboard.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
                        // Change the cell focus
                        ChangeCellFocusAndDisplayCorrespondingData(rowIndex: e.RowIndex, cellIndex: e.ColumnIndex);
                    }
                    // If only one row is selected, change the cell focus
                    else if (isClickedRowSelected && dataGridViewClipboard.SelectedRows.Count == 1)
                    {
                        ChangeCellFocusAndDisplayCorrespondingData(rowIndex: e.RowIndex, cellIndex: e.ColumnIndex);
                    }
                }

            }
        }

        private void contextMenu_copyColumn_Click(object sender, EventArgs e)
        {
            int columnIndex = recentRightClickedCell?.ColumnIndex ?? -1;
            copyTableRows(copyAllRows: true, forceNoHeader: false, onlyColumnIndex: columnIndex);
        }

        private void contextMenu_copyColumnNoHeader_Click(object sender, EventArgs e)
        {
            int columnIndex = recentRightClickedCell?.ColumnIndex ?? -1;
            copyTableRows(copyAllRows: true, forceNoHeader: true, onlyColumnIndex: columnIndex);
        }

        private void contextMenu_copySelectedCurrentColumnOnly_Click(object sender, EventArgs e)
        {
            int columnIndex = recentRightClickedCell?.ColumnIndex ?? -1;
            copyTableRows(copyAllRows: false, forceNoHeader: true, onlyColumnIndex: columnIndex);
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
            if (dropdownContentsViewMode.SelectedIndex == (int)ViewMode.HexEdit)
            {
                UpdatePlaintextFromHexView();
            }

        }

        private void richTextBox_HexPlaintext_TextChanged(object sender, EventArgs e)
        {
            // Only bother if in edit mode
            if (dropdownContentsViewMode.SelectedIndex == (int)ViewMode.HexEdit)
            {
                UpdateHexViewChanges();

            }
        }

        private void checkBoxPlainTextEditing_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEditControlsVisibility_AndPendingGridAppearance();
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

            if (dropdownContentsViewMode.SelectedIndex == (int)ViewMode.HexEdit || dropdownContentsViewMode.SelectedIndex == (int)ViewMode.Hex)
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

            if (dropdownContentsViewMode.SelectedIndex == (int)ViewMode.HexEdit || dropdownContentsViewMode.SelectedIndex == (int)ViewMode.Hex)
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
                setCopyModeChecks(clickedItem);
            }
        }

        private void menuOptions_CommaSeparation_Click(object sender, EventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem);
            }
        }

        private void menuOptions_PreFormatted_Click(object sender, EventArgs e)
        {
            if (sender is MenuItem clickedItem)
            {
                setCopyModeChecks(clickedItem);
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
            Guid guid;
            byte[]? originalData;
            ClipboardItem? originalItem = GetSelectedClipboardItemObject(returnEditedItemVersion: false);

            if (originalItem != null)
            {
                originalData = originalItem.RawData;
            }
            else // It must be a custom format so there is no original data. Assume user wants to reset the custom format data and removal status
            {
                originalItem = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
                originalData = new byte[0];
            }
            
            if (originalItem == null)
            {
                return; // Something else went wrong, just return
            }

            guid = originalItem.UniqueID;

            // Get the original item's data and apply it to the edited item
            UpdateEditedClipboardItemRawData(guid, originalData, setPendingEdit: false, setPendingRemoval: false);

            ResetOrderIndexes();

            // Check if any edited items still have pending changes or are pending removal, and update the pending changes label if necessary
            UpdateAnyPendingChangesFlag();

            // Update the view. Edited version should be the same as the original version now
            DisplayClipboardDataInTextBoxes(GetSelectedClipboardItemObject(returnEditedItemVersion: true));
            UpdateEditControlsVisibility_AndPendingGridAppearance();
        }

        private void dataGridViewClipboard_SelectionChanged(object sender, EventArgs e)
        {
            void buttonStatus_RequireSelection(bool enabledChoice, bool onlyCustomIncompatible = false)
            {
                // Custom incompatible
                menuEdit_CopySelectedRows.Enabled = enabledChoice;
                menuFile_ExportSelectedAsRawHex.Enabled = enabledChoice;
                menuFile_ExportSelectedStruct.Enabled = enabledChoice;
                menuFile_ExportSelectedAsFile.Enabled = enabledChoice;

                if (!onlyCustomIncompatible)
                {
                    // Able to be used with custom formats
                    menuFile_LoadBinaryDataToSelected.Enabled = enabledChoice;
                }
            }
            // -------------------------------------------------------------

            if (dataGridViewClipboard.SelectedRows.Count == 0)
            {
                richTextBoxContents.Clear();
                // Disable menu buttons that require a selectedItem
                buttonStatus_RequireSelection(enabledChoice: false);
                return;
            }

            // If it's a custom format, disable the buttons and always go to the edit view
            if (GetSelectedDataFromDataGridView(colName.FormatType) == FormatTypeNames.Custom)
            {
                ChangeCellFocusAndDisplayCorrespondingData(dataGridViewClipboard.SelectedRows[0].Index);
                buttonStatus_RequireSelection(enabledChoice: false, onlyCustomIncompatible: true);
                dropdownContentsViewMode.SelectedIndex = (int)ViewMode.HexEdit; // Hex (Editable)
                checkBoxPlainTextEditing.Checked = true;
                return;
            }

            // Assume focus of the first selected row if multiple are selected
            ChangeCellFocusAndDisplayCorrespondingData(dataGridViewClipboard.SelectedRows[0].Index);

            // Enable menu buttons that require a selectedItem
            buttonStatus_RequireSelection(enabledChoice: true);

            // If the auto selection checkbox is checked, decide which view mode to use based on item data
            if (checkBoxAutoViewMode.Checked)
            {
                // Get the selectedItem object
                ClipboardItem? item = GetSelectedClipboardItemObject(returnEditedItemVersion: true);

                if (item == null)
                {
                    return; // If the item is null, just return
                }

                // If a preferred view mode is set, prioritize that
                if (item.PreferredViewMode != ViewMode.None)
                {
                    dropdownContentsViewMode.SelectedIndex = (int)item.PreferredViewMode;
                    return;
                }

                // If there is a text preview, show text mode
                if (!string.IsNullOrEmpty(GetSelectedDataFromDataGridView(colName.TextPreview)))
                {
                    dropdownContentsViewMode.SelectedIndex = (int)ViewMode.Text; // Text
                }
                // If there is data object info, show object view mode. Also show if there are multiple data info entries or the first one isn't empty
                else if (item != null && (
                        (item.ClipDataObject != null)
                        || (item.RawData != null && item.RawData.Length > 5000) // If data is enough to cause performance issues in hex view, show object view
                        || (item.DataInfoList != null && (
                            item.DataInfoList.Count > 1
                            || (item.DataInfoList.Count > 0 && !string.IsNullOrEmpty(item.DataInfoList[0]))
                        ))
                    ))
                { 
                    dropdownContentsViewMode.SelectedIndex = (int)ViewMode.Object; // Object View
                }
                else
                {
                    dropdownContentsViewMode.SelectedIndex = (int)ViewMode.Hex; // Hex View (Non Editable)
                }
            }

        }

        private void menuEdit_CopyHexAsText_Click(object sender, EventArgs e)
        {
            // Get the clipboard selectedItem and its info
            ClipboardItem? itemToCopy = GetSelectedClipboardItemObject(returnEditedItemVersion: false);
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
            ClipboardItem? itemToCopy = GetSelectedClipboardItemObject(returnEditedItemVersion: false);
            if (itemToCopy == null)
            {
                return;
            }
            // Get the struct / object info that would be displayed in object view of rich text box and copy it to clipboard
            string data = FormatStructurePrinter.GetDataStringForTextbox(formatName: Utils.GetClipboardFormatNameFromId(itemToCopy.FormatId), fullItem: itemToCopy);
            Clipboard.SetText(data);
        }

        private void menuEdit_CopyEditedHexAsText_Click(object sender, EventArgs e)
        {
            // Get the edited clipboard selectedItem and its info
            ClipboardItem? itemToCopy = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
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
            copyTableRows(copyAllRows: false);
        }

        private void menuEdit_CopyEntireTable_Click(object sender, EventArgs e)
        {
            copyTableRows(copyAllRows: true);
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
            Guid uniqueID = GetSelectedClipboardItemObject(returnEditedItemVersion: true)?.UniqueID ?? Guid.Empty;

            if (uniqueID == Guid.Empty)
            {
                return;
            }

            ClipboardItem? originalItem = GetSelectedClipboardItemObject(returnEditedItemVersion: false);

            // Check if the edited data is actually different from the original data, apply the change and set anyPendingChanges accordingly
            // First check if there is even an original item. If not it's probably a custom added item so just updated it
            if (originalItem == null)
            {
                UpdateEditedClipboardItemRawData(uniqueID, rawDataFromTextbox);
                anyPendingChanges = true;
            }
            else if(!originalItem.RawData.SequenceEqual(rawDataFromTextbox))
            {
                UpdateEditedClipboardItemRawData(uniqueID, rawDataFromTextbox);
                anyPendingChanges = true;
            }
            else
            {
                // Don't change anyPendingChanges to false because there might be other items with pending changes
            }

            UpdateEditControlsVisibility_AndPendingGridAppearance();
        }

        private void toolStripButtonSaveEdited_Click(object sender, EventArgs e)
        {
            // Trigger the end edit event to save the current cell
            dataGridViewClipboard.EndEdit();

            if (!ValidateCustomFormats())
            {
                return;
            }
            SaveClipboardData();
            //anyPendingChanges = false; // Moved into RefreshClipboardItems
            RefreshClipboardItems();
            //UpdateEditControlsVisibility_AndPendingGridAppearance(); // Occurs in RefreshClipboardItems
        }

        private void menuFile_ExportSelectedAsRawHex_Click(object sender, EventArgs e)
        {
            ClipboardItem? itemToExport = GetSelectedClipboardItemObject(returnEditedItemVersion: false);
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
            ClipboardItem? itemToExport = GetSelectedClipboardItemObject(returnEditedItemVersion: false);
            if (itemToExport == null)
            {
                return;
            }
            string nameStem = itemToExport.FormatName + "_StructInfo";
            SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "txt", defaultFileNameStem: nameStem);
            if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                // Get the hex information
                string data = FormatStructurePrinter.GetDataStringForTextbox(formatName: Utils.GetClipboardFormatNameFromId(itemToExport.FormatId), fullItem: itemToExport);
                // TO DO - Export details of each object in the struct

                // Save the data to a file
                File.WriteAllText(saveFileDialogResult.FileName, data);
            }
        }

        private void menuFile_ExportSelectedAsFile_Click(object sender, EventArgs e)
        {

            List<ClipboardItem>? selectedItems = GetSelectedClipboardItemObjectList(returnEditedItemVersion: false);

            if (selectedItems == null || selectedItems.Count == 0)
            {
                return;
            }

            foreach (ClipboardItem item in selectedItems)
            {
                SaveBinaryFile(item);
            }
            
        }

        private void toolStripButtonExportSelected_Click(object sender, EventArgs e)
        {
            List<ClipboardItem>? selectedItems = GetSelectedClipboardItemObjectList(returnEditedItemVersion: false);

            if (selectedItems == null || selectedItems.Count == 0)
            {
                return;
            }

            foreach (ClipboardItem item in selectedItems)
            {
                SaveBinaryFile(item);
            }
        }

        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            RefreshClipboardAndRestoreSelection();
        }

        private void toolStripButtonTimedRefresh_Click(object sender, EventArgs e)
        {
            // Get input from the user from a message box. The user should input a number of seconds
            string input = "";
            DialogResult inputResult = Utils.ShowInputDialog(owner: this, ref input, instructions: "Enter a delay in seconds before refreshing:"); // Will put the user input in the "input" variable

            if (inputResult == DialogResult.Cancel)
            {
                return;
            }

            if (uint.TryParse(input, out uint delay))
            {
                // Create the timer
                Timer refreshTimer = new Timer();
                refreshTimer.Interval = (int)delay * 1000; // Convert seconds to milliseconds
                refreshTimer.Tick += (object sender, EventArgs e) =>
                {
                    RefreshClipboardAndRestoreSelection();
                    refreshTimer.Stop();
                    refreshTimer.Dispose();
                };
                // Actually run the timer and code inside the tick event
                refreshTimer.Start(); 
            }
            else
            {
                MessageBox.Show("Invalid input. Please enter a valid number of seconds.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewClipboard.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow selectedRow in dataGridViewClipboard.SelectedRows)
                {
                    if (Guid.TryParse(selectedRow.Cells[colName.UniqueID].Value.ToString(), out Guid uniqueID))
                    {
                        // Update editedClipboardItems to mark the item as deleted
                        MarkIndividualClipboardItemForRemoval(uniqueID);
                    }
                }
                UpdateEditControlsVisibility_AndPendingGridAppearance();
            }
        }

        private void splitContainerMain_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // Resize processedData grid view to fit the form window
            UpdateToolLocations();
        }

        // If double click on the splitter bar, fit the datagridview to the available space (resets the splitter position to fit data grid)
        private void splitContainerMain_DoubleClick(object sender, EventArgs e)
        {
            SplitContainer container = (SplitContainer)sender;
            Point clickPoint = container.PointToClient(Cursor.Position);

            // Define the area of the splitter
            Rectangle splitterRect;
            if (container.Orientation == Orientation.Vertical)
            {
                splitterRect = new Rectangle(container.SplitterDistance, 0, container.SplitterWidth, container.Height);
            }
            else
            {
                splitterRect = new Rectangle(0, container.SplitterDistance, container.Width, container.SplitterWidth);
            }

            if (splitterRect.Contains(clickPoint))
            {
                UpdateSplitterPosition_FitDataGrid(force: true);
            }

        }

        private void dropdownContentsViewMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Indexes:
            // 0: Text
            // 1: Hex
            // 2: Hex (Editable)
            // 3: Object / Struct View

            // Show buttons and labels for edited mode
            UpdateEditControlsVisibility_AndPendingGridAppearance();

            // For object view mode and text view mode, enable auto highlighting URLs in the text box
            if (dropdownContentsViewMode.SelectedIndex == (int)ViewMode.Text || dropdownContentsViewMode.SelectedIndex == (int)ViewMode.Object)
            {
                richTextBoxContents.DetectUrls = true;
            }
            else
            {
                richTextBoxContents.DetectUrls = false;
            }

            ClipboardItem? item = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
            if (item == null)
            {
                return;
            }

            DisplayClipboardDataInTextBoxes(item);
        }

        // Left Click
        private void dataGridViewClipboard_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            ChangeCellFocusAndDisplayCorrespondingData(e.RowIndex);
            UpdateEditControlsVisibility_AndPendingGridAppearance();
        }

        private void dataGridViewClipboard_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            // If it's the format ID column or another numerical column, sort them numerically instead of alphabetically
            if (e.Column.Name == colName.FormatId || e.Column.Name == colName.Index)
            {
                // Try to parse the values as numbers
                if (int.TryParse(e.CellValue1?.ToString(), out int value1) &&
                    int.TryParse(e.CellValue2?.ToString(), out int value2))
                {
                    // Compare the parsed numeric values
                    e.SortResult = value1.CompareTo(value2);
                    e.Handled = true;
                }
            }
        }

        private void dataGridViewClipboard_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == dataGridViewClipboard.Columns[colName.Index].Index)
            {
                // Suspend layout updates
                dataGridViewClipboard.SuspendLayout();

                // Sort the Index column
                dataGridViewClipboard.Sort(dataGridViewClipboard.Columns[colName.Index], System.ComponentModel.ListSortDirection.Ascending);

                // Hide the sort indicator
                dataGridViewClipboard.Columns[colName.Index].HeaderCell.SortGlyphDirection = SortOrder.None;

                // Resume layout updates
                dataGridViewClipboard.ResumeLayout();
            }
            else
            {
                // Set the focused cell to the same column as the clicked header
                dataGridViewClipboard.CurrentCell = dataGridViewClipboard.Rows[0].Cells[e.ColumnIndex];
            }

        }
        private void menuHelp_WhyTakingLong_Click(object sender, EventArgs e)
        {
            MessageBox.Show("In some cases, loading the clipboard may take longer than expected.\n\n" +
                "The reason is that many apps use a clipboard feature called \"Delayed Rendering\" to " +
                "optimize performance. With delayed rendering, apps don't actually copy data to the " +
                "clipboard until another app requests it.\n\n" +
                "When this app fetches the clipboard, it requests ALL of these delayed render formats for " +
                "so you can view the contents, causing the original apps to generate and transfer the data on demand. " +
                "This process can take time, especially for large amounts of data or complex formats.\n\n" +
                "This is also why you usually never notice the delay when pasting data into another app.",
                "Why is clipboard loading slow?", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripButtonAddFormat_Click(object sender, EventArgs e)
        {
            int itemIndex = dataGridViewClipboard.Rows.Count;  // The index of the current last item will be (count - 1) so the new item will be at index count

            string customName = MyStrings.DefaultCustomFormatName;
            // Check if the default name is already in use. If so, add a number to the end
            if (editedClipboardItems.Any(item => item.FormatName == customName))
            {
                int i = 1;
                while (editedClipboardItems.Any(item => item.FormatName == customName + " " + i))
                {
                    i++;
                }
                customName = $"{customName} {i}";
            }

            // Create a new boilerplate clipboard item
            ClipboardItem? newItem = new ClipboardItem()
            {
                FormatId = 0,
                FormatName = customName,
                RawData = new byte[0],
                ClipDataObject = null,
                DataInfoList = [ MyStrings.CustomPendingData ],
                OriginalIndex = itemIndex,
                FormatType = FormatTypeNames.Custom,
                PendingCustomAddition = true,
            };

            //UpdateClipboardItemsGridView_WithEmptyCustomFormat(newItem);
            editedClipboardItems.Add(newItem);
            anyPendingChanges = true;
            RefreshDataGridViewContents();
            UpdateSplitterPosition_FitDataGrid();

            //UpdateEditControlsVisibility_AndPendingGridAppearance(); // Occurs in RefreshDataGridViewContents

            // Set selected rows to just the new row
            dataGridViewClipboard.SelectionChanged -= dataGridViewClipboard_SelectionChanged;
            dataGridViewClipboard.ClearSelection();
            dataGridViewClipboard.SelectionChanged += dataGridViewClipboard_SelectionChanged;
            dataGridViewClipboard.Rows[itemIndex].Selected = true;

            // if the row isn't visible, scroll to it
            if (itemIndex >= dataGridViewClipboard.FirstDisplayedScrollingRowIndex + dataGridViewClipboard.DisplayedRowCount(false))
            {
                dataGridViewClipboard.FirstDisplayedScrollingRowIndex = itemIndex;
            }

        }

        private void dataGridViewClipboard_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ensure the indexes are valid
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }
            // Reset editability of the grid to false by default
            dataGridViewClipboard.ReadOnly = true;
            dataGridViewClipboard[e.ColumnIndex, e.RowIndex].ReadOnly = false;

            // Only allow editing for custom added formats
            ClipboardItem? item = GetSelectedClipboardItemObject(returnEditedItemVersion: true);
            if (item != null && item.PendingCustomAddition == true)
            {
                int rowIndex = e.RowIndex;
                int columnIndex = e.ColumnIndex;
                string columnName = dataGridViewClipboard.Columns[columnIndex].Name;

                List<string> allowedToEditColumns = new List<string> { colName.FormatName, colName.FormatId };

                // Dictionary with reasons not allowed to edit specific columns
                Dictionary<string, string> notAllowedToEditColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    //{ colName.FormatId, "Cannot Edit Format ID: This number is set automatically by windows." },
                    { colName.FormatType, "Cannot Edit Format Type: This column is informational only derived from other properties, it is not an actual value." },
                    { colName.Index, "Index cannot currently be changed." },
                };

                if (allowedToEditColumns.Contains(columnName))
                {
                    dataGridViewClipboard.ReadOnly = false;
                    dataGridViewClipboard[columnIndex, rowIndex].ReadOnly = false;
                    dataGridViewClipboard.BeginEdit(true);
                }
                else
                {
                    // If a message is available for the column, show it.
                    if (notAllowedToEditColumns.TryGetValue(columnName, out string message))
                    {
                        MessageBox.Show(message, "Cannot Edit Column", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

            }
        }

        private void dataGridViewClipboard_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            itemBeforeCellEditClone = null;
            itemBeforeCellEditClone = (ClipboardItem?)GetSelectedClipboardItemObject(returnEditedItemVersion: true)?.Clone();
        }

        private void dataGridViewClipboard_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Get the item before the cell was edited
            ClipboardItem? itemBeforeEdit = itemBeforeCellEditClone;
            if (itemBeforeEdit == null)
            {
                MessageBox.Show("Error: Couldn't find the clipboard object from before the edit.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataGridViewRow row = dataGridViewClipboard.Rows[e.RowIndex];

            // If the edited cell is in the name column, set the value in the format id column to 0
            if (dataGridViewClipboard.Columns[e.ColumnIndex].Name == colName.FormatName)
            {
                // Currently no cell value change event handler so no need to disable it, otherwise we would
                row.Cells[colName.FormatId].Value = MyStrings.DefaultCustomFormatID;
            }
            // If the edited cell is in the format id column, set the value in the format name column to say custom format
            else if (dataGridViewClipboard.Columns[e.ColumnIndex].Name == colName.FormatId)
            {
                row.Cells[colName.FormatName].Value = MyStrings.DefaultCustomFormatName;
            }

            // Updates the editedClipboardItems list with the new data
            Guid uniqueID = itemBeforeEdit.UniqueID;
            uint formatId = uint.Parse(row.Cells[colName.FormatId].Value.ToString());
            string formatName = row.Cells[colName.FormatName].Value.ToString();

            // Will need to add a validation function here later

            // Update the edited item
            ClipboardItem? editedItem = editedClipboardItems.FirstOrDefault(i => i.UniqueID == uniqueID);
            if (editedItem != null)
            {
                editedItem.FormatId = formatId;
                editedItem.FormatName = formatName;
            }
            else
            {
                MessageBox.Show("Error: Couldn't find the clipboard object in the edited object list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void menuItemFile_ExportRegisteredFormats_Click(object sender, EventArgs e)
        {
            Dictionary<uint,string>? formatPairs = Utils.GetAllPossibleRegisteredFormatNames();

            if (formatPairs == null)
            {
                return;
            }

            // Convert to formatted string for file output
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Format ID\tFormat Name");
            foreach (KeyValuePair<uint, string> pair in formatPairs)
            {
                sb.AppendLine($"{pair.Key}\t{pair.Value}");
            }

            SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "txt", defaultFileNameStem: "RegisteredFormats");
            if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialogResult.FileName, sb.ToString());
            }
        }

        private void toolStripButtonFetchManualFormat_Click(object sender, EventArgs e)
        {
            bool result;
            bool existingItem;

            // Get input from the user from a message box. They can enter a format name or format ID
            string input = "";
            DialogResult inputResult = Utils.ShowInputDialog(owner: this, ref input, instructions: "Enter format name or ID to add (or update):"); // Will put the user input in the "input" variable

            if (inputResult == DialogResult.Cancel)
            {
                return;
            }

            // Try to parse it as a uint first
            if (uint.TryParse(input, out uint formatId))
            {
                (result, existingItem) = ManuallyCopySpecifiedClipboardFormat(formatId: formatId);

                // If the result still failed, also try using the inputted number as a format name string just in case, though unlikely
                if (!result)
                {
                    (result, existingItem) = ManuallyCopySpecifiedClipboardFormat(formatName: input, silent: true); // Using silent to avoid invalid format name message
                }
            }
            // Not a uint, so must be a format name if anything
            else
            {
                formatId = Utils.GetClipboardFormatIdFromName(formatName: input, caseSensitive: false);

                if (formatId != 0)
                {
                    (result, existingItem) = ManuallyCopySpecifiedClipboardFormat(formatId: formatId);
                }
                else
                {
                    MessageBox.Show("Error: Couldn't find a format with the name or ID you entered.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Final result
            if (result)
            {
                ProcessClipboardData();
                if (existingItem)
                {
                    MessageBox.Show("Successfully fetched and updated specified format.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Successfully fetched and added specified format.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                    
            }
            else
            {
                MessageBox.Show($"Error: Couldn't fetch the specified format: {input}\n\nIt might not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuOptions_RetryMode_Click(object sender, EventArgs e)
        {
            menuOptions_RetryMode.Checked = !menuOptions_RetryMode.Checked; // Toggle check
        }

        private void menuFile_LoadBinaryDataToSelected_Click(object sender, EventArgs e)
        {
            // Open a file chooser dialog for any file type
            OpenFileDialog openFileDialogResult = new OpenFileDialog();
            if (openFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialogResult.FileName;

                // Check the file exists
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Error: The file you selected doesn't exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] fileData = File.ReadAllBytes(filePath);
                
                ClipboardItem? item = GetSelectedClipboardItemObject(returnEditedItemVersion: true);

                if (item == null)
                {
                    MessageBox.Show("Error: No format seems to be selected in the data grid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Guid uniqueID = item.UniqueID;

                UpdateEditedClipboardItemRawData(uniqueID, fileData);
                anyPendingChanges = true;

                DisplayClipboardDataInTextBoxes(item);
                UpdateEditControlsVisibility_AndPendingGridAppearance();
            }
        }

        private void buttonIncreaseIndexNumber_Click(object sender, EventArgs e)
        {
            ClipboardItem? item = GetSelectedClipboardItemObject(returnEditedItemVersion: true);

            if (item == null)
                return;

            int currentIndex = item.OriginalIndex;

            // Check if the index is already the highest index
            if (currentIndex == dataGridViewClipboard.Rows.Count - 1)
                return;

            // Get the item above the current one by searching through editedClipboardItems
            ClipboardItem? itemToSwap = editedClipboardItems.FirstOrDefault(i => i.OriginalIndex == currentIndex + 1);
            int indexToSwap = itemToSwap.OriginalIndex;

            if (itemToSwap == null)
                return;

            UpdateEditedClipboardItemIndex(item.UniqueID, indexToSwap);
            UpdateEditedClipboardItemIndex(itemToSwap.UniqueID, currentIndex);

            RefreshDataGridViewContents();
            DisplayClipboardDataInTextBoxes(item);
            //UpdateEditControlsVisibility_AndPendingGridAppearance();
        }

        private void buttonDecreaseIndexNumber_Click(object sender, EventArgs e)
        {
            ClipboardItem? item = GetSelectedClipboardItemObject(returnEditedItemVersion: true);

            if (item == null)
                return;

            int currentIndex = item.OriginalIndex;

            // Check if the index is already the lowest
            if (currentIndex == 0)
                return;

            // Get the item above the current one by searching through editedClipboardItems
            ClipboardItem? itemToSwap = editedClipboardItems.FirstOrDefault(i => i.OriginalIndex == currentIndex - 1);
            int indexToSwap = itemToSwap.OriginalIndex;

            if (itemToSwap == null)
                return;

            UpdateEditedClipboardItemIndex(item.UniqueID, indexToSwap);
            UpdateEditedClipboardItemIndex(itemToSwap.UniqueID, currentIndex);

            RefreshDataGridViewContents();
            DisplayClipboardDataInTextBoxes(item);
            //UpdateEditControlsVisibility_AndPendingGridAppearance();
        }

        private void menuEdit_RefreshDataTable_Click(object sender, EventArgs e)
        {
            RefreshDataGridViewContents();
        }

        private void EditedClipboardItems_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (editedClipboardItems != null && editedClipboardItems.Count > 0)
            {
                // Resort the editedClipboardItems list if the list has been changed
                RefreshDataGridViewContents();
            }
        }

        private void buttonResetOrder_Click(object sender, EventArgs e)
        {
            ResetOrderIndexes();
            RefreshDataGridViewContents();
        }

        private void menuFile_ExportAllFolder_Click(object sender, EventArgs e)
        {
            //List<ClipboardItem>? itemsToExport = GetSelectedClipboardItemObjectList(returnEditedItemVersion: false);

            if (clipboardItems.Count == 0)
                return;

            // Show a folder browser dialog
            FolderPicker folderOpenDialogue = new FolderPicker();
            folderOpenDialogue.InputPath = Directory.GetCurrentDirectory();
            string chosenPath;

            // Show the actual dialogue based on input path derived from stuff above
            if (folderOpenDialogue.ShowDialog(this.Handle, throwOnError: false) == true)
            {
                // Store the selected folder path to use next time
                chosenPath = folderOpenDialogue.ResultPath;
                ExportBackupFolder(itemsToExport: clipboardItems, path: chosenPath, zip: false);
            }
        }

        private void menuFile_ExportAllZip_Click(object sender, EventArgs e)
        {
            //List<ClipboardItem>? itemsToExport = GetSelectedClipboardItemObjectList(returnEditedItemVersion: false);

            if (clipboardItems.Count == 0)
                return;

            // Show save file dialog for zip file
            SaveFileDialog saveFileDialogResult = SaveFileDialog(extension: "zip", defaultFileNameStem: "Clipboard");
            if (saveFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialogResult.FileName;
                ExportBackupFolder(itemsToExport: clipboardItems, path: filePath, zip: true);
            }
        }

        private void menuFile_ImportBackupFolder_Click(object sender, EventArgs e)
        {
            // Show a folder browser dialog
            FolderPicker folderOpenDialogue = new FolderPicker();
            folderOpenDialogue.InputPath = Directory.GetCurrentDirectory();
            string chosenPath;

            // Show the actual dialogue based on input path derived from stuff above
            if (folderOpenDialogue.ShowDialog(this.Handle, throwOnError: false) == true)
            {
                // Store the selected folder path to use next time
                chosenPath = folderOpenDialogue.ResultPath;
                List<ClipboardItem> importedItems = LoadItemsFromBackup(chosenPath);

                if (importedItems.Count > 0)
                {
                    ProcessImportedItems(importedItems);
                }
            }
        }

        private void menuFile_ImportBackupZip_Click(object sender, EventArgs e)
        {
            // File selection dialogue for zip file
            OpenFileDialog openFileDialogResult = new OpenFileDialog();
            openFileDialogResult.Filter = "Zip Files|*.zip";
            if (openFileDialogResult.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialogResult.FileName;
                List<ClipboardItem> importedItems = LoadItemsFromBackup(filePath);

                if (importedItems.Count > 0)
                {
                    ProcessImportedItems(importedItems);
                }
            }
        }

        private void menuEdit_ClearClipboard_Click(object sender, EventArgs e)
        {
            ClearClipboard();

            RefreshClipboardItems();
            //UpdateAnyPendingChangesFlag(); // Moved into RefreshClipboardItems
            //UpdateEditControlsVisibility_AndPendingGridAppearance(); // Occurs in RefreshClipboardItems > RefreshDataGridViewContents
        }

        private void menuHelp_DebugInfo_Click(object sender, EventArgs e)
        {
            // Get the handle of the current window
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
            DiagnosticsInfo debugInfo = DiagnoseClipboardState();

            string debugInfoString = debugInfo.ReportString;
            
            MessageBox.Show(debugInfoString, "Debug Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        // --------------------------------------------- DEBUG CONTROLS AND BUTTONS --------------------------------------------------------

        // Using for testing random things during development via a button
        private void menuDebug_TestButton_Click(object sender, EventArgs e)
        {
            string input = "";
            Utils.ShowInputDialog(owner: this, ref input, instructions: "Enter new toolstrip height:");
            if (int.TryParse(input, out int newHeight))
            {
                toolStrip1.Height = newHeight;
                ScaleToolstripButtons();
            }
        }


        private void menuDebug_TooltipDimensionsMode_Click(object sender, EventArgs e)
        {
            menuDebug_TooltipDimensionsMode.Checked = !menuDebug_TooltipDimensionsMode.Checked;

            if (menuDebug_TooltipDimensionsMode.Checked)
            {
                DebugUtils.SetDebugTooltips(this);
            }
            else
            {
                DebugUtils.RestoreOriginalTooltips(this);
            }
        }


    } // ----------------------------- End of MainForm partial class -----------------------------


}
