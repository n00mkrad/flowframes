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
using Utils = Flowframes.Media.FFmpegUtils;

namespace Flowframes.Media
{
    partial class FfmpegAudioAndMetadata : FfmpegCommands
    {
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

        public static async Task ExtractSubtitles(string inputFile, string outFolder, Interpolate.OutMode outMode)
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

            if (subtitleTracks.Count > 0)
            {
                Logger.Log($"Extracted {subtitleTracks.Count} subtitle tracks from the input video.");
                Utils.ContainerSupportsSubs(Utils.GetExt(outMode), true);
            }
        }

        public static async Task<Dictionary<int, string>> GetSubtitleTracks(string inputFile)
        {
            Dictionary<int, string> subDict = new Dictionary<int, string>();
            string args = $"-i {inputFile.Wrap()}";
            string[] outputLines = (await GetFfmpegOutputAsync(args)).SplitIntoLines();
            string[] filteredLines = outputLines.Where(l => l.Contains(" Subtitle: ")).ToArray();
            int idx = 0;
            foreach (string line in filteredLines)
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
                subMapArgs += $" -map {subTrack + 2}";
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

            if (File.Exists(outPath) && IOUtils.GetFilesize(outPath) > 512)
            {
                File.Delete(tempPath);
                File.Move(outPath, inputFile);
            }
            else
            {
                File.Move(tempPath, inputFile);
            }
        }
    }
}
