namespace DiskAtlas
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.volumeLabel = new System.Windows.Forms.ToolStripLabel();
            this.volumeComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.scanButton = new System.Windows.Forms.ToolStripButton();
            this.cancelButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.refreshVolumesButton = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.scanProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.folderTreeView = new System.Windows.Forms.TreeView();
            this.contentListView = new System.Windows.Forms.ListView();
            this.nameColumn = new System.Windows.Forms.ColumnHeader();
            this.sizeOnDiskColumn = new System.Windows.Forms.ColumnHeader();
            this.logicalSizeColumn = new System.Windows.Forms.ColumnHeader();
            this.fileCountColumn = new System.Windows.Forms.ColumnHeader();
            this.typeColumn = new System.Windows.Forms.ColumnHeader();
            this.toolStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.volumeLabel,
            this.volumeComboBox,
            this.toolStripSeparator1,
            this.scanButton,
            this.cancelButton,
            this.toolStripSeparator2,
            this.refreshVolumesButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(6, 2, 2, 2);
            this.toolStrip.Size = new System.Drawing.Size(1084, 29);
            this.toolStrip.TabIndex = 0;
            //
            // volumeLabel
            //
            this.volumeLabel.Name = "volumeLabel";
            this.volumeLabel.Size = new System.Drawing.Size(52, 22);
            this.volumeLabel.Text = "Volume:";
            //
            // volumeComboBox
            //
            this.volumeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.volumeComboBox.Name = "volumeComboBox";
            this.volumeComboBox.Size = new System.Drawing.Size(320, 25);
            //
            // toolStripSeparator1
            //
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            //
            // scanButton
            //
            this.scanButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.scanButton.Name = "scanButton";
            this.scanButton.Size = new System.Drawing.Size(41, 22);
            this.scanButton.Text = "Scan";
            this.scanButton.ToolTipText = "Read the Master File Table of the selected volume";
            this.scanButton.Click += new System.EventHandler(this.ScanButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cancelButton.Enabled = false;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(51, 22);
            this.cancelButton.Text = "Cancel";
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            //
            // toolStripSeparator2
            //
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            //
            // refreshVolumesButton
            //
            this.refreshVolumesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.refreshVolumesButton.Name = "refreshVolumesButton";
            this.refreshVolumesButton.Size = new System.Drawing.Size(107, 22);
            this.refreshVolumesButton.Text = "Refresh volumes";
            this.refreshVolumesButton.Click += new System.EventHandler(this.RefreshVolumesButton_Click);
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.scanProgressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 639);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1084, 22);
            this.statusStrip.TabIndex = 2;
            //
            // statusLabel
            //
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(917, 17);
            this.statusLabel.Spring = true;
            this.statusLabel.Text = "Ready";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // scanProgressBar
            //
            this.scanProgressBar.Name = "scanProgressBar";
            this.scanProgressBar.Size = new System.Drawing.Size(150, 16);
            this.scanProgressBar.Visible = false;
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 29);
            this.splitContainer.Name = "splitContainer";
            //
            // splitContainer.Panel1
            //
            this.splitContainer.Panel1.Controls.Add(this.folderTreeView);
            this.splitContainer.Panel1MinSize = 200;
            //
            // splitContainer.Panel2
            //
            this.splitContainer.Panel2.Controls.Add(this.contentListView);
            this.splitContainer.Panel2MinSize = 300;
            this.splitContainer.Size = new System.Drawing.Size(1084, 610);
            this.splitContainer.SplitterDistance = 360;
            this.splitContainer.SplitterWidth = 5;
            this.splitContainer.TabIndex = 1;
            //
            // folderTreeView
            //
            this.folderTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.folderTreeView.HideSelection = false;
            this.folderTreeView.Location = new System.Drawing.Point(0, 0);
            this.folderTreeView.Name = "folderTreeView";
            this.folderTreeView.Size = new System.Drawing.Size(360, 610);
            this.folderTreeView.TabIndex = 0;
            this.folderTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.FolderTreeView_BeforeExpand);
            this.folderTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FolderTreeView_AfterSelect);
            //
            // contentListView
            //
            this.contentListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.nameColumn,
            this.sizeOnDiskColumn,
            this.logicalSizeColumn,
            this.fileCountColumn,
            this.typeColumn});
            this.contentListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentListView.FullRowSelect = true;
            this.contentListView.Location = new System.Drawing.Point(0, 0);
            this.contentListView.Name = "contentListView";
            this.contentListView.Size = new System.Drawing.Size(719, 610);
            this.contentListView.TabIndex = 0;
            this.contentListView.UseCompatibleStateImageBehavior = false;
            this.contentListView.View = System.Windows.Forms.View.Details;
            this.contentListView.DoubleClick += new System.EventHandler(this.ContentListView_DoubleClick);
            //
            // nameColumn
            //
            this.nameColumn.Text = "Name";
            this.nameColumn.Width = 320;
            //
            // sizeOnDiskColumn
            //
            this.sizeOnDiskColumn.Text = "Size on disk";
            this.sizeOnDiskColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.sizeOnDiskColumn.Width = 110;
            //
            // logicalSizeColumn
            //
            this.logicalSizeColumn.Text = "Logical size";
            this.logicalSizeColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.logicalSizeColumn.Width = 110;
            //
            // fileCountColumn
            //
            this.fileCountColumn.Text = "Files";
            this.fileCountColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.fileCountColumn.Width = 90;
            //
            // typeColumn
            //
            this.typeColumn.Text = "Type";
            this.typeColumn.Width = 90;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 661);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.MinimumSize = new System.Drawing.Size(720, 480);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Disk Atlas";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripLabel volumeLabel;
        private System.Windows.Forms.ToolStripComboBox volumeComboBox;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton scanButton;
        private System.Windows.Forms.ToolStripButton cancelButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton refreshVolumesButton;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar scanProgressBar;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TreeView folderTreeView;
        private System.Windows.Forms.ListView contentListView;
        private System.Windows.Forms.ColumnHeader nameColumn;
        private System.Windows.Forms.ColumnHeader sizeOnDiskColumn;
        private System.Windows.Forms.ColumnHeader logicalSizeColumn;
        private System.Windows.Forms.ColumnHeader fileCountColumn;
        private System.Windows.Forms.ColumnHeader typeColumn;
    }
}
