﻿namespace EditClipboardContents
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
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
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItemFile_ExportRegisteredFormats = new System.Windows.Forms.MenuItem();
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
            this.menuHelp_About = new System.Windows.Forms.MenuItem();
            this.menuHelp_WhyTakingLong = new System.Windows.Forms.MenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonFetchManualFormat = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonAddFormat = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSaveEdited = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExportSelected = new System.Windows.Forms.ToolStripButton();
            this.richTextBoxContents = new System.Windows.Forms.RichTextBox();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.labelLoading = new System.Windows.Forms.Label();
            this.labelCustomFormatNameID = new System.Windows.Forms.Label();
            this.checkBoxAutoViewMode = new System.Windows.Forms.CheckBox();
            this.splitterContainer_InnerTextBoxes = new System.Windows.Forms.SplitContainer();
            this.checkBoxPlainTextEditing = new System.Windows.Forms.CheckBox();
            this.dropdownHexToTextEncoding = new System.Windows.Forms.ComboBox();
            this.labelHexToPlaintextEncoding = new System.Windows.Forms.Label();
            this.richTextBox_HexPlaintext = new System.Windows.Forms.RichTextBox();
            this.labelSynthesizedTypeWarn = new System.Windows.Forms.Label();
            this.buttonResetEdit = new System.Windows.Forms.Button();
            this.buttonApplyEdit = new System.Windows.Forms.Button();
            this.labelViewMode = new System.Windows.Forms.Label();
            this.dropdownContentsViewMode = new System.Windows.Forms.ComboBox();
            this.labelPendingChanges = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.labelVersion = new System.Windows.Forms.Label();
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
            this.dataGridViewClipboard.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewClipboard.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewClipboard.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewClipboard.ContextMenuStrip = this.contextMenuStrip_dataGridView;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewClipboard.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewClipboard.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewClipboard.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dataGridViewClipboard.Name = "dataGridViewClipboard";
            this.dataGridViewClipboard.ReadOnly = true;
            this.dataGridViewClipboard.RowHeadersWidth = 62;
            this.dataGridViewClipboard.Size = new System.Drawing.Size(1548, 419);
            this.dataGridViewClipboard.TabIndex = 0;
            this.dataGridViewClipboard.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dataGridViewClipboard_CellBeginEdit);
            this.dataGridViewClipboard.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewClipboard_CellClick);
            this.dataGridViewClipboard.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewClipboard_CellDoubleClick);
            this.dataGridViewClipboard.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewClipboard_CellEndEdit);
            this.dataGridViewClipboard.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewClipboard_CellMouseDown);
            this.dataGridViewClipboard.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewClipboard_ColumnHeaderMouseClick);
            this.dataGridViewClipboard.SelectionChanged += new System.EventHandler(this.dataGridViewClipboard_SelectionChanged);
            this.dataGridViewClipboard.SortCompare += new System.Windows.Forms.DataGridViewSortCompareEventHandler(this.dataGridViewClipboard_SortCompare);
            this.dataGridViewClipboard.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridViewClipboard_KeyDown);
            this.dataGridViewClipboard.MouseEnter += new System.EventHandler(this.dataGridViewClipboard_MouseEnter);
            // 
            // contextMenuStrip_dataGridView
            // 
            this.contextMenuStrip_dataGridView.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip_dataGridView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyCellToolStripMenuItem,
            this.copyRowDataToolStripMenuItem,
            this.copySelectedRowsNoHeaderToolStripMenuItem});
            this.contextMenuStrip_dataGridView.Name = "contextMenuStrip_dataGridView";
            this.contextMenuStrip_dataGridView.Size = new System.Drawing.Size(346, 100);
            this.contextMenuStrip_dataGridView.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_dataGridView_Opening);
            // 
            // copyCellToolStripMenuItem
            // 
            this.copyCellToolStripMenuItem.Name = "copyCellToolStripMenuItem";
            this.copyCellToolStripMenuItem.Size = new System.Drawing.Size(345, 32);
            this.copyCellToolStripMenuItem.Text = "Copy Single Cell";
            this.copyCellToolStripMenuItem.Click += new System.EventHandler(this.copyCellToolStripMenuItem_Click);
            // 
            // copyRowDataToolStripMenuItem
            // 
            this.copyRowDataToolStripMenuItem.Name = "copyRowDataToolStripMenuItem";
            this.copyRowDataToolStripMenuItem.Size = new System.Drawing.Size(345, 32);
            this.copyRowDataToolStripMenuItem.Text = "Copy Selected Rows";
            this.copyRowDataToolStripMenuItem.Click += new System.EventHandler(this.copyRowDataToolStripMenuItem_Click);
            // 
            // copySelectedRowsNoHeaderToolStripMenuItem
            // 
            this.copySelectedRowsNoHeaderToolStripMenuItem.Name = "copySelectedRowsNoHeaderToolStripMenuItem";
            this.copySelectedRowsNoHeaderToolStripMenuItem.Size = new System.Drawing.Size(345, 32);
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
            this.menuFile_ExportSelectedStruct,
            this.menuItem1,
            this.menuItemFile_ExportRegisteredFormats});
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
            // menuItem1
            // 
            this.menuItem1.Index = 3;
            this.menuItem1.Text = "-";
            // 
            // menuItemFile_ExportRegisteredFormats
            // 
            this.menuItemFile_ExportRegisteredFormats.Index = 4;
            this.menuItemFile_ExportRegisteredFormats.Text = "Export All Registered Formats";
            this.menuItemFile_ExportRegisteredFormats.Click += new System.EventHandler(this.menuItemFile_ExportRegisteredFormats_Click);
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
            this.menuHelp_About,
            this.menuHelp_WhyTakingLong});
            this.menuItemHelp.Text = "Help";
            // 
            // menuHelp_About
            // 
            this.menuHelp_About.Index = 0;
            this.menuHelp_About.Text = "About";
            this.menuHelp_About.Click += new System.EventHandler(this.menuHelp_About_Click);
            // 
            // menuHelp_WhyTakingLong
            // 
            this.menuHelp_WhyTakingLong.Index = 1;
            this.menuHelp_WhyTakingLong.Text = "Why Is It Taking So Long?";
            this.menuHelp_WhyTakingLong.Click += new System.EventHandler(this.menuHelp_WhyTakingLong_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(42, 42);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonRefresh,
            this.toolStripButtonFetchManualFormat,
            this.toolStripButtonDelete,
            this.toolStripButtonAddFormat,
            this.toolStripButtonSaveEdited,
            this.toolStripButtonExportSelected});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(12, 0, 3, 0);
            this.toolStrip1.Size = new System.Drawing.Size(1573, 51);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonRefresh
            // 
            this.toolStripButtonRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRefresh.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRefresh.Image")));
            this.toolStripButtonRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRefresh.Name = "toolStripButtonRefresh";
            this.toolStripButtonRefresh.Size = new System.Drawing.Size(46, 46);
            this.toolStripButtonRefresh.Text = "Reload From Clipboard";
            this.toolStripButtonRefresh.Click += new System.EventHandler(this.toolStripButtonRefresh_Click);
            // 
            // toolStripButtonFetchManualFormat
            // 
            this.toolStripButtonFetchManualFormat.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonFetchManualFormat.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonFetchManualFormat.Image")));
            this.toolStripButtonFetchManualFormat.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonFetchManualFormat.Name = "toolStripButtonFetchManualFormat";
            this.toolStripButtonFetchManualFormat.Size = new System.Drawing.Size(46, 46);
            this.toolStripButtonFetchManualFormat.Text = "Manually Fetch Specific Clipboard Format";
            this.toolStripButtonFetchManualFormat.ToolTipText = "Manually fetch or re-fetch a specific format by name or ID";
            this.toolStripButtonFetchManualFormat.Click += new System.EventHandler(this.toolStripButtonFetchManualFormat_Click);
            // 
            // toolStripButtonDelete
            // 
            this.toolStripButtonDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDelete.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonDelete.Image")));
            this.toolStripButtonDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDelete.Name = "toolStripButtonDelete";
            this.toolStripButtonDelete.Size = new System.Drawing.Size(46, 46);
            this.toolStripButtonDelete.Text = "Delete Selected Item From Clipboard";
            this.toolStripButtonDelete.Click += new System.EventHandler(this.toolStripButtonDelete_Click);
            // 
            // toolStripButtonAddFormat
            // 
            this.toolStripButtonAddFormat.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonAddFormat.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonAddFormat.Image")));
            this.toolStripButtonAddFormat.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonAddFormat.Name = "toolStripButtonAddFormat";
            this.toolStripButtonAddFormat.Size = new System.Drawing.Size(46, 46);
            this.toolStripButtonAddFormat.Text = "Add a custom format";
            this.toolStripButtonAddFormat.ToolTipText = "Add a custom format";
            this.toolStripButtonAddFormat.Click += new System.EventHandler(this.toolStripButtonAddFormat_Click);
            // 
            // toolStripButtonSaveEdited
            // 
            this.toolStripButtonSaveEdited.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSaveEdited.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSaveEdited.Image")));
            this.toolStripButtonSaveEdited.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSaveEdited.Name = "toolStripButtonSaveEdited";
            this.toolStripButtonSaveEdited.Size = new System.Drawing.Size(46, 46);
            this.toolStripButtonSaveEdited.Text = "Save Edits To Clipboard";
            this.toolStripButtonSaveEdited.ToolTipText = "Re-Write clipboard with edited data";
            this.toolStripButtonSaveEdited.Click += new System.EventHandler(this.toolStripButtonSaveEdited_Click);
            // 
            // toolStripButtonExportSelected
            // 
            this.toolStripButtonExportSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonExportSelected.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonExportSelected.Image")));
            this.toolStripButtonExportSelected.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExportSelected.Name = "toolStripButtonExportSelected";
            this.toolStripButtonExportSelected.Size = new System.Drawing.Size(46, 46);
            this.toolStripButtonExportSelected.Text = "Export selected item data as file";
            this.toolStripButtonExportSelected.ToolTipText = "Export selected item data as file";
            this.toolStripButtonExportSelected.Click += new System.EventHandler(this.toolStripButtonExportSelected_Click);
            // 
            // richTextBoxContents
            // 
            this.richTextBoxContents.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxContents.DetectUrls = false;
            this.richTextBoxContents.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxContents.HideSelection = false;
            this.richTextBoxContents.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxContents.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.richTextBoxContents.Name = "richTextBoxContents";
            this.richTextBoxContents.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBoxContents.Size = new System.Drawing.Size(968, 429);
            this.richTextBoxContents.TabIndex = 4;
            this.richTextBoxContents.Text = "";
            this.richTextBoxContents.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBoxContents_LinkClicked);
            this.richTextBoxContents.SelectionChanged += new System.EventHandler(this.richTextBoxContents_SelectionChanged);
            this.richTextBoxContents.TextChanged += new System.EventHandler(this.richTextBoxContents_TextChanged);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Location = new System.Drawing.Point(12, 52);
            this.splitContainerMain.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.labelLoading);
            this.splitContainerMain.Panel1.Controls.Add(this.dataGridViewClipboard);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.labelCustomFormatNameID);
            this.splitContainerMain.Panel2.Controls.Add(this.checkBoxAutoViewMode);
            this.splitContainerMain.Panel2.Controls.Add(this.splitterContainer_InnerTextBoxes);
            this.splitContainerMain.Panel2.Controls.Add(this.labelSynthesizedTypeWarn);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonResetEdit);
            this.splitContainerMain.Panel2.Controls.Add(this.buttonApplyEdit);
            this.splitContainerMain.Panel2.Controls.Add(this.labelViewMode);
            this.splitContainerMain.Panel2.Controls.Add(this.dropdownContentsViewMode);
            this.splitContainerMain.Size = new System.Drawing.Size(1548, 902);
            this.splitContainerMain.SplitterDistance = 418;
            this.splitContainerMain.SplitterWidth = 10;
            this.splitContainerMain.TabIndex = 6;
            this.splitContainerMain.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainerMain_SplitterMoved);
            this.splitContainerMain.DoubleClick += new System.EventHandler(this.splitContainerMain_DoubleClick);
            // 
            // labelLoading
            // 
            this.labelLoading.AutoSize = true;
            this.labelLoading.BackColor = System.Drawing.SystemColors.ControlLight;
            this.labelLoading.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLoading.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.labelLoading.Location = new System.Drawing.Point(413, 170);
            this.labelLoading.Name = "labelLoading";
            this.labelLoading.Padding = new System.Windows.Forms.Padding(15);
            this.labelLoading.Size = new System.Drawing.Size(721, 88);
            this.labelLoading.TabIndex = 14;
            this.labelLoading.Text = "Loading Data From Clipboard\r\nSometimes this can take a while (See \"Help\" dropdown" +
    " for why)";
            this.labelLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCustomFormatNameID
            // 
            this.labelCustomFormatNameID.AutoSize = true;
            this.labelCustomFormatNameID.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCustomFormatNameID.ForeColor = System.Drawing.Color.DarkRed;
            this.labelCustomFormatNameID.Location = new System.Drawing.Point(475, 5);
            this.labelCustomFormatNameID.Name = "labelCustomFormatNameID";
            this.labelCustomFormatNameID.Size = new System.Drawing.Size(593, 25);
            this.labelCustomFormatNameID.TabIndex = 15;
            this.labelCustomFormatNameID.Text = "⚠️ Note: You can specify a custom Format Name or ID, but not both";
            this.toolTip1.SetToolTip(this.labelCustomFormatNameID, resources.GetString("labelCustomFormatNameID.ToolTip"));
            this.labelCustomFormatNameID.Visible = false;
            // 
            // checkBoxAutoViewMode
            // 
            this.checkBoxAutoViewMode.AutoSize = true;
            this.checkBoxAutoViewMode.Checked = true;
            this.checkBoxAutoViewMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoViewMode.Location = new System.Drawing.Point(375, 7);
            this.checkBoxAutoViewMode.Name = "checkBoxAutoViewMode";
            this.checkBoxAutoViewMode.Size = new System.Drawing.Size(69, 24);
            this.checkBoxAutoViewMode.TabIndex = 14;
            this.checkBoxAutoViewMode.Text = "Auto";
            this.checkBoxAutoViewMode.UseVisualStyleBackColor = true;
            // 
            // splitterContainer_InnerTextBoxes
            // 
            this.splitterContainer_InnerTextBoxes.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitterContainer_InnerTextBoxes.Location = new System.Drawing.Point(0, 43);
            this.splitterContainer_InnerTextBoxes.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.splitterContainer_InnerTextBoxes.Size = new System.Drawing.Size(1548, 431);
            this.splitterContainer_InnerTextBoxes.SplitterDistance = 966;
            this.splitterContainer_InnerTextBoxes.SplitterWidth = 10;
            this.splitterContainer_InnerTextBoxes.TabIndex = 13;
            this.splitterContainer_InnerTextBoxes.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitterContainer_InnerTextBoxes_SplitterMoved);
            // 
            // checkBoxPlainTextEditing
            // 
            this.checkBoxPlainTextEditing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxPlainTextEditing.AutoSize = true;
            this.checkBoxPlainTextEditing.Location = new System.Drawing.Point(-7, 395);
            this.checkBoxPlainTextEditing.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.checkBoxPlainTextEditing.Name = "checkBoxPlainTextEditing";
            this.checkBoxPlainTextEditing.Size = new System.Drawing.Size(148, 24);
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
            "UTF-16 LE (Unicode)",
            "UTF-16 BE",
            "UTF-32 LE",
            "UTF-32 BE",
            "Codepage 1252",
            "System Default"});
            this.dropdownHexToTextEncoding.Location = new System.Drawing.Point(245, 391);
            this.dropdownHexToTextEncoding.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dropdownHexToTextEncoding.Name = "dropdownHexToTextEncoding";
            this.dropdownHexToTextEncoding.Size = new System.Drawing.Size(217, 28);
            this.dropdownHexToTextEncoding.TabIndex = 2;
            this.dropdownHexToTextEncoding.SelectedIndexChanged += new System.EventHandler(this.dropdownHexToTextEncoding_SelectedIndexChanged);
            // 
            // labelHexToPlaintextEncoding
            // 
            this.labelHexToPlaintextEncoding.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHexToPlaintextEncoding.AutoSize = true;
            this.labelHexToPlaintextEncoding.Location = new System.Drawing.Point(158, 397);
            this.labelHexToPlaintextEncoding.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelHexToPlaintextEncoding.Name = "labelHexToPlaintextEncoding";
            this.labelHexToPlaintextEncoding.Size = new System.Drawing.Size(80, 20);
            this.labelHexToPlaintextEncoding.TabIndex = 1;
            this.labelHexToPlaintextEncoding.Text = "Encoding:";
            // 
            // richTextBox_HexPlaintext
            // 
            this.richTextBox_HexPlaintext.DetectUrls = false;
            this.richTextBox_HexPlaintext.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox_HexPlaintext.HideSelection = false;
            this.richTextBox_HexPlaintext.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_HexPlaintext.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.richTextBox_HexPlaintext.Name = "richTextBox_HexPlaintext";
            this.richTextBox_HexPlaintext.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBox_HexPlaintext.Size = new System.Drawing.Size(572, 376);
            this.richTextBox_HexPlaintext.TabIndex = 0;
            this.richTextBox_HexPlaintext.Text = "";
            this.richTextBox_HexPlaintext.SelectionChanged += new System.EventHandler(this.richTextBox_HexPlaintext_SelectionChanged);
            this.richTextBox_HexPlaintext.TextChanged += new System.EventHandler(this.richTextBox_HexPlaintext_TextChanged);
            // 
            // labelSynthesizedTypeWarn
            // 
            this.labelSynthesizedTypeWarn.AutoSize = true;
            this.labelSynthesizedTypeWarn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSynthesizedTypeWarn.Location = new System.Drawing.Point(451, 9);
            this.labelSynthesizedTypeWarn.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSynthesizedTypeWarn.Name = "labelSynthesizedTypeWarn";
            this.labelSynthesizedTypeWarn.Size = new System.Drawing.Size(171, 20);
            this.labelSynthesizedTypeWarn.TabIndex = 9;
            this.labelSynthesizedTypeWarn.Text = "⚠️ Synthesized Format";
            this.toolTip1.SetToolTip(this.labelSynthesizedTypeWarn, resources.GetString("labelSynthesizedTypeWarn.ToolTip"));
            this.labelSynthesizedTypeWarn.Visible = false;
            // 
            // buttonResetEdit
            // 
            this.buttonResetEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonResetEdit.Enabled = false;
            this.buttonResetEdit.Location = new System.Drawing.Point(1417, 2);
            this.buttonResetEdit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonResetEdit.Name = "buttonResetEdit";
            this.buttonResetEdit.Size = new System.Drawing.Size(124, 35);
            this.buttonResetEdit.TabIndex = 11;
            this.buttonResetEdit.Text = "Reset Edit";
            this.buttonResetEdit.UseVisualStyleBackColor = true;
            this.buttonResetEdit.Click += new System.EventHandler(this.buttonResetEdit_Click);
            // 
            // buttonApplyEdit
            // 
            this.buttonApplyEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonApplyEdit.Enabled = false;
            this.buttonApplyEdit.Location = new System.Drawing.Point(1296, 2);
            this.buttonApplyEdit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonApplyEdit.Name = "buttonApplyEdit";
            this.buttonApplyEdit.Size = new System.Drawing.Size(112, 35);
            this.buttonApplyEdit.TabIndex = 10;
            this.buttonApplyEdit.Text = "Apply Edit";
            this.buttonApplyEdit.UseVisualStyleBackColor = true;
            this.buttonApplyEdit.Visible = false;
            this.buttonApplyEdit.Click += new System.EventHandler(this.buttonApplyEdit_Click);
            // 
            // labelViewMode
            // 
            this.labelViewMode.AutoSize = true;
            this.labelViewMode.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelViewMode.Location = new System.Drawing.Point(4, 5);
            this.labelViewMode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelViewMode.Name = "labelViewMode";
            this.labelViewMode.Size = new System.Drawing.Size(116, 25);
            this.labelViewMode.TabIndex = 8;
            this.labelViewMode.Text = "View Mode:";
            // 
            // dropdownContentsViewMode
            // 
            this.dropdownContentsViewMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dropdownContentsViewMode.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dropdownContentsViewMode.FormattingEnabled = true;
            this.dropdownContentsViewMode.ImeMode = System.Windows.Forms.ImeMode.Katakana;
            this.dropdownContentsViewMode.Items.AddRange(new object[] {
            "Text",
            "Hex",
            "Hex (Editable)",
            "Object / Struct Details"});
            this.dropdownContentsViewMode.Location = new System.Drawing.Point(122, 2);
            this.dropdownContentsViewMode.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dropdownContentsViewMode.Name = "dropdownContentsViewMode";
            this.dropdownContentsViewMode.Size = new System.Drawing.Size(241, 30);
            this.dropdownContentsViewMode.TabIndex = 7;
            this.dropdownContentsViewMode.SelectedIndexChanged += new System.EventHandler(this.dropdownContentsViewMode_SelectedIndexChanged);
            // 
            // labelPendingChanges
            // 
            this.labelPendingChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPendingChanges.AutoSize = true;
            this.labelPendingChanges.BackColor = System.Drawing.Color.Transparent;
            this.labelPendingChanges.ForeColor = System.Drawing.Color.Firebrick;
            this.labelPendingChanges.Location = new System.Drawing.Point(989, 18);
            this.labelPendingChanges.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelPendingChanges.Name = "labelPendingChanges";
            this.labelPendingChanges.Size = new System.Drawing.Size(407, 20);
            this.labelPendingChanges.TabIndex = 12;
            this.labelPendingChanges.Text = "*Pending Changes - Click Save Icon to apply to clipboard";
            this.labelPendingChanges.Visible = false;
            // 
            // labelVersion
            // 
            this.labelVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelVersion.AutoSize = true;
            this.labelVersion.BackColor = System.Drawing.Color.Transparent;
            this.labelVersion.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.labelVersion.Location = new System.Drawing.Point(1454, 18);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(67, 20);
            this.labelVersion.TabIndex = 13;
            this.labelVersion.Text = "Version:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1573, 968);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.labelPendingChanges);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "Edit Clipboard Contents";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewClipboard)).EndInit();
            this.contextMenuStrip_dataGridView.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel1.PerformLayout();
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
        private System.Windows.Forms.Label labelViewMode;
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
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.CheckBox checkBoxAutoViewMode;
        private System.Windows.Forms.Label labelLoading;
        private System.Windows.Forms.MenuItem menuHelp_WhyTakingLong;
        private System.Windows.Forms.ToolStripButton toolStripButtonAddFormat;
        private System.Windows.Forms.Label labelCustomFormatNameID;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItemFile_ExportRegisteredFormats;
        private System.Windows.Forms.ToolStripButton toolStripButtonFetchManualFormat;
    }
}