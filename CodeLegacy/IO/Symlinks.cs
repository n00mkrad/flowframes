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

        public static async Task CreateSymlinksParallel(Dictionary<string, string> pathsLinkTarget, bool debug = false, int maxThreads = 150)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            ParallelOptions opts = new ParallelOptions() {MaxDegreeOfParallelism = maxThreads};

            Task forEach = Task.Run(async () => Parallel.ForEach(pathsLinkTarget, opts, pair =>
            {
                bool success = CreateSymbolicLink(pair.Key, pair.Value, Flag.Unprivileged);

                if (debug)
                    Logger.Log($"Created Symlink - Source: '{pair.Key}' - Target: '{pair.Value}' - Sucess: {success}", true);
            }));

            while (!forEach.IsCompleted) await Task.Delay(1);
            Logger.Log($"Created {pathsLinkTarget.Count} symlinks in {FormatUtils.TimeSw(sw)}", true);
        }

        public static async Task<bool> MakeSymlinksForEncode(string framesFile, string linksDir, int zPad = 8)
        {
            try
            { 
                IoUtils.DeleteIfExists(linksDir);
                Directory.CreateDirectory(linksDir);
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                Logger.Log($"Creating symlinks for '{framesFile}' in '{linksDir} with zPadding {zPad}'", true);

                int counter = 0;

                Dictionary<string, string> pathsLinkTarget = new Dictionary<string, string>();

                foreach (string line in File.ReadAllLines(framesFile))
                {
                    string relTargetPath = line.Remove("file '").Split('\'').FirstOrDefault(); // Relative path in frames file
                    string absTargetPath = Path.Combine(framesFile.GetParentDir(), relTargetPath); // Full path to frame
                    string linkPath = Path.Combine(linksDir, counter.ToString().PadLeft(zPad, '0') + Path.GetExtension(relTargetPath));
                    pathsLinkTarget.Add(linkPath, absTargetPath);
                    counter++;
                }

                await CreateSymlinksParallel(pathsLinkTarget);

                if (IoUtils.GetAmountOfFiles(linksDir, false) > 1)
                {
                    return true;
                }
                else
                {
                    Logger.Log("Symlink creation seems to have failed even though SymlinksAllowed was true! Encoding ini with concat demuxer instead.", true);
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Log("MakeSymlinks Exception: " + e.Message);
            }

            return false;
        }
    }
}
