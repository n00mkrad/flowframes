using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
using Flowframes.OS;
using Flowframes.UI;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using HTAlt.WinForms;
using Flowframes.Data;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Threading.Tasks;

namespace Flowframes
{
    public partial class Form1 : Form
    {
        public bool initialized = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            Text = $"Flowframes v{Program.version}";

            // Main Tab
            UIUtils.InitCombox(interpFactorCombox, 0);
            UIUtils.InitCombox(outModeCombox, 0);
            UIUtils.InitCombox(tilesize, 4);
            // Video Utils
            UIUtils.InitCombox(utilsLoopTimesCombox, 0);
            UIUtils.InitCombox(utilsSpeedCombox, 0);
            UIUtils.InitCombox(utilsConvCrf, 0);

            Program.mainForm = this;
            Logger.textbox = logBox;

            InitAis();
            InterpolateUtils.preview = previewPicturebox;

            ConfigParser.LoadComboxIndex(aiCombox);

            Setup.Init();
            UpdateStepByStepControls();

            Initialized();
            Checks();
        }

        void Checks ()
        {
            GetWebInfo.LoadNews(newsLabel);
            GetWebInfo.LoadPatronList(patronsLabel);
            Updater.AsyncUpdateCheck();
        }

        public HTTabControl GetMainTabControl() { return mainTabControl; }

        public bool IsInFocus() { return (ActiveForm == this); }

        public void SetTab (string tabName)
        {
            foreach(TabPage tab in mainTabControl.TabPages)
            {
                if (tab.Text.ToLower() == tabName.ToLower())
                    mainTabControl.SelectedTab = tab;
            }
            mainTabControl.Refresh();
            mainTabControl.Update();
        }

        public BatchEntry GetBatchEntry()
        {
            SetTab("interpolate");
            return new BatchEntry(inputTbox.Text.Trim(), outputTbox.Text.Trim(), GetAi(), fpsInTbox.GetFloat(), interpFactorCombox.GetInt(), GetOutMode());
        }

        public void LoadBatchEntry(BatchEntry entry)
        {
            inputTbox.Text = entry.inPath;
            outputTbox.Text = entry.outPath;
            interpFactorCombox.Text = entry.interpFactor.ToString();
            aiCombox.SelectedIndex = Networks.networks.IndexOf(entry.ai);
            SetOutMode(entry.outMode);
        }

        public void SetStatus(string str)
        {
            Logger.Log(str, true);
            statusLabel.Text = str;
        }

        public void SetProgress(int percent)
        {
            longProgBar.Value = percent.Clamp(0, 100);
            longProgBar.Refresh();
        }

        void InitAis()
        {
            foreach (AI ai in Networks.networks)
                aiCombox.Items.Add(ai.friendlyName + " - " + ai.description);
            aiCombox.SelectedIndex = 0;
        }

        public void Initialized()
        {
            initialized = true;
            runBtn.Enabled = true;
        }

