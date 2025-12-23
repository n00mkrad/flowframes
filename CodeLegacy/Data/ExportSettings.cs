using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Flowframes.Data.Enums.Encoding;
using static Flowframes.Data.Enums.Output;

namespace Flowframes.Data
{
    public class OutputSettings
    {
        public Format Format { get; set; }
        public Encoder Encoder { get; set; }
        public PixelFormat PixelFormat { get; set; }
        public string Quality { get; set; } = "";
        public string CustomQuality { get; set; } = "";

        public static List<Format> HdrRecommendedFormats { get; set; } = new List<Format>() { Format.Mkv, Format.Mp4, Format.Mov, Format.Images };
        public static List<Encoder> HdrRecommendedEncs { get; set; } = new List<Encoder>();

        static OutputSettings ()
        {
            HdrRecommendedEncs = GetHdrRecommEncs();
        }

        public static List<Encoder> GetHdrRecommEncs ()
        {
            var l = new List<Encoder>();
            var encs = Enum.GetValues(typeof(Encoder)).Cast<Encoder>();
            l.Add(encs.FirstOrDefault(e => $"{e}".EndsWith("265")));
            l.Add(encs.FirstOrDefault(e => $"{e}".EndsWith("Av1")));
            l.Add(encs.FirstOrDefault(e => $"{e}".EndsWith("Vp9")));
            l.AddRange(new[] { Encoder.ProResKs, Encoder.Exr });
            return l;
        }

        public string GetHdrNotSuitableReason ()
        {
            if(!Format.IsOneOf(true, HdrRecommendedFormats))
                return $"Output format may not work for HDR. Recommended: {string.Join(" - ", HdrRecommendedFormats.Select(x => Strings.OutputFormat[x.ToString()]))}";
        
            if(Encoder.IsOneOf(true, HdrRecommendedEncs))
                return $"Output encoder may not work for HDR. Recommended: {string.Join(" - ", HdrRecommendedEncs.Select(x => Strings.Encoder[x.ToString()]))}";

            string pixFmt = Strings.PixelFormat[PixelFormat.ToString()];
            var m = Regex.Match(pixFmt, @"(\d+)(?=-bit\b)");
            int bitDepth = m.Success ? m.Groups[1].Value.GetInt() : 0;

            if(bitDepth < 10)
                return "Output Pixel Format may not work for HDR. Recommended: 10-bit or higher.";

            return "";
        }
    }
}
