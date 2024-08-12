using System;
using System.Diagnostics;

namespace Flowframes.MiscUtils
{
    class NmkdStopwatch
    {
        public Stopwatch Sw = new Stopwatch();
        public TimeSpan Elapsed { get { return Sw.Elapsed; } }
        public long ElapsedMs { get { return Sw.ElapsedMilliseconds; } }

        public NmkdStopwatch(bool startOnCreation = true)
        {
            if (startOnCreation)
                Sw.Restart();
        }

        public override string ToString()
        {
            return FormatUtils.TimeSw(Sw);
        }
    }
}
