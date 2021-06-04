using Flowframes.IO;
using Flowframes.Media;
using Flowframes.MiscUtils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class SettingsForm : Form
    {
        bool initialized = false;

        public SettingsForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            MinimumSize = new Size(Width, Height);
            MaximumSize = new Size(Width, (Height * 1.5f).RoundToInt());

            LoadSettings();
            initialized = true;
            Task.Run(() => CheckModelCacheSize());
        }

        public async Task CheckModelCacheSize ()
        {
            await Task.Delay(200);

            long modelFoldersBytes = 0;

            foreach (string modelFolder in ModelDownloader.GetAllModelFolders())
                modelFoldersBytes += IOUtils.GetDirSize(modelFolder, true);

            if (modelFoldersBytes > 1024 * 1024)
                clearModelCacheBtn.Text = $"Clear Model Cache ({FormatUtils.Bytes(modelFoldersBytes)})";
            else
                clearModelCacheBtn.Enabled = false;
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            Program.mainForm.UpdateStepByStepControls();
            Program.mainForm.LoadQuickSettings();
        }

        void SaveSettings ()
        {
            // Clamp...
            mp4Crf.Text = ((int)mp4Crf.Value).Clamp(0, 50).ToString();
            vp9Crf.Text = ((int)vp9Crf.Value).Clamp(0, 63).ToString();
            // Remove spaces...
            torchGpus.Text = torchGpus.Text.Replace(" ", "");
            ncnnGpus.Text = ncnnGpus.Text.Replace(" ", "");

            // General
            ConfigParser.SaveComboxIndex(processingMode);
            ConfigParser.SaveGuiElement(maxVidHeight, ConfigParser.StringMode.Int);
            ConfigParser.SaveComboxIndex(tempFolderLoc);
            ConfigParser.SaveGuiElement(keepTempFolder);
            ConfigParser.SaveGuiElement(exportNamePattern);
            ConfigParser.SaveGuiElement(exportNamePatternLoop);
            ConfigParser.SaveGuiElement(delLogsOnStartup);
            ConfigParser.SaveGuiElement(clearLogOnInput);
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
            ConfigParser.SaveGuiElement(sbsAllowAutoEnc);
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
            ConfigParser.SaveComboxIndex(maxFpsMode);
            ConfigParser.SaveComboxIndex(loopMode);
            ConfigParser.SaveGuiElement(fixOutputDuration);
            // Encoding
            ConfigParser.SaveComboxIndex(mp4Enc);
            Config.Set(mp4CrfConfigKey, mp4Crf.Value.ToString());
            ConfigParser.SaveGuiElement(vp9Crf);
            ConfigParser.SaveComboxIndex(proResProfile);
            ConfigParser.SaveGuiElement(aviCodec);
            ConfigParser.SaveGuiElement(aviColors);
            ConfigParser.SaveGuiElement(gifColors);
            ConfigParser.SaveGuiElement(gifDitherType);
            ConfigParser.SaveGuiElement(imgSeqFormat);
            // Debugging
            ConfigParser.SaveComboxIndex(cmdDebugMode);
            ConfigParser.SaveGuiElement(autoDedupFrames);
            ConfigParser.SaveGuiElement(mdlBaseUrl);
            ConfigParser.SaveGuiElement(ffEncThreads, ConfigParser.StringMode.Int);
            ConfigParser.SaveGuiElement(ffEncPreset);
            ConfigParser.SaveGuiElement(ffEncArgs);
        }

        void LoadSettings()
        {
            // General
            ConfigParser.LoadComboxIndex(processingMode);
            ConfigParser.LoadGuiElement(maxVidHeight);
            ConfigParser.LoadComboxIndex(tempFolderLoc); ConfigParser.LoadGuiElement(tempDirCustom);
            ConfigParser.LoadGuiElement(delLogsOnStartup);
            ConfigParser.LoadGuiElement(keepTempFolder);
            ConfigParser.LoadGuiElement(exportNamePattern);
            ConfigParser.LoadGuiElement(exportNamePatternLoop);
            ConfigParser.LoadGuiElement(clearLogOnInput);
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
            ConfigParser.LoadGuiElement(sbsAllowAutoEnc);
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
            ConfigParser.LoadComboxIndex(maxFpsMode);
            ConfigParser.LoadComboxIndex(loopMode);
            ConfigParser.LoadGuiElement(fixOutputDuration);
            // Encoding
            ConfigParser.LoadComboxIndex(mp4Enc);
            ConfigParser.LoadGuiElement(vp9Crf);
            ConfigParser.LoadComboxIndex(proResProfile);
            ConfigParser.LoadGuiElement(aviCodec);
            ConfigParser.LoadGuiElement(aviColors);
            ConfigParser.LoadGuiElement(gifColors);
            ConfigParser.LoadGuiElement(gifDitherType);
            ConfigParser.LoadGuiElement(imgSeqFormat);
            // Debugging
            ConfigParser.LoadComboxIndex(cmdDebugMode);
            ConfigParser.LoadGuiElement(autoDedupFrames);
            ConfigParser.LoadGuiElement(mdlBaseUrl);
            ConfigParser.LoadGuiElement(ffEncThreads);
            ConfigParser.LoadGuiElement(ffEncPreset);
            ConfigParser.LoadGuiElement(ffEncArgs);
        }

        private void tempFolderLoc_SelectedIndexChanged(object sender, EventArgs e)
        {
            tempDirBrowseBtn.Visible = tempFolderLoc.SelectedIndex == 4;
            tempDirCustom.Visible = tempFolderLoc.SelectedIndex == 4;
        }

        private void tempDirBrowseBtn_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = tempDirCustom.Text.Trim();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                tempDirCustom.Text = dialog.FileName;

            ConfigParser.SaveGuiElement(tempDirCustom);
        }

        private void cmdDebugMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialized && cmdDebugMode.SelectedIndex == 2)
                MessageBox.Show("If you enable this, you need to close the CMD window manually after the process has finished, otherwise processing will be paused!", "Notice");
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

        string mp4CrfConfigKey;

        private void mp4Enc_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = mp4Enc.Text.ToUpper().Remove(" ");

            if (text.Contains(FFmpegUtils.Codec.H264.ToString().ToUpper()))
                mp4CrfConfigKey = "h264Crf";

            if (text.Contains(FFmpegUtils.Codec.H265.ToString().ToUpper()))
                mp4CrfConfigKey = "h265Crf";

            if (text.Contains(FFmpegUtils.Codec.AV1.ToString().ToUpper()))
                mp4CrfConfigKey = "av1Crf";

            mp4Crf.Value = Config.GetInt(mp4CrfConfigKey);
        }
    }
}
