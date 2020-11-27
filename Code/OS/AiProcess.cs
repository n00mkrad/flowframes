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
        public static Stopwatch processTimeMulti = new Stopwatch();

        public static int lastStartupTimeMs = 1000;

        static void Init (Process proc, int startupTimeMs, string defaultExt = "png")
        {
            lastStartupTimeMs = startupTimeMs;
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
            Init(dain, 1500);
            dain.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args} -f {InterpolateUtils.lastExt} -j 4:{Config.Get("ncnnThreads")}:4";
            Logger.Log("Running DAIN...", false);
            Logger.Log("cmd.exe " + dain.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                dain.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "dain-ncnn-log.txt"); };
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

            if (Interpolate.canceled) return;
            Magick.MagickDedupe.ZeroPadDir(outPath, InterpolateUtils.lastExt, 8);
            Logger.Log($"Done running DAIN - Interpolation took " + FormatUtils.Time(processTime.Elapsed));
            processTime.Stop();
        }

        public static async Task RunCainNcnnMulti (string framesPath, string outPath, int tilesize, int times)
        {
            processTimeMulti.Restart();
            Logger.Log("Running CAIN...", false);

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")}";
            await RunCainPartial(args);

            if(times == 4 || times == 8)    // #2
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running CAIN for 4x interpolation...", false);
                string run1ResultsPath = outPath + "-run1";
                IOUtils.TryDeleteIfExists(run1ResultsPath);
                Directory.Move(outPath, run1ResultsPath);
                Directory.CreateDirectory(outPath);
                args = $" -v -i {run1ResultsPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")}";
                await RunCainPartial(args);
                IOUtils.TryDeleteIfExists(run1ResultsPath);
            }

            if (times == 8)    // #3
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running CAIN for 8x interpolation...", false);
                string run2ResultsPath = outPath + "-run2";
                IOUtils.TryDeleteIfExists(run2ResultsPath);
                Directory.Move(outPath, run2ResultsPath);
                Directory.CreateDirectory(outPath);
                args = $" -v -i {run2ResultsPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")}";
                await RunCainPartial(args);
                IOUtils.TryDeleteIfExists(run2ResultsPath);
            }

            if (Interpolate.canceled) return;
            Magick.MagickDedupe.ZeroPadDir(outPath, InterpolateUtils.lastExt, 8);

            Logger.Log($"Done running CAIN - Interpolation took " + FormatUtils.Time(processTimeMulti.Elapsed));
            processTime.Stop();
        }

        static async Task RunCainPartial (string args)
        {
            string cainDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.cainNcnn.fileName));
            string cainExe = "cain-ncnn-vulkan.exe";
            Process cain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            Init(cain, 1500);
            cain.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {cainDir.Wrap()} & {cainExe} {args} -f {InterpolateUtils.lastExt} -j 4:{Config.Get("ncnnThreads")}:4";
            Logger.Log("cmd.exe " + cain.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                cain.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "cain-ncnn-log.txt"); };
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
            Init(rifePy, 3000, "png");
            string args = $" --input {framesPath.Wrap()} --times {(int)Math.Log(interpFactor, 2)}";
            rifePy.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeCuda).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get("torchGpus")} & {Pytorch.GetPyCmd()} {script} {args} --imgformat {InterpolateUtils.lastExt}";
            Logger.Log($"Running RIFE ({script})...", false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "rife-cuda-log.txt"); };
                rifePy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-cuda-log.txt"); };
            }
            rifePy.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.BeginOutputReadLine();
                rifePy.BeginErrorReadLine();
            }
            while (!rifePy.HasExited) await Task.Delay(1);
            Logger.Log($"Done running RIFE - Interpolation took " + FormatUtils.Time(processTime.Elapsed));
            processTime.Stop();
        }

        public static async Task RunRifeNcnn (string framesPath, string outPath, int interpFactor, int tilesize)
        {
            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")} -f {InterpolateUtils.lastExt} -j 4:{Config.Get("ncnnThreads")}:4";
            Process rifeNcnn = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            Init(rifeNcnn, 750);
            rifeNcnn.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeNcnn).Wrap()} & rife-ncnn-vulkan.exe {args}";
            Logger.Log("Running RIFE...", false);
            Logger.Log("cmd.exe " + rifeNcnn.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "rife-ncnn-log.txt"); };
                rifeNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-ncnn-log.txt"); };
            }
            rifeNcnn.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.BeginOutputReadLine();
                rifeNcnn.BeginErrorReadLine();
            }
            while (!rifeNcnn.HasExited) await Task.Delay(1);

            if (Interpolate.canceled) return;
            Magick.MagickDedupe.ZeroPadDir(outPath, InterpolateUtils.lastExt, 8);
            Logger.Log($"Done running RIFE - Interpolation took " + FormatUtils.Time(processTime.Elapsed));
            processTime.Stop();
        }

        public static async Task RunRifeNcnnMulti(string framesPath, string outPath, int tilesize, int times)
        {
            processTimeMulti.Restart();
            Logger.Log("Running RIFE...", false);

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")} -f {InterpolateUtils.lastExt} -j 4:{Config.Get("ncnnThreads")}:4";
            await RunRifePartial(args);

            if (times == 4 || times == 8)    // #2
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running RIFE for 4x interpolation...", false);
                string run1ResultsPath = outPath + "-run1";
                IOUtils.TryDeleteIfExists(run1ResultsPath);
                Directory.Move(outPath, run1ResultsPath);
                Directory.CreateDirectory(outPath);
                args = $" -v -i {run1ResultsPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")} -f {InterpolateUtils.lastExt} -j 4:{Config.Get("ncnnThreads")}:4";
                await RunRifePartial(args);
                IOUtils.TryDeleteIfExists(run1ResultsPath);
            }

            if (times == 8)    // #3
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running RIFE for 8x interpolation...", false);
                string run2ResultsPath = outPath + "-run2";
                IOUtils.TryDeleteIfExists(run2ResultsPath);
                Directory.Move(outPath, run2ResultsPath);
                Directory.CreateDirectory(outPath);
                args = $" -v -i {run2ResultsPath.Wrap()} -o {outPath.Wrap()} -t {tilesize} -g {Config.Get("ncnnGpus")} -f {InterpolateUtils.lastExt} -j 4:{Config.Get("ncnnThreads")}:4";
                await RunRifePartial(args);
                IOUtils.TryDeleteIfExists(run2ResultsPath);
            }

            if (Interpolate.canceled) return;
            Magick.MagickDedupe.ZeroPadDir(outPath, InterpolateUtils.lastExt, 8);

            Logger.Log($"Done running RIFE - Interpolation took " + FormatUtils.Time(processTimeMulti.Elapsed));
            processTime.Stop();
        }

        static async Task RunRifePartial(string args)
        {
            Process rifeNcnn = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            Init(rifeNcnn, 1500);
            rifeNcnn.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeNcnn).Wrap()} & rife-ncnn-vulkan.exe {args} -f {InterpolateUtils.lastExt} -j 4:{Config.Get("ncnnThreads")}:4";
            Logger.Log("cmd.exe " + rifeNcnn.StartInfo.Arguments, true);
            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "rife-ncnn-log.txt"); };
                rifeNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-ncnn-log.txt"); };
            }
            rifeNcnn.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.BeginOutputReadLine();
                rifeNcnn.BeginErrorReadLine();
            }
            while (!rifeNcnn.HasExited) await Task.Delay(1);
        }

        static void LogOutput (string line, string logFilename)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            Logger.LogToFile(line, false, logFilename);

            if (line.Contains("ff:nocuda-cpu"))
                Logger.Log("WARNING: CUDA-capable GPU device is not available, running on CPU instead!");

            if (!hasShownError && line.ToLower().Contains("out of memory"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"Your GPU ran out of VRAM! Please try a video with a lower resolution or use the Max Video Size option in the settings.\n\n{line}", "Error");
            }

            if (!hasShownError && line.ToLower().Contains("modulenotfounderror"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A python module is missing. Check {logFilename} for details.\n\n{line}\n\nIf you don't want to install it yourself, use the Python package from the Package Installer.", "Error");
            }

            if (!hasShownError && line.ToLower().Contains("no longer supports this gpu"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"Your GPU seems to be outdated and is not supported!\n\n{line}", "Error");
            }

            if (!hasShownError && line.Contains("RuntimeError"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"An error occured during interpolation!\n\n{line}", "Error");
            }

            if (!hasShownError && line.Contains("vkQueueSubmit failed"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A Vulkan error occured during interpolation!\n\n{line}", "Error");
            }

            if (hasShownError)
                Interpolate.Cancel();
        }
    }
}
