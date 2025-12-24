using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.AvProcess;

namespace Flowframes
{
    class FfmpegCommands
    {
        public static string hdrFilter = @"-vf zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap=tonemap=hable:desat=0,zscale=t=bt709:m=bt709:r=tv,format=yuv420p";
        public static string pngCompr = "-compression_level 3";

        /// <summary> Lookup table for mpdecimate sensitivity preset values (lo/hi/frac). </summary>
        public static Dictionary<Enums.Interpolation.MpDecimateSens, (int, int, float)> MpDecSensLookup = new Dictionary<Enums.Interpolation.MpDecimateSens,(int, int, float)>
        {
            { Enums.Interpolation.MpDecimateSens.Low,      (3,  10, 0.33f) },
            { Enums.Interpolation.MpDecimateSens.Normal,   (4,  12, 0.50f) },
            { Enums.Interpolation.MpDecimateSens.High,     (20, 18, 0.65f) },
            { Enums.Interpolation.MpDecimateSens.VeryHigh, (32, 24, 0.75f) },
            { Enums.Interpolation.MpDecimateSens.Extreme,  (40, 30, 0.90f) },
        };

        /// <summary>
        /// Construct mpdecimate filter with prefiltering: <paramref name="scaleSize"/> limits resolution, <paramref name="cropSize"/> center-crops the frame using a factor,
        /// <paramref name="lumaOnly"/> only processes luma channel.<br/><paramref name="wrap"/> wraps the filter in double quotes.
        /// </summary>
        public static string GetMpdecimate(bool wrap = true, int scaleSize = 640, float cropSize = 0.8f, bool lumaOnly = true)
        {
            int mpdValIndex = Config.GetInt(Config.Key.mpdecimateMode);
            (int lo, int hi, float frac) = MpDecSensLookup[(Enums.Interpolation.MpDecimateSens)mpdValIndex];
            string format = lumaOnly ? $"format=yuv420p,extractplanes=y" : "";
            string scale = scaleSize > 64 ? $"scale='min({scaleSize},iw)':min'({scaleSize},ih)':force_original_aspect_ratio=decrease:force_divisible_by=2" : "";
            string crop = cropSize > 0.1f && cropSize < 0.99f ? $"crop=iw*{cropSize}:ih*{cropSize}" : "";
            string mpdec = $"mpdecimate=hi=64*{hi}:lo=64*{lo}:frac={frac.ToString("0.0#")}";
            string filters = string.Join(",", new string[] { format, crop, scale, mpdec }.Where(s => s.IsNotEmpty())); // Only take non-empty filter strings
            return wrap ? filters.Wrap() : filters;
        }

        public enum ModuloMode { Disabled, ForInterpolation, ForEncoding }

        public static int GetModulo(ModuloMode mode)
        {
            if (mode == ModuloMode.ForEncoding)
            {
                string pixFmt = Interpolate.currentSettings.outSettings.PixelFormat.ToString().Lower();
                bool subsampled = pixFmt.Contains("420") || pixFmt.Contains("422") || pixFmt.Contains("p010") || pixFmt.Contains("p016");
                return subsampled ? 2 : 1;
            }
            else if (mode == ModuloMode.ForInterpolation)
            {
                bool RifeNeedsPadding(string ver) => ver.Split('.').Last().GetInt() >= 25; // e.g. "RIFE 4.25" needs padding

                if (Interpolate.currentSettings.model.Name.Contains("RIFE") && RifeNeedsPadding(Interpolate.currentSettings.model.Name))
                    return 64;

                if (Interpolate.currentSettings.ai == Implementations.flavrCuda)
                    return 8;
            }

            return 1;
        }

        public static string GetPadFilter(int width = -1, int height = -1)
        {
            int mod = GetModulo(ModuloMode.ForEncoding);

            if (mod < 2)
                return "";

            if (width > 0 && width % mod == 0 && height > 0 && height % mod == 0)
                return "";

            return $"pad=width=ceil(iw/{mod})*{mod}:height=ceil(ih/{mod})*{mod}:color=black@0";
        }

