using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.Os;
using Flowframes.Ui;
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
using System.Linq;
using System.Runtime.InteropServices;

#pragma warning disable IDE1006

namespace Flowframes
{
    public partial class Form1 : Form
    {
        [Flags]
        public enum EXECUTION_STATE : uint { ES_AWAYMODE_REQUIRED = 0x00000040, ES_CONTINUOUS = 0x80000000, ES_DISPLAY_REQUIRED = 0x00000002, ES_SYSTEM_REQUIRED = 0x00000001 }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags); // PREVENT WINDOWS FROM GOING TO SLEEP

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
            UiUtils.InitCombox(interpFactorCombox, 0);
            UiUtils.InitCombox(outSpeedCombox, 0);
            UiUtils.InitCombox(outModeCombox, 0);
            UiUtils.InitCombox(aiModel, 2);
            // Video Utils
            UiUtils.InitCombox(trimCombox, 0);

            Program.mainForm = this;
            Logger.textbox = logBox;
            NvApi.Init();
            InitAis();
            InterpolationProgress.preview = previewPicturebox;
            RemovePreviewIfDisabled();
            UpdateStepByStepControls();
            Initialized();
            HandleArgs();
            Text = $"Flowframes";

            if (Program.args.Contains("show-model-downloader"))
                new ModelDownloadForm().ShowDialog();
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Logger.Log("Debugger is attached - Flowframes seems to be running within VS.");
                scnDetectTestBtn.Visible = true;
            }

            completionAction.SelectedIndex = 0;
            await Checks();
        }

        async Task Checks()
        {
            try
            {
                Task.Run(() => Updater.UpdateModelList());
                Task.Run(() => Updater.AsyncUpdateCheck());
                Task.Run(() => GetWebInfo.LoadNews(newsLabel));
                Task.Run(() => GetWebInfo.LoadPatronListCsv(patronsLabel));
                Task.Run(() => Servers.Init());
                await Python.CheckCompression();
                await StartupChecks.SymlinksCheck();
            }
            catch (Exception e)
            {
                Logger.Log("Non-critical error while performing online checks. See logs for details.");
                Logger.Log($"{e.Message}\n{e.StackTrace}", true);
            }
        }

        void HandleArgs()
        {
            foreach (string arg in Program.args)
            {
                if (arg.StartsWith("out="))
                    outputTbox.Text = arg.Split('=').Last().Trim();

                if (arg.StartsWith("factor="))
                {
                    float factor = arg.Split('=').Last().GetFloat();
                    interpFactorCombox.Text = factor.ToString();
                }

                if (arg.StartsWith("ai="))
                    aiCombox.SelectedIndex = arg.Split('=').Last().GetInt();

                if (arg.StartsWith("model="))
                    aiModel.SelectedIndex = arg.Split('=').Last().GetInt();

                if (arg.StartsWith("output-mode="))
                    outModeCombox.SelectedIndex = arg.Split('=').Last().GetInt();
            }

            if (Program.fileArgs.Length > 0)
                DragDropHandler(Program.fileArgs.Where(x => IoUtils.IsFileValid(x)).ToArray());

        }

        void RemovePreviewIfDisabled()
        {
            if (!Config.GetBool(Config.Key.disablePreview))
                return;

            foreach (TabPage tab in mainTabControl.TabPages)
            {
                if (tab.Text.Trim() == "Preview")
                    mainTabControl.TabPages.Remove(tab);
            }
        }

        public HTTabControl GetMainTabControl() { return mainTabControl; }
        public TextBox GetInputFpsTextbox() { return fpsInTbox; }
        public Button GetPauseBtn() { return pauseBtn; }

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
            return new InterpSettings(inputTbox.Text.Trim(), outputTbox.Text.Trim(), GetAi(), currInFpsDetected, currInFps,
                interpFactorCombox.GetFloat(), outSpeedCombox.GetInt().Clamp(1, 64), GetOutMode(), GetModel(GetAi()));
        }

        public InterpSettings UpdateCurrentSettings(InterpSettings settings)
        {
            SetTab("interpolate");
            string inPath = inputTbox.Text.Trim();

            if (settings.inPath != inPath)     // If path changed, get new instance
            {
                Logger.Log($"settings.inPath ({settings.inPath}) mismatches GUI inPath ({settings.inPath} - Returning fresh instance", true);
                return GetCurrentSettings();
            }

            settings.inPath = inPath;
            settings.ai = GetAi();
            settings.inFpsDetected = currInFpsDetected;
            settings.inFps = currInFps;
            settings.interpFactor = interpFactorCombox.GetFloat();
            settings.outFps = settings.inFps * settings.interpFactor;
            settings.outMode = GetOutMode();
            settings.model = GetModel(GetAi());

            return settings;
        }

        public void LoadBatchEntry(InterpSettings entry)
        {
            inputTbox.Text = entry.inPath;
            MainUiFunctions.SetOutPath(outputTbox, entry.outPath);
            interpFactorCombox.Text = entry.interpFactor.ToString();
            aiCombox.SelectedIndex = Implementations.networks.IndexOf(Implementations.networks.Where(x => x.aiName == entry.ai.aiName).FirstOrDefault());
            SetOutMode(entry.outMode);
        }

        public void SetStatus(string str)
        {
            Logger.Log(str, true);
            statusLabel.Text = str;
        }

        public string GetStatus()
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

        public void UpdateInputInfo()
        {
            string str = $"Size: {(!currInRes.IsEmpty ? $"{currInRes.Width}x{currInRes.Height}" : "Unknown")} - ";
            str += $"FPS: {(currInFpsDetected.GetFloat() > 0f ? $"{currInFpsDetected} ({currInFpsDetected.GetFloat().ToString("0.000")})" : "Unknown")} - ";
            str += $"Frames: {(currInFrames > 0 ? $"{currInFrames}" : "Unknown")} - ";
            str += $"Duration: {(currInDuration > 0 ? FormatUtils.MsToTimestamp(currInDuration) : "Unknown")}";
            inputInfo.Text = str;
        }

        public void InterpolationDone()
        {
            SetStatus("Done interpolating!");

            if (!BatchProcessing.busy)
                CompletionAction();
        }

        public void CompletionAction()
        {
            if (Program.args.Contains("quit-when-done"))
                Application.Exit();

            if (completionAction.SelectedIndex == 1)
                new TimeoutForm(completionAction.Text, Application.Exit).ShowDialog();

            if (completionAction.SelectedIndex == 2)
                new TimeoutForm(completionAction.Text, OsUtils.Sleep).ShowDialog();

            if (completionAction.SelectedIndex == 3)
                new TimeoutForm(completionAction.Text, OsUtils.Hibernate).ShowDialog();

            if (completionAction.SelectedIndex == 4)
                new TimeoutForm(completionAction.Text, OsUtils.Shutdown).ShowDialog();
        }

        public void ResetInputInfo()
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
            bool pytorchAvailable = Python.HasEmbeddedPyFolder() || Python.IsSysPyInstalled();

            foreach (AI ai in Implementations.networks)
            {
                if (ai.backend == AI.Backend.Pytorch && !pytorchAvailable)
                {
                    Logger.Log($"AI implementation {ai.friendlyName} ({ai.backend}) has not been loaded because Pytorch was not found.", true);
                    continue;
                }
                    

                aiCombox.Items.Add(ai.friendlyName + " - " + ai.description);
            }

            string lastUsedAiName = Config.Get(Config.Key.lastUsedAiName);
            aiCombox.SelectedIndex = Implementations.networks.IndexOf(Implementations.networks.Where(x => x.aiName == lastUsedAiName).FirstOrDefault());
            if (aiCombox.SelectedIndex < 0) aiCombox.SelectedIndex = 0;
            Config.Set(Config.Key.lastUsedAiName, GetAi().aiName);

            ConfigParser.LoadComboxIndex(outModeCombox);
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
            ValidateFactor();

            if (!BatchProcessing.busy)      // Don't load values from gui if batch processing is used
                Interpolate.current = GetCurrentSettings();

            AiProcessSuspend.Reset();
            Interpolate.Start();
        }

        public ModelCollection.ModelInfo GetModel(AI currentAi)
        {
            try
            {
                return AiModels.GetModels(currentAi).models[aiModel.SelectedIndex];
            }
            catch
            {
                return null;
            }
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
            int targetIndex = 0;

            for (int i = 0; i < outModeCombox.Items.Count; i++)
            {
                string currentItem = outModeCombox.Items[i].ToString().ToLower();
                if (mode == Interpolate.OutMode.VidMkv && currentItem.Contains("mkv")) targetIndex = i;
                if (mode == Interpolate.OutMode.VidWebm && currentItem.Contains("webm")) targetIndex = i;
                if (mode == Interpolate.OutMode.VidProRes && currentItem.Contains("prores")) targetIndex = i;
                if (mode == Interpolate.OutMode.VidAvi && currentItem.Contains("avi")) targetIndex = i;
                if (mode == Interpolate.OutMode.VidGif && currentItem.Contains("gif")) targetIndex = i;
                if (mode == Interpolate.OutMode.ImgPng && currentItem.Contains("image")) targetIndex = i;
            }

            outModeCombox.SelectedIndex = targetIndex;
        }

        public AI GetAi()
        {
            return Implementations.networks[aiCombox.SelectedIndex];
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
                fpsOutTbox.Text = (frac * interpFactorCombox.GetFloat()).ToString() + " FPS";

                if (!fpsInTbox.ReadOnly)
                    currInFps = frac;
            }
            else    // Parse float
            {
                fpsInTbox.Text = fpsInTbox.Text.TrimNumbers(true);
                fpsOutTbox.Text = (fpsInTbox.GetFloat() * interpFactorCombox.GetFloat()).ToString() + " FPS";

                if (!fpsInTbox.ReadOnly)
                    currInFps = new Fraction(fpsInTbox.GetFloat());
            }
        }

        private void interpFactorCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUiFps();
            //int guiFactor = interpFactorCombox.GetInt();
            //
            //if (!initialized)
            //    return;
            //
            //string aiName = GetAi().aiName.Replace("_", "-");
        }

        public void ValidateFactor()
        {
            interpFactorCombox.Text = $"x{MainUiFunctions.ValidateInterpFactor(interpFactorCombox.GetFloat())}";
        }

        public void SetWorking(bool state, bool allowCancel = true)
        {
            Logger.Log($"SetWorking({state})", true);
            SetProgress(-1);
            Control[] controlsToDisable = new Control[] { runBtn, runStepBtn, stepSelector, settingsBtn };
            Control[] controlsToHide = new Control[] { runBtn, runStepBtn, stepSelector };
            progressCircle.Visible = state;
            busyControlsPanel.Visible = state;

            foreach (Control c in controlsToDisable)
                c.Enabled = !state;

            foreach (Control c in controlsToHide)
                c.Visible = !state;

            busyControlsPanel.Enabled = allowCancel;
            Program.busy = state;
            Program.mainForm.UpdateStepByStepControls();
        }

        string lastAiComboxStr = "";
        private void aiCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(aiCombox.Text) || aiCombox.Text == lastAiComboxStr) return;
            lastAiComboxStr = aiCombox.Text;
            UpdateAiModelCombox();

            interpFactorCombox.Items.Clear();

            foreach (int factor in GetAi().supportedFactors)
                interpFactorCombox.Items.Add($"x{factor}");

            interpFactorCombox.SelectedIndex = 0;

            if (initialized)
                Config.Set(Config.Key.lastUsedAiName, GetAi().aiName);

            interpFactorCombox_SelectedIndexChanged(null, null);
            fpsOutTbox.ReadOnly = GetAi().factorSupport != AI.FactorSupport.AnyFloat;
        }

        public void UpdateAiModelCombox()
        {
            aiModel = UiUtils.LoadAiModelsIntoGui(aiModel, GetAi());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Program.busy && !BackgroundTaskManager.IsBusy())
                return;

            string reason = "";

            if (BackgroundTaskManager.IsBusy())
                reason = "Some background tasks have not finished yet.";

            if (Program.busy)
                reason = "The program is still busy.";

            DialogResult dialog = MessageBox.Show($"Are you sure you want to exit the program?\n\n{reason}", "Are you sure?", MessageBoxButtons.YesNo);

            if (dialog == DialogResult.No)
                e.Cancel = true;
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

            bool start = Program.initialRun && Program.args.Contains("start");

            if (files.Length > 1)
            {
                queueBtn_Click(null, null);
                if (BatchProcessing.currentBatchForm != null)
                    BatchProcessing.currentBatchForm.LoadDroppedPaths(files, start);
            }
            else
            {
                SetTab("interpolation");
                Logger.Log("Selected video/directory: " + Path.GetFileName(files[0]), true);
                inputTbox.Text = files[0];

                bool resume = (IoUtils.GetAmountOfFiles(Path.Combine(files[0], Paths.resumeDir), true, "*.json") > 0);
                AutoEncodeResume.resumeNextRun = resume;

                if (resume)
                    AutoEncodeResume.LoadTempFolder(files[0]);

                trimCombox.SelectedIndex = 0;

                MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox, start);
            }
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult dialog = MessageBox.Show($"Are you sure you want to cancel the interpolation?", "Are you sure?", MessageBoxButtons.YesNo);

            if (dialog == DialogResult.Yes)
            {
                SetTab("interpolation");
                Interpolate.Cancel();
            }
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
            ValidateFactor();

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
            if (stepSelector.SelectedIndex < 0)
                stepSelector.SelectedIndex = 0;

            bool stepByStep = Config.GetInt(Config.Key.processingMode) == 1;
            runBtn.Visible = !stepByStep && !Program.busy;
        }

        private async void runStepBtn_Click(object sender, EventArgs e)
        {
            ValidateFactor();
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

        public void SaveQuickSettings(object sender, EventArgs e)
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
            ConfigParser.SaveGuiElement(maxFps);
        }

        public void LoadQuickSettings(object sender = null, EventArgs e = null)
        {
            ConfigParser.LoadGuiElement(maxVidHeight);
            ConfigParser.LoadComboxIndex(dedupMode);
            ConfigParser.LoadComboxIndex(mpdecimateMode);
            ConfigParser.LoadGuiElement(dedupThresh);
            ConfigParser.LoadGuiElement(enableLoop);
            ConfigParser.LoadGuiElement(scnDetect);
            ConfigParser.LoadGuiElement(scnDetectValue);
            ConfigParser.LoadGuiElement(maxFps);

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

        private void pauseBtn_Click(object sender, EventArgs e)
        {
            AiProcessSuspend.SuspendResumeAi(!AiProcessSuspend.aiProcFrozen);
        }

        private void debugBtn_Click(object sender, EventArgs e)
        {
            new DebugForm().ShowDialog();
        }

        private void encodingSettingsBtn_Click(object sender, EventArgs e)
        {
            new SettingsForm(4).ShowDialog();
        }

        private void outModeCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialized)
                ConfigParser.SaveComboxIndex(outModeCombox);
        }

        private void fpsOutTbox_Leave(object sender, EventArgs e)
        {
            float inFps = fpsInTbox.GetFloat();
            float outFps = fpsOutTbox.GetFloat();
            var targetFactorRounded = Math.Round((Decimal)(outFps / inFps), 3, MidpointRounding.AwayFromZero);
            interpFactorCombox.Text = $"{targetFactorRounded}";
            ValidateFactor();
            fpsOutTbox.Text = $"{inFps * interpFactorCombox.GetFloat()} FPS";
        }
    }
}