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
        public const int version = 16;

        public static Form1 mainForm;

        public static bool busy = false;

        public static string lastInputPath;
        public static bool lastInputPathIsSsd;

        public static Queue<BatchEntry> batchQueue = new Queue<BatchEntry>();

        [STAThread]
        static void Main()
        {
            Paths.Init();
            Config.Init();

            if (Config.GetBool("deleteLogsOnStartup"))
                IOUtils.DeleteContentsOfDir(Paths.GetLogPath());        // Clear out older logs not from this session

            string oldExePath = IOUtils.GetExe() + ".old";
            IOUtils.TryDeleteIfExists(oldExePath);

            PkgInstaller.Init();
            Networks.Init();
            NvApi.Init();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            mainForm = new Form1();
            Application.Run(mainForm);

        }
    }
}
