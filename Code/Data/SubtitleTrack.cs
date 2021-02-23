using System.Globalization;

namespace Flowframes.Data
{
    class SubtitleTrack
    {
        public int streamIndex;
        public string lang;
        public string langFriendly;
        public string encoding;

        public SubtitleTrack(int streamNum, string langStr, string encodingStr)
        {
            streamIndex = streamNum;
            lang = langStr.Trim().Replace(" ", ".");
            langFriendly = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(langStr.ToLower().Trim().Replace(" ", "."));
            encoding = encodingStr.Trim();
        }
    }
}
