using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.UI
{
    class FormatUtils
    {
        public static string Bytes(long sizeBytes)
        {
            int sizeKb = (int)Math.Round(sizeBytes / 1024f);
            int sizeMb = (int)Math.Round(sizeKb / 1024f);
            if (sizeBytes <= 8192)
            {
                return sizeBytes + " B";
            }
            if (sizeKb <= 8192)
            {
                return sizeKb + " KB";
            }
            return sizeMb + " MB"; ;
        }

        public static string Time(long milliseconds)
        {
            double secs = (milliseconds / 1000f);
            if (milliseconds <= 1000)
            {
                return milliseconds + "ms";
            }
            return secs.ToString("0.00") + "s";
        }

        public static string Time (TimeSpan span)
        {
            if(span.TotalHours >= 1f)
                return span.ToString(@"hh\:mm\:ss");

            if (span.TotalMinutes >= 1f)
                return span.ToString(@"mm\:ss");

            if (span.TotalSeconds >= 3f)
                return span.ToString(@"ss") + "s";

            return span.Milliseconds.ToString() + "ms";
        }

        public static string TimeSw(Stopwatch sw)
        {
            long elapsedMs = sw.ElapsedMilliseconds;
            return Time(elapsedMs);
        }

        public static string Ratio(long numFrom, long numTo)
        {
            float ratio = ((float)numTo / (float)numFrom) * 100f;
            return ratio.ToString("0.00") + "%";
        }

        public static string RatioInt(long numFrom, long numTo)
        {
            double ratio = Math.Round(((float)numTo / (float)numFrom) * 100f);
            return ratio + "%";
        }
    }
}
