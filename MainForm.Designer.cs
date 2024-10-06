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
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuMainFile = new System.Windows.Forms.MenuItem();
            this.menuFile_ExportAsRawHex = new System.Windows.Forms.MenuItem();
            this.menuItem_ExportSelectedAsFile = new System.Windows.Forms.MenuItem();
            this.menuItem_ExportSelectedStruct = new System.Windows.Forms.MenuItem();
            this.menuMainEdit = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopyObjectInfoAsText = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopyHexAsText = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopyEditedHexAsText = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuEdit_CopySelectedRows = new System.Windows.Forms.MenuItem();
            this.menuMainView = new System.Windows.Forms.MenuItem();
            this.menuItemOptions = new System.Windows.Forms.MenuItem();
            this.menuOptions_ShowLargeHex = new System.Windows.Forms.MenuItem();
            this.menuOptions_IncludeRowHeaders = new System.Windows.Forms.MenuItem();
            this.menuOptions_TableModeMenu = new System.Windows.Forms.MenuItem();
            this.menuOptions_PreFormatted = new System.Windows.Forms.MenuItem();
            this.menuOptions_TabSeparation = new System.Windows.Forms.MenuItem();
            this.menuOptions_CommaSeparation = new System.Windows.Forms.MenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSaveEdited = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExportSelected = new System.Windows.Forms.ToolStripButton();
            this.richTextBoxContents = new System.Windows.Forms.RichTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.labelSynthesizedTypeWarn = new System.Windows.Forms.Label();
            this.buttonResetEdit = new System.Windows.Forms.Button();
            this.buttonApplyEdit = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.dropdownContentsViewMode = new System.Windows.Forms.ComboBox();
            this.labelPendingChanges = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewClipboard)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridViewClipboard
            // 
            this.dataGridViewClipboard.AllowUserToAddRows = false;
            this.dataGridViewClipboard.AllowUserToDeleteRows = false;
            this.dataGridViewClipboard.AllowUserToResizeRows = false;
            this.dataGridViewClipboard.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewClipboard.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewClipboard.Name = "dataGridViewClipboard";
            this.dataGridViewClipboard.ReadOnly = true;
            this.dataGridViewClipboard.RowHeadersWidth = 62;
            this.dataGridViewClipboard.Size = new System.Drawing.Size(971, 266);
            this.dataGridViewClipboard.TabIndex = 0;
            this.dataGridViewClipboard.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewClipboard_CellClick);
            this.dataGridViewClipboard.SelectionChanged += new System.EventHandler(this.dataGridViewClipboard_SelectionChanged);
            this.dataGridViewClipboard.MouseEnter += new System.EventHandler(this.dataGridViewClipboard_MouseEnter);
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuMainFile,
            this.menuMainEdit,
            this.menuMainView,
            this.menuItemOptions});
            // 
            // menuMainFile
            // 
            this.menuMainFile.Index = 0;
            this.menuMainFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuFile_ExportAsRawHex,
            this.menuItem_ExportSelectedAsFile,
            this.menuItem_ExportSelectedStruct});
            this.menuMainFile.Text = "File";
            // 
            // menuFile_ExportAsRawHex
            // 
            this.menuFile_ExportAsRawHex.Index = 0;
            this.menuFile_ExportAsRawHex.Text = "Export Selected As Raw Hex";
            this.menuFile_ExportAsRawHex.Click += new System.EventHandler(this.menuFile_ExportAsRawHex_Click);
            // 
            // menuItem_ExportSelectedAsFile
            // 
            this.menuItem_ExportSelectedAsFile.Index = 1;
            this.menuItem_ExportSelectedAsFile.Text = "Export Selected As File";
            this.menuItem_ExportSelectedAsFile.Click += new System.EventHandler(this.menuItem_ExportSelectedAsFile_Click);
            // 
            // menuItem_ExportSelectedStruct
            // 
            this.menuItem_ExportSelectedStruct.Index = 2;
            this.menuItem_ExportSelectedStruct.Text = "Export Selected Object Info";
            this.menuItem_ExportSelectedStruct.Click += new System.EventHandler(this.menuItem_ExportSelectedStruct_Click);
            // 
            // menuMainEdit
            // 
            this.menuMainEdit.Index = 1;
            this.menuMainEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuEdit_CopyObjectInfoAsText,
            this.menuEdit_CopyHexAsText,
            this.menuEdit_CopyEditedHexAsText,
            this.menuItem3,
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
            // menuEdit_CopySelectedRows
            // 
            this.menuEdit_CopySelectedRows.Index = 4;
            this.menuEdit_CopySelectedRows.Text = "Copy Selected Table Rows";
            this.menuEdit_CopySelectedRows.Click += new System.EventHandler(this.menuEdit_CopySelectedRows_Click);
            // 
            // menuMainView
            // 
            this.menuMainView.Index = 2;
            this.menuMainView.Text = "View";
            // 
            // menuItemOptions
            // 
            this.menuItemOptions.Index = 3;
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
            this.richTextBoxContents.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxContents.Location = new System.Drawing.Point(3, 30);
            this.richTextBoxContents.Name = "richTextBoxContents";
            this.richTextBoxContents.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBoxContents.Size = new System.Drawing.Size(973, 278);
            this.richTextBoxContents.TabIndex = 4;
            this.richTextBoxContents.Text = "";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Location = new System.Drawing.Point(8, 34);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.dataGridViewClipboard);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.labelSynthesizedTypeWarn);
            this.splitContainer1.Panel2.Controls.Add(this.buttonResetEdit);
            this.splitContainer1.Panel2.Controls.Add(this.buttonApplyEdit);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2.Controls.Add(this.dropdownContentsViewMode);
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxContents);
            this.splitContainer1.Size = new System.Drawing.Size(977, 586);
            this.splitContainer1.SplitterDistance = 272;
            this.splitContainer1.TabIndex = 6;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
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
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(993, 629);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.labelPendingChanges);
            this.Controls.Add(this.toolStrip1);
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "Clipboard Manager";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewClipboard)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewClipboard;
        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem menuMainFile;
        private System.Windows.Forms.MenuItem menuFile_ExportAsRawHex;
        private System.Windows.Forms.MenuItem menuMainEdit;
        private System.Windows.Forms.MenuItem menuEdit_CopyHexAsText;
        private System.Windows.Forms.MenuItem menuItem_ExportSelectedAsFile;
        private System.Windows.Forms.MenuItem menuMainView;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonRefresh;
        private System.Windows.Forms.ToolStripButton toolStripButtonDelete;
        private System.Windows.Forms.RichTextBox richTextBoxContents;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ComboBox dropdownContentsViewMode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelSynthesizedTypeWarn;
        private System.Windows.Forms.ToolStripButton toolStripButtonSaveEdited;
        private System.Windows.Forms.ToolStripButton toolStripButtonExportSelected;
        private System.Windows.Forms.MenuItem menuItem_ExportSelectedStruct;
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
    }
}