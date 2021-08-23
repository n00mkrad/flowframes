using System.Globalization;

namespace Flowframes.Data
{
    class SubtitleTrack
    {
        public int streamIndex;
        public string lang;
        //public string langFriendly;
        public string encoding;

        public SubtitleTrack(int streamNum, string metaStr, string encodingStr)
        {
            streamIndex = streamNum;
            lang = metaStr.Trim().Replace("_", ".").Replace(" ", ".");
            //langFriendly = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(metaStr.ToLower().Trim().Replace("_", ".").Replace(" ", "."));
            encoding = encodingStr.Trim();
        }
    }
}
