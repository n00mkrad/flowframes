using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flowframes.Media
{
    class FfmpegUtils
    {
        public enum Codec { H264, H265, H264Nvenc, H265Nvenc, Av1, Vp9, ProRes, AviRaw, Gif }


        public static Codec GetCodec(Interpolate.OutMode mode)
        {
            if (mode == Interpolate.OutMode.VidMp4 || mode == Interpolate.OutMode.VidMkv)
            {
                int mp4MkvEnc = Config.GetInt(Config.Key.mp4Enc);
                if (mp4MkvEnc == 0) return Codec.H264;
                if (mp4MkvEnc == 1) return Codec.H265;
                if (mp4MkvEnc == 2) return Codec.H264Nvenc;
                if (mp4MkvEnc == 3) return Codec.H265Nvenc;
                if (mp4MkvEnc == 4) return Codec.Av1;
            }

            if (mode == Interpolate.OutMode.VidWebm)
                return Codec.Vp9;

            if (mode == Interpolate.OutMode.VidProRes)
                return Codec.ProRes;

            if (mode == Interpolate.OutMode.VidAvi)
                return Codec.AviRaw;

            if (mode == Interpolate.OutMode.VidGif)
                return Codec.Gif;

            return Codec.H264;
        }

        public static string GetEnc(Codec codec)
        {
            switch (codec)
            {
                case Codec.H264: return "libx264";
                case Codec.H265: return "libx265";
                case Codec.H264Nvenc: return "h264_nvenc";
                case Codec.H265Nvenc: return "hevc_nvenc";
                case Codec.Av1: return "libsvtav1";
                case Codec.Vp9: return "libvpx-vp9";
                case Codec.ProRes: return "prores_ks";
                case Codec.AviRaw: return Config.Get(Config.Key.aviCodec);
                case Codec.Gif: return "gif";
            }

            return "libx264";
        }

        public static string[] GetEncArgs(Codec codec, Size res, float fps, bool realtime = false) // Array contains as many entries as there are encoding passes. If "realtime" is true, force single pass.
        {
            int keyint = 10;

            if (codec == Codec.H264)
            {
                string preset = Config.Get(Config.Key.ffEncPreset).ToLower().Remove(" ");
                string g = GetKeyIntArg(fps, keyint);
                return new string[] { $"-c:v {GetEnc(codec)} -crf {Config.GetInt(Config.Key.h264Crf)} -preset {preset} {g} -pix_fmt {GetPixFmt()}" };
            }

            if (codec == Codec.H265)
            {
                string preset = Config.Get(Config.Key.ffEncPreset).ToLower().Remove(" ");
                int crf = Config.GetInt(Config.Key.h265Crf);
                string g = GetKeyIntArg(fps, keyint);
                return new string[] { $"-c:v {GetEnc(codec)} {(crf > 0 ? $"-crf {crf}" : "-x265-params lossless=1")} -preset {preset} {g} -pix_fmt {GetPixFmt()}" };
            }

            if (codec == Codec.H264Nvenc)
            {
                int cq = (Config.GetInt(Config.Key.h264Crf) * 1.1f).RoundToInt();
                return new string[] { $"-c:v {GetEnc(codec)} -b:v 0 {(cq > 0 ? $"-cq {cq} -preset p7" : "-preset lossless")} -pix_fmt {GetPixFmt()}" };
            }

            if (codec == Codec.H265Nvenc)
            {
                int cq = (Config.GetInt(Config.Key.h265Crf) * 1.1f).RoundToInt();
                return new string[] { $"-c:v {GetEnc(codec)} -b:v 0 {(cq > 0 ? $"-cq {cq} -preset p7" : "-preset lossless")} -pix_fmt {GetPixFmt()}" };
            }

            if (codec == Codec.Av1)
            {
                int cq = Config.GetInt(Config.Key.av1Crf);
                string g = GetKeyIntArg(fps, keyint);
                return new string[] { $"-c:v {GetEnc(codec)} -b:v 0 -qp {cq} {GetSvtAv1Speed()} {g} -pix_fmt {GetPixFmt()}" };
            }

            if (codec == Codec.Vp9)
            {
                int crf = Config.GetInt(Config.Key.vp9Crf);
                string qualityStr = (crf > 0) ? $"-crf {crf}" : "-lossless 1";
                string g = GetKeyIntArg(fps, keyint);
                string t = GetTilingArgs(res, "-tile-columns ", "-tile-rows ");

                if (realtime) // Force 1-pass
                {
                    return new string[] { $"-c:v {GetEnc(codec)} -b:v 0 {qualityStr} {GetVp9Speed()} {t} -row-mt 1 {g} -pix_fmt {GetPixFmt()}" };
                }
                else
                {
                    return new string[] { 
                        $"-c:v {GetEnc(codec)} -b:v 0 {qualityStr} {GetVp9Speed()} {t} -row-mt 1 {g} -pass 1 -pix_fmt {GetPixFmt()} -an", 
                        $"-c:v {GetEnc(codec)} -b:v 0 {qualityStr} {GetVp9Speed()} {t} -row-mt 1 {g} -pass 2 -pix_fmt {GetPixFmt()}"
                    };
                }
            }

            if (codec == Codec.ProRes)
            {
                return new string[] { $"-c:v {GetEnc(codec)} -profile:v {Config.GetInt(Config.Key.proResProfile)} -pix_fmt {GetPixFmt()}" };
            }

            if (codec == Codec.AviRaw)
            {
                return new string[] { $"-c:v {GetEnc(codec)} -pix_fmt {Config.Get(Config.Key.aviColors)}" };
            }

            if (codec == Codec.Gif)
            {
                return new string[] { $"-c:v {GetEnc(codec)} -gifflags -offsetting" };
            }

            return new string[0];
        }

        public static string GetTilingArgs(Size resolution, string colArg, string rowArg)
        {
            int cols = 0;
            if (resolution.Width >= 1920) cols = 1;
            if (resolution.Width >= 3840) cols = 2;
            if (resolution.Width >= 7680) cols = 3;

            int rows = 0;
            if (resolution.Height >= 1600) rows = 1;
            if (resolution.Height >= 3200) rows = 2;
            if (resolution.Height >= 6400) rows = 3;

            Logger.Log($"GetTilingArgs: Video resolution is {resolution.Width}x{resolution.Height} - Using 2^{cols} columns, 2^{rows} rows (=> {Math.Pow(2, cols)}x{Math.Pow(2, rows)} = {Math.Pow(2, cols) * Math.Pow(2, rows)} Tiles)", true);

            return $"{(cols > 0 ? colArg+cols : "")} {(rows > 0 ? rowArg + rows : "")}";
        }

        public static string GetKeyIntArg(float fps, int intervalSeconds, string arg = "-g ")
        {
            int keyInt = (fps * intervalSeconds).RoundToInt().Clamp(20, 300);
            return $"{arg}{keyInt}";
        }

        static string GetVp9Speed()
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
            string arg = "8";

            if (preset == "veryslow") arg = "3";
            if (preset == "slower") arg = "4";
            if (preset == "slow") arg = "5";
            if (preset == "medium") arg = "6";
            if (preset == "fast") arg = "7";
            if (preset == "faster") arg = "8";
            if (preset == "veryfast") arg = "9";

            return $"-preset {arg}";
        }

        static string GetPixFmt()
        {
            switch (Config.GetInt(Config.Key.pixFmt))
            {
                case 0: return "yuv420p";
                case 1: return "yuv444p";
                case 2: return "yuv420p10le";
                case 3: return "yuv444p10le";
            }

            return "yuv420p";
        }

        public static bool ContainerSupportsAllAudioFormats(Interpolate.OutMode outMode, List<string> codecs)
        {
            if (codecs.Count < 1)
                Logger.Log($"Warning: ContainerSupportsAllAudioFormats() was called, but codec list has {codecs.Count} entries.", true, false, "ffmpeg");

            foreach (string format in codecs)
            {
                if (!ContainerSupportsAudioFormat(outMode, format))
                    return false;
            }

            return true;
        }

        public static bool ContainerSupportsAudioFormat(Interpolate.OutMode outMode, string format)
        {
            bool supported = false;
            string alias = GetAudioExt(format);

            string[] formatsMp4 = new string[] { "m4a", "mp3", "ac3", "dts" };
            string[] formatsMkv = new string[] { "m4a", "mp3", "ac3", "dts", "ogg", "mp2", "wav", "wma" };
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

        public static string GetAudioExt(string codec)
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

        public static async Task<string> GetAudioFallbackArgs(string videoPath, Interpolate.OutMode outMode, float itsScale)
        {
            bool opusMp4 = Config.GetBool(Config.Key.allowOpusInMp4);
            int opusBr = Config.GetInt(Config.Key.opusBitrate, 128);
            int aacBr = Config.GetInt(Config.Key.aacBitrate, 160);
            int ac = (await GetVideoInfo.GetFfprobeInfoAsync(videoPath, GetVideoInfo.FfprobeMode.ShowStreams, "channels", 0)).GetInt();
            string af = GetAudioFilters(itsScale);

            if (outMode == Interpolate.OutMode.VidMkv || outMode == Interpolate.OutMode.VidWebm || (outMode == Interpolate.OutMode.VidMp4 && opusMp4))
                return $"-c:a libopus -b:a {(ac > 4 ? $"{opusBr * 2}" : $"{opusBr}")}k -ac {(ac > 0 ? $"{ac}" : "2")} {af}"; // Double bitrate if 5ch or more, ignore ac if <= 0
            else
                return $"-c:a aac -b:a {(ac > 4 ? $"{aacBr * 2}" : $"{aacBr}")}k -aac_coder twoloop -ac {(ac > 0 ? $"{ac}" : "2")} {af}";
        }

        private static string GetAudioFilters(float itsScale)
        {
            if (itsScale == 0 || itsScale == 1)
                return "";

            if (itsScale > 4)
                return $"-af atempo=0.5,atempo=0.5,atempo={((1f / itsScale) * 4).ToStringDot()}";
            else if (itsScale > 2)
                return $"-af atempo=0.5,atempo={((1f / itsScale) * 2).ToStringDot()}";
            else
                return $"-af atempo={(1f / itsScale).ToStringDot()}";
        }

        public static string GetSubCodecForContainer(string containerExt)
        {
            containerExt = containerExt.Remove(".");

            if (containerExt == "mp4" || containerExt == "mov") return "mov_text";
            if (containerExt == "webm") return "webvtt";

            return "copy";    // Default: Copy subs
        }

        public static bool ContainerSupportsSubs(string containerExt, bool showWarningIfNotSupported = true)
        {
            containerExt = containerExt.Remove(".");
            bool supported = (containerExt == "mp4" || containerExt == "mkv" || containerExt == "webm" || containerExt == "mov");
            Logger.Log($"Subtitles {(supported ? "are supported" : "not supported")} by {containerExt.ToUpper()}", true);

            if (showWarningIfNotSupported && Config.GetBool(Config.Key.keepSubs) && !supported)
                Logger.Log($"Warning: {containerExt.ToUpper()} exports do not include subtitles.");

            return supported;
        }

        public static void CreateConcatFile(string inputFilesDir, string outputPath, string[] validExtensions = null)
        {
            string concatFileContent = "";
            string[] files = IoUtils.GetFilesSorted(inputFilesDir);

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
