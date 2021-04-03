using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Flowframes.MiscUtils;

namespace Flowframes.IO
{
    class Symlinks
    {
        public enum Flag { File = 0, Directory = 1, Unprivileged = 2 }
        [DllImport("kernel32.dll")]
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, Flag dwFlags);

        public static bool SymlinksAllowed()
        {
            string origFile = Paths.GetExe();
            string linkPath = Paths.GetExe() + "linktest";
            bool success = CreateSymbolicLink(linkPath, origFile, Flag.Unprivileged);

            if (success)
            {
                File.Delete(linkPath);
                return true;
            }

            return false;
        }

        public static async Task CreateSymlinksParallel(Dictionary<string, string> pathsLinkTarget, int maxThreads = 150)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            ParallelOptions opts = new ParallelOptions() {MaxDegreeOfParallelism = maxThreads};

            Task forEach = Task.Run(async () => Parallel.ForEach(pathsLinkTarget, opts, pair =>
            {
                CreateSymbolicLink(pair.Key, pair.Value, Flag.Unprivileged);
            }));

            while (!forEach.IsCompleted) await Task.Delay(1);
            Logger.Log($"Created {pathsLinkTarget.Count} symlinks in {FormatUtils.TimeSw(sw)}", true);
        }
    }
}
