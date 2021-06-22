
namespace Flowframes.Forms
{
    partial class ModelDownloadForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModelDownloadForm));
            this.longProgBar = new HTAlt.WinForms.HTProgressBar();
            this.downloadModelsBtn = new HTAlt.WinForms.HTButton();
            this.titleLabel = new System.Windows.Forms.Label();
            this.progressCircle = new CircularProgressBar.CircularProgressBar();
            this.label39 = new System.Windows.Forms.Label();
            this.closeBtn = new HTAlt.WinForms.HTButton();
            this.cancelBtn = new HTAlt.WinForms.HTButton();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // longProgBar
            // 
            this.longProgBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.longProgBar.BorderThickness = 0;
            this.longProgBar.Location = new System.Drawing.Point(12, 254);
            this.longProgBar.Name = "longProgBar";
            this.longProgBar.Size = new System.Drawing.Size(600, 15);
            this.longProgBar.TabIndex = 34;
            // 
            // downloadModelsBtn
            // 
            this.downloadModelsBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.downloadModelsBtn.FlatAppearance.BorderSize = 0;
            this.downloadModelsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.downloadModelsBtn.ForeColor = System.Drawing.Color.White;
            this.downloadModelsBtn.Location = new System.Drawing.Point(12, 219);
            this.downloadModelsBtn.Name = "downloadModelsBtn";
            this.downloadModelsBtn.Size = new System.Drawing.Size(150, 23);
            this.downloadModelsBtn.TabIndex = 87;
            this.downloadModelsBtn.Text = "Download All Model Files";
            this.downloadModelsBtn.UseVisualStyleBackColor = false;
            this.downloadModelsBtn.Click += new System.EventHandler(this.downloadModelsBtn_Click);
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Yu Gothic UI", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(262, 40);
            this.titleLabel.TabIndex = 89;
            this.titleLabel.Text = "Model Downloader";
            // 
            // progressCircle
            // 
            this.progressCircle.AnimationFunction = WinFormAnimation.KnownAnimationFunctions.Liner;
            this.progressCircle.AnimationSpeed = 500;
            this.progressCircle.BackColor = System.Drawing.Color.Transparent;
            this.progressCircle.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Bold);
            this.progressCircle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.progressCircle.InnerColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.progressCircle.InnerMargin = 2;
            this.progressCircle.InnerWidth = -1;
            this.progressCircle.Location = new System.Drawing.Point(572, 12);
            this.progressCircle.MarqueeAnimationSpeed = 2000;
            this.progressCircle.Name = "progressCircle";
            this.progressCircle.OuterColor = System.Drawing.Color.Gray;
            this.progressCircle.OuterMargin = -21;
            this.progressCircle.OuterWidth = 21;
            this.progressCircle.ProgressColor = System.Drawing.Color.LimeGreen;
            this.progressCircle.ProgressWidth = 8;
            this.progressCircle.SecondaryFont = new System.Drawing.Font("Microsoft Sans Serif", 36F);
            this.progressCircle.Size = new System.Drawing.Size(40, 40);
            this.progressCircle.StartAngle = 270;
            this.progressCircle.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressCircle.SubscriptColor = System.Drawing.Color.FromArgb(((int)(((byte)(166)))), ((int)(((byte)(166)))), ((int)(((byte)(166)))));
            this.progressCircle.SubscriptMargin = new System.Windows.Forms.Padding(10, -35, 0, 0);
            this.progressCircle.SubscriptText = ".23";
            this.progressCircle.SuperscriptColor = System.Drawing.Color.FromArgb(((int)(((byte)(166)))), ((int)(((byte)(166)))), ((int)(((byte)(166)))));
            this.progressCircle.SuperscriptMargin = new System.Windows.Forms.Padding(10, 35, 0, 0);
            this.progressCircle.SuperscriptText = "°C";
            this.progressCircle.TabIndex = 90;
            this.progressCircle.TextMargin = new System.Windows.Forms.Padding(8, 8, 0, 0);
            this.progressCircle.Value = 33;
            this.progressCircle.Visible = false;
            // 
            // label39
            // 
            this.label39.Location = new System.Drawing.Point(12, 69);
            this.label39.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(600, 125);
            this.label39.TabIndex = 91;
            this.label39.Text = resources.GetString("label39.Text");
            // 
            // closeBtn
            // 
            this.closeBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.closeBtn.FlatAppearance.BorderSize = 0;
            this.closeBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeBtn.ForeColor = System.Drawing.Color.White;
            this.closeBtn.Location = new System.Drawing.Point(168, 219);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(150, 23);
            this.closeBtn.TabIndex = 92;
            this.closeBtn.Text = "Close";
            this.closeBtn.UseVisualStyleBackColor = false;
            this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cancelBtn.FlatAppearance.BorderSize = 0;
            this.cancelBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelBtn.ForeColor = System.Drawing.Color.White;
            this.cancelBtn.Location = new System.Drawing.Point(168, 219);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(150, 23);
            this.cancelBtn.TabIndex = 93;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = false;
            this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.ForeColor = System.Drawing.Color.White;
            this.statusLabel.Location = new System.Drawing.Point(329, 224);
            this.statusLabel.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 13);
            this.statusLabel.TabIndex = 94;
            // 
            // ModelDownloadForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(624, 281);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.closeBtn);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.label39);
            this.Controls.Add(this.progressCircle);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.downloadModelsBtn);
            this.Controls.Add(this.longProgBar);
            this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ModelDownloadForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Model Downloader";
            this.Load += new System.EventHandler(this.ModelDownloadForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private HTAlt.WinForms.HTProgressBar longProgBar;
        private HTAlt.WinForms.HTButton downloadModelsBtn;
        private System.Windows.Forms.Label titleLabel;
        private CircularProgressBar.CircularProgressBar progressCircle;
        private System.Windows.Forms.Label label39;
        private HTAlt.WinForms.HTButton closeBtn;
        private HTAlt.WinForms.HTButton cancelBtn;
        private System.Windows.Forms.Label statusLabel;
    }
}