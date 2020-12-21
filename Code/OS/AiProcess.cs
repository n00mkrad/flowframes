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
using Flowframes.Data;

namespace Flowframes
{
    class AiProcess
    {
        public static bool hasShownError;

        public static Process currentAiProcess;
        public static Stopwatch processTime = new Stopwatch();
        public static Stopwatch processTimeMulti = new Stopwatch();

        public static int lastStartupTimeMs = 1000;
        static string lastInPath;

        public static Dictionary<string, string> filenameMap = new Dictionary<string, string>();   // TODO: Store on disk instead for crashes?

        static void AiStarted (Process proc, int startupTimeMs, int factor, string inPath = "")
        {
            lastStartupTimeMs = startupTimeMs;
            processTime.Restart();
            currentAiProcess = proc;
            lastInPath = string.IsNullOrWhiteSpace(inPath) ? Interpolate.current.framesFolder : inPath;
            int frames = IOUtils.GetAmountOfFiles(lastInPath, false, "*.png");
            InterpolateUtils.currentFactor = factor;
            InterpolateUtils.targetFrames = (frames * factor) - (factor - 1);
            hasShownError = false;
        }

        static void AiFinished (string aiName)
        {
            Program.mainForm.SetProgress(100);
            InterpolateUtils.UpdateInterpProgress(IOUtils.GetAmountOfFiles(Interpolate.current.interpFolder, false, "*.png"), InterpolateUtils.targetFrames);
            string logStr = $"Done running {aiName} - Interpolation took {FormatUtils.Time(processTime.Elapsed)}";
            if (Interpolate.currentlyUsingAutoEnc && AutoEncode.HasWorkToDo())
                logStr += " - Waiting for encoding to finish...";
            Logger.Log(logStr);
            processTime.Stop();
        }

