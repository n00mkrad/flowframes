using Flowframes.Forms;
using Flowframes.Forms.Main;
using Flowframes.IO;
using Flowframes.Os;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: System.Windows.Media.DisableDpiAwareness] // Disable Dpi awareness in the application assembly.

namespace Flowframes
{
    static class Program
    {
        public static bool Debug = false;
        public static string[] args = new string[0];
        public static bool initialRun = true;
        public static Form1 mainForm;

        public static bool busy = false;

        public static string lastInputPath;
        public static bool lastInputPathIsSsd;

        public static Queue<InterpSettings> batchQueue = new Queue<InterpSettings>();
        public static bool CmdMode = false;

        [STAThread]
        static void Main()
        {
            // Catch unhandled exceptions across application
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            CmdMode = Paths.GetExe().EndsWith("Cmd.exe");
            Cli.HandleCli();
            Debug = Cli.Debug || System.Diagnostics.Debugger.IsAttached;

            if(CmdMode && !Cli.CanStart)
            {
                Environment.Exit(0);
            }

            // Show splash screen
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var splash = new SplashForm("Starting Flowframes...");

            // Force culture to en-US across entire application (to avoid number parsing issues etc)
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Set up TLS for web requests - Not sure if needed, but seemed to help with web request problems
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            Paths.Init();
            Config.Init();
            Logger.Log($"Command Line: {Environment.CommandLine}", true);
            Task.Run(() => DiskSpaceCheckLoop());

            LaunchGui();
        }

        private static void LaunchGui()
        {
            bool showMdlDownloader = Cli.ShowMdlDownloader; // The latter check may be needed for legacy reasons
            mainForm = new Form1() { ShowModelDownloader = showMdlDownloader, Enabled = !Cli.AutoRun };
            Application.Run(mainForm);
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string text = $"Unhandled Thread Exception!\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}\n\n" +
                $"The error has been copied to the clipboard. Please inform the developer about this.";
            ShowUnhandledError(text);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            string text = $"Unhandled UI Exception!\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\n" +
                $"The error has been copied to the clipboard. Please inform the developer about this.";
            ShowUnhandledError(text);
        }

        private static void ShowUnhandledError(string text)
        {
            UiUtils.ShowMessageBox(text, UiUtils.MessageType.Error);
            Clipboard.SetText(text);
        }

        public static void Cleanup()
        {
            int keepLogsDays = 7;
            int keepSessionDataDays = 4;

            try
            {
                CleanupOldDirectories(Paths.GetLogPath(true), keepLogsDays, "log");
                
                IoUtils.DeleteContentsOfDir(Paths.GetSessionDataPath()); // Clear this session's temp files...
                
                CleanupOldDirectories(Paths.GetSessionsPath(), keepSessionDataDays, "session");

                List<string> delPaths = IoUtils.GetFilesSorted(Paths.GetPkgPath(), false, "*.log*").ToList();
                string crashDumpsDir = Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "CrashDumps");
                delPaths.AddRange(IoUtils.GetFilesSorted(crashDumpsDir, false, "rife*.exe.*.dmp").ToList());
                delPaths.AddRange(IoUtils.GetFilesSorted(crashDumpsDir, false, "flowframes*.exe.*.dmp").ToList());

                string installerTempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowframesInstallerTemp");
                if (Directory.Exists(installerTempDir) && (DateTime.Now - Directory.GetLastWriteTime(installerTempDir)).TotalDays > 1d)
                {
                    delPaths.Add(installerTempDir);
                }

                delPaths.AddRange(IoUtils.GetFileInfosSorted(Paths.GetCachePath(), true).Where(cf => (DateTime.Now - cf.LastWriteTime).TotalDays > 3d).Select(cf => cf.FullName));

                delPaths.ForEach(p => IoUtils.TryDeleteIfExists(p));
            }
            catch (Exception e)
            {
                Logger.Log($"Cleanup Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void CleanupOldDirectories(string basePath, int keepDays, string folderType)
        {
            foreach (DirectoryInfo dir in new DirectoryInfo(basePath).GetDirectories())
            {
                int fileCount = dir.GetFiles("*", SearchOption.AllDirectories).Length;

                if (fileCount < 1)
                {
                    Logger.Log($"[Cleanup] Deleting {folderType} folder {dir.Name} (is empty)", true);
                    IoUtils.TryDeleteIfExists(dir.FullName);
                    continue;
                }

                string[] split = dir.Name.Split('-');
                int daysOld = (DateTime.Now - new DateTime(split[0].GetInt(), split[1].GetInt(), split[2].GetInt())).Days;

                if (daysOld > keepDays)
                {
                    Logger.Log($"[Cleanup] Deleting {folderType} folder {dir.Name} (is {daysOld} days old, {folderType} folders are only kept for {keepDays} days)", true);
                    IoUtils.TryDeleteIfExists(dir.FullName);
                }
            }
        }

        /// <summary>
        /// Continuously checks disk space in order to pause interpolation if disk space is running low. Is quite fast (sub 1ms)
        /// </summary>
        private static async Task DiskSpaceCheckLoop()
        {
            await Task.Delay(5000);

            while (true)
            {
                if (!busy || Interpolate.currentSettings == null || !Directory.Exists(Interpolate.currentSettings.tempFolder))
                {
                    await Task.Delay(5000);
                    continue;
                }

                try
                {
                    string drivePath = Interpolate.currentSettings.tempFolder.Substring(0, 2);
                    long mb = IoUtils.GetDiskSpace(Interpolate.currentSettings.tempFolder);
                    int nextWaitTimeMs = ((int)mb).Clamp(1000, 20000); // Check runs more often the less space there is (min 1s, max 20s interval)
                    bool lowDiskSpace = mb < (Config.GetInt(Config.Key.lowDiskSpacePauseGb, 5) * 1024);
                    bool tooLowDiskSpace = mb < (Config.GetInt(Config.Key.lowDiskSpaceCancelGb, 2) * 1024);
                    string spaceGb = (mb / 1024f).ToString("0.0");

                    // Logger.Log($"Disk space check for '{drivePath}/': {spaceGb} GB free, next check in {nextWaitTimeMs / 1024} sec", true);

                    if (!Interpolate.canceled && (AiProcess.lastAiProcess != null && !AiProcess.lastAiProcess.HasExited) && lowDiskSpace)
                    {
                        if (tooLowDiskSpace)
                        {
                            Interpolate.Cancel($"Not enough disk space for temporary files on '{drivePath}/' ({spaceGb} GB)!");
                        }
                        else
                        {
                            bool showMsg = !AiProcessSuspend.aiProcFrozen;
                            AiProcessSuspend.SuspendIfRunning();

                            if (showMsg)
                            {
                                UiUtils.ShowMessageBox($"Interpolation has been paused because you are running out of disk space on '{drivePath}/' ({spaceGb} GB)!\n\n" +
                                $"Please either clear up some disk space or cancel the interpolation.", UiUtils.MessageType.Warning);
                            }
                        }
                    }

                    await Task.Delay(nextWaitTimeMs);

                }
                catch (Exception e)
                {
                    Logger.Log($"Disk space check failed: {e.Message}", true);
                    await Task.Delay(5000);
                }
            }
        }
    }
}
