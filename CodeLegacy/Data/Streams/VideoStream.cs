using System.Collections.Generic;
using System.Drawing;

namespace Flowframes.Data.Streams
{
    public class VideoStream : Stream
    {
        public static readonly List<string> AlphaPixFmts = new List<string> { "argb", "rgba", "abgr", "bgra", "yuva420p", "yuva422p", "yuva444p", "yuva420p9be", "yuva420p9le", "yuva422p9be", "yuva422p9le", "yuva444p9be", "yuva444p9le", "yuva420p10be", "yuva420p10le", "yuva422p10be", "yuva422p10le", "yuva444p10be", "yuva444p10le", "yuva420p16be", "yuva420p16le", "yuva422p16be", "yuva422p16le", "yuva444p16be", "yuva444p16le", "rgba64be", "rgba64le", "bgra64be", "bgra64le", "gbrap", "gbrap16be", "gbrap16le", "vuya", "rgbaf16be", "rgbaf16le", "gbrap12be", "gbrap12le", "gbrap10be", "gbrap10le", "gbrapf32be", "gbrapf32le", "yuva422p12be", "yuva422p12le", "yuva444p12be", "yuva444p12le", "ayuv64le", "ayuv64be", "gbrap14be", "gbrap14le", "ayuv", "uyva", "rgba128be", "rgba128le", "gbrap32be", "gbrap32le", "rgbaf32be", "rgbaf32le" };

        public int FrameCount { get; } = 0;
        public string PixelFormat { get; }
        public int Kbits { get; }
        public Size Resolution { get; }
        public Size Sar { get; }
        public Size Dar { get; }
        public FpsInfo FpsInfo { get; }

        public Fraction Rate => FpsInfo.Fps;
        public bool CanHaveAlpha => AlphaPixFmts.Contains(PixelFormat.Lower());

        public VideoStream(string language, string title, string codec, string codecLong, string pixFmt, int kbits, Size resolution, Size sar, Size dar, FpsInfo fpsInf, int frameCount)
        {
            base.Type = StreamType.Video;
            Codec = codec;
            CodecLong = codecLong;
            PixelFormat = pixFmt;
            Kbits = kbits;
            Resolution = resolution;
            Sar = sar;
            Dar = dar;
            FpsInfo = fpsInf;
            FrameCount = frameCount;
            Language = language;
            Title = title;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - Language: {Language} - Color Format: {PixelFormat} - Size: {Resolution.Width}x{Resolution.Height} - FPS: {Rate}";
        }
    }
}