        public static async Task ConcatVideos(string concatFile, string outPath, int looptimes = -1, bool showLog = true)
        {
            Logger.Log($"ConcatVideos('{Path.GetFileName(concatFile)}', '{outPath}', {looptimes})", true, false, "ffmpeg");

            if (showLog)
                Logger.Log($"Merging videos...", false, Logger.GetLastLine().Contains("frame"));

            IoUtils.RenameExistingFileOrDir(outPath);
            string loopStr = (looptimes > 0) ? $"-stream_loop {looptimes}" : "";
            string vfrFilename = Path.GetFileName(concatFile);
            string args = $" {loopStr} -f concat -i {vfrFilename} -fps_mode cfr -c copy -movflags +faststart -fflags +genpts {outPath.Wrap()}";
            await RunFfmpeg(args, concatFile.GetParentDir(), LogMode.Hidden);
        }

        public static async Task LoopVideo(string inputFile, int times, bool delSrc = false)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            string loopSuffix = Config.Get(Config.Key.exportNamePatternLoop).Replace("[LOOPS]", $"{times}").Replace("[PLAYS]", $"{times + 1}");
            string outpath = $"{pathNoExt}{loopSuffix}{ext}";
            IoUtils.RenameExistingFileOrDir(outpath);
            string args = $" -stream_loop {times} -i {inputFile.Wrap()} -map 0 -c copy {outpath.Wrap()}";
            await RunFfmpeg(args, LogMode.Hidden);

            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task<long> GetDurationMs(string inputFile, MediaFile mediaFile, bool demuxInsteadOfPacketTs = false, bool allowDurationFromMetadata = true)
        {
            if (mediaFile.IsDirectory)
                return 0;

            if (allowDurationFromMetadata)
            {
                Logger.Log($"[{nameof(GetDurationMs)}] Reading duration by checking metadata", true, false, "ffmpeg");
                string argsMeta = $"ffprobe -v quiet -show_streams -select_streams v:0 -show_entries stream=duration {inputFile.Wrap()}";
                var outputLinesMeta = NUtilsTemp.OsUtils.RunCommand($"cd /D {GetAvDir().Wrap()} && {argsMeta}").SplitIntoLines();

                foreach (string line in outputLinesMeta.Where(l => l.MatchesWildcard("*?=?*")))
                {
                    var split = line.Split('=');
                    string key = split[0];
                    string value = split[1];

                    if (value.Contains("N/A"))
                        continue;

                    if (key == "duration")
                    {
                        return (long)TimeSpan.FromSeconds(value.GetFloat()).TotalMilliseconds;
                    }
                    else if (key == "TAG:DURATION")
                    {
                        string[] tsSplit = value.Split(':');
                        int hours = tsSplit[0].GetInt();
                        int minutes = tsSplit[1].GetInt();
                        float seconds = tsSplit[2].GetFloat();
                        return (long)TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes)).Add(TimeSpan.FromSeconds(seconds)).TotalMilliseconds;
                    }
                }
            }

