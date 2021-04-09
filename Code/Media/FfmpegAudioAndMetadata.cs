using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.AvProcess;
using Utils = Flowframes.Media.FFmpegUtils;
using I = Flowframes.Interpolate;

namespace Flowframes.Media
{
    partial class FfmpegAudioAndMetadata : FfmpegCommands
    {
        #region Audio

        public static async Task ExtractAudioTracks(string inputFile, string outFolder, bool showMsg = true)
        {
            string msg = "Extracting audio from video...";
            if (showMsg) Logger.Log(msg);

            List<AudioTrack> audioTracks = await GetAudioTracks(inputFile);
            int counter = 1;

            foreach (AudioTrack track in audioTracks)
            {
                if (I.canceled) break;

                string audioExt = Utils.GetAudioExt(inputFile, track.streamIndex);
                string outPath = Path.Combine(outFolder, $"{track.streamIndex}_{track.metadata}_audio.{audioExt}");
                string[] trim = FfmpegExtract.GetTrimArgs();
                string args = $"{trim[0]} -i {inputFile.Wrap()} {trim[1]} -map 0:{track.streamIndex} -vn -c:a copy {outPath.Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden, "panic");

                if (IOUtils.GetFilesize(outPath) < 512)
                {
                    // Logger.Log($"Failed to extract audio stream #{track.streamIndex} losslessly! Trying to re-encode.");
                    File.Delete(outPath);
                    outPath = Path.ChangeExtension(outPath, Utils.GetAudioExtForContainer(Path.GetExtension(inputFile)));
                    args = $"{trim[0]} -i {inputFile.Wrap()} {trim[1]} -vn {Utils.GetAudioFallbackArgs(I.current.outMode)} {outPath.Wrap()}";
                    await RunFfmpeg(args, LogMode.Hidden, "panic", TaskType.ExtractOther, true);

                    if (IOUtils.GetFilesize(outPath) < 512)
                    {
                        Logger.Log($"Failed to extract audio stream #{track.streamIndex}, even with re-encoding. Will be missing from output.");
                        IOUtils.TryDeleteIfExists(outPath);
                        return;
                    }

                    Logger.Log($"Audio stream #{track.streamIndex} has been re-encoded as it can't be extracted losslessly. This may decrease the quality slightly.", false, true);
                }

                if (audioTracks.Count > 1) Program.mainForm.SetProgress(FormatUtils.RatioInt(counter, audioTracks.Count));
                Logger.Log($"[FFCmds] Extracted audio track {track.streamIndex} to {outPath} ({FormatUtils.Bytes(IOUtils.GetFilesize(outPath))})", true, false, "ffmpeg");
                counter++;
            }
        }

        public static async Task<List<AudioTrack>> GetAudioTracks(string inputFile)
        {
            List<AudioTrack> audioTracks = new List<AudioTrack>();
            string args = $"-i {inputFile.Wrap()}";
            Logger.Log("GetAudioTracks()", true, false, "ffmpeg");
            string[] outputLines = (await GetFfmpegOutputAsync(args)).SplitIntoLines();

            for (int i = 0; i < outputLines.Length; i++)
            {
                string line = outputLines[i];
                if (!line.Contains(" Audio: ")) continue;

                int streamIndex = line.Remove("Stream #0:").Split(':')[0].Split('[')[0].GetInt();

                string meta = "";
                string codec = "";

                if ((i + 2 < outputLines.Length) && outputLines[i + 1].Contains("Metadata:") && (outputLines[i + 2].Contains("title") || outputLines[i + 2].Contains("lang")))
                    meta = outputLines[i + 2].Replace(":", "=").Remove(" ");

                Logger.Log($"Found audio stream #{streamIndex} {meta}", true, false, "ffmpeg");
                audioTracks.Add(new AudioTrack(streamIndex, meta, codec));
            }

            return audioTracks;
        }

        #endregion

        #region Subtitles

