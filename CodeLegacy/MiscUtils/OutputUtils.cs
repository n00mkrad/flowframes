using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using static Flowframes.Data.Enums.Encoding;
using Encoder = Flowframes.Data.Enums.Encoding.Encoder;
using PixFmt = Flowframes.Data.Enums.Encoding.PixelFormat;

namespace Flowframes.MiscUtils
{
    internal class OutputUtils
    {
        public static readonly List<PixFmt> AlphaFormats = new List<PixFmt> { PixFmt.Rgba, PixFmt.Yuva420P, PixFmt.Yuva444P10Le };

        public static EncoderInfoVideo GetEncoderInfoVideo(Encoder encoder)
        {
            if (encoder == Encoder.X264)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H264,
                    Name = "libx264",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                };
            }

            if (encoder == Encoder.X265)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H265,
                    Name = "libx265",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P, PixFmt.Yuv420P10Le, PixFmt.Yuv444P10Le },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                };
            }

            if (encoder == Encoder.SvtAv1)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.AV1,
                    Name = "libsvtav1",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv420P10Le },
                    PixelFormatDefault = PixFmt.Yuv420P10Le,
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    MaxFramerate = 240,
                    Lossless = null,
                };
            }

            if (encoder == Encoder.VpxVp9)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.VP9,
                    Name = "libvpx-vp9",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P, PixFmt.Yuv420P10Le, PixFmt.Yuv444P, PixFmt.Yuv444P10Le, PixFmt.Yuva420P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                };
            }

            if (encoder == Encoder.Nvenc264)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H264,
                    Name = "h264_nvenc",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    HwAccelerated = true,
                };
            }

            if (encoder == Encoder.Nvenc265)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H265,
                    Name = "hevc_nvenc",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P, PixFmt.Yuv420P10Le },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    HwAccelerated = true,
                };
            }

            if (encoder == Encoder.NvencAv1)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.AV1,
                    Name = "av1_nvenc",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P, PixFmt.Yuv420P10Le },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    PixelFormatDefault = PixFmt.Yuv420P10Le,
                    HwAccelerated = true,
                    Lossless = null,
                };
            }

            if (encoder == Encoder.Amf264)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H264,
                    Name = "h264_amf",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    HwAccelerated = true,
                    Lossless = null,
                };
            }

            if (encoder == Encoder.Amf265)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H265,
                    Name = "hevc_amf",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    HwAccelerated = true,
                    Lossless = null,
                };
            }

            if (encoder == Encoder.Qsv264)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H264,
                    Name = "h264_qsv",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    HwAccelerated = true,
                    Lossless = null,
                };
            }

            if (encoder == Encoder.Qsv265)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.H265,
                    Name = "hevc_qsv",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
                    HwAccelerated = true,
                    Lossless = null,
                };
            }

            if (encoder == Encoder.ProResKs)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.ProRes,
                    Name = "prores_ks",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv422P10Le, PixFmt.Yuv444P10Le, PixFmt.Yuva444P10Le },
                    PixelFormatDefault = PixFmt.Yuv422P10Le,
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.ProResProfile>(),
                    QualityDefault = (int)Quality.ProResProfile.Standard,
                };
            }

            if (encoder == Encoder.Gif)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Gif,
                    PixelFormats = new List<PixFmt>() { PixFmt.Pal8 },
                    PixelFormatDefault = PixFmt.Pal8,
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.GifColors>(),
                    QualityDefault = (int)Quality.GifColors.High128,
                    OverideExtension = "gif",
                    MaxFramerate = 50,
                };
            }

            if (encoder == Encoder.Ffv1)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Ffv1,
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P, PixFmt.Yuv422P, PixFmt.Yuv422P, PixFmt.Yuv420P10Le, PixFmt.Yuv444P10Le, PixFmt.Yuv444P12Le, PixFmt.Yuv444P16Le, PixFmt.Yuva420P, PixFmt.Yuva444P10Le, PixFmt.Rgb48Le, PixFmt.Rgba64Le },
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Huffyuv)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Huffyuv,
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv422P, PixFmt.Rgb24, PixFmt.Rgba },
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Magicyuv)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Magicyuv,
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv422P, PixFmt.Yuv444P },
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Rawvideo)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Rawvideo,
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Png)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Png,
                    PixelFormats = new List<PixFmt>() { PixFmt.Rgb24, PixFmt.Rgba, PixFmt.Rgb48Be, PixFmt.Rgba64Be },
                    PixelFormatDefault = PixFmt.Rgb24,
                    Lossless = true,
                    IsImageSequence = true,
                    OverideExtension = "png",
                };
            }

            if (encoder == Encoder.Jpeg)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Jpeg,
                    Name = "mjpeg",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv422P, PixFmt.Yuv444P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.JpegWebm>(),
                    QualityDefault = (int)Quality.JpegWebm.ImgHigh,
                    IsImageSequence = true,
                    OverideExtension = "jpg",
                };
            }

            if (encoder == Encoder.Webp)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Webp,
                    Name = "libwebp",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuva420P, PixFmt.Rgba }, // Actually only supports BGRA not RGBA, but ffmpeg will auto-pick that
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.JpegWebm>(),
                    QualityDefault = (int)Quality.JpegWebm.ImgHigh,
                    IsImageSequence = true,
                    OverideExtension = "webp",
                };
            }

            if (encoder == Encoder.Tiff)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Tiff,
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv422P, PixFmt.Yuv444P, PixFmt.Rgb24, PixFmt.Rgba, PixFmt.Rgb48Le, PixFmt.Rgba64Le },
                    PixelFormatDefault = PixFmt.Rgb24,
                    Lossless = true,
                    IsImageSequence = true,
                    OverideExtension = "tiff",
                };
            }

            if (encoder == Encoder.Exr)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Exr,
                    PixelFormats = new List<PixFmt>() { PixFmt.Gbrpf32Le, PixFmt.Gbrapf32Le },
                    PixelFormatDefault = PixFmt.Gbrpf32Le,
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.ExrPrecision>(),
                    QualityDefault = (int)Quality.ExrPrecision.Half,
                    Lossless = false,
                    IsImageSequence = true,
                    OverideExtension = "exr",
                };
            }

            return new EncoderInfoVideo();
        }

        public static List<Codec> GetSupportedCodecs(Enums.Output.Format format)
        {
            switch (format)
            {
                case Enums.Output.Format.Mp4: return new List<Codec> { Codec.H264, Codec.H265, Codec.AV1 };
                case Enums.Output.Format.Mkv: return new List<Codec> { Codec.H264, Codec.H265, Codec.AV1, Codec.VP9 };
                case Enums.Output.Format.Webm: return new List<Codec> { Codec.VP9, Codec.AV1 };
                case Enums.Output.Format.Mov: return new List<Codec> { Codec.ProRes, Codec.H264 };
                case Enums.Output.Format.Avi: return new List<Codec> { Codec.Ffv1, Codec.Huffyuv, Codec.Magicyuv, Codec.Rawvideo };
                case Enums.Output.Format.Gif: return new List<Codec> { Codec.Gif };
                case Enums.Output.Format.Images: return new List<Codec> { Codec.Png, Codec.Jpeg, Codec.Webp, Codec.Tiff, Codec.Exr };
                case Enums.Output.Format.Realtime: return new List<Codec> { };
                default: return new List<Codec> { };
            }
        }

        public static List<Encoder> GetAvailableEncoders(Enums.Output.Format format)
        {
            var allEncoders = Enum.GetValues(typeof(Encoder)).Cast<Encoder>();
            var supportedCodecs = GetSupportedCodecs(format);
            var availableEncoders = supportedCodecs.SelectMany(codec => allEncoders.Where(enc => enc.GetInfo().Codec == codec)).ToList();
            RemoveIncompatibleEncoders(ref availableEncoders, new[] { 
                Encoder.Nvenc264, Encoder.Nvenc265, Encoder.NvencAv1,
                Encoder.Amf264, Encoder.Amf265,
                Encoder.Qsv264, Encoder.Qsv265,
            });
            return availableEncoders;
        }

        private static void RemoveIncompatibleEncoders (ref List<Encoder> encoders, IEnumerable<Encoder> encodersToCheck)
        {
            var availHwEncs = Config.Get(Config.Key.SupportedHwEncoders).Split(',');

            foreach(Encoder enc in encodersToCheck)
            {
                if (encoders.Contains(enc) && !availHwEncs.Contains(enc.GetInfo().Name))
                    encoders.Remove(enc);
            }
        }

        public static int GetCrf (Quality.Common qualityLevel, Encoder encoder)
        {
            var encoderMultipliers = new Dictionary<Encoder, float>
            {
                { Encoder.X265, 1.0f },
                { Encoder.VpxVp9, 1.3f },
                { Encoder.SvtAv1, 1.3f },
                { Encoder.Nvenc264, 1.1f },
                { Encoder.Nvenc265, 1.15f },
                { Encoder.NvencAv1, 1.3f },
                { Encoder.Qsv265, 0.8f }
            };

            float multiplier = encoderMultipliers.TryGetValue(encoder, out float value) ? value : 1.0f;
            return (int)Math.Round(Crfs[qualityLevel] * multiplier);
        }

        public static int GetGifColors (Quality.GifColors qualityLevel)
        {
            switch (qualityLevel)
            {
                case Quality.GifColors.Max256: return 256;
                case Quality.GifColors.High128: return 128;
                case Quality.GifColors.Medium64: return 64;
                case Quality.GifColors.Low32: return 32;
                case Quality.GifColors.VeryLow16: return 16;
                default: return 128;
            }
        }

        public static Dictionary<Quality.Common, int> Crfs = new Dictionary<Quality.Common, int>
        {
            { Quality.Common.Lossless, 0 },
            { Quality.Common.VeryHigh, 16 },
            { Quality.Common.High, 20 },
            { Quality.Common.Medium, 26 },
            { Quality.Common.Low, 32 },
            { Quality.Common.VeryLow, 40 },
        };

        public static Dictionary<Quality.ProResProfile, string> ProresProfiles = new Dictionary<Quality.ProResProfile, string>
        {
            { Quality.ProResProfile.Proxy, "proxy" },
            { Quality.ProResProfile.Lt, "proxy" },
            { Quality.ProResProfile.Standard, "standard" },
            { Quality.ProResProfile.Hq, "hq" },
            { Quality.ProResProfile.Quad4, "4444" },
            { Quality.ProResProfile.Quad4Xq, "4444xq" },
        };

        public static Dictionary<Quality.JpegWebm, int> JpegQuality = new Dictionary<Quality.JpegWebm, int>
        {
            { Quality.JpegWebm.ImgMax, 1 },
            { Quality.JpegWebm.ImgHigh, 3 },
            { Quality.JpegWebm.ImgMed, 5 },
            { Quality.JpegWebm.ImgLow, 11 },
            { Quality.JpegWebm.ImgLowest, 31 },
        };

        public static Dictionary<Quality.JpegWebm, int> WebpQuality = new Dictionary<Quality.JpegWebm, int>
        {
            { Quality.JpegWebm.ImgMax, 100 },
            { Quality.JpegWebm.ImgHigh, 90 },
            { Quality.JpegWebm.ImgMed, 75 },
            { Quality.JpegWebm.ImgLow, 40 },
            { Quality.JpegWebm.ImgLowest, 0 },
        };

        public static int GetImgSeqQ (OutputSettings settings)
        {
            var qualityLevel = ParseUtils.GetEnum<Quality.JpegWebm>(settings.Quality, true, Strings.VideoQuality);

            if (settings.Encoder == Encoder.Jpeg)
                return JpegQuality[qualityLevel];

            if (settings.Encoder == Encoder.Webp)
                return WebpQuality[qualityLevel];

            return -1;
        }
    }
}
