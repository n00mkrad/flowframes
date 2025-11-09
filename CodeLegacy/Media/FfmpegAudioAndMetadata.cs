using Flowframes.IO;
using Flowframes.Main;
using Flowframes.Ui;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.AvProcess;
using I = Flowframes.Interpolate;
using Utils = Flowframes.Media.FfmpegUtils;

namespace Flowframes.Media
{
    partial class FfmpegAudioAndMetadata : FfmpegCommands
    {

        public static async Task MergeStreamsFromInput (string inputVideo, string interpVideo, string tempFolder, bool shortest)
        {
            if (!File.Exists(inputVideo) && !I.currentSettings.inputIsFrames)
            {
                Logger.Log("Warning: Input video file not found, can't copy audio/subtitle streams to output video!");
                return;
            }

            Data.Enums.Output.Format format = I.currentSettings.outSettings.Format;

            if (format == Data.Enums.Output.Format.Gif || format == Data.Enums.Output.Format.Images)
            {
                Logger.Log("Warning: Output format does not support audio.");
                return;
            }

            string containerExt = Path.GetExtension(interpVideo);
            string tempPath = Path.Combine(tempFolder, $"vid{containerExt}");
            string outPath = Path.Combine(tempFolder, $"muxed{containerExt}");
            IoUtils.TryDeleteIfExists(tempPath);
            File.Move(interpVideo, tempPath);
            string inName = Path.GetFileName(tempPath);
            string outName = Path.GetFileName(outPath);

            string subArgs = "-c:s " + Utils.GetSubCodecForContainer(containerExt);

            bool audioCompat = Utils.ContainerSupportsAllAudioFormats(I.currentSettings.outSettings.Format, GetAudioCodecs(inputVideo));
            bool slowmo = I.currentSettings.outItsScale != 0 && I.currentSettings.outItsScale != 1;
            string audioArgs = audioCompat && !slowmo ? "" : await Utils.GetAudioFallbackArgs(inputVideo, I.currentSettings.outSettings.Format, I.currentSettings.outItsScale);

            if (!audioCompat && !slowmo)
                Logger.Log("Warning: Input audio format(s) not fully supported in output container - Will re-encode.", true, false, "ffmpeg");

            bool audio = Config.GetBool(Config.Key.keepAudio);
            bool subs = Config.GetBool(Config.Key.keepSubs);
            bool meta = Config.GetBool(Config.Key.keepMeta);

            if (!audio)
                audioArgs = "-an";

            if (!subs || (subs && !Utils.ContainerSupportsSubs(containerExt)))
                subArgs = "-sn";

            bool isMkv = I.currentSettings.outSettings.Format == Data.Enums.Output.Format.Mkv;
            var muxArgs = new List<string>() { "-map 0:v:0", "-map 1:a:?", "-map 1:s:?", "-c copy", audioArgs, subArgs };
            muxArgs.AddIf("-max_interleave_delta 0", isMkv); // https://reddit.com/r/ffmpeg/comments/efddfs/starting_new_cluster_due_to_timestamp/
            muxArgs.AddIf("-map 1:t?", isMkv && meta); // https://reddit.com/r/ffmpeg/comments/fw4jnh/how_to_make_ffmpeg_keep_attached_images_in_mkv_as/
            muxArgs.AddIf("-shortest", shortest);
            muxArgs.AddIf($"-aspect {I.currentMediaFile.VideoExtraData.Dar}", I.currentMediaFile.VideoExtraData.Dar.MatchesWildcard("*:*"));
            string muxArgsStr = $"{string.Join(" ", muxArgs)} {outName}";
            var inputArgs = new List<string>();
            inputArgs.AddIf($"-display_rotation {I.currentMediaFile.VideoExtraData.Rotation}", I.currentMediaFile.VideoExtraData.Rotation != 0);
            string inputArgsStr = $"{string.Join(" ", inputArgs)} -i {inName}";

            if (I.currentMediaFile.IsVfr && I.currentMediaFile.OutputTimestamps.Any())
            {
                Export.MuxTimestamps(tempPath);
            }

            if (QuickSettingsTab.trimEnabled)
            {
                string otherStreamsName = $"otherStreams{containerExt}";

                string[] trim = FfmpegExtract.GetTrimArgs();
                string args1 = $"{trim[0]} -i {inputVideo.Wrap()} {trim[1]} -map 0 -map -0:v -map -0:d -c copy {audioArgs} {subArgs} {otherStreamsName}";  // Extract trimmed
                await RunFfmpeg(args1, tempFolder, LogMode.Hidden);

                string args2 = $"{inputArgsStr} -i {otherStreamsName} {muxArgsStr}"; // Merge interp + trimmed original
                await RunFfmpeg(args2, tempFolder, LogMode.Hidden);

                IoUtils.TryDeleteIfExists(Path.Combine(tempFolder, otherStreamsName));
            }
            else   // If trimming is disabled we can pull the streams directly from the input file
            {
                string args = $"{inputArgsStr} -i {inputVideo.Wrap()} {muxArgsStr}";
                await RunFfmpeg(args, tempFolder, LogMode.Hidden);
            }

            if (File.Exists(outPath) && IoUtils.GetFilesize(outPath) > 512)
            {
                File.Delete(tempPath);
                File.Move(outPath, interpVideo);
            }
            else
            {
                File.Move(tempPath, interpVideo);   // Muxing failed, move unmuxed video file back
            }
        }
    }
}
