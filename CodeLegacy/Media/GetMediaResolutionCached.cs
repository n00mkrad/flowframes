using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            long filesize = IoUtils.GetPathSize(path);
            QueryInfo hash = new QueryInfo(path, filesize);

            if (filesize > 0 && cache.ContainsKey(hash))
            {
                Size cachedVal = cache[hash];
                Logger.Log($"Resolution of '{Path.GetFileName(path)}': {cachedVal.Width}x{cachedVal.Height} [Cached]", true);
                return cachedVal;
            }

            Size size;
            size = await IoUtils.GetVideoOrFramesRes(path);

            if(size.Width > 0 && size.Height > 0)
            {
                cache.Add(hash, size);
            }

            Logger.Log($"Resolution of '{Path.GetFileName(path)}': {size.Width}x{size.Height}", true);
            return size;
        }

        public static void Clear()
        {
            cache.Clear();
        }
    }
}
