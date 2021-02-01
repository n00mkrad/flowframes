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
using static Flowframes.AvProcess;
using Utils = Flowframes.AudioVideo.FFmpegUtils;

namespace Flowframes
{
    class FFmpegCommands
    {
        public static string divisionFilter = "\"pad=width=ceil(iw/2)*2:height=ceil(ih/2)*2:color=black@0\"";
        public static string pngComprArg = "-compression_level 3";
        public static string mpDecDef = "\"mpdecimate\"";
        public static string mpDecAggr = "\"mpdecimate=hi=64*32:lo=64*32:frac=0.1\"";

        public static async Task ConcatVideos(string concatFile, string outPath, int looptimes = -1)
        {
            Logger.Log($"Merging videos...", false, Logger.GetLastLine().Contains("frame"));
            string loopStr = (looptimes > 0) ? $"-stream_loop {looptimes}" : "";
            string vfrFilename = Path.GetFileName(concatFile);
            string args = $" {loopStr} -vsync 1 -f concat -i {vfrFilename} -c copy -movflags +faststart {outPath.Wrap()}";
            await RunFfmpeg(args, concatFile.GetParentDir(), LogMode.Hidden, TaskType.Merge);
        }

        public static async Task FramesToGifConcat(string framesFile, string outPath, float fps, bool palette, int colors = 64, float resampleFps = -1, LogMode logMode = LogMode.OnlyLastLine)
        {
            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding GIF..." : $"Encoding GIF resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            string vfrFilename = Path.GetFileName(framesFile);
            string paletteFilter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither=floyd_steinberg:diff_mode=rectangle\"" : "";
            string fpsFilter = (resampleFps <= 0) ? "" : $"fps=fps={resampleFps.ToStringDot()}";
            string vf = FormatUtils.ConcatStrings(new string[] { paletteFilter, fpsFilter });
            string rate = fps.ToStringDot();
            string args = $"-loglevel error -f concat -r {rate} -i {vfrFilename.Wrap()} -f gif {vf} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), LogMode.OnlyLastLine, TaskType.Encode);
        }

        public static async Task ExtractAlphaDir (string rgbDir, string alphaDir)
        {
            Directory.CreateDirectory(alphaDir);
            foreach (FileInfo file in IOUtils.GetFileInfosSorted(rgbDir))
            {
                string args = $"-i {file.FullName.Wrap()} -vf format=yuva444p16le,alphaextract,format=yuv420p {Path.Combine(alphaDir, file.Name).Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden);
            }
        }

        public static async Task RemoveAlpha (string inputDir, string outputDir, string fillColor = "black")
        {
            Directory.CreateDirectory(outputDir);
            foreach (FileInfo file in IOUtils.GetFileInfosSorted(inputDir))
            {
                string outFilename = Path.Combine(outputDir, "_" + file.Name);
                Size res = IOUtils.GetImage(file.FullName).Size;
                string args = $" -f lavfi -i color={fillColor}:s={res.Width}x{res.Height} -i {file.FullName.Wrap()} " +
                    $"-filter_complex overlay=0:0:shortest=1 -pix_fmt rgb24 {outFilename.Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden);
                file.Delete();
                File.Move(outFilename, file.FullName);
            }
        }

        public static async Task MergeAlphaIntoRgb (string rgbDir, int rgbPad, string alphaDir, int aPad, bool deleteAlphaDir)
        {
            string filter = "-filter_complex [0:v:0][1:v:0]alphamerge[out] -map [out]";
            string args = $"-i \"{rgbDir}/%{rgbPad}d.png\" -i \"{alphaDir}/%{aPad}d.png\" {filter} \"{rgbDir}/%{rgbPad}d.png\"";
            await RunFfmpeg(args, LogMode.Hidden);
            if (deleteAlphaDir)
                IOUtils.TryDeleteIfExists(alphaDir);
        }

