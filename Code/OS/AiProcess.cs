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
using Flowframes.Magick;
using Flowframes.Media;

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

        static void SetProgressCheck(string interpPath, float factor)
        {
            int frames = IOUtils.GetAmountOfFiles(lastInPath, false, "*.png");
            int target = ((frames * factor) - (factor - 1)).RoundToInt();
            InterpolateUtils.progressPaused = false;
            InterpolateUtils.currentFactor = factor;

            if (InterpolateUtils.progCheckRunning)
                InterpolateUtils.targetFrames = target;
            else
                InterpolateUtils.GetProgressByFrameAmount(interpPath, target);
        }

        static async Task AiFinished(string aiName)
        {
            if (Interpolate.canceled) return;
            Program.mainForm.SetProgress(100);
            InterpolateUtils.UpdateInterpProgress(IOUtils.GetAmountOfFiles(Interpolate.current.interpFolder, false, "*.png"), InterpolateUtils.targetFrames);
            string logStr = $"Done running {aiName} - Interpolation took {FormatUtils.Time(processTime.Elapsed)}";
            if (Interpolate.currentlyUsingAutoEnc && AutoEncode.HasWorkToDo())
                logStr += " - Waiting for encoding to finish...";
            Logger.Log(logStr);
            processTime.Stop();

            while (Interpolate.currentlyUsingAutoEnc && Program.busy)
            {
                if (AvProcess.lastProcess != null && !AvProcess.lastProcess.HasExited && AvProcess.lastTask == AvProcess.TaskType.Encode)
                {
                    string lastLine = AvProcess.lastOutputFfmpeg.SplitIntoLines().Last();
                    Logger.Log(lastLine.Trim().TrimWhitespaces(), false, Logger.GetLastLine().Contains("frame"));
                }

                if (AvProcess.timeSinceLastOutput.IsRunning && AvProcess.timeSinceLastOutput.ElapsedMilliseconds > 2500)
                    break;

                await Task.Delay(500);
            }

            if (!Interpolate.canceled && Interpolate.current.alpha)
            {
                Logger.Log("Processing alpha...");
                string rgbInterpDir = Path.Combine(Interpolate.current.tempFolder, Paths.interpDir);
                string alphaInterpDir = Path.Combine(Interpolate.current.tempFolder, Paths.interpDir + Paths.alphaSuffix);
                if (!Directory.Exists(alphaInterpDir)) return;
                await FfmpegAlpha.MergeAlphaIntoRgb(rgbInterpDir, Padding.interpFrames, alphaInterpDir, Padding.interpFrames, false);
            }
        }

        public static async Task RunRifeCuda(string framesPath, float interpFactor, string mdl)
        {
            if(Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            string rifeDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.rifeCuda.fileName));
            string script = "rife.py";

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
                await RunRifeCudaProcess(framesPath + Paths.alphaSuffix, Paths.interpDir + Paths.alphaSuffix, script, interpFactor, mdl);
            }

            await AiFinished("RIFE");
        }

        public static async Task RunRifeCudaProcess (string inPath, string outDir, string script, float interpFactor, string mdl)
        {
            bool parallel = false;
            bool unbuffered = true;
            string uhdStr = await InterpolateUtils.UseUHD() ? "--UHD" : "";
            string outPath = Path.Combine(inPath.GetParentDir(), outDir);
            string args = $" --input {inPath.Wrap()} --output {outDir} --model {mdl} --exp {(int)Math.Log(interpFactor, 2)} ";
            if (parallel) args = $" --input {inPath.Wrap()} --output {outPath} --model {mdl} --factor {interpFactor}";
            if (parallel) script = "rife-parallel.py";

            Process rifePy = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(rifePy, 3500);
            SetProgressCheck(Path.Combine(Interpolate.current.tempFolder, outDir), interpFactor);
            rifePy.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeCuda).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get("torchGpus")} & {Python.GetPyCmd()} " + (unbuffered ? "-u" : "") + $" {script} {args}";
            Logger.Log($"Running RIFE {(await InterpolateUtils.UseUHD() ? "(UHD Mode)" : "")} ({script})...".TrimWhitespaces(), false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);

            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "rife-cuda-log"); };
                rifePy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-cuda-log", true); };
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

            await RunRifeNcnnMulti(framesPath, outPath, factor, mdl);

            if (!Interpolate.canceled && Interpolate.current.alpha)
            {
                InterpolateUtils.progressPaused = true;
                Logger.Log("Interpolating alpha channel...");
                await RunRifeNcnnMulti(framesPath + Paths.alphaSuffix, outPath + Paths.alphaSuffix, factor, mdl);
            }

            await AiFinished("RIFE");
        }

        static async Task RunRifeNcnnMulti(string framesPath, string outPath, int factor, string mdl)
        {
            int times = (int)Math.Log(factor, 2);

            if (times > 1)
                AutoEncode.paused = true;  // Disable autoenc until the last iteration
            else
                AutoEncode.paused = false;

            for (int iteration = 1; iteration <= times; iteration++)
            {
                if (Interpolate.canceled) return;

                if (Interpolate.currentlyUsingAutoEnc && iteration == times)      // Enable autoenc if this is the last iteration
                    AutoEncode.paused = false;

                if (iteration > 1)
                {
                    Logger.Log($"Re-Running RIFE for {Math.Pow(2, iteration)}x interpolation...", false);
                    string lastInterpPath = outPath + $"-run{iteration - 1}";
                    Directory.Move(outPath, lastInterpPath);      // Rename last interp folder
                    await RunRifeNcnnProcess(lastInterpPath, outPath, mdl);
                    IOUtils.TryDeleteIfExists(lastInterpPath);
                }
                else
                {
                    await RunRifeNcnnProcess(framesPath, outPath, mdl);
                }
            }
        }

        static async Task RunRifeNcnnProcess(string inPath, string outPath, string mdl)
        {
            Directory.CreateDirectory(outPath);
            Process rifeNcnn = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(rifeNcnn, 1500, inPath);
            SetProgressCheck(outPath, 2);

            string uhdStr = await InterpolateUtils.UseUHD() ? "-u" : "";
            string ttaStr = Config.GetBool("rifeNcnnUseTta", false) ? "-x" : "";

            string oldMdlName = mdl;
            mdl = RifeNcnn2Workaround(mdl);     // TODO: REMOVE ONCE NIHUI HAS GOTTEN RID OF THE SHITTY HARDCODED VERSION CHECK

            rifeNcnn.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {PkgUtils.GetPkgFolder(Packages.rifeNcnn).Wrap()} & rife-ncnn-vulkan.exe " +
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} -m {mdl.ToLower()} {ttaStr} {uhdStr} -g {Config.Get("ncnnGpus")} -f {GetNcnnPattern()} -j {GetNcnnThreads()}";
            
            Logger.Log("cmd.exe " + rifeNcnn.StartInfo.Arguments, true);
           
            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "rife-ncnn-log"); };
                rifeNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-ncnn-log", true); };
            }

            rifeNcnn.Start();

            if (!OSUtils.ShowHiddenCmd())
            {
                rifeNcnn.BeginOutputReadLine();
                rifeNcnn.BeginErrorReadLine();
            }

            while (!rifeNcnn.HasExited) await Task.Delay(1);
            RifeNcnn2Workaround(oldMdlName, true);
        }

        public static async Task RunDainNcnn(string framesPath, string outPath, float factor, string mdl, int tilesize)
        {
            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            await RunDainNcnnProcess(framesPath, outPath, factor, mdl, tilesize);

            if (!Interpolate.canceled && Interpolate.current.alpha)
            {
                InterpolateUtils.progressPaused = true;
                Logger.Log("Interpolating alpha channel...");
                await RunDainNcnnProcess(framesPath + Paths.alphaSuffix, outPath + Paths.alphaSuffix, factor, mdl, tilesize);
            }

            await AiFinished("DAIN");
        }

        public static async Task RunDainNcnnProcess (string framesPath, string outPath, float factor, string mdl, int tilesize)
        {
            string dainDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.dainNcnn.fileName));
            Directory.CreateDirectory(outPath);
            Process dain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(dain, 1500);
            SetProgressCheck(outPath, factor);
            int targetFrames = ((IOUtils.GetAmountOfFiles(lastInPath, false, "*.png") * factor).RoundToInt()) - (factor.RoundToInt() - 1); // TODO: Won't work with fractional factors

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -n {targetFrames} -m {mdl.ToLower()}" +
                $" -t {GetNcnnTilesize(tilesize)} -g {Config.Get("ncnnGpus")} -f {GetNcnnPattern()} -j 2:1:2";

            dain.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args}";
            Logger.Log("Running DAIN...", false);
            Logger.Log("cmd.exe " + dain.StartInfo.Arguments, true);

            if (!OSUtils.ShowHiddenCmd())
            {
                dain.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "dain-ncnn-log"); };
                dain.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "dain-ncnn-log", true); };
            }

            dain.Start();
            if (!OSUtils.ShowHiddenCmd())
            {
                dain.BeginOutputReadLine();
                dain.BeginErrorReadLine();
            }

            while (!dain.HasExited)
                await Task.Delay(100);
        }

        static void LogOutput (string line, string logFilename, bool err = false)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 6)
                return;

            Logger.LogToFile(line, false, logFilename);

            if (line.Contains("ff:nocuda-cpu"))
                Logger.Log("WARNING: CUDA-capable GPU device is not available, running on CPU instead!");

            if (!hasShownError && err && line.ToLower().Contains("out of memory"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"Your GPU ran out of VRAM! Please try a video with a lower resolution or use the Max Video Size option in the settings.\n\n{line}", "Error");
            }

            if (!hasShownError && err && line.ToLower().Contains("modulenotfounderror"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A python module is missing.\nCheck {logFilename} for details.\n\n{line}", "Error");
                if(!Python.HasEmbeddedPyFolder())
                    Process.Start("https://github.com/n00mkrad/flowframes/blob/main/PythonDependencies.md");
            }

            if (!hasShownError && line.ToLower().Contains("no longer supports this gpu"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"Your GPU seems to be outdated and is not supported!\n\n{line}", "Error");
            }

            if (!hasShownError && err && (line.Contains("RuntimeError") || line.Contains("ImportError") || line.Contains("OSError")))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A python error occured during interpolation!\nCheck {logFilename} for details.\n\n{line}", "Error");
            }

            if (!hasShownError && err && line.Contains("vk") && line.Contains(" failed"))
            {
                hasShownError = true;
                string dain = (Interpolate.current.ai.aiName == Networks.dainNcnn.aiName) ? "\n\nTry reducing the tile size in the AI settings." : "";
                InterpolateUtils.ShowMessage($"A Vulkan error occured during interpolation!\n\n{line}{dain}", "Error");
            }

            if (!hasShownError && err && line.Contains("vkAllocateMemory failed"))
            {
                hasShownError = true;
                bool usingDain = (Interpolate.current.ai.aiName == Networks.dainNcnn.aiName);
                string msg = usingDain ? "\n\nTry reducing the tile size in the AI settings." : "Try a lower resolution (Settings -> Max Video Size).";
                InterpolateUtils.ShowMessage($"Vulkan ran out of memory!\n\n{line}{msg}", "Error");
            }

            if (!hasShownError && err && line.Contains("invalid gpu device"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"A Vulkan error occured during interpolation!\n\n{line}\n\nAre your GPU IDs set correctly?", "Error");
            }

            if (hasShownError)
                Interpolate.Cancel();

            InterpolateUtils.UpdateLastFrameFromInterpOutput(line);
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

        static string RifeNcnn2Workaround (string modelName, bool reset = false)
        {
            if (modelName != "RIFE20") return modelName;
            string validMdlName = "rife-v2";
            string rifeFolderPath = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.rifeNcnn.fileName));
            string modelFolderPath = Path.Combine(rifeFolderPath, modelName);
            string fixedModelFolderPath = Path.Combine(rifeFolderPath, validMdlName);

            if (!reset)
            {
                IOUtils.TryDeleteIfExists(fixedModelFolderPath);
                Directory.Move(modelFolderPath, fixedModelFolderPath);
            }
            else
            {
                IOUtils.TryDeleteIfExists(modelFolderPath);
                Directory.Move(fixedModelFolderPath, modelFolderPath);
            }

            return validMdlName;
        }
    }
}