            if (demuxInsteadOfPacketTs)
            {
                Logger.Log($"[{nameof(GetDurationMs)}] Reading duration by demuxing", true, false, "ffmpeg");
                string argsDemux = $"ffmpeg -loglevel panic -stats -i {inputFile.Wrap()} -map 0:v:0 -c copy -f null NUL";
                var outputLinesDemux = NUtilsTemp.OsUtils.RunCommand($"cd /D {GetAvDir().Wrap()} && {argsDemux}").SplitIntoLines().Where(l => l.IsNotEmpty() && l.MatchesWildcard("*time=* *"));

                if (outputLinesDemux == null || outputLinesDemux.Count() == 0)
                    return 0;

                string output = outputLinesDemux.Last().Split("time=")[1].Split(" ")[0];
                return (long)TimeSpan.ParseExact(output, @"hh\:mm\:ss\.ff", null).TotalMilliseconds;
            }
            else
            {
                Logger.Log($"[{nameof(GetDurationMs)}] Reading duration using packet timestamps", true, false, "ffmpeg");
                string argsPackets = $"ffprobe -v error  -select_streams v:0 -show_packets -show_entries packet=pts_time -of csv=p=0 {inputFile.Wrap()}";
                var outputLinesPackets = NUtilsTemp.OsUtils.RunCommand($"cd /D {GetAvDir().Wrap()} && {argsPackets}").SplitIntoLines().Where(l => l.IsNotEmpty()).ToList();

                if (outputLinesPackets == null || outputLinesPackets.Count == 0)
                    return 0;

                CheckVfr(inputFile, mediaFile);
                string lastTimestamp = outputLinesPackets.Last().Split('=').Last();
                return (long)TimeSpan.FromSeconds(lastTimestamp.GetFloat()).TotalMilliseconds;
            }
        }

        public static void CheckVfr(string inputFile, MediaFile mediaFile, List<string> outputLinesPackets = null)
        {
            if (mediaFile.InputTimestamps.Any())
                return;

            Logger.Log($"Checking frame timing...", true, false, "ffmpeg");

            if (outputLinesPackets == null)
            {
                string argsPackets = $"ffprobe -v error  -select_streams v:0 -show_packets -show_entries packet=pts_time -read_intervals \"%+120\" -of csv=p=0 {inputFile.Wrap()}";
                outputLinesPackets = NUtilsTemp.OsUtils.RunCommand($"cd /D {GetAvDir().Wrap()} && {argsPackets}").SplitIntoLines().Where(l => l.IsNotEmpty()).ToList();
            }

            var timestamps = new List<float>();
            var timestampDurations = new List<float>();
            var timestampDurationsRes = new List<float>();

            foreach (string line in outputLinesPackets)
            {
                timestamps.Add(line.GetFloat());
            }

            timestamps = timestamps.OrderBy(x => x).ToList();

            for (int i = 1; i < (timestamps.Count - 1); i++)
            {
                float diff = Math.Abs(timestamps[i] - timestamps[i - 1]);
                timestampDurations.Add(diff);

                // if (diff > 0)
                // {
                //     Console.WriteLine($"Duration of {timestamps.Count}: {diff * 1000f} ms ({1f / diff} FPS)");
                // }
            }
            // 
            // var tsResampleTest = Interpolate.currentMediaFile.ResampleTimestamps(timestamps, 2.5f);
            // 
            // for (int i = 1; i < (tsResampleTest.Count - 1); i++)
            // {
            //     float diff = Math.Abs(tsResampleTest[i] - tsResampleTest[i - 1]);
            //     timestampDurationsRes.Add(diff);
            // 
            //     if (diff > 0)
            //     {
            //         Console.WriteLine($"Resampled duration of {tsResampleTest.Count}: {diff * 1000f} ms ({1f / diff} FPS)");
            //     }
            // }

            mediaFile.InputTimestamps = new List<float>(timestamps);

            float avgDuration = timestampDurations.Average();
            float maxDeviationMs = (timestampDurations.Max() - timestampDurations.Min()) * 1000f;
            float maxDeviationPercent = ((timestampDurations.Max() / timestampDurations.Min()) * 100f) - 100;
            // float maxDeviationMsResampled = (timestampDurationsRes.Max() - timestampDurationsRes.Min()) * 1000f;
            Logger.Log($"[VFR Check] Timestamp durations - Min: {timestampDurations.Min() * 1000f} ms - Max: {timestampDurations.Max() * 1000f} ms - Avg: {avgDuration * 1000f} - Biggest deviation: {maxDeviationMs.ToString("0.##")} ms", hidden: true);
            // Logger.Log($"Resampled - Min ts duration: {timestampDurationsRes.Min() * 1000f} ms - Max ts duration: {timestampDurationsRes.Max() * 1000f} ms - Biggest deviation: {maxDeviationMsResampled.ToString("0.##")} ms", hidden: true);

            mediaFile.InputTimestampDurations = new List<float>(timestampDurations);

            if(Config.GetInt(Config.Key.vfrHandling) == 1)
            {
                Logger.Log($"Ignoring VFR deviation threshold of {maxDeviationPercent.ToString("0.##")}%, force-enabling VFR mode due to settings");
                mediaFile.IsVfr = true;
                return;
            }
            else if (Config.GetInt(Config.Key.vfrHandling) == 2)
            {
                Logger.Log($"Ignoring VFR deviation threshold of {maxDeviationPercent.ToString("0.##")}%, force-disabling VFR mode due to settings");
                mediaFile.IsVfr = false;
                return;
            }

            if (maxDeviationPercent > 20f)
            {
                Logger.Log($"[VFR Check] Max timestamp deviation is {maxDeviationPercent.ToString("0.##")}% or {maxDeviationMs} ms - Assuming VFR input!", hidden: true);
                mediaFile.IsVfr = true;
            }
        }

        public static async Task<Fraction> GetFramerate(string inputFile, bool preferFfmpeg = false)
        {
            Logger.Log($"Getting FPS from '{inputFile}', preferFfmpeg = {preferFfmpeg}", true, false, "ffmpeg");
            Fraction ffprobeFps = new Fraction(0, 1);
            Fraction ffmpegFps = new Fraction(0, 1);

            try
            {
                string ffprobeOutput = await GetVideoInfo.GetFfprobeInfoAsync(inputFile, GetVideoInfo.FfprobeMode.ShowStreams, "r_frame_rate");
                string fpsStr = ffprobeOutput.SplitIntoLines().First();
                string[] numbers = fpsStr.Split('/');
                Logger.Log($"Fractional FPS from ffprobe: {numbers[0]}/{numbers[1]} = {((float)numbers[0].GetInt() / numbers[1].GetInt())}", true, false, "ffmpeg");
                ffprobeFps = new Fraction(numbers[0].GetInt(), numbers[1].GetInt());
            }
            catch (Exception ffprobeEx)
            {
                Logger.Log("GetFramerate ffprobe Error: " + ffprobeEx.Message, true, false);
            }

            try
            {
                string ffmpegOutput = await GetVideoInfo.GetFfmpegInfoAsync(inputFile);
                string[] entries = ffmpegOutput.Split(',');

                foreach (string entry in entries)
                {
                    if (entry.Contains(" fps") && !entry.Contains("Input "))    // Avoid reading FPS from the filename, in case filename contains "fps"
                    {
                        string num = entry.Replace(" fps", "").Trim().Replace(",", ".");
                        Logger.Log($"Float FPS from ffmpeg: {num.GetFloat()}", true, false, "ffmpeg");
                        ffmpegFps = new Fraction(num.GetFloat());
                    }
                }
            }
            catch (Exception ffmpegEx)
            {
                Logger.Log("GetFramerate ffmpeg Error: " + ffmpegEx.Message, true, false);
            }

            if (preferFfmpeg)
            {
                if (ffmpegFps.Float > 0)
                    return ffmpegFps;
                else
                    return ffprobeFps;
            }
            else
            {
                if (ffprobeFps.Float > 0)
                    return ffprobeFps;
                else
                    return ffmpegFps;
            }
        }

        public static Size GetSize(string inputFile)
        {
            string args = $" -v panic -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {inputFile.Wrap()}";
            string[] outputLines = GetFfprobeOutput(args).SplitIntoLines();

            foreach (string line in outputLines)
            {
                if (!line.Contains("x") || line.Trim().Length < 3)
                    continue;

                string[] numbers = line.Split('x');
                return new Size(numbers[0].GetInt(), numbers[1].GetInt());
            }

            return new Size(0, 0);
        }

        public static async Task<int> GetFrameCountAsync(string inputFile)
        {
            Logger.Log($"GetFrameCountAsync - Trying ffprobe packet counting first (fastest).", true, false, "ffmpeg");
            int frames = await ReadFrameCountFfprobePacketCount(inputFile);      // Try reading frame count with ffprobe packet counting
            if (frames > 0) return frames;

            Logger.Log($"GetFrameCountAsync - Trying ffmpeg demuxing.", true, false, "ffmpeg");
            frames = await ReadFrameCountFfmpegAsync(inputFile);       // Try reading frame count with ffmpeg
            if (frames > 0) return frames;

            Logger.Log($"GetFrameCountAsync - Trying ffprobe demuxing.", true, false, "ffmpeg");
            frames = await ReadFrameCountFfprobe(inputFile);      // Try reading frame count with ffprobe decoding
            if (frames > 0) return frames;



            Logger.Log("Failed to get total frame count of video.", true);
            return 0;
        }

        static int ReadFrameCountFromDuration(string inputFile, long durationMs, float fps)
        {
            float durationSeconds = durationMs / 1000f;
            float frameCount = durationSeconds * fps;
            int frameCountRounded = frameCount.RoundToInt();
            Logger.Log($"ReadFrameCountFromDuration: Got frame count of {frameCount}, rounded to {frameCountRounded}");
            return frameCountRounded;
        }

        public static async Task<int> ReadFrameCountFfprobePacketCount(string filePath)
        {
            string args = $"ffprobe -v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of csv=p=0 {filePath.Wrap()}";
            var outputLines = (await Task.Run(() => NUtilsTemp.OsUtils.RunCommand($"cd /D {GetAvDir().Wrap()} && {args}"))).SplitIntoLines().Where(l => l.IsNotEmpty());

            if (outputLines == null || !outputLines.Any())
                return 0;

            return outputLines.Last().GetInt();
        }

        public static async Task<int> ReadFrameCountFfprobe(string filePath)
        {
            FfprobeSettings s = new FfprobeSettings() { Args = $"-threads 0 -select_streams v:0 -show_entries stream=nb_frames -of default=noprint_wrappers=1 {filePath.Wrap()}", LoggingMode = LogMode.Hidden, LogLevel = "panic" };
            string info = await RunFfprobe(s);
            string[] entries = info.SplitIntoLines();

            try
            {
                foreach (string entry in entries)
                {
                    if (entry.Contains("nb_frames="))
                        return entry.GetInt();
                }
            }
            catch { }

            return -1;
        }

        public static async Task<int> ReadFrameCountFfmpegAsync(string filePath)
        {
            string args = $"{filePath.GetConcStr()} -i {filePath.Wrap()} -map 0:v:0 -c copy -f null - ";
            string info = await RunFfmpeg(args, LogMode.Hidden, "panic");
            try
            {
                string[] lines = info.SplitIntoLines();
                string lastLine = lines.Last().Lower();
                return lastLine.Substring(0, lastLine.IndexOf("fps")).GetInt();
            }
            catch
            {
                return -1;
            }
        }

        public static async Task<VidExtraData> GetVidExtraInfo(string inputFile)
        {
            try
            {
                string ffprobeOutput = await GetVideoInfo.GetFfprobeInfoAsync(inputFile, GetVideoInfo.FfprobeMode.ShowBoth);
                VidExtraData data = new VidExtraData(ffprobeOutput);
                return data;
            }
            catch
            {
                return new VidExtraData();
            }
        }

        public static async Task<bool> IsEncoderCompatible(string enc)
        {
            if (!File.Exists(Path.Combine(AvProcess.GetAvDir(), "ffmpeg.exe")))
            {
                Logger.Log($"Can't check encoder '{enc}', ffmpeg not found!", true, false, "ffmpeg");
                return false;
            }

            Logger.Log($"Running ffmpeg to check if encoder '{enc}' is available...", true, false, "ffmpeg");
            string args = $"-loglevel error -f lavfi -i color=black:s=1920x1080 -vframes 1 -c:v {enc} -f null -";
            string output = await RunFfmpeg(args, LogMode.Hidden);
            bool compat = !output.SplitIntoLines().Where(l => !l.Lower().StartsWith("frame") && l.IsNotEmpty()).Any();
            Logger.Log($"Encoder '{enc}' is {(compat ? "available!" : "not available.")}", true, false, "ffmpeg");
            return compat;
        }

        public static List<string> GetAudioCodecs(string path, int streamIndex = -1)
        {
            Logger.Log($"GetAudioCodecs('{Path.GetFileName(path)}', {streamIndex})", true, false, "ffmpeg");
            List<string> codecNames = new List<string>();
            string args = $"-loglevel panic -select_streams a -show_entries stream=codec_name {path.Wrap()}";
            string info = GetFfprobeOutput(args);
            string[] entries = info.SplitIntoLines();

            foreach (string entry in entries)
            {
                if (entry.Contains("codec_name="))
                    codecNames.Add(entry.Remove("codec_name=").Trim());
            }

            return codecNames;
        }

        public static void DeleteSource(string path)
        {
            Logger.Log("[FFCmds] Deleting input file/dir: " + path, true);

            if (IoUtils.IsPathDirectory(path) && Directory.Exists(path))
                Directory.Delete(path, true);

            if (!IoUtils.IsPathDirectory(path) && File.Exists(path))
                File.Delete(path);
        }
    }
}