        public static async Task ExtractSubtitles(string inputFile, string outFolder, Interpolate.OutMode outMode,bool showMsg = true)
        {
            try
            {
                string msg = "Extracting subtitles from video...";
                if (showMsg) Logger.Log(msg);

                List<SubtitleTrack> subtitleTracks = await GetSubtitleTracks(inputFile);
                int counter = 1;
                int extractedSuccessfully = 0;

                foreach (SubtitleTrack subTrack in subtitleTracks)
                {
                    if (Interpolate.canceled) break;

                    string outPath = Path.Combine(outFolder, $"{subTrack.streamIndex}_{subTrack.lang}_{subTrack.encoding}.srt");
                    string[] trim = FfmpegExtract.GetTrimArgs();
                    string args = $"-sub_charenc {subTrack.encoding} {trim[0]} -i {inputFile.Wrap()} {trim[1]} -map 0:{subTrack.streamIndex} {outPath.Wrap()}";
                    await RunFfmpeg(args, LogMode.Hidden, "error");
                    if (subtitleTracks.Count > 4) Program.mainForm.SetProgress(FormatUtils.RatioInt(counter, subtitleTracks.Count));
                    counter++;

                    if(IOUtils.GetFilesize(outPath) >= 32)
                    {
                        Logger.Log($"[FFCmds] Extracted subtitle track {subTrack.streamIndex} to {outPath} ({FormatUtils.Bytes(IOUtils.GetFilesize(outPath))})", true, false, "ffmpeg");
                        extractedSuccessfully++;
                    }
                    else
                    {
                        IOUtils.TryDeleteIfExists(outPath);     // Delete if encode was not successful
                    }
                }

                if (extractedSuccessfully > 0)
                {
                    Logger.Log($"Extracted {extractedSuccessfully} subtitle tracks from the input video.", false, Logger.GetLastLine().Contains(msg));
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

            for (int i = 0; i < outputLines.Length; i++)
            {
                string line = outputLines[i];
                if (!line.Contains(" Subtitle: ")) continue;

                int streamIndex = line.Remove("Stream #0:").Split(':')[0].GetInt();

                string lang = "";
                string subEnc = "UTF-8";

                if (line.Contains("(") && line.Contains("): Subtitle: "))   // Lang code in stream name, like "Stream #0:2(eng): Subtitle: ..."
                    lang = line.Split('(')[1].Split(')')[0];

                if ((i + 2 < outputLines.Length) && outputLines[i + 1].Contains("Metadata:") && outputLines[i + 2].Contains("SUB_CHARENC"))  // Subtitle encoding is in metadata!
                    subEnc = outputLines[i + 2].Remove("SUB_CHARENC").Remove(":").TrimWhitespaces();

                Logger.Log($"Found subtitle track #{streamIndex} with language '{lang}' and encoding '{subEnc}'", true, false, "ffmpeg");
                subtitleTracks.Add(new SubtitleTrack(streamIndex, lang, subEnc));
            }

            return subtitleTracks;
        }

        #endregion

        #region Mux From Input
        public static async Task MergeStreamsFromInput (string inputVideo, string interpVideo, string tempFolder)
        {
            if (!File.Exists(inputVideo) && !I.current.inputIsFrames)
            {
                Logger.Log("Warning: Input video file not found, can't copy audio/subtitle streams to output video!");
                return;
            }

            string containerExt = Path.GetExtension(interpVideo);
            string tempPath = Path.Combine(tempFolder, $"vid{containerExt}");
            string outPath = Path.Combine(tempFolder, $"muxed{containerExt}");
            File.Move(interpVideo, tempPath);
            string inName = Path.GetFileName(tempPath);
            string outName = Path.GetFileName(outPath);

            string subArgs = "-c:s " + Utils.GetSubCodecForContainer(containerExt);

            bool audioCompat = Utils.ContainerSupportsAllAudioFormats(I.current.outMode, GetAudioCodecs(inputVideo));
            string audioArgs = audioCompat ? "" : Utils.GetAudioFallbackArgs(I.current.outMode);

            if (!audioCompat)
                Logger.Log("Warning: Input audio format(s) not fully supported in output container - Will re-encode.", true, false, "ffmpeg");

            bool audio = Config.GetBool("keepAudio");
            bool subs = Config.GetBool("keepSubs");
            bool meta = Config.GetBool("keepMeta");

            if (!audio)
                audioArgs = "-an";

            if (!subs || (subs && !Utils.ContainerSupportsSubs(containerExt)))
                subArgs = "-sn";

            bool isMkv = I.current.outMode == I.OutMode.VidMkv;
            string mkvFix = isMkv ? "-max_interleave_delta 0" : ""; // https://reddit.com/r/ffmpeg/comments/efddfs/starting_new_cluster_due_to_timestamp/
            string metaArg = (isMkv && meta) ? "-map 1:t?" : ""; // https://reddit.com/r/ffmpeg/comments/fw4jnh/how_to_make_ffmpeg_keep_attached_images_in_mkv_as/

            if (QuickSettingsTab.trimEnabled)
            {
                string otherStreamsName = $"otherStreams{containerExt}";

                string[] trim = FfmpegExtract.GetTrimArgs();
                string args1 = $"{trim[0]} -i {inputVideo.Wrap()} {trim[1]} -map 0 -map -0:v -map -0:d -c copy {audioArgs} {subArgs} {otherStreamsName}";  // Extract trimmed
                await RunFfmpeg(args1, tempFolder, LogMode.Hidden);

                string args2 = $"-i {inName} -i {otherStreamsName} -map 0:v:0 -map 1:a:? -map 1:s:? {metaArg} -c copy {audioArgs} {subArgs} {mkvFix} {outName}"; // Merge interp + trimmed original
                await RunFfmpeg(args2, tempFolder, LogMode.Hidden);

                IOUtils.TryDeleteIfExists(Path.Combine(tempFolder, otherStreamsName));
            }
            else   // If trimming is disabled we can pull the streams directly from the input file
            {
                string args = $"-i {inName} -i {inputVideo.Wrap()} -map 0:v:0 -map 1:a:? -map 1:s:? {metaArg} -c copy {audioArgs} {subArgs} {mkvFix} {outName}";
                await RunFfmpeg(args, tempFolder, LogMode.Hidden);
            }

            if (File.Exists(outPath) && IOUtils.GetFilesize(outPath) > 512)
            {
                File.Delete(tempPath);
                File.Move(outPath, interpVideo);
            }
            else
            {
                File.Move(tempPath, interpVideo);   // Muxing failed, move unmuxed video file back
            }
        }

        #endregion

        #region Mux From Extracted Streams

        public static async Task MergeAudioAndSubs(string interpVideo, string tempFolder)    // https://superuser.com/a/277667
        {
            string containerExt = Path.GetExtension(interpVideo);
            string tempPath = Path.Combine(tempFolder, $"vid{containerExt}");
            string outPath = Path.Combine(tempFolder, $"muxed{containerExt}");
            File.Move(interpVideo, tempPath);
            string inName = Path.GetFileName(tempPath);
            string outName = Path.GetFileName(outPath);

            bool audio = Config.GetBool("keepAudio");
            bool subs = Config.GetBool("keepSubs") && Utils.ContainerSupportsSubs(containerExt, false);

            string[] audioTracks = audio ? IOUtils.GetFilesSorted(tempFolder, false, "*_audio.*") : new string[0];     // Find audio files
            string[] subTracks = subs ? IOUtils.GetFilesSorted(tempFolder, false, "*.srt") : new string[0];     // Find subtitle files

            Dictionary<int, string> trackFiles = new Dictionary<int, string>();    // Dict holding all track files with their index

            foreach (string audioTrack in audioTracks)  // Loop through audio streams to add them to the dict
                trackFiles[Path.GetFileNameWithoutExtension(audioTrack).Split('_')[0].GetInt()] = audioTrack;   // Add file, dict key is stream index

            foreach (string subTrack in subTracks)  // Loop through subtitle streams to add them to the dict
                trackFiles[Path.GetFileNameWithoutExtension(subTrack).Split('_')[0].GetInt()] = subTrack;   // Add file, dict key is stream index

            string trackInputArgs = "";
            string trackMapArgs = "";
            string trackMetaArgs = "";

            SortedDictionary<int, string> sortedTrackFiles = new SortedDictionary<int, string>(trackFiles);

            int inputIndex = 1; // Input index (= output stream index) - Start at 1 since 0 is the video stream

            foreach (KeyValuePair<int, string> track in sortedTrackFiles)
            {
                int streamIndex = track.Key;
                string trackFile = track.Value;

                trackInputArgs += $" -i {Path.GetFileName(trackFile)}";     // Input filename

                if (Path.GetFileNameWithoutExtension(trackFile).Contains("_audio"))
                    trackMapArgs += $" -map {inputIndex}:a";  // Map input file (audio track)
                else
                    trackMapArgs += $" -map {inputIndex}:s";  // Map input file (subtitle track)

                string meta = Path.GetFileNameWithoutExtension(trackFile).Split('_')[1];

                if (!string.IsNullOrWhiteSpace(meta))
                {
                    if (Path.GetFileNameWithoutExtension(trackFile).Contains("_audio"))
                        trackMetaArgs += $" -metadata:s:{inputIndex} {meta}"; // Metadata
                    else
                        trackMetaArgs += $" -metadata:s:{inputIndex} language={meta}"; // Language
                }

                inputIndex++;
            }

            bool audioCompat = true;

            foreach (string audioTrack in audioTracks)
                if (!Utils.ContainerSupportsAudioFormat(I.current.outMode, Path.GetExtension(audioTrack)))
                    audioCompat = false;

            if(!audioCompat)
                Logger.Log("Warning: Input audio format(s) not fully supported in output container - Will re-encode.", true, false, "ffmpeg");

            string subArgs = "-c:s " + Utils.GetSubCodecForContainer(containerExt);
            string audioArgs = audioCompat ? "-c:a copy" : Utils.GetAudioFallbackArgs(I.current.outMode);

            string args = $" -i {inName} {trackInputArgs} -map 0:v {trackMapArgs} -c:v copy {audioArgs} {subArgs} {trackMetaArgs} {outName}";

            await RunFfmpeg(args, tempFolder, LogMode.Hidden);


            // if (File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 1024)
            // {
            //     Logger.Log("Failed to merge audio losslessly! Trying to re-encode.", false, false, "ffmpeg");
            // 
            //     args = $" -i {inName} -stream_loop {looptimes} -i {audioName.Wrap()}" +
            //     $"{trackInputArgs} -map 0:v -map 1:a {trackMapArgs} -c:v copy {Utils.GetAudioFallbackArgs(Path.GetExtension(inputFile))} -c:s {subCodec} {trackMetaArgs} -shortest {outName}";
            // 
            //     await RunFfmpeg(args, tempFolder, LogMode.Hidden);
            // 
            //     if (File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 1024)
            //     {
            //         Logger.Log("Failed to merge audio, even with re-encoding. Output will not have audio.", false, false, "ffmpeg");
            //         IOUtils.TryMove(tempPath, inputFile);   // Move temp file back
            //         IOUtils.TryDeleteIfExists(tempPath);
            //         return;
            //     }
            // 
            //     string audioExt = Path.GetExtension(audioPath).Remove(".").ToUpper();
            //     Logger.Log($"Source audio ({audioExt}) has been re-encoded to fit into the target container ({containerExt.Remove(".").ToUpper()}). This may decrease the quality slightly.", false, true, "ffmpeg");
            // }

            if (File.Exists(outPath) && IOUtils.GetFilesize(outPath) > 512)
            {
                File.Delete(tempPath);
                File.Move(outPath, interpVideo);
            }
            else
            {
                File.Move(tempPath, interpVideo);
            }
        }

        #endregion
    }
}
