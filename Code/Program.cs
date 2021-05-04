using Flowframes.Data;
using Flowframes.IO;
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
                        if (Interpolate.current.tempFolder.Length < 3)
                            return;

                        string drivePath = Interpolate.current.tempFolder.Substring(0, 2);
                        long mb = IOUtils.GetDiskSpace(Interpolate.current.tempFolder);

                        Logger.Log($"Disk space check for '{drivePath}/': {(mb / 1024f).ToString("0.0")} GB free.", true);

                        if (!Interpolate.canceled && mb < (Config.GetInt("minDiskSpaceGb", 5) * 1024))
                            Interpolate.Cancel("Running out of disk space!");
                    }
                    catch
                    {
                        // Disk space check failed, this is not critical and might just be caused by a null ref
                    }
                }

                await Task.Delay(15000);
            }
        }
    }
}
