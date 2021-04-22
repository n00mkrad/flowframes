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
using System.Drawing;

namespace Flowframes
{
    class AiProcess
    {
        public static bool hasShownError;

        public static Process lastAiProcess;
        public static Stopwatch processTime = new Stopwatch();
        public static Stopwatch processTimeMulti = new Stopwatch();

        public static int lastStartupTimeMs = 1000;
        static string lastInPath;

        public static void Kill ()
        {
            if (lastAiProcess == null) return;

            try
            {
                OSUtils.KillProcessTree(lastAiProcess.Id);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to kill currentAiProcess process tree: {e.Message}", true);
            }
        }

        static void AiStarted (Process proc, int startupTimeMs, string inPath = "")
        {
            lastStartupTimeMs = startupTimeMs;
            processTime.Restart();
            lastAiProcess = proc;
            lastInPath = string.IsNullOrWhiteSpace(inPath) ? Interpolate.current.framesFolder : inPath;
            hasShownError = false;
        }

        static void SetProgressCheck(string interpPath, float factor)
        {
            int frames = IOUtils.GetAmountOfFiles(lastInPath, false, "*.*");
            int target = ((frames * factor) - (factor - 1)).RoundToInt();
            InterpolationProgress.progressPaused = false;
            InterpolationProgress.currentFactor = factor;

            if (InterpolationProgress.progCheckRunning)
                InterpolationProgress.targetFrames = target;
            else
                InterpolationProgress.GetProgressByFrameAmount(interpPath, target);
        }

        static async Task AiFinished(string aiName)
        {
            if (Interpolate.canceled) return;
            Program.mainForm.SetProgress(100);
            InterpolationProgress.UpdateInterpProgress(IOUtils.GetAmountOfFiles(Interpolate.current.interpFolder, false, "*" + Interpolate.current.interpExt), InterpolationProgress.targetFrames);
            string logStr = $"Done running {aiName} - Interpolation took {FormatUtils.Time(processTime.Elapsed)}. Peak Output FPS: {InterpolationProgress.peakFpsOut.ToString("0.00")}";
            
            if (Interpolate.currentlyUsingAutoEnc && AutoEncode.HasWorkToDo())
            {
                logStr += " - Waiting for encoding to finish...";
                Program.mainForm.SetStatus("Creating output video from frames...");
            }

            Logger.Log(logStr);
            processTime.Stop();

            while (Interpolate.currentlyUsingAutoEnc && Program.busy)
            {
                if (AvProcess.lastAvProcess != null && !AvProcess.lastAvProcess.HasExited && AvProcess.lastTask == AvProcess.TaskType.Encode)
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

            try
            {
                string rifeDir = Path.Combine(Paths.GetPkgPath(), Networks.rifeCuda.pkgDir);
                string script = "rife.py";

                if (!File.Exists(Path.Combine(rifeDir, script)))
                {
                    Interpolate.Cancel("RIFE script not found! Make sure you didn't modify any files.");
                    return;
                }

                await RunRifeCudaProcess(framesPath, Paths.interpDir, script, interpFactor, mdl);

                if (!Interpolate.canceled && Interpolate.current.alpha)
                {
                    InterpolationProgress.progressPaused = true;
                    Logger.Log("Interpolating alpha channel...");
                    await RunRifeCudaProcess(framesPath + Paths.alphaSuffix, Paths.interpDir + Paths.alphaSuffix, script, interpFactor, mdl);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error running RIFE-CUDA: " + e.Message);
            }

            await AiFinished("RIFE");
        }

        public static async Task RunRifeCudaProcess (string inPath, string outDir, string script, float interpFactor, string mdl)
        {
            string outPath = Path.Combine(inPath.GetParentDir(), outDir);
            Directory.CreateDirectory(outPath);
            string uhdStr = await InterpolateUtils.UseUhd() ? "--UHD" : "";
            string wthreads = $"--wthreads {2 * (int)interpFactor}";
            string rbuffer = $"--rbuffer {Config.GetInt("rifeCudaBufferSize", 200)}";
            //string scale = $"--scale {Config.GetFloat("rifeCudaScale", 1.0f).ToStringDot()}";
            string prec = Config.GetBool("rifeCudaFp16") ? "--fp16" : "";
            string args = $" --input {inPath.Wrap()} --output {outDir} --model {mdl} --exp {(int)Math.Log(interpFactor, 2)} {uhdStr} {wthreads} {rbuffer} {prec}";

            Process rifePy = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(rifePy, 3500);
            SetProgressCheck(Path.Combine(Interpolate.current.tempFolder, outDir), interpFactor);
            rifePy.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Networks.rifeCuda.pkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get("torchGpus")} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running RIFE (CUDA){(await InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);

            if (!OSUtils.ShowHiddenCmd())
            {
                rifePy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "rife-cuda-log"); };
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

        public static async Task RunFlavrCuda(string framesPath, float interpFactor, string mdl)
        {
            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            try
            {
                string flavDir = Path.Combine(Paths.GetPkgPath(), Networks.flavrCuda.pkgDir);
                string script = "flavr.py";

                if (!File.Exists(Path.Combine(flavDir, script)))
                {
                    Interpolate.Cancel("FLAVR script not found! Make sure you didn't modify any files.");
                    return;
                }

                await RunFlavrCudaProcess(framesPath, Paths.interpDir, script, interpFactor, mdl);

                if (!Interpolate.canceled && Interpolate.current.alpha)
                {
                    InterpolationProgress.progressPaused = true;
                    Logger.Log("Interpolating alpha channel...");
                    await RunFlavrCudaProcess(framesPath + Paths.alphaSuffix, Paths.interpDir + Paths.alphaSuffix, script, interpFactor, mdl);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error running FLAVR-CUDA: " + e.Message);
            }

            await AiFinished("FLAVR");
        }

        public static async Task RunFlavrCudaProcess(string inPath, string outDir, string script, float interpFactor, string mdl)
        {
            string outPath = Path.Combine(inPath.GetParentDir(), outDir);
            Directory.CreateDirectory(outPath);
            string args = $" --input {inPath.Wrap()} --output {outPath.Wrap()} --model {mdl}/{mdl}.pth --factor {interpFactor}";

            Process flavrPy = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(flavrPy, 4500);
            SetProgressCheck(Path.Combine(Interpolate.current.tempFolder, outDir), interpFactor);
            flavrPy.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Networks.flavrCuda.pkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get("torchGpus")} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running FLAVR (CUDA)...", false);
            Logger.Log("cmd.exe " + flavrPy.StartInfo.Arguments, true);

            if (!OSUtils.ShowHiddenCmd())
            {
                flavrPy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "flavr-cuda-log"); };
                flavrPy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "flavr-cuda-log", true); };
            }

