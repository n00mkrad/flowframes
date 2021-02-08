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
            try
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
            catch (Exception e)
            {
                Logger.Log("Error extracting audio: " + e.Message);
            }
        }

        public static async Task ExtractSubtitles(string inputFile, string outFolder, Interpolate.OutMode outMode,bool showMsg = true)
        {
            try
            {
                string msg = "Extracting subtitles from video...";
                if (showMsg) Logger.Log(msg);

                List<SubtitleTrack> subtitleTracks = await GetSubtitleTracks(inputFile);
                int counter = 1;

                foreach (SubtitleTrack subTrack in subtitleTracks)
                {
                    string outPath = Path.Combine(outFolder, $"{subTrack.streamIndex}_{subTrack.langFriendly}_{subTrack.encoding}.srt");
                    string args = $" -loglevel error -sub_charenc {subTrack.encoding} -i {inputFile.Wrap()} -map 0:s:{subTrack.streamIndex} {outPath.Wrap()}";
                    await RunFfmpeg(args, LogMode.Hidden);
                    if (subtitleTracks.Count > 4) Program.mainForm.SetProgress(FormatUtils.RatioInt(counter, subtitleTracks.Count));
                    Logger.Log($"[FFCmds] Extracted subtitle track {subTrack.streamIndex} to {outPath} ({FormatUtils.Bytes(IOUtils.GetFilesize(outPath))})", true, false, "ffmpeg");
                    counter++;
                }

                if (subtitleTracks.Count > 0)
                {
                    Logger.Log($"Extracted {subtitleTracks.Count} subtitle tracks from the input video.", false, Logger.GetLastLine().Contains(msg));
                    Utils.ContainerSupportsSubs(Utils.GetExt(outMode), true);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error extracting subtitles: " + e.Message);
            }

            Program.mainForm.SetProgress(0);
        }

        public static async Task<List<SubtitleTrack>> GetSubtitleTracks(string inputFile)
        {
            List<SubtitleTrack> subtitleTracks = new List<SubtitleTrack>();
            string args = $"-i {inputFile.Wrap()}";
            Logger.Log("GetSubtitleTracks()", true, false, "ffmpeg");
            string[] outputLines = (await GetFfmpegOutputAsync(args)).SplitIntoLines();
            int idx = 0;

            for (int i = 0; i < outputLines.Length; i++)
            {
                string line = outputLines[i];
                if (!line.Contains(" Subtitle: ")) continue;

                string lang = "unknown";
                string subEnc = "UTF-8";

                if (line.Contains("(") && line.Contains("): Subtitle: "))   // Lang code in stream name, like "Stream #0:2(eng): Subtitle: ..."
                    lang = line.Split('(')[1].Split(')')[0];

                if ((i + 2 < outputLines.Length) && outputLines[i + 1].Contains("Metadata:") && outputLines[i + 2].Contains("SUB_CHARENC"))  // Subtitle encoding is in metadata!
                    subEnc = outputLines[i + 2].Remove("SUB_CHARENC").Remove(":").TrimWhitespaces();

                Logger.Log($"Found subtitle track #{idx} with language '{lang}' and encoding '{subEnc}'", true, false, "ffmpeg");
                subtitleTracks.Add(new SubtitleTrack(idx, lang, subEnc));
                idx++;
            }

            return subtitleTracks;
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
                subMetaArgs += $" -metadata:s:s:{subTrack} language={Path.GetFileNameWithoutExtension(subTracks[subTrack]).Split('_')[1]}";
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
                    IOUtils.TryMove(tempPath, inputFile);   // Move temp file back
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
