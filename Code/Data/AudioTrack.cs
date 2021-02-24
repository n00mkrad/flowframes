using System.Globalization;

namespace Flowframes.Data
{
    class AudioTrack
    {
        public int streamIndex;
        public string title;
        public string codec;

        public AudioTrack(int streamNum, string titleStr, string codecStr)
        {
            streamIndex = streamNum;
            title = titleStr.Trim().Replace("_", ".").Replace(" ", ".");
            codec = codecStr.Trim().Replace("_", ".");
        }
    }
}
