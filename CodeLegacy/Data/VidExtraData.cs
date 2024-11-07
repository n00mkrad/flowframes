using Flowframes.IO;
using System;
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

        // Rotation
        public int Rotation = 0;

        private readonly string[] validColorSpaces = new string[] { "bt709", "bt470m", "bt470bg", "smpte170m", "smpte240m", "linear", "log100",
            "log316", "iec61966-2-4", "bt1361e", "iec61966-2-1", "bt2020-10", "bt2020-12", "smpte2084", "smpte428", "arib-std-b67" };

        public VidExtraData () { }

        public VidExtraData(string ffprobeOutput)
        {
            string[] lines = ffprobeOutput.SplitIntoLines();
            bool keepColorSpace = Config.GetBool(Config.Key.keepColorSpace, true);

            foreach (string line in lines)
            {
                if(line.StartsWith("rotation="))
                {
                    Rotation = line.Split('=').LastOrDefault().GetInt();
                    continue;
                }

                if (keepColorSpace)
                {
                    if (line.StartsWith("color_range"))
                    {
                        colorRange = line.Split('=').LastOrDefault().Lower();
                        continue;
                    }

                    if (line.StartsWith("color_space"))
                    {
                        colorSpace = line.Split('=').LastOrDefault().Lower();
                        continue;
                    }

                    if (line.StartsWith("color_transfer"))
                    {
                        colorTransfer = line.Split('=').LastOrDefault().Lower();
                        continue;
                    }

                    if (line.StartsWith("color_primaries"))
                    {
                        colorPrimaries = line.Split('=').LastOrDefault().Lower();
                        continue;
                    }
                }

                if (line.StartsWith("display_aspect_ratio") && Config.GetBool(Config.Key.keepAspectRatio, true))
                {
                    displayRatio = line.Split('=').LastOrDefault();
                    continue;
                }
            }

            if (!validColorSpaces.Contains(colorSpace.Trim()))
            {
                Logger.Log($"Warning: Ignoring invalid color space '{colorSpace.Trim()}'.", true, false, "ffmpeg");
                colorSpace = "";
            }

            if (colorRange.Trim() == "unknown")
                colorRange = "";

            if (!validColorSpaces.Contains(colorTransfer.Trim()))
            {
                Logger.Log($"Warning: Color Transfer '{colorTransfer.Trim()}' not valid.", true, false, "ffmpeg");
                colorTransfer = "";
            }
            else
            {
                colorTransfer = colorTransfer.Replace("bt470bg", "gamma28").Replace("bt470m", "gamma28"); // https://forum.videohelp.com/threads/394596-Color-Matrix
            }

            if (!validColorSpaces.Contains(colorPrimaries.Trim()))
            {
                Logger.Log($"Warning: Color Primaries '{colorPrimaries.Trim()}' not valid.", true, false, "ffmpeg");
                colorPrimaries = "";
            }  
        }

        public bool HasAllColorValues()
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
