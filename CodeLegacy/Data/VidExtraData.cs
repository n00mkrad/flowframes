using Flowframes.IO;
using System;
using System.Collections.Generic;
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
        public bool IsHdr => ColPrimaries == "bt2020" && (ColTransfer == "smpte2084" || ColTransfer == "arib-std-b67");

        // Sample/Display Aspect Ratios
        public string Sar = "";
        public string Dar = "";

        // Rotation
        public int Rotation = 0;

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
                ColRange = GetValue("color_range").Lower().Replace("unknown", "");
                ColPrimaries = GetValue("color_primaries").Lower().Replace("unknown", "");
                ColSpace = GetValue("color_space").Lower().Replace("unknown", "");
                ColTransfer = GetValue("color_transfer").Lower().Replace("unknown", "");

                ColPrimaries = CanonicalizeColorTags(ColPrimaries);
                ColSpace = CanonicalizeColorTags(ColSpace);
                ColTransfer = CanonicalizeColorTags(ColTransfer).Replace("bt470bg", "gamma28").Replace("bt470m", "gamma28"); // https://forum.videohelp.com/threads/394596-Color-Matrix

                if (ColSpace.IsNotEmpty() && !ColorSpaces.Contains(ColSpace.Trim()))
                {
                    Logger.Log($"Warning: Color Space '{ColSpace.Trim()}'.", true, false, "ffmpeg");
                    ColSpace = "";
                }

                if (ColPrimaries.IsNotEmpty() && !ColorPrimaries.Contains(ColPrimaries.Trim()))
                {
                    Logger.Log($"Warning: Color Primaries '{ColPrimaries.Trim()}' not valid.", true, false, "ffmpeg");
                    ColPrimaries = "";
                }

                if (ColTransfer.IsNotEmpty() && !ColorTransfers.Contains(ColTransfer.Trim()))
                {
                    Logger.Log($"Warning: Color Transfer '{ColTransfer.Trim()}' not valid.", true, false, "ffmpeg");
                    ColTransfer = "";
                }
            }

            Logger.Log($"{ColorsStr}; SAR {Sar.Wrap()}, DAR {Dar.Wrap()}, Rot. {Rotation}", true, false, "ffmpeg");
        }

        // FFmpeg -colorspace (matrix coefficients)
        public static readonly string[] ColorSpaces = { "rgb", "bt709", "unknown", "fcc", "bt470bg", "smpte170m", "smpte240m", "ycgco", "bt2020nc", "bt2020c", "smpte2085", "chroma-derived-nc", "chroma-derived-c", "ictcp", "ipt-c2", "ycgco-re", "ycgco-ro", };
        // FFmpeg -color_primaries
        public static readonly string[] ColorPrimaries = { "bt709", "unknown", "bt470m", "bt470bg", "smpte170m", "smpte240m", "film", "bt2020", "smpte428", "smpte431", "smpte432", "jedec-p22", };
        // FFmpeg -color_trc (transfer characteristics)
        public static readonly string[] ColorTransfers = { "bt709", "unknown", "gamma22", "gamma28", "smpte170m", "smpte240m", "linear", "log100", "log316", "iec61966-2-4", "bt1361e", "iec61966-2-1", "bt2020-10", "bt2020-12", "smpte2084", "smpte428", "arib-std-b67", };

        // Maps documented synonyms to the canonical names above.
        public static string CanonicalizeColorTags(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return tag;

            var key = tag.Trim().ToLowerInvariant();
            return _alias.TryGetValue(key, out var canon) ? canon : key;
        }

        private static readonly Dictionary<string, string> _alias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common "unspecified"
            ["unspecified"] = "unknown",
            // -colorspace aliases
            ["ycocg"] = "ycgco",
            ["bt2020_ncl"] = "bt2020nc",
            ["bt2020_cl"] = "bt2020c",
            // -color_primaries aliases
            ["smpte428_1"] = "smpte428",
            ["ebu3213"] = "jedec-p22",
            // -color_trc aliases
            ["log"] = "log100",
            ["log_sqrt"] = "log316",
            ["iec61966_2_4"] = "iec61966-2-4",
            ["bt1361"] = "bt1361e",
            ["iec61966_2_1"] = "iec61966-2-1",
            ["bt2020_10bit"] = "bt2020-10",
            ["bt2020_12bit"] = "bt2020-12",
        };
    }
}
