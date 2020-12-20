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
        SemVer latestPat;
        SemVer latestFree;

        public UpdaterForm()
        {
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

            if (Updater.VersionMatches(installed, latestFree))
            {
                statusLabel.Text = "Latest Free Version Is Installed.";

                if (Updater.IsVersionNewer(installed, latestPat))
                    statusLabel.Text += "\nBeta Update Available On Patreon.";

                return;
            }

            if (Updater.VersionMatches(installed, latestPat))
            {
                statusLabel.Text = "Latest Patreon/Beta Version Is Installed.";
                return;
            }

            if (Updater.IsVersionNewer(installed, latestPat))
            {
                statusLabel.Text = "Update available on Patreon!";

                if (Updater.IsVersionNewer(installed, latestFree))
                    statusLabel.Text += " - Beta Updates Available On Patreon and Itch.io.";

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
