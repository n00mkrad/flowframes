using System;
using System.Diagnostics;
using System.Linq;

namespace Flowframes.MiscUtils
{
    class NmkdStopwatch
    {
        public Stopwatch sw = new Stopwatch();
        public long ElapsedMs { get { return sw.ElapsedMilliseconds; } }

        public NmkdStopwatch(bool startOnCreation = true)
        {
            if (startOnCreation)
                sw.Restart();
        }

        public override string ToString()
        {
            return FormatUtils.TimeSw(sw);
        }
    }
}
