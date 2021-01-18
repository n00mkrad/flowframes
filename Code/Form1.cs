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
            AutoScaleMode = AutoScaleMode.None;
            Text = $"Flowframes {Updater.GetInstalledVer()}";

            // Main Tab
            UIUtils.InitCombox(interpFactorCombox, 0);
            UIUtils.InitCombox(outModeCombox, 0);
            UIUtils.InitCombox(aiModel, 2);
            // Video Utils
            UIUtils.InitCombox(utilsLoopTimesCombox, 0);
            UIUtils.InitCombox(utilsSpeedCombox, 0);
            UIUtils.InitCombox(utilsConvCrf, 0);

            Program.mainForm = this;
            Logger.textbox = logBox;

            InitAis();
            InterpolateUtils.preview = previewPicturebox;

            ConfigParser.LoadComboxIndex(aiCombox);

            UpdateStepByStepControls(true);

            Initialized();
            Checks();
        }

        void Checks()
        {
            try
            {
                GetWebInfo.LoadNews(newsLabel);
                GetWebInfo.LoadPatronListCsv(patronsLabel);
                Updater.AsyncUpdateCheck();
                Python.CheckCompression();
            }
            catch (Exception e)
            {
                Logger.Log("Non-critical error while performing online checks. See logs for details.");
                Logger.Log(e.Message + "\n" + e.StackTrace, true);
            }
        }

        public HTTabControl GetMainTabControl() { return mainTabControl; }

        public bool IsInFocus() { return (ActiveForm == this); }

        public void SetTab(string tabName)
        {
            foreach (TabPage tab in mainTabControl.TabPages)
            {
                if (tab.Text.ToLower() == tabName.ToLower())
                    mainTabControl.SelectedTab = tab;
            }
            mainTabControl.Refresh();
            mainTabControl.Update();
        }

        public InterpSettings GetCurrentSettings()
        {
            SetTab("interpolate");
            return new InterpSettings(inputTbox.Text.Trim(), outputTbox.Text.Trim(), GetAi(), fpsInTbox.GetFloat(), interpFactorCombox.GetInt(), GetOutMode(), GetModel());
        }

        public void LoadBatchEntry(InterpSettings entry)
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

        public Size currInRes;
        public float currInFps;
        public int currInFrames;
        public void UpdateInputInfo ()
        {
            string str = $"Resolution: {(!currInRes.IsEmpty ? $"{currInRes.Width}x{currInRes.Height}" : "Unknown")} - ";
            str += $"Framerate: {(currInFps > 0f ? $"{currInFps.ToStringDot()} FPS" : "Unknown")} - ";
            str += $"Frame Count: {(currInFrames > 0 ? $"{currInFrames} Frames" : "Unknown")}";
            inputInfo.Text = str;
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
                DragDropHandler(new string[] { dialog.FileName });
        }

        private void browseInputFileBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = inputTbox.Text.Trim();
            dialog.IsFolderPicker = false;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                DragDropHandler(new string[] { dialog.FileName });
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
            if (!BatchProcessing.busy)      // Don't load values from gui if batch processing is used
                Interpolate.current = GetCurrentSettings();

            Interpolate.Start();
        }

        public string GetModel()
        {
            return aiModel.Text.Split('-')[0].Remove(" ").Remove(".");
        }

        Interpolate.OutMode GetOutMode()
        {
            Interpolate.OutMode outMode = Interpolate.OutMode.VidMp4;
            if (outModeCombox.Text.ToLower().Contains("mkv")) outMode = Interpolate.OutMode.VidMkv;
            if (outModeCombox.Text.ToLower().Contains("webm")) outMode = Interpolate.OutMode.VidWebm;
            if (outModeCombox.Text.ToLower().Contains("prores")) outMode = Interpolate.OutMode.VidProRes;
            if (outModeCombox.Text.ToLower().Contains("avi")) outMode = Interpolate.OutMode.VidAvi;
            if (outModeCombox.Text.ToLower().Contains("gif")) outMode = Interpolate.OutMode.VidGif;
            if (outModeCombox.Text.ToLower().Contains("image")) outMode = Interpolate.OutMode.ImgPng;
            return outMode;
        }

        public void SetOutMode(Interpolate.OutMode mode)
        {
            int theIndex = 0;
            for(int i = 0; i < outModeCombox.Items.Count; i++)
            {
                string currentItem = outModeCombox.Items[i].ToString().ToLower();
                if (mode == Interpolate.OutMode.VidMkv && currentItem.Contains("mkv")) theIndex = i;
                if (mode == Interpolate.OutMode.VidWebm && currentItem.Contains("webm")) theIndex = i;
                if (mode == Interpolate.OutMode.VidProRes && currentItem.Contains("prores")) theIndex = i;
                if (mode == Interpolate.OutMode.VidAvi && currentItem.Contains("avi")) theIndex = i;
                if (mode == Interpolate.OutMode.VidGif && currentItem.Contains("gif")) theIndex = i;
                if (mode == Interpolate.OutMode.ImgPng && currentItem.Contains("image")) theIndex = i;
            }
            outModeCombox.SelectedIndex = theIndex;
        }

        AI GetAi()
        {
            return Networks.networks[aiCombox.SelectedIndex];
        }

        void inputTbox_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void inputTbox_DragDrop(object sender, DragEventArgs e)
        {
            DragDropHandler((string[])e.Data.GetData(DataFormats.FileDrop));
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
            //Interpolate.SetFps(fpsInTbox.GetFloat());
            UpdateOutputFPS();
        }

        public void UpdateOutputFPS()
        {
            float fpsOut = fpsInTbox.GetFloat() * interpFactorCombox.GetFloat();
            fpsOutTbox.Text = fpsOut.ToString();
            //Interpolate.interpFactor = interpFactorCombox.GetInt();
        }

        private void interpFactorCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateOutputFPS();
            int guiInterpFactor = interpFactorCombox.GetInt();
            if (!initialized)
                return;
            string aiName = GetAi().aiName.Replace("_", "-");
            if (!Program.busy && guiInterpFactor > 2 && !GetAi().supportsAnyExp && Config.GetInt("autoEncMode") > 0 && !Logger.GetLastLine().Contains(aiName))
                Logger.Log($"Warning: {aiName} doesn't natively support 4x/8x and will run multiple times for {guiInterpFactor}x. Auto-Encode will only work on the last run.");
        }

        public void SetWorking(bool state, bool allowCancel = true)
        {
            Logger.Log($"SetWorking({state})", true);
            SetProgress(-1);
            Control[] controlsToDisable = new Control[] { runBtn, runStepBtn, stepSelector, settingsBtn };
            Control[] controlsToHide = new Control[] { runBtn, runStepBtn, stepSelector };
            progressCircle.Visible = state;
            cancelBtn.Visible = state;
            foreach (Control c in controlsToDisable)
                c.Enabled = !state;
            foreach (Control c in controlsToHide)
                c.Visible = !state;
            cancelBtn.Enabled = allowCancel;
            Program.busy = state;
            Program.mainForm.UpdateStepByStepControls(false);
        }

        string lastAiComboxStr = "";
        private void aiCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(aiCombox.Text) || aiCombox.Text == lastAiComboxStr) return;
            lastAiComboxStr = aiCombox.Text;
            aiModel = UIUtils.FillAiModelsCombox(aiModel, GetAi());
            interpFactorCombox_SelectedIndexChanged(null, null);
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
            DragDropHandler((string[])e.Data.GetData(DataFormats.FileDrop));
        }

        public void DragDropHandler(string[] files)
        {
            if (Program.busy) return;

            if (files.Length > 1)
            {
                queueBtn_Click(null, null);
                if (BatchProcessing.currentBatchForm != null)
                    BatchProcessing.currentBatchForm.LoadDroppedPaths(files);
            }
            else
            {
                SetTab("interpolation");
                Logger.Log("Selected video/directory: " + Path.GetFileName(files[0]));
                inputTbox.Text = files[0];
                MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox);
            }
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

        public void UpdateStepByStepControls(bool settingsMayHaveChanged)
        {
            if (settingsMayHaveChanged)
            {
                stepSelector.Items.Clear();
                if (Config.GetBool("scnDetect"))
                    stepSelector.Items.AddRange(new string[] { "1) Extract Scene Changes", "2) Import/Extract Frames", "3) Run Interpolation", "4) Export", "5) Cleanup & Reset" });
                else
                    stepSelector.Items.AddRange(new string[] { "1) Import/Extract Frames", "2) Run Interpolation", "3) Export", "4) Cleanup & Reset" });
                stepSelector.SelectedIndex = 0;
            }
            bool stepByStep = Config.GetInt("processingMode") == 1;
            runBtn.Visible = !stepByStep && !Program.busy;
        }

        private async void runStepBtn_Click(object sender, EventArgs e)
        {
            SetTab("interpolate");
            await InterpolateSteps.Run(stepSelector.Text);
        }

        private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!initialized) return;
            aiCombox_SelectedIndexChanged(null, null);
        }
    }
}
