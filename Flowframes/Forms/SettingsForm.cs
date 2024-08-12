using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Media;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable IDE1006

namespace Flowframes.Forms
{
    public partial class SettingsForm : Form
    {
        bool initialized = false;

        public SettingsForm(int index = 0)
        {
            AutoScaleMode = AutoScaleMode.None;
            InitializeComponent();
            settingsTabList.SelectedIndex = index;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            MinimumSize = new Size(Width, Height);
            MaximumSize = new Size(Width, (Height * 1.5f).RoundToInt());

            InitServers();
            LoadSettings();
            initialized = true;
            Task.Run(() => CheckModelCacheSize());
        }

        void InitServers()
        {
            serverCombox.Items.Clear();
            serverCombox.Items.Add($"Automatic (Closest)");

            foreach (Servers.Server srv in Servers.serverList)
                serverCombox.Items.Add(srv.name);

            serverCombox.SelectedIndex = 0;
        }

        public async Task CheckModelCacheSize ()
        {
            await Task.Delay(200);

            long modelFoldersBytes = 0;

            foreach (string modelFolder in ModelDownloader.GetAllModelFolders())
                modelFoldersBytes += IoUtils.GetDirSize(modelFolder, true);

            if (modelFoldersBytes > 1024 * 1024)
            {
                clearModelCacheBtn.Enabled = true;
                clearModelCacheBtn.Text = $"Clear Model Cache ({FormatUtils.Bytes(modelFoldersBytes)})";
            }
            else
            {
                clearModelCacheBtn.Enabled = false;
            }
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            Program.mainForm.UpdateStepByStepControls();
            Program.mainForm.LoadQuickSettings();
        }

        void SaveSettings ()
        {
            // Remove spaces...
            torchGpus.Text = torchGpus.Text.Replace(" ", "");
            ncnnGpus.Text = ncnnGpus.Text.Replace(" ", "");

            // General
            ConfigParser.SaveComboxIndex(processingMode);
            ConfigParser.SaveGuiElement(maxVidHeight, ConfigParser.StringMode.Int);
            ConfigParser.SaveComboxIndex(tempFolderLoc);
            ConfigParser.SaveComboxIndex(outFolderLoc);
            ConfigParser.SaveGuiElement(keepTempFolder);
            ConfigParser.SaveGuiElement(exportNamePattern);
            ConfigParser.SaveGuiElement(exportNamePatternLoop);
            ConfigParser.SaveGuiElement(disablePreview);
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
            ConfigParser.SaveComboxIndex(sceneChangeFillMode);
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
            ConfigParser.SaveComboxIndex(cmdDebugMode);
            ConfigParser.SaveComboxIndex(serverCombox);
            ConfigParser.SaveGuiElement(ffEncPreset);
            ConfigParser.SaveGuiElement(ffEncArgs);
        }

        void LoadSettings()
        {
            // General
            ConfigParser.LoadComboxIndex(processingMode);
            ConfigParser.LoadGuiElement(maxVidHeight);
            ConfigParser.LoadComboxIndex(tempFolderLoc); ConfigParser.LoadGuiElement(tempDirCustom);
            ConfigParser.LoadComboxIndex(outFolderLoc); ConfigParser.LoadGuiElement(custOutDir);
            ConfigParser.LoadGuiElement(keepTempFolder);
            ConfigParser.LoadGuiElement(exportNamePattern);
            ConfigParser.LoadGuiElement(exportNamePatternLoop);
            ConfigParser.LoadGuiElement(disablePreview);
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
            ConfigParser.LoadComboxIndex(sceneChangeFillMode);
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
            ConfigParser.LoadComboxIndex(cmdDebugMode);
            ConfigParser.LoadComboxIndex(serverCombox);
            ConfigParser.LoadGuiElement(ffEncPreset);
            ConfigParser.LoadGuiElement(ffEncArgs);
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

        private void cmdDebugMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialized && cmdDebugMode.SelectedIndex == 2)
                UiUtils.ShowMessageBox("If you enable this, you need to close the CMD window manually after the process has finished, otherwise processing will be paused!", UiUtils.MessageType.Warning);
        }

        private void dedupMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            dedupeSensLabel.Visible = dedupMode.SelectedIndex != 0;
            magickDedupePanel.Visible = dedupMode.SelectedIndex == 1;
            mpDedupePanel.Visible = dedupMode.SelectedIndex == 2;
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
            autoEncBlockPanel.Visible = autoEncMode.SelectedIndex == 0;
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
    }
}