        public static async Task RunDainNcnn(string framesPath, string outPath, int targetFrames, int tilesize)
        {
            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -n {targetFrames} -t {GetNcnnTilesize(tilesize)} -g {Config.Get("ncnnGpus")}";

            string dainDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.dainNcnn.fileName));
            Process dain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(dain, 1500, Interpolate.current.interpFactor);
            dain.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args} -f {InterpolateUtils.GetOutExt()} -j {GetNcnnThreads()}";
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

            if (!Interpolate.currentlyUsingAutoEnc)
                IOUtils.ZeroPadDir(outPath, InterpolateUtils.GetOutExt(), Padding.interpFrames);

            AiFinished("DAIN");
        }

        public static async Task RunCainNcnnMulti (string framesPath, string outPath, int tilesize, int times)
        {
            processTimeMulti.Restart();
            Logger.Log("Running CAIN...", false);

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -g {Config.Get("ncnnGpus")}";
            await RunCainPartial(args);

            if(times == 4 || times == 8)    // #2
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running CAIN for 4x interpolation...", false);
                string run1ResultsPath = outPath + "-run1";
                IOUtils.TryDeleteIfExists(run1ResultsPath);
                Directory.Move(outPath, run1ResultsPath);
                Directory.CreateDirectory(outPath);
                args = $" -v -i {run1ResultsPath.Wrap()} -o {outPath.Wrap()} -g {Config.Get("ncnnGpus")}";
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
                args = $" -v -i {run2ResultsPath.Wrap()} -o {outPath.Wrap()} -g {Config.Get("ncnnGpus")}";
                await RunCainPartial(args);
                IOUtils.TryDeleteIfExists(run2ResultsPath);
            }

            if (Interpolate.canceled) return;

            if (!Interpolate.currentlyUsingAutoEnc)
                IOUtils.ZeroPadDir(outPath, InterpolateUtils.GetOutExt(), Padding.interpFrames);

            AiFinished("CAIN");
        }

        static async Task RunCainPartial (string args)
        {
            string cainDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.cainNcnn.fileName));
            string cainExe = "cain-ncnn-vulkan.exe";
            Process cain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(cain, 1500, 2);
            cain.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {cainDir.Wrap()} & {cainExe} {args} -f {InterpolateUtils.GetOutExt()} -j {GetNcnnThreads()}";
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
            string rifeDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.rifeCuda.fileName));
            string script = "inference_video.py";
            bool uhd = InterpolateUtils.GetOutputResolution(Interpolate.current.inPath).Height >= Config.GetInt("uhdThresh");
            string uhdStr = uhd ? "--UHD" : "";
            string args = $" --img {framesPath.Wrap()} --exp {(int)Math.Log(interpFactor, 2)} {uhdStr} --imgformat {InterpolateUtils.GetOutExt()} --output {Paths.interpDir}";

            if (!File.Exists(Path.Combine(rifeDir, script)))
            {
                Interpolate.Cancel("RIFE script not found! Make sure you didn't modify any files.");
                return;
            }

            Process rifePy = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(rifePy, 3500, Interpolate.current.interpFactor);
            rifePy.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeCuda).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get("torchGpus")} & {Pytorch.GetPyCmd()} {script} {args}";
            Logger.Log($"Running RIFE {(uhd ? "(UHD Mode)" : "")} ({script})...".TrimWhitespaces(), false);
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
            AiFinished("RIFE");
        }

        public static async Task RunRifeNcnnMulti(string framesPath, string outPath, int tilesize, int times)
        {
            processTimeMulti.Restart();
            Logger.Log("Running RIFE...", false);

            bool useAutoEnc = Interpolate.currentlyUsingAutoEnc;
            if(times > 2)
                AutoEncode.paused = true;  // Disable autoenc until the last iteration

            await RunRifePartial(framesPath, outPath);

            if (times == 4 || times == 8)    // #2
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running RIFE for 4x interpolation...", false);
                string run1ResultsPath = outPath + "-run1";
                IOUtils.TryDeleteIfExists(run1ResultsPath);
                Directory.Move(outPath, run1ResultsPath);
                Directory.CreateDirectory(outPath);
                if (useAutoEnc && times == 4)
                    AutoEncode.paused = false;
                await RunRifePartial(run1ResultsPath, outPath);
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
                if (useAutoEnc && times == 8)
                    AutoEncode.paused = false;                    
                await RunRifePartial(run2ResultsPath, outPath);
                IOUtils.TryDeleteIfExists(run2ResultsPath);
            }

            if (Interpolate.canceled) return;

            if (!Interpolate.currentlyUsingAutoEnc)
                IOUtils.ZeroPadDir(outPath, InterpolateUtils.GetOutExt(), Padding.interpFrames);

            AiFinished("RIFE");
        }

        static async Task RunRifePartial(string inPath, string outPath)
        {
            Process rifeNcnn = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(rifeNcnn, 1500, 2, inPath);
            rifeNcnn.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeNcnn).Wrap()} & rife-ncnn-vulkan.exe " +
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} -g {Config.Get("ncnnGpus")} -f {InterpolateUtils.GetOutExt()} -j {GetNcnnThreads()}";
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
            if (string.IsNullOrWhiteSpace(line) || line.Length < 6)
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
                InterpolateUtils.ShowMessage($"A python module is missing.\nCheck {logFilename} for details.\n\n{line}\n\nIf you don't want to install it yourself, use the Python package from the Package Installer.", "Error");
                if(!Pytorch.HasEmbeddedPyFolder())
                    Process.Start("https://github.com/n00mkrad/flowframes/blob/main/PythonDependencies.md");
            }

            if (!hasShownError && line.ToLower().Contains("no longer supports this gpu"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"Your GPU seems to be outdated and is not supported!\n\n{line}", "Error");
            }

            if (!hasShownError && (line.Contains("RuntimeError") || line.Contains("ImportError") || line.Contains("OSError")))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A python error occured during interpolation!\nCheck {logFilename} for details.\n\n{line}", "Error");
            }

            if (!hasShownError && (line.Contains("vkQueueSubmit failed") || line.Contains("vkAllocateMemory failed")))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A Vulkan error occured during interpolation!\n\n{line}", "Error");
            }

            if (!hasShownError && line.Contains("invalid gpu device"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A Vulkan error occured during interpolation!\n\n{line}\n\nAre your GPU IDs set correctly?", "Error");
            }

            if (hasShownError)
                Interpolate.Cancel();
        }

        static string GetNcnnTilesize(int tilesize)
        {
            int gpusAmount = Config.Get("ncnnGpus").Split(',').Length;
            string tilesizeStr = $"{tilesize}";

            for (int i = 1; i < gpusAmount; i++)
                tilesizeStr += $",{tilesize}";

            return tilesizeStr;
        }

        static string GetNcnnThreads ()
        {
            int gpusAmount = Config.Get("ncnnGpus").Split(',').Length;
            int procThreads = Config.GetInt("ncnnThreads");
            string progThreadsStr = $"{procThreads}";

            for (int i = 1; i < gpusAmount; i++)
                progThreadsStr += $",{procThreads}";

            return $"4:{progThreadsStr}:4"; ;
        }
    }
}
