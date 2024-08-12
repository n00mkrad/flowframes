namespace Flowframes.Forms.Main
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            titleLabel = new Label();
            aiModel = new ComboBox();
            aiCombox = new ComboBox();
            label13 = new Label();
            label9 = new Label();
            label8 = new Label();
            label6 = new Label();
            fpsOutTbox = new TextBox();
            fpsInTbox = new TextBox();
            interpFactorCombox = new ComboBox();
            label4 = new Label();
            outputTbox = new TextBox();
            inputTbox = new TextBox();
            label3 = new Label();
            label2 = new Label();
            label23 = new Label();
            label19 = new Label();
            label17 = new Label();
            label16 = new Label();
            label14 = new Label();
            runBtn = new Button();
            logBox = new TextBox();
            inFolderDialog = new FolderBrowserDialog();
            inFileDialog = new OpenFileDialog();
            outFolderDialog = new FolderBrowserDialog();
            label12 = new Label();
            panel1 = new Panel();
            statusLabel = new Label();
            toolTip1 = new ToolTip(components);
            pictureBox4 = new PictureBox();
            pictureBox3 = new PictureBox();
            info1 = new PictureBox();
            pictureBox1 = new PictureBox();
            pictureBox2 = new PictureBox();
            panel10 = new Panel();
            panel14 = new Panel();
            panel3 = new Panel();
            panel2 = new Panel();
            pictureBox5 = new PictureBox();
            welcomeTab = new TabPage();
            welcomeLabel2 = new Label();
            panel8 = new Panel();
            patronsLabel = new Label();
            label21 = new Label();
            panel6 = new Panel();
            newsLabel = new Label();
            label15 = new Label();
            label11 = new Label();
            interpOptsTab = new TabPage();
            labelOutput = new Label();
            flowLayoutPanel1 = new FlowLayoutPanel();
            comboxOutputFormat = new ComboBox();
            comboxOutputEncoder = new ComboBox();
            comboxOutputQuality = new ComboBox();
            textboxOutputQualityCust = new TextBox();
            comboxOutputColors = new ComboBox();
            outSpeedCombox = new ComboBox();
            completionActionPanel = new Panel();
            completionAction = new ComboBox();
            label25 = new Label();
            inputInfo = new Label();
            label1 = new Label();
            quickSettingsTab = new TabPage();
            label20 = new Label();
            maxFps = new ComboBox();
            label24 = new Label();
            linkLabel1 = new LinkLabel();
            mpDedupePanel = new Panel();
            mpdecimateMode = new ComboBox();
            magickDedupePanel = new Panel();
            dedupThresh = new NumericUpDown();
            dedupeSensLabel = new Label();
            dedupMode = new ComboBox();
            scnDetectValue = new NumericUpDown();
            label52 = new Label();
            label51 = new Label();
            scnDetect = new CheckBox();
            enableLoop = new CheckBox();
            label34 = new Label();
            maxVidHeight = new ComboBox();
            label18 = new Label();
            trimPanel = new Panel();
            trimStartBox = new TextBox();
            label10 = new Label();
            trimEndBox = new TextBox();
            trimCombox = new ComboBox();
            previewTab = new TabPage();
            label22 = new Label();
            previewPicturebox = new PictureBox();
            abtTab = new TabPage();
            runStepBtn = new Button();
            stepSelector = new ComboBox();
            busyControlsPanel = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            pauseBtn = new Button();
            cancelBtn = new Button();
            menuStripQueue = new ContextMenuStrip(components);
            addCurrentConfigurationToQueueToolStripMenuItem = new ToolStripMenuItem();
            tabControl1 = new TabControl();
            tabPageWelcome = new TabPage();
            tabPageInterp = new TabPage();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)info1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).BeginInit();
            panel8.SuspendLayout();
            panel6.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            completionActionPanel.SuspendLayout();
            quickSettingsTab.SuspendLayout();
            mpDedupePanel.SuspendLayout();
            magickDedupePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dedupThresh).BeginInit();
            ((System.ComponentModel.ISupportInitialize)scnDetectValue).BeginInit();
            trimPanel.SuspendLayout();
            previewTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)previewPicturebox).BeginInit();
            abtTab.SuspendLayout();
            busyControlsPanel.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            menuStripQueue.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPageWelcome.SuspendLayout();
            tabPageInterp.SuspendLayout();
            SuspendLayout();
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Yu Gothic UI", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(14, 10);
            titleLabel.Margin = new Padding(4, 0, 4, 12);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(404, 40);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Flowframes Video Interpolator";
            // 
            // aiModel
            // 
            aiModel.BackColor = Color.FromArgb(64, 64, 64);
            aiModel.DropDownStyle = ComboBoxStyle.DropDownList;
            aiModel.FlatStyle = FlatStyle.Flat;
            aiModel.ForeColor = Color.White;
            aiModel.FormattingEnabled = true;
            aiModel.Location = new Point(281, 126);
            aiModel.Name = "aiModel";
            aiModel.Size = new Size(400, 23);
            aiModel.TabIndex = 25;
            // 
            // aiCombox
            // 
            aiCombox.BackColor = Color.FromArgb(64, 64, 64);
            aiCombox.DropDownStyle = ComboBoxStyle.DropDownList;
            aiCombox.FlatStyle = FlatStyle.Flat;
            aiCombox.ForeColor = Color.White;
            aiCombox.FormattingEnabled = true;
            aiCombox.Location = new Point(281, 7);
            aiCombox.Name = "aiCombox";
            aiCombox.Size = new Size(400, 23);
            aiCombox.TabIndex = 21;
            aiCombox.SelectedIndexChanged += aiCombox_SelectedIndexChanged;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label13.ForeColor = Color.White;
            label13.Location = new Point(14, 13);
            label13.Margin = new Padding(8, 8, 3, 0);
            label13.Name = "label13";
            label13.Size = new Size(78, 13);
            label13.TabIndex = 20;
            label13.Text = "Interpolation AI";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label9.ForeColor = Color.White;
            label9.Location = new Point(14, 163);
            label9.Margin = new Padding(8, 0, 3, 0);
            label9.Name = "label9";
            label9.Size = new Size(74, 13);
            label9.TabIndex = 15;
            label9.Text = "Output Format";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label8.ForeColor = Color.White;
            label8.Location = new Point(14, 133);
            label8.Margin = new Padding(8, 0, 3, 0);
            label8.Name = "label8";
            label8.Size = new Size(49, 13);
            label8.TabIndex = 13;
            label8.Text = "AI Model";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.ForeColor = Color.White;
            label6.Location = new Point(453, 103);
            label6.Margin = new Padding(3, 0, 0, 0);
            label6.Name = "label6";
            label6.Size = new Size(15, 15);
            label6.TabIndex = 11;
            label6.Text = "=";
            // 
            // fpsOutTbox
            // 
            fpsOutTbox.BackColor = Color.FromArgb(64, 64, 64);
            fpsOutTbox.ForeColor = Color.White;
            fpsOutTbox.Location = new Point(468, 97);
            fpsOutTbox.MinimumSize = new Size(4, 21);
            fpsOutTbox.Name = "fpsOutTbox";
            fpsOutTbox.ReadOnly = true;
            fpsOutTbox.Size = new Size(100, 23);
            fpsOutTbox.TabIndex = 9;
            fpsOutTbox.Leave += fpsOutTbox_Leave;
            // 
            // fpsInTbox
            // 
            fpsInTbox.BackColor = Color.FromArgb(64, 64, 64);
            fpsInTbox.ForeColor = Color.White;
            fpsInTbox.Location = new Point(281, 97);
            fpsInTbox.MinimumSize = new Size(4, 21);
            fpsInTbox.Name = "fpsInTbox";
            fpsInTbox.ReadOnly = true;
            fpsInTbox.Size = new Size(100, 23);
            fpsInTbox.TabIndex = 8;
            fpsInTbox.Text = "0";
            fpsInTbox.TextChanged += fpsInTbox_TextChanged;
            // 
            // interpFactorCombox
            // 
            interpFactorCombox.BackColor = Color.FromArgb(64, 64, 64);
            interpFactorCombox.FlatStyle = FlatStyle.Flat;
            interpFactorCombox.ForeColor = Color.White;
            interpFactorCombox.FormattingEnabled = true;
            interpFactorCombox.Location = new Point(387, 97);
            interpFactorCombox.Name = "interpFactorCombox";
            interpFactorCombox.Size = new Size(57, 23);
            interpFactorCombox.TabIndex = 7;
            interpFactorCombox.SelectedIndexChanged += interpFactorCombox_SelectedIndexChanged;
            interpFactorCombox.TextUpdate += interpFactorCombox_SelectedIndexChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.White;
            label4.Location = new Point(14, 103);
            label4.Margin = new Padding(8, 0, 3, 0);
            label4.Name = "label4";
            label4.Size = new Size(117, 13);
            label4.TabIndex = 6;
            label4.Text = "Output FPS and Speed";
            // 
            // outputTbox
            // 
            outputTbox.AllowDrop = true;
            outputTbox.BackColor = Color.FromArgb(64, 64, 64);
            outputTbox.ForeColor = Color.White;
            outputTbox.Location = new Point(281, 67);
            outputTbox.MinimumSize = new Size(4, 21);
            outputTbox.Name = "outputTbox";
            outputTbox.Size = new Size(400, 23);
            outputTbox.TabIndex = 4;
            outputTbox.DragDrop += outputTbox_DragDrop;
            outputTbox.DragEnter += outputTbox_DragEnter;
            // 
            // inputTbox
            // 
            inputTbox.AllowDrop = true;
            inputTbox.BackColor = Color.FromArgb(64, 64, 64);
            inputTbox.ForeColor = Color.White;
            inputTbox.Location = new Point(281, 37);
            inputTbox.MinimumSize = new Size(4, 21);
            inputTbox.Name = "inputTbox";
            inputTbox.Size = new Size(400, 23);
            inputTbox.TabIndex = 2;
            inputTbox.DragDrop += inputTbox_DragDrop;
            inputTbox.DragEnter += inputTbox_DragEnter;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.White;
            label3.Location = new Point(14, 73);
            label3.Margin = new Padding(8, 0, 3, 0);
            label3.Name = "label3";
            label3.Size = new Size(84, 13);
            label3.TabIndex = 1;
            label3.Text = "Output Directory";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.White;
            label2.Location = new Point(14, 43);
            label2.Margin = new Padding(8, 0, 3, 0);
            label2.Name = "label2";
            label2.Size = new Size(150, 13);
            label2.TabIndex = 0;
            label2.Text = "Input Video (Or Frames Folder)";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.ForeColor = Color.White;
            label23.Location = new Point(8, 130);
            label23.Margin = new Padding(8, 0, 3, 0);
            label23.Name = "label23";
            label23.Size = new Size(105, 15);
            label23.TabIndex = 26;
            label23.Text = "Fix Scene Changes";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.ForeColor = Color.White;
            label19.Location = new Point(8, 100);
            label19.Margin = new Padding(8, 0, 3, 0);
            label19.Name = "label19";
            label19.Size = new Size(34, 15);
            label19.TabIndex = 20;
            label19.Text = "Loop";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.ForeColor = Color.White;
            label17.Location = new Point(8, 70);
            label17.Margin = new Padding(8, 0, 3, 0);
            label17.Name = "label17";
            label17.Size = new Size(87, 15);
            label17.TabIndex = 4;
            label17.Text = "De-Duplication";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.ForeColor = Color.White;
            label16.Location = new Point(10, 10);
            label16.Margin = new Padding(8, 0, 3, 0);
            label16.Name = "label16";
            label16.Size = new Size(61, 15);
            label16.TabIndex = 2;
            label16.Text = "Trim Input";
            // 
            // label14
            // 
            label14.ForeColor = Color.White;
            label14.Location = new Point(11, 8);
            label14.Margin = new Padding(8, 8, 3, 0);
            label14.Name = "label14";
            label14.Size = new Size(884, 189);
            label14.TabIndex = 2;
            label14.Text = resources.GetString("label14.Text");
            // 
            // runBtn
            // 
            runBtn.BackColor = Color.FromArgb(48, 48, 48);
            runBtn.Enabled = false;
            runBtn.FlatStyle = FlatStyle.Flat;
            runBtn.ForeColor = Color.White;
            runBtn.Location = new Point(12, 418);
            runBtn.Margin = new Padding(4, 3, 4, 3);
            runBtn.Name = "runBtn";
            runBtn.Size = new Size(237, 82);
            runBtn.TabIndex = 2;
            runBtn.Text = "Interpolate!";
            runBtn.UseVisualStyleBackColor = false;
            runBtn.Click += runBtn_Click;
            // 
            // logBox
            // 
            logBox.BackColor = Color.FromArgb(64, 64, 64);
            logBox.ForeColor = Color.White;
            logBox.Location = new Point(256, 357);
            logBox.Margin = new Padding(4, 3, 4, 3);
            logBox.MinimumSize = new Size(4, 24);
            logBox.Multiline = true;
            logBox.Name = "logBox";
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.Size = new Size(667, 127);
            logBox.TabIndex = 5;
            logBox.TabStop = false;
            logBox.Text = "Welcome to Flowframes!";
            // 
            // inFileDialog
            // 
            inFileDialog.FileName = "openFileDialog1";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.ForeColor = Color.White;
            label12.Location = new Point(9, 8);
            label12.Margin = new Padding(9, 0, 4, 0);
            label12.Name = "label12";
            label12.Size = new Size(42, 15);
            label12.TabIndex = 6;
            label12.Text = "Status:";
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(48, 48, 48);
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(statusLabel);
            panel1.Controls.Add(label12);
            panel1.Location = new Point(13, 357);
            panel1.Margin = new Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(236, 63);
            panel1.TabIndex = 6;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.White;
            statusLabel.Location = new Point(9, 35);
            statusLabel.Margin = new Padding(9, 0, 4, 0);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(70, 15);
            statusLabel.TabIndex = 7;
            statusLabel.Text = "Initializing...";
            // 
            // pictureBox4
            // 
            pictureBox4.BackgroundImage = Properties.Resources.questmark_72px_bordeer;
            pictureBox4.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox4.Location = new Point(167, 37);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(29, 21);
            pictureBox4.TabIndex = 31;
            pictureBox4.TabStop = false;
            toolTip1.SetToolTip(pictureBox4, "Set your input video file, or a folder that contains frames/images.\r\nSupports Drag-N-Drop.");
            // 
            // pictureBox3
            // 
            pictureBox3.BackgroundImage = Properties.Resources.questmark_72px_bordeer;
            pictureBox3.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox3.Location = new Point(101, 67);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(29, 21);
            pictureBox3.TabIndex = 30;
            pictureBox3.TabStop = false;
            toolTip1.SetToolTip(pictureBox3, "Supports Drag-N-Drop.\r\nGets auto-assigned whenever you set an input file.");
            // 
            // info1
            // 
            info1.BackgroundImage = Properties.Resources.questmark_72px_bordeer;
            info1.BackgroundImageLayout = ImageLayout.Zoom;
            info1.Location = new Point(134, 97);
            info1.Name = "info1";
            info1.Size = new Size(29, 21);
            info1.TabIndex = 27;
            info1.TabStop = false;
            toolTip1.SetToolTip(info1, "Here you can change your interpolation factor (different AIs support different factors), which will determine your output frame rate.");
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.questmark_72px_bordeer;
            pictureBox1.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox1.Location = new Point(66, 127);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(29, 21);
            pictureBox1.TabIndex = 28;
            pictureBox1.TabStop = false;
            toolTip1.SetToolTip(pictureBox1, "This is the training data the AI will use.\r\nDifferent AI models will produce slightly different results. Try them for yourself.");
            // 
            // pictureBox2
            // 
            pictureBox2.BackgroundImage = Properties.Resources.questmark_72px_bordeer;
            pictureBox2.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox2.Location = new Point(91, 157);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(29, 21);
            pictureBox2.TabIndex = 29;
            pictureBox2.TabStop = false;
            toolTip1.SetToolTip(pictureBox2, "Set your interpolation output format.\r\nEncoding and quality options can be changed in the Settings.");
            // 
            // panel10
            // 
            panel10.BackgroundImage = Properties.Resources.baseline_create_white_18dp_semiTransparent;
            panel10.BackgroundImageLayout = ImageLayout.Zoom;
            panel10.Location = new Point(373, 39);
            panel10.Name = "panel10";
            panel10.Size = new Size(21, 21);
            panel10.TabIndex = 67;
            toolTip1.SetToolTip(panel10, "Allows custom input.");
            // 
            // panel14
            // 
            panel14.BackgroundImage = Properties.Resources.baseline_create_white_18dp_semiTransparent;
            panel14.BackgroundImageLayout = ImageLayout.Zoom;
            panel14.Location = new Point(476, 130);
            panel14.Name = "panel14";
            panel14.Size = new Size(21, 21);
            panel14.TabIndex = 79;
            toolTip1.SetToolTip(panel14, "Allows custom input.");
            // 
            // panel3
            // 
            panel3.BackgroundImage = Properties.Resources.baseline_create_white_18dp_semiTransparent;
            panel3.BackgroundImageLayout = ImageLayout.Zoom;
            panel3.Location = new Point(114, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(21, 21);
            panel3.TabIndex = 57;
            toolTip1.SetToolTip(panel3, "Allows custom input.");
            // 
            // panel2
            // 
            panel2.BackgroundImage = Properties.Resources.baseline_create_white_18dp_semiTransparent;
            panel2.BackgroundImageLayout = ImageLayout.Zoom;
            panel2.Location = new Point(386, 159);
            panel2.Name = "panel2";
            panel2.Size = new Size(21, 21);
            panel2.TabIndex = 90;
            toolTip1.SetToolTip(panel2, "Allows custom input.");
            // 
            // pictureBox5
            // 
            pictureBox5.BackgroundImage = Properties.Resources.questmark_72px_bordeer;
            pictureBox5.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox5.Location = new Point(95, 7);
            pictureBox5.Name = "pictureBox5";
            pictureBox5.Size = new Size(29, 21);
            pictureBox5.TabIndex = 44;
            pictureBox5.TabStop = false;
            toolTip1.SetToolTip(pictureBox5, "Set the AI interpolation network to use for this video.\r\nDifferent AIs have different quality, VRAM requirements and speeds.");
            // 
            // welcomeTab
            // 
            welcomeTab.BackColor = Color.FromArgb(48, 48, 48);
            welcomeTab.Location = new Point(4, 27);
            welcomeTab.Name = "welcomeTab";
            welcomeTab.Padding = new Padding(3);
            welcomeTab.Size = new Size(901, 258);
            welcomeTab.TabIndex = 4;
            welcomeTab.Text = "Welcome";
            // 
            // welcomeLabel2
            // 
            welcomeLabel2.AutoSize = true;
            welcomeLabel2.Font = new Font("Yu Gothic UI", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            welcomeLabel2.ForeColor = Color.Gray;
            welcomeLabel2.Location = new Point(142, 3);
            welcomeLabel2.Margin = new Padding(3, 0, 3, 10);
            welcomeLabel2.Name = "welcomeLabel2";
            welcomeLabel2.Size = new Size(478, 40);
            welcomeLabel2.TabIndex = 5;
            welcomeLabel2.Text = "Click The Interpolation Tab To Begin.";
            welcomeLabel2.Click += welcomeLabel2_Click;
            // 
            // panel8
            // 
            panel8.AutoScroll = true;
            panel8.BackColor = Color.FromArgb(64, 64, 64);
            panel8.Controls.Add(patronsLabel);
            panel8.Controls.Add(label21);
            panel8.Location = new Point(593, 57);
            panel8.Margin = new Padding(5);
            panel8.Name = "panel8";
            panel8.Size = new Size(300, 193);
            panel8.TabIndex = 4;
            // 
            // patronsLabel
            // 
            patronsLabel.AutoSize = true;
            patronsLabel.ForeColor = Color.White;
            patronsLabel.Location = new Point(8, 31);
            patronsLabel.Margin = new Padding(8, 8, 3, 0);
            patronsLabel.Name = "patronsLabel";
            patronsLabel.Size = new Size(0, 15);
            patronsLabel.TabIndex = 9;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label21.ForeColor = Color.White;
            label21.Location = new Point(8, 8);
            label21.Margin = new Padding(8, 8, 3, 0);
            label21.Name = "label21";
            label21.Size = new Size(119, 15);
            label21.TabIndex = 8;
            label21.Text = "Patreon Supporters:";
            // 
            // panel6
            // 
            panel6.AutoScroll = true;
            panel6.BackColor = Color.FromArgb(64, 64, 64);
            panel6.Controls.Add(newsLabel);
            panel6.Controls.Add(label15);
            panel6.Location = new Point(8, 57);
            panel6.Margin = new Padding(5);
            panel6.Name = "panel6";
            panel6.Size = new Size(575, 193);
            panel6.TabIndex = 3;
            // 
            // newsLabel
            // 
            newsLabel.AutoSize = true;
            newsLabel.ForeColor = Color.White;
            newsLabel.Location = new Point(8, 31);
            newsLabel.Margin = new Padding(8, 8, 3, 0);
            newsLabel.Name = "newsLabel";
            newsLabel.Size = new Size(0, 15);
            newsLabel.TabIndex = 8;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label15.ForeColor = Color.White;
            label15.Location = new Point(8, 8);
            label15.Margin = new Padding(8, 8, 3, 0);
            label15.Name = "label15";
            label15.Size = new Size(41, 15);
            label15.TabIndex = 7;
            label15.Text = "News:";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Yu Gothic UI", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label11.ForeColor = Color.White;
            label11.Location = new Point(6, 3);
            label11.Margin = new Padding(3, 0, 3, 10);
            label11.Name = "label11";
            label11.Size = new Size(143, 40);
            label11.TabIndex = 1;
            label11.Text = "Welcome!";
            // 
            // interpOptsTab
            // 
            interpOptsTab.AllowDrop = true;
            interpOptsTab.BackColor = Color.FromArgb(48, 48, 48);
            interpOptsTab.Location = new Point(4, 27);
            interpOptsTab.Name = "interpOptsTab";
            interpOptsTab.Padding = new Padding(3);
            interpOptsTab.Size = new Size(901, 258);
            interpOptsTab.TabIndex = 0;
            interpOptsTab.Text = "Interpolation";
            interpOptsTab.DragDrop += Form1_DragDrop;
            interpOptsTab.DragEnter += Form1_DragEnter;
            // 
            // labelOutput
            // 
            labelOutput.AutoSize = true;
            labelOutput.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelOutput.ForeColor = Color.Silver;
            labelOutput.Location = new Point(695, 164);
            labelOutput.Margin = new Padding(8, 0, 3, 0);
            labelOutput.Name = "labelOutput";
            labelOutput.Size = new Size(0, 13);
            labelOutput.TabIndex = 47;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(comboxOutputFormat);
            flowLayoutPanel1.Controls.Add(comboxOutputEncoder);
            flowLayoutPanel1.Controls.Add(comboxOutputQuality);
            flowLayoutPanel1.Controls.Add(textboxOutputQualityCust);
            flowLayoutPanel1.Controls.Add(comboxOutputColors);
            flowLayoutPanel1.Location = new Point(281, 157);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(614, 23);
            flowLayoutPanel1.TabIndex = 46;
            // 
            // comboxOutputFormat
            // 
            comboxOutputFormat.BackColor = Color.FromArgb(64, 64, 64);
            comboxOutputFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            comboxOutputFormat.FlatStyle = FlatStyle.Flat;
            comboxOutputFormat.ForeColor = Color.White;
            comboxOutputFormat.FormattingEnabled = true;
            comboxOutputFormat.Location = new Point(0, 0);
            comboxOutputFormat.Margin = new Padding(0, 0, 6, 0);
            comboxOutputFormat.Name = "comboxOutputFormat";
            comboxOutputFormat.Size = new Size(75, 23);
            comboxOutputFormat.TabIndex = 47;
            comboxOutputFormat.SelectedIndexChanged += comboxOutputFormat_SelectedIndexChanged;
            // 
            // comboxOutputEncoder
            // 
            comboxOutputEncoder.BackColor = Color.FromArgb(64, 64, 64);
            comboxOutputEncoder.DropDownStyle = ComboBoxStyle.DropDownList;
            comboxOutputEncoder.FlatStyle = FlatStyle.Flat;
            comboxOutputEncoder.ForeColor = Color.White;
            comboxOutputEncoder.FormattingEnabled = true;
            comboxOutputEncoder.Location = new Point(81, 0);
            comboxOutputEncoder.Margin = new Padding(0, 0, 6, 0);
            comboxOutputEncoder.Name = "comboxOutputEncoder";
            comboxOutputEncoder.Size = new Size(90, 23);
            comboxOutputEncoder.TabIndex = 50;
            comboxOutputEncoder.SelectedIndexChanged += comboxOutputEncoder_SelectedIndexChanged;
            // 
            // comboxOutputQuality
            // 
            comboxOutputQuality.BackColor = Color.FromArgb(64, 64, 64);
            comboxOutputQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            comboxOutputQuality.FlatStyle = FlatStyle.Flat;
            comboxOutputQuality.ForeColor = Color.White;
            comboxOutputQuality.FormattingEnabled = true;
            comboxOutputQuality.Location = new Point(177, 0);
            comboxOutputQuality.Margin = new Padding(0, 0, 6, 0);
            comboxOutputQuality.Name = "comboxOutputQuality";
            comboxOutputQuality.Size = new Size(100, 23);
            comboxOutputQuality.TabIndex = 48;
            comboxOutputQuality.SelectedIndexChanged += comboxOutputQuality_SelectedIndexChanged;
            // 
            // textboxOutputQualityCust
            // 
            textboxOutputQualityCust.BackColor = Color.FromArgb(64, 64, 64);
            textboxOutputQualityCust.BorderStyle = BorderStyle.FixedSingle;
            textboxOutputQualityCust.ForeColor = Color.White;
            textboxOutputQualityCust.Location = new Point(283, 0);
            textboxOutputQualityCust.Margin = new Padding(0, 0, 6, 0);
            textboxOutputQualityCust.MaxLength = 3;
            textboxOutputQualityCust.MinimumSize = new Size(4, 21);
            textboxOutputQualityCust.Name = "textboxOutputQualityCust";
            textboxOutputQualityCust.Size = new Size(30, 23);
            textboxOutputQualityCust.TabIndex = 52;
            textboxOutputQualityCust.Text = "24";
            textboxOutputQualityCust.Visible = false;
            // 
            // comboxOutputColors
            // 
            comboxOutputColors.BackColor = Color.FromArgb(64, 64, 64);
            comboxOutputColors.DropDownStyle = ComboBoxStyle.DropDownList;
            comboxOutputColors.FlatStyle = FlatStyle.Flat;
            comboxOutputColors.ForeColor = Color.White;
            comboxOutputColors.FormattingEnabled = true;
            comboxOutputColors.Location = new Point(319, 0);
            comboxOutputColors.Margin = new Padding(0);
            comboxOutputColors.Name = "comboxOutputColors";
            comboxOutputColors.Size = new Size(117, 23);
            comboxOutputColors.TabIndex = 49;
            // 
            // outSpeedCombox
            // 
            outSpeedCombox.BackColor = Color.FromArgb(64, 64, 64);
            outSpeedCombox.DropDownStyle = ComboBoxStyle.DropDownList;
            outSpeedCombox.FlatStyle = FlatStyle.Flat;
            outSpeedCombox.ForeColor = Color.White;
            outSpeedCombox.FormattingEnabled = true;
            outSpeedCombox.Items.AddRange(new object[] { "Normal Speed", "2x Slowmo", "4x Slowmo", "8x Slowmo" });
            outSpeedCombox.Location = new Point(574, 97);
            outSpeedCombox.Name = "outSpeedCombox";
            outSpeedCombox.Size = new Size(107, 23);
            outSpeedCombox.TabIndex = 43;
            // 
            // completionActionPanel
            // 
            completionActionPanel.Controls.Add(completionAction);
            completionActionPanel.Controls.Add(label25);
            completionActionPanel.Location = new Point(681, 218);
            completionActionPanel.Margin = new Padding(0);
            completionActionPanel.Name = "completionActionPanel";
            completionActionPanel.Size = new Size(220, 40);
            completionActionPanel.TabIndex = 42;
            // 
            // completionAction
            // 
            completionAction.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            completionAction.BackColor = Color.FromArgb(64, 64, 64);
            completionAction.DropDownStyle = ComboBoxStyle.DropDownList;
            completionAction.FlatStyle = FlatStyle.Flat;
            completionAction.ForeColor = Color.White;
            completionAction.FormattingEnabled = true;
            completionAction.Items.AddRange(new object[] { "Do Nothing", "Close App", "Sleep", "Hibernate", "Shutdown" });
            completionAction.Location = new Point(84, 11);
            completionAction.Name = "completionAction";
            completionAction.Size = new Size(130, 23);
            completionAction.TabIndex = 40;
            // 
            // label25
            // 
            label25.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            label25.AutoSize = true;
            label25.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label25.ForeColor = Color.Silver;
            label25.Location = new Point(10, 15);
            label25.Margin = new Padding(8, 0, 3, 0);
            label25.Name = "label25";
            label25.Size = new Size(68, 13);
            label25.TabIndex = 41;
            label25.Text = "When Done:";
            // 
            // inputInfo
            // 
            inputInfo.AutoSize = true;
            inputInfo.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            inputInfo.ForeColor = Color.Silver;
            inputInfo.Location = new Point(283, 193);
            inputInfo.Margin = new Padding(8, 0, 3, 0);
            inputInfo.Name = "inputInfo";
            inputInfo.Size = new Size(0, 13);
            inputInfo.TabIndex = 37;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Silver;
            label1.Location = new Point(14, 193);
            label1.Margin = new Padding(8, 0, 3, 0);
            label1.Name = "label1";
            label1.Size = new Size(89, 13);
            label1.TabIndex = 36;
            label1.Text = "Current Input Info";
            // 
            // quickSettingsTab
            // 
            quickSettingsTab.BackColor = Color.FromArgb(48, 48, 48);
            quickSettingsTab.Controls.Add(panel2);
            quickSettingsTab.Controls.Add(label20);
            quickSettingsTab.Controls.Add(maxFps);
            quickSettingsTab.Controls.Add(label24);
            quickSettingsTab.Controls.Add(linkLabel1);
            quickSettingsTab.Controls.Add(mpDedupePanel);
            quickSettingsTab.Controls.Add(magickDedupePanel);
            quickSettingsTab.Controls.Add(dedupeSensLabel);
            quickSettingsTab.Controls.Add(dedupMode);
            quickSettingsTab.Controls.Add(scnDetectValue);
            quickSettingsTab.Controls.Add(panel14);
            quickSettingsTab.Controls.Add(label52);
            quickSettingsTab.Controls.Add(label51);
            quickSettingsTab.Controls.Add(scnDetect);
            quickSettingsTab.Controls.Add(enableLoop);
            quickSettingsTab.Controls.Add(label34);
            quickSettingsTab.Controls.Add(panel10);
            quickSettingsTab.Controls.Add(maxVidHeight);
            quickSettingsTab.Controls.Add(label18);
            quickSettingsTab.Controls.Add(trimPanel);
            quickSettingsTab.Controls.Add(trimCombox);
            quickSettingsTab.Controls.Add(label16);
            quickSettingsTab.Controls.Add(label17);
            quickSettingsTab.Controls.Add(label19);
            quickSettingsTab.Controls.Add(label23);
            quickSettingsTab.Location = new Point(4, 27);
            quickSettingsTab.Name = "quickSettingsTab";
            quickSettingsTab.Padding = new Padding(3);
            quickSettingsTab.Size = new Size(901, 258);
            quickSettingsTab.TabIndex = 1;
            quickSettingsTab.Text = "Quick Settings";
            quickSettingsTab.Enter += LoadQuickSettings;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.ForeColor = Color.Silver;
            label20.Location = new Point(420, 162);
            label20.Margin = new Padding(10, 10, 10, 7);
            label20.Name = "label20";
            label20.Size = new Size(339, 15);
            label20.TabIndex = 89;
            label20.Text = "Limit the final output video to this FPS. Leave empty to disable.";
            // 
            // maxFps
            // 
            maxFps.BackColor = Color.FromArgb(64, 64, 64);
            maxFps.FlatStyle = FlatStyle.Flat;
            maxFps.ForeColor = Color.White;
            maxFps.FormattingEnabled = true;
            maxFps.Items.AddRange(new object[] { "0", "30", "60", "120" });
            maxFps.Location = new Point(280, 157);
            maxFps.Name = "maxFps";
            maxFps.Size = new Size(100, 23);
            maxFps.TabIndex = 88;
            maxFps.SelectedIndexChanged += SaveQuickSettings;
            maxFps.TextChanged += SaveQuickSettings;
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.ForeColor = Color.White;
            label24.Location = new Point(6, 160);
            label24.Margin = new Padding(10, 10, 10, 7);
            label24.Name = "label24";
            label24.Size = new Size(165, 15);
            label24.TabIndex = 87;
            label24.Text = "Maximum Output Frame Rate";
            // 
            // linkLabel1
            // 
            linkLabel1.ActiveLinkColor = Color.LightSkyBlue;
            linkLabel1.AutoSize = true;
            linkLabel1.LinkColor = Color.DodgerBlue;
            linkLabel1.Location = new Point(6, 237);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(227, 15);
            linkLabel1.TabIndex = 86;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "Open the Settings Window for all options.";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // mpDedupePanel
            // 
            mpDedupePanel.Controls.Add(mpdecimateMode);
            mpDedupePanel.Location = new Point(601, 67);
            mpDedupePanel.Margin = new Padding(0);
            mpDedupePanel.Name = "mpDedupePanel";
            mpDedupePanel.Size = new Size(135, 23);
            mpDedupePanel.TabIndex = 85;
            // 
            // mpdecimateMode
            // 
            mpdecimateMode.BackColor = Color.FromArgb(64, 64, 64);
            mpdecimateMode.DropDownStyle = ComboBoxStyle.DropDownList;
            mpdecimateMode.FlatStyle = FlatStyle.Flat;
            mpdecimateMode.ForeColor = Color.White;
            mpdecimateMode.FormattingEnabled = true;
            mpdecimateMode.Items.AddRange(new object[] { "Normal", "High", "Very High", "Extreme" });
            mpdecimateMode.Location = new Point(0, 0);
            mpdecimateMode.Margin = new Padding(3, 3, 8, 3);
            mpdecimateMode.Name = "mpdecimateMode";
            mpdecimateMode.Size = new Size(135, 23);
            mpdecimateMode.TabIndex = 28;
            mpdecimateMode.SelectedIndexChanged += SaveQuickSettings;
            // 
            // magickDedupePanel
            // 
            magickDedupePanel.Controls.Add(dedupThresh);
            magickDedupePanel.Controls.Add(panel3);
            magickDedupePanel.Location = new Point(601, 67);
            magickDedupePanel.Margin = new Padding(0);
            magickDedupePanel.Name = "magickDedupePanel";
            magickDedupePanel.Size = new Size(135, 23);
            magickDedupePanel.TabIndex = 84;
            // 
            // dedupThresh
            // 
            dedupThresh.BackColor = Color.FromArgb(64, 64, 64);
            dedupThresh.DecimalPlaces = 1;
            dedupThresh.ForeColor = Color.White;
            dedupThresh.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            dedupThresh.Location = new Point(0, 0);
            dedupThresh.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            dedupThresh.Minimum = new decimal(new int[] { 5, 0, 0, 65536 });
            dedupThresh.Name = "dedupThresh";
            dedupThresh.Size = new Size(103, 23);
            dedupThresh.TabIndex = 75;
            dedupThresh.Tag = "";
            dedupThresh.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            dedupThresh.ValueChanged += SaveQuickSettings;
            // 
            // dedupeSensLabel
            // 
            dedupeSensLabel.AutoSize = true;
            dedupeSensLabel.ForeColor = Color.White;
            dedupeSensLabel.Location = new Point(535, 71);
            dedupeSensLabel.Margin = new Padding(3);
            dedupeSensLabel.Name = "dedupeSensLabel";
            dedupeSensLabel.Size = new Size(63, 15);
            dedupeSensLabel.TabIndex = 83;
            dedupeSensLabel.Text = "Sensitivity:";
            // 
            // dedupMode
            // 
            dedupMode.BackColor = Color.FromArgb(64, 64, 64);
            dedupMode.DropDownStyle = ComboBoxStyle.DropDownList;
            dedupMode.FlatStyle = FlatStyle.Flat;
            dedupMode.ForeColor = Color.White;
            dedupMode.FormattingEnabled = true;
            dedupMode.Items.AddRange(new object[] { "Disabled", "1: After Extraction - Slow, Accurate", "2: During Extraction - Fast, Less Accurate" });
            dedupMode.Location = new Point(279, 67);
            dedupMode.Name = "dedupMode";
            dedupMode.Size = new Size(250, 23);
            dedupMode.TabIndex = 81;
            dedupMode.SelectedIndexChanged += dedupMode_SelectedIndexChanged;
            // 
            // scnDetectValue
            // 
            scnDetectValue.BackColor = Color.FromArgb(64, 64, 64);
            scnDetectValue.DecimalPlaces = 2;
            scnDetectValue.ForeColor = Color.White;
            scnDetectValue.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            scnDetectValue.Location = new Point(370, 128);
            scnDetectValue.Maximum = new decimal(new int[] { 5, 0, 0, 65536 });
            scnDetectValue.Minimum = new decimal(new int[] { 2, 0, 0, 131072 });
            scnDetectValue.Name = "scnDetectValue";
            scnDetectValue.Size = new Size(100, 23);
            scnDetectValue.TabIndex = 80;
            scnDetectValue.Value = new decimal(new int[] { 5, 0, 0, 131072 });
            scnDetectValue.ValueChanged += SaveQuickSettings;
            // 
            // label52
            // 
            label52.AutoSize = true;
            label52.ForeColor = Color.Silver;
            label52.Location = new Point(510, 133);
            label52.Margin = new Padding(10, 10, 10, 7);
            label52.Name = "label52";
            label52.Size = new Size(246, 15);
            label52.TabIndex = 78;
            label52.Text = "Lower values will detect more scene changes.";
            // 
            // label51
            // 
            label51.AutoSize = true;
            label51.ForeColor = Color.White;
            label51.Location = new Point(301, 130);
            label51.Margin = new Padding(3);
            label51.Name = "label51";
            label51.Size = new Size(63, 15);
            label51.TabIndex = 77;
            label51.Text = "Sensitivity:";
            // 
            // scnDetect
            // 
            scnDetect.AutoSize = true;
            scnDetect.Location = new Point(280, 131);
            scnDetect.Name = "scnDetect";
            scnDetect.Size = new Size(15, 14);
            scnDetect.TabIndex = 76;
            scnDetect.UseVisualStyleBackColor = true;
            scnDetect.CheckedChanged += SaveQuickSettings;
            // 
            // enableLoop
            // 
            enableLoop.AutoSize = true;
            enableLoop.Location = new Point(280, 101);
            enableLoop.Name = "enableLoop";
            enableLoop.Size = new Size(15, 14);
            enableLoop.TabIndex = 75;
            enableLoop.UseVisualStyleBackColor = true;
            enableLoop.CheckedChanged += SaveQuickSettings;
            // 
            // label34
            // 
            label34.AutoSize = true;
            label34.ForeColor = Color.Silver;
            label34.Location = new Point(407, 42);
            label34.Margin = new Padding(10, 10, 10, 7);
            label34.Name = "label34";
            label34.Size = new Size(308, 15);
            label34.TabIndex = 69;
            label34.Text = "Maximum Height. Video will be downscaled if it's bigger.";
            // 
            // maxVidHeight
            // 
            maxVidHeight.BackColor = Color.FromArgb(64, 64, 64);
            maxVidHeight.FlatStyle = FlatStyle.Flat;
            maxVidHeight.ForeColor = Color.White;
            maxVidHeight.FormattingEnabled = true;
            maxVidHeight.Items.AddRange(new object[] { "4320", "2160", "1440", "1080", "720", "540", "360" });
            maxVidHeight.Location = new Point(280, 37);
            maxVidHeight.Name = "maxVidHeight";
            maxVidHeight.Size = new Size(87, 23);
            maxVidHeight.TabIndex = 68;
            maxVidHeight.SelectedIndexChanged += SaveQuickSettings;
            maxVidHeight.TextChanged += SaveQuickSettings;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.ForeColor = Color.White;
            label18.Location = new Point(8, 40);
            label18.Margin = new Padding(8, 0, 3, 0);
            label18.Name = "label18";
            label18.Size = new Size(196, 15);
            label18.TabIndex = 66;
            label18.Text = "Maximum Video Input Size (Height)";
            // 
            // trimPanel
            // 
            trimPanel.Controls.Add(trimStartBox);
            trimPanel.Controls.Add(label10);
            trimPanel.Controls.Add(trimEndBox);
            trimPanel.Location = new Point(433, 7);
            trimPanel.Margin = new Padding(0);
            trimPanel.Name = "trimPanel";
            trimPanel.Size = new Size(426, 23);
            trimPanel.TabIndex = 65;
            // 
            // trimStartBox
            // 
            trimStartBox.AllowDrop = true;
            trimStartBox.BackColor = Color.FromArgb(64, 64, 64);
            trimStartBox.ForeColor = Color.White;
            trimStartBox.Location = new Point(3, 0);
            trimStartBox.MinimumSize = new Size(4, 21);
            trimStartBox.Name = "trimStartBox";
            trimStartBox.Size = new Size(75, 23);
            trimStartBox.TabIndex = 62;
            trimStartBox.Text = "00:00:00";
            trimStartBox.TextChanged += trimBox_TextChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.ForeColor = Color.White;
            label10.Location = new Point(84, 3);
            label10.Name = "label10";
            label10.Size = new Size(12, 15);
            label10.TabIndex = 64;
            label10.Text = "-";
            // 
            // trimEndBox
            // 
            trimEndBox.AllowDrop = true;
            trimEndBox.BackColor = Color.FromArgb(64, 64, 64);
            trimEndBox.ForeColor = Color.White;
            trimEndBox.Location = new Point(102, 0);
            trimEndBox.MinimumSize = new Size(4, 21);
            trimEndBox.Name = "trimEndBox";
            trimEndBox.Size = new Size(75, 23);
            trimEndBox.TabIndex = 63;
            trimEndBox.Text = "00:00:00";
            trimEndBox.Leave += trimBox_TextChanged;
            // 
            // trimCombox
            // 
            trimCombox.BackColor = Color.FromArgb(64, 64, 64);
            trimCombox.DropDownStyle = ComboBoxStyle.DropDownList;
            trimCombox.FlatStyle = FlatStyle.Flat;
            trimCombox.ForeColor = Color.White;
            trimCombox.FormattingEnabled = true;
            trimCombox.Items.AddRange(new object[] { "Don't Trim", "Trim by Start/End Time" });
            trimCombox.Location = new Point(280, 7);
            trimCombox.Name = "trimCombox";
            trimCombox.Size = new Size(150, 23);
            trimCombox.TabIndex = 61;
            trimCombox.SelectedIndexChanged += trimCombox_SelectedIndexChanged;
            // 
            // previewTab
            // 
            previewTab.BackColor = Color.FromArgb(48, 48, 48);
            previewTab.Controls.Add(label22);
            previewTab.Controls.Add(previewPicturebox);
            previewTab.Location = new Point(4, 27);
            previewTab.Margin = new Padding(0);
            previewTab.Name = "previewTab";
            previewTab.Size = new Size(901, 258);
            previewTab.TabIndex = 3;
            previewTab.Text = "Preview";
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.ForeColor = Color.Silver;
            label22.Location = new Point(5, 5);
            label22.Margin = new Padding(3);
            label22.MaximumSize = new Size(160, 0);
            label22.Name = "label22";
            label22.Size = new Size(158, 30);
            label22.TabIndex = 38;
            label22.Text = "Click on the preview to open it in a separate window.";
            label22.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // previewPicturebox
            // 
            previewPicturebox.Dock = DockStyle.Fill;
            previewPicturebox.Image = Properties.Resources.baseline_image_white_48dp_4x_25pcAlpha;
            previewPicturebox.Location = new Point(0, 0);
            previewPicturebox.Margin = new Padding(0);
            previewPicturebox.Name = "previewPicturebox";
            previewPicturebox.Size = new Size(901, 258);
            previewPicturebox.SizeMode = PictureBoxSizeMode.Zoom;
            previewPicturebox.TabIndex = 0;
            previewPicturebox.TabStop = false;
            previewPicturebox.MouseClick += previewPicturebox_MouseClick;
            // 
            // abtTab
            // 
            abtTab.BackColor = Color.FromArgb(48, 48, 48);
            abtTab.Controls.Add(label14);
            abtTab.Location = new Point(4, 27);
            abtTab.Name = "abtTab";
            abtTab.Padding = new Padding(3);
            abtTab.Size = new Size(901, 258);
            abtTab.TabIndex = 2;
            abtTab.Text = "About";
            // 
            // runStepBtn
            // 
            runStepBtn.BackColor = Color.FromArgb(48, 48, 48);
            runStepBtn.FlatStyle = FlatStyle.Flat;
            runStepBtn.ForeColor = Color.White;
            runStepBtn.Location = new Point(12, 442);
            runStepBtn.Margin = new Padding(4, 0, 4, 3);
            runStepBtn.Name = "runStepBtn";
            runStepBtn.Size = new Size(237, 54);
            runStepBtn.TabIndex = 42;
            runStepBtn.Text = "Run This Step";
            runStepBtn.UseVisualStyleBackColor = false;
            runStepBtn.Click += runStepBtn_Click;
            // 
            // stepSelector
            // 
            stepSelector.BackColor = Color.FromArgb(64, 64, 64);
            stepSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            stepSelector.FlatStyle = FlatStyle.Flat;
            stepSelector.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            stepSelector.ForeColor = Color.White;
            stepSelector.FormattingEnabled = true;
            stepSelector.Items.AddRange(new object[] { "1) Import/Extract Frames", "2) Run Interpolation", "3) Export", "4) Cleanup & Reset" });
            stepSelector.Location = new Point(12, 418);
            stepSelector.Margin = new Padding(4, 3, 4, 0);
            stepSelector.Name = "stepSelector";
            stepSelector.Size = new Size(236, 24);
            stepSelector.TabIndex = 73;
            // 
            // busyControlsPanel
            // 
            busyControlsPanel.BackColor = Color.FromArgb(48, 48, 48);
            busyControlsPanel.Controls.Add(tableLayoutPanel1);
            busyControlsPanel.Location = new Point(12, 418);
            busyControlsPanel.Margin = new Padding(4, 3, 4, 3);
            busyControlsPanel.Name = "busyControlsPanel";
            busyControlsPanel.Size = new Size(237, 82);
            busyControlsPanel.TabIndex = 74;
            busyControlsPanel.Visible = false;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 51F));
            tableLayoutPanel1.Controls.Add(pauseBtn, 1, 0);
            tableLayoutPanel1.Controls.Add(cancelBtn, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(237, 82);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // pauseBtn
            // 
            pauseBtn.Anchor = AnchorStyles.None;
            pauseBtn.BackgroundImage = Properties.Resources.baseline_pause_white_48dp;
            pauseBtn.BackgroundImageLayout = ImageLayout.Zoom;
            pauseBtn.FlatAppearance.MouseOverBackColor = Color.DarkOrange;
            pauseBtn.FlatStyle = FlatStyle.Flat;
            pauseBtn.ForeColor = Color.DarkOrange;
            pauseBtn.Location = new Point(147, 12);
            pauseBtn.Margin = new Padding(4, 3, 4, 3);
            pauseBtn.Name = "pauseBtn";
            pauseBtn.Size = new Size(58, 58);
            pauseBtn.TabIndex = 1;
            pauseBtn.UseVisualStyleBackColor = true;
            pauseBtn.Visible = false;
            pauseBtn.Click += pauseBtn_Click;
            // 
            // cancelBtn
            // 
            cancelBtn.Anchor = AnchorStyles.None;
            cancelBtn.BackgroundImage = Properties.Resources.baseline_stop_white_48dp;
            cancelBtn.BackgroundImageLayout = ImageLayout.Zoom;
            cancelBtn.FlatAppearance.MouseOverBackColor = Color.Firebrick;
            cancelBtn.FlatStyle = FlatStyle.Flat;
            cancelBtn.ForeColor = Color.Firebrick;
            cancelBtn.Location = new Point(29, 12);
            cancelBtn.Margin = new Padding(4, 3, 4, 3);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new Size(58, 58);
            cancelBtn.TabIndex = 0;
            cancelBtn.UseVisualStyleBackColor = true;
            cancelBtn.Click += cancelBtn_Click;
            // 
            // menuStripQueue
            // 
            menuStripQueue.Items.AddRange(new ToolStripItem[] { addCurrentConfigurationToQueueToolStripMenuItem });
            menuStripQueue.Name = "menuStripQueue";
            menuStripQueue.Size = new Size(269, 26);
            // 
            // addCurrentConfigurationToQueueToolStripMenuItem
            // 
            addCurrentConfigurationToQueueToolStripMenuItem.Name = "addCurrentConfigurationToQueueToolStripMenuItem";
            addCurrentConfigurationToQueueToolStripMenuItem.Size = new Size(268, 22);
            addCurrentConfigurationToQueueToolStripMenuItem.Text = "Add Current Configuration to Queue";
            addCurrentConfigurationToQueueToolStripMenuItem.Click += addCurrentConfigurationToQueueToolStripMenuItem_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPageWelcome);
            tabControl1.Controls.Add(tabPageInterp);
            tabControl1.Location = new Point(14, 54);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(909, 289);
            tabControl1.TabIndex = 75;
            // 
            // tabPageWelcome
            // 
            tabPageWelcome.BackColor = Color.FromArgb(48, 48, 48);
            tabPageWelcome.Controls.Add(welcomeLabel2);
            tabPageWelcome.Controls.Add(panel8);
            tabPageWelcome.Controls.Add(panel6);
            tabPageWelcome.Controls.Add(label11);
            tabPageWelcome.ForeColor = Color.White;
            tabPageWelcome.Location = new Point(4, 24);
            tabPageWelcome.Name = "tabPageWelcome";
            tabPageWelcome.Padding = new Padding(3);
            tabPageWelcome.Size = new Size(901, 261);
            tabPageWelcome.TabIndex = 0;
            tabPageWelcome.Text = "Welcome";
            // 
            // tabPageInterp
            // 
            tabPageInterp.BackColor = Color.FromArgb(48, 48, 48);
            tabPageInterp.Controls.Add(labelOutput);
            tabPageInterp.Controls.Add(flowLayoutPanel1);
            tabPageInterp.Controls.Add(pictureBox5);
            tabPageInterp.Controls.Add(outSpeedCombox);
            tabPageInterp.Controls.Add(completionActionPanel);
            tabPageInterp.Controls.Add(inputInfo);
            tabPageInterp.Controls.Add(label1);
            tabPageInterp.Controls.Add(label4);
            tabPageInterp.Controls.Add(label2);
            tabPageInterp.Controls.Add(label3);
            tabPageInterp.Controls.Add(inputTbox);
            tabPageInterp.Controls.Add(outputTbox);
            tabPageInterp.Controls.Add(interpFactorCombox);
            tabPageInterp.Controls.Add(aiModel);
            tabPageInterp.Controls.Add(fpsInTbox);
            tabPageInterp.Controls.Add(fpsOutTbox);
            tabPageInterp.Controls.Add(aiCombox);
            tabPageInterp.Controls.Add(label6);
            tabPageInterp.Controls.Add(label13);
            tabPageInterp.Controls.Add(label8);
            tabPageInterp.Controls.Add(label9);
            tabPageInterp.Controls.Add(pictureBox4);
            tabPageInterp.Controls.Add(pictureBox3);
            tabPageInterp.Controls.Add(pictureBox2);
            tabPageInterp.Controls.Add(pictureBox1);
            tabPageInterp.Controls.Add(info1);
            tabPageInterp.ForeColor = Color.White;
            tabPageInterp.Location = new Point(4, 24);
            tabPageInterp.Name = "tabPageInterp";
            tabPageInterp.Padding = new Padding(3);
            tabPageInterp.Size = new Size(1053, 324);
            tabPageInterp.TabIndex = 1;
            tabPageInterp.Text = "Interpolation";
            // 
            // Form1
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(934, 501);
            Controls.Add(tabControl1);
            Controls.Add(busyControlsPanel);
            Controls.Add(runBtn);
            Controls.Add(stepSelector);
            Controls.Add(runStepBtn);
            Controls.Add(panel1);
            Controls.Add(logBox);
            Controls.Add(titleLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Flowframes";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            Shown += Form1_Shown;
            DragDrop += Form1_DragDrop;
            DragEnter += Form1_DragEnter;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)info1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).EndInit();
            panel8.ResumeLayout(false);
            panel8.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            completionActionPanel.ResumeLayout(false);
            completionActionPanel.PerformLayout();
            quickSettingsTab.ResumeLayout(false);
            quickSettingsTab.PerformLayout();
            mpDedupePanel.ResumeLayout(false);
            magickDedupePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dedupThresh).EndInit();
            ((System.ComponentModel.ISupportInitialize)scnDetectValue).EndInit();
            trimPanel.ResumeLayout(false);
            trimPanel.PerformLayout();
            previewTab.ResumeLayout(false);
            previewTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)previewPicturebox).EndInit();
            abtTab.ResumeLayout(false);
            busyControlsPanel.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            menuStripQueue.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPageWelcome.ResumeLayout(false);
            tabPageWelcome.PerformLayout();
            tabPageInterp.ResumeLayout(false);
            tabPageInterp.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button runBtn;
        private System.Windows.Forms.TextBox inputTbox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox outputTbox;
        private System.Windows.Forms.ComboBox interpFactorCombox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox fpsOutTbox;
        private System.Windows.Forms.TextBox fpsInTbox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private CircularProgressBar.CircularProgressBar progressCircle;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.FolderBrowserDialog inFolderDialog;
        private System.Windows.Forms.OpenFileDialog inFileDialog;
        private System.Windows.Forms.FolderBrowserDialog outFolderDialog;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ComboBox aiCombox;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox aiModel;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.PictureBox info1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox4;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Label label23;
        private HTAlt.WinForms.HTProgressBar longProgBar;
        private HTAlt.WinForms.HTButton discordBtn;
        private HTAlt.WinForms.HTButton paypalBtn;
        private HTAlt.WinForms.HTButton patreonBtn;
        private HTAlt.WinForms.HTButton settingsBtn;
        private HTAlt.WinForms.HTButton browseOutBtn;
        private HTAlt.WinForms.HTButton browseInputFileBtn;
        private HTAlt.WinForms.HTButton browseInputBtn;
        private System.Windows.Forms.PictureBox previewPicturebox;
        public HTAlt.WinForms.HTTabControl mainTabControl;
        private HTAlt.WinForms.HTButton queueBtn;
        private HTAlt.WinForms.HTButton htButton1;
        private HTAlt.WinForms.HTButton updateBtn;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.Label patronsLabel;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label newsLabel;
        private System.Windows.Forms.Label welcomeLabel2;
        private System.Windows.Forms.Button runStepBtn;
        private System.Windows.Forms.ComboBox stepSelector;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label inputInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox trimEndBox;
        private System.Windows.Forms.TextBox trimStartBox;
        private System.Windows.Forms.ComboBox trimCombox;
        private System.Windows.Forms.Panel trimPanel;
        private HTAlt.WinForms.HTButton trimResetBtn;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.ComboBox maxVidHeight;
        private System.Windows.Forms.CheckBox scnDetect;
        private System.Windows.Forms.CheckBox enableLoop;
        private System.Windows.Forms.NumericUpDown scnDetectValue;
        private System.Windows.Forms.Panel panel14;
        private System.Windows.Forms.Label label52;
        private System.Windows.Forms.Label label51;
        private System.Windows.Forms.Label dedupeSensLabel;
        private System.Windows.Forms.ComboBox dedupMode;
        private System.Windows.Forms.Panel magickDedupePanel;
        private System.Windows.Forms.NumericUpDown dedupThresh;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel mpDedupePanel;
        private System.Windows.Forms.ComboBox mpdecimateMode;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Panel busyControlsPanel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button pauseBtn;
        private System.Windows.Forms.Button cancelBtn;
        private HTAlt.WinForms.HTButton debugBtn;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.ComboBox maxFps;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Panel completionActionPanel;
        private System.Windows.Forms.ComboBox completionAction;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.ComboBox outSpeedCombox;
        private System.Windows.Forms.PictureBox pictureBox5;
        private HTAlt.WinForms.HTButton aiInfoBtn;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        public System.Windows.Forms.ComboBox comboxOutputFormat;
        public System.Windows.Forms.ComboBox comboxOutputColors;
        public System.Windows.Forms.ComboBox comboxOutputEncoder;
        public System.Windows.Forms.ComboBox comboxOutputQuality;
        private System.Windows.Forms.Label labelOutput;
        private System.Windows.Forms.ContextMenuStrip menuStripQueue;
        private System.Windows.Forms.ToolStripMenuItem addCurrentConfigurationToQueueToolStripMenuItem;
        private System.Windows.Forms.TextBox textboxOutputQualityCust;
        public System.Windows.Forms.TabPage interpOptsTab;
        public System.Windows.Forms.TabPage quickSettingsTab;
        public System.Windows.Forms.TabPage abtTab;
        public System.Windows.Forms.TabPage previewTab;
        public System.Windows.Forms.TabPage welcomeTab;
        private TabControl tabControl1;
        private TabPage tabPageWelcome;
        private TabPage tabPageInterp;
    }
}

