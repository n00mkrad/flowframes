using Flowframes.AudioVideo;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils = Flowframes.AudioVideo.FFmpegUtils;

namespace Flowframes
{
    class FFmpegCommands
    {
        static string hdrFilter = @"-vf select=gte(n\,%frNum%),zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap=tonemap=hable:desat=0,zscale=t=bt709:m=bt709:r=tv,format=yuv420p";

        static string videoEncArgs = "-pix_fmt yuv420p -movflags +faststart";
        static string divisionFilter = "\"crop = trunc(iw / 2) * 2:trunc(ih / 2) * 2\"";
        static string pngComprArg = "-compression_level 3";

        static string mpDecDef = "\"mpdecimate\"";
        static string mpDecAggr = "\"mpdecimate=hi=64*32:lo=64*32:frac=0.1\"";

        public static async Task ExtractSceneChanges(string inputFile, string frameFolderPath)
        {
            Logger.Log("Extracting scene changes...");
            await VideoToFrames(inputFile, frameFolderPath, false, false, new Size(320, 180), false, true);
            bool hiddenLog = Interpolate.currentInputFrameCount <= 50;
            Logger.Log($"Detected {IOUtils.GetAmountOfFiles(frameFolderPath, false)} scene changes.".Replace(" 0 ", " no "), false, !hiddenLog);
        }

        public static async Task VideoToFrames(string inputFile, string frameFolderPath, bool deDupe, bool delSrc, bool timecodes = true)
        {
            await VideoToFrames(inputFile, frameFolderPath, deDupe, delSrc, new Size(), timecodes);
        }

