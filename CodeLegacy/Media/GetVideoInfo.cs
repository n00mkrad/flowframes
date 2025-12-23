using System.Collections.Generic;
using System.Diagnostics;
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

        public static async Task<string> GetFfmpegInfoAsync(string path, string lineFilter = "", bool noCache = false)
        {
            return await GetFfmpegOutputAsync(path, "", lineFilter, noCache);
        }

        public static async Task<string> GetFfmpegOutputAsync(string path, string args, string lineFilter = "", bool noCache = false)
        {
            return await GetFfmpegOutputAsync(path, "", args, lineFilter, noCache);
        }

        public static async Task<string> GetFfmpegOutputAsync(string path, string argsIn, string argsOut, string lineFilter = "", bool noCache = false)
        {
            Process process = OsUtils.NewProcess(true);
            process.StartInfo.Arguments = $"/C cd /D {AvProcess.GetAvDir().Wrap()} & " +
                $"ffmpeg.exe -hide_banner -y {argsIn} {path.GetConcStr()} -i {path.Wrap()} {argsOut}";
            return await GetInfoAsync(path, process, lineFilter, noCache);
        }

        public static async Task<string> GetFfprobeInfoAsync(string path, FfprobeMode mode, string lineFilter = "", int streamIndex = -1, bool stripKeyName = true)
        {
            Process process = OsUtils.NewProcess(true);
            string showFormat = mode == FfprobeMode.ShowBoth || mode == FfprobeMode.ShowFormat ? "-show_format" : "";
            string showStreams = mode == FfprobeMode.ShowBoth || mode == FfprobeMode.ShowStreams ? "-show_streams" : "";

            process.StartInfo.Arguments = $"/C cd /D {AvProcess.GetAvDir().Wrap()} & " +
                $"ffprobe -v quiet {path.GetConcStr()} {showFormat} {showStreams} {path.Wrap()}";

            string output = await GetInfoAsync(path, process, lineFilter, streamIndex, stripKeyName);

            return output;
        }

        static async Task<string> GetInfoAsync(string path, Process process, string lineFilter, bool noCache = false) // for ffmpeg
        {
            string output = await GetOutputCached(path, process, noCache);

            if (!string.IsNullOrWhiteSpace(lineFilter.Trim()))
                output = string.Join("\n", output.SplitIntoLines().Where(x => x.Contains(lineFilter)).ToArray());

            return output;
        }

        static async Task<string> GetInfoAsync(string path, Process process, string lineFilter, int streamIndex = -1, bool stripKeyName = true, bool noCache = false) // for ffprobe
        {
            string output = await GetOutputCached(path, process, noCache);

            try
            {
                if (streamIndex >= 0)
                    output = output.Split("[/STREAM]")[streamIndex];
            }
            catch
            {
                Logger.Log($"output.Split(\"[/STREAM]\")[{streamIndex}] failed! Can't access index {streamIndex} because array only has {output.Split("[/STREAM]").Length} items.", true);
                return "";
            }

            if (!string.IsNullOrWhiteSpace(lineFilter.Trim()))
            {
                if (stripKeyName)
                {

                    List<string> filtered = output.SplitIntoLines().Where(x => x.Lower().Contains(lineFilter.Lower())).ToList();    // Filter
                    filtered = filtered.Select(x => string.Join("", x.Split('=').Skip(1))).ToList();    // Ignore everything before (and including) the first '=' sign
                    output = string.Join("\n", filtered);
                }
                else
                {
                    output = string.Join("\n", output.SplitIntoLines().Where(x => x.Lower().Contains(lineFilter.Lower())).ToArray());
                }
            }

            return output;
        }

        static async Task<string> GetOutputCached(string path, Process process, bool noCache = false)
        {
            long filesize = IoUtils.GetPathSize(path);
            QueryInfo hash = new QueryInfo(path, filesize, process.StartInfo.Arguments);

            if (!noCache && filesize > 0 && cmdCache.ContainsKey(hash))
                return cmdCache[hash];

            Logger.Log($"GetVideoInfo: '{path}' (not cached)", true, false, "ffmpeg");
            string output = await OsUtils.GetOutputAsync(process);
            cmdCache.Add(hash, output);
            return output;
        }

        public static void ClearCache()
        {
            cmdCache.Clear();
        }
    }
}
