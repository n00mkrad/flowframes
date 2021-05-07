using System;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.Data
{
    class VidExtraData
    {
        // Color
        public string colorSpace = "";
        public string colorRange = "";
        public string colorTransfer = "";
        public string colorPrimaries = "";

        // Aspect Ratio
        public string displayRatio = "";

        public VidExtraData(string ffprobeOutput)
        {
            string[] lines = ffprobeOutput.SplitIntoLines();

            foreach (string line in lines)
            {
                if (line.Contains("color_range"))
                {
                    colorRange = line.Split('=').LastOrDefault();
                    continue;
                }

                if (line.Contains("color_space"))
                {
                    colorSpace = line.Split('=').LastOrDefault();
                    continue;
                }

                if (line.Contains("color_transfer"))
                {
                    colorTransfer = line.Split('=').LastOrDefault();
                    continue;
                }

                if (line.Contains("color_primaries"))
                {
                    colorPrimaries = line.Split('=').LastOrDefault();
                    continue;
                }

                if (line.Contains("display_aspect_ratio"))
                {
                    displayRatio = line.Split('=').LastOrDefault();
                    continue;
                }
            }

            if (colorSpace.Trim() == "unknown")
                colorSpace = "";

            if (colorRange.Trim() == "unknown")
                colorRange = "";

            if (colorTransfer.Trim() == "unknown")
                colorTransfer = "";

            if (colorPrimaries.Trim() == "unknown")
                colorPrimaries = "";
        }

        public bool HasAnyValues ()
        {
            if (!string.IsNullOrWhiteSpace(colorSpace))
                return true;

            if (!string.IsNullOrWhiteSpace(colorRange))
                return true;

            if (!string.IsNullOrWhiteSpace(colorTransfer))
                return true;

            if (!string.IsNullOrWhiteSpace(colorPrimaries))
                return true;

            return false;
        }

        public bool HasAllValues()
        {
            if (string.IsNullOrWhiteSpace(colorSpace))
                return false;

            if (string.IsNullOrWhiteSpace(colorRange))
                return false;

            if (string.IsNullOrWhiteSpace(colorTransfer))
                return false;

            if (string.IsNullOrWhiteSpace(colorPrimaries))
                return false;

            return true;
        }
    }
}
