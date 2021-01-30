using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.MiscUtils
{
    class FormatUtils
    {
        public static string Bytes(long sizeBytes)
        {
            try
            {
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
                if (sizeBytes == 0)
                    return "0" + suf[0];
                long bytes = Math.Abs(sizeBytes);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return ($"{Math.Sign(sizeBytes) * num} {suf[place]}");
            }
            catch
            {
                return "N/A B";
            }
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

        public static string Time (TimeSpan span, bool allowMs = true)
        {
            if(span.TotalHours >= 1f)
                return span.ToString(@"hh\:mm\:ss");

            if (span.TotalMinutes >= 1f)
                return span.ToString(@"mm\:ss");

            if (span.TotalSeconds >= 1f || !allowMs)
                return span.ToString(@"ss".TrimStart('0').PadLeft(1, '0')) + "s";

            return span.ToString(@"fff").TrimStart('0').PadLeft(1, '0') + "ms";
        }

        public static string TimeSw(Stopwatch sw)
        {
            long elapsedMs = sw.ElapsedMilliseconds;
            return Time(elapsedMs);
        }

        public static long MsFromTimestamp(String timestamp)
        {
            string[] values = timestamp.Split(':');
            int hours = Int32.Parse(values[0]);
            int minutes = Int32.Parse(values[1]);
            int seconds = Int32.Parse(values[2].Split('.')[0]);
            int milliseconds = Int32.Parse(values[2].Split('.')[1].Substring(0, 2)) * 10;
            long ms = hours * 3600000 + minutes * 60000 + seconds * 1000 + milliseconds;
            return ms;
        }

        public static String MsToTimestamp(long milliseconds)
        {
            return (new DateTime(1970, 1, 1)).AddMilliseconds(milliseconds).ToLongTimeString();
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

        public static string ConcatStrings(string[] strings, char delimiter = ',', bool distinct = false)
        {
            string outStr = "";

            strings = strings.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if(distinct)
                strings = strings.Distinct().ToArray();

            for (int i = 0; i < strings.Length; i++)
            {
                outStr += strings[i];
                if (i + 1 != strings.Length)
                    outStr += delimiter;
            }

            return outStr;
        }
    }
}
