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
using Flowframes.Media;

#pragma warning disable IDE1006

namespace Flowframes.Forms.Main
{
    public partial class Form1 : Form
    {
        [Flags]
        public enum EXECUTION_STATE : uint { ES_AWAYMODE_REQUIRED = 0x00000040, ES_CONTINUOUS = 0x80000000, ES_DISPLAY_REQUIRED = 0x00000002, ES_SYSTEM_REQUIRED = 0x00000001 }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags); // PREVENT WINDOWS FROM GOING TO SLEEP

        private bool _initialized = false;
        private bool _mainTabInitialized = false;
        private bool _quickSettingsInitialized = false;

        public bool ShowModelDownloader = false;

        private Enums.VfrMode prevVfrMode = (Enums.VfrMode)(-1);
        private ControlTextResizer _ctrlTextResizer;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (Program.CmdMode)
                {
                    const int WS_EX_TOOLWINDOW = 0x80;
                    cp.ExStyle |= WS_EX_TOOLWINDOW; // Makes the window not appear in Alt-Tab
                }
                return cp;
            }
        }

        public Form1()
        {
            InitializeComponent();

            if (Program.CmdMode)
            {
                ShowInTaskbar = false;
                Opacity = 0;
                return;
            }

            _ctrlTextResizer = new ControlTextResizer();
            var resizableTextCtrls = new List<Control>() { logBox, newsLabel, patronsLabel };
            _ctrlTextResizer.Register(resizableTextCtrls);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = Debugger.IsAttached;
            AutoScaleMode = AutoScaleMode.None;
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            Program.mainForm = this;
            Refresh();
            await Task.Delay(1);

            StartupChecks.CheckOs();
            Program.Cleanup();

            // Main Tab
            UiUtils.InitCombox(interpFactorCombox, 0);
            UiUtils.InitCombox(outSpeedCombox, 0);
            // Quick Settings
            UiUtils.InitCombox(trimCombox, 0);
            mpdecimateMode.FillFromEnum<Enums.Interpolation.MpDecimateSens>(useKeyNames: true);
            vfrHandling.FillFromEnum<Enums.VfrMode>(stringMap: Strings.VfrMode);
            vfrHandling.SelectedIndexChanged += (s, ev) => VfrModeChange();

            Logger.textbox = logBox;
            VulkanUtils.Init();
            NvApi.Init();
            await Checks();
            InitOutputUi();
            InitAis();
            UpdateStepByStepControls();
            HandleArgs();
            Initialized();

            if (Debugger.IsAttached)
            {
                Logger.Log("Debugger is attached.");
            }

            completionAction.SelectedIndex = 0;
        }

        private void VfrModeChange()
        {
            var oldMode = (Enums.VfrMode)Config.GetInt(Config.Key.vfrHandling);
            var newMode = ParseUtils.GetEnum<Enums.VfrMode>(vfrHandling.Text, stringMap: Strings.VfrMode);

            if (newMode != oldMode)
            {
                SaveQuickSettings();
                HandleInputFiles(new[] { inputTbox.Text });
            }
        }

        private void InitOutputUi()
        {
            comboxOutputFormat.FillFromEnum<Enums.Output.Format>(Strings.OutputFormat, 0);
            UpdateOutputUi();
        }

        public async void ResetOutputUi()
        {
            comboxOutputEncoder.Items.Clear();
            Config.Set(Config.Key.PerformedHwEncCheck, false.ToString());
            await HwEncCheck.DetectHwEncoders();
            UpdateOutputUi();
        }

        private void UpdateOutputUi()
        {
            var outMode = ParseUtils.GetEnum<Enums.Output.Format>(comboxOutputFormat.Text, true, Strings.OutputFormat);
            comboxOutputEncoder.FillFromEnum(OutputUtils.GetAvailableEncoders(outMode), Strings.Encoder, 0);
            comboxOutputEncoder.Visible = comboxOutputEncoder.Items.Count > 0;
            UpdateOutputEncodingUi();
        }

        private void UpdateOutputEncodingUi()
        {
            var infoStrings = new List<string>() { "Format" };
            var encoder = ParseUtils.GetEnum<Enums.Encoding.Encoder>(comboxOutputEncoder.Text, true, Strings.Encoder);
            bool hasEncoder = (int)encoder != -1;

            comboxOutputQuality.Visible = hasEncoder;
            comboxOutputColors.Visible = hasEncoder;

            if (hasEncoder)
            {
                infoStrings.Add("Codec");
                EncoderInfoVideo info = OutputUtils.GetEncoderInfoVideo(encoder);

                bool adjustableQuality = !(info.Lossless == true) && info.QualityLevels != null && info.QualityLevels.Count > 0;
                comboxOutputQuality.Visible = adjustableQuality;
                comboxOutputQuality.Items.Clear();

                if (info.QualityLevels.Count > 0)
                {
                    infoStrings.Add("Quality");
                    var exclude = info.Lossless == null ? new List<string> { Enums.Encoding.Quality.Common.Lossless.ToString() } : null;
                    comboxOutputQuality.FillFromStrings(info.QualityLevels, Strings.VideoQuality, info.QualityDefault, exclude);
                }

                var pixelFormats = info.PixelFormats;
                comboxOutputColors.Visible = pixelFormats.Count > 0;
                int defaultPixFmt = (int)info.PixelFormatDefault != -1 ? info.PixelFormats.IndexOf(info.PixelFormatDefault) : 0;
                comboxOutputColors.FillFromEnum(pixelFormats, Strings.PixelFormat, defaultPixFmt);
                comboxOutputColors.Width = adjustableQuality ? 117 : 223;

                if (pixelFormats.Count > 0)
                    infoStrings.Add("Pixel Format");
            }

            labelOutput.Text = $"Set {string.Join(", ", infoStrings)}";
        }

        private async Task Checks()
        {
            try
            {
                // Task.Run(() => Updater.UpdateModelList());
                StartupChecks.GpuCheck();
                Task.Run(() => Updater.AsyncUpdateCheck());
                Task.Run(() => GetWebInfo.LoadNews(newsLabel));
                Task.Run(() => GetWebInfo.LoadPatronListCsv(patronsLabel));
                await Servers.InitAsync();
                await StartupChecks.SymlinksCheck();
                await HwEncCheck.DetectHwEncoders();
                await Python.CheckCompression();
            }
            catch (Exception e)
            {
                Logger.Log($"Checks Error: {e.Message}\n{e.StackTrace}", true);
            }
        }

        private void HandleArgs()
        {
            // Input & interpolation settings

            if (Cli.ValidFiles.Any())
            {
                Logger.Log($"[CLI] Loading file(s): {string.Join(", ", Cli.ValidFiles)}", true);
                HandleInputFiles(Cli.ValidFiles.ToArray());
            }

            if (Cli.InterpAi != (Implementations.Ai)(-1))
            {
                string name = Implementations.GetAi(Cli.InterpAi).NameInternal;
                aiCombox.SelectedIndex = Implementations.NetworksAvailable.IndexOf(Implementations.NetworksAvailable.Where(ai => ai.NameInternal == name).FirstOrDefault());
                Logger.Log($"[CLI] Using AI implementation '{aiCombox.Text}' ('{Cli.InterpAi}')", true);

            }

            if (Cli.InterpModel.IsNotEmpty())
            {
                aiModel.SelectedIndex = aiModel.Items.Cast<string>().Select((item, index) => new { item, index }).FirstOrDefault(x => x.item.Contains(Cli.InterpModel))?.index ?? -1;
                Logger.Log($"[CLI] Using interpolation model '{aiModel.Text}'", true);
            }

            if (Cli.InterpFactor > 0)
            {
                interpFactorCombox.Text = Cli.InterpFactor.ToString();
                ValidateFactor();
                Logger.Log($"[CLI] Using interpolation factor {interpFactorCombox.Text}", true);
            }

            // Output

            if (Cli.OutputDir.IsNotEmpty())
            {
                outputTbox.Text = Cli.OutputDir;
                Directory.CreateDirectory(outputTbox.Text);
                Logger.Log($"[CLI] Using output directory '{outputTbox.Text}'", true);
            }

            if (Cli.OutputFormat != (Enums.Output.Format)(-1))
            {
                SetFormat(Cli.OutputFormat);
                Logger.Log($"[CLI] Using output format '{comboxOutputFormat.Text}'", true);
            }

            if (Cli.Encoder != (Enums.Encoding.Encoder)(-1))
            {
                comboxOutputEncoder.SelectedIndex = comboxOutputEncoder.Items.Cast<string>().Select((item, index) => new { item, index })
                    .FirstOrDefault(x => x.item == Strings.Encoder[Cli.Encoder.ToString()])?.index ?? -1;
                Logger.Log($"[CLI] Using video encoder {comboxOutputEncoder.Text} ('{Cli.Encoder}')", true);
            }

            if (Cli.PixFmt != (Enums.Encoding.PixelFormat)(-1))
            {
                comboxOutputColors.SelectedIndex = comboxOutputColors.Items.Cast<string>().Select((item, index) => new { item, index })
                    .FirstOrDefault(x => x.item == Strings.PixelFormat[Cli.PixFmt.ToString()])?.index ?? -1;
                Logger.Log($"[CLI] Using color format {comboxOutputColors.Text} ('{Cli.PixFmt}')", true);
            }

            // Video processing settings

            if (Cli.MaxHeight >= 64 && Cli.MaxHeight <= 16384)
            {
                Logger.Log($"[CLI] Set max video height to {Cli.MaxHeight} px", true);
                Config.Set(Config.Key.maxVidHeight, Cli.MaxHeight.ToString());
            }

            if (Cli.Loop != null)
            {
                Logger.Log($"[CLI] Set loop mode to {(Cli.Loop == true ? "Enabled" : "Disabled")}", true);
                Config.Set(Config.Key.enableLoop, ((bool)Cli.Loop).ToString());
            }

            if (Cli.FixSceneChanges != null)
            {
                Logger.Log($"[CLI] Set scene change fix mode to {(Cli.FixSceneChanges == true ? "Enabled" : "Disabled")}", true);
                Config.Set(Config.Key.scnDetect, ((bool)Cli.FixSceneChanges).ToString());
            }

            if (Cli.FixSceneChangeVal > 0f)
            {
                Logger.Log($"[CLI] Set scene change sensitivity value to {Cli.FixSceneChangeVal}", true);
                Config.Set(Config.Key.scnDetectValue, Cli.FixSceneChangeVal.ToString());
            }

            if (Cli.MaxOutFps >= 1f)
            {
                Logger.Log($"[CLI] Set max output (encoding) FPS to {Cli.MaxOutFps}", true);
                Config.Set(Config.Key.maxFps, Cli.MaxOutFps.ToString());
            }
        }

        public HTTabControl GetMainTabControl() { return mainTabControl; }
        public TextBox GetInputFpsTextbox() { return fpsInTbox; }
        public Button GetPauseBtn() { return pauseBtn; }

        public bool IsInFocus() { return ActiveForm == this; }

        public void SetTab(string tabName)
        {
            var targetTab = mainTabControl.TabPages.Cast<TabPage>().Where(p => p.Name == tabName).FirstOrDefault();

            if (targetTab == null)
                return;

            mainTabControl.SelectedTab = targetTab;
            mainTabControl.Refresh();
            mainTabControl.Update();
        }

        public InterpSettings GetCurrentSettings(string path = "")
        {
            SetTab(interpOptsTab.Name);
            AiInfo ai = GetAi();

            var s = new InterpSettings()
            {
                inPath = path.IsNotEmpty() ? path : inputTbox.Text.Trim(),
                outPath = outputTbox.Text.Trim(),
                ai = ai,
                dedupe = Config.GetInt(Config.Key.dedupMode) != 0,
                inFpsDetected = currInFpsDetected,
                inFps = currInFps,
                interpFactor = interpFactorCombox.GetFloat(),
                outItsScale = outSpeedCombox.GetInt().Clamp(1, 64),
                outSettings = GetOutputSettings(),
                model = GetModel(ai),
            };

            s.InitArgs();
            return s;
        }

        public void LoadBatchEntry(InterpSettings entry)
        {
            inputTbox.Text = entry.inPath;
            MainUiFunctions.SetOutPath(outputTbox, entry.outPath);
            fpsInTbox.Text = entry.inFps.ToString();
            interpFactorCombox.Text = entry.interpFactor.ToString();
            aiCombox.SelectedIndex = Implementations.NetworksAvailable.IndexOf(Implementations.NetworksAvailable.Where(x => x.NameInternal == entry.ai.NameInternal).FirstOrDefault());
            SetFormat(entry.outSettings.Format);
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
            str += $"FPS: {(currInFpsDetected.Float > 0f ? FormatUtils.Fraction(currInFpsDetected) : "Unknown")} - ";
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
            if (Cli.AutoRun)
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

        private void InitAis()
        {
            bool pytorchAvailable = Python.IsPytorchReady();

            foreach (AiInfo ai in Implementations.NetworksAvailable)
                aiCombox.Items.Add(GetAiComboboxName(ai));

            string lastUsedAiName = Config.Get(Config.Key.lastUsedAiName);
            aiCombox.SelectedIndex = Implementations.NetworksAvailable.IndexOf(Implementations.NetworksAvailable.Where(x => x.NameInternal == lastUsedAiName).FirstOrDefault());
            if (aiCombox.SelectedIndex < 0) aiCombox.SelectedIndex = 0;
            Config.Set(Config.Key.lastUsedAiName, GetAi().NameInternal);
        }

        private string GetAiComboboxName(AiInfo ai)
        {
            return ai.FriendlyName + " - " + ai.Description;
        }

        private void InitializeMainTab()
        {
            if (_mainTabInitialized)
                return;

            LoadOutputSettings();
            _mainTabInitialized = true;
        }

        public static void CloseAllSplashForms ()
        {
            Application.OpenForms.OfType<SplashForm>().ToList().ForEach(f => f.Close());
        }

        public void Initialized()
        {
            CloseAllSplashForms();

            _initialized = true;
            runBtn.Enabled = true;
            SetStatus("Ready");

            if (ShowModelDownloader)
            {
                new ModelDownloadForm().ShowDialog();
            }

            if (!Program.CmdMode)
                Opacity = 1.0f;
        }

        private void browseInputBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = inputTbox.Text.Trim(), IsFolderPicker = true };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                HandleInputFiles(new string[] { dialog.FileName });
        }

        private void browseInputFileBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = inputTbox.Text.Trim(), IsFolderPicker = false };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                HandleInputFiles(new string[] { dialog.FileName });
        }

        private void browseOutBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = outputTbox.Text.Trim(), IsFolderPicker = true };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                outputTbox.Text = dialog.FileName;
        }

        public async void runBtn_Click(object sender, EventArgs e)
        {
            if (Interpolate.currentMediaFile == null || !Interpolate.currentMediaFile.Initialized)
            {
                SetTab(interpOptsTab.Name);
                return;
            }

            ValidateFactor();

            if (!BatchProcessing.busy)      // Don't load values from GUI if batch processing is used
                Interpolate.currentSettings = GetCurrentSettings();

            AiProcessSuspend.Reset();
            SaveOutputSettings();

            if (Interpolate.currentSettings.outSettings.Format == Enums.Output.Format.Realtime)
            {
                await Interpolate.Realtime();
                SetProgress(0);
            }
            else
            {
                await Interpolate.Start();
            }
        }

        private void SaveOutputSettings()
        {
            var strings = new List<string>();
            if (comboxOutputFormat.Visible) strings.Add(comboxOutputFormat.Text);
            if (comboxOutputEncoder.Visible) strings.Add(comboxOutputEncoder.Text);
            if (comboxOutputQuality.Visible) strings.Add(comboxOutputQuality.Text);
            if (comboxOutputColors.Visible) strings.Add(comboxOutputColors.Text);
            Config.Set(Config.Key.lastOutputSettings, string.Join(",", strings));
        }

        private void LoadOutputSettings()
        {
            string[] strings = Config.Get(Config.Key.lastOutputSettings).Split(',');

            if (strings.Length < 4)
                return;

            if (comboxOutputFormat.Visible) comboxOutputFormat.Text = strings[0];
            if (comboxOutputEncoder.Visible) comboxOutputEncoder.Text = strings[1];
            if (comboxOutputQuality.Visible) comboxOutputQuality.Text = strings[2];
            if (comboxOutputColors.Visible) comboxOutputColors.Text = strings[3];
        }

        public void SetFormat(Enums.Output.Format format)
        {
            comboxOutputFormat.Text = Strings.OutputFormat.Get(format.ToString());
        }

        void inputTbox_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void inputTbox_DragDrop(object sender, DragEventArgs e)
        {
            HandleInputFiles((string[])e.Data.GetData(DataFormats.FileDrop));
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

            foreach (int factor in GetAi().SupportedFactors)
                interpFactorCombox.Items.Add($"x{factor}");

            interpFactorCombox.SelectedIndex = 0;

            if (_initialized)
                Config.Set(Config.Key.lastUsedAiName, GetAi().NameInternal);

            interpFactorCombox_SelectedIndexChanged(null, null);
            fpsOutTbox.ReadOnly = GetAi().FactorSupport != AiInfo.InterpFactorSupport.AnyFloat;
        }

        public void UpdateAiModelCombox()
        {
            aiModel = UiUtils.LoadAiModelsIntoGui(aiModel, GetAi());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Program.busy)
                return;

            string reason = "";

            if (Program.busy)
                reason = "The program is still busy.";

            DialogResult dialog = UiUtils.ShowMessageBox($"Are you sure you want to exit the program?\n\n{reason}", "Are you sure?", MessageBoxButtons.YesNo);

            if (dialog == DialogResult.No)
                e.Cancel = true;

            Program.Cleanup();
        }

        private void licenseBtn_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Path.Combine(Paths.GetPkgPath(), Paths.licensesDir));
        }

        private void Form1_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private async void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            await Task.Delay(1); // Release drop
            HandleInputFiles(files);
        }

        public void HandleInputFiles(string[] files)
        {
            if (Program.busy) return;

            bool start = Program.initialRun && Cli.AutoRun;

            if (files.Length > 1)
            {
                SetTab(interpOptsTab.Name);
                queueBtn_Click(null, null);
                BatchProcessing.currentBatchForm?.LoadDroppedPaths(files, start);
            }
            else
            {
                SetTab(interpOptsTab.Name);
                Logger.Log($"Selected video/directory: {Path.GetFileName(files[0])}", true);
                inputTbox.Text = files[0];

                bool resume = Directory.Exists(files[0]) && IoUtils.GetAmountOfFiles(Path.Combine(files[0], Paths.resumeDir), true, "*.json") > 0;
                AutoEncodeResume.resumeNextRun = resume;

                if (resume)
                    AutoEncodeResume.LoadTempFolder(files[0]);

                trimCombox.SelectedIndex = 0;

                MainUiFunctions.InitInput(outputTbox, inputTbox, fpsInTbox, start);
            }
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult dialog = UiUtils.ShowMessageBox($"Are you sure you want to cancel the interpolation?", "Are you sure?", MessageBoxButtons.YesNo);

            if (dialog == DialogResult.Yes)
            {
                SetTab(interpOptsTab.Name);
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

        private async void updateBtn_Click(object sender, EventArgs e)
        {
            new UpdaterForm().ShowDialog();
        }

        private void welcomeLabel2_Click(object sender, EventArgs e)
        {
            SetTab(interpOptsTab.Name);
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
            SetTab(interpOptsTab.Name);
            await InterpolateSteps.Run(stepSelector.Text);
        }

        private void mainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_initialized) return;

            if (mainTabControl.SelectedTab == interpOptsTab)
            {
                aiCombox_SelectedIndexChanged(null, null);
                InitializeMainTab();
            }
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

        public void SaveQuickSettings(object sender = null, EventArgs e = null)
        {
            if (!_quickSettingsInitialized) return;

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
            ConfigParser.SaveComboxIndex(vfrHandling);
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
            ConfigParser.LoadComboxIndex(vfrHandling);

            _quickSettingsInitialized = true;
        }

        private void dedupMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            dedupeSensLabel.Visible = dedupMode.SelectedIndex != 0;
            mpDedupePanel.Visible = dedupMode.SelectedIndex == 1;
            magickDedupePanel.Visible = dedupMode.SelectedIndex == 2; // Never true atm
            SaveQuickSettings(null, null);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Program.busy) return;
            new SettingsForm().ShowDialog();
        }

        #endregion

        private void pauseBtn_Click(object sender, EventArgs e)
        {
            AiProcessSuspend.SuspendResumeAi(!AiProcessSuspend.aiProcFrozen);
        }

        private void debugBtn_Click(object sender, EventArgs e)
        {
            new DebugForm().ShowDialog();
        }

        private void fpsOutTbox_Leave(object sender, EventArgs e)
        {
            float inFps = fpsInTbox.GetFloat();
            float outFps = fpsOutTbox.GetFloat();

            if (inFps == 0 || outFps == 0)
                return;

            var targetFactorRounded = Math.Round((Decimal)(outFps / inFps), 3, MidpointRounding.AwayFromZero);
            interpFactorCombox.Text = $"{targetFactorRounded}";
            ValidateFactor();
            fpsOutTbox.Text = $"{inFps * interpFactorCombox.GetFloat()} FPS";
        }

        private void aiInfoBtn_Click(object sender, EventArgs e)
        {
            var ai = GetAi();

            if (ai != null)
                UiUtils.ShowMessageBox(ai.GetVerboseInfo(), UiUtils.MessageType.Message);
        }

        private void comboxOutputFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateOutputUi();
        }

        private void comboxOutputEncoder_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateOutputEncodingUi();
        }

        private void queueBtn_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                menuStripQueue.Show(Cursor.Position);
        }

        private void addCurrentConfigurationToQueueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.batchQueue.Enqueue(Program.mainForm.GetCurrentSettings());
            Application.OpenForms.Cast<Form>().Where(f => f is BatchForm).Select(f => (BatchForm)f).ToList().ForEach(f => f.RefreshGui());
        }

        private void comboxOutputQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            var qualityPreset = ParseUtils.GetEnum<Enums.Encoding.Quality.Common>(comboxOutputQuality.Text, true, Strings.VideoQuality);
            bool cust = qualityPreset == Enums.Encoding.Quality.Common.Custom;
            textboxOutputQualityCust.Visible = cust;
            comboxOutputQuality.Margin = new System.Windows.Forms.Padding(0, 0, cust ? 0 : 6, 0);
            comboxOutputQuality.Width = cust ? 70 : 100;

            if (!cust)
                textboxOutputQualityCust.Text = "";
            else
                textboxOutputQualityCust.Focus();

            Refresh();
        }
    }
}