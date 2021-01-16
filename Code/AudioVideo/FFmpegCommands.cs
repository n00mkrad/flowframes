using Flowframes.AudioVideo;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utils = Flowframes.AudioVideo.FFmpegUtils;

namespace Flowframes
{
    class FFmpegCommands
    {
        static string hdrFilter = @"-vf select=gte(n\,%frNum%),zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap=tonemap=hable:desat=0,zscale=t=bt709:m=bt709:r=tv,format=yuv420p";
        static string divisionFilter = "\"crop=trunc(iw/2)*2:trunc(ih/2)*2\"";
        static string pngComprArg = "-compression_level 3";
        static string mpDecDef = "\"mpdecimate\"";
        static string mpDecAggr = "\"mpdecimate=hi=64*32:lo=64*32:frac=0.1\"";

        public static async Task ExtractSceneChanges(string inputFile, string frameFolderPath, float rate)
        {
            Logger.Log("Extracting scene changes...");
            await VideoToFrames(inputFile, frameFolderPath, rate, false, false, new Size(320, 180), false, true);
            bool hiddenLog = Interpolate.currentInputFrameCount <= 50;
            int amount = IOUtils.GetAmountOfFiles(frameFolderPath, false);
            Logger.Log($"Detected {amount} scene {(amount == 1 ? "change" : "changes")}.".Replace(" 0 ", " no "), false, !hiddenLog);
        }

        public static async Task VideoToFrames(string inputFile, string frameFolderPath, float rate, bool deDupe, bool delSrc, bool timecodes = true)
        {
            await VideoToFrames(inputFile, frameFolderPath, rate, deDupe, delSrc, new Size(), timecodes);
        }

