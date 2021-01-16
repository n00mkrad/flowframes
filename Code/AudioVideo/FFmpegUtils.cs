using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.AudioVideo
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
                args += $"-crf {Config.GetInt("h264Crf")} -preset default -pix_fmt yuv420p";
            }

            if (codec == Codec.NVENC265)
            {
                args += $"-crf {Config.GetInt("h265Crf")} -preset default -pix_fmt yuv420p";
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

        public static string GetAudioEnc(Codec codec)
        {
            switch (codec)
            {
                case Codec.VP9: return "libopus";
            }
            return "aac";
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

        public static string GetAudioExt(string videoFile)
        {
            switch (FFmpegCommands.GetAudioCodec(videoFile))
            {
                case "vorbis": return "ogg";
                case "opus": return "ogg";
                case "mp2": return "mp2";
                case "aac": return "m4a";
                default: return "wav";
            }
        }

        public static string GetAudioFallbackArgs (string containerExt)
        {
            containerExt = containerExt.Remove(".");
            string codec = "aac";
            string bitrate = $"{Config.GetInt("aacBitrate", 160)}k";

            if(containerExt == "webm" || containerExt == "mkv")
            {
                codec = "libopus";
                bitrate = $"{Config.GetInt("opusBitrate", 128)}";
            }

            return $"-c:a {codec} -b:a {bitrate} -ac 2";
        }

        public static string GetAudioExtForContainer(string containerExt)
        {
            containerExt = containerExt.Remove(".");
            string ext = "m4a";

            if (containerExt == "webm" || containerExt == "mkv")
                ext = "ogg";

            return ext;
        }
    }
}
