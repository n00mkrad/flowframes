using System.Collections.Generic;

namespace Flowframes.Data
{
    public class Strings
    {
        public static Dictionary<string, string> OutputFormat = new Dictionary<string, string>
        {
            { Enums.Output.Format.Mp4.ToString(), "MP4" },
            { Enums.Output.Format.Mkv.ToString(), "MKV" },
            { Enums.Output.Format.Webm.ToString(), "WEBM" },
            { Enums.Output.Format.Mov.ToString(), "MOV" },
            { Enums.Output.Format.Avi.ToString(), "AVI" },
            { Enums.Output.Format.Gif.ToString(), "GIF" },
            { Enums.Output.Format.Images.ToString(), "Frames" },
            { Enums.Output.Format.Realtime.ToString(), "Realtime" },
        };

        public static Dictionary<string, string> Encoder = new Dictionary<string, string>
        {
            { Enums.Encoding.Encoder.X264.ToString(), "h264" },
            { Enums.Encoding.Encoder.X265.ToString(), "h265" },
            { Enums.Encoding.Encoder.SvtAv1.ToString(), "AV1" },
            { Enums.Encoding.Encoder.VpxVp9.ToString(), "VP9" },
            { Enums.Encoding.Encoder.ProResKs.ToString(), "ProRes" },
            { Enums.Encoding.Encoder.Nvenc264.ToString(), "h264 NVENC" },
            { Enums.Encoding.Encoder.Nvenc265.ToString(), "h265 NVENC" },
            { Enums.Encoding.Encoder.NvencAv1.ToString(), "AV1 NVENC" },
            { Enums.Encoding.Encoder.Amf264.ToString(), "h264 AMF" },
            { Enums.Encoding.Encoder.Amf265.ToString(), "h265 AMF" },
            { Enums.Encoding.Encoder.Qsv264.ToString(), "h264 QuickSync" },
            { Enums.Encoding.Encoder.Qsv265.ToString(), "h265 QuickSync" },
            { Enums.Encoding.Encoder.Gif.ToString(), "GIF" },
            { Enums.Encoding.Encoder.Png.ToString(), "PNG" },
            { Enums.Encoding.Encoder.Jpeg.ToString(), "JPEG" },
            { Enums.Encoding.Encoder.Webp.ToString(), "WEBP" },
            { Enums.Encoding.Encoder.Tiff.ToString(), "TIFF" },
            { Enums.Encoding.Encoder.Exr.ToString(), "EXR" },
            { Enums.Encoding.Encoder.Ffv1.ToString(), "FFV1" },
            { Enums.Encoding.Encoder.Huffyuv.ToString(), "HuffYUV" },
            { Enums.Encoding.Encoder.Magicyuv.ToString(), "MagicYUV" },
            { Enums.Encoding.Encoder.Rawvideo.ToString(), "Raw Video" },
        };

