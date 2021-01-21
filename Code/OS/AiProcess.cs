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
using Flowframes.MiscUtils;

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

        static void AiStarted (Process proc, int startupTimeMs, string inPath = "")
        {
            lastStartupTimeMs = startupTimeMs;
            processTime.Restart();
            currentAiProcess = proc;
            lastInPath = string.IsNullOrWhiteSpace(inPath) ? Interpolate.current.framesFolder : inPath;
            hasShownError = false;
        }

        static void SetProgressCheck(string interpPath, int factor)
        {
            int frames = IOUtils.GetAmountOfFiles(lastInPath, false, "*.png");
            int target = (frames * factor) - (factor - 1);
            InterpolateUtils.progressPaused = false;
            InterpolateUtils.currentFactor = factor;

            if (InterpolateUtils.progCheckRunning)
                InterpolateUtils.targetFrames = target;
            else
                InterpolateUtils.GetProgressByFrameAmount(interpPath, target);
        }

        static async Task AiFinished (string aiName)
        {
            Program.mainForm.SetProgress(100);
            InterpolateUtils.UpdateInterpProgress(IOUtils.GetAmountOfFiles(Interpolate.current.interpFolder, false, "*.png"), InterpolateUtils.targetFrames);
            string logStr = $"Done running {aiName} - Interpolation took {FormatUtils.Time(processTime.Elapsed)}";
            if (Interpolate.currentlyUsingAutoEnc && AutoEncode.HasWorkToDo())
                logStr += " - Waiting for encoding to finish...";
            Logger.Log(logStr);
            processTime.Stop();

            Stopwatch timeSinceFfmpegRan = new Stopwatch();
            timeSinceFfmpegRan.Restart();

            while (Interpolate.currentlyUsingAutoEnc && Program.busy)
            {
                if (AvProcess.lastProcess != null && !AvProcess.lastProcess.HasExited && AvProcess.lastTask == AvProcess.TaskType.Encode)
                {
                    timeSinceFfmpegRan.Restart();
                    string lastLine = AvProcess.lastOutputFfmpeg.SplitIntoLines().Last();
                    Logger.Log(lastLine.Trim().TrimWhitespaces(), false, Logger.GetLastLine().Contains("frame"));
                }
                if (timeSinceFfmpegRan.ElapsedMilliseconds > 3000)
                    break;
                // Logger.Log($"AiProcess loop - Program.busy = {Program.busy}");
                await Task.Delay(500);
            }
        }

        public static async Task RunRifeCuda(string framesPath, int interpFactor, string mdl)
        {
            string rifeDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.rifeCuda.fileName));
            string script = "inference_video.py";

            if (!File.Exists(Path.Combine(rifeDir, script)))
            {
                Interpolate.Cancel("RIFE script not found! Make sure you didn't modify any files.");
                return;
            }

            await RunRifeCudaProcess(framesPath, Paths.interpDir, script, interpFactor, mdl);

            if (!Interpolate.canceled && Interpolate.current.alpha)
            {
                InterpolateUtils.progressPaused = true;
                Logger.Log("Interpolating alpha channel...");
                await RunRifeCudaProcess(framesPath + "-a", Paths.interpDir + "-a", script, interpFactor, mdl);
            }

            await AiFinished("RIFE");

            if (!Interpolate.canceled && Interpolate.current.alpha)
            {
                Logger.Log("Processing alpha...");
                string rgbInterpDir = Path.Combine(Interpolate.current.tempFolder, Paths.interpDir);
                await FFmpegCommands.MergeAlphaIntoRgb(rgbInterpDir, Padding.interpFrames, rgbInterpDir + "-a", Padding.interpFrames);
            }
        }

        public static async Task RunRifeCudaProcess (string inPath, string outDir, string script, int interpFactor, string mdl)
        {
            string uhdStr = await InterpolateUtils.UseUHD() ? "--UHD" : "";
            string args = $" --input {inPath.Wrap()} --model {mdl} --exp {(int)Math.Log(interpFactor, 2)} {uhdStr} --imgformat {InterpolateUtils.GetOutExt()} --output {outDir}";
            
            Process rifePy = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(rifePy, 3500);
            SetProgressCheck(Path.Combine(Interpolate.current.tempFolder, outDir), interpFactor);
            rifePy.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeCuda).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get("torchGpus")} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running RIFE {(await InterpolateUtils.UseUHD() ? "(UHD Mode)" : "")} ({script})...".TrimWhitespaces(), false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);

            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "rife-cuda-log"); };
                rifePy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-cuda-log"); };
            }
            rifePy.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.BeginOutputReadLine();
                rifePy.BeginErrorReadLine();
            }

            while (!rifePy.HasExited) await Task.Delay(1);
        }

        public static async Task RunRifeNcnn (string framesPath, string outPath, int factor, string mdl)
        {
            processTimeMulti.Restart();
            Logger.Log($"Running RIFE{(await InterpolateUtils.UseUHD() ? " (UHD Mode)" : "")}...", false);

            bool useAutoEnc = Interpolate.currentlyUsingAutoEnc;
            if(factor > 2)
                AutoEncode.paused = true;  // Disable autoenc until the last iteration

            await RunRifeNcnnProcess(framesPath, outPath, mdl);

            if (factor == 4 || factor == 8)    // #2
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running RIFE for 4x interpolation...", false);
                string run1ResultsPath = outPath + "-run1";
                IOUtils.TryDeleteIfExists(run1ResultsPath);
                Directory.Move(outPath, run1ResultsPath);
                Directory.CreateDirectory(outPath);
                if (useAutoEnc && factor == 4)
                    AutoEncode.paused = false;
                await RunRifeNcnnProcess(run1ResultsPath, outPath, mdl);
                IOUtils.TryDeleteIfExists(run1ResultsPath);
            }

            if (factor == 8)    // #3
            {
                if (Interpolate.canceled) return;
                Logger.Log("Re-Running RIFE for 8x interpolation...", false);
                string run2ResultsPath = outPath + "-run2";
                IOUtils.TryDeleteIfExists(run2ResultsPath);
                Directory.Move(outPath, run2ResultsPath);
                Directory.CreateDirectory(outPath);
                if (useAutoEnc && factor == 8)
                    AutoEncode.paused = false;                    
                await RunRifeNcnnProcess(run2ResultsPath, outPath, mdl);
                IOUtils.TryDeleteIfExists(run2ResultsPath);
            }

            if (Interpolate.canceled) return;

            if (!Interpolate.currentlyUsingAutoEnc)
                IOUtils.ZeroPadDir(outPath, InterpolateUtils.GetOutExt(), Padding.interpFrames);

            AiFinished("RIFE");
        }

        static async Task RunRifeNcnnProcess(string inPath, string outPath, string mdl)
        {
            Process rifeNcnn = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(rifeNcnn, 1500, inPath);
            SetProgressCheck(outPath, 2);

            string uhdStr = await InterpolateUtils.UseUHD() ? "-u" : "";
            string ttaStr = Config.GetBool("rifeNcnnUseTta", false) ? "-x" : "";

            rifeNcnn.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeNcnn).Wrap()} & rife-ncnn-vulkan.exe " +
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} -m {mdl.ToLower()} {ttaStr} {uhdStr} -g {Config.Get("ncnnGpus")} -f {GetNcnnPattern()} -j {GetNcnnThreads()}";
            
            Logger.Log("cmd.exe " + rifeNcnn.StartInfo.Arguments, true);
           
            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "rife-ncnn-log"); };
                rifeNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-ncnn-log"); };
            }

            rifeNcnn.Start();

            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.BeginOutputReadLine();
                rifeNcnn.BeginErrorReadLine();
            }

            while (!rifeNcnn.HasExited) await Task.Delay(1);
        }

        public static async Task RunDainNcnn(string framesPath, string outPath, int factor, string mdl, int tilesize)
        {
            string dainDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.dainNcnn.fileName));
            Process dain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());

            AiStarted(dain, 1500);
            SetProgressCheck(outPath, factor);

            int targetFrames = (IOUtils.GetAmountOfFiles(lastInPath, false, "*.png") * factor) - (factor - 1);

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -n {targetFrames} -m {mdl.ToLower()}" +
                $" -t {GetNcnnTilesize(tilesize)} -g {Config.Get("ncnnGpus")} -f {GetNcnnPattern()} -j 2:1:2";

            dain.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args}";
            Logger.Log("Running DAIN...", false);
            Logger.Log("cmd.exe " + dain.StartInfo.Arguments, true);

            if (!OSUtils.ShowHiddenCmd())
            {
                dain.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "dain-ncnn-log"); };
                dain.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "dain-ncnn-log"); };
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

            //if (!Interpolate.currentlyUsingAutoEnc)
            //    IOUtils.ZeroPadDir(outPath, InterpolateUtils.GetOutExt(), Padding.interpFrames);

            AiFinished("DAIN");
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
                if(!Python.HasEmbeddedPyFolder())
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

        static string GetNcnnPattern ()
        {
            return $"%0{Padding.interpFrames}d.{InterpolateUtils.GetOutExt()}";
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
