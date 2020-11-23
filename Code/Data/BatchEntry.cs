using Flowframes.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes
{
    public struct BatchEntry
    {
        public string inPath;
        public string outPath;
        public AI ai;
        public float inFps;
        public int interpFactor;
        public Interpolate.OutMode outMode;

        public BatchEntry(string inPathArg, string outPathArg, AI aiArg, float inFpsArg, int interpFactorArg, Interpolate.OutMode outModeArg)
        {
            inPath = inPathArg;
            outPath = outPathArg;
            ai = aiArg;
            inFps = inFpsArg;
            interpFactor = interpFactorArg;
            outMode = outModeArg;
        }
    }
}