        public static Dictionary<string, string> PixelFormat = new Dictionary<string, string>
        {
            { Enums.Encoding.PixelFormat.Yuv420P.ToString(), "YUV 4:2:0 8-bit" },
            { Enums.Encoding.PixelFormat.Yuva420P.ToString(), "YUVA 4:2:0 8-bit" },
            { Enums.Encoding.PixelFormat.Yuv420P10Le.ToString(), "YUV 4:2:0 10-bit" },
            { Enums.Encoding.PixelFormat.Yuv422P.ToString(), "YUV 4:2:2 8-bit" },
            { Enums.Encoding.PixelFormat.Yuv422P10Le.ToString(), "YUV 4:2:2 10-bit" },
            { Enums.Encoding.PixelFormat.Yuv444P.ToString(), "YUV 4:4:4 8-bit" },
            { Enums.Encoding.PixelFormat.Yuv444P10Le.ToString(), "YUV 4:4:4 10-bit" },
            { Enums.Encoding.PixelFormat.Yuva444P10Le.ToString(), "YUVA 4:4:4 10-bit" },
            { Enums.Encoding.PixelFormat.Yuv444P12Le.ToString(), "YUV 4:4:4 12-bit" },
            { Enums.Encoding.PixelFormat.Yuv444P16Le.ToString(), "YUV 4:4:4 16-bit" },
            { Enums.Encoding.PixelFormat.P010Le.ToString(), "YUV 4:2:0 10-bit" }, // HW enc specific
            { Enums.Encoding.PixelFormat.P016Le.ToString(), "YUV 4:2:0 16-bit" }, // HW enc specific
            { Enums.Encoding.PixelFormat.Rgb24.ToString(), "RGB 8-bit" },
            { Enums.Encoding.PixelFormat.Rgba.ToString(), "RGBA 8-bit" },
            { Enums.Encoding.PixelFormat.Rgb48Le.ToString(), "RGB 12-bit LE" },
            { Enums.Encoding.PixelFormat.Rgb48Be.ToString(), "RGB 12-bit BE" },
            { Enums.Encoding.PixelFormat.Rgba64Le.ToString(), "RGBA 16-bit LE" },
            { Enums.Encoding.PixelFormat.Rgba64Be.ToString(), "RGBA 16-bit BE" },
            { Enums.Encoding.PixelFormat.Pal8.ToString(), "256-color Palette" },
            { Enums.Encoding.PixelFormat.Gbrpf32Le.ToString(),  "RGB 32-bit Float" },
            { Enums.Encoding.PixelFormat.Gbrapf32Le.ToString(), "RGBA 32-bit Float" },
        };

        public static Dictionary<string, string> VideoQuality = new Dictionary<string, string>
        {
            { Enums.Encoding.Quality.Common.Lossless.ToString(), "Lossless" },
            { Enums.Encoding.Quality.Common.VeryHigh.ToString(), "Very High" },
            { Enums.Encoding.Quality.Common.High.ToString(), "High" },
            { Enums.Encoding.Quality.Common.Medium.ToString(), "Medium" },
            { Enums.Encoding.Quality.Common.Low.ToString(), "Low" },
            { Enums.Encoding.Quality.Common.VeryLow.ToString(), "Very Low" },
            { Enums.Encoding.Quality.ProResProfile.Proxy.ToString(), "Proxy" },
            { Enums.Encoding.Quality.ProResProfile.Lt.ToString(), "LT" },
            { Enums.Encoding.Quality.ProResProfile.Standard.ToString(), "Standard" },
            { Enums.Encoding.Quality.ProResProfile.Hq.ToString(), "HQ" },
            { Enums.Encoding.Quality.ProResProfile.Quad4.ToString(), "4444" },
            { Enums.Encoding.Quality.ProResProfile.Quad4Xq.ToString(), "4444 XQ" },
            { Enums.Encoding.Quality.GifColors.Max256.ToString(), "Max (256)" },
            { Enums.Encoding.Quality.GifColors.High128.ToString(), "High (128)" },
            { Enums.Encoding.Quality.GifColors.Medium64.ToString(), "Medium (64)" },
            { Enums.Encoding.Quality.GifColors.Low32.ToString(), "Low (32)" },
            { Enums.Encoding.Quality.GifColors.VeryLow16.ToString(), "Very Low (16)" },
            { Enums.Encoding.Quality.JpegWebm.ImgMax.ToString(), "Maximum" },
            { Enums.Encoding.Quality.JpegWebm.ImgHigh.ToString(), "High" },
            { Enums.Encoding.Quality.JpegWebm.ImgMed.ToString(), "Medium" },
            { Enums.Encoding.Quality.JpegWebm.ImgLow.ToString(), "Low" },
            { Enums.Encoding.Quality.JpegWebm.ImgLowest.ToString(), "Lowest" },
        };

        public static Dictionary<string, string> VfrMode = new Dictionary<string, string>
        {
            { Enums.VfrMode.Auto.ToString(), "Automatic" },
            { Enums.VfrMode.All.ToString(), "Treat all videos as VFR" },
            { Enums.VfrMode.None.ToString(), "Treat no videos as VFR" },
        };
    }
}
