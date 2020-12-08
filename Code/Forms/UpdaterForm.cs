using Flowframes.Data;
using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class UpdaterForm : Form
    {
        SemVer installed;
        SemVer latest;

        public UpdaterForm()
        {
            InitializeComponent();
        }

        private async void UpdaterForm_Load(object sender, EventArgs e)
        {
            installed = Updater.GetInstalledVer();
            latest = Updater.GetLatestVer();

            installedLabel.Text = "v" + installed;
            await Task.Delay(100);
            latestLabel.Text = "v" + latest;

            if (installedLabel.Text == latestLabel.Text)
            {
                updateBtn.Text = "Redownload Latest Version";
                statusLabel.Text = "Latest Version Is Installed.";
            }
            else
            {
                if(Updater.IsVersionNewer(installed, latest))
                {
                    updateBtn.Text = "Update To Latest Version!";
                    statusLabel.Text = "Update Available!";
                }
                else
                {
                    updateBtn.Text = "Rollback To Public Version";
                    statusLabel.Text = "Using Newer Version Than Latest Public Release.";
                }
            }

            updateBtn.Enabled = true;
        }

        float lastProg = -1f;
        public void SetProgLabel (float prog, string str)
        {
            if (prog == lastProg) return;
            lastProg = prog;
            downloadingLabel.Text = str;
        }

        private async void updateBtn_Click(object sender, EventArgs e)
        {
            string link = Updater.GetLatestVerLink();
            if(!string.IsNullOrWhiteSpace(link))
                Process.Start(link);
        }
    }
}
