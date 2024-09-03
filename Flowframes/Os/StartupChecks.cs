using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.IO;
using Flowframes.Ui;

namespace Flowframes.Os
{
    class StartupChecks
    {
        static bool IsWin10Or11()
        {
            string osInfoStr = OsUtils.TryGetOs();

            if (string.IsNullOrWhiteSpace(osInfoStr))
                return true;    // If it fails, assume we are on Win10

            string[] osInfo = osInfoStr.Split(" | ");
            string version = osInfo[0].Remove("Microsoft").Trim();

            return (version.Lower().Contains("windows 10") || version.Lower().Contains("windows 11"));
        }

        static bool Is32Bit()
        {
            string osInfoStr = OsUtils.TryGetOs();

            if (string.IsNullOrWhiteSpace(osInfoStr))
                return false;    // If it fails, assume we are on 64bit

            string[] osInfo = osInfoStr.Split(" | ");
            string arch = osInfo[1].Trim();
            return arch.Contains("32");
        }

        public static void CheckOs()
        {
            if (!File.Exists(Paths.GetVerPath()) && Paths.GetExeDir().Lower().Contains("temp"))
            {
                UiUtils.ShowMessageBox("You seem to be running Flowframes out of an archive.\nPlease extract the whole archive first!", UiUtils.MessageType.Error);
                IoUtils.TryDeleteIfExists(Paths.GetDataPath());
                Application.Exit();
            }

            string[] osInfo = OsUtils.TryGetOs().Split(" | ");
            string version = osInfo[0].Remove("Microsoft").Trim();

            if (Is32Bit() && !Config.GetBool("allow32Bit", false))
            {
                UiUtils.ShowMessageBox("This application is not compatible with 32 bit operating systems!", UiUtils.MessageType.Error);
                Application.Exit();
            }

            if (string.IsNullOrWhiteSpace(version))
                return;

            if (!version.Lower().Contains("windows 10") && !version.Lower().Contains("windows 11") && !Config.GetBool("ignoreIncompatibleOs", false))
            {
                UiUtils.ShowMessageBox($"This application was made for Windows 10/11 and is not officially compatible with {version}.\n\n" +
                                $"Use it at your own risk and do NOT ask for support as long as your are on {version}.", UiUtils.MessageType.Warning);
            }
        }

        public static async Task SymlinksCheck()
        {
            if (!IsWin10Or11())
                return;

            bool silent = Config.GetBool("silentDevmodeCheck", true);
            string ver = Updater.GetInstalledVer().ToString();
            bool symlinksAllowed = Symlinks.SymlinksAllowed();
            Logger.Log($"SymlinksAllowed: {symlinksAllowed}", true);

            if (!symlinksAllowed && Config.Get(Config.Key.askedForDevModeVersion) != ver)
            {
                if (!silent)
                {
                    UiUtils.ShowMessageBox("Flowframes will now enable Windows' Developer Mode which is required for video encoding improvements.\n\n" +
                                    "This requires administrator privileges once.", UiUtils.MessageType.Message);
                }

                Logger.Log($"Trying to enable dev mode.", true);

                string devmodeBatchPath = Path.Combine(Paths.GetDataPath(), "devmode.bat");
                File.WriteAllText(devmodeBatchPath, Properties.Resources.devmode);

                Process devmodeProc = OsUtils.NewProcess(true);
                devmodeProc.StartInfo.Arguments = $"/C {devmodeBatchPath.Wrap()}";
                devmodeProc.Start();
                while (!devmodeProc.HasExited) await Task.Delay(100);

                bool symlinksWorksNow = false;

                for (int retries = 8; retries > 0; retries--)
                {
                    symlinksWorksNow = Symlinks.SymlinksAllowed();

                    if (symlinksWorksNow)
                        break;

                    await Task.Delay(500);
                }

                if (!symlinksWorksNow)
                {
                    if (!silent)
                    {
                        UiUtils.ShowMessageBox("Failed to enable developer mode - Perhaps you do not have sufficient privileges.\n\n" +
                                        "Without Developer Mode, video encoding will be noticably slower.\n\nYou can still try enabling " +
                                        "it manually in the Windows 10 Settings:\nSettings -> Update & security -> For developers -> Developer mode.", UiUtils.MessageType.Message);
                    }

                    Logger.Log("Failed to enable dev mode.", true);
                    Config.Set("askedForDevModeVersion", ver);
                }
                else
                {
                    Logger.Log("Enabled Windows Developer Mode.", silent);
                }

                IoUtils.TryDeleteIfExists(devmodeBatchPath);
            }
        }

        public static async Task DetectHwEncoders ()
        {
            if (Config.GetBool(Config.Key.PerformedHwEncCheck))
                return;

            Logger.Log($"Detecting hardare encoding support...");
            var encoders = new[] { "h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_amf", "hevc_amf" };
            var compatEncoders = new List<string>();

            foreach(string e in encoders)
            {
                bool compat = await FfmpegCommands.IsEncoderCompatible(e);

                if (compat)
                {
                    compatEncoders.Add(e);
                    Logger.Log($"HW Encoder supported: {e}", true);
                }
            }

            Logger.Log($"Available hardware encoders: {string.Join(", ", compatEncoders.Select(e => e.Replace("_", " ").Upper()))}");
            Config.Set(Config.Key.SupportedHwEncoders, string.Join(",", compatEncoders));
            Config.Set(Config.Key.PerformedHwEncCheck, true.ToString());
        }
    }
}
