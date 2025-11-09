using Flowframes.IO;
using System;
using System.Drawing;
using System.Linq;

namespace Flowframes.Data
{
    public class VidExtraData
    {
        // Color
        public string ColSpace = "";
        public string ColRange = "";
        public string ColTransfer = "";
        public string ColPrimaries = "";

        // Sample/Display Aspect Ratios
        public string Sar = "";
        public string Dar = "";

        // Rotation
        public int Rotation = 0;

        private readonly string[] _validColorSpaces = new string[] { "bt709", "bt470m", "bt470bg", "smpte170m", "smpte240m", "linear", "log100", "log316", "iec61966-2-4", "bt1361e", "iec61966-2-1", "bt2020-10", "bt2020-12", "smpte2084", "smpte428", "arib-std-b67" };

        public bool HasAllColorValues => ColSpace.IsNotEmpty() && ColRange.IsNotEmpty() && ColTransfer.IsNotEmpty() && ColPrimaries.IsNotEmpty();
        public bool HasAnyColorValues => ColSpace.IsNotEmpty() || ColRange.IsNotEmpty() || ColTransfer.IsNotEmpty() || ColPrimaries.IsNotEmpty();
        public string ColorsStr => $"Color Primaries {(ColPrimaries.IsEmpty() ? "unset" : ColPrimaries)}, Space {(ColSpace.IsEmpty() ? "unset" : ColSpace)}, Transfer {(ColTransfer.IsEmpty() ? "unset" : ColTransfer)}, Range {(ColRange.IsEmpty() ? "unset" : ColRange)}";

        public VidExtraData() { }

        public VidExtraData(string ffprobeOutput, bool allowColorData = true)
        {
            string[] lines = ffprobeOutput.SplitIntoLines();
            bool keepColorSpace = allowColorData && Config.GetBool(Config.Key.keepColorSpace, true);

            string GetValue(string key, string key2 = "") => lines.FirstOrDefault(l => l.StartsWith(key + "=") || key2.IsNotEmpty() && l.StartsWith(key2 + "="))?.Split('=').Last() ?? "";

            int w = GetValue("width", "coded_width").GetInt();
            int h = GetValue("height", "coded_height").GetInt();
            Rotation = GetValue("display_rotation", "rotation").GetInt();
            Sar = GetValue("sample_aspect_ratio");
            Dar = GetValue("display_aspect_ratio");

            if (keepColorSpace)
            {
                ColPrimaries = GetValue("color_primaries").Lower().Replace("unknown", "");
                ColRange = GetValue("color_range").Lower().Replace("unknown", "");
                ColSpace = GetValue("color_space").Lower().Replace("unknown", "");
                ColTransfer = GetValue("color_transfer").Lower().Replace("unknown", "");
                ColTransfer = ColTransfer.Replace("bt470bg", "gamma28").Replace("bt470m", "gamma28"); // https://forum.videohelp.com/threads/394596-Color-Matrix

                if (ColSpace.IsNotEmpty() && !_validColorSpaces.Contains(ColSpace.Trim()))
                {
                    Logger.Log($"Warning: Color Space '{ColSpace.Trim()}'.", true, false, "ffmpeg");
                    ColSpace = "";
                }

                if (ColTransfer.IsNotEmpty() && !_validColorSpaces.Contains(ColTransfer.Trim()))
                {
                    Logger.Log($"Warning: Color Transfer '{ColTransfer.Trim()}' not valid.", true, false, "ffmpeg");
                    ColTransfer = "";
                }

                if (ColPrimaries.IsNotEmpty() && !_validColorSpaces.Contains(ColPrimaries.Trim()))
                {
                    Logger.Log($"Warning: Color Primaries '{ColPrimaries.Trim()}' not valid.", true, false, "ffmpeg");
                    ColPrimaries = "";
                }
            }

            Logger.Log($"{ColorsStr}; SAR {Sar.Wrap()}, DAR {Dar.Wrap()}, Rot. {Rotation}", true, false, "ffmpeg");
        }
    }
}
