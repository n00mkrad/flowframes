namespace Flowframes.Forms
{
    partial class UpdaterForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdaterForm));
            this.titleLabel = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.updatePatreonBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.installedLabel = new System.Windows.Forms.Label();
            this.latestLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.downloadingLabel = new System.Windows.Forms.Label();
            this.updateFreeBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Yu Gothic UI", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(121, 40);
            this.titleLabel.TabIndex = 2;
            this.titleLabel.Text = "Updater";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(17, 67);
            this.label13.Margin = new System.Windows.Forms.Padding(8, 8, 3, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(110, 16);
            this.label13.TabIndex = 35;
            this.label13.Text = "Installed Version:";
            // 
            // updatePatreonBtn
            // 
            this.updatePatreonBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.updatePatreonBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.updatePatreonBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.updatePatreonBtn.ForeColor = System.Drawing.Color.White;
            this.updatePatreonBtn.Location = new System.Drawing.Point(12, 229);
            this.updatePatreonBtn.Name = "updatePatreonBtn";
            this.updatePatreonBtn.Size = new System.Drawing.Size(203, 40);
            this.updatePatreonBtn.TabIndex = 36;
            this.updatePatreonBtn.Text = "Download Patreon Version";
            this.updatePatreonBtn.UseVisualStyleBackColor = true;
            this.updatePatreonBtn.Click += new System.EventHandler(this.updatePatreonBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(16, 93);
            this.label1.Margin = new System.Windows.Forms.Padding(8, 10, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 16);
            this.label1.TabIndex = 37;
            this.label1.Text = "Latest Version:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(16, 119);
            this.label2.Margin = new System.Windows.Forms.Padding(8, 10, 3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 16);
            this.label2.TabIndex = 38;
            this.label2.Text = "Status:";
            // 
            // installedLabel
            // 
            this.installedLabel.AutoSize = true;
            this.installedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.installedLabel.ForeColor = System.Drawing.Color.White;
            this.installedLabel.Location = new System.Drawing.Point(170, 67);
            this.installedLabel.Margin = new System.Windows.Forms.Padding(8, 8, 3, 0);
            this.installedLabel.Name = "installedLabel";
            this.installedLabel.Size = new System.Drawing.Size(76, 16);
            this.installedLabel.TabIndex = 39;
            this.installedLabel.Text = "Loading...";
            // 
            // latestLabel
            // 
            this.latestLabel.AutoSize = true;
            this.latestLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.latestLabel.ForeColor = System.Drawing.Color.White;
            this.latestLabel.Location = new System.Drawing.Point(170, 93);
            this.latestLabel.Margin = new System.Windows.Forms.Padding(8, 8, 3, 0);
            this.latestLabel.Name = "latestLabel";
            this.latestLabel.Size = new System.Drawing.Size(76, 16);
            this.latestLabel.TabIndex = 40;
            this.latestLabel.Text = "Loading...";
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusLabel.ForeColor = System.Drawing.Color.White;
            this.statusLabel.Location = new System.Drawing.Point(170, 119);
            this.statusLabel.Margin = new System.Windows.Forms.Padding(8, 8, 3, 0);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(76, 16);
            this.statusLabel.TabIndex = 41;
            this.statusLabel.Text = "Loading...";
            // 
            // downloadingLabel
            // 
            this.downloadingLabel.AutoSize = true;
            this.downloadingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.downloadingLabel.ForeColor = System.Drawing.Color.White;
            this.downloadingLabel.Location = new System.Drawing.Point(226, 241);
            this.downloadingLabel.Margin = new System.Windows.Forms.Padding(8, 10, 3, 0);
            this.downloadingLabel.Name = "downloadingLabel";
            this.downloadingLabel.Size = new System.Drawing.Size(0, 16);
            this.downloadingLabel.TabIndex = 42;
            // 
            // updateFreeBtn
            // 
            this.updateFreeBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.updateFreeBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.updateFreeBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.updateFreeBtn.ForeColor = System.Drawing.Color.White;
            this.updateFreeBtn.Location = new System.Drawing.Point(221, 229);
            this.updateFreeBtn.Name = "updateFreeBtn";
            this.updateFreeBtn.Size = new System.Drawing.Size(203, 40);
            this.updateFreeBtn.TabIndex = 43;
            this.updateFreeBtn.Text = "Download Free Version";
            this.updateFreeBtn.UseVisualStyleBackColor = true;
            this.updateFreeBtn.Click += new System.EventHandler(this.updateFreeBtn_Click);
            // 
            // UpdaterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(624, 281);
            this.Controls.Add(this.updateFreeBtn);
            this.Controls.Add(this.downloadingLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.latestLabel);
            this.Controls.Add(this.installedLabel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.updatePatreonBtn);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.titleLabel);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UpdaterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Updater";
            this.Load += new System.EventHandler(this.UpdaterForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button updatePatreonBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label installedLabel;
        private System.Windows.Forms.Label latestLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label downloadingLabel;
        private System.Windows.Forms.Button updateFreeBtn;
    }
}