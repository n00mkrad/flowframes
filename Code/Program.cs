using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
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

            mainForm = new Form1();
            Application.Run(mainForm);
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

                        if (!Interpolate.canceled && mb < (Config.GetInt("minDiskSpaceGb", 6) * 1024))
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