            flavrPy.Start();

            if (!OSUtils.ShowHiddenCmd())
            {
                flavrPy.BeginOutputReadLine();
                flavrPy.BeginErrorReadLine();
            }

            while (!flavrPy.HasExited) await Task.Delay(1);
        }

        public static async Task RunRifeNcnn (string framesPath, string outPath, int factor, string mdl)
        {
            processTimeMulti.Restart();

            try
            {
                Logger.Log($"Running RIFE (NCNN){(await InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);

                await RunRifeNcnnMulti(framesPath, outPath, factor, mdl);

                if (!Interpolate.canceled && Interpolate.current.alpha)
                {
                    InterpolationProgress.progressPaused = true;
                    Logger.Log("Interpolating alpha channel...");
                    await RunRifeNcnnMulti(framesPath + Paths.alphaSuffix, outPath + Paths.alphaSuffix, factor, mdl);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error running RIFE-NCNN: " + e.Message);
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

            string uhdStr = await InterpolateUtils.UseUhd() ? "-u" : "";
            string ttaStr = Config.GetBool("rifeNcnnUseTta", false) ? "-x" : "";

            string oldMdlName = mdl;
            mdl = RifeNcnn2Workaround(mdl);     // TODO: REMOVE ONCE NIHUI HAS GOTTEN RID OF THE SHITTY HARDCODED VERSION CHECK

            rifeNcnn.StartInfo.Arguments = $"{OSUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Networks.rifeNcnn.pkgDir).Wrap()} & rife-ncnn-vulkan.exe " +
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

            try
            {
                await RunDainNcnnProcess(framesPath, outPath, factor, mdl, tilesize);

                if (!Interpolate.canceled && Interpolate.current.alpha)
                {
                    InterpolationProgress.progressPaused = true;
                    Logger.Log("Interpolating alpha channel...");
                    await RunDainNcnnProcess(framesPath + Paths.alphaSuffix, outPath + Paths.alphaSuffix, factor, mdl, tilesize);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error running DAIN-NCNN: " + e.Message);
            }

            await AiFinished("DAIN");
        }

        public static async Task RunDainNcnnProcess (string framesPath, string outPath, float factor, string mdl, int tilesize)
        {
            string dainDir = Path.Combine(Paths.GetPkgPath(), Networks.dainNcnn.pkgDir);
            Directory.CreateDirectory(outPath);
            Process dain = OSUtils.NewProcess(!OSUtils.ShowHiddenCmd());
            AiStarted(dain, 1500);
            SetProgressCheck(outPath, factor);
            int targetFrames = ((IOUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt()) - (factor.RoundToInt() - 1); // TODO: Won't work with fractional factors

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

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            if (line.Contains("iVBOR"))
            {
                try
                {
                    string[] split = line.Split(':');
                    //MemoryStream stream = new MemoryStream(Convert.FromBase64String(split[1]));
                    //Image img = Image.FromStream(stream);
                    Logger.Log($"Received image {split[0]} in {sw.ElapsedMilliseconds} ms", true);
                }
                catch (Exception e)
                {
                    Logger.Log($"Failed to decode b64 string - {e}:");
                    Logger.Log(line);
                }
                return;
            }

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

            if (!hasShownError && line.ToLower().Contains("illegal memory access"))
            {
                hasShownError = true;
                InterpolateUtils.ShowMessage($"Your GPU appears to be unstable! If you have an overclock enabled, please disable it!\n\n{line}", "Error");
            }

            if (!hasShownError && line.ToLower().Contains("error(s) in loading state_dict"))
            {
                hasShownError = true;
                string msg = (Interpolate.current.ai.aiName == Networks.flavrCuda.aiName) ? "\n\nFor FLAVR, you need to select the correct model for each scale!" : "";
                InterpolateUtils.ShowMessage($"Error loading the AI model!\n\n{line}{msg}", "Error");
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

            InterpolationProgress.UpdateLastFrameFromInterpOutput(line);
        }

        static string GetNcnnPattern ()
        {
            return $"%0{Padding.interpFrames}d{Interpolate.current.interpExt}";
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
            if (!modelName.StartsWith("RIFE2")) return modelName;
            string validMdlName = "rife-v2";
            string rifeFolderPath = Path.Combine(Paths.GetPkgPath(), Networks.rifeNcnn.pkgDir);
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
