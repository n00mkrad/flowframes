using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Ui;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Os
{
    class Updater
    {
        public enum VersionCompareResult { Older, Newer, Equal };
        public static string latestVerUrl = "https://raw.githubusercontent.com/n00mkrad/flowframes/main/ver.txt";

        public static string GetInstalledVerStr()
        {
            Version ver = GetInstalledVer();
            if (ver.Major == 0 && ver.Minor == 0 && ver.Minor == 0) return "";
            return ver.ToString();
        }

        public static Version GetInstalledVer()
        {
            try
            {
                string verStr = IoUtils.ReadLines(Paths.GetVerPath())[0];
                return new Version(verStr);
            }
            catch (Exception e)
            {
                Logger.Log("Error getting installed version!");
                Logger.Log(e.Message, true);
                return new Version(0, 0, 0);
            }
        }

        public static VersionCompareResult CompareVersions(Version currentVersion, Version newVersion)
        {
            Logger.Log($"Checking if {newVersion} > {currentVersion}", true);
            int result = newVersion.CompareTo(currentVersion);

            if (result > 0)
            {
                Logger.Log($"{newVersion} is newer than {currentVersion}.", true);
                return VersionCompareResult.Newer;
            }

            if (result < 0)
            {
                Logger.Log($"{newVersion} is older than {currentVersion}.", true);
                return VersionCompareResult.Older;
            }

            Logger.Log($"{newVersion} is equal to {currentVersion}.", true);
            return VersionCompareResult.Equal;
        }

        public static Version GetLatestVer(bool patreon)
        {
            var client = new WebClient();
            int line = patreon ? 0 : 2;
            return new Version(client.DownloadString(latestVerUrl).SplitIntoLines()[line]);
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

        public static async Task UpdateTo(int version, UpdaterForm form = null)
        {
            Logger.Log("Updating to " + version, true);
            string savePath = Path.Combine(Paths.GetExeDir(), $"FlowframesV{version}");
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
                UiUtils.ShowMessageBox("Error: Failed to download update.\n\n" + e.Message, UiUtils.MessageType.Error);
                Logger.Log("Updater Error during download: " + e.Message, true);
                return;
            }
            try
            {
                Logger.Log("Installing v" + version, true);
                string runningExePath = Paths.GetExe();
                string oldExePath = runningExePath + ".old";
                IoUtils.TryDeleteIfExists(oldExePath);
                File.Move(runningExePath, oldExePath);
                File.Move(savePath, runningExePath);
            }
            catch (Exception e)
            {
                UiUtils.ShowMessageBox("Error: Failed to install update.\n\n" + e.Message, UiUtils.MessageType.Error);
                Logger.Log("Updater Error during install: " + e.Message, true);
                return;
            }
            form.SetProgLabel(101f, $"Update downloaded.");
            await Task.Delay(20);
            UiUtils.ShowMessageBox("Update was installed!\nFlowframes will now close. Restart it to use the new version.");
            Application.Exit();
        }

        public static async Task AsyncUpdateCheck()
        {
            Version installed = GetInstalledVer();
            Version latestPat = GetLatestVer(true);
            Version latestFree = GetLatestVer(false);

            Logger.Log($"You are running Flowframes {installed}. The latest Patreon version is {latestPat}, the latest free version is {latestFree}.");

            string gpus = OsUtils.GetGpus();

            if (installed.ToString() != "0.0.0")
                Program.mainForm.Text = $"Flowframes {installed}";
            else
                Program.mainForm.Text = $"Flowframes [Unknown Version]";

            if (!string.IsNullOrWhiteSpace(gpus.Trim()))
                Program.mainForm.Text = $"{Program.mainForm.Text} [{gpus}]";
        }

        public static async Task UpdateModelList()
        {
            if (!Config.GetBool("fetchModelsFromRepo", false))
                return;

            foreach (AI ai in Implementations.NetworksAll)
            {
                try
                {
                    var client = new WebClient();
                    string aiName = ai.PkgDir;
                    string url = $"https://raw.githubusercontent.com/n00mkrad/flowframes/main/Pkgs/{aiName}/models.txt";
                    string movePath = Path.Combine(Paths.GetPkgPath(), aiName, "models.txt");
                    string savePath = movePath + ".tmp";

                    if (!Directory.Exists(savePath.GetParentDir()))
                    {
                        Logger.Log($"Skipping {ai.PkgDir} models file download as '{savePath.GetParentDir()}' does not exist!", true);
                        continue;
                    }

                    Logger.Log($"Saving models file from '{url}' to '{savePath}'", true);
                    client.DownloadFile(url, savePath);

                    if (IoUtils.GetFilesize(savePath) > 8)
                    {
                        File.Delete(movePath);
                        File.Move(savePath, movePath);
                    }
                    else
                    {
                        File.Delete(savePath);
                    }

                    Program.mainForm.UpdateAiModelCombox();
                }
                catch (Exception e)
                {
                    Logger.Log($"Failed to fetch models file for {ai.FriendlyName}. Ignore this if you are not connected to the internet.");
                    Logger.Log($"{e.Message}\n{e.StackTrace}", true);
                }
            }
        }
    }
}
