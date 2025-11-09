namespace Flowframes.Forms
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.settingsTabList = new Cyotek.Windows.Forms.TabList();
            this.generalTab = new Cyotek.Windows.Forms.TabListPage();
            this.flowPanelApplication = new System.Windows.Forms.FlowLayoutPanel();
            this.panProcessingStyle = new System.Windows.Forms.Panel();
            this.label39 = new System.Windows.Forms.Label();
            this.processingMode = new System.Windows.Forms.ComboBox();
            this.panMaxRes = new System.Windows.Forms.Panel();
            this.label31 = new System.Windows.Forms.Label();
            this.maxVidHeight = new System.Windows.Forms.ComboBox();
            this.panel10 = new System.Windows.Forms.Panel();
            this.label34 = new System.Windows.Forms.Label();
            this.panTempFolder = new System.Windows.Forms.Panel();
            this.label36 = new System.Windows.Forms.Label();
            this.tempFolderLoc = new System.Windows.Forms.ComboBox();
            this.tempDirCustom = new System.Windows.Forms.TextBox();
            this.tempDirBrowseBtn = new HTAlt.WinForms.HTButton();
            this.panOutputLocation = new System.Windows.Forms.Panel();
            this.label78 = new System.Windows.Forms.Label();
            this.outFolderLoc = new System.Windows.Forms.ComboBox();
            this.custOutDir = new System.Windows.Forms.TextBox();
            this.custOutDirBrowseBtn = new HTAlt.WinForms.HTButton();
            this.panKeepTempFolder = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.keepTempFolder = new System.Windows.Forms.CheckBox();
            this.panExportName = new System.Windows.Forms.Panel();
            this.label67 = new System.Windows.Forms.Label();
            this.exportNamePattern = new System.Windows.Forms.TextBox();
            this.info1 = new System.Windows.Forms.PictureBox();
            this.label68 = new System.Windows.Forms.Label();
            this.exportNamePatternLoop = new System.Windows.Forms.TextBox();
            this.label69 = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label64 = new System.Windows.Forms.Label();
            this.clearModelCacheBtn = new HTAlt.WinForms.HTButton();
            this.modelDownloaderBtn = new HTAlt.WinForms.HTButton();
            this.panel11 = new System.Windows.Forms.Panel();
            this.label10 = new System.Windows.Forms.Label();
            this.btnResetHwEnc = new HTAlt.WinForms.HTButton();
            this.tabListPage2 = new Cyotek.Windows.Forms.TabListPage();
            this.flowPanelInterpolation = new System.Windows.Forms.FlowLayoutPanel();
            this.panTitleInputMedia = new System.Windows.Forms.Panel();
            this.label24 = new System.Windows.Forms.Label();
            this.panCopyInputMedia = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.keepAudio = new System.Windows.Forms.CheckBox();
            this.keepSubs = new System.Windows.Forms.CheckBox();
            this.keepMeta = new System.Windows.Forms.CheckBox();
            this.panEnableAlpha = new System.Windows.Forms.Panel();
            this.label25 = new System.Windows.Forms.Label();
            this.enableAlpha = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.panHqJpegImport = new System.Windows.Forms.Panel();
            this.label63 = new System.Windows.Forms.Label();
            this.jpegFrames = new System.Windows.Forms.CheckBox();
            this.label74 = new System.Windows.Forms.Label();
            this.panTitleInterpHelpers = new System.Windows.Forms.Panel();
            this.label18 = new System.Windows.Forms.Label();
            this.panDedupe = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.dedupMode = new System.Windows.Forms.ComboBox();
            this.dedupeSensLabel = new System.Windows.Forms.Label();
            this.mpDedupePanel = new System.Windows.Forms.Panel();
            this.mpdecimateMode = new System.Windows.Forms.ComboBox();
            this.magickDedupePanel = new System.Windows.Forms.Panel();
            this.dedupThresh = new System.Windows.Forms.NumericUpDown();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panLoop = new System.Windows.Forms.Panel();
            this.label15 = new System.Windows.Forms.Label();
            this.enableLoop = new System.Windows.Forms.CheckBox();
            this.panSceneChange = new System.Windows.Forms.Panel();
            this.label50 = new System.Windows.Forms.Label();
            this.scnDetect = new System.Windows.Forms.CheckBox();
            this.label51 = new System.Windows.Forms.Label();
            this.label52 = new System.Windows.Forms.Label();
            this.panel14 = new System.Windows.Forms.Panel();
            this.scnDetectValue = new System.Windows.Forms.NumericUpDown();
            this.panAutoEnc = new System.Windows.Forms.Panel();
            this.label49 = new System.Windows.Forms.Label();
            this.autoEncMode = new System.Windows.Forms.ComboBox();
            this.panAutoEncInSbsMode = new System.Windows.Forms.Panel();
            this.label53 = new System.Windows.Forms.Label();
            this.sbsAllowAutoEnc = new System.Windows.Forms.CheckBox();
            this.panAutoEncBackups = new System.Windows.Forms.Panel();
            this.label16 = new System.Windows.Forms.Label();
            this.autoEncBackupMode = new System.Windows.Forms.ComboBox();
            this.label41 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.panAutoEncLowSpaceMode = new System.Windows.Forms.Panel();
            this.label58 = new System.Windows.Forms.Label();
            this.label70 = new System.Windows.Forms.Label();
            this.alwaysWaitForAutoEnc = new System.Windows.Forms.CheckBox();
            this.aiOptsPage = new Cyotek.Windows.Forms.TabListPage();
            this.flowPanelAiOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.panTitleAiFramework = new System.Windows.Forms.Panel();
            this.label32 = new System.Windows.Forms.Label();
            this.panTorchGpus = new System.Windows.Forms.Panel();
            this.tooltipTorchGpu = new System.Windows.Forms.PictureBox();
            this.label33 = new System.Windows.Forms.Label();
            this.torchGpus = new System.Windows.Forms.ComboBox();
            this.panNcnnGpus = new System.Windows.Forms.Panel();
            this.tooltipNcnnGpu = new System.Windows.Forms.PictureBox();
            this.label5 = new System.Windows.Forms.Label();
            this.ncnnGpus = new System.Windows.Forms.ComboBox();
            this.panNcnnThreads = new System.Windows.Forms.Panel();
            this.label43 = new System.Windows.Forms.Label();
            this.label44 = new System.Windows.Forms.Label();
            this.ncnnThreads = new System.Windows.Forms.NumericUpDown();
            this.panTitleRife = new System.Windows.Forms.Panel();
            this.label11 = new System.Windows.Forms.Label();
            this.panUhdThresh = new System.Windows.Forms.Panel();
            this.label29 = new System.Windows.Forms.Label();
            this.uhdThresh = new System.Windows.Forms.ComboBox();
            this.panel6 = new System.Windows.Forms.Panel();
            this.label30 = new System.Windows.Forms.Label();
            this.panRifeCudaHalfPrec = new System.Windows.Forms.Panel();
            this.label65 = new System.Windows.Forms.Label();
            this.label66 = new System.Windows.Forms.Label();
            this.rifeCudaFp16 = new System.Windows.Forms.CheckBox();
            this.panTitleDainNcnn = new System.Windows.Forms.Panel();
            this.label19 = new System.Windows.Forms.Label();
            this.panDainNcnnTileSize = new System.Windows.Forms.Panel();
            this.label27 = new System.Windows.Forms.Label();
            this.label35 = new System.Windows.Forms.Label();
            this.dainNcnnTilesize = new System.Windows.Forms.ComboBox();
            this.panel12 = new System.Windows.Forms.Panel();
            this.vidExportTab = new Cyotek.Windows.Forms.TabListPage();
            this.label73 = new System.Windows.Forms.Label();
            this.fixOutputDuration = new System.Windows.Forms.CheckBox();
            this.label72 = new System.Windows.Forms.Label();
            this.minOutVidLength = new System.Windows.Forms.NumericUpDown();
            this.loopMode = new System.Windows.Forms.ComboBox();
            this.label55 = new System.Windows.Forms.Label();
            this.panel8 = new System.Windows.Forms.Panel();
            this.panel7 = new System.Windows.Forms.Panel();
            this.label22 = new System.Windows.Forms.Label();
            this.maxFps = new System.Windows.Forms.ComboBox();
            this.label20 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.debugTab = new Cyotek.Windows.Forms.TabListPage();
            this.label7 = new System.Windows.Forms.Label();
            this.serverCombox = new System.Windows.Forms.ComboBox();
            this.ffEncArgs = new System.Windows.Forms.TextBox();
            this.label56 = new System.Windows.Forms.Label();
            this.label54 = new System.Windows.Forms.Label();
            this.ffEncPreset = new System.Windows.Forms.ComboBox();
            this.label47 = new System.Windows.Forms.Label();
            this.label46 = new System.Windows.Forms.Label();
            this.label45 = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.resetBtn = new HTAlt.WinForms.HTButton();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.onlyShowRelevantSettings = new System.Windows.Forms.CheckBox();
            this.settingsTabList.SuspendLayout();
            this.generalTab.SuspendLayout();
            this.flowPanelApplication.SuspendLayout();
            this.panProcessingStyle.SuspendLayout();
            this.panMaxRes.SuspendLayout();
            this.panTempFolder.SuspendLayout();
            this.panOutputLocation.SuspendLayout();
            this.panKeepTempFolder.SuspendLayout();
            this.panExportName.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.info1)).BeginInit();
            this.panel5.SuspendLayout();
            this.panel11.SuspendLayout();
            this.tabListPage2.SuspendLayout();
            this.flowPanelInterpolation.SuspendLayout();
            this.panTitleInputMedia.SuspendLayout();
            this.panCopyInputMedia.SuspendLayout();
            this.panEnableAlpha.SuspendLayout();
            this.panHqJpegImport.SuspendLayout();
            this.panTitleInterpHelpers.SuspendLayout();
            this.panDedupe.SuspendLayout();
            this.mpDedupePanel.SuspendLayout();
            this.magickDedupePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dedupThresh)).BeginInit();
            this.panLoop.SuspendLayout();
            this.panSceneChange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scnDetectValue)).BeginInit();
            this.panAutoEnc.SuspendLayout();
            this.panAutoEncInSbsMode.SuspendLayout();
            this.panAutoEncBackups.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.panAutoEncLowSpaceMode.SuspendLayout();
            this.aiOptsPage.SuspendLayout();
            this.flowPanelAiOptions.SuspendLayout();
            this.panTitleAiFramework.SuspendLayout();
            this.panTorchGpus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tooltipTorchGpu)).BeginInit();
            this.panNcnnGpus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tooltipNcnnGpu)).BeginInit();
            this.panNcnnThreads.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ncnnThreads)).BeginInit();
            this.panTitleRife.SuspendLayout();
            this.panUhdThresh.SuspendLayout();
            this.panRifeCudaHalfPrec.SuspendLayout();
            this.panTitleDainNcnn.SuspendLayout();
            this.panDainNcnnTileSize.SuspendLayout();
            this.vidExportTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minOutVidLength)).BeginInit();
            this.debugTab.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // settingsTabList
            // 
            this.settingsTabList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.settingsTabList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.settingsTabList.Controls.Add(this.generalTab);
            this.settingsTabList.Controls.Add(this.tabListPage2);
            this.settingsTabList.Controls.Add(this.aiOptsPage);
            this.settingsTabList.Controls.Add(this.vidExportTab);
            this.settingsTabList.Controls.Add(this.debugTab);
            this.settingsTabList.ForeColor = System.Drawing.Color.DodgerBlue;
            this.settingsTabList.Location = new System.Drawing.Point(12, 62);
            this.settingsTabList.Name = "settingsTabList";
            this.settingsTabList.Size = new System.Drawing.Size(920, 427);
            this.settingsTabList.TabIndex = 0;
            this.settingsTabList.SelectedIndexChanged += new System.EventHandler(this.settingsTabList_SelectedIndexChanged);
            // 
            // generalTab
            // 
            this.generalTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.generalTab.Controls.Add(this.flowPanelApplication);
            this.generalTab.ForeColor = System.Drawing.Color.White;
            this.generalTab.Name = "generalTab";
            this.generalTab.Size = new System.Drawing.Size(762, 419);
            this.generalTab.Text = "Application";
            // 
            // flowPanelApplication
            // 
            this.flowPanelApplication.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.flowPanelApplication.Controls.Add(this.panProcessingStyle);
            this.flowPanelApplication.Controls.Add(this.panMaxRes);
            this.flowPanelApplication.Controls.Add(this.panTempFolder);
            this.flowPanelApplication.Controls.Add(this.panOutputLocation);
            this.flowPanelApplication.Controls.Add(this.panKeepTempFolder);
            this.flowPanelApplication.Controls.Add(this.panExportName);
            this.flowPanelApplication.Controls.Add(this.panel5);
            this.flowPanelApplication.Controls.Add(this.panel11);
            this.flowPanelApplication.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowPanelApplication.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowPanelApplication.Location = new System.Drawing.Point(0, 0);
            this.flowPanelApplication.Margin = new System.Windows.Forms.Padding(0);
            this.flowPanelApplication.Name = "flowPanelApplication";
            this.flowPanelApplication.Size = new System.Drawing.Size(762, 419);
            this.flowPanelApplication.TabIndex = 96;
            // 
            // panProcessingStyle
            // 
            this.panProcessingStyle.Controls.Add(this.label39);
            this.panProcessingStyle.Controls.Add(this.processingMode);
            this.panProcessingStyle.Location = new System.Drawing.Point(0, 0);
            this.panProcessingStyle.Margin = new System.Windows.Forms.Padding(0);
            this.panProcessingStyle.Name = "panProcessingStyle";
            this.panProcessingStyle.Size = new System.Drawing.Size(762, 30);
            this.panProcessingStyle.TabIndex = 0;
            // 
            // label39
            // 
            this.label39.AutoSize = true;
            this.label39.Location = new System.Drawing.Point(10, 10);
            this.label39.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(85, 13);
            this.label39.TabIndex = 71;
            this.label39.Text = "Processing Style";
            // 
            // processingMode
            // 
            this.processingMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.processingMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.processingMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.processingMode.ForeColor = System.Drawing.Color.White;
            this.processingMode.FormattingEnabled = true;
            this.processingMode.Items.AddRange(new object[] {
            "Do All Steps At Once (Extract, Interpolate, Encode)",
            "Run Each Step Separately (For Manually Editing Frames)"});
            this.processingMode.Location = new System.Drawing.Point(280, 7);
            this.processingMode.Name = "processingMode";
            this.processingMode.Size = new System.Drawing.Size(300, 21);
            this.processingMode.TabIndex = 72;
            // 
            // panMaxRes
            // 
            this.panMaxRes.Controls.Add(this.label31);
            this.panMaxRes.Controls.Add(this.maxVidHeight);
            this.panMaxRes.Controls.Add(this.panel10);
            this.panMaxRes.Controls.Add(this.label34);
            this.panMaxRes.Location = new System.Drawing.Point(0, 30);
            this.panMaxRes.Margin = new System.Windows.Forms.Padding(0);
            this.panMaxRes.Name = "panMaxRes";
            this.panMaxRes.Size = new System.Drawing.Size(762, 30);
            this.panMaxRes.TabIndex = 1;
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(10, 10);
            this.label31.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(171, 13);
            this.label31.TabIndex = 62;
            this.label31.Text = "Maximum Video Input Size (Height)";
            // 
            // maxVidHeight
            // 
            this.maxVidHeight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.maxVidHeight.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.maxVidHeight.ForeColor = System.Drawing.Color.White;
            this.maxVidHeight.FormattingEnabled = true;
            this.maxVidHeight.Items.AddRange(new object[] {
            "4320",
            "2160",
            "1440",
            "1080",
            "720",
            "540",
            "360"});
            this.maxVidHeight.Location = new System.Drawing.Point(280, 7);
            this.maxVidHeight.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this.maxVidHeight.Name = "maxVidHeight";
            this.maxVidHeight.Size = new System.Drawing.Size(87, 21);
            this.maxVidHeight.TabIndex = 63;
            // 
            // panel10
            // 
            this.panel10.BackgroundImage = global::Flowframes.Properties.Resources.baseline_create_white_18dp_semiTransparent;
            this.panel10.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel10.Location = new System.Drawing.Point(378, 7);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(21, 21);
            this.panel10.TabIndex = 61;
            this.toolTip1.SetToolTip(this.panel10, "Allows custom input.");
            // 
            // label34
            // 
            this.label34.AutoSize = true;
            this.label34.ForeColor = System.Drawing.Color.Silver;
            this.label34.Location = new System.Drawing.Point(412, 11);
            this.label34.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(268, 13);
            this.label34.TabIndex = 64;
            this.label34.Text = "Maximum Height. Video will be downscaled if it\'s bigger.";
            // 
            // panTempFolder
            // 
            this.panTempFolder.Controls.Add(this.label36);
            this.panTempFolder.Controls.Add(this.tempFolderLoc);
            this.panTempFolder.Controls.Add(this.tempDirCustom);
            this.panTempFolder.Controls.Add(this.tempDirBrowseBtn);
            this.panTempFolder.Location = new System.Drawing.Point(0, 60);
            this.panTempFolder.Margin = new System.Windows.Forms.Padding(0);
            this.panTempFolder.Name = "panTempFolder";
            this.panTempFolder.Size = new System.Drawing.Size(762, 30);
            this.panTempFolder.TabIndex = 2;
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Location = new System.Drawing.Point(10, 10);
            this.label36.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(133, 13);
            this.label36.TabIndex = 66;
            this.label36.Text = "Temporary Folder Location";
            // 
            // tempFolderLoc
            // 
            this.tempFolderLoc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tempFolderLoc.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tempFolderLoc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.tempFolderLoc.ForeColor = System.Drawing.Color.White;
            this.tempFolderLoc.FormattingEnabled = true;
            this.tempFolderLoc.Items.AddRange(new object[] {
            "Windows Temp Folder",
            "Same As Input Directory",
            "Same As Output Directory",
            "Flowframes Program Folder",
            "Custom..."});
            this.tempFolderLoc.Location = new System.Drawing.Point(280, 7);
            this.tempFolderLoc.Name = "tempFolderLoc";
            this.tempFolderLoc.Size = new System.Drawing.Size(200, 21);
            this.tempFolderLoc.TabIndex = 65;
            this.tempFolderLoc.SelectedIndexChanged += new System.EventHandler(this.tempFolderLoc_SelectedIndexChanged);
            // 
            // tempDirCustom
            // 
            this.tempDirCustom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tempDirCustom.ForeColor = System.Drawing.Color.White;
            this.tempDirCustom.Location = new System.Drawing.Point(486, 7);
            this.tempDirCustom.MinimumSize = new System.Drawing.Size(4, 21);
            this.tempDirCustom.Name = "tempDirCustom";
            this.tempDirCustom.Size = new System.Drawing.Size(212, 20);
            this.tempDirCustom.TabIndex = 69;
            // 
            // tempDirBrowseBtn
            // 
            this.tempDirBrowseBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tempDirBrowseBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.tempDirBrowseBtn.ForeColor = System.Drawing.Color.White;
            this.tempDirBrowseBtn.Location = new System.Drawing.Point(704, 5);
            this.tempDirBrowseBtn.Name = "tempDirBrowseBtn";
            this.tempDirBrowseBtn.Size = new System.Drawing.Size(55, 23);
            this.tempDirBrowseBtn.TabIndex = 70;
            this.tempDirBrowseBtn.Text = "Browse";
            this.tempDirBrowseBtn.UseVisualStyleBackColor = false;
            this.tempDirBrowseBtn.Click += new System.EventHandler(this.tempDirBrowseBtn_Click);
            // 
            // panOutputLocation
            // 
            this.panOutputLocation.Controls.Add(this.label78);
            this.panOutputLocation.Controls.Add(this.outFolderLoc);
            this.panOutputLocation.Controls.Add(this.custOutDir);
            this.panOutputLocation.Controls.Add(this.custOutDirBrowseBtn);
            this.panOutputLocation.Location = new System.Drawing.Point(0, 90);
            this.panOutputLocation.Margin = new System.Windows.Forms.Padding(0);
            this.panOutputLocation.Name = "panOutputLocation";
            this.panOutputLocation.Size = new System.Drawing.Size(762, 30);
            this.panOutputLocation.TabIndex = 3;
            // 
            // label78
            // 
            this.label78.AutoSize = true;
            this.label78.Location = new System.Drawing.Point(10, 10);
            this.label78.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label78.Name = "label78";
            this.label78.Size = new System.Drawing.Size(120, 13);
            this.label78.TabIndex = 90;
            this.label78.Text = "Default Output Location";
            // 
            // outFolderLoc
            // 
            this.outFolderLoc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.outFolderLoc.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.outFolderLoc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.outFolderLoc.ForeColor = System.Drawing.Color.White;
            this.outFolderLoc.FormattingEnabled = true;
            this.outFolderLoc.Items.AddRange(new object[] {
            "Same As Input Directory",
            "Custom..."});
            this.outFolderLoc.Location = new System.Drawing.Point(280, 7);
            this.outFolderLoc.Name = "outFolderLoc";
            this.outFolderLoc.Size = new System.Drawing.Size(200, 21);
            this.outFolderLoc.TabIndex = 91;
            this.outFolderLoc.SelectedIndexChanged += new System.EventHandler(this.outFolderLoc_SelectedIndexChanged);
            // 
            // custOutDir
            // 
            this.custOutDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.custOutDir.ForeColor = System.Drawing.Color.White;
            this.custOutDir.Location = new System.Drawing.Point(486, 7);
            this.custOutDir.MinimumSize = new System.Drawing.Size(4, 21);
            this.custOutDir.Name = "custOutDir";
            this.custOutDir.Size = new System.Drawing.Size(212, 20);
            this.custOutDir.TabIndex = 92;
            // 
            // custOutDirBrowseBtn
            // 
            this.custOutDirBrowseBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.custOutDirBrowseBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.custOutDirBrowseBtn.ForeColor = System.Drawing.Color.White;
            this.custOutDirBrowseBtn.Location = new System.Drawing.Point(704, 5);
            this.custOutDirBrowseBtn.Name = "custOutDirBrowseBtn";
            this.custOutDirBrowseBtn.Size = new System.Drawing.Size(55, 23);
            this.custOutDirBrowseBtn.TabIndex = 93;
            this.custOutDirBrowseBtn.Text = "Browse";
            this.custOutDirBrowseBtn.UseVisualStyleBackColor = false;
            this.custOutDirBrowseBtn.Click += new System.EventHandler(this.custOutDirBrowseBtn_Click);
            // 
            // panKeepTempFolder
            // 
            this.panKeepTempFolder.Controls.Add(this.label6);
            this.panKeepTempFolder.Controls.Add(this.keepTempFolder);
            this.panKeepTempFolder.Location = new System.Drawing.Point(0, 120);
            this.panKeepTempFolder.Margin = new System.Windows.Forms.Padding(0);
            this.panKeepTempFolder.Name = "panKeepTempFolder";
            this.panKeepTempFolder.Size = new System.Drawing.Size(762, 30);
            this.panKeepTempFolder.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(10, 10);
            this.label6.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(203, 13);
            this.label6.TabIndex = 67;
            this.label6.Text = "Keep Temporary Folder After Interpolation";
            // 
            // keepTempFolder
            // 
            this.keepTempFolder.AutoSize = true;
            this.keepTempFolder.Location = new System.Drawing.Point(280, 10);
            this.keepTempFolder.Name = "keepTempFolder";
            this.keepTempFolder.Size = new System.Drawing.Size(15, 14);
            this.keepTempFolder.TabIndex = 68;
            this.keepTempFolder.UseVisualStyleBackColor = true;
            // 
            // panExportName
            // 
            this.panExportName.Controls.Add(this.label67);
            this.panExportName.Controls.Add(this.exportNamePattern);
            this.panExportName.Controls.Add(this.info1);
            this.panExportName.Controls.Add(this.label68);
            this.panExportName.Controls.Add(this.exportNamePatternLoop);
            this.panExportName.Controls.Add(this.label69);
            this.panExportName.Location = new System.Drawing.Point(0, 150);
            this.panExportName.Margin = new System.Windows.Forms.Padding(0);
            this.panExportName.Name = "panExportName";
            this.panExportName.Size = new System.Drawing.Size(762, 30);
            this.panExportName.TabIndex = 5;
            // 
            // label67
            // 
            this.label67.AutoSize = true;
            this.label67.Location = new System.Drawing.Point(10, 10);
            this.label67.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label67.Name = "label67";
            this.label67.Size = new System.Drawing.Size(105, 13);
            this.label67.TabIndex = 80;
            this.label67.Text = "Export Name Pattern";
            // 
            // exportNamePattern
            // 
            this.exportNamePattern.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.exportNamePattern.ForeColor = System.Drawing.Color.White;
            this.exportNamePattern.Location = new System.Drawing.Point(317, 7);
            this.exportNamePattern.MinimumSize = new System.Drawing.Size(4, 21);
            this.exportNamePattern.Name = "exportNamePattern";
            this.exportNamePattern.Size = new System.Drawing.Size(232, 20);
            this.exportNamePattern.TabIndex = 81;
            // 
            // info1
            // 
            this.info1.BackgroundImage = global::Flowframes.Properties.Resources.questmark_72px_bordeer;
            this.info1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.info1.Location = new System.Drawing.Point(730, 7);
            this.info1.Name = "info1";
            this.info1.Size = new System.Drawing.Size(29, 21);
            this.info1.TabIndex = 82;
            this.info1.TabStop = false;
            this.toolTip1.SetToolTip(this.info1, resources.GetString("info1.ToolTip"));
            // 
            // label68
            // 
            this.label68.AutoSize = true;
            this.label68.Location = new System.Drawing.Point(277, 10);
            this.label68.Margin = new System.Windows.Forms.Padding(10, 10, 3, 7);
            this.label68.Name = "label68";
            this.label68.Size = new System.Drawing.Size(34, 13);
            this.label68.TabIndex = 83;
            this.label68.Text = "Base:";
            // 
            // exportNamePatternLoop
            // 
            this.exportNamePatternLoop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.exportNamePatternLoop.ForeColor = System.Drawing.Color.White;
            this.exportNamePatternLoop.Location = new System.Drawing.Point(624, 7);
            this.exportNamePatternLoop.MinimumSize = new System.Drawing.Size(4, 21);
            this.exportNamePatternLoop.Name = "exportNamePatternLoop";
            this.exportNamePatternLoop.Size = new System.Drawing.Size(100, 20);
            this.exportNamePatternLoop.TabIndex = 85;
            // 
            // label69
            // 
            this.label69.AutoSize = true;
            this.label69.Location = new System.Drawing.Point(555, 10);
            this.label69.Margin = new System.Windows.Forms.Padding(3, 10, 3, 7);
            this.label69.Name = "label69";
            this.label69.Size = new System.Drawing.Size(63, 13);
            this.label69.TabIndex = 84;
            this.label69.Text = "Loop Suffix:";
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.label64);
            this.panel5.Controls.Add(this.clearModelCacheBtn);
            this.panel5.Controls.Add(this.modelDownloaderBtn);
            this.panel5.Location = new System.Drawing.Point(0, 180);
            this.panel5.Margin = new System.Windows.Forms.Padding(0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(762, 30);
            this.panel5.TabIndex = 6;
            // 
            // label64
            // 
            this.label64.AutoSize = true;
            this.label64.Location = new System.Drawing.Point(10, 10);
            this.label64.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label64.Name = "label64";
            this.label64.Size = new System.Drawing.Size(165, 13);
            this.label64.TabIndex = 78;
            this.label64.Text = "Manage Downloaded Model Files";
            // 
            // clearModelCacheBtn
            // 
            this.clearModelCacheBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.clearModelCacheBtn.FlatAppearance.BorderSize = 0;
            this.clearModelCacheBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearModelCacheBtn.ForeColor = System.Drawing.Color.White;
            this.clearModelCacheBtn.Location = new System.Drawing.Point(280, 5);
            this.clearModelCacheBtn.Name = "clearModelCacheBtn";
            this.clearModelCacheBtn.Size = new System.Drawing.Size(206, 23);
            this.clearModelCacheBtn.TabIndex = 79;
            this.clearModelCacheBtn.Text = "Clear Model Cache";
            this.clearModelCacheBtn.UseVisualStyleBackColor = false;
            this.clearModelCacheBtn.Click += new System.EventHandler(this.clearModelCacheBtn_Click);
            // 
            // modelDownloaderBtn
            // 
            this.modelDownloaderBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.modelDownloaderBtn.FlatAppearance.BorderSize = 0;
            this.modelDownloaderBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.modelDownloaderBtn.ForeColor = System.Drawing.Color.White;
            this.modelDownloaderBtn.Location = new System.Drawing.Point(492, 5);
            this.modelDownloaderBtn.Name = "modelDownloaderBtn";
            this.modelDownloaderBtn.Size = new System.Drawing.Size(206, 23);
            this.modelDownloaderBtn.TabIndex = 86;
            this.modelDownloaderBtn.Text = "Open Model Downloader";
            this.modelDownloaderBtn.UseVisualStyleBackColor = false;
            this.modelDownloaderBtn.Click += new System.EventHandler(this.modelDownloaderBtn_Click);
            // 
            // panel11
            // 
            this.panel11.Controls.Add(this.label10);
            this.panel11.Controls.Add(this.btnResetHwEnc);
            this.panel11.Location = new System.Drawing.Point(0, 210);
            this.panel11.Margin = new System.Windows.Forms.Padding(0);
            this.panel11.Name = "panel11";
            this.panel11.Size = new System.Drawing.Size(762, 30);
            this.panel11.TabIndex = 7;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(10, 10);
            this.label10.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(186, 13);
            this.label10.TabIndex = 94;
            this.label10.Text = "Manage Detected Hardware Features";
            // 
            // btnResetHwEnc
            // 
            this.btnResetHwEnc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnResetHwEnc.FlatAppearance.BorderSize = 0;
            this.btnResetHwEnc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetHwEnc.ForeColor = System.Drawing.Color.White;
            this.btnResetHwEnc.Location = new System.Drawing.Point(280, 5);
            this.btnResetHwEnc.Name = "btnResetHwEnc";
            this.btnResetHwEnc.Size = new System.Drawing.Size(206, 23);
            this.btnResetHwEnc.TabIndex = 95;
            this.btnResetHwEnc.Text = "Re-Detected Hardware Encoders";
            this.btnResetHwEnc.UseVisualStyleBackColor = false;
            this.btnResetHwEnc.Click += new System.EventHandler(this.btnResetHwEnc_Click);
            // 
            // tabListPage2
            // 
            this.tabListPage2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.tabListPage2.Controls.Add(this.flowPanelInterpolation);
            this.tabListPage2.ForeColor = System.Drawing.Color.White;
            this.tabListPage2.Name = "tabListPage2";
            this.tabListPage2.Size = new System.Drawing.Size(762, 419);
            this.tabListPage2.Text = "Interpolation";
            // 
            // flowPanelInterpolation
            // 
            this.flowPanelInterpolation.Controls.Add(this.panTitleInputMedia);
            this.flowPanelInterpolation.Controls.Add(this.panCopyInputMedia);
            this.flowPanelInterpolation.Controls.Add(this.panEnableAlpha);
            this.flowPanelInterpolation.Controls.Add(this.panHqJpegImport);
            this.flowPanelInterpolation.Controls.Add(this.panTitleInterpHelpers);
            this.flowPanelInterpolation.Controls.Add(this.panDedupe);
            this.flowPanelInterpolation.Controls.Add(this.panLoop);
            this.flowPanelInterpolation.Controls.Add(this.panSceneChange);
            this.flowPanelInterpolation.Controls.Add(this.panAutoEnc);
            this.flowPanelInterpolation.Controls.Add(this.panAutoEncInSbsMode);
            this.flowPanelInterpolation.Controls.Add(this.panAutoEncBackups);
            this.flowPanelInterpolation.Controls.Add(this.panAutoEncLowSpaceMode);
            this.flowPanelInterpolation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowPanelInterpolation.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowPanelInterpolation.Location = new System.Drawing.Point(0, 0);
            this.flowPanelInterpolation.Margin = new System.Windows.Forms.Padding(0);
            this.flowPanelInterpolation.Name = "flowPanelInterpolation";
            this.flowPanelInterpolation.Size = new System.Drawing.Size(762, 419);
            this.flowPanelInterpolation.TabIndex = 94;
            // 
            // panTitleInputMedia
            // 
            this.panTitleInputMedia.Controls.Add(this.label24);
            this.panTitleInputMedia.Location = new System.Drawing.Point(0, 0);
            this.panTitleInputMedia.Margin = new System.Windows.Forms.Padding(0);
            this.panTitleInputMedia.Name = "panTitleInputMedia";
            this.panTitleInputMedia.Size = new System.Drawing.Size(762, 30);
            this.panTitleInputMedia.TabIndex = 7;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label24.Location = new System.Drawing.Point(10, 10);
            this.label24.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(87, 16);
            this.label24.TabIndex = 44;
            this.label24.Text = "Input Media";
            // 
            // panCopyInputMedia
            // 
            this.panCopyInputMedia.Controls.Add(this.label1);
            this.panCopyInputMedia.Controls.Add(this.keepAudio);
            this.panCopyInputMedia.Controls.Add(this.keepSubs);
            this.panCopyInputMedia.Controls.Add(this.keepMeta);
            this.panCopyInputMedia.Location = new System.Drawing.Point(0, 30);
            this.panCopyInputMedia.Margin = new System.Windows.Forms.Padding(0);
            this.panCopyInputMedia.Name = "panCopyInputMedia";
            this.panCopyInputMedia.Size = new System.Drawing.Size(762, 30);
            this.panCopyInputMedia.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(124, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "Input Media To Preserve";
            // 
            // keepAudio
            // 
            this.keepAudio.AutoSize = true;
            this.keepAudio.Location = new System.Drawing.Point(280, 9);
            this.keepAudio.Name = "keepAudio";
            this.keepAudio.Size = new System.Drawing.Size(81, 17);
            this.keepAudio.TabIndex = 25;
            this.keepAudio.Text = "Keep Audio";
            this.keepAudio.UseVisualStyleBackColor = true;
            // 
            // keepSubs
            // 
            this.keepSubs.AutoSize = true;
            this.keepSubs.Location = new System.Drawing.Point(367, 9);
            this.keepSubs.Name = "keepSubs";
            this.keepSubs.Size = new System.Drawing.Size(94, 17);
            this.keepSubs.TabIndex = 75;
            this.keepSubs.Text = "Keep Subtitles";
            this.keepSubs.UseVisualStyleBackColor = true;
            // 
            // keepMeta
            // 
            this.keepMeta.AutoSize = true;
            this.keepMeta.Location = new System.Drawing.Point(467, 9);
            this.keepMeta.Name = "keepMeta";
            this.keepMeta.Size = new System.Drawing.Size(99, 17);
            this.keepMeta.TabIndex = 81;
            this.keepMeta.Text = "Keep Metadata";
            this.keepMeta.UseVisualStyleBackColor = true;
            // 
            // panEnableAlpha
            // 
            this.panEnableAlpha.Controls.Add(this.label25);
            this.panEnableAlpha.Controls.Add(this.enableAlpha);
            this.panEnableAlpha.Controls.Add(this.label4);
            this.panEnableAlpha.Location = new System.Drawing.Point(0, 60);
            this.panEnableAlpha.Margin = new System.Windows.Forms.Padding(0);
            this.panEnableAlpha.Name = "panEnableAlpha";
            this.panEnableAlpha.Size = new System.Drawing.Size(762, 30);
            this.panEnableAlpha.TabIndex = 6;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(10, 10);
            this.label25.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(209, 13);
            this.label25.TabIndex = 76;
            this.label25.Text = "Enable Alpha/Transparency (Experimental)";
            // 
            // enableAlpha
            // 
            this.enableAlpha.AutoSize = true;
            this.enableAlpha.Location = new System.Drawing.Point(280, 10);
            this.enableAlpha.Name = "enableAlpha";
            this.enableAlpha.Size = new System.Drawing.Size(15, 14);
            this.enableAlpha.TabIndex = 77;
            this.enableAlpha.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.Silver;
            this.label4.Location = new System.Drawing.Point(308, 10);
            this.label4.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(354, 13);
            this.label4.TabIndex = 78;
            this.label4.Text = "Enables transparency (alpha channel) interpolation with supported formats";
            // 
            // panHqJpegImport
            // 
            this.panHqJpegImport.Controls.Add(this.label63);
            this.panHqJpegImport.Controls.Add(this.jpegFrames);
            this.panHqJpegImport.Controls.Add(this.label74);
            this.panHqJpegImport.Location = new System.Drawing.Point(0, 90);
            this.panHqJpegImport.Margin = new System.Windows.Forms.Padding(0);
            this.panHqJpegImport.Name = "panHqJpegImport";
            this.panHqJpegImport.Size = new System.Drawing.Size(762, 30);
            this.panHqJpegImport.TabIndex = 8;
            // 
            // label63
            // 
            this.label63.AutoSize = true;
            this.label63.Location = new System.Drawing.Point(10, 10);
            this.label63.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label63.Name = "label63";
            this.label63.Size = new System.Drawing.Size(170, 13);
            this.label63.TabIndex = 84;
            this.label63.Text = "Import HQ JPEGs instead of PNGs";
            // 
            // jpegFrames
            // 
            this.jpegFrames.AutoSize = true;
            this.jpegFrames.Location = new System.Drawing.Point(280, 10);
            this.jpegFrames.Name = "jpegFrames";
            this.jpegFrames.Size = new System.Drawing.Size(15, 14);
            this.jpegFrames.TabIndex = 85;
            this.jpegFrames.UseVisualStyleBackColor = true;
            // 
            // label74
            // 
            this.label74.AutoSize = true;
            this.label74.ForeColor = System.Drawing.Color.Silver;
            this.label74.Location = new System.Drawing.Point(308, 10);
            this.label74.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label74.Name = "label74";
            this.label74.Size = new System.Drawing.Size(433, 13);
            this.label74.TabIndex = 86;
            this.label74.Text = "Makes frame extraction faster and take up less disk space with no visible quality" +
    " reduction.";
            // 
            // panTitleInterpHelpers
            // 
            this.panTitleInterpHelpers.Controls.Add(this.label18);
            this.panTitleInterpHelpers.Location = new System.Drawing.Point(0, 120);
            this.panTitleInterpHelpers.Margin = new System.Windows.Forms.Padding(0);
            this.panTitleInterpHelpers.Name = "panTitleInterpHelpers";
            this.panTitleInterpHelpers.Size = new System.Drawing.Size(762, 30);
            this.panTitleInterpHelpers.TabIndex = 9;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(10, 10);
            this.label18.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(152, 16);
            this.label18.TabIndex = 82;
            this.label18.Text = "Interpolation Helpers";
            // 
            // panDedupe
            // 
            this.panDedupe.Controls.Add(this.label2);
            this.panDedupe.Controls.Add(this.dedupMode);
            this.panDedupe.Controls.Add(this.dedupeSensLabel);
            this.panDedupe.Controls.Add(this.mpDedupePanel);
            this.panDedupe.Controls.Add(this.magickDedupePanel);
            this.panDedupe.Location = new System.Drawing.Point(0, 150);
            this.panDedupe.Margin = new System.Windows.Forms.Padding(0);
            this.panDedupe.Name = "panDedupe";
            this.panDedupe.Size = new System.Drawing.Size(762, 30);
            this.panDedupe.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 10);
            this.label2.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(139, 13);
            this.label2.TabIndex = 26;
            this.label2.Text = "Frame De-Duplication Mode";
            // 
            // dedupMode
            // 
            this.dedupMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.dedupMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dedupMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dedupMode.ForeColor = System.Drawing.Color.White;
            this.dedupMode.FormattingEnabled = true;
            this.dedupMode.Items.AddRange(new object[] {
            "Disabled",
            "Enabled (mpdecimate)"});
            this.dedupMode.Location = new System.Drawing.Point(280, 7);
            this.dedupMode.Name = "dedupMode";
            this.dedupMode.Size = new System.Drawing.Size(250, 21);
            this.dedupMode.TabIndex = 27;
            this.dedupMode.SelectedIndexChanged += new System.EventHandler(this.dedupMode_SelectedIndexChanged);
            // 
            // dedupeSensLabel
            // 
            this.dedupeSensLabel.AutoSize = true;
            this.dedupeSensLabel.Location = new System.Drawing.Point(536, 11);
            this.dedupeSensLabel.Margin = new System.Windows.Forms.Padding(3);
            this.dedupeSensLabel.Name = "dedupeSensLabel";
            this.dedupeSensLabel.Size = new System.Drawing.Size(57, 13);
            this.dedupeSensLabel.TabIndex = 29;
            this.dedupeSensLabel.Text = "Sensitivity:";
            // 
            // mpDedupePanel
            // 
            this.mpDedupePanel.Controls.Add(this.mpdecimateMode);
            this.mpDedupePanel.Location = new System.Drawing.Point(599, 7);
            this.mpDedupePanel.Margin = new System.Windows.Forms.Padding(0);
            this.mpDedupePanel.Name = "mpDedupePanel";
            this.mpDedupePanel.Size = new System.Drawing.Size(135, 21);
            this.mpDedupePanel.TabIndex = 61;
            // 
            // mpdecimateMode
            // 
            this.mpdecimateMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.mpdecimateMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.mpdecimateMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.mpdecimateMode.ForeColor = System.Drawing.Color.White;
            this.mpdecimateMode.FormattingEnabled = true;
            this.mpdecimateMode.Items.AddRange(new object[] {
            "Normal",
            "Aggressive"});
            this.mpdecimateMode.Location = new System.Drawing.Point(0, 0);
            this.mpdecimateMode.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this.mpdecimateMode.Name = "mpdecimateMode";
            this.mpdecimateMode.Size = new System.Drawing.Size(135, 21);
            this.mpdecimateMode.TabIndex = 28;
            // 
            // magickDedupePanel
            // 
            this.magickDedupePanel.Controls.Add(this.dedupThresh);
            this.magickDedupePanel.Controls.Add(this.panel3);
            this.magickDedupePanel.Location = new System.Drawing.Point(599, 7);
            this.magickDedupePanel.Margin = new System.Windows.Forms.Padding(0);
            this.magickDedupePanel.Name = "magickDedupePanel";
            this.magickDedupePanel.Size = new System.Drawing.Size(135, 21);
            this.magickDedupePanel.TabIndex = 60;
            // 
            // dedupThresh
            // 
            this.dedupThresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.dedupThresh.DecimalPlaces = 1;
            this.dedupThresh.ForeColor = System.Drawing.Color.White;
            this.dedupThresh.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.dedupThresh.Location = new System.Drawing.Point(0, 1);
            this.dedupThresh.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.dedupThresh.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.dedupThresh.Name = "dedupThresh";
            this.dedupThresh.Size = new System.Drawing.Size(103, 20);
            this.dedupThresh.TabIndex = 75;
            this.dedupThresh.Tag = "";
            this.dedupThresh.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // panel3
            // 
            this.panel3.BackgroundImage = global::Flowframes.Properties.Resources.baseline_create_white_18dp_semiTransparent;
            this.panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel3.Location = new System.Drawing.Point(114, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(21, 21);
            this.panel3.TabIndex = 57;
            this.toolTip1.SetToolTip(this.panel3, "Allows custom input.");
            // 
            // panLoop
            // 
            this.panLoop.Controls.Add(this.label15);
            this.panLoop.Controls.Add(this.enableLoop);
            this.panLoop.Location = new System.Drawing.Point(0, 180);
            this.panLoop.Margin = new System.Windows.Forms.Padding(0);
            this.panLoop.Name = "panLoop";
            this.panLoop.Size = new System.Drawing.Size(762, 30);
            this.panLoop.TabIndex = 11;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(10, 10);
            this.label15.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(217, 13);
            this.label15.TabIndex = 30;
            this.label15.Text = "Loop Interpolation (Copy First Frame To End)";
            // 
            // enableLoop
            // 
            this.enableLoop.AutoSize = true;
            this.enableLoop.Location = new System.Drawing.Point(280, 10);
            this.enableLoop.Name = "enableLoop";
            this.enableLoop.Size = new System.Drawing.Size(15, 14);
            this.enableLoop.TabIndex = 31;
            this.enableLoop.UseVisualStyleBackColor = true;
            // 
            // panSceneChange
            // 
            this.panSceneChange.Controls.Add(this.label50);
            this.panSceneChange.Controls.Add(this.scnDetect);
            this.panSceneChange.Controls.Add(this.label51);
            this.panSceneChange.Controls.Add(this.label52);
            this.panSceneChange.Controls.Add(this.panel14);
            this.panSceneChange.Controls.Add(this.scnDetectValue);
            this.panSceneChange.Location = new System.Drawing.Point(0, 210);
            this.panSceneChange.Margin = new System.Windows.Forms.Padding(0);
            this.panSceneChange.Name = "panSceneChange";
            this.panSceneChange.Size = new System.Drawing.Size(762, 30);
            this.panSceneChange.TabIndex = 12;
            // 
            // label50
            // 
            this.label50.AutoSize = true;
            this.label50.Location = new System.Drawing.Point(10, 10);
            this.label50.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label50.Name = "label50";
            this.label50.Size = new System.Drawing.Size(129, 13);
            this.label50.TabIndex = 63;
            this.label50.Text = "Fix Scene Changes (Cuts)";
            // 
            // scnDetect
            // 
            this.scnDetect.AutoSize = true;
            this.scnDetect.Location = new System.Drawing.Point(280, 10);
            this.scnDetect.Name = "scnDetect";
            this.scnDetect.Size = new System.Drawing.Size(15, 14);
            this.scnDetect.TabIndex = 64;
            this.scnDetect.UseVisualStyleBackColor = true;
            // 
            // label51
            // 
            this.label51.AutoSize = true;
            this.label51.Location = new System.Drawing.Point(301, 11);
            this.label51.Margin = new System.Windows.Forms.Padding(3);
            this.label51.Name = "label51";
            this.label51.Size = new System.Drawing.Size(57, 13);
            this.label51.TabIndex = 66;
            this.label51.Text = "Sensitivity:";
            // 
            // label52
            // 
            this.label52.AutoSize = true;
            this.label52.ForeColor = System.Drawing.Color.Silver;
            this.label52.Location = new System.Drawing.Point(509, 12);
            this.label52.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label52.Name = "label52";
            this.label52.Size = new System.Drawing.Size(225, 13);
            this.label52.TabIndex = 67;
            this.label52.Text = "Lower values will detect more scene changes.";
            // 
            // panel14
            // 
            this.panel14.BackgroundImage = global::Flowframes.Properties.Resources.baseline_create_white_18dp_semiTransparent;
            this.panel14.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel14.Location = new System.Drawing.Point(475, 7);
            this.panel14.Name = "panel14";
            this.panel14.Size = new System.Drawing.Size(21, 21);
            this.panel14.TabIndex = 68;
            this.toolTip1.SetToolTip(this.panel14, "Allows custom input.");
            // 
            // scnDetectValue
            // 
            this.scnDetectValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.scnDetectValue.DecimalPlaces = 2;
            this.scnDetectValue.ForeColor = System.Drawing.Color.White;
            this.scnDetectValue.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.scnDetectValue.Location = new System.Drawing.Point(364, 8);
            this.scnDetectValue.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.scnDetectValue.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.scnDetectValue.Name = "scnDetectValue";
            this.scnDetectValue.Size = new System.Drawing.Size(100, 20);
            this.scnDetectValue.TabIndex = 74;
            this.scnDetectValue.Value = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            // 
            // panAutoEnc
            // 
            this.panAutoEnc.Controls.Add(this.label49);
            this.panAutoEnc.Controls.Add(this.autoEncMode);
            this.panAutoEnc.Location = new System.Drawing.Point(0, 240);
            this.panAutoEnc.Margin = new System.Windows.Forms.Padding(0);
            this.panAutoEnc.Name = "panAutoEnc";
            this.panAutoEnc.Size = new System.Drawing.Size(762, 30);
            this.panAutoEnc.TabIndex = 13;
            // 
            // label49
            // 
            this.label49.AutoSize = true;
            this.label49.Location = new System.Drawing.Point(10, 10);
            this.label49.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label49.Name = "label49";
            this.label49.Size = new System.Drawing.Size(206, 13);
            this.label49.TabIndex = 69;
            this.label49.Text = "Auto-Encode (Encode While Interpolating)";
            // 
            // autoEncMode
            // 
            this.autoEncMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.autoEncMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.autoEncMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.autoEncMode.ForeColor = System.Drawing.Color.White;
            this.autoEncMode.FormattingEnabled = true;
            this.autoEncMode.Items.AddRange(new object[] {
            "Disabled",
            "Enabled (Keep Interpolated Frames)",
            "Enabled (Delete Frames Once Encoded)"});
            this.autoEncMode.Location = new System.Drawing.Point(280, 7);
            this.autoEncMode.Name = "autoEncMode";
            this.autoEncMode.Size = new System.Drawing.Size(250, 21);
            this.autoEncMode.TabIndex = 70;
            this.autoEncMode.SelectedIndexChanged += new System.EventHandler(this.autoEncMode_SelectedIndexChanged);
            // 
            // panAutoEncInSbsMode
            // 
            this.panAutoEncInSbsMode.Controls.Add(this.label53);
            this.panAutoEncInSbsMode.Controls.Add(this.sbsAllowAutoEnc);
            this.panAutoEncInSbsMode.Location = new System.Drawing.Point(0, 270);
            this.panAutoEncInSbsMode.Margin = new System.Windows.Forms.Padding(0);
            this.panAutoEncInSbsMode.Name = "panAutoEncInSbsMode";
            this.panAutoEncInSbsMode.Size = new System.Drawing.Size(762, 30);
            this.panAutoEncInSbsMode.TabIndex = 14;
            // 
            // label53
            // 
            this.label53.AutoSize = true;
            this.label53.Location = new System.Drawing.Point(10, 10);
            this.label53.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label53.Name = "label53";
            this.label53.Size = new System.Drawing.Size(203, 13);
            this.label53.TabIndex = 71;
            this.label53.Text = "Allow Auto-Encode in Step-By-Step Mode";
            // 
            // sbsAllowAutoEnc
            // 
            this.sbsAllowAutoEnc.AutoSize = true;
            this.sbsAllowAutoEnc.Location = new System.Drawing.Point(280, 10);
            this.sbsAllowAutoEnc.Name = "sbsAllowAutoEnc";
            this.sbsAllowAutoEnc.Size = new System.Drawing.Size(15, 14);
            this.sbsAllowAutoEnc.TabIndex = 72;
            this.sbsAllowAutoEnc.UseVisualStyleBackColor = true;
            // 
            // panAutoEncBackups
            // 
            this.panAutoEncBackups.Controls.Add(this.label16);
            this.panAutoEncBackups.Controls.Add(this.autoEncBackupMode);
            this.panAutoEncBackups.Controls.Add(this.label41);
            this.panAutoEncBackups.Controls.Add(this.pictureBox2);
            this.panAutoEncBackups.Location = new System.Drawing.Point(0, 300);
            this.panAutoEncBackups.Margin = new System.Windows.Forms.Padding(0);
            this.panAutoEncBackups.Name = "panAutoEncBackups";
            this.panAutoEncBackups.Size = new System.Drawing.Size(762, 30);
            this.panAutoEncBackups.TabIndex = 15;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(10, 10);
            this.label16.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(114, 13);
            this.label16.TabIndex = 87;
            this.label16.Text = "Auto-Encode Backups";
            // 
            // autoEncBackupMode
            // 
            this.autoEncBackupMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.autoEncBackupMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.autoEncBackupMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.autoEncBackupMode.ForeColor = System.Drawing.Color.White;
            this.autoEncBackupMode.FormattingEnabled = true;
            this.autoEncBackupMode.Items.AddRange(new object[] {
            "Disabled",
            "Enabled (Only Video)",
            "Enabled (Complete - With Audio, Subtitles)"});
            this.autoEncBackupMode.Location = new System.Drawing.Point(280, 7);
            this.autoEncBackupMode.Name = "autoEncBackupMode";
            this.autoEncBackupMode.Size = new System.Drawing.Size(250, 21);
            this.autoEncBackupMode.TabIndex = 88;
            // 
            // label41
            // 
            this.label41.AutoSize = true;
            this.label41.ForeColor = System.Drawing.Color.Silver;
            this.label41.Location = new System.Drawing.Point(578, 10);
            this.label41.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label41.Name = "label41";
            this.label41.Size = new System.Drawing.Size(158, 13);
            this.label41.TabIndex = 89;
            this.label41.Text = "Can cause slowdown on HDDs!";
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImage = global::Flowframes.Properties.Resources.questmark_72px_bordeer;
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox2.Location = new System.Drawing.Point(536, 7);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(29, 21);
            this.pictureBox2.TabIndex = 90;
            this.pictureBox2.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox2, resources.GetString("pictureBox2.ToolTip"));
            // 
            // panAutoEncLowSpaceMode
            // 
            this.panAutoEncLowSpaceMode.Controls.Add(this.label58);
            this.panAutoEncLowSpaceMode.Controls.Add(this.label70);
            this.panAutoEncLowSpaceMode.Controls.Add(this.alwaysWaitForAutoEnc);
            this.panAutoEncLowSpaceMode.Location = new System.Drawing.Point(0, 330);
            this.panAutoEncLowSpaceMode.Margin = new System.Windows.Forms.Padding(0);
            this.panAutoEncLowSpaceMode.Name = "panAutoEncLowSpaceMode";
            this.panAutoEncLowSpaceMode.Size = new System.Drawing.Size(762, 30);
            this.panAutoEncLowSpaceMode.TabIndex = 16;
            // 
            // label58
            // 
            this.label58.AutoSize = true;
            this.label58.Location = new System.Drawing.Point(10, 10);
            this.label58.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label58.Name = "label58";
            this.label58.Size = new System.Drawing.Size(229, 13);
            this.label58.TabIndex = 91;
            this.label58.Text = "Low Disk Space Mode (Wait For Auto-Encode)";
            // 
            // label70
            // 
            this.label70.AutoSize = true;
            this.label70.ForeColor = System.Drawing.Color.Silver;
            this.label70.Location = new System.Drawing.Point(308, 10);
            this.label70.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label70.Name = "label70";
            this.label70.Size = new System.Drawing.Size(443, 13);
            this.label70.TabIndex = 93;
            this.label70.Text = "Avoids filling up your hard drive by temporarily pausing interpolation if encodin" +
    "g can\'t keep up";
            // 
            // alwaysWaitForAutoEnc
            // 
            this.alwaysWaitForAutoEnc.AutoSize = true;
            this.alwaysWaitForAutoEnc.Location = new System.Drawing.Point(280, 10);
            this.alwaysWaitForAutoEnc.Name = "alwaysWaitForAutoEnc";
            this.alwaysWaitForAutoEnc.Size = new System.Drawing.Size(15, 14);
            this.alwaysWaitForAutoEnc.TabIndex = 92;
            this.alwaysWaitForAutoEnc.UseVisualStyleBackColor = true;
            // 
            // aiOptsPage
            // 
            this.aiOptsPage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.aiOptsPage.Controls.Add(this.flowPanelAiOptions);
            this.aiOptsPage.ForeColor = System.Drawing.Color.White;
            this.aiOptsPage.Name = "aiOptsPage";
            this.aiOptsPage.Size = new System.Drawing.Size(762, 419);
            this.aiOptsPage.Text = "AI Specific Options";
            // 
            // flowPanelAiOptions
            // 
            this.flowPanelAiOptions.Controls.Add(this.panTitleAiFramework);
            this.flowPanelAiOptions.Controls.Add(this.panTorchGpus);
            this.flowPanelAiOptions.Controls.Add(this.panNcnnGpus);
            this.flowPanelAiOptions.Controls.Add(this.panNcnnThreads);
            this.flowPanelAiOptions.Controls.Add(this.panTitleRife);
            this.flowPanelAiOptions.Controls.Add(this.panUhdThresh);
            this.flowPanelAiOptions.Controls.Add(this.panRifeCudaHalfPrec);
            this.flowPanelAiOptions.Controls.Add(this.panTitleDainNcnn);
            this.flowPanelAiOptions.Controls.Add(this.panDainNcnnTileSize);
            this.flowPanelAiOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowPanelAiOptions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowPanelAiOptions.Location = new System.Drawing.Point(0, 0);
            this.flowPanelAiOptions.Margin = new System.Windows.Forms.Padding(0);
            this.flowPanelAiOptions.Name = "flowPanelAiOptions";
            this.flowPanelAiOptions.Size = new System.Drawing.Size(762, 419);
            this.flowPanelAiOptions.TabIndex = 95;
            // 
            // panTitleAiFramework
            // 
            this.panTitleAiFramework.Controls.Add(this.label32);
            this.panTitleAiFramework.Location = new System.Drawing.Point(0, 0);
            this.panTitleAiFramework.Margin = new System.Windows.Forms.Padding(0);
            this.panTitleAiFramework.Name = "panTitleAiFramework";
            this.panTitleAiFramework.Size = new System.Drawing.Size(762, 30);
            this.panTitleAiFramework.TabIndex = 7;
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.label32.Location = new System.Drawing.Point(10, 10);
            this.label32.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(161, 16);
            this.label32.TabIndex = 51;
            this.label32.Text = "AI Framework Settings";
            // 
            // panTorchGpus
            // 
            this.panTorchGpus.Controls.Add(this.tooltipTorchGpu);
            this.panTorchGpus.Controls.Add(this.label33);
            this.panTorchGpus.Controls.Add(this.torchGpus);
            this.panTorchGpus.Location = new System.Drawing.Point(0, 30);
            this.panTorchGpus.Margin = new System.Windows.Forms.Padding(0);
            this.panTorchGpus.Name = "panTorchGpus";
            this.panTorchGpus.Size = new System.Drawing.Size(762, 30);
            this.panTorchGpus.TabIndex = 5;
            // 
            // tooltipTorchGpu
            // 
            this.tooltipTorchGpu.BackgroundImage = global::Flowframes.Properties.Resources.questmark_72px_bordeer;
            this.tooltipTorchGpu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.tooltipTorchGpu.Location = new System.Drawing.Point(536, 7);
            this.tooltipTorchGpu.Name = "tooltipTorchGpu";
            this.tooltipTorchGpu.Size = new System.Drawing.Size(29, 21);
            this.tooltipTorchGpu.TabIndex = 91;
            this.tooltipTorchGpu.TabStop = false;
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(10, 10);
            this.label33.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(83, 13);
            this.label33.TabIndex = 54;
            this.label33.Text = "Pytorch GPU ID";
            // 
            // torchGpus
            // 
            this.torchGpus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.torchGpus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.torchGpus.ForeColor = System.Drawing.Color.White;
            this.torchGpus.FormattingEnabled = true;
            this.torchGpus.Location = new System.Drawing.Point(280, 7);
            this.torchGpus.Name = "torchGpus";
            this.torchGpus.Size = new System.Drawing.Size(250, 21);
            this.torchGpus.TabIndex = 55;
            // 
            // panNcnnGpus
            // 
            this.panNcnnGpus.Controls.Add(this.tooltipNcnnGpu);
            this.panNcnnGpus.Controls.Add(this.label5);
            this.panNcnnGpus.Controls.Add(this.ncnnGpus);
            this.panNcnnGpus.Location = new System.Drawing.Point(0, 60);
            this.panNcnnGpus.Margin = new System.Windows.Forms.Padding(0);
            this.panNcnnGpus.Name = "panNcnnGpus";
            this.panNcnnGpus.Size = new System.Drawing.Size(762, 30);
            this.panNcnnGpus.TabIndex = 6;
            // 
            // tooltipNcnnGpu
            // 
            this.tooltipNcnnGpu.BackgroundImage = global::Flowframes.Properties.Resources.questmark_72px_bordeer;
            this.tooltipNcnnGpu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.tooltipNcnnGpu.Location = new System.Drawing.Point(536, 7);
            this.tooltipNcnnGpu.Name = "tooltipNcnnGpu";
            this.tooltipNcnnGpu.Size = new System.Drawing.Size(29, 21);
            this.tooltipNcnnGpu.TabIndex = 92;
            this.tooltipNcnnGpu.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(10, 10);
            this.label5.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 13);
            this.label5.TabIndex = 52;
            this.label5.Text = "NCNN GPU ID";
            // 
            // ncnnGpus
            // 
            this.ncnnGpus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ncnnGpus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ncnnGpus.ForeColor = System.Drawing.Color.White;
            this.ncnnGpus.FormattingEnabled = true;
            this.ncnnGpus.Location = new System.Drawing.Point(280, 7);
            this.ncnnGpus.Name = "ncnnGpus";
            this.ncnnGpus.Size = new System.Drawing.Size(250, 21);
            this.ncnnGpus.TabIndex = 53;
            // 
            // panNcnnThreads
            // 
            this.panNcnnThreads.Controls.Add(this.label43);
            this.panNcnnThreads.Controls.Add(this.label44);
            this.panNcnnThreads.Controls.Add(this.ncnnThreads);
            this.panNcnnThreads.Location = new System.Drawing.Point(0, 90);
            this.panNcnnThreads.Margin = new System.Windows.Forms.Padding(0);
            this.panNcnnThreads.Name = "panNcnnThreads";
            this.panNcnnThreads.Size = new System.Drawing.Size(762, 30);
            this.panNcnnThreads.TabIndex = 8;
            // 
            // label43
            // 
            this.label43.AutoSize = true;
            this.label43.Location = new System.Drawing.Point(10, 10);
            this.label43.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label43.Name = "label43";
            this.label43.Size = new System.Drawing.Size(161, 13);
            this.label43.TabIndex = 58;
            this.label43.Text = "NCNN GPU Processing Threads";
            // 
            // label44
            // 
            this.label44.AutoSize = true;
            this.label44.ForeColor = System.Drawing.Color.Silver;
            this.label44.Location = new System.Drawing.Point(370, 11);
            this.label44.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label44.Name = "label44";
            this.label44.Size = new System.Drawing.Size(330, 13);
            this.label44.TabIndex = 60;
            this.label44.Text = "Higher will cause more GPU/VRAM load but can be faster. 0 = Auto.";
            // 
            // ncnnThreads
            // 
            this.ncnnThreads.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ncnnThreads.ForeColor = System.Drawing.Color.White;
            this.ncnnThreads.Location = new System.Drawing.Point(280, 8);
            this.ncnnThreads.Maximum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.ncnnThreads.Name = "ncnnThreads";
            this.ncnnThreads.Size = new System.Drawing.Size(77, 20);
            this.ncnnThreads.TabIndex = 75;
            this.ncnnThreads.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // panTitleRife
            // 
            this.panTitleRife.Controls.Add(this.label11);
            this.panTitleRife.Location = new System.Drawing.Point(0, 120);
            this.panTitleRife.Margin = new System.Windows.Forms.Padding(0);
            this.panTitleRife.Name = "panTitleRife";
            this.panTitleRife.Size = new System.Drawing.Size(762, 30);
            this.panTitleRife.TabIndex = 9;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.label11.Location = new System.Drawing.Point(10, 10);
            this.label11.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(101, 16);
            this.label11.TabIndex = 61;
            this.label11.Text = "RIFE Settings";
            // 
            // panUhdThresh
            // 
            this.panUhdThresh.Controls.Add(this.label29);
            this.panUhdThresh.Controls.Add(this.uhdThresh);
            this.panUhdThresh.Controls.Add(this.panel6);
            this.panUhdThresh.Controls.Add(this.label30);
            this.panUhdThresh.Location = new System.Drawing.Point(0, 150);
            this.panUhdThresh.Margin = new System.Windows.Forms.Padding(0);
            this.panUhdThresh.Name = "panUhdThresh";
            this.panUhdThresh.Size = new System.Drawing.Size(762, 30);
            this.panUhdThresh.TabIndex = 10;
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(10, 10);
            this.label29.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(151, 13);
            this.label29.TabIndex = 62;
            this.label29.Text = "UHD Mode Threshold (Height)";
            // 
            // uhdThresh
            // 
            this.uhdThresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.uhdThresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.uhdThresh.ForeColor = System.Drawing.Color.White;
            this.uhdThresh.FormattingEnabled = true;
            this.uhdThresh.Items.AddRange(new object[] {
            "4320",
            "2160",
            "1440",
            "1080",
            "720"});
            this.uhdThresh.Location = new System.Drawing.Point(280, 7);
            this.uhdThresh.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this.uhdThresh.Name = "uhdThresh";
            this.uhdThresh.Size = new System.Drawing.Size(87, 21);
            this.uhdThresh.TabIndex = 66;
            // 
            // panel6
            // 
            this.panel6.BackgroundImage = global::Flowframes.Properties.Resources.baseline_create_white_18dp_semiTransparent;
            this.panel6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel6.Location = new System.Drawing.Point(378, 7);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(21, 21);
            this.panel6.TabIndex = 65;
            this.toolTip1.SetToolTip(this.panel6, "Allows custom input.");
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.ForeColor = System.Drawing.Color.Silver;
            this.label30.Location = new System.Drawing.Point(412, 11);
            this.label30.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(335, 13);
            this.label30.TabIndex = 67;
            this.label30.Text = "Minimum height to enable UHD mode to improve high-res interpolation";
            // 
            // panRifeCudaHalfPrec
            // 
            this.panRifeCudaHalfPrec.Controls.Add(this.label65);
            this.panRifeCudaHalfPrec.Controls.Add(this.label66);
            this.panRifeCudaHalfPrec.Controls.Add(this.rifeCudaFp16);
            this.panRifeCudaHalfPrec.Location = new System.Drawing.Point(0, 180);
            this.panRifeCudaHalfPrec.Margin = new System.Windows.Forms.Padding(0);
            this.panRifeCudaHalfPrec.Name = "panRifeCudaHalfPrec";
            this.panRifeCudaHalfPrec.Size = new System.Drawing.Size(762, 30);
            this.panRifeCudaHalfPrec.TabIndex = 11;
            // 
            // label65
            // 
            this.label65.AutoSize = true;
            this.label65.Location = new System.Drawing.Point(10, 10);
            this.label65.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label65.Name = "label65";
            this.label65.Size = new System.Drawing.Size(165, 13);
            this.label65.TabIndex = 81;
            this.label65.Text = "Half-Precision Mode (CUDA Only)";
            // 
            // label66
            // 
            this.label66.AutoSize = true;
            this.label66.ForeColor = System.Drawing.Color.Silver;
            this.label66.Location = new System.Drawing.Point(307, 11);
            this.label66.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label66.Name = "label66";
            this.label66.Size = new System.Drawing.Size(448, 13);
            this.label66.TabIndex = 83;
            this.label66.Text = "Faster, uses less VRAM, but can cause glitches and flickering, especially on RIFE" +
    " 3.x models.";
            // 
            // rifeCudaFp16
            // 
            this.rifeCudaFp16.AutoSize = true;
            this.rifeCudaFp16.Location = new System.Drawing.Point(279, 10);
            this.rifeCudaFp16.Name = "rifeCudaFp16";
            this.rifeCudaFp16.Size = new System.Drawing.Size(15, 14);
            this.rifeCudaFp16.TabIndex = 82;
            this.rifeCudaFp16.UseVisualStyleBackColor = true;
            // 
            // panTitleDainNcnn
            // 
            this.panTitleDainNcnn.Controls.Add(this.label19);
            this.panTitleDainNcnn.Location = new System.Drawing.Point(0, 210);
            this.panTitleDainNcnn.Margin = new System.Windows.Forms.Padding(0);
            this.panTitleDainNcnn.Name = "panTitleDainNcnn";
            this.panTitleDainNcnn.Size = new System.Drawing.Size(762, 30);
            this.panTitleDainNcnn.TabIndex = 12;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.label19.Location = new System.Drawing.Point(10, 10);
            this.label19.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(151, 16);
            this.label19.TabIndex = 76;
            this.label19.Text = "DAIN-NCNN Settings";
            // 
            // panDainNcnnTileSize
            // 
            this.panDainNcnnTileSize.Controls.Add(this.label27);
            this.panDainNcnnTileSize.Controls.Add(this.label35);
            this.panDainNcnnTileSize.Controls.Add(this.dainNcnnTilesize);
            this.panDainNcnnTileSize.Controls.Add(this.panel12);
            this.panDainNcnnTileSize.Location = new System.Drawing.Point(0, 240);
            this.panDainNcnnTileSize.Margin = new System.Windows.Forms.Padding(0);
            this.panDainNcnnTileSize.Name = "panDainNcnnTileSize";
            this.panDainNcnnTileSize.Size = new System.Drawing.Size(762, 30);
            this.panDainNcnnTileSize.TabIndex = 13;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(10, 10);
            this.label27.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(93, 13);
            this.label27.TabIndex = 77;
            this.label27.Text = "Tile Size (Splitting)";
            // 
            // label35
            // 
            this.label35.AutoSize = true;
            this.label35.ForeColor = System.Drawing.Color.Silver;
            this.label35.Location = new System.Drawing.Point(412, 11);
            this.label35.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(294, 13);
            this.label35.TabIndex = 80;
            this.label35.Text = "Lower values decrease VRAM usage but also reduce speed.";
            // 
            // dainNcnnTilesize
            // 
            this.dainNcnnTilesize.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.dainNcnnTilesize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dainNcnnTilesize.ForeColor = System.Drawing.Color.White;
            this.dainNcnnTilesize.FormattingEnabled = true;
            this.dainNcnnTilesize.Items.AddRange(new object[] {
            "256",
            "384",
            "512",
            "768",
            "1024",
            "1536",
            "2048"});
            this.dainNcnnTilesize.Location = new System.Drawing.Point(280, 7);
            this.dainNcnnTilesize.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this.dainNcnnTilesize.Name = "dainNcnnTilesize";
            this.dainNcnnTilesize.Size = new System.Drawing.Size(87, 21);
            this.dainNcnnTilesize.TabIndex = 79;
            // 
            // panel12
            // 
            this.panel12.BackgroundImage = global::Flowframes.Properties.Resources.baseline_create_white_18dp_semiTransparent;
            this.panel12.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel12.Location = new System.Drawing.Point(378, 7);
            this.panel12.Name = "panel12";
            this.panel12.Size = new System.Drawing.Size(21, 21);
            this.panel12.TabIndex = 78;
            this.toolTip1.SetToolTip(this.panel12, "Allows custom input.");
            // 
            // vidExportTab
            // 
            this.vidExportTab.AutoScroll = true;
            this.vidExportTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.vidExportTab.Controls.Add(this.label73);
            this.vidExportTab.Controls.Add(this.fixOutputDuration);
            this.vidExportTab.Controls.Add(this.label72);
            this.vidExportTab.Controls.Add(this.minOutVidLength);
            this.vidExportTab.Controls.Add(this.loopMode);
            this.vidExportTab.Controls.Add(this.label55);
            this.vidExportTab.Controls.Add(this.panel8);
            this.vidExportTab.Controls.Add(this.panel7);
            this.vidExportTab.Controls.Add(this.label22);
            this.vidExportTab.Controls.Add(this.maxFps);
            this.vidExportTab.Controls.Add(this.label20);
            this.vidExportTab.Controls.Add(this.label21);
            this.vidExportTab.Controls.Add(this.label9);
            this.vidExportTab.Controls.Add(this.label8);
            this.vidExportTab.ForeColor = System.Drawing.Color.White;
            this.vidExportTab.Name = "vidExportTab";
            this.vidExportTab.Size = new System.Drawing.Size(762, 419);
            this.vidExportTab.Text = "Export Options";
            // 
            // label73
            // 
            this.label73.AutoSize = true;
            this.label73.ForeColor = System.Drawing.Color.Silver;
            this.label73.Location = new System.Drawing.Point(308, 130);
            this.label73.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label73.Name = "label73";
            this.label73.Size = new System.Drawing.Size(322, 13);
            this.label73.TabIndex = 84;
            this.label73.Text = "Repeats the last frame so the output is exactly as long as the input.";
            // 
            // fixOutputDuration
            // 
            this.fixOutputDuration.AutoSize = true;
            this.fixOutputDuration.Location = new System.Drawing.Point(280, 130);
            this.fixOutputDuration.Name = "fixOutputDuration";
            this.fixOutputDuration.Size = new System.Drawing.Size(15, 14);
            this.fixOutputDuration.TabIndex = 83;
            this.fixOutputDuration.UseVisualStyleBackColor = true;
            // 
            // label72
            // 
            this.label72.AutoSize = true;
            this.label72.Location = new System.Drawing.Point(10, 130);
            this.label72.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label72.Name = "label72";
            this.label72.Size = new System.Drawing.Size(172, 13);
            this.label72.TabIndex = 76;
            this.label72.Text = "Make Output Match Input Duration";
            // 
            // minOutVidLength
            // 
            this.minOutVidLength.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.minOutVidLength.ForeColor = System.Drawing.Color.White;
            this.minOutVidLength.Location = new System.Drawing.Point(280, 37);
            this.minOutVidLength.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.minOutVidLength.Name = "minOutVidLength";
            this.minOutVidLength.Size = new System.Drawing.Size(100, 20);
            this.minOutVidLength.TabIndex = 75;
            this.minOutVidLength.Value = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            // 
            // loopMode
            // 
            this.loopMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.loopMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.loopMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.loopMode.ForeColor = System.Drawing.Color.White;
            this.loopMode.FormattingEnabled = true;
            this.loopMode.Items.AddRange(new object[] {
            "Only Save Looped Video",
            "Save Both Original And Looped Video"});
            this.loopMode.Location = new System.Drawing.Point(280, 67);
            this.loopMode.Name = "loopMode";
            this.loopMode.Size = new System.Drawing.Size(400, 21);
            this.loopMode.TabIndex = 62;
            // 
            // label55
            // 
            this.label55.AutoSize = true;
            this.label55.Location = new System.Drawing.Point(10, 70);
            this.label55.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label55.Name = "label55";
            this.label55.Size = new System.Drawing.Size(150, 13);
            this.label55.TabIndex = 61;
            this.label55.Text = "Minimum Length Saving Mode";
            // 
            // panel8
            // 
            this.panel8.BackgroundImage = global::Flowframes.Properties.Resources.baseline_create_white_18dp_semiTransparent;
            this.panel8.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel8.Location = new System.Drawing.Point(386, 97);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(21, 21);
            this.panel8.TabIndex = 60;
            this.toolTip1.SetToolTip(this.panel8, "Allows custom input.");
            // 
            // panel7
            // 
            this.panel7.BackgroundImage = global::Flowframes.Properties.Resources.baseline_create_white_18dp_semiTransparent;
            this.panel7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel7.Location = new System.Drawing.Point(386, 37);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(21, 21);
            this.panel7.TabIndex = 59;
            this.toolTip1.SetToolTip(this.panel7, "Allows custom input.");
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.ForeColor = System.Drawing.Color.Silver;
            this.label22.Location = new System.Drawing.Point(420, 101);
            this.label22.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(302, 13);
            this.label22.TabIndex = 48;
            this.label22.Text = "Limit the final output video to this FPS. Leave empty to disable.";
            // 
            // maxFps
            // 
            this.maxFps.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.maxFps.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.maxFps.ForeColor = System.Drawing.Color.White;
            this.maxFps.FormattingEnabled = true;
            this.maxFps.Items.AddRange(new object[] {
            "0",
            "30",
            "60",
            "120"});
            this.maxFps.Location = new System.Drawing.Point(280, 97);
            this.maxFps.Name = "maxFps";
            this.maxFps.Size = new System.Drawing.Size(100, 21);
            this.maxFps.TabIndex = 47;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(10, 100);
            this.label20.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(144, 13);
            this.label20.TabIndex = 46;
            this.label20.Text = "Maximum Output Frame Rate";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.Location = new System.Drawing.Point(10, 10);
            this.label21.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(226, 16);
            this.label21.TabIndex = 45;
            this.label21.Text = "Length And Frame Rate Options";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.ForeColor = System.Drawing.Color.Silver;
            this.label9.Location = new System.Drawing.Point(420, 41);
            this.label9.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(308, 13);
            this.label9.TabIndex = 31;
            this.label9.Text = "Output will be looped until it meets the specified minimum length.";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(10, 40);
            this.label8.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(192, 13);
            this.label8.TabIndex = 30;
            this.label8.Text = "Minimum Loop Video Length (Seconds)";
            // 
            // debugTab
            // 
            this.debugTab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.debugTab.Controls.Add(this.label7);
            this.debugTab.Controls.Add(this.serverCombox);
            this.debugTab.Controls.Add(this.ffEncArgs);
            this.debugTab.Controls.Add(this.label56);
            this.debugTab.Controls.Add(this.label54);
            this.debugTab.Controls.Add(this.ffEncPreset);
            this.debugTab.Controls.Add(this.label47);
            this.debugTab.Controls.Add(this.label46);
            this.debugTab.Controls.Add(this.label45);
            this.debugTab.ForeColor = System.Drawing.Color.White;
            this.debugTab.Name = "debugTab";
            this.debugTab.Size = new System.Drawing.Size(762, 419);
            this.debugTab.Text = "Developer Options";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 40);
            this.label7.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(121, 13);
            this.label7.TabIndex = 87;
            this.label7.Text = "Model Download Server";
            // 
            // serverCombox
            // 
            this.serverCombox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.serverCombox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.serverCombox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.serverCombox.ForeColor = System.Drawing.Color.White;
            this.serverCombox.FormattingEnabled = true;
            this.serverCombox.Items.AddRange(new object[] {
            "Very Fast",
            "Faster",
            "Fast",
            "Medium",
            "Slow",
            "Slower",
            "Very Slow"});
            this.serverCombox.Location = new System.Drawing.Point(280, 37);
            this.serverCombox.Name = "serverCombox";
            this.serverCombox.Size = new System.Drawing.Size(250, 21);
            this.serverCombox.TabIndex = 86;
            // 
            // ffEncArgs
            // 
            this.ffEncArgs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ffEncArgs.ForeColor = System.Drawing.Color.White;
            this.ffEncArgs.Location = new System.Drawing.Point(280, 160);
            this.ffEncArgs.MinimumSize = new System.Drawing.Size(4, 21);
            this.ffEncArgs.Name = "ffEncArgs";
            this.ffEncArgs.Size = new System.Drawing.Size(400, 21);
            this.ffEncArgs.TabIndex = 85;
            // 
            // label56
            // 
            this.label56.AutoSize = true;
            this.label56.Location = new System.Drawing.Point(10, 163);
            this.label56.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label56.Name = "label56";
            this.label56.Size = new System.Drawing.Size(147, 13);
            this.label56.TabIndex = 84;
            this.label56.Text = "Additional (Output) Arguments";
            // 
            // label54
            // 
            this.label54.AutoSize = true;
            this.label54.ForeColor = System.Drawing.Color.Silver;
            this.label54.Location = new System.Drawing.Point(543, 134);
            this.label54.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label54.Name = "label54";
            this.label54.Size = new System.Drawing.Size(118, 13);
            this.label54.TabIndex = 82;
            this.label54.Text = "Slower is more efficient.\r\n";
            // 
            // ffEncPreset
            // 
            this.ffEncPreset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ffEncPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ffEncPreset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ffEncPreset.ForeColor = System.Drawing.Color.White;
            this.ffEncPreset.FormattingEnabled = true;
            this.ffEncPreset.Items.AddRange(new object[] {
            "Very Fast",
            "Faster",
            "Fast",
            "Medium",
            "Slow",
            "Slower",
            "Very Slow"});
            this.ffEncPreset.Location = new System.Drawing.Point(280, 130);
            this.ffEncPreset.Name = "ffEncPreset";
            this.ffEncPreset.Size = new System.Drawing.Size(250, 21);
            this.ffEncPreset.TabIndex = 78;
            // 
            // label47
            // 
            this.label47.AutoSize = true;
            this.label47.Location = new System.Drawing.Point(10, 133);
            this.label47.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label47.Name = "label47";
            this.label47.Size = new System.Drawing.Size(174, 13);
            this.label47.TabIndex = 77;
            this.label47.Text = "Encoding Preset (Speed vs Quality)";
            // 
            // label46
            // 
            this.label46.AutoSize = true;
            this.label46.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label46.Location = new System.Drawing.Point(10, 100);
            this.label46.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label46.Name = "label46";
            this.label46.Size = new System.Drawing.Size(133, 16);
            this.label46.TabIndex = 76;
            this.label46.Text = "Encoding (ffmpeg)";
            // 
            // label45
            // 
            this.label45.AutoSize = true;
            this.label45.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label45.Location = new System.Drawing.Point(10, 10);
            this.label45.Margin = new System.Windows.Forms.Padding(10, 10, 10, 7);
            this.label45.Name = "label45";
            this.label45.Size = new System.Drawing.Size(62, 16);
            this.label45.TabIndex = 75;
            this.label45.Text = "General";
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Yu Gothic UI", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(12, 9);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(119, 40);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.Text = "Settings";
            // 
            // resetBtn
            // 
            this.resetBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.resetBtn.ButtonImage = global::Flowframes.Properties.Resources.baseline_restart_alt_white_48dp_40px;
            this.resetBtn.DrawImage = true;
            this.resetBtn.FlatAppearance.BorderSize = 0;
            this.resetBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.resetBtn.ForeColor = System.Drawing.Color.White;
            this.resetBtn.ImageIndex = 0;
            this.resetBtn.Location = new System.Drawing.Point(889, 12);
            this.resetBtn.Name = "resetBtn";
            this.resetBtn.Size = new System.Drawing.Size(40, 40);
            this.resetBtn.TabIndex = 39;
            this.toolTip1.SetToolTip(this.resetBtn, "Reset To Default");
            this.resetBtn.UseVisualStyleBackColor = false;
            this.resetBtn.Click += new System.EventHandler(this.resetBtn_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.flowLayoutPanel1.Controls.Add(this.onlyShowRelevantSettings);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(683, 12);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(200, 40);
            this.flowLayoutPanel1.TabIndex = 41;
            // 
            // onlyShowRelevantSettings
            // 
            this.onlyShowRelevantSettings.Checked = true;
            this.onlyShowRelevantSettings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.onlyShowRelevantSettings.ForeColor = System.Drawing.Color.White;
            this.onlyShowRelevantSettings.Location = new System.Drawing.Point(9, 3);
            this.onlyShowRelevantSettings.Margin = new System.Windows.Forms.Padding(9, 3, 3, 3);
            this.onlyShowRelevantSettings.Name = "onlyShowRelevantSettings";
            this.onlyShowRelevantSettings.Size = new System.Drawing.Size(179, 34);
            this.onlyShowRelevantSettings.TabIndex = 0;
            this.onlyShowRelevantSettings.Text = "Only Show Relevant Settings";
            this.onlyShowRelevantSettings.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(944, 501);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.resetBtn);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.settingsTabList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Flowframes Settings ";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsForm_FormClosing);
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.settingsTabList.ResumeLayout(false);
            this.generalTab.ResumeLayout(false);
            this.flowPanelApplication.ResumeLayout(false);
            this.panProcessingStyle.ResumeLayout(false);
            this.panProcessingStyle.PerformLayout();
            this.panMaxRes.ResumeLayout(false);
            this.panMaxRes.PerformLayout();
            this.panTempFolder.ResumeLayout(false);
            this.panTempFolder.PerformLayout();
            this.panOutputLocation.ResumeLayout(false);
            this.panOutputLocation.PerformLayout();
            this.panKeepTempFolder.ResumeLayout(false);
            this.panKeepTempFolder.PerformLayout();
            this.panExportName.ResumeLayout(false);
            this.panExportName.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.info1)).EndInit();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel11.ResumeLayout(false);
            this.panel11.PerformLayout();
            this.tabListPage2.ResumeLayout(false);
            this.flowPanelInterpolation.ResumeLayout(false);
            this.panTitleInputMedia.ResumeLayout(false);
            this.panTitleInputMedia.PerformLayout();
            this.panCopyInputMedia.ResumeLayout(false);
            this.panCopyInputMedia.PerformLayout();
            this.panEnableAlpha.ResumeLayout(false);
            this.panEnableAlpha.PerformLayout();
            this.panHqJpegImport.ResumeLayout(false);
            this.panHqJpegImport.PerformLayout();
            this.panTitleInterpHelpers.ResumeLayout(false);
            this.panTitleInterpHelpers.PerformLayout();
            this.panDedupe.ResumeLayout(false);
            this.panDedupe.PerformLayout();
            this.mpDedupePanel.ResumeLayout(false);
            this.magickDedupePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dedupThresh)).EndInit();
            this.panLoop.ResumeLayout(false);
            this.panLoop.PerformLayout();
            this.panSceneChange.ResumeLayout(false);
            this.panSceneChange.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scnDetectValue)).EndInit();
            this.panAutoEnc.ResumeLayout(false);
            this.panAutoEnc.PerformLayout();
            this.panAutoEncInSbsMode.ResumeLayout(false);
            this.panAutoEncInSbsMode.PerformLayout();
            this.panAutoEncBackups.ResumeLayout(false);
            this.panAutoEncBackups.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.panAutoEncLowSpaceMode.ResumeLayout(false);
            this.panAutoEncLowSpaceMode.PerformLayout();
            this.aiOptsPage.ResumeLayout(false);
            this.flowPanelAiOptions.ResumeLayout(false);
            this.panTitleAiFramework.ResumeLayout(false);
            this.panTitleAiFramework.PerformLayout();
            this.panTorchGpus.ResumeLayout(false);
            this.panTorchGpus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tooltipTorchGpu)).EndInit();
            this.panNcnnGpus.ResumeLayout(false);
            this.panNcnnGpus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tooltipNcnnGpu)).EndInit();
            this.panNcnnThreads.ResumeLayout(false);
            this.panNcnnThreads.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ncnnThreads)).EndInit();
            this.panTitleRife.ResumeLayout(false);
            this.panTitleRife.PerformLayout();
            this.panUhdThresh.ResumeLayout(false);
            this.panUhdThresh.PerformLayout();
            this.panRifeCudaHalfPrec.ResumeLayout(false);
            this.panRifeCudaHalfPrec.PerformLayout();
            this.panTitleDainNcnn.ResumeLayout(false);
            this.panTitleDainNcnn.PerformLayout();
            this.panDainNcnnTileSize.ResumeLayout(false);
            this.panDainNcnnTileSize.PerformLayout();
            this.vidExportTab.ResumeLayout(false);
            this.vidExportTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minOutVidLength)).EndInit();
            this.debugTab.ResumeLayout(false);
            this.debugTab.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Cyotek.Windows.Forms.TabList settingsTabList;
        private Cyotek.Windows.Forms.TabListPage generalTab;
        private Cyotek.Windows.Forms.TabListPage tabListPage2;
        private Cyotek.Windows.Forms.TabListPage debugTab;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox keepAudio;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox dedupMode;
        private System.Windows.Forms.Label dedupeSensLabel;
        private System.Windows.Forms.CheckBox enableLoop;
        private System.Windows.Forms.Label label15;
        private Cyotek.Windows.Forms.TabListPage vidExportTab;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.ComboBox maxFps;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label24;
        private Cyotek.Windows.Forms.TabListPage aiOptsPage;
        private System.Windows.Forms.ComboBox torchGpus;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.ComboBox ncnnGpus;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.ComboBox maxVidHeight;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.ComboBox tempFolderLoc;
        private System.Windows.Forms.CheckBox keepTempFolder;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tempDirCustom;
        private HTAlt.WinForms.HTButton tempDirBrowseBtn;
        private System.Windows.Forms.ComboBox processingMode;
        private System.Windows.Forms.Label label39;
        private System.Windows.Forms.Panel mpDedupePanel;
        private System.Windows.Forms.ComboBox mpdecimateMode;
        private System.Windows.Forms.Panel magickDedupePanel;
        private System.Windows.Forms.Label label44;
        private System.Windows.Forms.Label label43;
        private System.Windows.Forms.ComboBox ffEncPreset;
        private System.Windows.Forms.Label label47;
        private System.Windows.Forms.Label label46;
        private System.Windows.Forms.Label label45;
        private System.Windows.Forms.Label label51;
        private System.Windows.Forms.CheckBox scnDetect;
        private System.Windows.Forms.Label label50;
        private System.Windows.Forms.Label label52;
        private System.Windows.Forms.Label label54;
        private System.Windows.Forms.ComboBox loopMode;
        private System.Windows.Forms.Label label55;
        private System.Windows.Forms.Panel panel14;
        private System.Windows.Forms.Label label49;
        private System.Windows.Forms.ComboBox autoEncMode;
        private System.Windows.Forms.CheckBox sbsAllowAutoEnc;
        private System.Windows.Forms.Label label53;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.ComboBox uhdThresh;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox ffEncArgs;
        private System.Windows.Forms.Label label56;
        private System.Windows.Forms.Label label64;
        private HTAlt.WinForms.HTButton clearModelCacheBtn;
        private System.Windows.Forms.NumericUpDown scnDetectValue;
        private System.Windows.Forms.NumericUpDown dedupThresh;
        private System.Windows.Forms.NumericUpDown ncnnThreads;
        private System.Windows.Forms.NumericUpDown minOutVidLength;
        private System.Windows.Forms.CheckBox keepSubs;
        private System.Windows.Forms.CheckBox enableAlpha;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label35;
        private System.Windows.Forms.Panel panel12;
        private System.Windows.Forms.ComboBox dainNcnnTilesize;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.CheckBox rifeCudaFp16;
        private System.Windows.Forms.Label label65;
        private System.Windows.Forms.Label label66;
        private System.Windows.Forms.TextBox exportNamePattern;
        private System.Windows.Forms.Label label67;
        private System.Windows.Forms.PictureBox info1;
        private System.Windows.Forms.TextBox exportNamePatternLoop;
        private System.Windows.Forms.Label label69;
        private System.Windows.Forms.Label label68;
        private System.Windows.Forms.Label label73;
        private System.Windows.Forms.CheckBox fixOutputDuration;
        private System.Windows.Forms.Label label72;
        private System.Windows.Forms.CheckBox keepMeta;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label74;
        private System.Windows.Forms.CheckBox jpegFrames;
        private System.Windows.Forms.Label label63;
        private System.Windows.Forms.Label label41;
        private System.Windows.Forms.ComboBox autoEncBackupMode;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.PictureBox pictureBox2;
        private HTAlt.WinForms.HTButton modelDownloaderBtn;
        private System.Windows.Forms.Label label70;
        private System.Windows.Forms.CheckBox alwaysWaitForAutoEnc;
        private System.Windows.Forms.Label label58;
        private HTAlt.WinForms.HTButton resetBtn;
        private HTAlt.WinForms.HTButton custOutDirBrowseBtn;
        private System.Windows.Forms.TextBox custOutDir;
        private System.Windows.Forms.ComboBox outFolderLoc;
        private System.Windows.Forms.Label label78;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox serverCombox;
        private HTAlt.WinForms.HTButton btnResetHwEnc;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.FlowLayoutPanel flowPanelApplication;
        public System.Windows.Forms.Panel panProcessingStyle;
        public System.Windows.Forms.Panel panMaxRes;
        public System.Windows.Forms.Panel panTempFolder;
        public System.Windows.Forms.Panel panOutputLocation;
        public System.Windows.Forms.Panel panKeepTempFolder;
        public System.Windows.Forms.Panel panExportName;
        public System.Windows.Forms.Panel panel5;
        public System.Windows.Forms.Panel panel11;
        private System.Windows.Forms.FlowLayoutPanel flowPanelInterpolation;
        public System.Windows.Forms.Panel panCopyInputMedia;
        public System.Windows.Forms.Panel panEnableAlpha;
        public System.Windows.Forms.Panel panTitleInputMedia;
        public System.Windows.Forms.Panel panHqJpegImport;
        public System.Windows.Forms.Panel panTitleInterpHelpers;
        public System.Windows.Forms.Panel panDedupe;
        public System.Windows.Forms.Panel panLoop;
        public System.Windows.Forms.Panel panSceneChange;
        public System.Windows.Forms.Panel panAutoEnc;
        public System.Windows.Forms.Panel panAutoEncInSbsMode;
        public System.Windows.Forms.Panel panAutoEncBackups;
        public System.Windows.Forms.Panel panAutoEncLowSpaceMode;
        private System.Windows.Forms.FlowLayoutPanel flowPanelAiOptions;
        public System.Windows.Forms.Panel panTitleAiFramework;
        public System.Windows.Forms.Panel panTorchGpus;
        public System.Windows.Forms.Panel panNcnnGpus;
        public System.Windows.Forms.Panel panNcnnThreads;
        public System.Windows.Forms.Panel panTitleRife;
        public System.Windows.Forms.Panel panUhdThresh;
        public System.Windows.Forms.Panel panRifeCudaHalfPrec;
        public System.Windows.Forms.Panel panTitleDainNcnn;
        public System.Windows.Forms.Panel panDainNcnnTileSize;
        private System.Windows.Forms.PictureBox tooltipTorchGpu;
        private System.Windows.Forms.PictureBox tooltipNcnnGpu;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox onlyShowRelevantSettings;
    }
}