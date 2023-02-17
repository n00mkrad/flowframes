using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.Os;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Utilities
{
    class NcnnUtils
    {
        /// <summary> Get amount of GPU Compute Queues (VK) for each GPU </summary>
        public static async Task<Dictionary<int, int>> GetNcnnGpuComputeQueueCounts ()
        {
            Dictionary<int, int> queueCounts = new Dictionary<int, int>(); // int gpuId, int queueCount

            Process rifeNcnn = OsUtils.NewProcess(true);
            rifeNcnn.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D  {Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnn.PkgDir)} & rife-ncnn-vulkan.exe -i dummydir -o dummydir";

            string output = await Task.Run(() => OsUtils.GetProcStdOut(rifeNcnn, true));
            var queueLines = output.SplitIntoLines().Where(x => x.MatchesWildcard(@"*queueC=*queue*"));

            foreach (var line in queueLines)
            {
                int gpuId = line.Split(' ')[0].GetInt();
                int queueCount = line.Split("queue")[1].Split('[')[1].Split(']')[0].GetInt();
                Logger.Log($"NCNN: Found GPU {gpuId} with compute queue count {queueCount}", true);
                queueCounts[gpuId] = queueCount;
            }

            return queueCounts;
        }

        public static async Task<int> GetRifeNcnnGpuThreads(Size res, int gpuId, AI ai)
        {
            int threads = Config.GetInt(Config.Key.ncnnThreads);
            //if (res.Width * res.Height > 2560 * 1440) threads = 4;
            // if (res.Width * res.Height > 3840 * 2160) threads = 1;

            if (threads != 1)
            {
                var queueDict = await GetNcnnGpuComputeQueueCounts();
                int maxThreads = queueDict.ContainsKey(gpuId) ? queueDict[gpuId] : 1;
                threads = threads.Clamp(1, maxThreads); // To avoid exceeding the max queue count
                Logger.Log($"Using {threads}/{maxThreads} GPU threads.", true, false, ai.LogFilename);
            }
            else
            {
                Logger.Log($"Using {threads} GPU thread.", true, false, ai.LogFilename);
            }

            return threads;
        }

        public static string GetNcnnPattern()
        {
            return $"%0{Padding.interpFrames}d{Interpolate.currentSettings.interpExt}";
        }

        public static string GetNcnnTilesize(int tilesize)
        {
            int gpusAmount = Config.Get(Config.Key.ncnnGpus).Split(',').Length;
            string tilesizeStr = $"{tilesize}";

            for (int i = 1; i < gpusAmount; i++)
                tilesizeStr += $",{tilesize}";

            return tilesizeStr;
        }

        public static async Task<string> GetNcnnThreads(AI ai)
        {
            int gpusAmount = Config.Get(Config.Key.ncnnGpus).Split(',').Length;
            int threads = await GetRifeNcnnGpuThreads(new Size(), Config.Get(Config.Key.ncnnGpus).Split(',')[0].GetInt(), ai);
            string progThreadsStr = $"{threads}";

            for (int i = 1; i < gpusAmount; i++)
                progThreadsStr += $",{threads}";

            return $"{(Interpolate.currentlyUsingAutoEnc ? 2 : 4)}:{progThreadsStr}:4"; // Read threads: 1 for singlethreaded, 2 for autoenc, 4 if order is irrelevant
        }

        public static async Task DeleteNcnnDupes(string dir, float factor)
        {
            int dupeCount = InterpolateUtils.GetRoundedInterpFramesPerInputFrame(factor);
            var files = IoUtils.GetFileInfosSorted(dir, false).Reverse().Take(dupeCount).ToList();
            Logger.Log($"DeleteNcnnDupes: Calculated dupe count from factor; deleting last {dupeCount} interp frames of {IoUtils.GetAmountOfFiles(dir, false)} ({string.Join(", ", files.Select(x => x.Name))})", true);

            int attempts = 4;

            while (attempts > 0)
            {
                try
                {
                    files.ForEach(x => x.Delete());
                    break;
                }
                catch (Exception ex)
                {
                    attempts--;

                    if (attempts < 1)
                    {
                        Logger.Log($"DeleteNcnnDupes Error: {ex.Message}", true);
                        break;
                    }
                    else
                    {
                        await Task.Delay(500);
                    }
                }
            }
        }
    }
}
