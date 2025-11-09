using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Os;
using Flowframes.Ui;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable IDE1006

namespace Flowframes.Forms
{
    public partial class SettingsForm : CustomForm
    {
        private bool _initialized = false;
        private AiInfo _currentAi = null;

        public SettingsForm(int tabIndex = 0, AiInfo currentAi = null)
        {
            AutoScaleMode = AutoScaleMode.None;
            InitializeComponent();

            _currentAi = currentAi == null ? Program.mainForm.GetAi() : currentAi;
            settingsTabList.SelectedIndex = tabIndex;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            MinimumSize = new Size(Width, Height);
            MaximumSize = new Size(Width, (Height * 1.5f).RoundToInt());
            mpdecimateMode.FillFromEnum<Enums.Interpolation.MpDecimateSens>(useKeyNames: true);
            onlyShowRelevantSettings.Click += (s, ea) => SetVisibility();

            InitGpus();
            InitServers();
            LoadSettings();
            AddTooltipClickFunction();
            _initialized = true;
            Task.Run(() => CheckModelCacheSize());
        }

        private void InitGpus()
        {
            string tooltipTorch = "";
            string tooltipNcnn = "";

            for (int i = 0; i < NvApi.NvGpus.Count; i++)
            {
                torchGpus.Items.Add(i);
                tooltipTorch += $"{i} = {NvApi.NvGpus[i].FullName} ({NvApi.NvGpus[i].GetVramGb().ToString("0.")} GB)\n";
            }

            ncnnGpus.Items.Add(-1);
            tooltipNcnn += $"-1 = CPU\n";

            foreach (var vkGpu in VulkanUtils.VkDevices)
            {
                ncnnGpus.Items.Add(vkGpu.Id);
                tooltipNcnn += $"{vkGpu.Id.ToString().PadLeft(2)} = {vkGpu.Name}\n";
            }

            toolTip1.SetToolTip(tooltipTorchGpu, tooltipTorch.Trim());
            toolTip1.SetToolTip(tooltipNcnnGpu, tooltipNcnn.Trim());
        }

        private void InitServers()
        {
            serverCombox.Items.Clear();
            serverCombox.Items.Add($"Automatic (Closest)");

            foreach (Servers.Server srv in Servers.serverList)
                serverCombox.Items.Add(srv.name);

            serverCombox.SelectedIndex = 0;
        }

        public async Task CheckModelCacheSize()
        {
            await Task.Delay(200);

            float modelFoldersMib = 0;

            foreach (string modelFolder in ModelDownloader.GetAllModelFolders())
                modelFoldersMib += IoUtils.GetDirSize(modelFolder, true) / (float)(1024 * 1024);

            clearModelCacheBtn.Invoke(() => 
            {
                clearModelCacheBtn.Enabled = modelFoldersMib > 1f;
                clearModelCacheBtn.Text = modelFoldersMib > 1f ? $"Clear Model Cache ({modelFoldersMib.ToString("0")} MB)" : "Clear Model Cache";
            });
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            Program.mainForm.UpdateStepByStepControls();
            Program.mainForm.LoadQuickSettings();
        }

        void SaveSettings()
        {
            // Remove spaces...
            torchGpus.Text = torchGpus.Text.Replace(" ", "");
            ncnnGpus.Text = ncnnGpus.Text.Replace(" ", "");

            // General
            ConfigParser.SaveGuiElement(onlyShowRelevantSettings);
            ConfigParser.SaveComboxIndex(processingMode);
            ConfigParser.SaveGuiElement(maxVidHeight, ConfigParser.StringMode.Int);
            ConfigParser.SaveComboxIndex(tempFolderLoc); ConfigParser.SaveGuiElement(tempDirCustom);
            ConfigParser.SaveComboxIndex(outFolderLoc); ConfigParser.SaveGuiElement(custOutDir);
            ConfigParser.SaveGuiElement(keepTempFolder);
            ConfigParser.SaveGuiElement(exportNamePattern);
            ConfigParser.SaveGuiElement(exportNamePatternLoop);
            // Interpolation
            ConfigParser.SaveGuiElement(keepAudio);
            ConfigParser.SaveGuiElement(keepSubs);
            ConfigParser.SaveGuiElement(keepMeta);
            ConfigParser.SaveGuiElement(enableAlpha);
            ConfigParser.SaveGuiElement(jpegFrames);
            ConfigParser.SaveComboxIndex(dedupMode);
            ConfigParser.SaveComboxIndex(mpdecimateMode);
            ConfigParser.SaveGuiElement(dedupThresh);
            ConfigParser.SaveGuiElement(enableLoop);
            ConfigParser.SaveGuiElement(scnDetect);
            ConfigParser.SaveGuiElement(scnDetectValue);
            // ConfigParser.SaveComboxIndex(sceneChangeFillMode);
            ConfigParser.SaveComboxIndex(autoEncMode);
            ConfigParser.SaveComboxIndex(autoEncBackupMode);
            ConfigParser.SaveGuiElement(sbsAllowAutoEnc);
            ConfigParser.SaveGuiElement(alwaysWaitForAutoEnc);
            // AI
            ConfigParser.SaveGuiElement(torchGpus);
            ConfigParser.SaveGuiElement(ncnnGpus);
            ConfigParser.SaveGuiElement(ncnnThreads);
            ConfigParser.SaveGuiElement(uhdThresh);
            ConfigParser.SaveGuiElement(rifeCudaFp16);
            ConfigParser.SaveGuiElement(dainNcnnTilesize, ConfigParser.StringMode.Int);
            // Export
            ConfigParser.SaveGuiElement(minOutVidLength, ConfigParser.StringMode.Int);
            ConfigParser.SaveGuiElement(maxFps);
            ConfigParser.SaveComboxIndex(loopMode);
            ConfigParser.SaveGuiElement(fixOutputDuration);
            // Debugging
            ConfigParser.SaveComboxIndex(serverCombox);
            ConfigParser.SaveGuiElement(ffEncPreset);
            ConfigParser.SaveGuiElement(ffEncArgs);
        }

