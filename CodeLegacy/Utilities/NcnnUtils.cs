using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.Os;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Flowframes.Utilities
{
    class NcnnUtils
    {
        public static int GetRifeNcnnGpuThreads(Size res, int gpuId, AiInfo ai)
        {
            int threads = Config.GetInt(Config.Key.ncnnThreads);
            int maxThreads = VulkanUtils.GetMaxNcnnThreads(gpuId).Clamp(1, 64);

            if(threads == 0)
            {
                threads = (maxThreads / 2f).RoundToInt().Clamp(1, maxThreads); // Default to half of max threads
            }

            threads = threads.Clamp(1, maxThreads); // To avoid exceeding the max queue count
            Logger.Log($"Using {threads}/{maxThreads} GPU compute threads.", true, false, ai.LogFilename);

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

        public static string GetNcnnThreads(AiInfo ai)
        {
            List<int> enabledGpuIds = Config.Get(Config.Key.ncnnGpus).Split(',').Select(s => s.GetInt()).ToList(); // Get GPU IDs
            List<int> gpuThreadCounts = enabledGpuIds.Select(g => GetRifeNcnnGpuThreads(new Size(), g, ai)).ToList(); // Get max thread count for each GPU
            string progThreadsStr = string.Join(",", gpuThreadCounts);
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
                    files.ForEach(x => IoUtils.DeleteIfExists(x.FullName));
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

        public static int GetDainNcnnTileSizeBasedOnVram(int defaultTileSize = 512)
        {
            if(NvApi.NvGpus.Count < 1)
                return defaultTileSize;

            float vram = NvApi.GpuWithMostVram.GetVramGb();
            int tileSize = defaultTileSize;

            if (vram > 5.5f) tileSize = 640; // 6 GB VRAM default
            else if (vram > 7.5f) tileSize = 768; // 8 GB VRAM default
            else if (vram > 11.5f) tileSize = 1024; // 12 GB VRAM default
            else if (vram > 15.5f) tileSize = 1536; // 16 GB VRAM default
            else if (vram > 19.5f) tileSize = 2048; // 20+ GB VRAM default

            Logger.Log($"Using DAIN NCNN tile size {tileSize} for {vram.ToString("0.")} GB GPU", true);

            return tileSize;
        }
    }
}
