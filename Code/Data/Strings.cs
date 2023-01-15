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
            { Enums.Output.Format.Images.ToString(), "Images" },
            { Enums.Output.Format.Realtime.ToString(), "Real-time" },
        };

        public static Dictionary<string, string> Encoder = new Dictionary<string, string>
        {
            { Enums.Encoding.Encoder.X264.ToString(), "h264" },
            { Enums.Encoding.Encoder.X265.ToString(), "h265" },
            { Enums.Encoding.Encoder.SvtAv1.ToString(), "AV1" },
            { Enums.Encoding.Encoder.VpxVp9.ToString(), "VP9" },
            { Enums.Encoding.Encoder.ProResKs.ToString(), "ProRes" },
            { Enums.Encoding.Encoder.Nvenc264.ToString(), "h264 (NVENC)" },
            { Enums.Encoding.Encoder.Nvenc265.ToString(), "h265 (NVENC)" },
            { Enums.Encoding.Encoder.NvencAv1.ToString(), "AV1 (NVENC)" },
            { Enums.Encoding.Encoder.Gif.ToString(), "Animated GIF" },
            { Enums.Encoding.Encoder.Png.ToString(), "PNG" },
            { Enums.Encoding.Encoder.Jpeg.ToString(), "JPEG" },
            { Enums.Encoding.Encoder.Webp.ToString(), "WEBP" },
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
            { Enums.Encoding.PixelFormat.Rgb24.ToString(), "RGB 8-bit" },
            { Enums.Encoding.PixelFormat.Rgb8.ToString(), "RGB 256-color" },
            { Enums.Encoding.PixelFormat.Rgba.ToString(), "RGBA 8-bit" },
        };
    }
}
