using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Os;

namespace Flowframes.Media
{
    class GetVideoInfoCached
    {
        enum InfoType { Ffmpeg, Ffprobe };

        static Dictionary<PseudoUniqueFile, string> ffmpegCache = new Dictionary<PseudoUniqueFile, string>();
        static Dictionary<PseudoUniqueFile, string> ffprobeCache = new Dictionary<PseudoUniqueFile, string>();

        public static async Task<string> GetFfmpegInfoAsync(string path, string lineFilter = "")
        {
            return await GetInfoAsync(path, InfoType.Ffmpeg, lineFilter);
        }

        public static async Task<string> GetFfprobeInfoAsync(string path, string lineFilter = "")
        {
            return await GetInfoAsync(path, InfoType.Ffprobe, lineFilter);
        }

        static async Task<string> GetInfoAsync(string path, InfoType type, string lineFilter)
        {
            Logger.Log($"Get{type}InfoAsync({path})", true);
            Dictionary<PseudoUniqueFile, string> cacheDict = new Dictionary<PseudoUniqueFile, string>(type == InfoType.Ffmpeg ? ffmpegCache : ffprobeCache);
            long filesize = IoUtils.GetFilesize(path);
            PseudoUniqueFile hash = new PseudoUniqueFile(path, filesize);

            if (filesize > 0 && CacheContains(hash, ref cacheDict))
            {
                Logger.Log($"Returning cached {type} info.", true);
                return GetFromCache(hash, ref cacheDict);
            }

            Process process = OsUtils.NewProcess(true);
            string avPath = Path.Combine(Paths.GetPkgPath(), Paths.audioVideoDir);

            if(type == InfoType.Ffmpeg)
                process.StartInfo.Arguments = $"/C cd /D {avPath.Wrap()} & ffmpeg.exe -hide_banner -y -stats -i {path.Wrap()}";

            if (type == InfoType.Ffprobe)
                process.StartInfo.Arguments = $"/C cd /D {avPath.Wrap()} & ffprobe -v quiet -show_format -show_streams {path.Wrap()}";

            string output = await OsUtils.GetOutputAsync(process);

            if (type == InfoType.Ffmpeg)
                ffmpegCache.Add(hash, output);

            if (type == InfoType.Ffprobe)
                ffprobeCache.Add(hash, output);

            if (!string.IsNullOrWhiteSpace(lineFilter.Trim()))
                output = string.Join("\n", output.SplitIntoLines().Where(x => x.Contains(lineFilter)).ToArray());

            return output;
        }

        private static bool CacheContains(PseudoUniqueFile hash, ref Dictionary<PseudoUniqueFile, string> cacheDict)
        {
            foreach (KeyValuePair<PseudoUniqueFile, string> entry in cacheDict)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return true;

            return false;
        }

        private static string GetFromCache(PseudoUniqueFile hash, ref Dictionary<PseudoUniqueFile, string> cacheDict)
        {
            foreach (KeyValuePair<PseudoUniqueFile, string> entry in cacheDict)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize)
                    return entry.Value;

            return "";
        }
    }
}
