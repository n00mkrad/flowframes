using Flowframes.Data;
using Flowframes.IO;
using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                    string drivePath = Interpolate.current.tempFolder.Substring(0, 2);
                    long mb = IOUtils.GetDiskSpace(Interpolate.current.tempFolder);

                    Logger.Log($"Disk space check for '{drivePath}/': {mb} MB free.", true);

                    if (mb < 4096)
                    {
                        Interpolate.Cancel("Running out of disk space!");
                    }
                }

                await Task.Delay(15000);
            }
        }
    }
}