        void LoadSettings()
        {
            ConfigParser.LoadGuiElement(onlyShowRelevantSettings);
            // General
            ConfigParser.LoadComboxIndex(processingMode);
            ConfigParser.LoadGuiElement(maxVidHeight);
            ConfigParser.LoadComboxIndex(tempFolderLoc); ConfigParser.LoadGuiElement(tempDirCustom);
            ConfigParser.LoadComboxIndex(outFolderLoc); ConfigParser.LoadGuiElement(custOutDir);
            ConfigParser.LoadGuiElement(keepTempFolder);
            ConfigParser.LoadGuiElement(exportNamePattern);
            ConfigParser.LoadGuiElement(exportNamePatternLoop);
            // Interpolation
            ConfigParser.LoadGuiElement(keepAudio);
            ConfigParser.LoadGuiElement(keepSubs);
            ConfigParser.LoadGuiElement(keepMeta);
            ConfigParser.LoadGuiElement(enableAlpha);
            ConfigParser.LoadGuiElement(jpegFrames);
            ConfigParser.LoadComboxIndex(dedupMode);
            ConfigParser.LoadComboxIndex(mpdecimateMode);
            ConfigParser.LoadGuiElement(dedupThresh);
            ConfigParser.LoadGuiElement(enableLoop);
            ConfigParser.LoadGuiElement(scnDetect);
            ConfigParser.LoadGuiElement(scnDetectValue);
            // ConfigParser.LoadComboxIndex(sceneChangeFillMode);
            ConfigParser.LoadComboxIndex(autoEncMode);
            ConfigParser.LoadComboxIndex(autoEncBackupMode);
            ConfigParser.LoadGuiElement(sbsAllowAutoEnc);
            ConfigParser.LoadGuiElement(alwaysWaitForAutoEnc);
            // AI
            ConfigParser.LoadGuiElement(torchGpus);
            ConfigParser.LoadGuiElement(ncnnGpus);
            ConfigParser.LoadGuiElement(ncnnThreads);
            ConfigParser.LoadGuiElement(uhdThresh);
            ConfigParser.LoadGuiElement(rifeCudaFp16);
            ConfigParser.LoadGuiElement(dainNcnnTilesize);
            // Export
            ConfigParser.LoadGuiElement(minOutVidLength);
            ConfigParser.LoadGuiElement(maxFps);
            ConfigParser.LoadComboxIndex(loopMode);
            ConfigParser.LoadGuiElement(fixOutputDuration);
            // Debugging
            ConfigParser.LoadComboxIndex(serverCombox);
            ConfigParser.LoadGuiElement(ffEncPreset);
            ConfigParser.LoadGuiElement(ffEncArgs);
        }

