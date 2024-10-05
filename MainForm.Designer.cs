﻿namespace ClipboardManager
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
            this.menuEdit_CopyAsText = new System.Windows.Forms.MenuItem();
            this.menuMainView = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItemShowLargeHex = new System.Windows.Forms.MenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSaveEdited = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExportSelected = new System.Windows.Forms.ToolStripButton();
            this.richTextBoxContents = new System.Windows.Forms.RichTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.labelSynthesizedTypeWarn = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dropdownContentsViewMode = new System.Windows.Forms.ComboBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.buttonTestResize = new System.Windows.Forms.Button();
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
            this.dataGridViewClipboard.Location = new System.Drawing.Point(4, 5);
            this.dataGridViewClipboard.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dataGridViewClipboard.Name = "dataGridViewClipboard";
            this.dataGridViewClipboard.ReadOnly = true;
            this.dataGridViewClipboard.RowHeadersWidth = 62;
            this.dataGridViewClipboard.Size = new System.Drawing.Size(1457, 439);
            this.dataGridViewClipboard.TabIndex = 0;
            this.dataGridViewClipboard.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewClipboard_CellClick);
            this.dataGridViewClipboard.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewClipboard_CellContentClick);
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuMainFile,
            this.menuMainEdit,
            this.menuMainView,
            this.menuItem1});
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
            this.menuItem_ExportSelectedAsFile.Text = "Export Selected As File (ToDo)";
            this.menuItem_ExportSelectedAsFile.Click += new System.EventHandler(this.menuItem1_Click);
            // 
            // menuItem_ExportSelectedStruct
            // 
            this.menuItem_ExportSelectedStruct.Index = 2;
            this.menuItem_ExportSelectedStruct.Text = "Export Selected Struct Info";
            this.menuItem_ExportSelectedStruct.Click += new System.EventHandler(this.menuItem_ExportSelectedStruct_Click);
            // 
            // menuMainEdit
            // 
            this.menuMainEdit.Index = 1;
            this.menuMainEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuEdit_CopyAsText});
            this.menuMainEdit.Text = "Edit";
            // 
            // menuEdit_CopyAsText
            // 
            this.menuEdit_CopyAsText.Index = 0;
            this.menuEdit_CopyAsText.Text = "Copy As Text (ToDo)";
            this.menuEdit_CopyAsText.Click += new System.EventHandler(this.menuEdit_CopyAsText_Click);
            // 
            // menuMainView
            // 
            this.menuMainView.Index = 2;
            this.menuMainView.Text = "View";
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 3;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemShowLargeHex});
            this.menuItem1.Text = "Options";
            // 
            // menuItemShowLargeHex
            // 
            this.menuItemShowLargeHex.Index = 0;
            this.menuItemShowLargeHex.Text = "Show Hex For Large Files";
            this.menuItemShowLargeHex.Click += new System.EventHandler(this.menuItemShowLargeHex_Click);
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
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.toolStrip1.Size = new System.Drawing.Size(1490, 38);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonRefresh
            // 
            this.toolStripButtonRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRefresh.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRefresh.Image")));
            this.toolStripButtonRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRefresh.Name = "toolStripButtonRefresh";
            this.toolStripButtonRefresh.Size = new System.Drawing.Size(34, 33);
            this.toolStripButtonRefresh.Text = "Refresh";
            this.toolStripButtonRefresh.Click += new System.EventHandler(this.toolStripButtonRefresh_Click);
            // 
            // toolStripButtonDelete
            // 
            this.toolStripButtonDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDelete.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonDelete.Image")));
            this.toolStripButtonDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDelete.Name = "toolStripButtonDelete";
            this.toolStripButtonDelete.Size = new System.Drawing.Size(34, 28);
            this.toolStripButtonDelete.Text = "Delete Item From Clipboard";
            this.toolStripButtonDelete.Click += new System.EventHandler(this.toolStripButtonDelete_Click);
            // 
            // toolStripButtonSaveEdited
            // 
            this.toolStripButtonSaveEdited.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSaveEdited.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSaveEdited.Image")));
            this.toolStripButtonSaveEdited.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSaveEdited.Name = "toolStripButtonSaveEdited";
            this.toolStripButtonSaveEdited.Size = new System.Drawing.Size(34, 28);
            this.toolStripButtonSaveEdited.Text = "Save Edited Clipboard";
            this.toolStripButtonSaveEdited.Click += new System.EventHandler(this.toolStripButtonSaveEdited_Click);
            // 
            // toolStripButtonExportSelected
            // 
            this.toolStripButtonExportSelected.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonExportSelected.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonExportSelected.Image")));
            this.toolStripButtonExportSelected.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExportSelected.Name = "toolStripButtonExportSelected";
            this.toolStripButtonExportSelected.Size = new System.Drawing.Size(34, 28);
            this.toolStripButtonExportSelected.Text = "toolStripButton1";
            this.toolStripButtonExportSelected.Click += new System.EventHandler(this.toolStripButtonExportSelected_Click);
            // 
            // richTextBoxContents
            // 
            this.richTextBoxContents.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxContents.Location = new System.Drawing.Point(4, 46);
            this.richTextBoxContents.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.richTextBoxContents.Name = "richTextBoxContents";
            this.richTextBoxContents.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBoxContents.Size = new System.Drawing.Size(1457, 426);
            this.richTextBoxContents.TabIndex = 4;
            this.richTextBoxContents.Text = "";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Location = new System.Drawing.Point(12, 52);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2.Controls.Add(this.dropdownContentsViewMode);
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxContents);
            this.splitContainer1.Size = new System.Drawing.Size(1465, 902);
            this.splitContainer1.SplitterDistance = 419;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 6;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // labelSynthesizedTypeWarn
            // 
            this.labelSynthesizedTypeWarn.AutoSize = true;
            this.labelSynthesizedTypeWarn.Location = new System.Drawing.Point(383, 13);
            this.labelSynthesizedTypeWarn.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSynthesizedTypeWarn.Name = "labelSynthesizedTypeWarn";
            this.labelSynthesizedTypeWarn.Size = new System.Drawing.Size(594, 20);
            this.labelSynthesizedTypeWarn.TabIndex = 9;
            this.labelSynthesizedTypeWarn.Text = "⚠️ Note: Selected format type will be automatically re-created by Windows if dele" +
    "ted\r\n";
            this.labelSynthesizedTypeWarn.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 13);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 20);
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
            this.dropdownContentsViewMode.Location = new System.Drawing.Point(113, 5);
            this.dropdownContentsViewMode.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dropdownContentsViewMode.Name = "dropdownContentsViewMode";
            this.dropdownContentsViewMode.Size = new System.Drawing.Size(241, 28);
            this.dropdownContentsViewMode.TabIndex = 7;
            this.dropdownContentsViewMode.SelectedIndexChanged += new System.EventHandler(this.dropdownContentsViewMode_SelectedIndexChanged);
            // 
            // buttonTestResize
            // 
            this.buttonTestResize.Location = new System.Drawing.Point(1103, 13);
            this.buttonTestResize.Name = "buttonTestResize";
            this.buttonTestResize.Size = new System.Drawing.Size(75, 31);
            this.buttonTestResize.TabIndex = 7;
            this.buttonTestResize.Text = "AutoSize";
            this.buttonTestResize.UseVisualStyleBackColor = true;
            this.buttonTestResize.Click += new System.EventHandler(this.buttonTestResize_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1490, 968);
            this.Controls.Add(this.buttonTestResize);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
        private System.Windows.Forms.MenuItem menuEdit_CopyAsText;
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
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItemShowLargeHex;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button buttonTestResize;
    }
}