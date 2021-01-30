using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Data
{
    public struct ResumeState
    {
        bool autoEncode;
        int lastInterpolatedInputFrame;

        public ResumeState (bool autoEncArg, int lastInterpInFrameArg)
        {
            autoEncode = autoEncArg;
            lastInterpolatedInputFrame = lastInterpInFrameArg;
        }

        public override string ToString ()
        {
            string s = $"AUTOENC|{autoEncode}";

            if (!autoEncode)
            {
                s += $"\nLASTINPUTFRAME|{lastInterpolatedInputFrame}";
            }

            return s;
        }
    }
}
