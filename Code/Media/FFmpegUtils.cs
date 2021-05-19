using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flowframes.Media
{
    class FFmpegUtils
    {
        public enum Codec { H264, H265, H264NVENC, H265NVENC, AV1, VP9, ProRes, AviRaw }


        public static Codec GetCodec(Interpolate.OutMode mode)
        {
            if (mode == Interpolate.OutMode.VidMp4 || mode == Interpolate.OutMode.VidMkv)
            {
                int mp4Enc = Config.GetInt(Config.Key.mp4Enc);
                if (mp4Enc == 0) return Codec.H264;
                if (mp4Enc == 1) return Codec.H265;
                if (mp4Enc == 2) return Codec.H264NVENC;
                if (mp4Enc == 3) return Codec.H265NVENC;
                if (mp4Enc == 4) return Codec.AV1;
            }

            if (mode == Interpolate.OutMode.VidWebm)
                return Codec.VP9;

            if (mode == Interpolate.OutMode.VidProRes)
                return Codec.ProRes;

            if (mode == Interpolate.OutMode.VidAvi)
                return Codec.AviRaw;

            return Codec.H264;
        }

        public static string GetEnc(Codec codec)
        {
            switch (codec)
            {
                case Codec.H264: return "libx264";
                case Codec.H265: return "libx265";
                case Codec.H264NVENC: return "h264_nvenc";
                case Codec.H265NVENC: return "hevc_nvenc";
                case Codec.AV1: return "libsvtav1";
                case Codec.VP9: return "libvpx-vp9";
                case Codec.ProRes: return "prores_ks";
                case Codec.AviRaw: return Config.Get(Config.Key.aviCodec);
            }
            return "libx264";
        }

        public static string GetEncArgs (Codec codec)
        {
            string args = $"-c:v { GetEnc(codec)} ";

            if(codec == Codec.H264)
            {
                string preset = Config.Get(Config.Key.ffEncPreset).ToLower().Remove(" ");
                args += $"-crf {Config.GetInt(Config.Key.h264Crf)} -preset {preset} -pix_fmt yuv420p";
            }

            if (codec == Codec.H265)
            {
                string preset = Config.Get(Config.Key.ffEncPreset).ToLower().Remove(" ");
                int crf = Config.GetInt(Config.Key.h265Crf);
                args += $"{(crf > 0 ? $"-crf {crf}" : "-x265-params lossless=1")} -preset {preset} -pix_fmt yuv420p";
            }

            if (codec == Codec.H264NVENC)
            {
                int cq = (Config.GetInt(Config.Key.h264Crf) * 1.1f).RoundToInt();
                args += $"-b:v 0 {(cq > 0 ? $"-cq {cq} -preset p7" : "-preset lossless")} -pix_fmt yuv420p";
            }

            if (codec == Codec.H265NVENC)
            {
                int cq = (Config.GetInt(Config.Key.h265Crf) * 1.1f).RoundToInt();
                args += $"-b:v 0 {(cq > 0 ? $"-cq {cq} -preset p7" : "-preset lossless")} -pix_fmt yuv420p";
            }

            if (codec == Codec.AV1)
            {
                int cq = (Config.GetInt(Config.Key.av1Crf) * 1.0f).RoundToInt();
                args += $"-b:v 0 -qp {cq} -g 240 {GetSvtAv1Speed()} -tile_rows 2 -tile_columns 2 -pix_fmt yuv420p";
            }

            if (codec == Codec.VP9)
            {
                int crf = Config.GetInt(Config.Key.vp9Crf);
                string qualityStr = (crf > 0) ? $"-b:v 0 -crf {crf}" : "-lossless 1";
                args += $"{qualityStr} {GetVp9Speed()} -tile-columns 2 -tile-rows 2 -row-mt 1 -pix_fmt yuv420p";
            }

            if(codec == Codec.ProRes)
            {
                args += $"-profile:v {Config.GetInt(Config.Key.proResProfile)} -pix_fmt yuv420p";
            }

            if (codec == Codec.AviRaw)
            {
                args += $"-pix_fmt {Config.Get(Config.Key.aviColors)}";
            }

            return args;
        }

        static string GetVp9Speed ()
        {
            string preset = Config.Get(Config.Key.ffEncPreset).ToLower().Remove(" ");
            string arg = "";

            if (preset == "veryslow") arg = "0";
            if (preset == "slower") arg = "1";
            if (preset == "slow") arg = "2";
            if (preset == "medium") arg = "3";
            if (preset == "fast") arg = "4";
            if (preset == "faster") arg = "5";
            if (preset == "veryfast") arg = "4 -deadline realtime";

            return $"-cpu-used {arg}";
        }

        static string GetSvtAv1Speed()
        {
            string preset = Config.Get(Config.Key.ffEncPreset).ToLower().Remove(" ");
            string arg = "";

            if (preset == "veryslow") arg = "2";
            if (preset == "slower") arg = "3";
            if (preset == "slow") arg = "4";
            if (preset == "medium") arg = "5";
            if (preset == "fast") arg = "6";
            if (preset == "faster") arg = "7";
            if (preset == "veryfast") arg = "8";

            return $"-preset {arg}";
        }

        public static bool ContainerSupportsAllAudioFormats (Interpolate.OutMode outMode, List<string> codecs)
        {
            if(codecs.Count < 1)
                Logger.Log($"Warning: ContainerSupportsAllAudioFormats() was called, but codec list has {codecs.Count} entries.", true, false, "ffmpeg");

            foreach (string format in codecs)
            {
                if (!ContainerSupportsAudioFormat(outMode, format))
                    return false;
            }

            return true;
        }

        public static bool ContainerSupportsAudioFormat (Interpolate.OutMode outMode, string format)
        {
            bool supported = false;
            string alias = GetAudioExt(format);

            string[] formatsMp4 = new string[] { "m4a", "ac3", "dts" };
            string[] formatsMkv = new string[] { "m4a", "ac3", "dts", "ogg", "mp2", "mp3", "wav", "wma" };
            string[] formatsWebm = new string[] { "ogg" };
            string[] formatsProres = new string[] { "m4a", "ac3", "dts", "wav" };
            string[] formatsAvi = new string[] { "m4a", "ac3", "dts" };

            switch (outMode)
            {
                case Interpolate.OutMode.VidMp4: supported = formatsMp4.Contains(alias); break;
                case Interpolate.OutMode.VidMkv: supported = formatsMkv.Contains(alias); break;
                case Interpolate.OutMode.VidWebm: supported = formatsWebm.Contains(alias); break;
                case Interpolate.OutMode.VidProRes: supported = formatsProres.Contains(alias); break;
                case Interpolate.OutMode.VidAvi: supported = formatsAvi.Contains(alias); break;
            }

            Logger.Log($"Checking if {outMode} supports audio format '{format}' ({alias}): {supported}", true, false, "ffmpeg");
            return supported;
        }

        public static string GetExt(Interpolate.OutMode outMode, bool dot = true)
        {
            string ext = dot ? "." : "";

            switch (outMode)
            {
                case Interpolate.OutMode.VidMp4: ext += "mp4"; break;
                case Interpolate.OutMode.VidMkv: ext += "mkv"; break;
                case Interpolate.OutMode.VidWebm: ext += "webm"; break;
                case Interpolate.OutMode.VidProRes: ext += "mov"; break;
                case Interpolate.OutMode.VidAvi: ext += "avi"; break;
                case Interpolate.OutMode.VidGif: ext += "gif"; break;
            }

            return ext;
        }

        public static string GetAudioExt(string videoFile, int streamIndex = -1)
        {
            return GetAudioExt(FfmpegCommands.GetAudioCodec(videoFile, streamIndex));
        }

        public static string GetAudioExt (string codec)
        {
            if (codec.StartsWith("pcm_"))
                return "wav";

            switch (codec)
            {
                case "vorbis": return "ogg";
                case "opus": return "ogg";
                case "mp2": return "mp2";
                case "mp3": return "mp3";
                case "aac": return "m4a";
                case "ac3": return "ac3";
                case "eac3": return "ac3";
                case "dts": return "dts";
                case "alac": return "wav";
                case "flac": return "wav";
                case "wmav1": return "wma";
                case "wmav2": return "wma";
            }

            return "unsupported";
        }

        public static string GetAudioFallbackArgs (Interpolate.OutMode outMode)
        {
            string codec = "aac";
            string bitrate = $"{Config.GetInt(Config.Key.aacBitrate, 160)}";

            if(outMode == Interpolate.OutMode.VidMkv || outMode == Interpolate.OutMode.VidWebm)
            {
                codec = "libopus";
                bitrate = $"{Config.GetInt(Config.Key.opusBitrate, 128)}";
            }

            return $"-c:a {codec} -b:a {bitrate}k -ac 2";
        }

        public static string GetAudioExtForContainer(string containerExt)
        {
            containerExt = containerExt.Remove(".");
            string ext = "m4a";

            if (containerExt == "webm" || containerExt == "mkv")
                ext = "ogg";

            return ext;
        }

        public static string GetSubCodecForContainer(string containerExt)
        {
            containerExt = containerExt.Remove("."); 

            if (containerExt == "mp4") return "mov_text";
            if (containerExt == "webm") return "webvtt";

            return "copy";    // Default: Copy SRT subs
        }

        public static bool ContainerSupportsSubs(string containerExt, bool showWarningIfNotSupported = true)
        {
            containerExt = containerExt.Remove(".");
            bool supported = (containerExt == "mp4" || containerExt == "mkv" || containerExt == "webm" || containerExt == "mov");
            Logger.Log($"Subtitles {(supported ? "are supported" : "not supported")} by {containerExt.ToUpper()}", true);

            if (showWarningIfNotSupported && Config.GetBool(Config.Key.keepSubs) && !supported)
                Logger.Log($"Warning: Subtitle transfer is enabled, but {containerExt.ToUpper()} does not support subtitles properly. MKV is recommended instead.");
           
            return supported;
        }

        public static void CreateConcatFile (string inputFilesDir, string outputPath, string[] validExtensions = null)
        {
            string concatFileContent = "";
            string[] files = IOUtils.GetFilesSorted(inputFilesDir);

            foreach (string file in files)
            {
                if (validExtensions != null && !validExtensions.Contains(Path.GetExtension(file).ToLower()))
                    continue;

                concatFileContent += $"file '{file.Replace(@"\", "/")}'\n";
            }

            File.WriteAllText(outputPath, concatFileContent);
        }
    }
}
