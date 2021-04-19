using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.IO;

namespace Flowframes.OS
{
    class StartupChecks
    {
        static bool IsWin10 ()
        {
            string[] osInfo = OSUtils.GetOs().Split(" | ");
            string version = osInfo[0].Remove("Microsoft").Trim();
            return version.ToLower().Contains("windows 10");
        }

        static bool Is32Bit ()
        {
            string[] osInfo = OSUtils.GetOs().Split(" | ");
            string arch = osInfo[1].Trim();
            return arch.Contains("32");
        }

        public static void CheckOs ()
        {
            if (!File.Exists(Paths.GetVerPath()) && Paths.GetExeDir().ToLower().Contains("temp"))
            {
                MessageBox.Show("You seem to be running Flowframes out of an archive.\nPlease extract the whole archive first!", "Error");
                IOUtils.TryDeleteIfExists(Paths.GetDataPath());
                Application.Exit();
            }

            string[] osInfo = OSUtils.GetOs().Split(" | ");
            string version = osInfo[0].Remove("Microsoft").Trim();

            if (Is32Bit() && !Config.GetBool("allow32Bit", false))
            {
                MessageBox.Show("This application is not compatible with 32 bit operating systems!", "Error");
                Application.Exit();
            }

            if (!version.ToLower().Contains("windows 10") && !Config.GetBool("ignoreIncompatibleOs", false))
            {
                MessageBox.Show($"This application was made for Windows 10 and is not officially compatible with {version}.\n\n" +
                                $"Use it at your own risk and do NOT ask for support as long as your are on {version}.", "Warning");
            }
        }

        public static async Task SymlinksCheck()
        {
            if (!IsWin10())
                return;

            bool silent = Config.GetBool("silentDevmodeCheck", true);
            string ver = Updater.GetInstalledVer().ToString();
            bool symlinksAllowed = Symlinks.SymlinksAllowed();
            Logger.Log($"SymlinksAllowed: {symlinksAllowed}", true);

            if (!symlinksAllowed && Config.Get("askedForDevModeVersion") != ver)
            {
                if (!silent)
                {
                    MessageBox.Show("Flowframes will now enable Windows' Developer Mode which is required for video encoding improvements.\n\n" +
                                    "This requires administrator privileges once.", "Message");
                }

                Logger.Log($"Trying to enable dev mode.", true);

                string devmodeBatchPath = Path.Combine(Paths.GetDataPath(), "devmode.bat");
                File.WriteAllText(devmodeBatchPath, Properties.Resources.devmode);

                Process devmodeProc = OSUtils.NewProcess(true);
                devmodeProc.StartInfo.Arguments = $"/C {devmodeBatchPath.Wrap()}";
                devmodeProc.Start();
                while (!devmodeProc.HasExited) await Task.Delay(100);

                bool symlinksWorksNow = false;

                for (int retries = 8; retries > 0; retries--)
                {
                    symlinksWorksNow = Symlinks.SymlinksAllowed();

                    if(symlinksWorksNow)
                        break;

                    await Task.Delay(500);
                }

                if (!symlinksWorksNow)
                {
                    if (!silent)
                    {
                        MessageBox.Show("Failed to enable developer mode - Perhaps you do not have sufficient privileges.\n\n" +
                                        "Without Developer Mode, video encoding will be noticably slower.\n\nYou can still try enabling " +
                                        "it manually in the Windows 10 Settings:\nSettings -> Update & security -> For developers -> Developer mode.", "Message");
                    }

                    Logger.Log("Failed to enable dev mode.", true);
                    Config.Set("askedForDevModeVersion", ver);
                }
                else
                {
                    Logger.Log("Enabled Windows Developer Mode.", silent);
                }

                IOUtils.TryDeleteIfExists(devmodeBatchPath);
            }
        }
    }
}
