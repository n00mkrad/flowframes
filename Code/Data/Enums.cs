namespace Flowframes.Data
{
    public class Enums
    {
        public class Output
        {
            public enum Format { Mp4, Mkv, Webm, Mov, Avi, Gif, Images, Realtime };
            public enum ImageFormat { Png, Jpeg, Webp };
            public enum Dithering { None, Bayer, FloydSteinberg };
        }

        public class Encoding
        {
            public enum Codec { H264, H265, AV1, VP9, ProRes, Gif, Png, Jpeg, Webp, Ffv1, Huffyuv, Magicyuv, Rawvideo }
            public enum Encoder { X264, X265, SvtAv1, VpxVp9, Nvenc264, Nvenc265, NvencAv1, ProResKs, Gif, Png, Jpeg, Webp, Ffv1, Huffyuv, Magicyuv, Rawvideo }
            public enum PixelFormat { Yuv420P, Yuva420P, Yuv420P10Le, Yuv422P, Yuv422P10Le, Yuv444P, Yuv444P10Le, Yuva444P10Le, Rgb24, Rgba, Rgb8 };
            public enum ProResProfiles { Proxy, Lt, Standard, Hq, Quad4, Quad4Xq }
        }
    }
}