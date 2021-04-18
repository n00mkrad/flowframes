using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Flowframes.Data;
using Flowframes.IO;

namespace Flowframes.Media
{
    class GetMediaResolutionCached
    {
        public static Dictionary<PseudoUniqueFile, Size> cache = new Dictionary<PseudoUniqueFile, Size>();

        public static async Task<Size> GetSizeAsync(string path)
        {
            Logger.Log($"Getting media resolution ({path})", true);

            long filesize = IOUtils.GetFilesize(path);
            PseudoUniqueFile hash = new PseudoUniqueFile(path, filesize);

            if (filesize > 0 && CacheContains(hash))
            {
                Logger.Log($"Cache contains this hash, using cached value.", true);
                return GetFromCache(hash);
            }
            else
            {
                Logger.Log($"Hash not cached, reading resolution.", true);
            }

            Size size;

            size = await IOUtils.GetVideoOrFramesRes(path);

            Logger.Log($"Adding hash with value {size} to cache.", true);
            cache.Add(hash, size);

            return size;
        }

        private static bool CacheContains(PseudoUniqueFile hash)
        {
            foreach (KeyValuePair<PseudoUniqueFile, Size> entry in cache)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return true;

            return false;
        }

        private static Size GetFromCache(PseudoUniqueFile hash)
        {
            foreach (KeyValuePair<PseudoUniqueFile, Size> entry in cache)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return entry.Value;

            return new Size();
        }
    }
}
