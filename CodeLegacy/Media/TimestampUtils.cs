using Flowframes.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flowframes.Media
{
    public class TimestampUtils
    {
        public static void CalcTimestamps(MediaFile media, InterpSettings settings)
        {
            float avgDuration = media.InputTimestampDurations.Average();
            media.InputTimestamps.Add(media.InputTimestamps.Last() + avgDuration); // Add extra frame using avg. duration, needed for duration matching or looping
            media.OutputTimestamps = StretchTimestamps(media.InputTimestamps, settings.interpFactor);

            if (settings.FpsResampling)
            {
                List<float[]> timestampsWithInputIdx = ConvertToConstantFrameRate(media.OutputTimestamps, settings.outFpsResampled.Float);
                media.OutputTimestamps = timestampsWithInputIdx.Select(x => x[0]).ToList();
                media.OutputFrameIndexes = timestampsWithInputIdx.Select(x => (int)x[1]).ToList();
            }
        }

        public static string WriteTsFile(List<float> timestamps, string path)
        {
            var lines = new List<string>() { "# timecode format v2" };

            foreach (var ts in timestamps)
            {
                lines.Add((ts * 1000f).ToString("0.000000"));
            }

            File.WriteAllLines(path, lines);
            return path;
        }

        public static List<float> StretchTimestamps(List<float> timestamps, double factor)
        {
            int originalCount = timestamps.Count;
            int newCount = (int)Math.Round(originalCount * factor);
            List<float> resampledTimestamps = new List<float>();

            for (int i = 0; i < newCount; i++)
            {
                double x = i / factor;

                if (x >= originalCount - 1)
                {
                    resampledTimestamps.Add(timestamps[originalCount - 1]);
                }
                else
                {
                    int index = (int)Math.Floor(x);
                    double fraction = x - index;
                    float startTime = timestamps[index];
                    float endTime = timestamps[index + 1];

                    float interpolatedTime = (float)(startTime + (endTime - startTime) * fraction);
                    resampledTimestamps.Add(interpolatedTime);
                }
            }

            return resampledTimestamps;
        }

        public static List<float[]> ConvertToConstantFrameRate(List<float> inputTimestamps, float targetFrameRate, Enums.Round roundMethod = Enums.Round.Near)
        {
            List<float[]> outputTimestamps = new List<float[]>(); // Resulting list of timestamps
            float targetFrameInterval = 1.0f / targetFrameRate; // Interval for the target frame rate
            float currentTargetTime = 0.0f; // Start time for the target frame rate
            int index = 0; // Index for iterating through the input timestamps

            while (currentTargetTime <= inputTimestamps.Last()) // Use ^1 to get the last element
            {
                switch (roundMethod)
                {
                    case Enums.Round.Near: // Find the closest timestamp to the current target time
                        while (index < inputTimestamps.Count - 1 && Math.Abs(inputTimestamps[index + 1] - currentTargetTime) < Math.Abs(inputTimestamps[index] - currentTargetTime)) index++;
                        break;
                    case Enums.Round.Down: // Find the closest timestamp that is <= the current target time
                        while (index < inputTimestamps.Count - 1 && inputTimestamps[index + 1] <= currentTargetTime) index++;
                        break;
                    case Enums.Round.Up: // Find the closest timestamp that is >= the current target time
                        while (index < inputTimestamps.Count - 1 && inputTimestamps[index] < currentTargetTime) index++;
                        break;
                }

                if (Program.Debug)
                {
                    Console.WriteLine($"-> Frame {(outputTimestamps.Count + 1).ToString().PadLeft(4)} | Target Time: {(currentTargetTime * 1000f):F5} | Picked Input Index: {(index).ToString().PadLeft(4)} | Input TS: {(inputTimestamps[index] * 1000f):F3}");
                }

                // Add the closest timestamp to the output list, along with the index of the input timestamp
                outputTimestamps.Add(new float[] { inputTimestamps[index], index });

                // Move to the next frame time in the target frame rate
                currentTargetTime += targetFrameInterval;
            }

            Logger.Log($"CFR Downsampling: Picked {outputTimestamps.Count} out of {inputTimestamps.Count} timestamps for {targetFrameRate} FPS (Round: {roundMethod})", true);
            return outputTimestamps;
        }
    }
}
