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
        public static Stopwatch timeSinceLastOutput = new Stopwatch();
        public enum TaskType { ExtractFrames, Encode, GetInfo, Merge, Other };
        public static TaskType lastTask = TaskType.Other;

        public static string lastOutputFfmpeg;
        public static string lastOutputGifski;

        public enum LogMode { Visible, OnlyLastLine, Hidden }
        static LogMode currentLogMode;
        static bool showProgressBar;

        public static async Task RunFfmpeg(string args, LogMode logMode, TaskType taskType = TaskType.Other, bool progressBar = false)
        {
            await RunFfmpeg(args, "", logMode, taskType, progressBar);
        }

        public static async Task RunFfmpeg(string args, string workingDir, LogMode logMode, TaskType taskType = TaskType.Other, bool progressBar = false)
        {
            lastOutputFfmpeg = "";
            currentLogMode = logMode;
            showProgressBar = progressBar;
            Process ffmpeg = OSUtils.NewProcess(true);
            timeSinceLastOutput.Restart();
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

            if(progressBar)
                Program.mainForm.SetProgress(0);
        }

        static void FfmpegOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            timeSinceLastOutput.Restart();
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

            if (!hidden && showProgressBar && line.Contains("time="))
            {
                Regex timeRegex = new Regex("(?<=time=).*(?= )");
                UpdateFfmpegProgress(timeRegex.Match(line).Value);
            }
        }

        static void FfmpegOutputHandlerSilent (object sendingProcess, DataReceivedEventArgs outLine)
        {
            timeSinceLastOutput.Restart();
            if (outLine == null || outLine.Data == null || outLine.Data.Trim().Length < 2)
                return;
            string line = outLine.Data;

            if (!string.IsNullOrWhiteSpace(lastOutputFfmpeg))
                lastOutputFfmpeg += "\n";
            lastOutputFfmpeg = lastOutputFfmpeg + line;
            Logger.Log(line, true, false, "ffmpeg");

            if (showProgressBar && line.Contains("time="))
            {
                Logger.Log($"showProgressBar, contains: {line.Contains("time=")}", true, false, "ffmpeg");
                Regex timeRegex = new Regex("(?<=time=).*(?= )");
                UpdateFfmpegProgress(timeRegex.Match(line).Value);
            }
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

        public static async Task<string> GetFfmpegOutputAsync(string args, bool setBusy = false, bool progressBar = false)
        {
            timeSinceLastOutput.Restart();
            if (Program.busy) setBusy = false;
            lastOutputFfmpeg = "";
            showProgressBar = progressBar;
            Process ffmpeg = OSUtils.NewProcess(true);
            lastProcess = ffmpeg;
            ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffmpeg.exe -hide_banner -y -stats {args}";
            Logger.Log("cmd.exe " + ffmpeg.StartInfo.Arguments, true, false, "ffmpeg");
            if (setBusy) Program.mainForm.SetWorking(true);
            ffmpeg.OutputDataReceived += new DataReceivedEventHandler(FfmpegOutputHandlerSilent);
            ffmpeg.ErrorDataReceived += new DataReceivedEventHandler(FfmpegOutputHandlerSilent);
            ffmpeg.Start();
            ffmpeg.BeginOutputReadLine();
            ffmpeg.BeginErrorReadLine();
            while (!ffmpeg.HasExited) await Task.Delay(50);
            while(timeSinceLastOutput.ElapsedMilliseconds < 200) await Task.Delay(50);
            if (setBusy) Program.mainForm.SetWorking(false);
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

        public static void UpdateFfmpegProgress(string ffmpegTime)
        {
            if (Program.mainForm.currInDuration < 1)
            {
                Program.mainForm.SetProgress(0);
                return;
            }

            long total = Program.mainForm.currInDuration / 100;
            long current = FormatUtils.MsFromTimestamp(ffmpegTime);
            int progress = Convert.ToInt32(current / total);
            Program.mainForm.SetProgress(progress);
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
