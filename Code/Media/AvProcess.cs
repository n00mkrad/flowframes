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
                ffmpeg.OutputDataReceived += (sender, outLine) => { AvOutputHandler.LogOutput(outLine.Data, ref processOutput, "ffmpeg", logMode, progressBar); timeSinceLastOutput.sw.Restart(); };
                ffmpeg.ErrorDataReceived += (sender, outLine) => { AvOutputHandler.LogOutput(outLine.Data, ref processOutput, "ffmpeg", logMode, progressBar); timeSinceLastOutput.sw.Restart(); };
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

        public static string GetFfmpegDefaultArgs (string loglevel = "warning")
        {
            return $"-hide_banner -stats -loglevel {loglevel} -y";
        }

        public static async Task<string> RunFfprobe(string args, LogMode logMode = LogMode.Hidden, string loglevel = "quiet")
        {
            bool show = Config.GetInt(Config.Key.cmdDebugMode) > 0;
            string processOutput = "";
            Process ffprobe = OsUtils.NewProcess(!show);
            NmkdStopwatch timeSinceLastOutput = new NmkdStopwatch();
            lastAvProcess = ffprobe;

            if (string.IsNullOrWhiteSpace(loglevel))
                loglevel = defLogLevel;

            ffprobe.StartInfo.Arguments = $"{GetCmdArg()} cd /D {GetAvDir().Wrap()} & ffprobe -v {loglevel} {args}";

            if (logMode != LogMode.Hidden) Logger.Log("Running FFprobe...", false);
            Logger.Log($"ffprobe -v {loglevel} {args}", true, false, "ffmpeg");

            if (!show)
            {
                ffprobe.OutputDataReceived += (sender, outLine) => { AvOutputHandler.LogOutput(outLine.Data, ref processOutput, "ffmpeg", logMode, false); timeSinceLastOutput.sw.Restart(); };
                ffprobe.ErrorDataReceived += (sender, outLine) => { AvOutputHandler.LogOutput(outLine.Data, ref processOutput, "ffmpeg", logMode, false); timeSinceLastOutput.sw.Restart(); };
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

        public static string GetFfprobeOutput (string args)
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
        
        static string GetAvDir ()
        {
            return Path.Combine(Paths.GetPkgPath(), Paths.audioVideoDir);
        }

        static string GetCmdArg ()
        {
            return "/C";
        }

        public static async Task SetBusyWhileRunning ()
        {
            if (Program.busy) return;

            await Task.Delay(100);
            while(!lastAvProcess.HasExited)
                await Task.Delay(10);
        }
    }
}
