namespace Flowframes.Data
{
    public class FpsInfo
    {
        public Fraction Fps { get; set; }
        public Fraction AverageFps { get; set; }
        public float VfrRatio { get => AverageFps.GetFloat() / Fps.GetFloat(); }
        public float VfrRatioInverse { get => Fps.GetFloat() / AverageFps.GetFloat(); }

        public FpsInfo() { }

        public FpsInfo(Fraction fps)
        {
            Fps = fps;
            AverageFps = fps;
        }

        public FpsInfo(Fraction fps, Fraction avgFps)
        {
            Fps = fps;
            AverageFps = avgFps;
        }
    }
}
