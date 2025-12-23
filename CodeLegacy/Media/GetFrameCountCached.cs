using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flowframes.Data;
using Flowframes.IO;

namespace Flowframes.Media
{
    class GetFrameCountCached
    {
        private static Dictionary<QueryInfo, int> cache = new Dictionary<QueryInfo, int>();

        public static async Task<int> GetFrameCountAsync(MediaFile mf, int retryCount = 3)
        {
            return await GetFrameCountAsync(mf.SourcePath, retryCount);
        }

        public static async Task<int> GetFrameCountAsync(string path, int retryCount = 3)
        {
            Logger.Log($"Getting frame count ({path})...", true);

            long filesize = IoUtils.GetPathSize(path);
            QueryInfo hash = new QueryInfo(path, filesize);

            if (filesize > 0 && cache.ContainsKey(hash))
            {
                Logger.Log($"Using cached frame count: {cache[hash]}", true);
                return cache[hash];
            }

            int frameCount;

            if (IoUtils.IsPathDirectory(path))
            {
                frameCount = IoUtils.GetAmountOfFiles(path, false); // Count frames based on image file amount
            }
            else
            {
                if (path.IsConcatFile())
                {
                    var lines = IoUtils.ReadFileLines(path);
                    var filtered = lines.Where(l => l.StartsWith("file '"));
                    frameCount = filtered.Count(); // Count frames from concat file
                }
                else
                    frameCount = await FfmpegCommands.GetFrameCountAsync(path); // Count frames from video stream
            }

            if (frameCount > 0)
            {
                Logger.Log($"Got frame count of {frameCount} (caching)", true);
                cache.Add(hash, frameCount);
            }
            else
            {
                if (retryCount > 0)
                {
                    Logger.Log($"Got {frameCount} frames, retrying ({retryCount} left)", true);
                    Clear();
                    frameCount = await GetFrameCountAsync(path, retryCount - 1);
                }
                else
                {
                    Logger.Log($"Failed to get frames and out of retries ({frameCount} frames for {path})", true);
                }
            }

            return frameCount;
        }

        public static void Clear()
        {
            cache.Clear();
        }
    }
}
