
namespace Flowframes.Forms
{
    partial class DebugForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DebugForm));
            this.titleLabel = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.configDataGrid = new System.Windows.Forms.DataGridView();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.copyTextClipboardBtn = new HTAlt.WinForms.HTButton();
            this.monospaceBtn = new HTAlt.WinForms.HTButton();
            this.refreshBtn = new HTAlt.WinForms.HTButton();
            this.textWrapBtn = new HTAlt.WinForms.HTButton();
            this.clearLogsBtn = new HTAlt.WinForms.HTButton();
            this.openLogFolderBtn = new HTAlt.WinForms.HTButton();
            this.logFilesDropdown = new System.Windows.Forms.ComboBox();
            this.logBox = new System.Windows.Forms.TextBox();
            this.htTabControl1 = new HTAlt.WinForms.HTTabControl();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tabPage2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.configDataGrid)).BeginInit();
            this.tabPage1.SuspendLayout();
            this.htTabControl1.SuspendLayout();
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
            this.titleLabel.Size = new System.Drawing.Size(175, 40);
            this.titleLabel.TabIndex = 2;
            this.titleLabel.Text = "Debug Tools";
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.tabPage2.Controls.Add(this.panel2);
            this.tabPage2.Controls.Add(this.panel1);
            this.tabPage2.Location = new System.Drawing.Point(4, 27);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(912, 396);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Config Editor";
            this.tabPage2.Enter += new System.EventHandler(this.tabPage2_Enter);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label1);
            this.panel2.Location = new System.Drawing.Point(6, 6);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(900, 68);
            this.panel2.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.SystemColors.Control;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(415, 60);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.configDataGrid);
            this.panel1.Location = new System.Drawing.Point(6, 80);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(900, 310);
            this.panel1.TabIndex = 4;
            // 
            // configDataGrid
            // 
            this.configDataGrid.AllowUserToResizeColumns = false;
            this.configDataGrid.AllowUserToResizeRows = false;
            this.configDataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.configDataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.configDataGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.configDataGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.configDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.configDataGrid.Location = new System.Drawing.Point(0, 0);
            this.configDataGrid.MultiSelect = false;
            this.configDataGrid.Name = "configDataGrid";
            this.configDataGrid.Size = new System.Drawing.Size(900, 310);
            this.configDataGrid.TabIndex = 0;
            this.configDataGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.configDataGrid_CellValueChanged);
            this.configDataGrid.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.configDataGrid_RowsAdded);
            this.configDataGrid.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.configDataGrid_RowsRemoved);
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.tabPage1.Controls.Add(this.copyTextClipboardBtn);
            this.tabPage1.Controls.Add(this.monospaceBtn);
            this.tabPage1.Controls.Add(this.refreshBtn);
            this.tabPage1.Controls.Add(this.textWrapBtn);
            this.tabPage1.Controls.Add(this.clearLogsBtn);
            this.tabPage1.Controls.Add(this.openLogFolderBtn);
            this.tabPage1.Controls.Add(this.logFilesDropdown);
            this.tabPage1.Controls.Add(this.logBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 27);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(912, 396);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Log Viewer";
            // 
            // copyTextClipboardBtn
            // 
            this.copyTextClipboardBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.copyTextClipboardBtn.FlatAppearance.BorderSize = 0;
            this.copyTextClipboardBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.copyTextClipboardBtn.ForeColor = System.Drawing.Color.White;
            this.copyTextClipboardBtn.Location = new System.Drawing.Point(600, 6);
            this.copyTextClipboardBtn.Name = "copyTextClipboardBtn";
            this.copyTextClipboardBtn.Size = new System.Drawing.Size(150, 23);
            this.copyTextClipboardBtn.TabIndex = 80;
            this.copyTextClipboardBtn.Text = "Copy Text To Clipboard";
            this.copyTextClipboardBtn.UseVisualStyleBackColor = false;
            this.copyTextClipboardBtn.Click += new System.EventHandler(this.copyTextClipboardBtn_Click);
            // 
            // monospaceBtn
            // 
            this.monospaceBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.monospaceBtn.ButtonImage = global::Flowframes.Properties.Resources.baseline_format_size_white_48dp;
            this.monospaceBtn.DrawImage = true;
            this.monospaceBtn.FlatAppearance.BorderSize = 0;
            this.monospaceBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.monospaceBtn.ForeColor = System.Drawing.Color.White;
            this.monospaceBtn.ImageSizeMode = HTAlt.WinForms.HTButton.ButtonImageSizeMode.Zoom;
            this.monospaceBtn.Location = new System.Drawing.Point(241, 6);
            this.monospaceBtn.Name = "monospaceBtn";
            this.monospaceBtn.Size = new System.Drawing.Size(23, 23);
            this.monospaceBtn.TabIndex = 79;
            this.toolTip.SetToolTip(this.monospaceBtn, "Toggle Monospace Font");
            this.monospaceBtn.UseVisualStyleBackColor = false;
            this.monospaceBtn.Click += new System.EventHandler(this.monospaceBtn_Click);
            // 
            // refreshBtn
            // 
            this.refreshBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.refreshBtn.ButtonImage = global::Flowframes.Properties.Resources.baseline_refresh_white_48dp;
            this.refreshBtn.DrawImage = true;
            this.refreshBtn.FlatAppearance.BorderSize = 0;
            this.refreshBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.refreshBtn.ForeColor = System.Drawing.Color.White;
            this.refreshBtn.ImageSizeMode = HTAlt.WinForms.HTButton.ButtonImageSizeMode.Zoom;
            this.refreshBtn.Location = new System.Drawing.Point(212, 6);
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(23, 23);
            this.refreshBtn.TabIndex = 78;
            this.toolTip.SetToolTip(this.refreshBtn, "Refresh");
            this.refreshBtn.UseVisualStyleBackColor = false;
            this.refreshBtn.Click += new System.EventHandler(this.refreshBtn_Click);
            // 
            // textWrapBtn
            // 
            this.textWrapBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textWrapBtn.ButtonImage = global::Flowframes.Properties.Resources.baseline_wrap_text_white_48dp;
            this.textWrapBtn.DrawImage = true;
            this.textWrapBtn.FlatAppearance.BorderSize = 0;
            this.textWrapBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.textWrapBtn.ForeColor = System.Drawing.Color.White;
            this.textWrapBtn.ImageSizeMode = HTAlt.WinForms.HTButton.ButtonImageSizeMode.Zoom;
            this.textWrapBtn.Location = new System.Drawing.Point(270, 5);
            this.textWrapBtn.Name = "textWrapBtn";
            this.textWrapBtn.Size = new System.Drawing.Size(23, 23);
            this.textWrapBtn.TabIndex = 77;
            this.toolTip.SetToolTip(this.textWrapBtn, "Toggle Text Wrap");
            this.textWrapBtn.UseVisualStyleBackColor = false;
            this.textWrapBtn.Click += new System.EventHandler(this.textWrapBtn_Click);
            // 
            // clearLogsBtn
            // 
            this.clearLogsBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.clearLogsBtn.FlatAppearance.BorderSize = 0;
            this.clearLogsBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearLogsBtn.ForeColor = System.Drawing.Color.White;
            this.clearLogsBtn.Location = new System.Drawing.Point(756, 6);
            this.clearLogsBtn.Name = "clearLogsBtn";
            this.clearLogsBtn.Size = new System.Drawing.Size(150, 23);
            this.clearLogsBtn.TabIndex = 76;
            this.clearLogsBtn.Text = "Clear Logs";
            this.clearLogsBtn.UseVisualStyleBackColor = false;
            this.clearLogsBtn.Click += new System.EventHandler(this.clearLogsBtn_Click);
            // 
            // openLogFolderBtn
            // 
            this.openLogFolderBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.openLogFolderBtn.FlatAppearance.BorderSize = 0;
            this.openLogFolderBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.openLogFolderBtn.ForeColor = System.Drawing.Color.White;
            this.openLogFolderBtn.Location = new System.Drawing.Point(444, 6);
            this.openLogFolderBtn.Name = "openLogFolderBtn";
            this.openLogFolderBtn.Size = new System.Drawing.Size(150, 23);
            this.openLogFolderBtn.TabIndex = 75;
            this.openLogFolderBtn.Text = "Open Log Folder";
            this.openLogFolderBtn.UseVisualStyleBackColor = false;
            this.openLogFolderBtn.Click += new System.EventHandler(this.openLogFolderBtn_Click);
            // 
            // logFilesDropdown
            // 
            this.logFilesDropdown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.logFilesDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.logFilesDropdown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.logFilesDropdown.ForeColor = System.Drawing.Color.White;
            this.logFilesDropdown.FormattingEnabled = true;
            this.logFilesDropdown.Location = new System.Drawing.Point(6, 6);
            this.logFilesDropdown.Name = "logFilesDropdown";
            this.logFilesDropdown.Size = new System.Drawing.Size(200, 23);
            this.logFilesDropdown.TabIndex = 73;
            this.logFilesDropdown.SelectedIndexChanged += new System.EventHandler(this.logFilesDropdown_SelectedIndexChanged);
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.logBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.logBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logBox.ForeColor = System.Drawing.Color.White;
            this.logBox.Location = new System.Drawing.Point(6, 35);
            this.logBox.MinimumSize = new System.Drawing.Size(4, 21);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.logBox.Size = new System.Drawing.Size(900, 355);
            this.logBox.TabIndex = 6;
            this.logBox.TabStop = false;
            this.logBox.WordWrap = false;
            // 
            // htTabControl1
            // 
            this.htTabControl1.AllowDrop = true;
            this.htTabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.htTabControl1.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.htTabControl1.BorderTabLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.htTabControl1.Controls.Add(this.tabPage1);
            this.htTabControl1.Controls.Add(this.tabPage2);
            this.htTabControl1.DisableClose = true;
            this.htTabControl1.DisableDragging = true;
            this.htTabControl1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.htTabControl1.HoverTabButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(82)))), ((int)(((byte)(176)))), ((int)(((byte)(239)))));
            this.htTabControl1.HoverTabColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.htTabControl1.HoverUnselectedTabButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.htTabControl1.Location = new System.Drawing.Point(12, 62);
            this.htTabControl1.Name = "htTabControl1";
            this.htTabControl1.Padding = new System.Drawing.Point(14, 4);
            this.htTabControl1.SelectedIndex = 0;
            this.htTabControl1.SelectedTabButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.htTabControl1.SelectedTabColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.htTabControl1.Size = new System.Drawing.Size(920, 427);
            this.htTabControl1.TabIndex = 3;
            this.htTabControl1.TextColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.htTabControl1.UnderBorderTabLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(67)))), ((int)(((byte)(70)))));
            this.htTabControl1.UnselectedBorderTabLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.htTabControl1.UnselectedTabColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.htTabControl1.UpDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.htTabControl1.UpDownTextColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(112)))));
            // 
            // DebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(944, 501);
            this.Controls.Add(this.htTabControl1);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DebugForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Debug Tools";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DebugForm_FormClosing);
            this.Shown += new System.EventHandler(this.DebugForm_Shown);
            this.tabPage2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.configDataGrid)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.htTabControl1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage1;
        private HTAlt.WinForms.HTTabControl htTabControl1;
        private System.Windows.Forms.DataGridView configDataGrid;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.ComboBox logFilesDropdown;
        private HTAlt.WinForms.HTButton clearLogsBtn;
        private HTAlt.WinForms.HTButton openLogFolderBtn;
        private HTAlt.WinForms.HTButton textWrapBtn;
        private HTAlt.WinForms.HTButton refreshBtn;
        private System.Windows.Forms.ToolTip toolTip;
        private HTAlt.WinForms.HTButton monospaceBtn;
        private HTAlt.WinForms.HTButton copyTextClipboardBtn;
    }
}