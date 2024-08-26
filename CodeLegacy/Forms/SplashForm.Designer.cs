namespace Flowframes.Forms
{
    partial class SplashForm
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
            this.loadingTextLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // loadingTextLabel
            // 
            this.loadingTextLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.loadingTextLabel.Font = new System.Drawing.Font("Yu Gothic UI", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loadingTextLabel.ForeColor = System.Drawing.Color.White;
            this.loadingTextLabel.Location = new System.Drawing.Point(0, 0);
            this.loadingTextLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.loadingTextLabel.Name = "loadingTextLabel";
            this.loadingTextLabel.Size = new System.Drawing.Size(480, 220);
            this.loadingTextLabel.TabIndex = 2;
            this.loadingTextLabel.Text = "Starting Flowframes...";
            this.loadingTextLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.loadingTextLabel.UseWaitCursor = true;
            // 
            // SplashForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(480, 220);
            this.Controls.Add(this.loadingTextLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximumSize = new System.Drawing.Size(480, 220);
            this.MinimumSize = new System.Drawing.Size(480, 220);
            this.Name = "SplashForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SplashForm";
            this.UseWaitCursor = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label loadingTextLabel;
    }
}