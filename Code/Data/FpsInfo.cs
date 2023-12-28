namespace Flowframes.Data
{
    public class FpsInfo
    {
        public Fraction Fps { get; set; }
        public Fraction SpecifiedFps { get; set; }
        public float VfrRatio { get => Fps.GetFloat() / SpecifiedFps.GetFloat(); }
        public float VfrRatioInverse { get => SpecifiedFps.GetFloat() / Fps.GetFloat(); }

        public FpsInfo() { }

        public FpsInfo(Fraction fps)
        {
            Fps = fps;
            SpecifiedFps = fps;
        }

        public FpsInfo(Fraction fps, Fraction specifiedFps)
        {
            Fps = fps;
            SpecifiedFps = specifiedFps;
        }
    }
}
