namespace Flowframes.Forms
{
    partial class BatchForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BatchForm));
            this.titleLabel = new System.Windows.Forms.Label();
            this.stopBtn = new System.Windows.Forms.Button();
            this.runBtn = new System.Windows.Forms.Button();
            this.addToQueue = new System.Windows.Forms.Button();
            this.forceStopBtn = new System.Windows.Forms.Button();
            this.clearBtn = new System.Windows.Forms.Button();
            this.taskList = new System.Windows.Forms.ListBox();
            this.clearSelectedBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.moveDownBtn = new System.Windows.Forms.Button();
            this.moveUpBtn = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
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
            this.titleLabel.Size = new System.Drawing.Size(323, 40);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.Text = "Batch Processing Queue";
            // 
            // stopBtn
            // 
            this.stopBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.stopBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.stopBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.stopBtn.ForeColor = System.Drawing.Color.White;
            this.stopBtn.Location = new System.Drawing.Point(682, 351);
            this.stopBtn.Name = "stopBtn";
            this.stopBtn.Size = new System.Drawing.Size(250, 40);
            this.stopBtn.TabIndex = 35;
            this.stopBtn.Text = "Stop After Current Task";
            this.stopBtn.UseVisualStyleBackColor = false;
            this.stopBtn.Visible = false;
            this.stopBtn.Click += new System.EventHandler(this.stopBtn_Click);
            // 
            // runBtn
            // 
            this.runBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.runBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.runBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.runBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.runBtn.ForeColor = System.Drawing.Color.White;
            this.runBtn.Location = new System.Drawing.Point(682, 443);
            this.runBtn.Name = "runBtn";
            this.runBtn.Size = new System.Drawing.Size(250, 42);
            this.runBtn.TabIndex = 36;
            this.runBtn.Text = "Start";
            this.runBtn.UseVisualStyleBackColor = false;
            this.runBtn.Click += new System.EventHandler(this.runBtn_Click);
            // 
            // addToQueue
            // 
            this.addToQueue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addToQueue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.addToQueue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.addToQueue.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.addToQueue.ForeColor = System.Drawing.Color.White;
            this.addToQueue.Location = new System.Drawing.Point(682, 65);
            this.addToQueue.Name = "addToQueue";
            this.addToQueue.Size = new System.Drawing.Size(250, 40);
            this.addToQueue.TabIndex = 39;
            this.addToQueue.Text = "Add Current Configuration To Queue";
            this.addToQueue.UseVisualStyleBackColor = false;
            this.addToQueue.Click += new System.EventHandler(this.addToQueue_Click);
            // 
            // forceStopBtn
            // 
            this.forceStopBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.forceStopBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.forceStopBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.forceStopBtn.ForeColor = System.Drawing.Color.White;
            this.forceStopBtn.Location = new System.Drawing.Point(682, 397);
            this.forceStopBtn.Name = "forceStopBtn";
            this.forceStopBtn.Size = new System.Drawing.Size(250, 40);
            this.forceStopBtn.TabIndex = 40;
            this.forceStopBtn.Text = "Force Stop Now";
            this.forceStopBtn.UseVisualStyleBackColor = false;
            this.forceStopBtn.Visible = false;
            this.forceStopBtn.Click += new System.EventHandler(this.forceStopBtn_Click);
            // 
            // clearBtn
            // 
            this.clearBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.clearBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.clearBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearBtn.ForeColor = System.Drawing.Color.White;
            this.clearBtn.Location = new System.Drawing.Point(682, 157);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(250, 40);
            this.clearBtn.TabIndex = 41;
            this.clearBtn.Text = "Clear All Queue Entries";
            this.clearBtn.UseVisualStyleBackColor = false;
            this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);
            // 
            // taskList
            // 
            this.taskList.AllowDrop = true;
            this.taskList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.taskList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.taskList.ForeColor = System.Drawing.Color.White;
            this.taskList.FormattingEnabled = true;
            this.taskList.ItemHeight = 16;
            this.taskList.Location = new System.Drawing.Point(12, 65);
            this.taskList.Name = "taskList";
            this.taskList.Size = new System.Drawing.Size(664, 420);
            this.taskList.TabIndex = 43;
            this.taskList.SelectedIndexChanged += new System.EventHandler(this.taskList_SelectedIndexChanged);
            this.taskList.DragDrop += new System.Windows.Forms.DragEventHandler(this.taskList_DragDrop);
            this.taskList.DragEnter += new System.Windows.Forms.DragEventHandler(this.taskList_DragEnter);
            // 
            // clearSelectedBtn
            // 
            this.clearSelectedBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.clearSelectedBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.clearSelectedBtn.Enabled = false;
            this.clearSelectedBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearSelectedBtn.ForeColor = System.Drawing.Color.White;
            this.clearSelectedBtn.Location = new System.Drawing.Point(682, 111);
            this.clearSelectedBtn.Name = "clearSelectedBtn";
            this.clearSelectedBtn.Size = new System.Drawing.Size(250, 40);
            this.clearSelectedBtn.TabIndex = 44;
            this.clearSelectedBtn.Text = "Clear Selected Queue Entry";
            this.clearSelectedBtn.UseVisualStyleBackColor = false;
            this.clearSelectedBtn.Click += new System.EventHandler(this.clearSelectedBtn_Click);
            // 
            // label1
            // 
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(6, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(238, 111);
            this.label1.TabIndex = 45;
            this.label1.Text = "Tip:\r\nYou can also drag and drop multiple videos into the list.\r\nThey will be add" +
    "ed to the queue using the interpolation settings set in the GUI.";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panel1.Location = new System.Drawing.Point(682, 203);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(250, 142);
            this.panel1.TabIndex = 46;
            // 
            // moveDownBtn
            // 
            this.moveDownBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.moveDownBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.moveDownBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.moveDownBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.moveDownBtn.ForeColor = System.Drawing.Color.White;
            this.moveDownBtn.Location = new System.Drawing.Point(636, 445);
            this.moveDownBtn.Margin = new System.Windows.Forms.Padding(0);
            this.moveDownBtn.Name = "moveDownBtn";
            this.moveDownBtn.Size = new System.Drawing.Size(35, 35);
            this.moveDownBtn.TabIndex = 47;
            this.moveDownBtn.Text = "↓";
            this.moveDownBtn.UseVisualStyleBackColor = false;
            this.moveDownBtn.Visible = false;
            this.moveDownBtn.Click += new System.EventHandler(this.moveDownBtn_Click);
            // 
            // moveUpBtn
            // 
            this.moveUpBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.moveUpBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.moveUpBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.moveUpBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.moveUpBtn.ForeColor = System.Drawing.Color.White;
            this.moveUpBtn.Location = new System.Drawing.Point(597, 445);
            this.moveUpBtn.Margin = new System.Windows.Forms.Padding(0);
            this.moveUpBtn.Name = "moveUpBtn";
            this.moveUpBtn.Size = new System.Drawing.Size(35, 35);
            this.moveUpBtn.TabIndex = 48;
            this.moveUpBtn.Text = "↑";
            this.moveUpBtn.UseVisualStyleBackColor = false;
            this.moveUpBtn.Visible = false;
            this.moveUpBtn.Click += new System.EventHandler(this.moveUpBtn_Click);
            // 
            // BatchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(944, 501);
            this.Controls.Add(this.moveUpBtn);
            this.Controls.Add(this.moveDownBtn);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.clearSelectedBtn);
            this.Controls.Add(this.taskList);
            this.Controls.Add(this.clearBtn);
            this.Controls.Add(this.forceStopBtn);
            this.Controls.Add(this.addToQueue);
            this.Controls.Add(this.runBtn);
            this.Controls.Add(this.stopBtn);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "BatchForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Batch Processing";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BatchForm_FormClosing);
            this.Load += new System.EventHandler(this.BatchForm_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button stopBtn;
        private System.Windows.Forms.Button runBtn;
        private System.Windows.Forms.Button addToQueue;
        private System.Windows.Forms.Button forceStopBtn;
        private System.Windows.Forms.Button clearBtn;
        private System.Windows.Forms.ListBox taskList;
        private System.Windows.Forms.Button clearSelectedBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button moveDownBtn;
        private System.Windows.Forms.Button moveUpBtn;
    }
}