namespace Flowframes.Data
{
    class AudioTrack
    {
        public int streamIndex;
        public string metadata;
        public string codec;

        public AudioTrack(int streamNum, string metaStr, string codecStr)
        {
            streamIndex = streamNum;
            metadata = metaStr.Trim().Replace("_", ".").Replace(" ", ".");
            codec = codecStr.Trim().Replace("_", ".");
        }
    }
}
