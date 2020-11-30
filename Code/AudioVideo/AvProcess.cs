using Flowframes.IO;
using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes
{
    class AvProcess
    {
        public static Process lastProcess;

        public static string lastOutputFfmpeg;
        public static string lastOutputGifski;

        public enum LogMode { Visible, OnlyLastLine, Hidden }
        static LogMode currentLogMode;

        public static async Task RunFfmpeg(string args, LogMode logMode)
        {
            await RunFfmpeg(args, "", logMode);
        }

        public static async Task RunFfmpeg(string args, string workingDir, LogMode logMode)
        {
            lastOutputFfmpeg = "";
            currentLogMode = logMode;
            Process ffmpeg = new Process();
            lastProcess = ffmpeg;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.FileName = "cmd.exe";
            if(!string.IsNullOrWhiteSpace(workingDir))
                ffmpeg.StartInfo.Arguments = $"/C cd /D {workingDir.Wrap()} & {Path.Combine(GetAvDir(), "ffmpeg.exe").Wrap()} -hide_banner -loglevel warning -y -stats {args}";
            else
                ffmpeg.StartInfo.Arguments = $"/C cd /D {GetAvDir().Wrap()} & ffmpeg.exe -hide_banner -loglevel warning -y -stats {args}";
            if (logMode != LogMode.Hidden) Logger.Log("Running ffmpeg...", false);
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments, true, false, "ffmpeg.txt");
            ffmpeg.OutputDataReceived += new DataReceivedEventHandler(FfmpegOutputHandler);
            ffmpeg.ErrorDataReceived += new DataReceivedEventHandler(FfmpegOutputHandler);
            ffmpeg.Start();
            ffmpeg.BeginOutputReadLine();
            ffmpeg.BeginErrorReadLine();
            while (!ffmpeg.HasExited)
                await Task.Delay(100);
            Logger.Log("Done running ffmpeg.", true);
        }

        static void FfmpegOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine == null || outLine.Data == null)
                return;
            string line = outLine.Data;
            lastOutputFfmpeg = lastOutputFfmpeg + line + "\n";
            bool hidden = currentLogMode == LogMode.Hidden;
            bool replaceLastLine = currentLogMode == LogMode.OnlyLastLine;
            string trimmedLine = line.Remove("q=-0.0").Remove("size=N/A").Remove("bitrate=N/A").TrimWhitespaces();
            Logger.Log(trimmedLine, hidden, replaceLastLine, "ffmpeg.txt");

            if(line.Contains("Could not open file"))
            {
                Interpolate.Cancel("Failed to write frames - Make sure the input folder is not restricted!");
            }
        }

        public static string GetFfmpegOutput (string args)
        {
            Process ffmpeg = OSUtils.NewProcess(true);
            ffmpeg.StartInfo.Arguments = $"/C cd /D {GetAvDir().Wrap()} & ffmpeg.exe -hide_banner -y -stats {args}";
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments, true);
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            string output = ffmpeg.StandardOutput.ReadToEnd();
            string err = ffmpeg.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
            return output;
        }

        public static string GetFfprobeOutput (string args)
        {
            Process ffprobe = OSUtils.NewProcess(true);
            ffprobe.StartInfo.Arguments = $"/C cd /D {GetAvDir().Wrap()} & ffprobe.exe {args}";
            Logger.Log("cmd.exe " + ffprobe.StartInfo.Arguments, true, false, "ffmpeg.txt");
            ffprobe.Start();
            ffprobe.WaitForExit();
            string output = ffprobe.StandardOutput.ReadToEnd();
            string err = ffprobe.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
            return output;
        }

        public static async Task RunGifski(string args, LogMode logMode)
        {
            lastOutputGifski = "";
            currentLogMode = logMode;
            Process gifski = OSUtils.NewProcess(true);
            lastProcess = gifski;
            gifski.StartInfo.Arguments = $"/C cd /D {GetAvDir().Wrap()} & gifski.exe {args}";
            Logger.Log("Running gifski...");
            Logger.Log("cmd.exe " + gifski.StartInfo.Arguments, true, false, "ffmpeg.txt");
            gifski.OutputDataReceived += new DataReceivedEventHandler(OutputHandlerGifski);
            gifski.ErrorDataReceived += new DataReceivedEventHandler(OutputHandlerGifski);
            gifski.Start();
            gifski.BeginOutputReadLine();
            gifski.BeginErrorReadLine();
            while (!gifski.HasExited)
                await Task.Delay(100);
            Logger.Log("Done running gifski.", true);
        }

        static void OutputHandlerGifski(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine == null || outLine.Data == null)
                return;
            lastOutputGifski = lastOutputGifski + outLine.Data + "\n";
            bool hidden = currentLogMode == LogMode.Hidden;
            bool replaceLastLine = currentLogMode == LogMode.OnlyLastLine;
            Logger.Log(outLine.Data, hidden, replaceLastLine);
        }

        static string GetAvDir ()
        {
            return Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.audioVideo.fileName));
        }
    }
}
