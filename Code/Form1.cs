using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.OS;
using Flowframes.UI;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HTAlt.WinForms;
using Flowframes.Data;
using Microsoft.WindowsAPICodePack.Taskbar;
using Flowframes.MiscUtils;
using System.Threading.Tasks;

namespace Flowframes
{
    public partial class Form1 : Form
    {
        public bool initialized = false;
        public bool quickSettingsInitialized = false;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            AutoScaleMode = AutoScaleMode.None;

            StartupChecks.CheckOs();

            // Main Tab
            UIUtils.InitCombox(interpFactorCombox, 0);
            UIUtils.InitCombox(outModeCombox, 0);
            UIUtils.InitCombox(aiModel, 2);
            // Video Utils
            UIUtils.InitCombox(trimCombox, 0);

            Program.mainForm = this;
            Logger.textbox = logBox;
            NvApi.Init();
            InitAis();
            InterpolationProgress.preview = previewPicturebox;
            UpdateStepByStepControls();
            Initialized();
            HandleArguments();
            Text = $"Flowframes";
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Logger.Log("Debugger is attached - Flowframes seems to be running within VS.");
                scnDetectTestBtn.Visible = true;
            }

            await Checks();
        }

        async Task Checks()
        {
            try
            {
                await Task.Delay(100);
                await StartupChecks.SymlinksCheck();
                await Updater.UpdateModelList();    // Update AI model list
                await Updater.AsyncUpdateCheck();   // Check for Flowframes updates
                await GetWebInfo.LoadNews(newsLabel);   // Loads news/MOTD
                await GetWebInfo.LoadPatronListCsv(patronsLabel);   // Load patron list
                await Python.CheckCompression();
            }
            catch (Exception e)
            {
                Logger.Log("Non-critical error while performing online checks. See logs for details.");
                Logger.Log($"{e.Message}\n{e.StackTrace}", true);
            }
        }

        void HandleArguments()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                List<string> files = new List<string>();

                foreach (string arg in args)
                    if (Path.GetExtension(arg) != ".exe" && IOUtils.IsFileValid(arg)) 
                        files.Add(arg);

                if(files.Count > 0)
                    DragDropHandler(files.ToArray());
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to load input from given launch arguments.");
                Logger.Log($"{e.Message}\n{e.StackTrace}", true);
            }
        }

        public HTTabControl GetMainTabControl() { return mainTabControl; }
        public TextBox GetInputFpsTextbox () { return fpsInTbox; }

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
            return new InterpSettings(inputTbox.Text.Trim(), outputTbox.Text.Trim(), GetAi(), currInFpsDetected, currInFps, interpFactorCombox.GetInt(), GetOutMode(), GetModel());
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

        public string GetStatus ()
        {
            return statusLabel.Text;
        }

        public void SetProgress(int percent)
        {
            percent = percent.Clamp(0, 100);
            TaskbarManager.Instance.SetProgressValue(percent, 100);
            longProgBar.Value = percent;
            longProgBar.Refresh();
        }

        public Size currInRes;
        public Fraction currInFpsDetected;
        public Fraction currInFps;
        public int currInFrames;
        public long currInDuration;
        public long currInDurationCut;

        public void UpdateInputInfo ()
        {
            string str = $"Size: {(!currInRes.IsEmpty ? $"{currInRes.Width}x{currInRes.Height}" : "Unknown")} - ";
            str += $"Rate: {(currInFpsDetected.GetFloat() > 0f ? $"{currInFpsDetected} ({currInFpsDetected.GetFloat()})" : "Unknown")} - ";
            str += $"Frames: {(currInFrames > 0 ? $"{currInFrames}" : "Unknown")} - ";
            str += $"Duration: {(currInDuration > 0 ? FormatUtils.MsToTimestamp(currInDuration) : "Unknown")}";
            inputInfo.Text = str;
        }

        public void ResetInputInfo ()
        {
            currInRes = new Size();
            currInFpsDetected = new Fraction();
            currInFps = new Fraction();
            currInFrames = 0;
            currInDuration = 0;
            currInDurationCut = 0;
            UpdateInputInfo();
        }

        void InitAis()
        {
            foreach (AI ai in Networks.networks)
                aiCombox.Items.Add(ai.friendlyName + " - " + ai.description);
            ConfigParser.LoadComboxIndex(aiCombox);
        }

        public void Initialized()
        {
            initialized = true;
            runBtn.Enabled = true;
        }

        private void browseInputBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = inputTbox.Text.Trim(), IsFolderPicker = true };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                DragDropHandler(new string[] { dialog.FileName });
        }

        private void browseInputFileBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = inputTbox.Text.Trim(), IsFolderPicker = false };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                DragDropHandler(new string[] { dialog.FileName });
        }

        private void browseOutBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = inputTbox.Text.Trim(), IsFolderPicker = true };

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
            UpdateUiFps();
        }

        public void UpdateUiFps()
        {
            if (fpsInTbox.Text.Contains("/"))   // Parse fraction
            {
                string[] split = fpsInTbox.Text.Split('/');
                Fraction frac = new Fraction(split[0].GetInt(), split[1].GetInt());
                fpsOutTbox.Text = (frac * interpFactorCombox.GetFloat()).ToString();

                if (!fpsInTbox.ReadOnly)
                    currInFps = frac;
            }
            else    // Parse float
            {
                fpsInTbox.Text = fpsInTbox.Text.TrimNumbers(true);
                fpsOutTbox.Text = (fpsInTbox.GetFloat() * interpFactorCombox.GetFloat()).ToString();

                if (!fpsInTbox.ReadOnly)
                    currInFps = new Fraction(fpsInTbox.GetFloat());
            }
        }

        private void interpFactorCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUiFps();
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
            Program.mainForm.UpdateStepByStepControls();
        }

        string lastAiComboxStr = "";
        private void aiCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(aiCombox.Text) || aiCombox.Text == lastAiComboxStr) return;
            lastAiComboxStr = aiCombox.Text;
            UpdateAiModelCombox();
            
            if(initialized)
                ConfigParser.SaveComboxIndex(aiCombox);

            interpFactorCombox_SelectedIndexChanged(null, null);
        }

        public void UpdateAiModelCombox ()
        {
            aiModel = UIUtils.FillAiModelsCombox(aiModel, GetAi());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.Log("Closing main form.", true);
        }

        private void licenseBtn_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Path.Combine(Paths.GetPkgPath(), Paths.licensesDir));
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

                bool resume = (IOUtils.GetAmountOfFiles(Path.Combine(files[0], Paths.resumeDir), true) > 0);
                ResumeUtils.resumeNextRun = resume;

                if (resume)
                    ResumeUtils.LoadTempFolder(files[0]);

                trimCombox.SelectedIndex = 0;

                MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox);
            }
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
            if (InterpolationProgress.bigPreviewForm == null)
            {
                InterpolationProgress.bigPreviewForm = new BigPreviewForm();
                InterpolationProgress.bigPreviewForm.Show();
                InterpolationProgress.bigPreviewForm.SetImage(previewPicturebox.Image);
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

        public void UpdateStepByStepControls()
        {
            if(stepSelector.SelectedIndex < 0)
                stepSelector.SelectedIndex = 0;

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

        private void trimCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            QuickSettingsTab.trimEnabled = trimCombox.SelectedIndex > 0;
            trimPanel.Visible = QuickSettingsTab.trimEnabled;

            if (trimCombox.SelectedIndex == 1)
            {
                trimStartBox.Text = "00:00:00";
                trimEndBox.Text = FormatUtils.MsToTimestamp(currInDuration);
            }
        }

        private void trimResetBtn_Click(object sender, EventArgs e)
        {
            trimCombox_SelectedIndexChanged(null, null);
        }

        private void trimBox_TextChanged(object sender, EventArgs e)
        {
            QuickSettingsTab.UpdateTrim(trimStartBox, trimEndBox);
        }

        #region Quick Settings

        public void SaveQuickSettings (object sender, EventArgs e)
        {
            if (!quickSettingsInitialized) return;

            if (Program.busy)
                LoadQuickSettings();    // Discard any changes if busy

            ConfigParser.SaveGuiElement(maxVidHeight, ConfigParser.StringMode.Int);
            ConfigParser.SaveComboxIndex(dedupMode);
            ConfigParser.SaveComboxIndex(mpdecimateMode);
            ConfigParser.SaveGuiElement(dedupThresh);
            ConfigParser.SaveGuiElement(enableLoop);
            ConfigParser.SaveGuiElement(scnDetect);
            ConfigParser.SaveGuiElement(scnDetectValue);
        }

        public void LoadQuickSettings (object sender = null, EventArgs e = null)
        {
            ConfigParser.LoadGuiElement(maxVidHeight);
            ConfigParser.LoadComboxIndex(dedupMode);
            ConfigParser.LoadComboxIndex(mpdecimateMode);
            ConfigParser.LoadGuiElement(dedupThresh);
            ConfigParser.LoadGuiElement(enableLoop);
            ConfigParser.LoadGuiElement(scnDetect);
            ConfigParser.LoadGuiElement(scnDetectValue);

            quickSettingsInitialized = true;
        }

        private void dedupMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            dedupeSensLabel.Visible = dedupMode.SelectedIndex != 0;
            magickDedupePanel.Visible = dedupMode.SelectedIndex == 1;
            mpDedupePanel.Visible = dedupMode.SelectedIndex == 2;
            SaveQuickSettings(null, null);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Program.busy) return;
            new SettingsForm().ShowDialog();
        }

        #endregion

        private void scnDetectTestBtn_Click(object sender, EventArgs e)
        {
            Magick.SceneDetect.RunSceneDetection(inputTbox.Text.Trim());
        }
    }
}