using Flowframes.Data;
using Flowframes.Forms.Main;
using Flowframes.IO;
using Flowframes.Media;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using HTAlt.WinForms;
using Microsoft.VisualBasic;
using Microsoft.WindowsAPICodePack.Dialogs;
using NvAPIWrapper.Native.GPU.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;
using Win32Interop.Enums;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

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
            changeTextofCores();
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

        public async Task CheckModelCacheSize()
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

        void SaveSettings()
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
            ConfigParser.SaveGuiElement(formatofInterp);
            ConfigParser.SaveGuiElement(intelQSVDecode);
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
            ConfigParser.SaveGuiElement(rifeCudaBufferSize);
            ConfigParser.SaveGuiElement(rifeCudaFp16);
            ConfigParser.SaveGuiElement(wthreads);
            ConfigParser.SaveGuiElement(dainNcnnTilesize, ConfigParser.StringMode.Int);
            // Export
            ConfigParser.SaveGuiElement(minOutVidLength, ConfigParser.StringMode.Int);
            ConfigParser.SaveGuiElement(maxFps);
            ConfigParser.SaveComboxIndex(loopMode);
            ConfigParser.SaveGuiElement(fixOutputDuration);
            ConfigParser.SaveGuiElement(systemSoundActivated); ConfigParser.SaveGuiElement(playSoundCustom);
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
            ConfigParser.LoadGuiElement(formatofInterp);
            ConfigParser.LoadGuiElement(intelQSVDecode);
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
            ConfigParser.LoadGuiElement(wthreads);
            ConfigParser.LoadGuiElement(rifeCudaBufferSize);
            ConfigParser.LoadGuiElement(dainNcnnTilesize);
            // Export
            ConfigParser.LoadGuiElement(minOutVidLength);
            ConfigParser.LoadGuiElement(maxFps);
            ConfigParser.LoadComboxIndex(loopMode);
            ConfigParser.LoadGuiElement(fixOutputDuration);
            ConfigParser.LoadGuiElement(systemSoundActivated); ConfigParser.LoadGuiElement(playSoundCustom);

            // Debugging
            ConfigParser.LoadComboxIndex(cmdDebugMode);
            ConfigParser.LoadComboxIndex(serverCombox);
            ConfigParser.LoadGuiElement(ffEncPreset);
            ConfigParser.LoadGuiElement(ffEncArgs);
        }

        private void tempFolderLoc_SelectedIndexChanged(object sender, EventArgs e)
        {
            tempDirBrowseBtn.Visible = tempFolderLoc.SelectedIndex == 5;
            tempDirCustom.Visible = tempFolderLoc.SelectedIndex == 5;
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

        private void label63_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void autoEncBlockPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void titleLabel_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void formatofInterp_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void playSoundCustom_TextChanged(object sender, EventArgs e)
        {

        }

        private void playSoundCustomBtn_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = playSoundCustom.Text.Trim(),
                Title = "Browse Text Files",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "wav",
                Filter = "WAVE (Uncompressed Audio)|*.wav",
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                playSoundCustom.Text = openFileDialog1.FileName;
            }

            ConfigParser.SaveGuiElement(playSoundCustom);
        }

        private void systemSoundActivated_SelectedValueChanged(object sender, EventArgs e)
        {
            playSoundCustom.Visible = systemSoundActivated.SelectedIndex == 7;
            playSoundCustomBtn.Visible = systemSoundActivated.SelectedIndex == 7;
        }

        private void changeTextofCores() {
            int coreCount = 0;
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }

            int threadcustom = Environment.ProcessorCount;
            string customtext = "System Cores/Threads:" + coreCount + "/" + threadcustom;
            label38.Text = customtext;
        }

        private void label38_TextChanged(object sender, EventArgs e)
        {

        }

        Form1 dotheResetInMainGUI = Application.OpenForms.OfType<Form1>().FirstOrDefault();

        private async void changePresetToIntelNvidia()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to PERFORMANCE Intel/Nvidia Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "2");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "True");
            Config.Set(Config.Key.jpegFrames, "True");
            Config.Set(Config.Key.formatofInterp, "jpg");
            Config.Set(Config.Key.rifeCudaFp16, "True");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264 NVENC,Very High,YUV 4:2:0 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_CUDA");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }

        private async void changePresetToAMDNvidia()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to PERFORMANCE AMD/Nvida Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "2");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "False");
            Config.Set(Config.Key.jpegFrames, "True");
            Config.Set(Config.Key.formatofInterp, "jpg");
            Config.Set(Config.Key.rifeCudaFp16, "True");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264 NVENC,Very High,YUV 4:2:0 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_CUDA");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }


        private async void changePresetToAMDAMD()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to PERFORMANCE AMD/AMD Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "2");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "False");
            Config.Set(Config.Key.jpegFrames, "True");
            Config.Set(Config.Key.formatofInterp, "jpg");
            Config.Set(Config.Key.rifeCudaFp16, "False");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264,Very High,YUV 4:2:0 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_NCNN");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }

        private async void changePresetToIntelIntel()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to PERFORMANCE Intel/Intel Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "2");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "True");
            Config.Set(Config.Key.jpegFrames, "True");
            Config.Set(Config.Key.formatofInterp, "jpg");
            Config.Set(Config.Key.rifeCudaFp16, "False");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264,Very High,YUV 4:2:0 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_CUDA");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }

        private async void changePresetToNormal()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to Normal Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "0");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "False");
            Config.Set(Config.Key.jpegFrames, "True");
            Config.Set(Config.Key.formatofInterp, "png");
            Config.Set(Config.Key.rifeCudaFp16, "False");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264,Very High,YUV 4:2:0 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_CUDA");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }

        private async void changePresetToPurist()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to Purist Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "0");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "False");
            Config.Set(Config.Key.jpegFrames, "False");
            Config.Set(Config.Key.formatofInterp, "png");
            Config.Set(Config.Key.rifeCudaFp16, "False");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264,Lossless,YUV 4:2:0 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_CUDA");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }

        private async void changePresetToSuperPurist()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to Super Purist Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "0");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "False");
            Config.Set(Config.Key.jpegFrames, "False");
            Config.Set(Config.Key.formatofInterp, "bmp");
            Config.Set(Config.Key.rifeCudaFp16, "False");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264,Lossless,YUV 4:4:4 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_CUDA");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }


        private async void changePresetToLowDiskSpaceMode()
        {
            DialogResult dialog2 = UiUtils.ShowMessageBox($"Are you sure you want to change to Super Purist Preset?", "Setting Preset", MessageBoxButtons.YesNo);
            if (dialog2 == DialogResult.No) return;
            await Task.Run(() => Config.Reset(3, this));
            Config.Set(Config.Key.autoEncMode, "2");
            Config.Set(Config.Key.tempFolderLoc, "0");
            Config.Set(Config.Key.intelQSVDecode, "False");
            Config.Set(Config.Key.jpegFrames, "True");
            Config.Set(Config.Key.formatofInterp, "jpg");
            Config.Set(Config.Key.rifeCudaFp16, "False");
            Config.Set(Config.Key.rifeCudaBufferSize, "250");
            int threadcount = Environment.ProcessorCount;
            Config.Set(Config.Key.wthreads, (threadcount * 3).ToString());
            Config.Set(Config.Key.lastOutputSettings, "MP4,h264,very low,YUV 4:2:0 8-bit");
            Config.Set(Config.Key.lastUsedAiName, "RIFE_CUDA");
            Config.Set(Config.Key.alwaysWaitForAutoEnc, "True");
            SettingsForm_Load(null, null);
            dotheResetInMainGUI.Reset();
        }


        private void htButton1_Click(object sender, EventArgs e)
        {
            changePresetToIntelNvidia();
        }

        private void htButton2_Click(object sender, EventArgs e)
        {
            changePresetToAMDNvidia();
        }

        private void htButton3_Click(object sender, EventArgs e)
        {
            changePresetToAMDAMD();
        }

        private void htButton4_Click(object sender, EventArgs e)
        {
            changePresetToIntelIntel();
        }


        private void htButton5_Click(object sender, EventArgs e)
        {
            changePresetToNormal();
        }

        private void htButton6_Click(object sender, EventArgs e)
        {
            changePresetToPurist();
        }

        private void htButton7_Click(object sender, EventArgs e)
        {
            changePresetToSuperPurist();
        }

        private void htButton8_Click(object sender, EventArgs e)
        {
            changePresetToLowDiskSpaceMode();
        }
    }
}