using Flowframes.IO;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Flowframes
{
    class FFmpegCommands
    {
        static string hdrFilter = @"-vf select=gte(n\,%frNum%),zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap=tonemap=hable:desat=0,zscale=t=bt709:m=bt709:r=tv,format=yuv420p";

        static string videoEncArgs = "-pix_fmt yuv420p -movflags +faststart -vf \"crop = trunc(iw / 2) * 2:trunc(ih / 2) * 2\"";
        static string divisionFilter = "\"crop = trunc(iw / 2) * 2:trunc(ih / 2) * 2\"";
        static string pngComprArg = "-compression_level 3";

        static string mpDecDef = "\"mpdecimate\"";
        static string mpDecAggr = "\"mpdecimate=hi=64*32:lo=64*32:frac=0.1\"";

        public static async Task VideoToFrames(string inputFile, string frameFolderPath, bool deDupe, bool delSrc)
        {
            await VideoToFrames(inputFile, frameFolderPath, deDupe, delSrc, new Size());
        }

        public static async Task VideoToFrames(string inputFile, string frameFolderPath, bool deDupe, bool delSrc, Size size)
        {
            string sizeStr = "";
            if (size.Width > 1 && size.Height > 1) sizeStr = $"-s {size.Width}x{size.Height}";
            if (!Directory.Exists(frameFolderPath))
                Directory.CreateDirectory(frameFolderPath);
            string args = $"-i {inputFile.Wrap()} {pngComprArg} -vsync 0 -pix_fmt rgb24 -copyts -r 1000 -frame_pts true -vf {divisionFilter} {sizeStr} \"{frameFolderPath}/%08d.png\"";
            if (deDupe)
            {
                string mpStr = (Config.GetInt("mpdecimateMode") == 0) ? mpDecDef : mpDecAggr;
                args = $"-i {inputFile.Wrap()} -copyts -r 1000 {pngComprArg} -vsync 0 -pix_fmt rgb24 -frame_pts true -vf {mpStr},{divisionFilter} {sizeStr} \"{frameFolderPath}/%08d.png\"";
            }
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            await Task.Delay(1);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async void ExtractSingleFrame(string inputFile, int frameNum, bool hdr, bool delSrc)
        {
            string hdrStr = "";
            if (hdr) hdrStr = hdrFilter;
            string args = "-i \"" + inputFile + "\" " + hdrStr
                + " -vf \"select=eq(n\\," + frameNum + ")\" -vframes 1  \"" + inputFile + "-frame" + frameNum + ".png\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task FramesToMp4(string inputDir, string outPath, bool useH265, int crf, float fps, string prefix, bool delSrc, int looptimes = -1, string imgFormat = "png")
        {
            Logger.Log($"Encoding MP4 video with CRF {crf}...");
            int nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, $"*.{imgFormat}")[0], prefix);
            string enc = useH265 ? "libx265" : "libx264";
            string loopStr = (looptimes > 0) ? $"-stream_loop {looptimes}" : "";
            string presetStr = $"-preset {Config.Get("ffEncPreset")}";
            string args = $" {loopStr} -framerate {fps.ToString().Replace(",",".")} -i \"{inputDir}\\{prefix}%0{nums}d.{imgFormat}\" -c:v {enc} -crf {crf} {presetStr} {videoEncArgs} -threads {Config.GetInt("ffEncThreads")} -c:a copy {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputDir);
        }

        public static async Task FramesToMp4Vfr(string framesFile, string outPath, bool useH265, int crf, float fps, int looptimes = -1)
        {
            Logger.Log($"Encoding MP4 video with CRF {crf}...");
            string enc = useH265 ? "libx265" : "libx264";
            string loopStr = (looptimes > 0) ? $"-stream_loop {looptimes}" : "";
            string presetStr = $"-preset {Config.Get("ffEncPreset")}";
            string args = $" {loopStr} -vsync 1 -f concat -safe 0 -i {framesFile.Wrap()} -r {fps.ToString().Replace(",", ".")} -c:v {enc} -crf {crf} {presetStr} {videoEncArgs} -threads {Config.GetInt("ffEncThreads")} -c:a copy {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
        }

        public static async Task ConvertFramerate (string inputPath, string outPath, bool useH265, int crf, float newFps, bool delSrc = false)
        {
            Logger.Log($"Changing video frame rate...");
            string enc = useH265 ? "libx265" : "libx264";
            string presetStr = $"-preset {Config.Get("ffEncPreset")}";
            string args = $" -i {inputPath.Wrap()} -filter:v fps=fps={newFps} -c:v {enc} -crf {crf} {presetStr} -pix_fmt yuv420p -movflags +faststart {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputPath);
        }

        public static async void FramesToApng (string inputDir, bool opti, int fps, string prefix, bool delSrc)
        {
            int nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, "*.png")[0], prefix);
            string filter = "";
            if(opti) filter = "-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\"";
            string args = "-framerate " + fps + " -i \"" + inputDir + "\\" + prefix + "%0" + nums + "d.png\" -f apng -plays 0 " + filter + " \"" + inputDir + "-anim.png\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputDir);
        }

        public static async void FramesToGif (string inputDir, bool opti, int fps, string prefix, bool delSrc)
        {
            int nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, "*.png")[0], prefix);
            string filter = "";
            if (opti) filter = "-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\"";
            string args = "-framerate " + fps + " -i \"" + inputDir + "\\" + prefix + "%0" + nums + "d.png\" -f gif " + filter + " \"" + inputDir + ".gif\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputDir);
        }

        public static async Task LoopVideo (string inputFile, int times, bool delSrc)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            string args = $" -stream_loop {times} -i {inputFile.Wrap()} -c copy \"{pathNoExt}-{times}xLoop{ext}\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task LoopVideoEnc (string inputFile, int times, bool useH265, int crf, bool delSrc)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            string enc = "libx264";
            if (useH265) enc = "libx265";
            string args = " -stream_loop " + times + " -i \"" + inputFile +  "\"  -c:v " + enc + " -crf " + crf + " -c:a copy \"" + pathNoExt + "-" + times + "xLoop" + ext + "\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ChangeSpeed (string inputFile, float newSpeedPercent, bool delSrc)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            float val = newSpeedPercent / 100f;
            string speedVal = (1f / val).ToString("0.0000").Replace(",", ".");
            string args = " -itsscale " + speedVal + " -i \"" + inputFile + "\"  -c copy \"" + pathNoExt + "-" + newSpeedPercent + "pcSpeed" + ext + "\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task Encode (string inputFile, string vcodec, string acodec, int crf, int audioKbps, bool delSrc)
        {
            string outPath = Path.ChangeExtension(inputFile, null) + "-convert.mp4";
            string args = $" -i {inputFile.Wrap()} -c:v {vcodec} -crf {crf} -pix_fmt yuv420p -c:a {acodec} -b:a {audioKbps} {outPath.Wrap()}";
            if (string.IsNullOrWhiteSpace(acodec))
                args = args.Replace("-c:a", "-an");
            if(audioKbps < 0)
                args = args.Replace($" -b:a {audioKbps}", "");
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ExtractAudio (string inputFile, string outFile)    // https://stackoverflow.com/a/27413824/14274419
        {
            Logger.Log($"[FFCmds] Extracting audio from {inputFile} to {outFile}", true);
            string ext = GetAudioExt(inputFile);
            outFile = Path.ChangeExtension(outFile, ext);
            string args = $" -loglevel panic -i {inputFile.Wrap()} -vn -acodec copy {outFile.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if (AvProcess.lastOutputFfmpeg.ToLower().Contains("error") && File.Exists(outFile))    // If broken file was written
                File.Delete(outFile);
        }

        static string GetAudioExt (string videoFile)
        {
            switch (GetAudioCodec(videoFile))
            {
                case "vorbis": return "ogg";
                case "mp2": return "mp2";
                case "aac": return "m4a";
                default: return "wav";
            }
        }

        public static async Task MergeAudio(string inputFile, string audioPath, int looptimes = -1)    // https://superuser.com/a/277667
        {
            Logger.Log($"[FFCmds] Merging audio from {audioPath} into {inputFile}", true);
            string tempPath = inputFile + "-temp.mp4";
            if (Path.GetExtension(audioPath) == ".wav")
            {
                Logger.Log("Using MKV instead of MP4 to enable support for raw audio.");
                tempPath = Path.ChangeExtension(tempPath, "mkv");
            }
            string args = $" -i {inputFile.Wrap()} -stream_loop {looptimes} -i {audioPath.Wrap()} -shortest -c copy {tempPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if(AvProcess.lastOutputFfmpeg.Contains("Invalid data"))
            {
                Logger.Log("Failed to merge audio!");
                return;
            }
            File.Delete(inputFile);
            File.Move(tempPath, inputFile);
        }

        public static float GetFramerate (string inputFile)
        {
            string args = $" -i {inputFile.Wrap()}";
            string output = AvProcess.GetFfmpegOutput(args);
            string[] entries = output.Split(',');
            foreach(string entry in entries)
            {
                if (entry.Contains(" fps") && !entry.Contains("Input "))    // Avoid reading FPS from the filename, in case filename contains "fps"
                {
                    Logger.Log("[FFCmds] FPS Entry: " + entry, true);
                    string num = entry.Replace(" fps", "").Trim().Replace(",", ".");
                    float value;
                    float.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                    return value;
                }
            }
            return 0f;
        }

        public static Size GetSize (string inputFile)
        {
            string args = $" -v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {inputFile.Wrap()}";
            string output = AvProcess.GetFfprobeOutput(args);

            if (output.Length > 4 && output.Contains("x"))
            {
                string[] numbers = output.Split('x');
                return new Size(numbers[0].GetInt(), numbers[1].GetInt());
            }
            return new Size(0, 0);
        }

        public static int GetFrameCount(string inputFile)
        {
            int frames = 0;

            frames = ReadFrameCountFfprobe(inputFile, Config.GetBool("ffprobeCountFrames"));      // Try reading frame count with ffprobe
            if (frames > 0)
                return frames;

            frames = ReadFrameCountFfmpeg(inputFile);       // Try reading frame count with ffmpeg
            if (frames > 0)
                return frames;

            Logger.Log("Failed to get total frame count of video.");
            return 0;
        }

        static int ReadFrameCountFfprobe (string inputFile, bool readFramesSlow)
        {
            string args = $" -v panic -select_streams v:0 -show_entries stream=nb_frames -of default=noprint_wrappers=1 {inputFile.Wrap()}";
            if (readFramesSlow)
            {
                Logger.Log("Counting total frames using FFprobe. This can take a moment...");
                args = $" -v panic -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of default=nokey=1:noprint_wrappers=1 {inputFile.Wrap()}";
            }
            string info = AvProcess.GetFfprobeOutput(args);
            string[] entries = info.SplitIntoLines();
            try
            {
                Logger.Log("[FFCmds] ReadFrameCountFfprobe - ffprobe output: " + info, true);
                if (readFramesSlow)
                    return info.GetInt();
                foreach (string entry in entries)
                {
                    if (entry.Contains("nb_frames="))
                    {
                        Logger.Log("[FFCmds] Getting Int from " + entry, true);
                        return entry.GetInt();
                    }
                }
            }
            catch { }
            return -1;
        }

        static int ReadFrameCountFfmpeg(string inputFile)
        {
            string args = $" -loglevel panic -i {inputFile.Wrap()} -map 0:v:0 -c copy -f null - ";
            string info = AvProcess.GetFfmpegOutput(args);
            string[] entries = info.SplitIntoLines();
            foreach (string entry in entries)
            {
                if (entry.Contains("frame="))
                {
                    Logger.Log("[FFCmds] Getting Int from entry " + entry, true);
                    Logger.Log("[FFCmds] Getting Int from " + entry.Substring(0, entry.IndexOf("fps")), true);
                    return entry.Substring(0, entry.IndexOf("fps")).GetInt();
                }
            }
            return -1;
        }

        static string GetAudioCodec (string path)
        {
            string args = $" -v panic -show_streams -select_streams a -show_entries stream=codec_name {path.Wrap()}";
            string info = AvProcess.GetFfprobeOutput(args);
            string[] entries = info.SplitIntoLines();
            foreach (string entry in entries)
            {
                if (entry.Contains("codec_name="))
                {
                    Logger.Log($"[FFCmds] Audio Codec Entry: {entry}", true);
                    return entry.Split('=')[1];
                }
            }
            return "";
        }

        static string GetFirstStreamInfo (string ffmpegOutput)
        {
            foreach (string line in Regex.Split(ffmpegOutput, "\r\n|\r|\n"))
            {
                if (line.Contains("Stream #0"))
                    return line;
            }
            return "";
        }

        static void DeleteSource (string path)
        {
            Logger.Log("Deleting input file/dir: " + path);

            if (IOUtils.IsPathDirectory(path) && Directory.Exists(path))
                Directory.Delete(path, true);

            if (!IOUtils.IsPathDirectory(path) && File.Exists(path))
                File.Delete(path);
        }
    }
}
