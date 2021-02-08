using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Flowframes.Data
{
    class SubtitleTrack
    {
        public int streamIndex;
        public string lang;
        public string langFriendly;
        public string encoding;

        public SubtitleTrack (int streamNum, string langStr, string encodingStr)
        {
            streamIndex = streamNum;
            lang = langStr;
            langFriendly = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(langStr.ToLower());
            encoding = encodingStr.Trim();
        }
    }
}
