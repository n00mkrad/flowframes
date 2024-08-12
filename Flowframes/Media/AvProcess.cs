using Flowframes.IO;
using Flowframes.Os;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flowframes.MiscUtils;
using Microsoft.VisualBasic;
using Flowframes.Media;
using System.Windows.Input;

namespace Flowframes
{
    class AvProcess
    {
        public static Process lastAvProcess;
        public static Stopwatch timeSinceLastOutput = new Stopwatch();

        public static string lastOutputFfmpeg;

        public enum LogMode { Visible, OnlyLastLine, Hidden }
        static LogMode currentLogMode;
        static bool showProgressBar;

        static readonly string defLogLevel = "warning";

        public static void Kill()
        {
            if (lastAvProcess == null) return;

            try
            {
                OsUtils.KillProcessTree(lastAvProcess.Id);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to kill lastAvProcess process tree: {e.Message}", true);
            }
        }

        public static async Task<string> RunFfmpeg(string args, LogMode logMode, bool reliableOutput = true, bool progressBar = false)
        {
            return await RunFfmpeg(args, "", logMode, defLogLevel, reliableOutput, progressBar);
        }

        public static async Task<string> RunFfmpeg(string args, LogMode logMode, string loglevel, bool reliableOutput = true, bool progressBar = false)
        {
            return await RunFfmpeg(args, "", logMode, loglevel, reliableOutput, progressBar);
        }

        public static async Task<string> RunFfmpeg(string args, string workingDir, LogMode logMode, bool reliableOutput = true, bool progressBar = false)
        {
            return await RunFfmpeg(args, workingDir, logMode, defLogLevel, reliableOutput, progressBar);
        }

        public static async Task<string> RunFfmpeg(string args, string workingDir, LogMode logMode, string loglevel, bool reliableOutput = true, bool progressBar = false)
        {
            bool show = Config.GetInt(Config.Key.cmdDebugMode) > 0;
            string processOutput = "";
            Process ffmpeg = OsUtils.NewProcess(!show);
            NmkdStopwatch timeSinceLastOutput = new NmkdStopwatch();
            lastAvProcess = ffmpeg;

            if (string.IsNullOrWhiteSpace(loglevel))
                loglevel = defLogLevel;

            string beforeArgs = $"-hide_banner -stats -loglevel {loglevel} -y";

            if (!string.IsNullOrWhiteSpace(workingDir))
                ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {workingDir.Wrap()} & {Path.Combine(GetAvDir(), "ffmpeg.exe").Wrap()} {beforeArgs} {args}";
            else
                ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffmpeg {beforeArgs} {args}";

            if (logMode != LogMode.Hidden) Logger.Log("Running FFmpeg...", false);
            Logger.Log($"ffmpeg {beforeArgs} {args}", true, false, "ffmpeg");

            if (!show)
            {
                ffmpeg.OutputDataReceived += (sender, outLine) => { AvOutputHandler.LogOutput(outLine.Data, ref processOutput, "ffmpeg", logMode, progressBar); timeSinceLastOutput.Sw.Restart(); };
                ffmpeg.ErrorDataReceived += (sender, outLine) => { AvOutputHandler.LogOutput(outLine.Data, ref processOutput, "ffmpeg", logMode, progressBar); timeSinceLastOutput.Sw.Restart(); };
            }

            ffmpeg.Start();
            ffmpeg.PriorityClass = ProcessPriorityClass.BelowNormal;

            if (!show)
            {
                ffmpeg.BeginOutputReadLine();
                ffmpeg.BeginErrorReadLine();
            }

            while (!ffmpeg.HasExited) await Task.Delay(10);
            while (reliableOutput && timeSinceLastOutput.ElapsedMs < 200) await Task.Delay(50);

            if (progressBar)
                Program.mainForm.SetProgress(0);

            return processOutput;
        }

