using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Media
{
    class FFmpegUtils
    {
        public enum Codec { H264, H265, NVENC264, NVENC265, VP9, ProRes, AviRaw }


        public static Codec GetCodec(Interpolate.OutMode mode)
        {
            if (mode == Interpolate.OutMode.VidMp4 || mode == Interpolate.OutMode.VidMkv)
            {
                int mp4Enc = Config.GetInt("mp4Enc");
                if (mp4Enc == 0) return Codec.H264;
                if (mp4Enc == 1) return Codec.H265;
                if (mp4Enc == 2) return Codec.NVENC264;
                if (mp4Enc == 3) return Codec.NVENC265;
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
                case Codec.NVENC264: return "h264_nvenc";
                case Codec.NVENC265: return "hevc_nvenc";
                case Codec.VP9: return "libvpx-vp9";
                case Codec.ProRes: return "prores_ks";
                case Codec.AviRaw: return Config.Get("aviCodec");
            }
            return "libx264";
        }

        public static string GetEncArgs (Codec codec)
        {
            string args = $"-c:v { GetEnc(codec)} ";

            if(codec == Codec.H264)
            {
                args += $"-crf {Config.GetInt("h264Crf")} -preset {Config.Get("ffEncPreset")} -pix_fmt yuv420p";
            }

            if (codec == Codec.H265)
            {
                args += $"-crf {Config.GetInt("h265Crf")} -preset {Config.Get("ffEncPreset")} -pix_fmt yuv420p";
            }

            if (codec == Codec.NVENC264)
            {
                args += $"-cq {Config.GetInt("h264Crf")} -preset slow -pix_fmt yuv420p";
            }

            if (codec == Codec.NVENC265)
            {
                args += $"-cq {Config.GetInt("h265Crf")} -preset slow -pix_fmt yuv420p";
            }

            if (codec == Codec.VP9)
            {
                int crf = Config.GetInt("vp9Crf");
                string qualityStr = (crf > 0) ? $"-crf {crf}" : "-lossless 1";
                string cpuUsed = Config.GetInt("vp9Speed", 3).ToString();
                args += $"{qualityStr} -cpu-used {cpuUsed} -tile-columns 2 -tile-rows 2 -row-mt 1 -pix_fmt yuv420p";
            }

            if(codec == Codec.ProRes)
            {
                args += $"-profile:v {Config.GetInt("proResProfile")} -pix_fmt yuv420p";
            }

            if (codec == Codec.AviRaw)
            {
                args += $"-pix_fmt {Config.Get("aviColors")}";
            }

            return args;
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
            string alias = GetAudioExt(format);
            Logger.Log($"Checking if {outMode} supports audio format '{format}' (alias {alias})", true, false, "ffmpeg");

            string[] formatsMp4 = new string[] { "m4a", "ac3", "dts" };
            string[] formatsMkv = new string[] { "m4a", "ac3", "dts", "ogg", "mp2", "wav" };
            string[] formatsWebm = new string[] { "ogg" };
            string[] formatsProres = new string[] { "m4a", "ac3", "dts", "wav" };
            string[] formatsAvi = new string[] { "m4a", "ac3", "dts" };

            switch (outMode)
            {
                case Interpolate.OutMode.VidMp4: return formatsMp4.Contains(alias);
                case Interpolate.OutMode.VidMkv: return formatsMkv.Contains(alias);
                case Interpolate.OutMode.VidWebm: return formatsWebm.Contains(alias);
                case Interpolate.OutMode.VidProRes: return formatsProres.Contains(alias);
                case Interpolate.OutMode.VidAvi: return formatsAvi.Contains(alias);
            }

            return false;
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
            }

            return "unsupported";
        }

        public static string GetAudioFallbackArgs (Interpolate.OutMode outMode)
        {
            string codec = "aac";
            string bitrate = $"{Config.GetInt("aacBitrate", 160)}";

            if(outMode == Interpolate.OutMode.VidMkv || outMode == Interpolate.OutMode.VidWebm)
            {
                codec = "libopus";
                bitrate = $"{Config.GetInt("opusBitrate", 128)}";
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

            if (showWarningIfNotSupported && Config.GetBool("keepSubs") && !supported)
                Logger.Log($"Warning: Subtitle transfer is enabled, but {containerExt.ToUpper()} does not support subtitles properly. MKV is recommended instead.");
           
            return supported;
        }
    }
}