        public static async Task VideoToFrames(string inputFile, string frameFolderPath, float rate, bool deDupe, bool delSrc, Size size, bool timecodes, bool sceneDetect = false)
        {
            if (!sceneDetect) Logger.Log("Extracting video frames from input video...");
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            IOUtils.CreateDir(frameFolderPath);
            string timecodeStr = timecodes ? $"-copyts -r {FrameOrder.timebase} -frame_pts true" : "-copyts -frame_pts true";
            string scnDetect = sceneDetect ? $"\"select='gt(scene,{Config.GetFloatString("scnDetectValue")})'\"" : "";
            string mpStr = deDupe ? ((Config.GetInt("mpdecimateMode") == 0) ? mpDecDef : mpDecAggr) : "";
            string filters = FormatUtils.ConcatStrings(new string[] { scnDetect, mpStr } );
            string vf = filters.Length > 2 ? $"-vf {filters}" : "";
            string rateArg = (rate > 0) ? $" -r {rate.ToStringDot()}" : "";
            string pad = Padding.inputFrames.ToString();
            string args = $"{rateArg} -i {inputFile.Wrap()} {pngComprArg} -vsync 0 -pix_fmt rgb24 {timecodeStr} {vf} {sizeStr} \"{frameFolderPath}/%{pad}d.png\"";
            AvProcess.LogMode logMode = Interpolate.currentInputFrameCount > 50 ? AvProcess.LogMode.OnlyLastLine : AvProcess.LogMode.Hidden;
            await AvProcess.RunFfmpeg(args, logMode, AvProcess.TaskType.ExtractFrames);
            int amount = IOUtils.GetAmountOfFiles(frameFolderPath, false, "*.png");
            if (!sceneDetect) Logger.Log($"Extracted {amount} {(amount == 1 ? "frame" : "frames")} from input.", false, true);
            await Task.Delay(1);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ImportImages(string inpath, string outpath, Size size, bool delSrc = false, bool showLog = true)
        {
            if (showLog) Logger.Log("Importing images...");
            Logger.Log($"Importing images from {inpath} to {outpath}.");
            IOUtils.CreateDir(outpath);
            string concatFile = Path.Combine(Paths.GetDataPath(), "png-concat-temp.ini");
            string concatFileContent = "";
            foreach (string img in IOUtils.GetFilesSorted(inpath))
                concatFileContent += $"file '{img.Replace(@"\", "/")}'\n";
            File.WriteAllText(concatFile, concatFileContent);

            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            string args = $" -loglevel panic -f concat -safe 0 -i {concatFile.Wrap()} {pngComprArg} {sizeStr} -pix_fmt rgb24 -vsync 0 -vf {divisionFilter} \"{outpath}/%{Padding.inputFrames}d.png\"";
            AvProcess.LogMode logMode = IOUtils.GetAmountOfFiles(inpath, false) > 50 ? AvProcess.LogMode.OnlyLastLine : AvProcess.LogMode.Hidden;
            await AvProcess.RunFfmpeg(args, logMode, AvProcess.TaskType.ExtractFrames);
            if (delSrc)
                DeleteSource(inpath);
        }

        public static async Task ImportSingleImage(string inputFile, string outPath, Size size)
        {
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            bool isPng = (Path.GetExtension(outPath).ToLower() == ".png");
            string comprArg = isPng ? pngComprArg : "";
            string pixFmt = "-pix_fmt " + (isPng ? $"rgb24 {comprArg}" : "yuvj420p");
            string args = $"-i {inputFile.Wrap()} {comprArg} {sizeStr} {pixFmt} -vf {divisionFilter} {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden, AvProcess.TaskType.ExtractFrames);
        }

        public static async Task ExtractSingleFrame(string inputFile, string outputPath, int frameNum)
        {
            bool isPng = (Path.GetExtension(outputPath).ToLower() == ".png");
            string comprArg = isPng ? pngComprArg : "";
            string pixFmt = "-pix_fmt " + (isPng ? $"rgb24 {comprArg}" : "yuvj420p");
            string args = $"-i {inputFile.Wrap()} -vf \"select=eq(n\\,{frameNum})\" -vframes 1 {pixFmt} {outputPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden, AvProcess.TaskType.ExtractFrames);
        }

        public static async Task ExtractLastFrame(string inputFile, string outputPath, Size size)
        {
            if (IOUtils.IsPathDirectory(outputPath))
                outputPath = Path.Combine(outputPath, "last.png");
            bool isPng = (Path.GetExtension(outputPath).ToLower() == ".png");
            string comprArg = isPng ? pngComprArg : "";
            string pixFmt = "-pix_fmt " + (isPng ? $"rgb24 {comprArg}" : "yuvj420p");
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            string args = $"-sseof -1 -i {inputFile.Wrap()}  -update 1 {pixFmt} {sizeStr} {outputPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden, AvProcess.TaskType.ExtractFrames);
        }

        public static async Task FramesToVideoConcat(string framesFile, string outPath, Interpolate.OutMode outMode, float fps, AvProcess.LogMode logMode = AvProcess.LogMode.OnlyLastLine, bool isChunk = false)
        {
            await FramesToVideoConcat(framesFile, outPath, outMode, fps, 0, logMode, isChunk);
        }

        public static async Task FramesToVideoConcat(string framesFile, string outPath, Interpolate.OutMode outMode, float fps, float resampleFps, AvProcess.LogMode logMode = AvProcess.LogMode.OnlyLastLine, bool isChunk = false)
        {
            if (logMode != AvProcess.LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding video..." : $"Encoding video resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            Directory.CreateDirectory(outPath.GetParentDir());
            string encArgs = Utils.GetEncArgs(Utils.GetCodec(outMode));
            if (!isChunk) encArgs += $" -movflags +faststart";
            string vfrFilename = Path.GetFileName(framesFile);
            string rate = fps.ToString().Replace(",", ".");
            string vf = (resampleFps <= 0) ? "" : $"-vf fps=fps={resampleFps.ToStringDot()}";
            string extraArgs = Config.Get("ffEncArgs");
            string args = $"-loglevel error -vsync 0 -f concat -r {rate} -i {vfrFilename} {encArgs} {vf} {extraArgs} -threads {Config.GetInt("ffEncThreads")} {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, framesFile.GetParentDir(), logMode, AvProcess.TaskType.Encode);
        }

        public static async Task ConcatVideos(string concatFile, string outPath, int looptimes = -1)
        {
            Logger.Log($"Merging videos...", false, Logger.GetLastLine().Contains("frame"));
            string loopStr = (looptimes > 0) ? $"-stream_loop {looptimes}" : "";
            string vfrFilename = Path.GetFileName(concatFile);
            string args = $" {loopStr} -vsync 1 -f concat -i {vfrFilename} -c copy -movflags +faststart {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, concatFile.GetParentDir(), AvProcess.LogMode.Hidden, AvProcess.TaskType.Merge);
        }

        public static async Task FramesToGifConcat(string framesFile, string outPath, float fps, bool palette, int colors = 64, float resampleFps = -1, AvProcess.LogMode logMode = AvProcess.LogMode.OnlyLastLine)
        {
            if (logMode != AvProcess.LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding GIF..." : $"Encoding GIF resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            string vfrFilename = Path.GetFileName(framesFile);
            string paletteFilter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither=floyd_steinberg:diff_mode=rectangle\"" : "";
            string fpsFilter = (resampleFps <= 0) ? "" : $"fps=fps={resampleFps.ToStringDot()}";
            string vf = FormatUtils.ConcatStrings(new string[] { paletteFilter, fpsFilter });
            string rate = fps.ToStringDot();
            string args = $"-loglevel error -f concat -r {rate} -i {vfrFilename.Wrap()} -f gif {vf} {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, framesFile.GetParentDir(), AvProcess.LogMode.OnlyLastLine, AvProcess.TaskType.Encode);
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
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.OnlyLastLine, AvProcess.TaskType.Encode);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ExtractAudio(string inputFile, string outFile)    // https://stackoverflow.com/a/27413824/14274419
        {
            string audioExt = Utils.GetAudioExt(inputFile);
            outFile = Path.ChangeExtension(outFile, audioExt);
            Logger.Log($"[FFCmds] Extracting audio from {inputFile} to {outFile}", true);
            string args = $" -loglevel panic -i {inputFile.Wrap()} -vn -c:a copy {outFile.Wrap()}";
            await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
            if (File.Exists(outFile) && IOUtils.GetFilesize(outFile) < 512)
            {
                Logger.Log("Failed to extract audio losslessly! Trying to re-encode.");
                File.Delete(outFile);

                outFile = Path.ChangeExtension(outFile, Utils.GetAudioExtForContainer(Path.GetExtension(inputFile)));
                args = $" -loglevel panic -i {inputFile.Wrap()} -vn {Utils.GetAudioFallbackArgs(Path.GetExtension(inputFile))} {outFile.Wrap()}";
                await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);

                if ((File.Exists(outFile) && IOUtils.GetFilesize(outFile) < 512) || AvProcess.lastOutputFfmpeg.Contains("Invalid data"))
                {
                    Logger.Log("Failed to extract audio, even with re-encoding. Output will not have audio.");
                    IOUtils.TryDeleteIfExists(outFile);
                    return;
                }

                Logger.Log($"Source audio has been re-encoded as it can't be extracted losslessly. This may decrease the quality slightly.", false, true);
            }
        }

        public static async Task ExtractSubtitles (string inputFile, string outFolder, Interpolate.OutMode outMode)
        {
            Dictionary<int, string> subDict = await GetSubtitleTracks(inputFile);
            foreach (KeyValuePair<int, string> subTrack in subDict)
            {
                string trackName = subTrack.Value.Length > 4 ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(subTrack.Value.ToLower()) : subTrack.Value.ToUpper();
                string outPath = Path.Combine(outFolder, $"{subTrack.Key}-{trackName}.srt");
                string args = $" -loglevel error -i {inputFile.Wrap()} -map 0:s:{subTrack.Key} {outPath.Wrap()}";
                await AvProcess.RunFfmpeg(args, AvProcess.LogMode.Hidden);
                if (AvProcess.lastOutputFfmpeg.Contains("matches no streams"))  // Break if there are no more subtitle tracks
                    break;
                Logger.Log($"[FFCmds] Extracted subtitle track {subTrack.Key} to {outPath} ({FormatUtils.Bytes(IOUtils.GetFilesize(outPath))})", true, false, "ffmpeg");
            }
            if(subDict.Count > 0)
            {
                Logger.Log($"Extracted {subDict.Count} subtitle tracks from the input video.");
                Utils.ContainerSupportsSubs(Utils.GetExt(outMode), true);
            }
        }

        public static async Task<Dictionary<int, string>> GetSubtitleTracks (string inputFile)
        {
            Dictionary<int, string> subDict = new Dictionary<int, string>();
            string args = $"-i {inputFile.Wrap()}";
            string[] outputLines = (await AvProcess.GetFfmpegOutputAsync(args)).SplitIntoLines();
            string[] filteredLines = outputLines.Where(l => l.Contains(" Subtitle: ")).ToArray();
            int idx = 0;
            foreach(string line in filteredLines)
            {
                string lang = "unknown";
                bool hasLangInfo = line.Contains("(") && line.Contains("): Subtitle: ");
                if (hasLangInfo)
                    lang = line.Split('(')[1].Split(')')[0];
                subDict.Add(idx, lang);
                idx++;
            }
            return subDict;
        }

        public static async Task MergeAudioAndSubs(string inputFile, string audioPath, string tempFolder, int looptimes = -1)    // https://superuser.com/a/277667
        {
            Logger.Log($"[FFCmds] Merging audio from {audioPath} into {inputFile}", true);
            string containerExt = Path.GetExtension(inputFile);
            string tempPath = Path.Combine(tempFolder, $"vid{containerExt}"); // inputFile + "-temp" + Path.GetExtension(inputFile);
            string outPath = Path.Combine(tempFolder, $"muxed{containerExt}"); // inputFile + "-temp" + Path.GetExtension(inputFile);
            File.Move(inputFile, tempPath);
            string inName = Path.GetFileName(tempPath);
            string audioName = Path.GetFileName(audioPath);
            string outName = Path.GetFileName(outPath);

            bool subs = Utils.ContainerSupportsSubs(containerExt, false) && Config.GetBool("keepSubs");
            string subInputArgs = "";
            string subMapArgs = "";
            string subMetaArgs = "";
            string[] subTracks = subs ? IOUtils.GetFilesSorted(tempFolder, false, "*.srt") : new string[0];
            for (int subTrack = 0; subTrack < subTracks.Length; subTrack++)
            {
                subInputArgs += $" -i {Path.GetFileName(subTracks[subTrack])}";
                subMapArgs += $" -map {subTrack+2}";
                subMetaArgs += $" -metadata:s:s:{subTrack} language={Path.GetFileNameWithoutExtension(subTracks[subTrack]).Split('-').Last()}";
            }

            string subCodec = Utils.GetSubCodecForContainer(containerExt);
            string args = $" -i {inName} -stream_loop {looptimes} -i {audioName.Wrap()}" +
                $"{subInputArgs} -map 0:v -map 1:a {subMapArgs} -c:v copy -c:a copy -c:s {subCodec} {subMetaArgs} -shortest {outName}";

            await AvProcess.RunFfmpeg(args, tempFolder, AvProcess.LogMode.Hidden);

            if ((File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 1024) || AvProcess.lastOutputFfmpeg.Contains("Invalid data") || AvProcess.lastOutputFfmpeg.Contains("Error initializing output stream"))
            {
                Logger.Log("Failed to merge audio losslessly! Trying to re-encode.", false, false, "ffmpeg");

                args = $" -i {inName} -stream_loop {looptimes} -i {audioName.Wrap()}" +
                $"{subInputArgs} -map 0:v -map 1:a {subMapArgs} -c:v copy {Utils.GetAudioFallbackArgs(Path.GetExtension(inputFile))} -c:s {subCodec} {subMetaArgs} -shortest {outName}";
                
                await AvProcess.RunFfmpeg(args, tempFolder, AvProcess.LogMode.Hidden);

                if ((File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 1024) || AvProcess.lastOutputFfmpeg.Contains("Invalid data") || AvProcess.lastOutputFfmpeg.Contains("Error initializing output stream"))
                {
                    Logger.Log("Failed to merge audio, even with re-encoding. Output will not have audio.", false, false, "ffmpeg");
                    IOUtils.TryDeleteIfExists(tempPath);
                    return;
                }

                string audioExt = Path.GetExtension(audioPath).Remove(".").ToUpper();
                Logger.Log($"Source audio ({audioExt}) has been re-encoded to fit into the target container ({containerExt.Remove(".").ToUpper()}). This may decrease the quality slightly.", false, true, "ffmpeg");
            }

            //string movePath = Path.ChangeExtension(inputFile, Path.GetExtension(tempPath));
            //File.Delete(movePath);
            if(File.Exists(outPath) && IOUtils.GetFilesize(outPath) > 512)
                {
                File.Delete(tempPath);
                File.Move(outPath, inputFile);
            }
            else
            {
                File.Move(tempPath, inputFile);
            }
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
            string args = $" -v panic -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 {inputFile.Wrap()}";
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
                    return entry.Split('=')[1];
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
