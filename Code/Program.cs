using Flowframes.Data;
using Flowframes.IO;
using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: System.Windows.Media.DisableDpiAwareness] // Disable Dpi awareness in the application assembly.

namespace Flowframes
{
    static class Program
    {
        public static Form1 mainForm;

        public static bool busy = false;

        public static string lastInputPath;
        public static bool lastInputPathIsSsd;

        public static Queue<InterpSettings> batchQueue = new Queue<InterpSettings>();

        [STAThread]
        static void Main()
        {
            Config.Init();

            if (Config.GetBool("delLogsOnStartup"))
                IOUtils.DeleteContentsOfDir(Paths.GetLogPath());        // Clear out older logs from previous session

            Networks.Init();
            Task.Run(() => Logger.Run());
            Task.Run(() => DiskSpaceCheckLoop());

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            mainForm = new Form1();
            Application.Run(mainForm);
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string text = $"Unhandled Thread Exception!\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}\n\n" +
                $"The error has been copied to the clipboard. Please inform the developer about this.";
            ShowUnhandledError(text);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            string text = $"Unhandled UI Exception!\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\n" +
                $"The error has been copied to the clipboard. Please inform the developer about this.";
            ShowUnhandledError(text);
        }

        static void ShowUnhandledError (string text)
        {
            MessageBox.Show(text, "Unhandled Error");
            Clipboard.SetText(text);
        }

        static async Task DiskSpaceCheckLoop ()
        {
            while (true)
            {
                if (busy)
                {
                    try
                    {
                        if (Interpolate.current == null || Interpolate.current.tempFolder.Length < 3)
                            return;

                        string drivePath = Interpolate.current.tempFolder.Substring(0, 2);
                        long mb = IOUtils.GetDiskSpace(Interpolate.current.tempFolder);

                        Logger.Log($"Disk space check for '{drivePath}/': {(mb / 1024f).ToString("0.0")} GB free.", true);

                        bool lowDiskSpace = mb < (Config.GetInt("lowDiskSpacePauseGb", 5) * 1024);
                        bool tooLowDiskSpace = mb < (Config.GetInt("lowDiskSpaceCancelGb", 2) * 1024);
                        string spaceGb = (mb / 1024f).ToString("0.0");

                        if (!Interpolate.canceled && (AiProcess.lastAiProcess != null && !AiProcess.lastAiProcess.HasExited) && lowDiskSpace)
                        {
                            if (tooLowDiskSpace)
                            {
                                Interpolate.Cancel($"Not enough disk space on '{drivePath}/' ({spaceGb} GB)!");
                            }
                            else
                            {
                                AiProcessSuspend.SuspendIfRunning();
                                MessageBox.Show($"Interpolation has been paused because you are running out of disk space on '{drivePath}/' ({spaceGb} GB)!\n\n" +
                                    $"Please either clear up some disk space or cancel the interpolation.", "Warning");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Disk space check failed: {e.Message}", true);
                    }
                }

                await Task.Delay(15000);
            }
        }
    }
}
