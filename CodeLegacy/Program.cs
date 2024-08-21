using Flowframes.Data;
using Flowframes.Forms.Main;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Os;
using Flowframes.Ui;
using NDesk.Options;
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
        public static string[] fileArgs = new string[0];
        public static string[] args = new string[0];
        public static bool initialRun = true;
        public static Form1 mainForm;

        public static bool busy = false;

        public static string lastInputPath;
        public static bool lastInputPathIsSsd;

        public static Queue<InterpSettings> batchQueue = new Queue<InterpSettings>();

        public static class Cli
        {
            public static bool ShowMdlDownloader = false;
            public static bool ExitWhenDone = false;
            public static bool InterpStart = false;
            public static float InterpFactor = -1f;
            public static Implementations.Ai InterpAi = (Implementations.Ai)(-1);
            public static Enums.Output.Format OutputFormat = Enums.Output.Format.Mp4;
            public static string InterpModel = "";
            public static string OutputDir = "";
            public static List<string> ValidFiles = new List<string>();
        }

        [STAThread]
        static void Main()
        {
            // Force culture to en-US across entire application (to avoid number parsing issues etc)
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Catch unhandled exceptions across application
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Set up TLS for web requests - Not sure if needed, but seemed to help with web request problems
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            Paths.Init();
            Config.Init();

            Task.Run(() => DiskSpaceCheckLoop());
            fileArgs = Environment.GetCommandLineArgs().Where(a => a[0] != '-' && File.Exists(a)).ToList().Skip(1).ToArray();
            args = Environment.GetCommandLineArgs().Where(a => a[0] == '-').Select(x => x.Trim().Substring(1).ToLowerInvariant()).ToArray();
            Logger.Log($"Command Line: {Environment.CommandLine}", true);
            Logger.Log($"Files: {(fileArgs.Length > 0 ? string.Join(", ", fileArgs) : "None")}", true);
            Logger.Log($"Args: {(args.Length > 0 ? string.Join(", ", args) : "None")}", true);

            HandleCli();
            LaunchGui();
        }

        private static void HandleCli ()
        {
            string ais = string.Join(", ", Enum.GetNames(typeof(Implementations.Ai)));
            string formats = string.Join(", ", Enum.GetNames(typeof(Enums.Output.Format)));

            var opts = new OptionSet
            {
                { "np|no_python", "Disable Python implementations", v => Python.DisablePython = v != null },
                { "md|open_model_downloader", "Open model downloader GUI on startup", v => Cli.ShowMdlDownloader = v != null },
                { "e|exit", "Exit automatically after interpolation has finished", v => Cli.ExitWhenDone = v != null },
                { "s|start", "Start interpolation automatically if valid parameters are provided", v => Cli.InterpStart = v != null },
                { "f|factor=", "Interpolation factor", v => Cli.InterpFactor = v.GetFloat() },
                { "a|ai=", $"Interpolation AI implementation to use (Option: {ais})", v => Cli.InterpAi = ParseUtils.GetEnum<Implementations.Ai>(v.Trim().Replace("_", "")) },
                { "m|model=", $"AI model to use", v => Cli.InterpModel = v.Trim() },
                { "v|video_format=", $"Output video format to use (Options: {formats})", v => Cli.OutputFormat = ParseUtils.GetEnum<Enums.Output.Format>(v.Trim()) },
                { "o|output_dir=", $"Output folder to save the interpolated video in", v => Cli.OutputDir = v.Trim() },
                { "<>", "Input file(s)", Cli.ValidFiles.Add },
            };

            try
            {
                opts.Parse(Environment.GetCommandLineArgs());
                Cli.ValidFiles = Cli.ValidFiles.Skip(1).Where(f => File.Exists(f)).ToList();
                Logger.Log($"Parsed CLI: Start {Cli.InterpStart}, Exit {Cli.ExitWhenDone}, Factor {Cli.InterpFactor}, AI {Cli.InterpAi}, Model '{Cli.InterpModel}', " +
                    $"Video Format {Cli.OutputFormat}, Output Dir '{Cli.OutputDir}', No Python {Python.DisablePython}, MdlDl {Cli.ShowMdlDownloader}", true);
            }
            catch (OptionException e)
            {
                Logger.Log($"Error parsing CLI options: {e.Message}", true);
            }
        }

        private static void LaunchGui()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool showMdlDownloader = Cli.ShowMdlDownloader || args.Contains("show-model-downloader"); // The latter check may be needed for legacy reasons

            mainForm = new Form1() { ShowModelDownloader = showMdlDownloader };
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
            int keepLogsDays = 4;
            int keepSessionDataDays = 4;

            try
            {
                foreach (DirectoryInfo dir in new DirectoryInfo(Paths.GetLogPath(true)).GetDirectories())
                {
                    string[] split = dir.Name.Split('-');
                    int daysOld = (DateTime.Now - new DateTime(split[0].GetInt(), split[1].GetInt(), split[2].GetInt())).Days;
                    int fileCount = dir.GetFiles("*", SearchOption.AllDirectories).Length;

                    if (daysOld > keepLogsDays || fileCount < 1) // keep logs for 4 days
                    {
                        Logger.Log($"Cleanup: Log folder {dir.Name} is {daysOld} days old and has {fileCount} files - Will Delete", true);
                        IoUtils.TryDeleteIfExists(dir.FullName);
                    }
                }

                IoUtils.DeleteContentsOfDir(Paths.GetSessionDataPath()); // Clear this session's temp files...

                foreach (DirectoryInfo dir in new DirectoryInfo(Paths.GetSessionsPath()).GetDirectories())
                {
                    string[] split = dir.Name.Split('-');
                    int daysOld = (DateTime.Now - new DateTime(split[0].GetInt(), split[1].GetInt(), split[2].GetInt())).Days;
                    int fileCount = dir.GetFiles("*", SearchOption.AllDirectories).Length;

                    if (daysOld > keepSessionDataDays || fileCount < 1) // keep temp files for 2 days
                    {
                        Logger.Log($"Cleanup: Session folder {dir.Name} is {daysOld} days old and has {fileCount} files - Will Delete", true);
                        IoUtils.TryDeleteIfExists(dir.FullName);
                    }
                }

                IoUtils.GetFilesSorted(Paths.GetPkgPath(), false, "*.log*").ToList().ForEach(x => IoUtils.TryDeleteIfExists(x));
                IoUtils.GetFilesSorted(Path.Combine(Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%"), "CrashDumps"), false, "rife*.exe.*.dmp").ToList().ForEach(x => IoUtils.TryDeleteIfExists(x));
            }
            catch (Exception e)
            {
                Logger.Log($"Cleanup Error: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Continuously checks disk space in order to pause interpolation if disk space is running low. Is quite fast (sub 1ms)
        /// </summary>
        private static async Task DiskSpaceCheckLoop()
        {
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
