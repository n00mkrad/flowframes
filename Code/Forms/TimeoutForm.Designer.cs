
namespace Flowframes.Forms
{
    partial class TimeoutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimeoutForm));
            this.mainLabel = new System.Windows.Forms.Label();
            this.countdownLabel = new System.Windows.Forms.Label();
            this.cancelActionBtn = new HTAlt.WinForms.HTButton();
            this.skipCountdownBtn = new HTAlt.WinForms.HTButton();
            this.SuspendLayout();
            // 
            // mainLabel
            // 
            this.mainLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainLabel.ForeColor = System.Drawing.Color.White;
            this.mainLabel.Location = new System.Drawing.Point(12, 9);
            this.mainLabel.Name = "mainLabel";
            this.mainLabel.Size = new System.Drawing.Size(320, 23);
            this.mainLabel.TabIndex = 0;
            this.mainLabel.Text = "Waiting before running action...";
            this.mainLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // countdownLabel
            // 
            this.countdownLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.countdownLabel.ForeColor = System.Drawing.Color.White;
            this.countdownLabel.Location = new System.Drawing.Point(12, 42);
            this.countdownLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            this.countdownLabel.Name = "countdownLabel";
            this.countdownLabel.Size = new System.Drawing.Size(320, 23);
            this.countdownLabel.TabIndex = 1;
            this.countdownLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cancelActionBtn
            // 
            this.cancelActionBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.cancelActionBtn.FlatAppearance.BorderSize = 0;
            this.cancelActionBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelActionBtn.ForeColor = System.Drawing.Color.White;
            this.cancelActionBtn.Location = new System.Drawing.Point(12, 126);
            this.cancelActionBtn.Name = "cancelActionBtn";
            this.cancelActionBtn.Size = new System.Drawing.Size(320, 23);
            this.cancelActionBtn.TabIndex = 39;
            this.cancelActionBtn.Text = "Cancel Action";
            this.cancelActionBtn.UseVisualStyleBackColor = false;
            this.cancelActionBtn.Click += new System.EventHandler(this.cancelActionBtn_Click);
            // 
            // skipCountdownBtn
            // 
            this.skipCountdownBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.skipCountdownBtn.FlatAppearance.BorderSize = 0;
            this.skipCountdownBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.skipCountdownBtn.ForeColor = System.Drawing.Color.White;
            this.skipCountdownBtn.Location = new System.Drawing.Point(12, 94);
            this.skipCountdownBtn.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.skipCountdownBtn.Name = "skipCountdownBtn";
            this.skipCountdownBtn.Size = new System.Drawing.Size(320, 23);
            this.skipCountdownBtn.TabIndex = 40;
            this.skipCountdownBtn.Text = "Run Action Now";
            this.skipCountdownBtn.UseVisualStyleBackColor = false;
            this.skipCountdownBtn.Click += new System.EventHandler(this.skipCountdownBtn_Click);
            // 
            // TimeoutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(344, 161);
            this.Controls.Add(this.skipCountdownBtn);
            this.Controls.Add(this.cancelActionBtn);
            this.Controls.Add(this.countdownLabel);
            this.Controls.Add(this.mainLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TimeoutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Timeout";
            this.Load += new System.EventHandler(this.TimeoutForm_Load);
            this.Shown += new System.EventHandler(this.TimeoutForm_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label mainLabel;
        private System.Windows.Forms.Label countdownLabel;
        private HTAlt.WinForms.HTButton cancelActionBtn;
        private HTAlt.WinForms.HTButton skipCountdownBtn;
    }
}