using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Flowframes.Data;
using Flowframes.IO;

namespace Flowframes.Media
{
    class GetMediaResolutionCached
    {
        private static Dictionary<QueryInfo, Size> cache = new Dictionary<QueryInfo, Size>();

        public static async Task<Size> GetSizeAsync(string path)
        {
            Logger.Log($"Getting media resolution ({path})", true);

            long filesize = IoUtils.GetPathSize(path);
            QueryInfo hash = new QueryInfo(path, filesize);

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
            size = await IoUtils.GetVideoOrFramesRes(path);

            if(size.Width > 0 && size.Height > 0)
            {
                Logger.Log($"Adding hash with value {size} to cache.", true);
                cache.Add(hash, size);
            }

            return size;
        }

        private static bool CacheContains(QueryInfo hash)
        {
            foreach (KeyValuePair<QueryInfo, Size> entry in cache)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return true;

            return false;
        }

        private static Size GetFromCache(QueryInfo hash)
        {
            foreach (KeyValuePair<QueryInfo, Size> entry in cache)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return entry.Value;

            return new Size();
        }

        public static void Clear()
        {
            cache.Clear();
        }
    }
}
