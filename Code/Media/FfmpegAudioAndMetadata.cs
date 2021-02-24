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
        #region Audio

        public static async Task ExtractAudioTracks(string inputFile, string outFolder, bool showMsg = true)
        {
            string msg = "Extracting audio from video...";
            if (showMsg) Logger.Log(msg);

            List<AudioTrack> audioTracks = await GetAudioTracks(inputFile);
            int counter = 1;

            foreach (AudioTrack track in audioTracks)
            {
                string audioExt = Utils.GetAudioExt(inputFile, track.streamIndex);
                string outPath = Path.Combine(outFolder, $"{track.streamIndex}_{track.metadata}_audio.{audioExt}");
                string args = $" -loglevel panic -i {inputFile.Wrap()} -map 0:{track.streamIndex} -vn -c:a copy {outPath.Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden);

                if (File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 512)
                {
                    Logger.Log($"Failed to extract audio stream #{track.streamIndex} losslessly! Trying to re-encode.");
                    File.Delete(outPath);
                    outPath = Path.ChangeExtension(outPath, Utils.GetAudioExtForContainer(Path.GetExtension(inputFile)));
                    args = $" -loglevel panic -i {inputFile.Wrap()} -vn {Utils.GetAudioFallbackArgs(Path.GetExtension(inputFile))} {outPath.Wrap()}";
                    await RunFfmpeg(args, LogMode.Hidden, TaskType.ExtractOther, true);

                    if (File.Exists(outPath) && IOUtils.GetFilesize(outPath) < 512)
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

                int streamIndex = line.Remove("Stream #0:").Split(':')[0].GetInt();

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

                foreach (SubtitleTrack subTrack in subtitleTracks)
                {
                    string outPath = Path.Combine(outFolder, $"{subTrack.streamIndex}_{subTrack.lang}_{subTrack.encoding}.srt");
                    string args = $" -loglevel error -sub_charenc {subTrack.encoding} -i {inputFile.Wrap()} -map 0:{subTrack.streamIndex} {outPath.Wrap()}";
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

        public static async Task MergeAudioAndSubs(string inputFile, string tempFolder)    // https://superuser.com/a/277667
        {
            string containerExt = Path.GetExtension(inputFile);
            string tempPath = Path.Combine(tempFolder, $"vid{containerExt}");
            string outPath = Path.Combine(tempFolder, $"muxed{containerExt}");
            File.Move(inputFile, tempPath);
            string inName = Path.GetFileName(tempPath);
            string outName = Path.GetFileName(outPath);

            bool audio = Config.GetBool("keepAudio");
            bool subs = Utils.ContainerSupportsSubs(containerExt, false) && Config.GetBool("keepSubs");

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
                trackMapArgs += $" -map {inputIndex}";  // Map input file

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

            bool allAudioCodecsSupported = true;

            foreach (string audioTrack in audioTracks)
                if (!Utils.ContainerSupportsAudioFormat(Interpolate.current.outMode, Path.GetExtension(audioTrack)))
                    allAudioCodecsSupported = false;

            if(!allAudioCodecsSupported)
                Logger.Log("Warning: Input audio format(s) not fully support in output container. Audio transfer will not be lossless.", false, false, "ffmpeg");

            string subArgs = "-c:s " + Utils.GetSubCodecForContainer(containerExt);
            string audioArgs = allAudioCodecsSupported ? "-c:a copy" : Utils.GetAudioFallbackArgs(Path.GetExtension(inputFile));

            string args = $" -i {inName} {trackInputArgs} -map 0:v {trackMapArgs} -c:v copy {audioArgs} {subArgs} {trackMetaArgs} -shortest {outName}";

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
                File.Move(outPath, inputFile);
            }
            else
            {
                File.Move(tempPath, inputFile);
            }
        }
    }
}
