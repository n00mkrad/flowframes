using Flowframes.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class SettingsForm : Form
    {
        bool initialized = false;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            initialized = true;
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            Program.mainForm.UpdateStepByStepControls(true);
        }

        void SaveSettings ()
        {
            // Clamp...
            h264Crf.Text = h264Crf.GetInt().Clamp(0, 51).ToString();
            h265Crf.Text = h265Crf.GetInt().Clamp(0, 51).ToString();
            vp9Crf.Text = vp9Crf.GetInt().Clamp(0, 63).ToString();
            ncnnThreads.Text = ncnnThreads.GetInt().Clamp(1, 8).ToString();
            // Remove spaces...
            torchGpus.Text = torchGpus.Text.Replace(" ", "");
            ncnnGpus.Text = ncnnGpus.Text.Replace(" ", "");
            // Force numbers...
            ffEncThreads.Text = ffEncThreads.GetInt().ToString();

            // General
            ConfigParser.SaveComboxIndex(processingMode);
            ConfigParser.SaveGuiElement(maxVidHeight);
            ConfigParser.SaveComboxIndex(tempFolderLoc);
            ConfigParser.SaveGuiElement(keepTempFolder);
            ConfigParser.SaveGuiElement(delLogsOnStartup);
            // Interpolation
            ConfigParser.SaveGuiElement(enableAudio);
            ConfigParser.SaveComboxIndex(dedupMode);
            ConfigParser.SaveComboxIndex(mpdecimateMode);
            ConfigParser.SaveGuiElement(dedupThresh, ConfigParser.StringMode.Float);
            ConfigParser.SaveComboxIndex(timingMode);
            ConfigParser.SaveGuiElement(enableLoop);
            ConfigParser.SaveGuiElement(jpegInterps);
            ConfigParser.SaveGuiElement(scnDetect);
            ConfigParser.SaveGuiElement(scnDetectValue, ConfigParser.StringMode.Float);
            ConfigParser.SaveComboxIndex(autoEncMode);
            ConfigParser.SaveGuiElement(sbsAllowAutoEnc);
            // AI
            ConfigParser.SaveGuiElement(torchGpus);
            ConfigParser.SaveGuiElement(ncnnGpus);
            ConfigParser.SaveGuiElement(ncnnThreads);
            ConfigParser.SaveGuiElement(uhdThresh);
            // Video Export
            ConfigParser.SaveGuiElement(minOutVidLength, ConfigParser.StringMode.Int);
            ConfigParser.SaveComboxIndex(mp4Enc);
            ConfigParser.SaveGuiElement(h264Crf);
            ConfigParser.SaveGuiElement(h265Crf);
            ConfigParser.SaveGuiElement(vp9Crf);
            ConfigParser.SaveComboxIndex(proResProfile);
            ConfigParser.SaveGuiElement(gifColors);
            ConfigParser.SaveGuiElement(aviCodec);
            ConfigParser.SaveGuiElement(aviColors);
            ConfigParser.SaveGuiElement(maxFps);
            ConfigParser.SaveComboxIndex(maxFpsMode);
            ConfigParser.SaveComboxIndex(loopMode);
            // Debugging
            ConfigParser.SaveComboxIndex(cmdDebugMode);
            ConfigParser.SaveGuiElement(autoDedupFrames);
            ConfigParser.SaveGuiElement(ffEncThreads);
            ConfigParser.SaveGuiElement(ffEncPreset);
            ConfigParser.SaveGuiElement(ffEncArgs);
            ConfigParser.SaveGuiElement(ffprobeCountFrames);
        }

        void LoadSettings()
        {
            // General
            ConfigParser.LoadComboxIndex(processingMode);
            ConfigParser.LoadGuiElement(maxVidHeight);
            ConfigParser.LoadComboxIndex(tempFolderLoc); ConfigParser.LoadGuiElement(tempDirCustom);
            ConfigParser.LoadGuiElement(delLogsOnStartup);
            ConfigParser.LoadGuiElement(keepTempFolder);
            // Interpolation
            ConfigParser.LoadGuiElement(enableAudio);
            ConfigParser.LoadComboxIndex(dedupMode);
            ConfigParser.LoadComboxIndex(mpdecimateMode);
            ConfigParser.LoadGuiElement(dedupThresh, "%");
            ConfigParser.LoadComboxIndex(timingMode);
            ConfigParser.LoadGuiElement(enableLoop);
            ConfigParser.LoadGuiElement(jpegInterps);
            ConfigParser.LoadGuiElement(scnDetect);
            ConfigParser.LoadGuiElement(scnDetectValue);
            ConfigParser.LoadComboxIndex(autoEncMode);
            ConfigParser.LoadGuiElement(sbsAllowAutoEnc);
            // AI
            ConfigParser.LoadGuiElement(torchGpus);
            ConfigParser.LoadGuiElement(ncnnGpus);
            ConfigParser.LoadGuiElement(ncnnThreads);
            ConfigParser.LoadGuiElement(uhdThresh);
            // Video Export
            ConfigParser.LoadGuiElement(minOutVidLength);
            ConfigParser.LoadComboxIndex(mp4Enc);
            ConfigParser.LoadGuiElement(h264Crf);
            ConfigParser.LoadGuiElement(h265Crf);
            ConfigParser.LoadGuiElement(vp9Crf);
            ConfigParser.LoadComboxIndex(proResProfile);
            ConfigParser.LoadGuiElement(gifColors);
            ConfigParser.LoadGuiElement(aviCodec);
            ConfigParser.LoadGuiElement(aviColors);
            ConfigParser.LoadGuiElement(maxFps);
            ConfigParser.LoadComboxIndex(maxFpsMode);
            ConfigParser.LoadComboxIndex(loopMode);
            // Debugging
            ConfigParser.LoadComboxIndex(cmdDebugMode);
            ConfigParser.LoadGuiElement(autoDedupFrames);
            ConfigParser.LoadGuiElement(ffEncThreads);
            ConfigParser.LoadGuiElement(ffEncPreset);
            ConfigParser.LoadGuiElement(ffEncArgs);
            ConfigParser.LoadGuiElement(ffprobeCountFrames);
        }

        private void dedupThresh_Leave(object sender, EventArgs e)
        {
            dedupThresh.Text = dedupThresh.GetFloat().ToString() + "%";
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
            magickDedupePanel.Visible = dedupMode.SelectedIndex == 1;
            mpDedupePanel.Visible = dedupMode.SelectedIndex == 2;
        }
    }
}
