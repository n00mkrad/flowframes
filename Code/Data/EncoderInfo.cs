using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Data
{
    public class EncoderInfo
    {
        public string Name { get; set; } = "unknown";

        public EncoderInfo() { }

        public EncoderInfo(string name)
        {
            Name = name;
        }
    }
}
