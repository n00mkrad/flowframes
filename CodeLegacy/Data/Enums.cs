namespace Flowframes.Data
{
    public class Enums
    {
        public class Output
        {
            public enum Format { Mp4, Mkv, Webm, Mov, Avi, Gif, Images, Realtime };
            public enum ImageFormat { Png, Jpeg, Webp, Tiff, Exr };
            public enum Dithering { None, Bayer, FloydSteinberg };
        }

        public class Encoding
        {
            public enum Codec { H264, H265, AV1, VP9, ProRes, Gif, Png, Jpeg, Webp, Tiff, Exr, Ffv1, Huffyuv, Magicyuv, Rawvideo }
            public enum Encoder { X264, X265, SvtAv1, VpxVp9, Nvenc264, Nvenc265, NvencAv1, Amf264, Amf265, Qsv264, Qsv265, ProResKs, Gif, Png, Jpeg, Webp, Tiff, Exr, Ffv1, Huffyuv, Magicyuv, Rawvideo }
            public enum PixelFormat
            { 
                Yuv420P, Yuva420P, Yuv420P10Le, Yuv422P, Yuv422P10Le, Yuv444P, Yuv444P10Le, Yuva444P10Le, Yuv444P12Le, Yuv444P16Le, P010Le, P016Le, // YUV & similar
                Rgb24, Rgba, Rgb48Le, Rgb48Be, Rgba64Le, Rgba64Be, Pal8, // RGB & other
                Gbrpf32Le, Gbrapf32Le, // Float
            };

            public class Quality
            {
                public enum Common { Lossless, VeryHigh, High, Medium, Low, VeryLow, Custom }
                public enum JpegWebm { ImgMax, ImgHigh, ImgMed, ImgLow, ImgLowest }
                public enum ProResProfile { Proxy, Lt, Standard, Hq, Quad4, Quad4Xq }
                public enum GifColors { Max256, High128, Medium64, Low32, VeryLow16 }
                public enum ExrPrecision { Float, Half }
            }
        }
    }
}