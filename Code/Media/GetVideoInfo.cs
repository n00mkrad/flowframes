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
    class GetVideoInfo
    {
        enum InfoType { Ffmpeg, Ffprobe };
        public enum FfprobeMode { ShowFormat, ShowStreams, ShowBoth };

        static Dictionary<QueryInfo, string> cmdCache = new Dictionary<QueryInfo, string>();

        private static string GetAvPath()
        {
            return Path.Combine(Paths.GetPkgPath(), Paths.audioVideoDir);
        }

        public static async Task<string> GetFfmpegInfoAsync(string path, string lineFilter = "")
        {
            return await GetFfmpegOutputAsync(path, "", lineFilter);
        }

        public static async Task<string> GetFfmpegOutputAsync(string path, string args, string lineFilter = "")
        {
            Process process = OsUtils.NewProcess(true);
            process.StartInfo.Arguments = $"/C cd /D {GetAvPath().Wrap()} & ffmpeg.exe -hide_banner -y -stats -i {path.Wrap()} {args}";
            return await GetInfoAsync(path, process, lineFilter, false);
        }

        public static async Task<string> GetFfprobeInfoAsync(string path, FfprobeMode mode, string lineFilter = "", int streamIndex = -1, bool stripKeyName = true)
        {
            Process process = OsUtils.NewProcess(true);
            string showFormat = mode == FfprobeMode.ShowBoth || mode == FfprobeMode.ShowFormat ? "-show_format" : "";
            string showStreams = mode == FfprobeMode.ShowBoth || mode == FfprobeMode.ShowStreams ? "-show_streams" : "";
            string streamSelect = (streamIndex >= 0) ? $"-select_streams {streamIndex}" : "";
            process.StartInfo.Arguments = $"/C cd /D {GetAvPath().Wrap()} & ffprobe -v quiet {showFormat} {showStreams} {streamSelect} {path.Wrap()}";
            return await GetInfoAsync(path, process, lineFilter, stripKeyName);
        }

        static async Task<string> GetInfoAsync(string path, Process process, string lineFilter, bool stripKeyName = true)
        {
            string output = await GetOutputCached(path, process);

            if (!string.IsNullOrWhiteSpace(lineFilter.Trim()))
            {
                if (stripKeyName)
                {
                    List<string> filtered = output.SplitIntoLines().Where(x => x.Contains(lineFilter)).ToList();    // Filter
                    filtered = filtered.Select(x => string.Join("", x.Split('=').Skip(1))).ToList();    // Ignore everything before (and including) the first '=' sign
                    output = string.Join("\n", filtered);
                }
                else
                {
                    output = string.Join("\n", output.SplitIntoLines().Where(x => x.Contains(lineFilter)).ToArray());
                }
            }


            return output;
        }

        static async Task<string> GetOutputCached(string path, Process process)
        {
            long filesize = IoUtils.GetFilesize(path);
            QueryInfo hash = new QueryInfo(path, filesize, process.StartInfo.Arguments);

            if (filesize > 0 && CacheContains(hash, ref cmdCache))
            {
                Logger.Log($"GetVideoInfo: '{process.StartInfo.FileName} {process.StartInfo.Arguments}' cached, won't re-run.", true, false, "ffmpeg");
                return GetFromCache(hash, ref cmdCache);
            }

            Logger.Log($"GetVideoInfo: '{process.StartInfo.FileName} {process.StartInfo.Arguments}' not cached, running.", true, false, "ffmpeg");
            string output = await OsUtils.GetOutputAsync(process);
            cmdCache.Add(hash, output);
            return output;
        }

        private static bool CacheContains(QueryInfo hash, ref Dictionary<QueryInfo, string> cacheDict)
        {
            foreach (KeyValuePair<QueryInfo, string> entry in cacheDict)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize && entry.Key.cmd == hash.cmd)
                    return true;

            return false;
        }

        private static string GetFromCache(QueryInfo hash, ref Dictionary<QueryInfo, string> cacheDict)
        {
            foreach (KeyValuePair<QueryInfo, string> entry in cacheDict)
                if (entry.Key.path == hash.path && entry.Key.filesize == hash.filesize && entry.Key.cmd == hash.cmd)
                    return entry.Value;

            return "";
        }
    }
}
