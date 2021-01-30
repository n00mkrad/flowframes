using Flowframes.IO;
using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flowframes.MiscUtils;

namespace Flowframes
{
    class AvProcess
    {
        public static Process lastProcess;
        public enum TaskType { ExtractFrames, Encode, GetInfo, Merge, Other };
        public static TaskType lastTask = TaskType.Other;

        public static string lastOutputFfmpeg;
        public static string lastOutputGifski;

        public enum LogMode { Visible, OnlyLastLine, Hidden }
        static LogMode currentLogMode;

        public static async Task RunFfmpeg(string args, LogMode logMode, TaskType taskType = TaskType.Other)
        {
            await RunFfmpeg(args, "", logMode, taskType);
        }

        public static async Task RunFfmpeg(string args, string workingDir, LogMode logMode, TaskType taskType = TaskType.Other)
        {
            lastOutputFfmpeg = "";
            currentLogMode = logMode;
            Process ffmpeg = OSUtils.NewProcess(true);
            lastProcess = ffmpeg;
            lastTask = taskType;
            if(!string.IsNullOrWhiteSpace(workingDir))
                ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {workingDir.Wrap()} & {Path.Combine(GetAvDir(), "ffmpeg.exe").Wrap()} -hide_banner -loglevel warning -y -stats {args}";
            else
                ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffmpeg.exe -hide_banner -loglevel warning -y -stats {args}";
            if (logMode != LogMode.Hidden) Logger.Log("Running ffmpeg...", false);
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments, true, false, "ffmpeg");
            ffmpeg.OutputDataReceived += new DataReceivedEventHandler(FfmpegOutputHandler);
            ffmpeg.ErrorDataReceived += new DataReceivedEventHandler(FfmpegOutputHandler);
            ffmpeg.Start();
            ffmpeg.BeginOutputReadLine();
            ffmpeg.BeginErrorReadLine();
            while (!ffmpeg.HasExited)
                await Task.Delay(1);
        }

        static void FfmpegOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine == null || outLine.Data == null)
                return;
            string line = outLine.Data;
            lastOutputFfmpeg = lastOutputFfmpeg + "\n" + line;
            bool hidden = currentLogMode == LogMode.Hidden;
            bool replaceLastLine = currentLogMode == LogMode.OnlyLastLine;
            string trimmedLine = line.Remove("q=-0.0").Remove("size=N/A").Remove("bitrate=N/A").TrimWhitespaces();
            Logger.Log(trimmedLine, hidden, replaceLastLine, "ffmpeg");

            if(line.Contains("Could not open file"))
                Interpolate.Cancel($"FFmpeg Error: {line}");

            if (line.Contains("No NVENC capable devices found"))
                Interpolate.Cancel($"FFmpeg Error: {line}\nMake sure you have an NVENC-capable Nvidia GPU.");

            if (line.Contains("time=") && !hidden)
            {
                Regex timeRegex = new Regex("(?<=time=).*(?= )");
                String timestamp = timeRegex.Match(line).Value;
                UpdateFfmpegProgress(timeRegex.Match(line).Value);
            }
        }

        static void FfmpegOutputHandlerSilent (object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine == null || outLine.Data == null || outLine.Data.Trim().Length < 2)
                return;
            string line = outLine.Data;

            if (!string.IsNullOrWhiteSpace(lastOutputFfmpeg))
                lastOutputFfmpeg += "\n";
            lastOutputFfmpeg = lastOutputFfmpeg + line;
            Logger.Log(line, true, false, "ffmpeg");
        }

        public static string GetFfmpegOutput (string args)
        {
            Process ffmpeg = OSUtils.NewProcess(true);
            lastProcess = ffmpeg;
            ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffmpeg.exe -hide_banner -y -stats {args}";
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments, true, false, "ffmpeg");
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            string output = ffmpeg.StandardOutput.ReadToEnd();
            string err = ffmpeg.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
            return output;
        }

        public static async Task<string> GetFfmpegOutputAsync(string args, bool setBusy = false)
        {
            if (Program.busy)
                setBusy = false;
            lastOutputFfmpeg = "";
            Process ffmpeg = OSUtils.NewProcess(true);
            lastProcess = ffmpeg;
            ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffmpeg.exe -hide_banner -y -stats {args}";
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments, true, false, "ffmpeg");
            if (setBusy)
                Program.mainForm.SetWorking(true);
            ffmpeg.Start();
            ffmpeg.OutputDataReceived += new DataReceivedEventHandler(FfmpegOutputHandlerSilent);
            ffmpeg.ErrorDataReceived += new DataReceivedEventHandler(FfmpegOutputHandlerSilent);
            ffmpeg.Start();
            ffmpeg.BeginOutputReadLine();
            ffmpeg.BeginErrorReadLine();
            while (!ffmpeg.HasExited)
                await Task.Delay(10);
            if (setBusy)
                Program.mainForm.SetWorking(false);
            await Task.Delay(100);
            return lastOutputFfmpeg;
        }

        public static string GetFfprobeOutput (string args)
        {
            Process ffprobe = OSUtils.NewProcess(true);
            ffprobe.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffprobe.exe {args}";
            Logger.Log("cmd.exe " + ffprobe.StartInfo.Arguments, true, false, "ffmpeg");
            ffprobe.Start();
            ffprobe.WaitForExit();
            string output = ffprobe.StandardOutput.ReadToEnd();
            string err = ffprobe.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
            return output;
        }

        public static async Task<string> GetFfprobeOutputAsync(string args)
        {
            Process ffprobe = OSUtils.NewProcess(true);
            ffprobe.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffprobe.exe {args}";
            Logger.Log("cmd.exe " + ffprobe.StartInfo.Arguments, true, false, "ffmpeg");
            ffprobe.Start();
            while (!ffprobe.HasExited)
                await Task.Delay(1);
            string output = ffprobe.StandardOutput.ReadToEnd();
            string err = ffprobe.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
            return output;
        }

        public static void UpdateFfmpegProgress(String ffmpegTime)
        {
            long total = Program.mainForm.currInDuration / 100;
            if (total == 0) return;
            long current = FormatUtils.MsFromTimestamp(ffmpegTime);
            int progress = Convert.ToInt32(current / total);
            Program.mainForm.SetProgress(progress);
        }


        public static async Task RunGifski(string args, LogMode logMode)
        {
            lastOutputGifski = "";
            currentLogMode = logMode;
            Process gifski = OSUtils.NewProcess(true);
            lastProcess = gifski;
            gifski.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & gifski.exe {args}";
            Logger.Log("Running gifski...");
            Logger.Log("cmd.exe " + gifski.StartInfo.Arguments, true, false, "ffmpeg");
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

        static string GetCmdArg ()
        {
            return "/C";
        }

        public static async Task SetBusyWhileRunning ()
        {
            if (Program.busy) return;

            await Task.Delay(100);
            while(!lastProcess.HasExited)
                await Task.Delay(10);
        }
    }
}
