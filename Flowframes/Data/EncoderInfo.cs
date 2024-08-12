namespace Flowframes.Data
{
    public class EncoderInfo
    {
        public virtual string FfmpegName { get; set; } = "";

        public EncoderInfo() { }

        public EncoderInfo(string name)
        {
            FfmpegName = name;
        }
    }
}
