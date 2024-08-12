using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Data
{
    public class OutputSettings
    {
        public Enums.Output.Format Format { get; set; }
        public Enums.Encoding.Encoder Encoder { get; set; }
        public Enums.Encoding.PixelFormat PixelFormat { get; set; }
        public string Quality { get; set; } = "";
        public string CustomQuality { get; set; } = "";
    }
}
