using System.Drawing;

namespace Flowframes.Data.Streams
{
    public class VideoStream : Stream
    {
        public int FrameCount { get; } = 0;
        public string PixelFormat { get; }
        public int Kbits { get; }
        public Size Resolution { get; }
        public Size Sar { get; }
        public Size Dar { get; }
        public FpsInfo FpsInfo { get; }
        public Fraction Rate { get => FpsInfo.Fps; }

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
