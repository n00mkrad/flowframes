using Flowframes.Forms.Main;
using Flowframes.MiscUtils;
using System;
using System.Text.RegularExpressions;
using static Flowframes.AvProcess;

namespace Flowframes.Media
{
    class AvOutputHandler
    {
        public static readonly string prefix = "[ffmpeg]";

        public static void LogOutput(string line, ref string appendStr, string logFilename, LogMode logMode, bool showProgressBar)
        {
            if (Interpolate.canceled || string.IsNullOrWhiteSpace(line) || line.Trim().Length < 1)
                return;

            bool hidden = logMode == LogMode.Hidden;

            if (HideMessage(line)) // Don't print certain warnings 
                hidden = true;

            bool replaceLastLine = logMode == LogMode.OnlyLastLine;

            if (line.Contains("time=") && (line.StartsWith("frame=") || line.StartsWith("size=")))
                line = FormatUtils.BeautifyFfmpegStats(line);

            appendStr += Environment.NewLine + line;
            Logger.Log($"{prefix} {line}", hidden, replaceLastLine, logFilename, toConsole: Cli.Verbose || logMode != LogMode.Hidden);

            if (!hidden && showProgressBar && line.Contains("Time:"))
            {
                Regex timeRegex = new Regex("(?<=Time:).*(?= )");
                UpdateFfmpegProgress(timeRegex.Match(line).Value);
            }


            if (line.Contains("Unable to"))
            {
                Interpolate.Cancel($"Error: {line}");
                return;
            }

            if (line.Contains("Could not open file"))
            {
                Interpolate.Cancel($"Error: {line}");
                return;
            }

            if (line.Contains("No NVENC capable devices found") || line.MatchesWildcard("*nvcuda.dll*"))
            {
                Interpolate.Cancel($"Error: {line}\n\nMake sure you have an NVENC-capable Nvidia GPU.");
                return;
            }

            if (line.Contains("not currently supported in container") || line.Contains("Unsupported codec id"))
            {
                Interpolate.Cancel($"Error: {line}\n\nIt looks like you are trying to copy a stream into a container that doesn't support this codec.");
                return;
            }

            if (line.Contains("Subtitle encoding currently only possible from text to text or bitmap to bitmap"))
            {
                Interpolate.Cancel($"Error: {line}\n\nYou cannot encode image-based subtitles into text-based subtitles. Please use the Copy Subtitles option instead, with a compatible container.");
                return;
            }

            if (line.Contains("Only VP8 or VP9 or AV1 video and Vorbis or Opus audio and WebVTT subtitles are supported for WebM"))
            {
                Interpolate.Cancel($"Error: {line}\n\nIt looks like you are trying to copy an unsupported stream into WEBM!");
                return;
            }

            if (line.MatchesWildcard("*codec*not supported*"))
            {
                Interpolate.Cancel($"Error: {line}\n\nTry using a different codec.");
                return;
            }

            if (line.Contains("GIF muxer supports only a single video GIF stream"))
            {
                Interpolate.Cancel($"Error: {line}\n\nYou tried to mux a non-GIF stream into a GIF file.");
                return;
            }

            if (line.Contains("Width and height of input videos must be same"))
            {
                Interpolate.Cancel($"Error: {line}");
                return;
            }
        }

        public static void UpdateFfmpegProgress(string ffmpegTime)
        {
            try
            {
                Form1 form = Program.mainForm;
                long currInDuration = (form.currInDurationCut < form.currInDuration) ? form.currInDurationCut : form.currInDuration;

                if (currInDuration < 1)
                {
                    Program.mainForm.SetProgress(0);
                    return;
                }

                long total = currInDuration / 100;
                long current = FormatUtils.TimestampToMs(ffmpegTime);
                int progress = Convert.ToInt32(current / total);
                Program.mainForm.SetProgress(progress);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to get ffmpeg progress: {e.Message}", true);
            }
        }

        static bool HideMessage(string msg)
        {
            string[] hiddenMsgs = new string[] { "can produce invalid output", "pixel format", "provided invalid", "Non-monotonous", "not enough frames to estimate rate", "invalid dropping", "message repeated" };

            foreach (string str in hiddenMsgs)
                if (msg.MatchesWildcard($"*{str}*"))
                    return true;

            return false;
        }
    }
}
