using Flowframes.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Win32Interop.Enums;
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
                };
            }

            if (encoder == Encoder.VpxVp9)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.VP9,
                    Name = "libvpx-vp9",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P, PixFmt.Yuv420P10Le, PixFmt.Yuv444P, PixFmt.Yuv444P10Le },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.Common>(),
                    QualityDefault = (int)Quality.Common.VeryHigh,
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
                    Name = "gif",
                    PixelFormats = new List<PixFmt>() { PixFmt.Pal8 },
                    PixelFormatDefault = PixFmt.Pal8,
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.GifColors>(),
                    QualityDefault = (int)Quality.GifColors.High128,
                    OverideExtension = "gif",
                    MaxFramerate = 50,
                    Modulo = 1,
                };
            }

            if (encoder == Encoder.Ffv1)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Ffv1,
                    Name = "ffv1",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv444P, PixFmt.Yuv422P, PixFmt.Yuv422P, PixFmt.Yuv420P10Le, PixFmt.Yuv444P10Le },
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Huffyuv)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Huffyuv,
                    Name = "huffyuv",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv422P, PixFmt.Rgb24 },
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Magicyuv)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Magicyuv,
                    Name = "magicyuv",
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuv422P, PixFmt.Yuv444P },
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Rawvideo)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Rawvideo,
                    Name = "rawvideo",
                    Lossless = true,
                };
            }

            if (encoder == Encoder.Png)
            {
                return new EncoderInfoVideo
                {
                    Codec = Codec.Png,
                    Name = "png",
                    PixelFormats = new List<PixFmt>() { PixFmt.Rgb24, PixFmt.Rgba },
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
                    PixelFormats = new List<PixFmt>() { PixFmt.Yuv420P, PixFmt.Yuva420P },
                    QualityLevels = ParseUtils.GetEnumStrings<Quality.JpegWebm>(),
                    QualityDefault = (int)Quality.JpegWebm.ImgHigh,
                    IsImageSequence = true,
                    OverideExtension = "webp",
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
                case Enums.Output.Format.Mov: return new List<Codec> { Codec.ProRes };
                case Enums.Output.Format.Avi: return new List<Codec> { Codec.Ffv1, Codec.Huffyuv, Codec.Magicyuv, Codec.Rawvideo };
                case Enums.Output.Format.Gif: return new List<Codec> { Codec.Gif };
                case Enums.Output.Format.Images: return new List<Codec> { Codec.Png, Codec.Jpeg, Codec.Webp };
                case Enums.Output.Format.Realtime: return new List<Codec> { };
                default: return new List<Codec> { };
            }
        }

        public static List<Encoder> GetAvailableEncoders(Enums.Output.Format format)
        {
            var allEncoders = Enum.GetValues(typeof(Encoder)).Cast<Encoder>();
            var supportedCodecs = GetSupportedCodecs(format);
            var availableEncoders = supportedCodecs.SelectMany(codec => allEncoders.Where(enc => enc.GetInfo().Codec == codec));
            return availableEncoders.ToList();
        }

        public static int GetCrf (Quality.Common qualityLevel, Encoder encoder)
        {
            int baseCrf = Crfs[qualityLevel];
            float multiplier = 1f;

            if (encoder == Encoder.X265)
                multiplier = 1.0f;
            if (encoder == Encoder.VpxVp9)
                multiplier = 1.3f;
            if (encoder == Encoder.SvtAv1)
                multiplier = 1.3f;
            if (encoder == Encoder.Nvenc264)
                multiplier = 1.1f;
            if (encoder == Encoder.Nvenc265)
                multiplier = 1.15f;
            if (encoder == Encoder.NvencAv1)
                multiplier = 1.3f;

            return (baseCrf * multiplier).RoundToInt();
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
