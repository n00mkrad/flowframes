using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Flowframes.OS;
using Flowframes.UI;
using Flowframes.Main;

namespace Flowframes
{
    class AiProcess
    {
        public static bool hasShownError;

        public static Process currentAiProcess;
        public static Stopwatch processTime = new Stopwatch();

        static void Init (Process proc, string defaultExt = "png", bool needsFirstFrameFix = false)
        {
            Interpolate.firstFrameFix = needsFirstFrameFix;
            InterpolateUtils.lastExt = defaultExt;
            if (Config.GetBool("jpegInterps")) InterpolateUtils.lastExt = "jpg";
            processTime.Restart();
            currentAiProcess = proc;
            hasShownError = false;
        }

        public static async Task RunDainNcnn(string framesPath, string outPath, int targetFrames, int tilesize)
        {
            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -n {targetFrames} -t {tilesize} -g {Config.Get("ncnnGpus")}";

            string dainDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.dainNcnn.fileName));
            Process dain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            Init(dain);
            dain.StartInfo.Arguments = $"{OSUtils.GetHiddenCmdArg()} cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args} -f {InterpolateUtils.lastExt}";
            Logger.Log("Running DAIN...", false);
            Logger.Log("cmd.exe " + dain.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                dain.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "dain-ncnn-log.txt"); };
                dain.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "dain-ncnn-log.txt"); };
            }
            dain.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                dain.BeginOutputReadLine();
                dain.BeginErrorReadLine();
            }
            while (!dain.HasExited)
                await Task.Delay(1);
            Logger.Log($"Done running DAIN - Interpolation took " + FormatUtils.Time(processTime.Elapsed));
            processTime.Stop();
        }

        public static async Task RunCainNcnnMulti (string framesPath, string outPath, int tilesize, int times)
        {
            Logger.Log("Running CAIN...", false);

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")}";
            await RunCainPartial(args);

            if(times == 4 || times == 8)    // #2
            {
                Logger.Log("Re-Running CAIN for 4x interpolation...", false);
                string run1ResultsPath = outPath + "-run1";
                IOUtils.TryDeleteIfExists(run1ResultsPath);
                Directory.Move(outPath, run1ResultsPath);
                Directory.CreateDirectory(outPath);
                args = $" -v -i {run1ResultsPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")}";
                await RunCainPartial(args);
                Directory.Delete(run1ResultsPath, true);
            }

            if (times == 8)    // #3
            {
                Logger.Log("Re-Running CAIN for 8x interpolation...", false);
                string run2ResultsPath = outPath + "-run2";
                IOUtils.TryDeleteIfExists(run2ResultsPath);
                Directory.Move(outPath, run2ResultsPath);
                Directory.CreateDirectory(outPath);
                args = $" -v -i {run2ResultsPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")}";
                await RunCainPartial(args);
                Directory.Delete(run2ResultsPath, true);
            }

            Logger.Log($"Done running CAIN - Interpolation took " + FormatUtils.Time(processTime.Elapsed));
            processTime.Stop();
        }

        static async Task RunCainPartial (string args)
        {
            string cainDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.cainNcnn.fileName));
            string cainExe = "cain-ncnn-vulkan.exe";
            Process cain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            Init(cain);
            cain.StartInfo.Arguments = $"{OSUtils.GetHiddenCmdArg()} cd /D {cainDir.Wrap()} & {cainExe} {args} -f {InterpolateUtils.lastExt}";
            Logger.Log("cmd.exe " + cain.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                cain.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "cain-ncnn-log.txt"); };
                cain.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "cain-ncnn-log.txt"); };
            }
            cain.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                cain.BeginOutputReadLine();
                cain.BeginErrorReadLine();
            }
            while (!cain.HasExited) await Task.Delay(1);

        }

        public static async Task RunRifeCuda(string framesPath, int interpFactor)
        {
            string script = "interp-parallel.py";
            if(Config.GetInt("rifeMode") == 0 || IOUtils.GetAmountOfFiles(framesPath, false) < 10)
                script = "interp-basic.py";

            string rifeDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.rifeCuda.fileName));
            Process rifePy = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            Init(rifePy, "png", true);
            string args = $" --input {framesPath.Wrap()} --times {(int)Math.Log(interpFactor, 2)}";
            rifePy.StartInfo.Arguments = $"{OSUtils.GetHiddenCmdArg()} cd /D {rifeDir.Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get("torchGpus")} & {Python.GetPyCmd()} {script} {args} --imgformat {InterpolateUtils.lastExt}";
            Logger.Log($"Running RIFE ({script})...", false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "rife-cuda-log.txt"); };
                rifePy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-cuda-log.txt"); };
            }
            rifePy.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.BeginOutputReadLine();
                rifePy.BeginErrorReadLine();
            }
            while (!rifePy.HasExited)
                await Task.Delay(1);
            Logger.Log($"Done running RIFE - Interpolation took " + FormatUtils.Time(processTime.Elapsed));
            processTime.Stop();
        }   

        static void LogOutput (string line, string logFilename)
        {
            if (line == null)
                return;

            Logger.LogToFile(line, false, logFilename);

            if (!hasShownError && line.ToLower().Contains("out of memory"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"Your GPU ran out of VRAM! Please try a video with a lower resolution or use the Max Video Size option in the settings.\n\n{line}", "Error");
            }

            if (!hasShownError && line.ToLower().Contains("modulenotfounderror"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A python module is missing. Check {logFilename} for details.\n\n{line}", "Error");
            }

            if (!hasShownError && line.ToLower().Contains("ff:nocuda-cpu"))
                Logger.Log($"WARNING: CUDA-capable GPU device is not available, running on CPU instead!");

            if (!hasShownError && line.ToLower().Contains("no longer supports this gpu"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage("Your GPU seems to be outdated and is not supported!\n\n{line}", "Error");
            }

            if (!hasShownError && line.Contains("RuntimeError"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"An error occured during interpolation!\n\n{line}", "Error");
            }
        }
    }
}
