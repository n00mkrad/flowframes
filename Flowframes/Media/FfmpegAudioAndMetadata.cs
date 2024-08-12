using Flowframes.IO;
using Flowframes.Ui;
using System.IO;
using System.Threading.Tasks;
using static Flowframes.AvProcess;
using Utils = Flowframes.Media.FfmpegUtils;
using I = Flowframes.Interpolate;

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
            string mkvFix = isMkv ? "-max_interleave_delta 0" : ""; // https://reddit.com/r/ffmpeg/comments/efddfs/starting_new_cluster_due_to_timestamp/
            string metaArg = (isMkv && meta) ? "-map 1:t?" : ""; // https://reddit.com/r/ffmpeg/comments/fw4jnh/how_to_make_ffmpeg_keep_attached_images_in_mkv_as/
            string shortestArg = shortest ? "-shortest" : "";

            if (QuickSettingsTab.trimEnabled)
            {
                string otherStreamsName = $"otherStreams{containerExt}";

                string[] trim = FfmpegExtract.GetTrimArgs();
                string args1 = $"{trim[0]} -i {inputVideo.Wrap()} {trim[1]} -map 0 -map -0:v -map -0:d -c copy {audioArgs} {subArgs} {otherStreamsName}";  // Extract trimmed
                await RunFfmpeg(args1, tempFolder, LogMode.Hidden);

                string args2 = $"-i {inName} -i {otherStreamsName} -map 0:v:0 -map 1:a:? -map 1:s:? {metaArg} -c copy {audioArgs} {subArgs} {mkvFix} {shortestArg} {outName}"; // Merge interp + trimmed original
                await RunFfmpeg(args2, tempFolder, LogMode.Hidden);

                IoUtils.TryDeleteIfExists(Path.Combine(tempFolder, otherStreamsName));
            }
            else   // If trimming is disabled we can pull the streams directly from the input file
            {
                string args = $"-i {inName} -i {inputVideo.Wrap()} -map 0:v:0 -map 1:a:? -map 1:s:? {metaArg} -c copy {audioArgs} {subArgs} {mkvFix} {shortestArg} {outName}";
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
