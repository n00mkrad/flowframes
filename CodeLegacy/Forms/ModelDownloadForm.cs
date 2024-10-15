using Flowframes.Data;
using Flowframes.MiscUtils;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class ModelDownloadForm : Form
    {
        public ModelDownloadForm()
        {
            InitializeComponent();
        }

        private void ModelDownloadForm_Load(object sender, EventArgs e)
        {
            var availImplems = Implementations.NetworksAvailable.Select(ai => ai.NameInternal.Replace("_", "").Lower());

            void SetChecked(CheckBox cb)
            {
                cb.Visible = availImplems.Contains(cb.Name.Lower());
            }

            new List<Control> { rifeCuda, rifeNcnn, dainNcnn, flavrCuda, xvfiCuda }.ForEach(cb => SetChecked((CheckBox)cb));
        }

        public void SetWorking(bool state, bool allowCancel = true)
        {
            Logger.Log($"ModelDownloadForm SetWorking({state})", true);
            SetProgress(-1);
            Control[] controlsToDisable = new Control[] { downloadModelsBtn };
            Control[] controlsToHide = new Control[] { closeBtn };
            progressCircle.Visible = state;

            foreach (Control c in controlsToDisable)
                c.Enabled = !state;

            foreach (Control c in controlsToHide)
                c.Visible = !state;

            Program.busy = state;
            Program.mainForm.UpdateStepByStepControls();
        }

        public void SetProgress(int percent)
        {
            percent = percent.Clamp(0, 100);
            TaskbarManager.Instance.SetProgressValue(percent, 100);
            longProgBar.Value = percent;
            longProgBar.Refresh();
        }

        public void SetStatus(string status)
        {
            statusLabel.Text = status;
        }

        public void SetDownloadBtnEnabled(bool state)
        {
            downloadModelsBtn.Enabled = state;
        }

        private void downloadModelsBtn_Click(object sender, EventArgs e)
        {
            ModelDownloadFormUtils.form = this;
            bool rifeC = rifeCuda.Visible && rifeCuda.Checked;
            bool rifeN = rifeNcnn.Visible && rifeNcnn.Checked;
            bool dainN = dainNcnn.Visible && dainNcnn.Checked;
            bool flavrC = flavrCuda.Visible && flavrCuda.Checked;
            bool xvfiC = xvfiCuda.Visible && xvfiCuda.Checked;
            ModelDownloadFormUtils.DownloadModels(rifeC, rifeN, dainN, flavrC, xvfiC);
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            ModelDownloadFormUtils.Cancel();
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            ModelDownloadFormUtils.Cancel();
            Close();
        }
    }
}
