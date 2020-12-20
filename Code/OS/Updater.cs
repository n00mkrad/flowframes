using Flowframes.Data;
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
        public static string latestVerUrl = "https://dl.nmkd.de/flowframes/exe/ver.ini";

        public static SemVer GetInstalledVer()
        {
            try
            {
                string verStr = IOUtils.ReadLines(Paths.GetVerPath())[0];
                return new SemVer(verStr);
            }
            catch (Exception e)
            {
                Logger.Log("Error getting installed version: " + e.Message);
                return new SemVer(0, 0, 0);
            }
        }

        public static bool IsVersionNewer (SemVer currentVer, SemVer newVer)
        {
            if (newVer.major > currentVer.major)
            {
                return true;
            }
            else
            {
                if(newVer.minor > currentVer.minor)
                {
                    return true;
                }
                else
                {
                    if (newVer.patch > currentVer.patch)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public static bool VersionMatches (SemVer v1, SemVer v2)
        {
            return v1.major == v2.major && v1.minor == v2.minor && v1.patch == v2.patch;
        }

        public static SemVer GetLatestVer (bool patreon)
        {
            var client = new WebClient();
            int line = patreon ? 0 : 2;
            return new SemVer(client.DownloadString(latestVerUrl).SplitIntoLines()[line]);
        }

        public static string GetLatestVerLink(bool patreon)
        {
            int line = patreon ? 1 : 3;
            var client = new WebClient();
            try
            {
                return client.DownloadString(latestVerUrl).SplitIntoLines()[line].Trim();
            }
            catch
            {
                Logger.Log("Failed to get latest version link from ver.ini!", true);
                return "";
            }
        }

        public static async Task UpdateTo (int version, UpdaterForm form = null)
        {
            Logger.Log("Updating to " + version, true);
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
            SemVer installed = GetInstalledVer();
            SemVer latestPat = GetLatestVer(true);
            SemVer latestFree = GetLatestVer(false);

            Logger.Log($"You are running Flowframes {installed}. The latest Patreon version is {latestPat}, the latest free version is {latestFree}.");

            // if (IsVersionNewer(installed, latest))
            //     Logger.Log($"An update for Flowframes ({latest}) is available! Download it from the Updater.");
            // else
            //     Logger.Log($"Flowframes is up to date ({installed}).");
        }
    }
}
