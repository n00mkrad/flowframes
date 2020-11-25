using Flowframes.Forms;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.OS
{
    class Updater
    {
        public static string latestVerUrl = "https://dl.nmkd.de/flowframes/exe/latest.txt";

        public static int GetInstalledVer()
        {
            return Program.version;
        }

        public static int GetLatestVer ()
        {
            var client = new WebClient();
            return client.DownloadString(latestVerUrl).GetInt();
        }

        public static async Task UpdateTo (int version, UpdaterForm form = null)
        {
            Logger.Log("Updating to v" + version, true);
            string savePath = Path.Combine(IOUtils.GetExeDir(), $"FlowframesV{version}");
            try
            {
                var client = new WebClient();
                client.DownloadProgressChanged += async (sender, args) =>
                {
                    if (form != null && (args.ProgressPercentage % 5 == 0))
                    {
                        Logger.Log("Downloading update... " + args.ProgressPercentage, true);
                        form.SetProgLabel(args.ProgressPercentage, $"Downloading latest version... {args.ProgressPercentage}%");
                        await Task.Delay(20);
                    }
                };
                client.DownloadFileCompleted += (sender, args) =>
                {
                    form.SetProgLabel(100f, $"Downloading latest version... 100%");
                };
                await client.DownloadFileTaskAsync(new Uri($"https://dl.nmkd.de/flowframes/exe/{version}/Flowframes.exe"), savePath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: Failed to download update.\n\n" + e.Message, "Error");
                Logger.Log("Updater Error during download: " + e.Message, true);
                return;
            }
            try
            {
                Logger.Log("Installing v" + version, true);
                string runningExePath = IOUtils.GetExe();
                string oldExePath = runningExePath + ".old";
                IOUtils.TryDeleteIfExists(oldExePath);
                File.Move(runningExePath, oldExePath);
                File.Move(savePath, runningExePath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: Failed to install update.\n\n" + e.Message, "Error");
                Logger.Log("Updater Error during install: " + e.Message, true);
                return;
            }
            form.SetProgLabel(101f, $"Update downloaded.");
            await Task.Delay(20);
            MessageBox.Show("Update was installed!\nFlowframes will now close. Restart it to use the new version.", "Message");
            Application.Exit();
        }

        public static async Task AsyncUpdateCheck ()
        {
            var client = new WebClient();
            var latestVer = await client.DownloadStringTaskAsync(latestVerUrl);
            int latest = latestVer.GetInt();
            int installed = GetInstalledVer();

            if (installed < latest)
                Logger.Log("An update for Flowframes is available! Download it using the Updater.");
            else
                Logger.Log("Flowframes is up to date.");
        }
    }
}
