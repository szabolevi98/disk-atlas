namespace DiskAtlas
{
    partial class AboutForm
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
            this.iconBox = new System.Windows.Forms.PictureBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.separatorPanel = new System.Windows.Forms.Panel();
            this.copyrightLabel = new System.Windows.Forms.Label();
            this.licenseLabel = new System.Windows.Forms.Label();
            this.repositoryLink = new System.Windows.Forms.LinkLabel();
            this.closeButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.iconBox)).BeginInit();
            this.SuspendLayout();
            //
            // iconBox
            //
            this.iconBox.BackColor = System.Drawing.Color.Transparent;
            this.iconBox.Location = new System.Drawing.Point(28, 28);
            this.iconBox.Name = "iconBox";
            this.iconBox.Size = new System.Drawing.Size(56, 56);
            this.iconBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.iconBox.TabIndex = 0;
            this.iconBox.TabStop = false;
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(246)))));
            this.titleLabel.Location = new System.Drawing.Point(102, 28);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(120, 30);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.Text = "Disk Atlas";
            //
            // versionLabel
            //
            this.versionLabel.AutoSize = true;
            this.versionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.versionLabel.Location = new System.Drawing.Point(104, 62);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(80, 15);
            this.versionLabel.TabIndex = 2;
            this.versionLabel.Text = "Version 1.0.0";
            //
            // descriptionLabel
            //
            this.descriptionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(147)))), ((int)(((byte)(164)))), ((int)(((byte)(186)))));
            this.descriptionLabel.Location = new System.Drawing.Point(28, 104);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(428, 40);
            this.descriptionLabel.TabIndex = 3;
            this.descriptionLabel.Text = "Maps an NTFS volume in seconds by reading the Master File Table directly, instead " +
    "of walking one directory at a time.";
            //
            // separatorPanel
            //
            this.separatorPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(55)))), ((int)(((byte)(74)))));
            this.separatorPanel.Location = new System.Drawing.Point(28, 158);
            this.separatorPanel.Name = "separatorPanel";
            this.separatorPanel.Size = new System.Drawing.Size(428, 1);
            this.separatorPanel.TabIndex = 4;
            //
            // copyrightLabel
            //
            this.copyrightLabel.AutoSize = true;
            this.copyrightLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(147)))), ((int)(((byte)(164)))), ((int)(((byte)(186)))));
            this.copyrightLabel.Location = new System.Drawing.Point(28, 176);
            this.copyrightLabel.Name = "copyrightLabel";
            this.copyrightLabel.Size = new System.Drawing.Size(180, 15);
            this.copyrightLabel.TabIndex = 5;
            this.copyrightLabel.Text = "Copyright © 2026 szabolevi98";
            //
            // licenseLabel
            //
            this.licenseLabel.AutoSize = true;
            this.licenseLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.licenseLabel.Location = new System.Drawing.Point(28, 198);
            this.licenseLabel.Name = "licenseLabel";
            this.licenseLabel.Size = new System.Drawing.Size(74, 15);
            this.licenseLabel.TabIndex = 6;
            this.licenseLabel.Text = "MIT License";
            //
            // repositoryLink
            //
            this.repositoryLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(184)))), ((int)(((byte)(166)))));
            this.repositoryLink.AutoSize = true;
            this.repositoryLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.repositoryLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(212)))), ((int)(((byte)(191)))));
            this.repositoryLink.Location = new System.Drawing.Point(28, 220);
            this.repositoryLink.Name = "repositoryLink";
            this.repositoryLink.Size = new System.Drawing.Size(240, 15);
            this.repositoryLink.TabIndex = 7;
            this.repositoryLink.TabStop = true;
            this.repositoryLink.Text = "https://github.com/szabolevi98/disk-atlas";
            this.repositoryLink.VisitedLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(212)))), ((int)(((byte)(191)))));
            this.repositoryLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RepositoryLink_LinkClicked);
            //
            // closeButton
            //
            this.closeButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(40)))), ((int)(((byte)(56)))));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(246)))));
            this.closeButton.Location = new System.Drawing.Point(372, 214);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(84, 30);
            this.closeButton.TabIndex = 8;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = false;
            //
            // AboutForm
            //
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(22)))), ((int)(((byte)(32)))));
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(484, 272);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.repositoryLink);
            this.Controls.Add(this.licenseLabel);
            this.Controls.Add(this.copyrightLabel);
            this.Controls.Add(this.separatorPanel);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.iconBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About Disk Atlas";
            ((System.ComponentModel.ISupportInitialize)(this.iconBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox iconBox;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.Panel separatorPanel;
        private System.Windows.Forms.Label copyrightLabel;
        private System.Windows.Forms.Label licenseLabel;
        private System.Windows.Forms.LinkLabel repositoryLink;
        private System.Windows.Forms.Button closeButton;
    }
}
