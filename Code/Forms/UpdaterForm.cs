using Flowframes.Data;
using Flowframes.Os;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class UpdaterForm : Form
    {
        Version installed;
        Version latestPat;
        Version latestFree;

        public UpdaterForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            InitializeComponent();
        }

        private async void UpdaterForm_Load(object sender, EventArgs e)
        {
            installed = Updater.GetInstalledVer();
            latestPat = Updater.GetLatestVer(true);
            latestFree = Updater.GetLatestVer(false);

            installedLabel.Text = installed.ToString();
            await Task.Delay(100);
            latestLabel.Text = $"{latestPat} (Patreon/Beta) - {latestFree} (Free/Stable)";

            if (Updater.CompareVersions(installed, latestFree) == Updater.VersionCompareResult.Equal)
            {
                statusLabel.Text = "Latest Free Version Is Installed.";

                if (Updater.CompareVersions(installed, latestPat) == Updater.VersionCompareResult.Newer)
                    statusLabel.Text += "\nBeta Update Available On Patreon.";

                return;
            }

            if (Updater.CompareVersions(installed, latestPat) == Updater.VersionCompareResult.Equal)
            {
                statusLabel.Text = "Latest Patreon/Beta Version Is Installed.";
                return;
            }

            if (Updater.CompareVersions(installed, latestPat) == Updater.VersionCompareResult.Newer)
            {
                statusLabel.Text = "Update available on Patreon!";

                if (Updater.CompareVersions(installed, latestFree) == Updater.VersionCompareResult.Newer)
                    statusLabel.Text = $"Beta Updates Available On Patreon and Itch.io.";

                return;
            }
        }

        float lastProg = -1f;
        public void SetProgLabel (float prog, string str)
        {
            if (prog == lastProg) return;
            lastProg = prog;
            downloadingLabel.Text = str;
        }

        private void updatePatreonBtn_Click(object sender, EventArgs e)
        {
            string link = Updater.GetLatestVerLink(true);
            if(!string.IsNullOrWhiteSpace(link))
                Process.Start(link);
        }

        private void updateFreeBtn_Click(object sender, EventArgs e)
        {
            string link = Updater.GetLatestVerLink(false);
            if (!string.IsNullOrWhiteSpace(link))
                Process.Start(link);
        }
    }
}