        private void SetVisibility()
        {
            bool onlyRelevant = onlyShowRelevantSettings.Checked;

            // Dev options
            List<Control> devOptions = new List<Control> { panKeepTempFolder, };
            devOptions.ForEach(c => c.SetVisible(Program.Debug));

            // Legacy/deprecated/untested options
            List<Control> legacyUntestedOptions = new List<Control> { panProcessingStyle, panEnableAlpha, panHqJpegImport };
            legacyUntestedOptions.ForEach(c => c.SetVisible(Program.Debug));

            // AutoEnc options
            bool autoEncPossible = !_currentAi.Piped;
            panAutoEnc.Visible = !(onlyRelevant && !autoEncPossible);
            bool autoEncEnabled = autoEncMode.Visible && autoEncMode.SelectedIndex != 0;
            List<Control> autoEncOptions = new List<Control> { panAutoEncBackups, panAutoEncLowSpaceMode };
            autoEncOptions.ForEach(c => c.SetVisible(autoEncEnabled));
            panAutoEncInSbsMode.SetVisible(autoEncEnabled && panProcessingStyle.Visible);

            var availAis = Implementations.NetworksAvailable;
            bool showTorchSettings = !(onlyRelevant && _currentAi.Backend != AiInfo.AiBackend.Pytorch);
            panTorchGpus.SetVisible(showTorchSettings && NvApi.NvGpus.Count > 0 && Python.IsPytorchReady());
            bool showNcnnSettings = !(onlyRelevant && _currentAi.Backend != AiInfo.AiBackend.Ncnn);
            panNcnnGpus.SetVisible(showNcnnSettings && VulkanUtils.VkDevices.Count > 0);
            bool showRifeCudaSettings = !(onlyRelevant && _currentAi != Implementations.rifeCuda);
            panRifeCudaHalfPrec.SetVisible(showRifeCudaSettings && NvApi.NvGpus.Count > 0 && availAis.Contains(Implementations.rifeCuda));
            bool showDainNcnnSettings = !(onlyRelevant && _currentAi != Implementations.dainNcnn);
            new List<Control> { panTitleDainNcnn, panDainNcnnTileSize }.ForEach(c => c.SetVisible(showDainNcnnSettings && availAis.Contains(Implementations.dainNcnn)));
        }

        private void AddTooltipClickFunction()
        {
            foreach (Control control in AllControls)
            {
                if(!(control is PictureBox))
                    continue;

                string tooltipText = toolTip1.GetToolTip(control);

                if (tooltipText.IsEmpty())
                    continue;

                control.Click += (sender, e) => { MessageBox.Show(tooltipText, "Tooltip", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            }
        }

        private void tempFolderLoc_SelectedIndexChanged(object sender, EventArgs e)
        {
            tempDirBrowseBtn.Visible = tempFolderLoc.SelectedIndex == 4;
            tempDirCustom.Visible = tempFolderLoc.SelectedIndex == 4;
        }

        private void outFolderLoc_SelectedIndexChanged(object sender, EventArgs e)
        {
            custOutDirBrowseBtn.Visible = outFolderLoc.SelectedIndex == 1;
            custOutDir.Visible = outFolderLoc.SelectedIndex == 1;
        }

        private void tempDirBrowseBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = tempDirCustom.Text.Trim(), IsFolderPicker = true };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                tempDirCustom.Text = dialog.FileName;

            ConfigParser.SaveGuiElement(tempDirCustom);
        }

        private void custOutDirBrowseBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { InitialDirectory = custOutDir.Text.Trim(), IsFolderPicker = true };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                custOutDir.Text = dialog.FileName;

            ConfigParser.SaveGuiElement(custOutDir);
        }

        private void dedupMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            magickDedupePanel.Visible = false; // dedupMode.SelectedIndex == 2;
            dedupeSensLabel.Visible = false; // dedupMode.SelectedIndex != 0;
            mpDedupePanel.Visible = dedupMode.SelectedIndex == 1;
        }

        private void clearModelCacheBtn_Click(object sender, EventArgs e)
        {
            ModelDownloader.DeleteAllModels();
            clearModelCacheBtn.Text = "Clear Model Cache";
            CheckModelCacheSize();
        }

        private void modelDownloaderBtn_Click(object sender, EventArgs e)
        {
            new ModelDownloadForm().ShowDialog();
            CheckModelCacheSize();
        }

        private void autoEncMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetVisibility();
        }

        private async void resetBtn_Click(object sender, EventArgs e)
        {
            DialogResult dialog = UiUtils.ShowMessageBox($"Are you sure you want to reset the configuration?", "Are you sure?", MessageBoxButtons.YesNo);

            if (dialog == DialogResult.No)
                return;

            await Config.Reset(3, this);
            SettingsForm_Load(null, null);
        }

        private void btnResetHwEnc_Click(object sender, EventArgs e)
        {
            Close();
            Program.mainForm.ResetOutputUi();
        }

        private bool _sizeFixApplied = false;

        private void settingsTabList_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetVisibility();

            if (!_sizeFixApplied)
            {
                Size = new Size(Width + 1, Height + 1);
                Size = new Size(Width - 1, Height - 1);
                _sizeFixApplied = true;
            }
        }
    }
}
