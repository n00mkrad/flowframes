using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Flowframes.Data.Enums.Encoding;

namespace Flowframes.Data
{
    public class EncoderInfoVideo : EncoderInfo
    {
        public Codec Codec { get; set; } = (Codec)(-1);
        public bool Lossless { get; set; } = false;
        public bool HwAccelerated { get; set; } = false;
        public int Modulo { get; set; } = 2;
        public int MaxFramerate { get; set; } = 1000;
        public List<PixelFormat> PixelFormats { get; set; } = new List<PixelFormat>();
        public PixelFormat PixelFormatDefault { get; set; } = (PixelFormat)(-1);
        public bool IsImageSequence { get; set; } = false;
        public string OverideExtension { get; set; } = "";
        public List<string> QualityLevels { get; set; } = new List<string> ();
        public int QualityDefault { get; set; } = 0;
    }
}