        //public enum TimecodeMode { None, Consecutive, Realtime }
        public static async Task VideoToFrames(string inputFile, string frameFolderPath, bool deDupe, bool delSrc, Size size, bool timecodes, bool sceneDetect = false)
        {
            if (!sceneDetect) Logger.Log("Extracting video frames from input video...");
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            IOUtils.CreateDir(frameFolderPath);
            string timecodeStr = timecodes ? $"-copyts -r {FrameTiming.timebase} -frame_pts true" : "-copyts -frame_pts true";
            string scnDetect = sceneDetect ? $"\"select='gt(scene,{Config.GetFloatString("scnDetectValue")})'\"" : "";
            string mpStr = deDupe ? ((Config.GetInt("mpdecimateMode") == 0) ? mpDecDef : mpDecAggr) : "";
            string fpsFilter = $"\"fps=fps={Interpolate.current.inFps.ToString().Replace(",", ".")}\"";
            string filters = FormatUtils.ConcatStrings(new string[] { scnDetect, mpStr/*, fpsFilter*/ } );
            string vf = filters.Length > 2 ? $"-vf {filters}" : "";
            string pad = Padding.inputFrames.ToString();
            string args = $"-i {inputFile.Wrap()} {pngComprArg} -vsync 0 -pix_fmt rgb24 {timecodeStr} {vf} {sizeStr} \"{frameFolderPath}/%{pad}d.png\"";
            AvProcess.LogMode logMode = Interpolate.currentInputFrameCount > 50 ? AvProcess.LogMode.OnlyLastLine : AvProcess.LogMode.Hidden;
            await AvProcess.RunFfmpeg(args, logMode);
            if (!sceneDetect) Logger.Log($"Extracted {IOUtils.GetAmountOfFiles(frameFolderPath, false, "*.png")} frames from input.", false, true);
            await Task.Delay(1);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ImportImages(string inpath, string outpath, bool delSrc = false, bool showLog = true)
        {
            if (showLog) Logger.Log("Importing images...");
            Logger.Log($"Importing images from {inpath} to {outpath}.");
            IOUtils.CreateDir(outpath);
            string concatFile = Path.Combine(Paths.GetDataPath(), "png-concat-temp.ini");
            string concatFileContent = "";
            foreach (string img in IOUtils.GetFilesSorted(inpath))
                concatFileContent += $"file '{img.Replace(@"\", "/")}'\n";
            File.WriteAllText(concatFile, concatFileContent);

            string args = $" -loglevel panic -f concat -safe 0 -i {concatFile.Wrap()} {pngComprArg} -pix_fmt rgb24 -vsync 0 -vf {divisionFilter} \"{outpath}/%{Padding.inputFrames}d.png\"";
            AvProcess.LogMode logMode = IOUtils.GetAmountOfFiles(inpath, false) > 50 ? AvProcess.LogMode.OnlyLastLine : AvProcess.LogMode.Hidden;
            await AvProcess.RunFfmpeg(args, logMode);
            if (delSrc)
                DeleteSource(inpath);
        }

        public static async Task ExtractSingleFrame(string inputFile, int frameNum, bool hdr, bool delSrc)
        {
            string outPath = $"{inputFile}-frame{frameNum}.png";
            await ExtractSingleFrame(inputFile, outPath, frameNum, hdr, delSrc);
        }

        public static async Task ExtractSingleFrame(string inputFile, string outputPath, int frameNum, bool hdr, bool delSrc)
        {
            string hdrStr = hdr ? hdrFilter : "";
            string args = $"-i {inputFile.Wrap()} {pngComprArg} {hdrStr }-vf \"select=eq(n\\,{frameNum})\" -vframes 1  {outputPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task FramesToVideoConcat(string framesFile, string outPath, Interpolate.OutMode outMode, float fps, AvProcess.LogMode logMode = AvProcess.LogMode.OnlyLastLine, bool isChunk = false)
        {
            if (logMode != AvProcess.LogMode.Hidden)
                Logger.Log($"Encoding video...");
            string encArgs = Utils.GetEncArgs(Utils.GetCodec(outMode)) + " -pix_fmt yuv420p ";
            if (!isChunk) encArgs += $"-movflags +faststart";
            string vfrFilename = Path.GetFileName(framesFile);
            //string vsync = (Interpolate.current.interpFactor == 2) ? "-vsync 1" : "-vsync 2";
            string rate = fps.ToString().Replace(",", ".");
            string extraArgs = Config.Get("ffEncArgs");
            string args = $"-loglevel error -vsync 0 -f concat -r {rate} -i {vfrFilename} {encArgs} {extraArgs} -threads {Config.GetInt("ffEncThreads")} {outPath.Wrap()}";
            //string args = $"-vsync 0 -f concat -i {vfrFilename} {encArgs} {extraArgs} -threads {Config.GetInt("ffEncThreads")} {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, framesFile.GetParentDir(), logMode);
        }

        public static async Task ConcatVideos(string concatFile, string outPath, float fps, int looptimes = -1)
        {
            Logger.Log($"Merging videos...");
            string loopStr = (looptimes > 0) ? $"-stream_loop {looptimes}" : "";
            string vfrFilename = Path.GetFileName(concatFile);
            string args = $" {loopStr} -vsync 1 -f concat -r {fps.ToString().Replace(",", ".")} -i {vfrFilename} -c copy -pix_fmt yuv420p {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, concatFile.GetParentDir(), AvProcess.LogMode.Hidden);
        }

        public static async Task ConvertFramerate(string inputPath, string outPath, bool useH265, int crf, float newFps, bool delSrc = false)
        {
            Logger.Log($"Changing video frame rate...");
            string enc = useH265 ? "libx265" : "libx264";
            string presetStr = $"-preset {Config.Get("ffEncPreset")}";
            string args = $" -i {inputPath.Wrap()} -filter:v fps=fps={newFps} -c:v {enc} -crf {crf} {presetStr} {videoEncArgs} {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputPath);
        }

        public static async void FramesToGif(string inputDir, bool palette, int fps, string prefix, bool delSrc = false)
        {
            int nums = IOUtils.GetFilenameCounterLength(IOUtils.GetFilesSorted(inputDir, false, "*.png")[0], prefix);
            string filter = palette ? "-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\"" : "";
            string args = "-framerate " + fps + " -i \"" + inputDir + "\\" + prefix + "%0" + nums + "d.png\" -f gif " + filter + " \"" + inputDir + ".gif\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputDir);
        }

        public static async Task FramesToGifVfr(string framesFile, string outPath, bool palette, int colors = 64)
        {
            Logger.Log($"Encoding GIF...");
            string vfrFilename = Path.GetFileName(framesFile);
            string filter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither=floyd_steinberg:diff_mode=rectangle\"" : "";
            string args = $"-f concat -i {vfrFilename.Wrap()} -f gif {filter} {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, framesFile.GetParentDir(), AvProcess.LogMode.OnlyLastLine);
        }

        public static async Task LoopVideo(string inputFile, int times, bool delSrc = false)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            string args = $" -stream_loop {times} -i {inputFile.Wrap()} -c copy \"{pathNoExt}-Loop{times}{ext}\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task LoopVideoEnc(string inputFile, int times, bool useH265, int crf, bool delSrc = false)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            string enc = "libx264";
            if (useH265) enc = "libx265";
            string args = " -stream_loop " + times + " -i \"" + inputFile + "\"  -c:v " + enc + " -crf " + crf + " -c:a copy \"" + pathNoExt + "-" + times + "xLoop" + ext + "\"";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ChangeSpeed(string inputFile, float newSpeedPercent, bool delSrc = false)
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

        public static async Task Encode(string inputFile, string vcodec, string acodec, int crf, int audioKbps = 0, bool delSrc = false)
        {
            string outPath = Path.ChangeExtension(inputFile, null) + "-convert.mp4";
            string args = $" -i {inputFile.Wrap()} -c:v {vcodec} -crf {crf} -pix_fmt yuv420p -c:a {acodec} -b:a {audioKbps}k -vf {divisionFilter} {outPath.Wrap()}";
            if (string.IsNullOrWhiteSpace(acodec))
                args = args.Replace("-c:a", "-an");
            if (audioKbps < 0)
                args = args.Replace($" -b:a {audioKbps}", "");
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ExtractAudio(string inputFile, string outFile)    // https://stackoverflow.com/a/27413824/14274419
        {
            Logger.Log($"[FFCmds] Extracting audio from {inputFile} to {outFile}", true);
            //string ext = GetAudioExt(inputFile);
            outFile = Path.ChangeExtension(outFile, ".ogg");
            string args = $" -loglevel panic -i {inputFile.Wrap()} -vn -acodec libopus -b:a 256k {outFile.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if (AvProcess.lastOutputFfmpeg.ToLower().Contains("error") && File.Exists(outFile))    // If broken file was written
                File.Delete(outFile);
        }

        public static async Task MergeAudio(string inputFile, string audioPath, int looptimes = -1)    // https://superuser.com/a/277667
        {
            Logger.Log($"[FFCmds] Merging audio from {audioPath} into {inputFile}", true);
            string tempPath = inputFile + "-temp" + Path.GetExtension(inputFile);
            // if (Path.GetExtension(audioPath) == ".wav")
            // {
            //     Logger.Log("Using MKV instead of MP4 to enable support for raw audio.");
            //     tempPath = Path.ChangeExtension(tempPath, "mkv");
            // }
            string aCodec = Utils.GetAudioEnc(Utils.GetCodec(Interpolate.current.outMode));
            int aKbits = Utils.GetAudioKbits(aCodec);
            string args = $" -i {inputFile.Wrap()} -stream_loop {looptimes} -i {audioPath.Wrap()} -shortest -c:v copy -c:a {aCodec} -b:a {aKbits}k {tempPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if (AvProcess.lastOutputFfmpeg.Contains("Invalid data"))
            {
                Logger.Log("Failed to merge audio!");
                return;
            }
            string movePath = Path.ChangeExtension(inputFile, Path.GetExtension(tempPath));
            File.Delete(movePath);
            File.Delete(inputFile);
            File.Move(tempPath, movePath);
        }

        public static float GetFramerate(string inputFile)
        {
            Logger.Log("Reading FPS using ffmpeg.", true, false, "ffmpeg");
            string args = $" -i {inputFile.Wrap()}";
            string output = AvProcess.GetFfmpegOutput(args);
            string[] entries = output.Split(',');
            foreach (string entry in entries)
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

        public static Size GetSize(string inputFile)
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

            Logger.Log("Reading frame count using ffprobe.", true, false, "ffmpeg");
            frames = ReadFrameCountFfprobe(inputFile, Config.GetBool("ffprobeCountFrames"));      // Try reading frame count with ffprobe
            if (frames > 0)
                return frames;

            Logger.Log($"Failed to get frame count using ffprobe (frames = {frames}). Reading frame count using ffmpeg.", true, false, "ffmpeg");
            frames = ReadFrameCountFfmpeg(inputFile);       // Try reading frame count with ffmpeg
            if (frames > 0)
                return frames;

            Logger.Log("Failed to get total frame count of video.");
            return 0;
        }

        public static async Task<int> GetFrameCountAsync(string inputFile)
        {
            int frames = 0;

            Logger.Log("Reading frame count using ffprobe.", true, false, "ffmpeg");
            frames = await ReadFrameCountFfprobeAsync(inputFile, Config.GetBool("ffprobeCountFrames"));      // Try reading frame count with ffprobe
            if (frames > 0)
                return frames;

            Logger.Log($"Failed to get frame count using ffprobe (frames = {frames}). Reading frame count using ffmpeg.", true, false, "ffmpeg");
            frames = await ReadFrameCountFfmpegAsync(inputFile);       // Try reading frame count with ffmpeg
            if (frames > 0)
                return frames;

            Logger.Log("Failed to get total frame count of video.");
            return 0;
        }

        static int ReadFrameCountFfprobe(string inputFile, bool readFramesSlow)
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
                if (readFramesSlow)
                    return info.GetInt();
                foreach (string entry in entries)
                {
                    if (entry.Contains("nb_frames="))
                        return entry.GetInt();
                }
            }
            catch { }
            return -1;
        }

        static async Task<int> ReadFrameCountFfprobeAsync(string inputFile, bool readFramesSlow)
        {
            string args = $" -v panic -select_streams v:0 -show_entries stream=nb_frames -of default=noprint_wrappers=1 {inputFile.Wrap()}";
            if (readFramesSlow)
            {
                Logger.Log("Counting total frames using FFprobe. This can take a moment...");
                await Task.Delay(10);
                args = $" -v panic -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of default=nokey=1:noprint_wrappers=1 {inputFile.Wrap()}";
            }
            string info = AvProcess.GetFfprobeOutput(args);
            string[] entries = info.SplitIntoLines();
            try
            {
                if (readFramesSlow)
                    return info.GetInt();
                foreach (string entry in entries)
                {
                    if (entry.Contains("nb_frames="))
                        return entry.GetInt();
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
                    return entry.Substring(0, entry.IndexOf("fps")).GetInt();
            }
            return -1;
        }

        static async Task<int> ReadFrameCountFfmpegAsync (string inputFile)
        {
            string args = $" -loglevel panic -i {inputFile.Wrap()} -map 0:v:0 -c copy -f null - ";
            string info = await AvProcess.GetFfmpegOutputAsync(args, true);
            try
            {
                string[] lines = info.SplitIntoLines();
                string lastLine = lines.Last();
                return lastLine.Substring(0, lastLine.IndexOf("fps")).GetInt();
            }
            catch
            {
                return -1;
            }
        }

        public static string GetAudioCodec(string path)
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

        static string GetFirstStreamInfo(string ffmpegOutput)
        {
            foreach (string line in Regex.Split(ffmpegOutput, "\r\n|\r|\n"))
            {
                if (line.Contains("Stream #0"))
                    return line;
            }
            return "";
        }

        static void DeleteSource(string path)
        {
            Logger.Log("[FFCmds] Deleting input file/dir: " + path, true);

            if (IOUtils.IsPathDirectory(path) && Directory.Exists(path))
                Directory.Delete(path, true);

            if (!IOUtils.IsPathDirectory(path) && File.Exists(path))
                File.Delete(path);
        }
    }
}
