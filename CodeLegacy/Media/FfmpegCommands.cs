using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
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

        public enum MpDecSensitivity { Normal = 4, High = 20, VeryHigh = 32, Extreme = 40 }
        
        public static string GetMpdecimate (int sensitivity = 4, bool wrap = true)
        {
            string mpd = $"mpdecimate=hi=64*1024:lo=64*{sensitivity}:frac=1.0";
            return wrap ? mpd.Wrap() : mpd;
        }

        public static string GetMpdecimate(bool wrap = true)
        {
            int mpdValIndex = Config.GetInt(Config.Key.mpdecimateMode);
            var mpdVal = ((MpDecSensitivity[])Enum.GetValues(typeof(MpDecSensitivity)))[mpdValIndex];
            string mpd = $"mpdecimate=hi=64*1024:lo=64*{(int)mpdVal}:frac=1.0";
            return wrap ? mpd.Wrap() : mpd;
        }

        public static int GetModulo ()
        {
            if (Interpolate.currentSettings.ai.NameInternal == Implementations.flavrCuda.NameInternal)
                return 8;

            return Interpolate.currentSettings.outSettings.Encoder.GetInfo().Modulo;
        }

        public static string GetPadFilter ()
        {
            int mod = GetModulo();

            if (mod < 2)
                return "";

            return $"pad=width=ceil(iw/{mod})*{mod}:height=ceil(ih/{mod})*{mod}:color=black@0";
        }

        public static async Task ConcatVideos(string concatFile, string outPath, int looptimes = -1, bool showLog = true)
        {
            Logger.Log($"ConcatVideos('{Path.GetFileName(concatFile)}', '{outPath}', {looptimes})", true, false, "ffmpeg");

            if(showLog)
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
            string args = $" -stream_loop {times} -i {inputFile.Wrap()} -c copy {outpath.Wrap()}";
            await RunFfmpeg(args, LogMode.Hidden);

            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ChangeSpeed(string inputFile, float newSpeedPercent, bool delSrc = false)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            float val = newSpeedPercent / 100f;
            string speedVal = (1f / val).ToString("0.0000").Replace(",", ".");
            string args = " -itsscale " + speedVal + " -i \"" + inputFile + "\" -c copy \"" + pathNoExt + "-" + newSpeedPercent + "pcSpeed" + ext + "\"";
            await RunFfmpeg(args, LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task<long> GetDurationMs(string inputFile)
        {
            Logger.Log($"GetDuration({inputFile}) - Reading Duration using ffprobe.", true, false, "ffmpeg");
            string args = $"-select_streams v:0 -show_entries format=duration -of csv=s=x:p=0 -sexagesimal {inputFile.Wrap()}";
            FfprobeSettings settings = new FfprobeSettings() { Args = args };
            string output = await RunFfprobe(settings);

            return FormatUtils.TimestampToMs(output);
        }

        public static async Task<Fraction> GetFramerate(string inputFile, bool preferFfmpeg = false)
        {
            Logger.Log($"GetFramerate(inputFile = '{inputFile}', preferFfmpeg = {preferFfmpeg})", true, false, "ffmpeg");
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
            catch(Exception ffmpegEx)
            {
                Logger.Log("GetFramerate ffmpeg Error: " + ffmpegEx.Message, true, false);
            }

            if (preferFfmpeg)
            {
                if (ffmpegFps.GetFloat() > 0)
                    return ffmpegFps;
                else
                    return ffprobeFps;
            }
            else
            {
                if (ffprobeFps.GetFloat() > 0)
                    return ffprobeFps;
                else
                    return ffmpegFps;
            }
        }

        public static Size GetSize(string inputFile)
        {
            Logger.Log($"GetSize('{inputFile}')", true, false, "ffmpeg");
            string args = $" -v panic -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {inputFile.Wrap()}";
            string[] outputLines = GetFfprobeOutput(args).SplitIntoLines();

            foreach(string line in outputLines)
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
            FfprobeSettings settings = new FfprobeSettings() { Args = $"-select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of csv=p=0 {filePath.Wrap()}", LoggingMode = LogMode.Hidden, LogLevel = "error" };
            string output = await RunFfprobe(settings);
            string[] lines = output.SplitIntoLines().Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            if (lines == null || lines.Length < 1)
                return 0;

            return lines.Last().GetInt();
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
            string ffprobeOutput = await GetVideoInfo.GetFfprobeInfoAsync(inputFile, GetVideoInfo.FfprobeMode.ShowBoth);
            VidExtraData data = new VidExtraData(ffprobeOutput);
            return data;
        }

        public static async Task<bool> IsEncoderCompatible(string enc)
        {
            Logger.Log($"IsEncoderCompatible('{enc}')", true, false, "ffmpeg");
            string args = $"-loglevel error -f lavfi -i color=black:s=1920x1080 -vframes 1 -c:v {enc} -f null -";
            string output = await RunFfmpeg(args, LogMode.Hidden);
            return !output.SplitIntoLines().Where(l => !l.Lower().StartsWith("frame") && l.IsNotEmpty()).Any();
        }

        public static string GetAudioCodec(string path, int streamIndex = -1)
        {
            Logger.Log($"GetAudioCodec('{Path.GetFileName(path)}', {streamIndex})", true, false, "ffmpeg");
            string stream = (streamIndex < 0) ? "a" : $"{streamIndex}";
            string args = $"-v panic -show_streams -select_streams {stream} -show_entries stream=codec_name {path.Wrap()}";
            string info = GetFfprobeOutput(args);
            string[] entries = info.SplitIntoLines();

            foreach (string entry in entries)
            {
                if (entry.Contains("codec_name="))
                    return entry.Split('=')[1];
            }
            return "";
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