        public static async Task LoopVideo(string inputFile, int times, bool delSrc = false)
        {
            string pathNoExt = Path.ChangeExtension(inputFile, null);
            string ext = Path.GetExtension(inputFile);
            string args = $" -stream_loop {times} -i {inputFile.Wrap()} -c copy \"{pathNoExt}-Loop{times}{ext}\"";
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
            string args = " -itsscale " + speedVal + " -i \"" + inputFile + "\"  -c copy \"" + pathNoExt + "-" + newSpeedPercent + "pcSpeed" + ext + "\"";
            await RunFfmpeg(args, LogMode.OnlyLastLine);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ExtractAudio(string inputFile, string outFile)    // https://stackoverflow.com/a/27413824/14274419
        {
            string audioExt = Utils.GetAudioExt(inputFile);
            outFile = Path.ChangeExtension(outFile, audioExt);
            Logger.Log($"[FFCmds] Extracting audio from {inputFile} to {outFile}", true);
            string args = $" -loglevel panic -i {inputFile.Wrap()} -vn -c:a copy {outFile.Wrap()}";
            await RunFfmpeg(args, LogMode.Hidden);
            if (File.Exists(outFile) && IOUtils.GetFilesize(outFile) < 512)
            {
                Logger.Log("Failed to extract audio losslessly! Trying to re-encode.");
                File.Delete(outFile);

                outFile = Path.ChangeExtension(outFile, Utils.GetAudioExtForContainer(Path.GetExtension(inputFile)));
                args = $" -loglevel panic -i {inputFile.Wrap()} -vn {Utils.GetAudioFallbackArgs(Path.GetExtension(inputFile))} {outFile.Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden);

                if ((File.Exists(outFile) && IOUtils.GetFilesize(outFile) < 512) || lastOutputFfmpeg.Contains("Invalid data"))
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
            Dictionary<int, string> subtitleTracks = await GetSubtitleTracks(inputFile);

            foreach (KeyValuePair<int, string> subTrack in subtitleTracks)
            {
                string trackName = subTrack.Value.Length > 4 ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(subTrack.Value.ToLower()) : subTrack.Value.ToUpper();
                string outPath = Path.Combine(outFolder, $"{subTrack.Key}-{trackName}.srt");
                string args = $" -loglevel error -i {inputFile.Wrap()} -map 0:s:{subTrack.Key} {outPath.Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden);
                if (lastOutputFfmpeg.Contains("matches no streams"))  // Break if there are no more subtitle tracks
                    break;
                Logger.Log($"[FFCmds] Extracted subtitle track {subTrack.Key} to {outPath} ({FormatUtils.Bytes(IOUtils.GetFilesize(outPath))})", true, false, "ffmpeg");
            }

            if(subtitleTracks.Count > 0)
            {
                Logger.Log($"Extracted {subtitleTracks.Count} subtitle tracks from the input video.");
                Utils.ContainerSupportsSubs(Utils.GetExt(outMode), true);
            }
        }

        public static async Task<Dictionary<int, string>> GetSubtitleTracks (string inputFile)
        {
            Dictionary<int, string> subDict = new Dictionary<int, string>();
            string args = $"-i {inputFile.Wrap()}";
            string[] outputLines = (await GetFfmpegOutputAsync(args)).SplitIntoLines();
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

            await RunFfmpeg(args, tempFolder, LogMode.Hidden);

            if ((File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 1024) || lastOutputFfmpeg.Contains("Invalid data") || lastOutputFfmpeg.Contains("Error initializing output stream"))
            {
                Logger.Log("Failed to merge audio losslessly! Trying to re-encode.", false, false, "ffmpeg");

                args = $" -i {inName} -stream_loop {looptimes} -i {audioName.Wrap()}" +
                $"{subInputArgs} -map 0:v -map 1:a {subMapArgs} -c:v copy {Utils.GetAudioFallbackArgs(Path.GetExtension(inputFile))} -c:s {subCodec} {subMetaArgs} -shortest {outName}";
                
                await RunFfmpeg(args, tempFolder, LogMode.Hidden);

                if ((File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 1024) || lastOutputFfmpeg.Contains("Invalid data") || lastOutputFfmpeg.Contains("Error initializing output stream"))
                {
                    Logger.Log("Failed to merge audio, even with re-encoding. Output will not have audio.", false, false, "ffmpeg");
                    IOUtils.TryDeleteIfExists(tempPath);
                    return;
                }

                string audioExt = Path.GetExtension(audioPath).Remove(".").ToUpper();
                Logger.Log($"Source audio ({audioExt}) has been re-encoded to fit into the target container ({containerExt.Remove(".").ToUpper()}). This may decrease the quality slightly.", false, true, "ffmpeg");
            }

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

        public static long GetDuration(string inputFile)
        {
            Logger.Log("Reading Duration using ffprobe.", true, false, "ffprobe");
            string args = $" -v panic -select_streams v:0 -show_entries format=duration -of csv=s=x:p=0 -sexagesimal {inputFile.Wrap()}";
            string info = GetFfprobeOutput(args);
            return FormatUtils.MsFromTimestamp(info);
            return -1;
        }

        public static float GetFramerate(string inputFile)
        {
            Logger.Log("Reading FPS using ffmpeg.", true, false, "ffmpeg");
            string args = $" -i {inputFile.Wrap()}";
            string output = GetFfmpegOutput(args);
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
            string output = GetFfprobeOutput(args);

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
            string info = GetFfprobeOutput(args);
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
            string info = GetFfprobeOutput(args);
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
            string info = GetFfmpegOutput(args);
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
            string info = await GetFfmpegOutputAsync(args, true);
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
            string info = GetFfprobeOutput(args);
            string[] entries = info.SplitIntoLines();
            foreach (string entry in entries)
            {
                if (entry.Contains("codec_name="))
                    return entry.Split('=')[1];
            }
            return "";
        }

        public static void DeleteSource(string path)
        {
            Logger.Log("[FFCmds] Deleting input file/dir: " + path, true);

            if (IOUtils.IsPathDirectory(path) && Directory.Exists(path))
                Directory.Delete(path, true);

            if (!IOUtils.IsPathDirectory(path) && File.Exists(path))
                File.Delete(path);
        }
    }
}
