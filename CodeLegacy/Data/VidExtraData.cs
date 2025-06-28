using Flowframes.IO;
using System;
using System.Linq;

namespace Flowframes.Data
{
    class VidExtraData
    {
        // Color
        public string ColSpace = "";
        public string ColRange = "";
        public string ColTransfer = "";
        public string ColPrimaries = "";

        // Aspect Ratio
        public string Dar = "";

        // Rotation
        public int Rotation = 0;

        private readonly string[] _validColorSpaces = new string[] { "bt709", "bt470m", "bt470bg", "smpte170m", "smpte240m", "linear", "log100", "log316", "iec61966-2-4", "bt1361e", "iec61966-2-1", "bt2020-10", "bt2020-12", "smpte2084", "smpte428", "arib-std-b67" };

        public bool HasAllColorValues => ColSpace.IsNotEmpty() && ColRange.IsNotEmpty() && ColTransfer.IsNotEmpty() && ColPrimaries.IsNotEmpty();
        public bool HasAnyColorValues => ColSpace.IsNotEmpty() || ColRange.IsNotEmpty() || ColTransfer.IsNotEmpty() || ColPrimaries.IsNotEmpty();
        public string ColorsStr => $"Color Primaries {(ColPrimaries.IsEmpty() ? "unset" : ColPrimaries)}, Space {(ColSpace.IsEmpty() ? "unset" : ColSpace)}, Transfer {(ColTransfer.IsEmpty() ? "unset" : ColTransfer)}, Range {(ColRange.IsEmpty() ? "unset" : ColRange)}";

        public VidExtraData () { }

        public VidExtraData(string ffprobeOutput)
        {
            string[] lines = ffprobeOutput.SplitIntoLines();
            bool keepColorSpace = Config.GetBool(Config.Key.keepColorSpace, true);

            string GetValue (string key)
            {
                return lines.FirstOrDefault(l => l.StartsWith(key + "="))?.Split('=').LastOrDefault();
            }

            Rotation = GetValue("display_rotation")?.GetInt() ?? 0;
            Dar = GetValue("display_aspect_ratio") ?? "";

            if (keepColorSpace)
            {
                ColPrimaries = GetValue("color_primaries")?.Lower().Replace("unknown", "") ?? "";
                ColRange = GetValue("color_range")?.Lower().Replace("unknown", "") ?? "";
                ColSpace = GetValue("color_space")?.Lower().Replace("unknown", "") ?? "";
                ColTransfer = GetValue("color_transfer")?.Lower().Replace("unknown", "") ?? "";
                ColTransfer = ColTransfer.Replace("bt470bg", "gamma28").Replace("bt470m", "gamma28"); // https://forum.videohelp.com/threads/394596-Color-Matrix
            }

            Logger.Log($"{ColorsStr}; Display Aspect Ratio {Dar.Wrap()}, Rotation {Rotation}", true, false, "ffmpeg");

            if (!_validColorSpaces.Contains(ColSpace.Trim()))
            {
                Logger.Log($"Warning: Ignoring invalid color space '{ColSpace.Trim()}'.", true, false, "ffmpeg");
                ColSpace = "";
            }

            if (!_validColorSpaces.Contains(ColTransfer.Trim()))
            {
                Logger.Log($"Warning: Color Transfer '{ColTransfer.Trim()}' not valid.", true, false, "ffmpeg");
                ColTransfer = "";
            }

            if (!_validColorSpaces.Contains(ColPrimaries.Trim()))
            {
                Logger.Log($"Warning: Color Primaries '{ColPrimaries.Trim()}' not valid.", true, false, "ffmpeg");
                ColPrimaries = "";
            }  
        }
    }
}