        private void browseInputBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = inputTbox.Text.Trim();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                inputTbox.Text = dialog.FileName;
                MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox);
            }
        }

        private void browseInputFileBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = inputTbox.Text.Trim();
            dialog.IsFolderPicker = false;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                inputTbox.Text = dialog.FileName;
                MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox);
            }
        }

        private void browseOutBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = inputTbox.Text.Trim();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                outputTbox.Text = dialog.FileName;
        }

        public void runBtn_Click(object sender, EventArgs e)
        {
            SetTab("interpolate");
            if (!BatchProcessing.busy)
                SetTab("interpolation");
            Interpolate.SetFps(fpsInTbox.GetFloat());
            Interpolate.interpFactor = interpFactorCombox.GetInt();
            string inPath = inputTbox.Text.Trim();
            string outPath = outputTbox.Text.Trim();
            Interpolate.Start(inPath, outPath, GetTilesize(), GetOutMode(), GetAi());
        }

        public int GetTilesize ()
        {
            if (GetAi().supportsTiling)
                return tilesize.GetInt();
            else
                return 512;
        }

        Interpolate.OutMode GetOutMode()
        {
            Interpolate.OutMode outMode = Interpolate.OutMode.VidMp4;
            if (outModeCombox.Text.ToLower().Contains("gif")) outMode = Interpolate.OutMode.VidGif;
            if (outModeCombox.Text.ToLower().Contains("image")) outMode = Interpolate.OutMode.ImgPng;
            return outMode;
        }

        public void SetOutMode(Interpolate.OutMode mode)
        {
            if (mode == Interpolate.OutMode.VidMp4) outModeCombox.SelectedIndex = 0;
            if (mode == Interpolate.OutMode.VidGif) outModeCombox.SelectedIndex = 1;
            if (mode == Interpolate.OutMode.ImgPng) outModeCombox.SelectedIndex = 2;
        }

        AI GetAi()
        {
            return Networks.networks[aiCombox.SelectedIndex];
        }

        void inputTbox_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void inputTbox_DragDrop(object sender, DragEventArgs e)
        {
            if (Program.busy) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            inputTbox.Text = files[0];
            MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox);
        }

        void outputTbox_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void outputTbox_DragDrop(object sender, DragEventArgs e)
        {
            if (Program.busy) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            outputTbox.Text = files[0];
        }

        private void fpsInTbox_TextChanged(object sender, EventArgs e)
        {
            fpsInTbox.Text = fpsInTbox.Text.TrimNumbers(true);
            Interpolate.SetFps(fpsInTbox.GetFloat());
            UpdateOutputFPS();
        }

        public void UpdateOutputFPS()
        {
            float fpsOut = fpsInTbox.GetFloat() * interpFactorCombox.GetFloat();
            fpsOutTbox.Text = fpsOut.ToString();
            Interpolate.interpFactor = interpFactorCombox.GetInt();
        }

        private void interpFactorCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateOutputFPS();
        }

        public void SetWorking(bool state)
        {
            Control[] controlsToDisable = new Control[] { runBtn, runStepBtn, stepSelector, settingsBtn, installerBtn };
            Control[] controlsToHide = new Control[] { runBtn, runStepBtn, stepSelector };
            progressCircle.Visible = state;
            cancelBtn.Visible = state;
            foreach (Control c in controlsToDisable)
                c.Enabled = !state;
            foreach (Control c in controlsToHide)
                c.Visible = !state;
            Program.busy = state;
        }

        private void aiCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            tilesize.Visible = GetAi().supportsTiling;
            tilesizeNotAvailLabel.Visible = !tilesize.Visible;
            interpFactorCombox_SelectedIndexChanged(null, null);
            if(GetAi().supportsTiling)
                tilesize.Text = Config.GetInt($"tilesize_{GetAi().aiName}").ToString();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ConfigParser.SaveComboxIndex(aiCombox);
        }

        private async void debugExtractFramesBtn_Click(object sender, EventArgs e)
        {
            await UtilsTab.ExtractVideo(inputTbox.Text.Trim(), utilsExtractAudioCbox.Checked);
        }

        private void licenseBtn_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.licenses.fileName)));
        }

        private async void utilsLoopVidBtn_Click(object sender, EventArgs e)
        {
            await UtilsTab.LoopVideo(inputTbox.Text.Trim(), utilsLoopTimesCombox);
        }

        private async void utilsChangeSpeedBtn_Click(object sender, EventArgs e)
        {
            await UtilsTab.ChangeSpeed(inputTbox.Text.Trim(), utilsSpeedCombox);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (Program.busy) return;
            SetTab("interpolation");
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            inputTbox.Text = files[0];
            Logger.Log("Selected video/directory: " + Path.GetFileName(files[0]));
            MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox);
        }

        private async void utilsConvertMp4Btn_Click(object sender, EventArgs e)
        {
            await UtilsTab.Convert(inputTbox.Text.Trim(), utilsConvCrf);
        }

        private void utilsDedupBtn_Click(object sender, EventArgs e)
        {
            UtilsTab.Dedupe(inputTbox.Text.Trim(), false);
        }

        private void utilsDedupTestBtn_Click(object sender, EventArgs e)
        {
            UtilsTab.Dedupe(inputTbox.Text.Trim(), true);
        }

        private void installerBtn_Click(object sender, EventArgs e)
        {
            new InstallerForm().ShowDialog();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            SetTab("interpolation");
            Interpolate.Cancel();
        }

        private void discordBtn_Click(object sender, EventArgs e)
        {
            Process.Start("https://discord.gg/eJHD2NSJRe");
        }

        private void paypalBtn_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.paypal.com/paypalme/nmkd/10");
        }

        private void patreonBtn_Click(object sender, EventArgs e)
        {
            Process.Start("https://patreon.com/n00mkrad");
        }

        private void settingsBtn_Click(object sender, EventArgs e)
        {
            new SettingsForm().ShowDialog();
        }

        private void queueBtn_Click(object sender, EventArgs e)
        {
            if (BatchProcessing.currentBatchForm != null)
            {
                BatchProcessing.currentBatchForm.WindowState = FormWindowState.Normal;
                BatchProcessing.currentBatchForm.BringToFront();
            }
            else
            {
                new BatchForm().Show();
            }
        }

        private void previewPicturebox_MouseClick(object sender, MouseEventArgs e)
        {
            if (InterpolateUtils.bigPreviewForm == null)
            {
                InterpolateUtils.bigPreviewForm = new BigPreviewForm();
                InterpolateUtils.bigPreviewForm.Show();
                InterpolateUtils.bigPreviewForm.SetImage(previewPicturebox.Image);
            }
        }

        private async void updateBtn_Click(object sender, EventArgs e)
        {
            new UpdaterForm().ShowDialog();
        }

        private void welcomeLabel2_Click(object sender, EventArgs e)
        {
            SetTab("interpolation");
        }

        public void UpdateStepByStepControls ()
        {
            stepSelector.Items.Clear();
            if(Config.GetBool("scnDetect"))
                stepSelector.Items.AddRange(new string[] { "1) Extract Scene Changes", "2) Import/Extract Frames", "3) Run Interpolation", "4) Export", "5) Cleanup & Reset" });
            else
                stepSelector.Items.AddRange(new string[] { "1) Import/Extract Frames", "2) Run Interpolation", "3) Export", "4) Cleanup & Reset" });
            stepSelector.SelectedIndex = 0;
            bool stepByStep = Config.GetInt("processingMode") == 1;
            stepSelector.Visible = stepByStep;
            runStepBtn.Visible = stepByStep;
            runBtn.Visible = !stepByStep;
        }

        private async void runStepBtn_Click(object sender, EventArgs e)
        {
            SetTab("interpolate");
            Interpolate.SetFps(fpsInTbox.GetFloat());
            await InterpolateSteps.Run(stepSelector.Text);
        }

        private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!initialized) return;
            aiCombox_SelectedIndexChanged(null, null);
        }

        private void tilesize_TextChanged(object sender, EventArgs e)
        {
            if (!initialized || !GetAi().supportsTiling) return;
            Config.Set($"tilesize_{GetAi().aiName}", tilesize.GetInt().Clamp(32, 4096).ToString());
        }
    }
}
