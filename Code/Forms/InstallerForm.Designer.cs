namespace Flowframes.Forms
{
    partial class InstallerForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallerForm));
            this.pkgList = new System.Windows.Forms.CheckedListBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.logBox = new System.Windows.Forms.TextBox();
            this.downloadPackagesBtn = new System.Windows.Forms.Button();
            this.doneBtn = new System.Windows.Forms.Button();
            this.redownloadPkgsBtn = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.pkgInfoTextbox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // pkgList
            // 
            this.pkgList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.pkgList.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pkgList.ForeColor = System.Drawing.Color.White;
            this.pkgList.FormattingEnabled = true;
            this.pkgList.Location = new System.Drawing.Point(12, 62);
            this.pkgList.Name = "pkgList";
            this.pkgList.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.pkgList.Size = new System.Drawing.Size(398, 292);
            this.pkgList.TabIndex = 0;
            this.pkgList.SelectedIndexChanged += new System.EventHandler(this.pkgList_SelectedIndexChanged);
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Yu Gothic UI", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(229, 40);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.Text = "Package Installer";
            // 
            // logBox
            // 
            this.logBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.logBox.ForeColor = System.Drawing.Color.White;
            this.logBox.Location = new System.Drawing.Point(622, 62);
            this.logBox.MinimumSize = new System.Drawing.Size(4, 21);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.logBox.Size = new System.Drawing.Size(300, 427);
            this.logBox.TabIndex = 6;
            this.logBox.TabStop = false;
            // 
            // downloadPackagesBtn
            // 
            this.downloadPackagesBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.downloadPackagesBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.downloadPackagesBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.downloadPackagesBtn.ForeColor = System.Drawing.Color.White;
            this.downloadPackagesBtn.Location = new System.Drawing.Point(416, 62);
            this.downloadPackagesBtn.Name = "downloadPackagesBtn";
            this.downloadPackagesBtn.Size = new System.Drawing.Size(200, 60);
            this.downloadPackagesBtn.TabIndex = 9;
            this.downloadPackagesBtn.Text = "Install Selected Packages\r\n(Uninstall Deselected Packages)";
            this.toolTip1.SetToolTip(this.downloadPackagesBtn, "This will install ticked packages and uninstall the ones you unticked (if they ar" +
        "e installed).");
            this.downloadPackagesBtn.UseVisualStyleBackColor = false;
            this.downloadPackagesBtn.Click += new System.EventHandler(this.downloadPackagesBtn_Click);
            // 
            // doneBtn
            // 
            this.doneBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.doneBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.doneBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.doneBtn.ForeColor = System.Drawing.Color.White;
            this.doneBtn.Location = new System.Drawing.Point(416, 314);
            this.doneBtn.Name = "doneBtn";
            this.doneBtn.Size = new System.Drawing.Size(200, 40);
            this.doneBtn.TabIndex = 10;
            this.doneBtn.Text = "Done";
            this.doneBtn.UseVisualStyleBackColor = false;
            this.doneBtn.Click += new System.EventHandler(this.doneBtn_Click);
            // 
            // redownloadPkgsBtn
            // 
            this.redownloadPkgsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.redownloadPkgsBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.redownloadPkgsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.redownloadPkgsBtn.ForeColor = System.Drawing.Color.White;
            this.redownloadPkgsBtn.Location = new System.Drawing.Point(416, 128);
            this.redownloadPkgsBtn.Name = "redownloadPkgsBtn";
            this.redownloadPkgsBtn.Size = new System.Drawing.Size(200, 40);
            this.redownloadPkgsBtn.TabIndex = 11;
            this.redownloadPkgsBtn.Text = "Redownload Selected Package";
            this.toolTip1.SetToolTip(this.redownloadPkgsBtn, "This will first uninstall the installed packages and then redownload them.");
            this.redownloadPkgsBtn.UseVisualStyleBackColor = false;
            this.redownloadPkgsBtn.Click += new System.EventHandler(this.redownloadPkgsBtn_Click);
            // 
            // pkgInfoTextbox
            // 
            this.pkgInfoTextbox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.pkgInfoTextbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pkgInfoTextbox.ForeColor = System.Drawing.Color.White;
            this.pkgInfoTextbox.Location = new System.Drawing.Point(12, 360);
            this.pkgInfoTextbox.MinimumSize = new System.Drawing.Size(4, 21);
            this.pkgInfoTextbox.Multiline = true;
            this.pkgInfoTextbox.Name = "pkgInfoTextbox";
            this.pkgInfoTextbox.Size = new System.Drawing.Size(604, 129);
            this.pkgInfoTextbox.TabIndex = 12;
            this.pkgInfoTextbox.TabStop = false;
            this.pkgInfoTextbox.Text = "Friendly Name:\r\nPackage Name:\r\nDownload Size:\r\n\r\nDescription:";
            // 
            // InstallerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(934, 501);
            this.Controls.Add(this.pkgInfoTextbox);
            this.Controls.Add(this.redownloadPkgsBtn);
            this.Controls.Add(this.doneBtn);
            this.Controls.Add(this.downloadPackagesBtn);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.pkgList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "InstallerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Flowframes Package Installer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.InstallerForm_FormClosing);
            this.Load += new System.EventHandler(this.InstallerForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox pkgList;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.Button downloadPackagesBtn;
        private System.Windows.Forms.Button doneBtn;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button redownloadPkgsBtn;
        private System.Windows.Forms.TextBox pkgInfoTextbox;
    }
}