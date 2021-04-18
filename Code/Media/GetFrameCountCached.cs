using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Flowframes.Data;
using Flowframes.IO;

namespace Flowframes.Media
{
    class GetFrameCountCached
    {
        public static Dictionary<PseudoUniqueFile, int> cache = new Dictionary<PseudoUniqueFile, int>();

        public static async Task<int> GetFrameCountAsync(string path)
        {
            Logger.Log($"Getting frame count ({path})", true);

            long filesize = IOUtils.GetFilesize(path);
            PseudoUniqueFile hash = new PseudoUniqueFile(path, filesize);

            if (filesize > 0 && CacheContains(hash))
            {
                Logger.Log($"Cache contains this hash, using cached value.", true);
                return GetFromCache(hash);
            }
            else
            {
                Logger.Log($"Hash not cached, reading frame count.", true);
            }

            int frameCount;

            if (IOUtils.IsPathDirectory(path))
                frameCount = IOUtils.GetAmountOfFiles(path, false);
            else
                frameCount = await FfmpegCommands.GetFrameCountAsync(path);

            Logger.Log($"Adding hash with value {frameCount} to cache.", true);
            cache.Add(hash, frameCount);

            return frameCount;
        }

        private static bool CacheContains (PseudoUniqueFile hash)
        {
            foreach(KeyValuePair<PseudoUniqueFile, int> entry in cache)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return true;

            return false;
        }

        private static int GetFromCache(PseudoUniqueFile hash)
        {
            foreach (KeyValuePair<PseudoUniqueFile, int> entry in cache)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return entry.Value;

            return 0;
        }
    }
}
