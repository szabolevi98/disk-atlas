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
            this.headerPanel = new System.Windows.Forms.Panel();
            this.cancelScanButton = new System.Windows.Forms.Button();
            this.scanButton = new System.Windows.Forms.Button();
            this.volumeComboBox = new System.Windows.Forms.ComboBox();
            this.subtitleLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.progressStripe = new DiskAtlas.Controls.ProgressStripe();
            this.statsBar = new DiskAtlas.Controls.StatsBar();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.folderTreeView = new System.Windows.Forms.TreeView();
            this.rightSplitContainer = new System.Windows.Forms.SplitContainer();
            this.treemapControl = new DiskAtlas.Controls.TreemapControl();
            this.contentListView = new System.Windows.Forms.ListView();
            this.nameColumn = new System.Windows.Forms.ColumnHeader();
            this.sizeOnDiskColumn = new System.Windows.Forms.ColumnHeader();
            this.shareColumn = new System.Windows.Forms.ColumnHeader();
            this.logicalSizeColumn = new System.Windows.Forms.ColumnHeader();
            this.fileCountColumn = new System.Windows.Forms.ColumnHeader();
            this.typeColumn = new System.Windows.Forms.ColumnHeader();
            this.legendControl = new DiskAtlas.Controls.LegendControl();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.hoverLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rightSplitContainer)).BeginInit();
            this.rightSplitContainer.Panel1.SuspendLayout();
            this.rightSplitContainer.Panel2.SuspendLayout();
            this.rightSplitContainer.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // headerPanel
            //
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(31)))), ((int)(((byte)(44)))));
            this.headerPanel.Controls.Add(this.cancelScanButton);
            this.headerPanel.Controls.Add(this.scanButton);
            this.headerPanel.Controls.Add(this.volumeComboBox);
            this.headerPanel.Controls.Add(this.subtitleLabel);
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(1180, 68);
            this.headerPanel.TabIndex = 0;
            //
            // cancelScanButton
            //
            this.cancelScanButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelScanButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(40)))), ((int)(((byte)(56)))));
            this.cancelScanButton.Enabled = false;
            this.cancelScanButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelScanButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(147)))), ((int)(((byte)(164)))), ((int)(((byte)(186)))));
            this.cancelScanButton.Location = new System.Drawing.Point(1064, 20);
            this.cancelScanButton.Name = "cancelScanButton";
            this.cancelScanButton.Size = new System.Drawing.Size(92, 30);
            this.cancelScanButton.TabIndex = 4;
            this.cancelScanButton.Text = "Cancel";
            this.cancelScanButton.UseVisualStyleBackColor = false;
            this.cancelScanButton.Click += new System.EventHandler(this.CancelScanButton_Click);
            //
            // scanButton
            //
            this.scanButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.scanButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(212)))), ((int)(((byte)(191)))));
            this.scanButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.scanButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(9)))), ((int)(((byte)(20)))), ((int)(((byte)(28)))));
            this.scanButton.Location = new System.Drawing.Point(956, 20);
            this.scanButton.Name = "scanButton";
            this.scanButton.Size = new System.Drawing.Size(100, 30);
            this.scanButton.TabIndex = 3;
            this.scanButton.Text = "Scan volume";
            this.scanButton.UseVisualStyleBackColor = false;
            this.scanButton.Click += new System.EventHandler(this.ScanButton_Click);
            //
            // volumeComboBox
            //
            this.volumeComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.volumeComboBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(40)))), ((int)(((byte)(56)))));
            this.volumeComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.volumeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.volumeComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.volumeComboBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(246)))));
            this.volumeComboBox.ItemHeight = 24;
            this.volumeComboBox.Location = new System.Drawing.Point(604, 22);
            this.volumeComboBox.Name = "volumeComboBox";
            this.volumeComboBox.Size = new System.Drawing.Size(340, 30);
            this.volumeComboBox.TabIndex = 2;
            this.volumeComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.VolumeComboBox_DrawItem);
            //
            // subtitleLabel
            //
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.subtitleLabel.Location = new System.Drawing.Point(26, 40);
            this.subtitleLabel.Name = "subtitleLabel";
            this.subtitleLabel.Size = new System.Drawing.Size(215, 15);
            this.subtitleLabel.TabIndex = 1;
            this.subtitleLabel.Text = "NTFS Master File Table analyzer";
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(246)))));
            this.titleLabel.Location = new System.Drawing.Point(24, 13);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(120, 25);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Disk Atlas";
            //
            // progressStripe
            //
            this.progressStripe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(55)))), ((int)(((byte)(74)))));
            this.progressStripe.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressStripe.Location = new System.Drawing.Point(0, 68);
            this.progressStripe.Name = "progressStripe";
            this.progressStripe.Size = new System.Drawing.Size(1180, 3);
            this.progressStripe.TabIndex = 1;
            //
            // statsBar
            //
            this.statsBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(22)))), ((int)(((byte)(32)))));
            this.statsBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.statsBar.Location = new System.Drawing.Point(0, 71);
            this.statsBar.Name = "statsBar";
            this.statsBar.Padding = new System.Windows.Forms.Padding(16, 12, 16, 6);
            this.statsBar.Size = new System.Drawing.Size(1180, 78);
            this.statsBar.TabIndex = 2;
            //
            // mainSplitContainer
            //
            this.mainSplitContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(22)))), ((int)(((byte)(32)))));
            this.mainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 149);
            this.mainSplitContainer.Name = "mainSplitContainer";
            //
            // mainSplitContainer.Panel1
            //
            this.mainSplitContainer.Panel1.Controls.Add(this.folderTreeView);
            this.mainSplitContainer.Panel1.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.mainSplitContainer.Panel1MinSize = 220;
            //
            // mainSplitContainer.Panel2
            //
            this.mainSplitContainer.Panel2.Controls.Add(this.rightSplitContainer);
            this.mainSplitContainer.Panel2.Padding = new System.Windows.Forms.Padding(0, 0, 16, 0);
            this.mainSplitContainer.Panel2MinSize = 360;
            this.mainSplitContainer.Size = new System.Drawing.Size(1180, 488);
            this.mainSplitContainer.SplitterDistance = 392;
            this.mainSplitContainer.SplitterWidth = 10;
            this.mainSplitContainer.TabIndex = 3;
            //
            // folderTreeView
            //
            this.folderTreeView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(31)))), ((int)(((byte)(44)))));
            this.folderTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.folderTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.folderTreeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll;
            this.folderTreeView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(246)))));
            this.folderTreeView.FullRowSelect = true;
            this.folderTreeView.HideSelection = false;
            this.folderTreeView.Indent = 18;
            this.folderTreeView.ItemHeight = 26;
            this.folderTreeView.Location = new System.Drawing.Point(16, 0);
            this.folderTreeView.Name = "folderTreeView";
            this.folderTreeView.ShowLines = false;
            this.folderTreeView.ShowPlusMinus = false;
            this.folderTreeView.ShowRootLines = false;
            this.folderTreeView.Size = new System.Drawing.Size(376, 488);
            this.folderTreeView.TabIndex = 0;
            this.folderTreeView.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.FolderTreeView_DrawNode);
            this.folderTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.FolderTreeView_BeforeExpand);
            this.folderTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FolderTreeView_AfterSelect);
            this.folderTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FolderTreeView_MouseDown);
            //
            // rightSplitContainer
            //
            this.rightSplitContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(22)))), ((int)(((byte)(32)))));
            this.rightSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.rightSplitContainer.Name = "rightSplitContainer";
            this.rightSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            //
            // rightSplitContainer.Panel1
            //
            this.rightSplitContainer.Panel1.Controls.Add(this.treemapControl);
            this.rightSplitContainer.Panel1MinSize = 160;
            //
            // rightSplitContainer.Panel2
            //
            this.rightSplitContainer.Panel2.Controls.Add(this.contentListView);
            this.rightSplitContainer.Panel2MinSize = 120;
            this.rightSplitContainer.Size = new System.Drawing.Size(762, 488);
            this.rightSplitContainer.SplitterDistance = 300;
            this.rightSplitContainer.SplitterWidth = 10;
            this.rightSplitContainer.TabIndex = 0;
            //
            // treemapControl
            //
            this.treemapControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(22)))), ((int)(((byte)(32)))));
            this.treemapControl.Cursor = System.Windows.Forms.Cursors.Cross;
            this.treemapControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treemapControl.Location = new System.Drawing.Point(0, 0);
            this.treemapControl.Name = "treemapControl";
            this.treemapControl.Size = new System.Drawing.Size(762, 300);
            this.treemapControl.TabIndex = 0;
            this.treemapControl.NodeActivated += new System.EventHandler<DiskAtlas.Model.DiskNode>(this.TreemapControl_NodeActivated);
            this.treemapControl.NodeHovered += new System.EventHandler<DiskAtlas.Model.DiskNode>(this.TreemapControl_NodeHovered);
            //
            // contentListView
            //
            this.contentListView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(31)))), ((int)(((byte)(44)))));
            this.contentListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.contentListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.nameColumn,
            this.sizeOnDiskColumn,
            this.shareColumn,
            this.logicalSizeColumn,
            this.fileCountColumn,
            this.typeColumn});
            this.contentListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentListView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(246)))));
            this.contentListView.FullRowSelect = true;
            this.contentListView.Location = new System.Drawing.Point(0, 0);
            this.contentListView.Name = "contentListView";
            this.contentListView.OwnerDraw = true;
            this.contentListView.Size = new System.Drawing.Size(762, 178);
            this.contentListView.TabIndex = 0;
            this.contentListView.UseCompatibleStateImageBehavior = false;
            this.contentListView.View = System.Windows.Forms.View.Details;
            this.contentListView.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ContentListView_DrawColumnHeader);
            this.contentListView.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.ContentListView_DrawItem);
            this.contentListView.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ContentListView_DrawSubItem);
            this.contentListView.DoubleClick += new System.EventHandler(this.ContentListView_DoubleClick);
            //
            // nameColumn
            //
            this.nameColumn.Text = "Name";
            this.nameColumn.Width = 300;
            //
            // sizeOnDiskColumn
            //
            this.sizeOnDiskColumn.Text = "Size on disk";
            this.sizeOnDiskColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.sizeOnDiskColumn.Width = 110;
            //
            // shareColumn
            //
            this.shareColumn.Text = "Share";
            this.shareColumn.Width = 120;
            //
            // logicalSizeColumn
            //
            this.logicalSizeColumn.Text = "Logical size";
            this.logicalSizeColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.logicalSizeColumn.Width = 100;
            //
            // fileCountColumn
            //
            this.fileCountColumn.Text = "Files";
            this.fileCountColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.fileCountColumn.Width = 80;
            //
            // typeColumn
            //
            this.typeColumn.Text = "Type";
            this.typeColumn.Width = 110;
            //
            // legendControl
            //
            this.legendControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(31)))), ((int)(((byte)(44)))));
            this.legendControl.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.legendControl.Location = new System.Drawing.Point(0, 637);
            this.legendControl.Name = "legendControl";
            this.legendControl.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
            this.legendControl.Size = new System.Drawing.Size(1180, 30);
            this.legendControl.TabIndex = 4;
            //
            // statusPanel
            //
            this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(22)))), ((int)(((byte)(32)))));
            this.statusPanel.Controls.Add(this.hoverLabel);
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusPanel.Location = new System.Drawing.Point(0, 667);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Size = new System.Drawing.Size(1180, 28);
            this.statusPanel.TabIndex = 5;
            //
            // hoverLabel
            //
            this.hoverLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))));
            this.hoverLabel.AutoEllipsis = true;
            this.hoverLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(147)))), ((int)(((byte)(164)))), ((int)(((byte)(186)))));
            this.hoverLabel.Location = new System.Drawing.Point(560, 7);
            this.hoverLabel.Name = "hoverLabel";
            this.hoverLabel.Size = new System.Drawing.Size(600, 15);
            this.hoverLabel.TabIndex = 1;
            this.hoverLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // statusLabel
            //
            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.statusLabel.Location = new System.Drawing.Point(20, 7);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(520, 15);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Ready";
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(22)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(1180, 695);
            this.Controls.Add(this.mainSplitContainer);
            this.Controls.Add(this.statsBar);
            this.Controls.Add(this.legendControl);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.progressStripe);
            this.Controls.Add(this.headerPanel);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(246)))));
            this.MinimumSize = new System.Drawing.Size(900, 560);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Disk Atlas";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
            this.mainSplitContainer.ResumeLayout(false);
            this.rightSplitContainer.Panel1.ResumeLayout(false);
            this.rightSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.rightSplitContainer)).EndInit();
            this.rightSplitContainer.ResumeLayout(false);
            this.statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.ComboBox volumeComboBox;
        private System.Windows.Forms.Button scanButton;
        private System.Windows.Forms.Button cancelScanButton;
        private DiskAtlas.Controls.ProgressStripe progressStripe;
        private DiskAtlas.Controls.StatsBar statsBar;
        private System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.TreeView folderTreeView;
        private System.Windows.Forms.SplitContainer rightSplitContainer;
        private DiskAtlas.Controls.TreemapControl treemapControl;
        private System.Windows.Forms.ListView contentListView;
        private System.Windows.Forms.ColumnHeader nameColumn;
        private System.Windows.Forms.ColumnHeader sizeOnDiskColumn;
        private System.Windows.Forms.ColumnHeader shareColumn;
        private System.Windows.Forms.ColumnHeader logicalSizeColumn;
        private System.Windows.Forms.ColumnHeader fileCountColumn;
        private System.Windows.Forms.ColumnHeader typeColumn;
        private DiskAtlas.Controls.LegendControl legendControl;
        private System.Windows.Forms.Panel statusPanel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label hoverLabel;
    }
}
