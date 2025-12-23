using System.Collections.Generic;

namespace Flowframes.Data
{
    public struct ResumeState
    {
        public bool autoEncode;
        public int interpolatedInputFrames;

        public ResumeState (bool autoEncArg, int lastInterpInFrameArg)
        {
            autoEncode = autoEncArg;
            interpolatedInputFrames = lastInterpInFrameArg;
        }

        public ResumeState(string serializedData)
        {
            autoEncode = false;
            interpolatedInputFrames = 0;

            Dictionary<string, string> entries = new Dictionary<string, string>();

            foreach (string line in serializedData.SplitIntoLines())
            {
                if (line.Length < 3) continue;
                string[] keyValuePair = line.Split('|');
                entries.Add(keyValuePair[0], keyValuePair[1]);
            }

            foreach (KeyValuePair<string, string> entry in entries)
            {
                switch (entry.Key)
                {
                    case "AUTOENC": autoEncode = bool.Parse(entry.Value); break;
                    case "INTERPOLATEDINPUTFRAMES": interpolatedInputFrames = entry.Value.GetInt(); break;
                }
            }
        }

        public override string ToString ()
        {
            string s = $"AUTOENC|{autoEncode}\n";

            if (!autoEncode)
            {
                s += $"INTERPOLATEDINPUTFRAMES|{interpolatedInputFrames}";
            }

            return s;
        }
    }
}
