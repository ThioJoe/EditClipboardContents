namespace ClipboardManager
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.dataGridViewClipboard = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip_dataGridView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyCellToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyRowDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copySelectedRowsNoHeaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuMainFile = new System.Windows.Forms.MenuItem();
            this.menuFile_ExportSelectedAsRawHex = new System.Windows.Forms.MenuItem();
            this.menuFile_ExportSelectedAsFile = new System.Windows.Forms.MenuItem();
            this.menuFile_ExportSelectedStruct = new System.Windows.Forms.MenuItem();
            this.menuMainEdit = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopyObjectInfoAsText = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopyHexAsText = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopyEditedHexAsText = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopyEntireTable = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopySelectedRows = new System.Windows.Forms.MenuItem();
            this.menuItemOptions = new System.Windows.Forms.MenuItem();
            this.menuOptions_ShowLargeHex = new System.Windows.Forms.MenuItem();
            this.menuOptions_IncludeRowHeaders = new System.Windows.Forms.MenuItem();
            this.menuOptions_TableModeMenu = new System.Windows.Forms.MenuItem();
            this.menuOptions_PreFormatted = new System.Windows.Forms.MenuItem();
            this.menuOptions_TabSeparation = new System.Windows.Forms.MenuItem();
            this.menuOptions_CommaSeparation = new System.Windows.Forms.MenuItem();
            this.menuItemHelp = new System.Windows.Forms.MenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSaveEdited = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExportSelected = new System.Windows.Forms.ToolStripButton();
            this.richTextBoxContents = new System.Windows.Forms.RichTextBox();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.splitterContainer_InnerTextBoxes = new System.Windows.Forms.SplitContainer();
            this.checkBoxPlainTextEditing = new System.Windows.Forms.CheckBox();
            this.dropdownHexToTextEncoding = new System.Windows.Forms.ComboBox();
            this.labelHexToPlaintextEncoding = new System.Windows.Forms.Label();
            this.richTextBox_HexPlaintext = new System.Windows.Forms.RichTextBox();
            this.labelSynthesizedTypeWarn = new System.Windows.Forms.Label();
            this.buttonResetEdit = new System.Windows.Forms.Button();
            this.buttonApplyEdit = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.dropdownContentsViewMode = new System.Windows.Forms.ComboBox();
            this.labelPendingChanges = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.menuHelp_About = new System.Windows.Forms.MenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewClipboard)).BeginInit();
            this.contextMenuStrip_dataGridView.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitterContainer_InnerTextBoxes)).BeginInit();
            this.splitterContainer_InnerTextBoxes.Panel1.SuspendLayout();
            this.splitterContainer_InnerTextBoxes.Panel2.SuspendLayout();
            this.splitterContainer_InnerTextBoxes.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridViewClipboard
            // 
            this.dataGridViewClipboard.AllowUserToAddRows = false;
            this.dataGridViewClipboard.AllowUserToDeleteRows = false;
            this.dataGridViewClipboard.AllowUserToResizeRows = false;
            this.dataGridViewClipboard.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewClipboard.ContextMenuStrip = this.contextMenuStrip_dataGridView;
            this.dataGridViewClipboard.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewClipboard.Name = "dataGridViewClipboard";
            this.dataGridViewClipboard.ReadOnly = true;
            this.dataGridViewClipboard.RowHeadersWidth = 62;
            this.dataGridViewClipboard.Size = new System.Drawing.Size(971, 266);
            this.dataGridViewClipboard.TabIndex = 0;
            this.dataGridViewClipboard.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewClipboard_CellClick);
            this.dataGridViewClipboard.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewClipboard_CellMouseDown);
            this.dataGridViewClipboard.SelectionChanged += new System.EventHandler(this.dataGridViewClipboard_SelectionChanged);
            this.dataGridViewClipboard.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridViewClipboard_KeyDown);
            this.dataGridViewClipboard.MouseEnter += new System.EventHandler(this.dataGridViewClipboard_MouseEnter);
            // 
            // contextMenuStrip_dataGridView
            // 
            this.contextMenuStrip_dataGridView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyCellToolStripMenuItem,
            this.copyRowDataToolStripMenuItem,
            this.copySelectedRowsNoHeaderToolStripMenuItem});
            this.contextMenuStrip_dataGridView.Name = "contextMenuStrip_dataGridView";
            this.contextMenuStrip_dataGridView.Size = new System.Drawing.Size(249, 70);
            this.contextMenuStrip_dataGridView.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_dataGridView_Opening);
            // 
            // copyCellToolStripMenuItem
            // 
            this.copyCellToolStripMenuItem.Name = "copyCellToolStripMenuItem";
            this.copyCellToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.copyCellToolStripMenuItem.Text = "Copy Single Cell";
            this.copyCellToolStripMenuItem.Click += new System.EventHandler(this.copyCellToolStripMenuItem_Click);
            // 
            // copyRowDataToolStripMenuItem
            // 
            this.copyRowDataToolStripMenuItem.Name = "copyRowDataToolStripMenuItem";
            this.copyRowDataToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.copyRowDataToolStripMenuItem.Text = "Copy Selected Rows";
            this.copyRowDataToolStripMenuItem.Click += new System.EventHandler(this.copyRowDataToolStripMenuItem_Click);
            // 
            // copySelectedRowsNoHeaderToolStripMenuItem
            // 
            this.copySelectedRowsNoHeaderToolStripMenuItem.Name = "copySelectedRowsNoHeaderToolStripMenuItem";
            this.copySelectedRowsNoHeaderToolStripMenuItem.Size = new System.Drawing.Size(248, 22);
            this.copySelectedRowsNoHeaderToolStripMenuItem.Text = "Copy Selected Rows (No Header)";
            this.copySelectedRowsNoHeaderToolStripMenuItem.Click += new System.EventHandler(this.copySelectedRowsNoHeaderToolStripMenuItem_Click);
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuMainFile,
            this.menuMainEdit,
            this.menuItemOptions,
            this.menuItemHelp});
            // 
            // menuMainFile
            // 
            this.menuMainFile.Index = 0;
            this.menuMainFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuFile_ExportSelectedAsRawHex,
            this.menuFile_ExportSelectedAsFile,
            this.menuFile_ExportSelectedStruct});
            this.menuMainFile.Text = "File";
            // 
            // menuFile_ExportSelectedAsRawHex
            // 
            this.menuFile_ExportSelectedAsRawHex.Index = 0;
            this.menuFile_ExportSelectedAsRawHex.Text = "Export Selected As Raw Hex";
            this.menuFile_ExportSelectedAsRawHex.Click += new System.EventHandler(this.menuFile_ExportSelectedAsRawHex_Click);
            // 
            // menuFile_ExportSelectedAsFile
            // 
            this.menuFile_ExportSelectedAsFile.Index = 1;
            this.menuFile_ExportSelectedAsFile.Text = "Export Selected As File";
            this.menuFile_ExportSelectedAsFile.Click += new System.EventHandler(this.menuFile_ExportSelectedAsFile_Click);
            // 
            // menuFile_ExportSelectedStruct
            // 
            this.menuFile_ExportSelectedStruct.Index = 2;
            this.menuFile_ExportSelectedStruct.Text = "Export Selected Object Info";
            this.menuFile_ExportSelectedStruct.Click += new System.EventHandler(this.menuFile_ExportSelectedStruct_Click);
            // 
            // menuMainEdit
            // 
            this.menuMainEdit.Index = 1;
            this.menuMainEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuEdit_CopyObjectInfoAsText,
            this.menuEdit_CopyHexAsText,
            this.menuEdit_CopyEditedHexAsText,
            this.menuItem3,
            this.menuEdit_CopyEntireTable,
            this.menuEdit_CopySelectedRows});
            this.menuMainEdit.Text = "Edit";
            // 
            // menuEdit_CopyObjectInfoAsText
            // 
            this.menuEdit_CopyObjectInfoAsText.Index = 0;
            this.menuEdit_CopyObjectInfoAsText.Text = "Copy Object Info As Text";
            this.menuEdit_CopyObjectInfoAsText.Click += new System.EventHandler(this.menuEdit_CopyObjectInfoAsText_Click);
            // 
            // menuEdit_CopyHexAsText
            // 
            this.menuEdit_CopyHexAsText.Index = 1;
            this.menuEdit_CopyHexAsText.Text = "Copy Hex Data As Text";
            this.menuEdit_CopyHexAsText.Click += new System.EventHandler(this.menuEdit_CopyHexAsText_Click);
            // 
            // menuEdit_CopyEditedHexAsText
            // 
            this.menuEdit_CopyEditedHexAsText.Enabled = false;
            this.menuEdit_CopyEditedHexAsText.Index = 2;
            this.menuEdit_CopyEditedHexAsText.Text = "Copy Edited Hex As Text";
            this.menuEdit_CopyEditedHexAsText.Click += new System.EventHandler(this.menuEdit_CopyEditedHexAsText_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 3;
            this.menuItem3.Text = "-";
            // 
            // menuEdit_CopyEntireTable
            // 
            this.menuEdit_CopyEntireTable.Index = 4;
            this.menuEdit_CopyEntireTable.Text = "Copy All Table Data";
            this.menuEdit_CopyEntireTable.Click += new System.EventHandler(this.menuEdit_CopyEntireTable_Click);
            // 
            // menuEdit_CopySelectedRows
            // 
            this.menuEdit_CopySelectedRows.Index = 5;
            this.menuEdit_CopySelectedRows.Text = "Copy Selected Table Rows";
            this.menuEdit_CopySelectedRows.Click += new System.EventHandler(this.menuEdit_CopySelectedRows_Click);
            // 
            // menuItemOptions
            // 
            this.menuItemOptions.Index = 2;
            this.menuItemOptions.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuOptions_ShowLargeHex,
            this.menuOptions_IncludeRowHeaders,
            this.menuOptions_TableModeMenu});
            this.menuItemOptions.Text = "Options";
            // 
            // menuOptions_ShowLargeHex
            // 
            this.menuOptions_ShowLargeHex.Index = 0;
            this.menuOptions_ShowLargeHex.Text = "Show Hex For Large Files";
            this.menuOptions_ShowLargeHex.Click += new System.EventHandler(this.menuItemShowLargeHex_Click);
            // 
            // menuOptions_IncludeRowHeaders
            // 
            this.menuOptions_IncludeRowHeaders.Checked = true;
            this.menuOptions_IncludeRowHeaders.Index = 1;
            this.menuOptions_IncludeRowHeaders.Text = "Include Headers When Copying Table";
            this.menuOptions_IncludeRowHeaders.Click += new System.EventHandler(this.menuOptions_IncludeRowHeaders_Click);
            // 
            // menuOptions_TableModeMenu
            // 
            this.menuOptions_TableModeMenu.Index = 2;
            this.menuOptions_TableModeMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuOptions_PreFormatted,
            this.menuOptions_TabSeparation,
            this.menuOptions_CommaSeparation});
            this.menuOptions_TableModeMenu.Text = "Table Copying Mode";
            // 
            // menuOptions_PreFormatted
            // 
            this.menuOptions_PreFormatted.Checked = true;
            this.menuOptions_PreFormatted.Index = 0;
            this.menuOptions_PreFormatted.Text = "Pre-Formatted";
            this.menuOptions_PreFormatted.Click += new System.EventHandler(this.menuOptions_PreFormatted_Click);
            // 
            // menuOptions_TabSeparation
            // 
            this.menuOptions_TabSeparation.Index = 1;
            this.menuOptions_TabSeparation.Text = "Single-Tab Separation";
            this.menuOptions_TabSeparation.Click += new System.EventHandler(this.menuOptions_TabSeparation_Click);
            // 
            // menuOptions_CommaSeparation
            // 
            this.menuOptions_CommaSeparation.Index = 2;
            this.menuOptions_CommaSeparation.Text = "Comma Separation";
            this.menuOptions_CommaSeparation.Click += new System.EventHandler(this.menuOptions_CommaSeparation_Click);
            // 
            // menuItemHelp
            // 
            this.menuItemHelp.Index = 3;
            this.menuItemHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuHelp_About});
            this.menuItemHelp.Text = "Help";
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonRefresh,
            this.toolStripButtonDelete,
            this.toolStripButtonSaveEdited,
            this.toolStripButtonExportSelected});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStrip1.Size = new System.Drawing.Size(993, 31);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonRefresh
            // 
            this.toolStripButtonRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRefresh.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRefresh.Image")));
            this.toolStripButtonRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRefresh.Name = "toolStripButtonRefresh";
            this.toolStripButtonRefresh.Size = new System.Drawing.Size(28, 28);
            this.toolStripButtonRefresh.Text = "Refresh";
            this.toolStripButtonRefresh.Click += new System.EventHandler(this.toolStripButtonRefresh_Click);
            // 
            // toolStripButtonDelete
            // 
            this.toolStripButtonDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDelete.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonDelete.Image")));
            this.toolStripButtonDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDelete.Name = "toolStripButtonDelete";
            this.toolStripButtonDelete.Size = new System.Drawing.Size(28, 28);
            this.toolStripButtonDelete.Text = "Delete Item From Clipboard";
            this.toolStripButtonDelete.Click += new System.EventHandler(this.toolStripButtonDelete_Click);
            // 
            // toolStripButtonSaveEdited
            // 
            this.toolStripButtonSaveEdited.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSaveEdited.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSaveEdited.Image")));
            this.toolStripButtonSaveEdited.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSaveEdited.Name = "toolStripButtonSaveEdited";
            this.toolStripButtonSaveEdited.Size = new System.Drawing.Size(28, 28);
            this.toolStripButtonSaveEdited.Text = "Save Edited Clipboard";
            this.toolStripButtonSaveEdited.ToolTipText = "Re-Write clipboard with edited data";
            this.toolStripButtonSaveEdited.Click += new System.EventHandler(this.toolStripButtonSaveEdited_Click);
            // 
            // toolStripButtonExportSelected
            // 
            this.toolStripButtonExportSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonExportSelected.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonExportSelected.Image")));
            this.toolStripButtonExportSelected.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExportSelected.Name = "toolStripButtonExportSelected";
            this.toolStripButtonExportSelected.Size = new System.Drawing.Size(28, 28);
            this.toolStripButtonExportSelected.Text = "toolStripButton1";
            this.toolStripButtonExportSelected.ToolTipText = "Export selected item data as file";
            this.toolStripButtonExportSelected.Click += new System.EventHandler(this.toolStripButtonExportSelected_Click);
            // 
            // richTextBoxContents
            // 
            this.richTextBoxContents.DetectUrls = false;
            this.richTextBoxContents.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxContents.HideSelection = false;
            this.richTextBoxContents.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxContents.Name = "richTextBoxContents";
            this.richTextBoxContents.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBoxContents.Size = new System.Drawing.Size(629, 280);
            this.richTextBoxContents.TabIndex = 4;
            this.richTextBoxContents.Text = "";
            this.richTextBoxContents.SelectionChanged += new System.EventHandler(this.richTextBoxContents_SelectionChanged);
            this.richTextBoxContents.TextChanged += new System.EventHandler(this.richTextBoxContents_TextChanged);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Location = new System.Drawing.Point(8, 34);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.dataGridViewClipboard);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.splitterContainer_InnerTextBoxes);
            this.splitContainerMain.Panel2.Controls.Add(this.labelSynthesizedTypeWarn);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonResetEdit);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonApplyEdit);
            this.splitContainerMain.Panel2.Controls.Add(this.label1);
            this.splitContainerMain.Panel2.Controls.Add(this.dropdownContentsViewMode);
            this.splitContainerMain.Size = new System.Drawing.Size(977, 586);
            this.splitContainerMain.SplitterDistance = 272;
            this.splitContainerMain.TabIndex = 6;
            this.splitContainerMain.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // splitterContainer_InnerTextBoxes
            // 
            this.splitterContainer_InnerTextBoxes.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitterContainer_InnerTextBoxes.Location = new System.Drawing.Point(0, 30);
            this.splitterContainer_InnerTextBoxes.Name = "splitterContainer_InnerTextBoxes";
            // 
            // splitterContainer_InnerTextBoxes.Panel1
            // 
            this.splitterContainer_InnerTextBoxes.Panel1.Controls.Add(this.richTextBoxContents);
            // 
            // splitterContainer_InnerTextBoxes.Panel2
            // 
            this.splitterContainer_InnerTextBoxes.Panel2.Controls.Add(this.checkBoxPlainTextEditing);
            this.splitterContainer_InnerTextBoxes.Panel2.Controls.Add(this.dropdownHexToTextEncoding);
            this.splitterContainer_InnerTextBoxes.Panel2.Controls.Add(this.labelHexToPlaintextEncoding);
            this.splitterContainer_InnerTextBoxes.Panel2.Controls.Add(this.richTextBox_HexPlaintext);
            this.splitterContainer_InnerTextBoxes.Size = new System.Drawing.Size(977, 280);
            this.splitterContainer_InnerTextBoxes.SplitterDistance = 631;
            this.splitterContainer_InnerTextBoxes.TabIndex = 13;
            this.splitterContainer_InnerTextBoxes.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitterContainer_InnerTextBoxes_SplitterMoved);
            // 
            // checkBoxPlainTextEditing
            // 
            this.checkBoxPlainTextEditing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxPlainTextEditing.AutoSize = true;
            this.checkBoxPlainTextEditing.Location = new System.Drawing.Point(13, 255);
            this.checkBoxPlainTextEditing.Name = "checkBoxPlainTextEditing";
            this.checkBoxPlainTextEditing.Size = new System.Drawing.Size(101, 17);
            this.checkBoxPlainTextEditing.TabIndex = 3;
            this.checkBoxPlainTextEditing.Text = "Plaintext Editing";
            this.toolTip1.SetToolTip(this.checkBoxPlainTextEditing, resources.GetString("checkBoxPlainTextEditing.ToolTip"));
            this.checkBoxPlainTextEditing.UseVisualStyleBackColor = true;
            this.checkBoxPlainTextEditing.CheckedChanged += new System.EventHandler(this.checkBoxPlainTextEditing_CheckedChanged);
            // 
            // dropdownHexToTextEncoding
            // 
            this.dropdownHexToTextEncoding.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.dropdownHexToTextEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dropdownHexToTextEncoding.FormattingEnabled = true;
            this.dropdownHexToTextEncoding.Items.AddRange(new object[] {
            "UTF-8",
            "UTF-16 (Unicode)"});
            this.dropdownHexToTextEncoding.Location = new System.Drawing.Point(193, 255);
            this.dropdownHexToTextEncoding.Name = "dropdownHexToTextEncoding";
            this.dropdownHexToTextEncoding.Size = new System.Drawing.Size(146, 21);
            this.dropdownHexToTextEncoding.TabIndex = 2;
            this.dropdownHexToTextEncoding.SelectedIndexChanged += new System.EventHandler(this.dropdownHexToTextEncoding_SelectedIndexChanged);
            // 
            // labelHexToPlaintextEncoding
            // 
            this.labelHexToPlaintextEncoding.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHexToPlaintextEncoding.AutoSize = true;
            this.labelHexToPlaintextEncoding.Location = new System.Drawing.Point(132, 259);
            this.labelHexToPlaintextEncoding.Name = "labelHexToPlaintextEncoding";
            this.labelHexToPlaintextEncoding.Size = new System.Drawing.Size(55, 13);
            this.labelHexToPlaintextEncoding.TabIndex = 1;
            this.labelHexToPlaintextEncoding.Text = "Encoding:";
            // 
            // richTextBox_HexPlaintext
            // 
            this.richTextBox_HexPlaintext.DetectUrls = false;
            this.richTextBox_HexPlaintext.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox_HexPlaintext.HideSelection = false;
            this.richTextBox_HexPlaintext.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_HexPlaintext.Name = "richTextBox_HexPlaintext";
            this.richTextBox_HexPlaintext.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBox_HexPlaintext.Size = new System.Drawing.Size(342, 246);
            this.richTextBox_HexPlaintext.TabIndex = 0;
            this.richTextBox_HexPlaintext.Text = "";
            this.richTextBox_HexPlaintext.SelectionChanged += new System.EventHandler(this.richTextBox_HexPlaintext_SelectionChanged);
            this.richTextBox_HexPlaintext.TextChanged += new System.EventHandler(this.richTextBox_HexPlaintext_TextChanged);
            // 
            // labelSynthesizedTypeWarn
            // 
            this.labelSynthesizedTypeWarn.AutoSize = true;
            this.labelSynthesizedTypeWarn.Location = new System.Drawing.Point(243, 6);
            this.labelSynthesizedTypeWarn.Name = "labelSynthesizedTypeWarn";
            this.labelSynthesizedTypeWarn.Size = new System.Drawing.Size(402, 13);
            this.labelSynthesizedTypeWarn.TabIndex = 9;
            this.labelSynthesizedTypeWarn.Text = "⚠️ Note: Selected format type will be automatically re-created by Windows if dele" +
    "ted\r\n";
            this.labelSynthesizedTypeWarn.Visible = false;
            // 
            // buttonResetEdit
            // 
            this.buttonResetEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonResetEdit.Enabled = false;
            this.buttonResetEdit.Location = new System.Drawing.Point(890, 3);
            this.buttonResetEdit.Name = "buttonResetEdit";
            this.buttonResetEdit.Size = new System.Drawing.Size(83, 23);
            this.buttonResetEdit.TabIndex = 11;
            this.buttonResetEdit.Text = "Reset Edit";
            this.buttonResetEdit.UseVisualStyleBackColor = true;
            this.buttonResetEdit.Click += new System.EventHandler(this.buttonResetEdit_Click);
            // 
            // buttonApplyEdit
            // 
            this.buttonApplyEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonApplyEdit.Enabled = false;
            this.buttonApplyEdit.Location = new System.Drawing.Point(809, 3);
            this.buttonApplyEdit.Name = "buttonApplyEdit";
            this.buttonApplyEdit.Size = new System.Drawing.Size(75, 23);
            this.buttonApplyEdit.TabIndex = 10;
            this.buttonApplyEdit.Text = "Apply Edit";
            this.buttonApplyEdit.UseVisualStyleBackColor = true;
            this.buttonApplyEdit.Visible = false;
            this.buttonApplyEdit.Click += new System.EventHandler(this.buttonApplyEdit_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "View Mode:";
            // 
            // dropdownContentsViewMode
            // 
            this.dropdownContentsViewMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dropdownContentsViewMode.FormattingEnabled = true;
            this.dropdownContentsViewMode.ImeMode = System.Windows.Forms.ImeMode.Katakana;
            this.dropdownContentsViewMode.Items.AddRange(new object[] {
            "Text",
            "Hex",
            "Hex (Editable)",
            "Object / Struct Details"});
            this.dropdownContentsViewMode.Location = new System.Drawing.Point(75, 3);
            this.dropdownContentsViewMode.Name = "dropdownContentsViewMode";
            this.dropdownContentsViewMode.Size = new System.Drawing.Size(162, 21);
            this.dropdownContentsViewMode.TabIndex = 7;
            this.dropdownContentsViewMode.SelectedIndexChanged += new System.EventHandler(this.dropdownContentsViewMode_SelectedIndexChanged);
            // 
            // labelPendingChanges
            // 
            this.labelPendingChanges.AutoSize = true;
            this.labelPendingChanges.ForeColor = System.Drawing.Color.Firebrick;
            this.labelPendingChanges.Location = new System.Drawing.Point(702, 12);
            this.labelPendingChanges.Name = "labelPendingChanges";
            this.labelPendingChanges.Size = new System.Drawing.Size(277, 13);
            this.labelPendingChanges.TabIndex = 12;
            this.labelPendingChanges.Text = "*Pending Changes - Click Save Icon to apply to clipboard";
            this.labelPendingChanges.Visible = false;
            // 
            // menuHelp_About
            // 
            this.menuHelp_About.Index = 0;
            this.menuHelp_About.Text = "About";
            this.menuHelp_About.Click += new System.EventHandler(this.menuHelp_About_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(993, 629);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.labelPendingChanges);
            this.Controls.Add(this.toolStrip1);
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "Clipboard Manager";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewClipboard)).EndInit();
            this.contextMenuStrip_dataGridView.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            this.splitContainerMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.splitterContainer_InnerTextBoxes.Panel1.ResumeLayout(false);
            this.splitterContainer_InnerTextBoxes.Panel2.ResumeLayout(false);
            this.splitterContainer_InnerTextBoxes.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitterContainer_InnerTextBoxes)).EndInit();
            this.splitterContainer_InnerTextBoxes.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewClipboard;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuMainFile;
        private System.Windows.Forms.MenuItem menuFile_ExportSelectedAsRawHex;
        private System.Windows.Forms.MenuItem menuMainEdit;
        private System.Windows.Forms.MenuItem menuEdit_CopyHexAsText;
        private System.Windows.Forms.MenuItem menuFile_ExportSelectedAsFile;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonRefresh;
        private System.Windows.Forms.ToolStripButton toolStripButtonDelete;
        private System.Windows.Forms.RichTextBox richTextBoxContents;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.ComboBox dropdownContentsViewMode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelSynthesizedTypeWarn;
        private System.Windows.Forms.ToolStripButton toolStripButtonSaveEdited;
        private System.Windows.Forms.ToolStripButton toolStripButtonExportSelected;
        private System.Windows.Forms.MenuItem menuFile_ExportSelectedStruct;
        private System.Windows.Forms.MenuItem menuItemOptions;
        private System.Windows.Forms.MenuItem menuOptions_ShowLargeHex;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button buttonApplyEdit;
        private System.Windows.Forms.Button buttonResetEdit;
        private System.Windows.Forms.Label labelPendingChanges;
        private System.Windows.Forms.MenuItem menuEdit_CopyObjectInfoAsText;
        private System.Windows.Forms.MenuItem menuEdit_CopyEditedHexAsText;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem menuEdit_CopySelectedRows;
        private System.Windows.Forms.MenuItem menuOptions_IncludeRowHeaders;
        private System.Windows.Forms.MenuItem menuOptions_TabSeparation;
        private System.Windows.Forms.MenuItem menuOptions_TableModeMenu;
        private System.Windows.Forms.MenuItem menuOptions_CommaSeparation;
        private System.Windows.Forms.MenuItem menuOptions_PreFormatted;
        private System.Windows.Forms.MenuItem menuEdit_CopyEntireTable;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_dataGridView;
        private System.Windows.Forms.ToolStripMenuItem copyCellToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyRowDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copySelectedRowsNoHeaderToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitterContainer_InnerTextBoxes;
        private System.Windows.Forms.RichTextBox richTextBox_HexPlaintext;
        private System.Windows.Forms.ComboBox dropdownHexToTextEncoding;
        private System.Windows.Forms.Label labelHexToPlaintextEncoding;
        private System.Windows.Forms.CheckBox checkBoxPlainTextEditing;
        private System.Windows.Forms.MenuItem menuItemHelp;
        private System.Windows.Forms.MenuItem menuHelp_About;
    }
}