        public static string RunFfmpegSync(string args, string workingDir = "", LogMode logMode = LogMode.Hidden, string loglevel = "warning")
        {
            Process ffmpeg = OsUtils.NewProcess(true);
            lastAvProcess = ffmpeg;

            if (string.IsNullOrWhiteSpace(loglevel))
                loglevel = defLogLevel;

            string beforeArgs = $"-hide_banner -stats -loglevel {loglevel} -y";

            if (!string.IsNullOrWhiteSpace(workingDir))
                ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {workingDir.Wrap()} & {Path.Combine(GetAvDir(), "ffmpeg.exe").Wrap()} {beforeArgs} {args}";
            else
                ffmpeg.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffmpeg {beforeArgs} {args}";

            if (logMode != LogMode.Hidden) Logger.Log("Running FFmpeg...", false);
            Logger.Log($"ffmpeg {beforeArgs} {args}", true, false, "ffmpeg");

            ffmpeg.StartInfo.Arguments += " 2>&1";
            ffmpeg.Start();
            ffmpeg.PriorityClass = ProcessPriorityClass.BelowNormal;
            string output = ffmpeg.StandardOutput.ReadToEnd();
            ffmpeg.WaitForExit();
            Logger.Log($"Synchronous ffmpeg output:\n{output}", true, false, "ffmpeg");
            return output;
        }

        public static string GetFfmpegDefaultArgs(string loglevel = "warning")
        {
            return $"-hide_banner -stats -loglevel {loglevel} -y";
        }

        public class FfprobeSettings
        {
            public string Args { get; set; } = "";
            public LogMode LoggingMode { get; set; } = LogMode.Hidden;
            public string LogLevel { get; set; } = "panic";
            public bool SetBusy { get; set; } = false;
        }

        public static async Task<string> RunFfprobe(FfprobeSettings settings, bool asyncOutput = false)
        {
            bool show = Config.GetInt(Config.Key.cmdDebugMode) > 0;

            string processOutput = "";
            Process ffprobe = OsUtils.NewProcess(!show);
            NmkdStopwatch timeSinceLastOutput = new NmkdStopwatch();

            bool concat = settings.Args.Split(" \"").Last().Remove("\"").Trim().EndsWith(".concat");
            string args = $"-v {settings.LogLevel} {(concat ? "-f concat -safe 0 " : "")}{settings.Args}";
            ffprobe.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffprobe {args}";

            if (settings.LoggingMode != LogMode.Hidden) Logger.Log("Running FFprobe...", false);
            Logger.Log($"ffprobe {args}", true, false, "ffmpeg");

            if (!asyncOutput)
                return await Task.Run(() => OsUtils.GetProcStdOut(ffprobe));

            if (!show)
            {
                string[] ignore = new string[0];
                ffprobe.OutputDataReceived += (sender, outLine) => { processOutput += outLine + Environment.NewLine; };
                ffprobe.ErrorDataReceived += (sender, outLine) => { processOutput += outLine + Environment.NewLine; };
            }

            ffprobe.Start();
            ffprobe.PriorityClass = ProcessPriorityClass.BelowNormal;

            if (!show)
            {
                ffprobe.BeginOutputReadLine();
                ffprobe.BeginErrorReadLine();
            }

            while (!ffprobe.HasExited) await Task.Delay(10);
            while (timeSinceLastOutput.ElapsedMs < 200) await Task.Delay(50);

            return processOutput;
        }

        public static string GetFfprobeOutput(string args)
        {
            Process ffprobe = OsUtils.NewProcess(true);
            ffprobe.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffprobe.exe {args}";
            Logger.Log($"ffprobe {args}", true, false, "ffmpeg");
            ffprobe.Start();
            ffprobe.WaitForExit();
            string output = ffprobe.StandardOutput.ReadToEnd();
            string err = ffprobe.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
            return output;
        }

        static string GetAvDir()
        {
            return Path.Combine(Paths.GetPkgPath(), Paths.audioVideoDir);
        }

        static string GetCmdArg()
        {
            return "/C";
        }

        public static async Task SetBusyWhileRunning()
        {
            if (Program.busy) return;

            await Task.Delay(100);
            while (!lastAvProcess.HasExited)
                await Task.Delay(10);
        }
    }
}
