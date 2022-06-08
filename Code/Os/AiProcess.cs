using Flowframes.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flowframes.Ui;
using Flowframes.Main;
using Flowframes.Data;
using Flowframes.MiscUtils;
using System.Collections.Generic;
using ImageMagick;
using Paths = Flowframes.IO.Paths;
using Flowframes.Media;
using System.Drawing;

namespace Flowframes.Os
{
    class AiProcess
    {
        public static bool hasShownError;
        public static string lastLogName;
        public static Process lastAiProcess;
        public static Stopwatch processTime = new Stopwatch();
        public static Stopwatch processTimeMulti = new Stopwatch();

        public static int lastStartupTimeMs = 1000;
        static string lastInPath;

        public static void Kill()
        {
            if (lastAiProcess == null) return;

            try
            {
                AiProcessSuspend.SetRunning(false);
                OsUtils.KillProcessTree(lastAiProcess.Id);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to kill currentAiProcess process tree: {e.Message}", true);
            }
        }

        static void AiStarted(Process proc, int startupTimeMs, string inPath = "")
        {
            lastStartupTimeMs = startupTimeMs;
            processTime.Restart();
            lastAiProcess = proc;
            AiProcessSuspend.SetRunning(true);
            lastInPath = string.IsNullOrWhiteSpace(inPath) ? Interpolate.current.framesFolder : inPath;
            hasShownError = false;
        }

        static void AiStartedRt(Process proc, string inPath = "")
        {
            lastAiProcess = proc;
            AiProcessSuspend.SetRunning(true);
            lastInPath = string.IsNullOrWhiteSpace(inPath) ? Interpolate.current.framesFolder : inPath;
            hasShownError = false;
        }

        static void SetProgressCheck(string interpPath, float factor)
        {
            int frames = IoUtils.GetAmountOfFiles(lastInPath, false);
            int target = ((frames * factor) - (factor - 1)).RoundToInt();
            InterpolationProgress.progressPaused = false;
            InterpolationProgress.currentFactor = factor;

            if (InterpolationProgress.progCheckRunning)
                InterpolationProgress.targetFrames = target;
            else
                InterpolationProgress.GetProgressByFrameAmount(interpPath, target);
        }

        static void SetProgressCheck(int sourceFrames, float factor, string logFile)
        {
            int target = ((sourceFrames * factor) - (factor - 1)).RoundToInt();
            InterpolationProgress.progressPaused = false;
            InterpolationProgress.currentFactor = factor;

            if (InterpolationProgress.progCheckRunning)
                InterpolationProgress.targetFrames = target;
            else
                InterpolationProgress.GetProgressFromFfmpegLog(logFile, target);
        }

        static async Task AiFinished(string aiName, bool rt = false)
        {
            if (Interpolate.canceled) return;
            Program.mainForm.SetProgress(100);
            AiProcessSuspend.SetRunning(false);

            if (rt)
            {
                Logger.Log($"Stopped running {aiName}.");
                return;
            }

            int interpFramesFiles = IoUtils.GetAmountOfFiles(Interpolate.current.interpFolder, false, "*" + Interpolate.current.interpExt);
            int interpFramesCount = interpFramesFiles + InterpolationProgress.deletedFramesCount;

            if (!Interpolate.current.ai.Piped)
                InterpolationProgress.UpdateInterpProgress(interpFramesCount, InterpolationProgress.targetFrames);

            string logStr = $"Done running {aiName} - Interpolation took {FormatUtils.Time(processTime.Elapsed)}. Peak Output FPS: {InterpolationProgress.peakFpsOut.ToString("0.00")}";

            if (Interpolate.currentlyUsingAutoEnc && AutoEncode.HasWorkToDo())
            {
                logStr += " - Waiting for encoding to finish...";
                Program.mainForm.SetStatus("Creating output video from frames...");
            }

            Logger.Log(logStr);
            processTime.Stop();

            if (!Interpolate.current.ai.Piped && interpFramesCount < 3)
            {
                string[] logLines = File.ReadAllLines(Path.Combine(Paths.GetLogPath(), lastLogName + ".txt"));
                string log = string.Join("\n", logLines.Reverse().Take(10).Reverse().Select(x => x.Split("]: ").Last()).ToList());
                string amount = interpFramesCount > 0 ? $"Only {interpFramesCount}" : "No";
                Interpolate.Cancel($"Interpolation failed - {amount} interpolated frames were created.\n\n\nLast 10 log lines:\n{log}\n\nCheck the log '{lastLogName}' for more details.");
                return;
            }

            try
            {
                while (Interpolate.currentlyUsingAutoEnc && Program.busy)
                {
                    if (AvProcess.lastAvProcess != null && !AvProcess.lastAvProcess.HasExited)
                    {
                        if (Logger.LastLogLine.ToLower().Contains("frame: "))
                            Logger.Log(FormatUtils.BeautifyFfmpegStats(Logger.LastLogLine), false, Logger.LastUiLine.ToLower().Contains("frame"));
                    }

                    if (AvProcess.lastAvProcess.HasExited && !AutoEncode.HasWorkToDo())     // Stop logging if ffmpeg is not running & AE is done
                        break;

                    await Task.Delay(500);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"AiFinished encoder logging error: {e.Message}\n{e.StackTrace}", true);
            }
        }

        public static async Task RunRifeCuda(string framesPath, float interpFactor, string mdl)
        {
            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            try
            {
                string rifeDir = Path.Combine(Paths.GetPkgPath(), Implementations.rifeCuda.PkgDir);
                string script = "rife.py";

                if (!File.Exists(Path.Combine(rifeDir, script)))
                {
                    Interpolate.Cancel("RIFE script not found! Make sure you didn't modify any files.");
                    return;
                }

                string archFilesDir = Path.Combine(rifeDir, "arch");
                string archFilesDirModel = Path.Combine(rifeDir, mdl, "arch");

                if (Directory.Exists(archFilesDirModel))
                {
                    Logger.Log($"Model {mdl} has architecture python files - copying to arch.", true);
                    IoUtils.DeleteContentsOfDir(archFilesDir);
                    IoUtils.CopyDir(archFilesDirModel, archFilesDir);
                }

                await RunRifeCudaProcess(framesPath, Paths.interpDir, script, interpFactor, mdl);
            }
            catch (Exception e)
            {
                Logger.Log("Error running RIFE-CUDA: " + e.Message);
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished("RIFE");
        }

        public static async Task RunRifeCudaProcess(string inPath, string outDir, string script, float interpFactor, string mdl)
        {
            string outPath = Path.Combine(inPath.GetParentDir(), outDir);
            Directory.CreateDirectory(outPath);
            string uhdStr = await InterpolateUtils.UseUhd() ? "--UHD" : "";
            string wthreads = $"--wthreads {2 * (int)interpFactor}";
            string rbuffer = $"--rbuffer {Config.GetInt(Config.Key.rifeCudaBufferSize, 200)}";
            //string scale = $"--scale {Config.GetFloat("rifeCudaScale", 1.0f).ToStringDot()}";
            string prec = Config.GetBool(Config.Key.rifeCudaFp16) ? "--fp16" : "";
            string args = $" --input {inPath.Wrap()} --output {outDir} --model {mdl} --multi {interpFactor} {uhdStr} {wthreads} {rbuffer} {prec}";

            Process rifePy = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(rifePy, 3500);
            SetProgressCheck(Path.Combine(Interpolate.current.tempFolder, outDir), interpFactor);
            rifePy.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.rifeCuda.PkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running RIFE (CUDA){(await InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                rifePy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "rife-cuda-log"); };
                rifePy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "rife-cuda-log", true); };
            }

            rifePy.Start();

            if (!OsUtils.ShowHiddenCmd())
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
                string flavDir = Path.Combine(Paths.GetPkgPath(), Implementations.flavrCuda.PkgDir);
                string script = "flavr.py";

                if (!File.Exists(Path.Combine(flavDir, script)))
                {
                    Interpolate.Cancel("FLAVR script not found! Make sure you didn't modify any files.");
                    return;
                }

                await RunFlavrCudaProcess(framesPath, Paths.interpDir, script, interpFactor, mdl);
            }
            catch (Exception e)
            {
                Logger.Log("Error running FLAVR-CUDA: " + e.Message);
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished("FLAVR");
        }

        public static async Task RunFlavrCudaProcess(string inPath, string outDir, string script, float interpFactor, string mdl)
        {
            string outPath = Path.Combine(inPath.GetParentDir(), outDir);
            Directory.CreateDirectory(outPath);
            string args = $" --input {inPath.Wrap()} --output {outPath.Wrap()} --model {mdl}/{mdl}.pth --factor {interpFactor}";

            Process flavrPy = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(flavrPy, 4000);
            SetProgressCheck(Path.Combine(Interpolate.current.tempFolder, outDir), interpFactor);
            flavrPy.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.flavrCuda.PkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running FLAVR (CUDA)...", false);
            Logger.Log("cmd.exe " + flavrPy.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                flavrPy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "flavr-cuda-log"); };
                flavrPy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "flavr-cuda-log", true); };
            }

            flavrPy.Start();

            if (!OsUtils.ShowHiddenCmd())
            {
                flavrPy.BeginOutputReadLine();
                flavrPy.BeginErrorReadLine();
            }

            while (!flavrPy.HasExited) await Task.Delay(1);
        }

        public static async Task RunRifeNcnn(string framesPath, string outPath, float factor, string mdl)
        {
            processTimeMulti.Restart();

            try
            {
                Logger.Log($"Running RIFE (NCNN){(await InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);

                //await RunRifeNcnnMulti(framesPath, outPath, factor, mdl);
                await RunRifeNcnnProcess(framesPath, factor, outPath, mdl);
                await DeleteNcnnDupes(outPath, factor);
            }
            catch (Exception e)
            {
                Logger.Log("Error running RIFE-NCNN: " + e.Message);
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished("RIFE");
        }

        static async Task RunRifeNcnnProcess(string inPath, float factor, string outPath, string mdl)
        {
            Directory.CreateDirectory(outPath);
            string logFileName = "rife-ncnn-log";
            Process rifeNcnn = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(rifeNcnn, 1500, inPath);
            SetProgressCheck(outPath, factor);
            int targetFrames = ((IoUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt()); // TODO: Maybe won't work with fractional factors ??

            string frames = mdl.Contains("v4") ? $"-n {targetFrames}" : "";
            string uhdStr = await InterpolateUtils.UseUhd() ? "-u" : "";
            string ttaStr = Config.GetBool(Config.Key.rifeNcnnUseTta, false) ? "-x" : "";

            rifeNcnn.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnn.PkgDir).Wrap()} & rife-ncnn-vulkan.exe " +
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} {frames} -m {mdl.ToLower()} {ttaStr} {uhdStr} -g {Config.Get(Config.Key.ncnnGpus)} -f {GetNcnnPattern()} -j {GetNcnnThreads()}";

            Logger.Log("cmd.exe " + rifeNcnn.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                rifeNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, logFileName); };
                rifeNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, logFileName, true); };
            }

            rifeNcnn.Start();

            if (!OsUtils.ShowHiddenCmd())
            {
                rifeNcnn.BeginOutputReadLine();
                rifeNcnn.BeginErrorReadLine();
            }

            while (!rifeNcnn.HasExited) await Task.Delay(1);
        }

        public static async Task RunRifeNcnnVs(string framesPath, string outPath, float factor, string mdl, bool rt = false)
        {
            processTimeMulti.Restart();

            try
            {
                bool uhd = await InterpolateUtils.UseUhd();
                Logger.Log($"Running RIFE (NCNN-VS){(uhd ? " (UHD Mode)" : "")}...", false);

                await RunRifeNcnnVsProcess(framesPath, factor, outPath, mdl, uhd, rt);
            }
            catch (Exception e)
            {
                Logger.Log("Error running RIFE-NCNN-VS: " + e.Message);
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished("RIFE", true);
        }

        static async Task RunRifeNcnnVsProcess(string inPath, float factor, string outPath, string mdl, bool uhd, bool rt = false)
        {
            IoUtils.CreateDir(outPath);
            string logFileName = "rife-ncnn-vs-log";
            Process rifeNcnnVs = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());

            if (rt)
            {
                AiStartedRt(rifeNcnnVs, inPath);
            }
            else
            {
                SetProgressCheck(Interpolate.currentInputFrameCount, factor, logFileName);
                AiStarted(rifeNcnnVs, 1500, inPath);
            }

            string avDir = Path.Combine(Paths.GetPkgPath(), Paths.audioVideoDir);
            string rtArgs = $"-window_title \"Flowframes Realtime Interpolation ({Interpolate.current.inFps.GetString()} FPS x{factor} = {Interpolate.current.outFps.GetString()} FPS - {mdl})\" -autoexit -seek_interval {VapourSynthUtils.GetSeekSeconds(Program.mainForm.currInDuration)} ";

            Interpolate.current.FullOutPath = Path.Combine(Interpolate.current.outPath, await IoUtils.GetCurrentExportFilename(false, true));
            string encArgs = FfmpegUtils.GetEncArgs(FfmpegUtils.GetCodec(Interpolate.current.outMode), (Interpolate.current.ScaledResolution.IsEmpty ? Interpolate.current.InputResolution : Interpolate.current.ScaledResolution), Interpolate.current.outFps.GetFloat(), true).FirstOrDefault();
            string ffmpegArgs = rt ? $"{Path.Combine(avDir, "ffplay").Wrap()} {rtArgs} - " : $"{Path.Combine(avDir, "ffmpeg").Wrap()} -y -i pipe: {encArgs} {Interpolate.current.FullOutPath.Wrap()}";

            string pkgDir = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir);

            VapourSynthUtils.VsSettings vsSettings = new VapourSynthUtils.VsSettings()
            {
                InterpSettings = Interpolate.current,
                ModelDir = mdl,
                Factor = factor,
                Res = InterpolateUtils.GetOutputResolution(Interpolate.current.InputResolution, true, true),
                Uhd = uhd,
                SceneDetectSensitivity = Config.GetBool(Config.Key.scnDetect) ? Config.GetFloat(Config.Key.scnDetectValue) * 0.7f : 0f,
                Loop = Config.GetBool(Config.Key.enableLoop),
                MatchDuration = Config.GetBool(Config.Key.fixOutputDuration),
                Dedupe = Config.GetInt(Config.Key.dedupMode) != 0,
                Realtime = rt
            };

            rifeNcnnVs.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {pkgDir.Wrap()} & vspipe {VapourSynthUtils.CreateScript(vsSettings).Wrap()} -c y4m - | {ffmpegArgs}";

            Logger.Log("cmd.exe " + rifeNcnnVs.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                rifeNcnnVs.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, logFileName); };
                rifeNcnnVs.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, logFileName, true); };
            }

            rifeNcnnVs.Start();

            if (!OsUtils.ShowHiddenCmd())
            {
                rifeNcnnVs.BeginOutputReadLine();
                rifeNcnnVs.BeginErrorReadLine();
            }

            while (!rifeNcnnVs.HasExited) await Task.Delay(1);
        }

        public static async Task RunDainNcnn(string framesPath, string outPath, float factor, string mdl, int tilesize)
        {
            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            try
            {
                await RunDainNcnnProcess(framesPath, outPath, factor, mdl, tilesize);
                await DeleteNcnnDupes(outPath, factor);
            }
            catch (Exception e)
            {
                Logger.Log("Error running DAIN-NCNN: " + e.Message);
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished("DAIN");
        }

        public static async Task RunDainNcnnProcess(string framesPath, string outPath, float factor, string mdl, int tilesize)
        {
            string dainDir = Path.Combine(Paths.GetPkgPath(), Implementations.dainNcnn.PkgDir);
            Directory.CreateDirectory(outPath);
            Process dain = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(dain, 1500);
            SetProgressCheck(outPath, factor);
            int targetFrames = ((IoUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt());

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -n {targetFrames} -m {mdl.ToLower()}" +
                $" -t {GetNcnnTilesize(tilesize)} -g {Config.Get(Config.Key.ncnnGpus)} -f {GetNcnnPattern()} -j 2:1:2";

            dain.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args}";
            Logger.Log("Running DAIN...", false);
            Logger.Log("cmd.exe " + dain.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                dain.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, "dain-ncnn-log"); };
                dain.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "dain-ncnn-log", true); };
            }

            dain.Start();

            if (!OsUtils.ShowHiddenCmd())
            {
                dain.BeginOutputReadLine();
                dain.BeginErrorReadLine();
            }

            while (!dain.HasExited)
                await Task.Delay(100);
        }

        public static async Task RunXvfiCuda(string framesPath, float interpFactor, string mdl)
        {
            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            try
            {
                string xvfiDir = Path.Combine(Paths.GetPkgPath(), Implementations.xvfiCuda.PkgDir);
                string script = "main.py";

                if (!File.Exists(Path.Combine(xvfiDir, script)))
                {
                    Interpolate.Cancel("XVFI script not found! Make sure you didn't modify any files.");
                    return;
                }

                await RunXvfiCudaProcess(framesPath, Paths.interpDir, script, interpFactor, mdl);
            }
            catch (Exception e)
            {
                Logger.Log("Error running XVFI-CUDA: " + e.Message);
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished("XVFI");
        }

        public static async Task RunXvfiCudaProcess(string inPath, string outDir, string script, float interpFactor, string mdlDir)
        {
            string pkgPath = Path.Combine(Paths.GetPkgPath(), Implementations.xvfiCuda.PkgDir);
            string basePath = inPath.GetParentDir();
            string outPath = Path.Combine(basePath, outDir);
            Directory.CreateDirectory(outPath);
            string mdlArgs = File.ReadAllText(Path.Combine(pkgPath, mdlDir, "args.ini"));
            string args = $" --custom_path {basePath.Wrap()} --input {inPath.Wrap()} --output {outPath.Wrap()} --mdl_dir {mdlDir}" +
                $" --multiple {interpFactor} --gpu 0 {mdlArgs}";

            Process xvfiPy = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(xvfiPy, 3500);
            SetProgressCheck(Path.Combine(Interpolate.current.tempFolder, outDir), interpFactor);
            xvfiPy.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {pkgPath.Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running XVFI (CUDA)...", false);
            Logger.Log("cmd.exe " + xvfiPy.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                xvfiPy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, "xvfi-cuda-log"); };
                xvfiPy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, "xvfi-cuda-log", true); };
            }

            xvfiPy.Start();

            if (!OsUtils.ShowHiddenCmd())
            {
                xvfiPy.BeginOutputReadLine();
                xvfiPy.BeginErrorReadLine();
            }

            while (!xvfiPy.HasExited) await Task.Delay(1);
        }

        static void LogOutput(string line, string logFilename, bool err = false)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 6)
                return;

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            lastLogName = logFilename;
            Logger.Log(line, true, false, logFilename);

            if (line.Contains("ff:nocuda-cpu"))
                Logger.Log("WARNING: CUDA-capable GPU device is not available, running on CPU instead!");

            if (true /* if NCNN VS */)
            {
                if (!hasShownError && line.ToLower().Contains("allocate memory failed"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"Out of memory!\nTry reducing your RAM usage by closing some programs.\n\n{line}", UiUtils.MessageType.Error);
                }

                if (!hasShownError && line.ToLower().Contains("vapoursynth.error:"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"VapourSynth Error:\n\n{line}", UiUtils.MessageType.Error);
                }
            }

            if (!hasShownError && err && line.ToLower().Contains("out of memory"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"Your GPU ran out of VRAM! Please try a video with a lower resolution or use the Max Video Size option in the settings.\n\n{line}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && err && line.ToLower().Contains("modulenotfounderror"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"A python module is missing.\nCheck {logFilename} for details.\n\n{line}", UiUtils.MessageType.Error);
                if (!Python.HasEmbeddedPyFolder())
                    Process.Start("https://github.com/n00mkrad/flowframes/blob/main/PythonDependencies.md");
            }

            if (!hasShownError && line.ToLower().Contains("no longer supports this gpu"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"Your GPU seems to be outdated and is not supported!\n\n{line}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && line.ToLower().Contains("illegal memory access"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"Your GPU appears to be unstable! If you have an overclock enabled, please disable it!\n\n{line}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && line.ToLower().Contains("error(s) in loading state_dict"))
            {
                hasShownError = true;
                string msg = (Interpolate.current.ai.AiName == Implementations.flavrCuda.AiName) ? "\n\nFor FLAVR, you need to select the correct model for each scale!" : "";
                UiUtils.ShowMessageBox($"Error loading the AI model!\n\n{line}{msg}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && line.ToLower().Contains("unicodeencodeerror"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"It looks like your path contains invalid characters - remove them and try again!\n\n{line}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && err && (line.Contains("RuntimeError") || line.Contains("ImportError") || line.Contains("OSError")))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"A python error occured during interpolation!\nCheck {logFilename} for details.\n\n{line}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && err && line.MatchesWildcard("vk*Instance* failed"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"Vulkan failed to start up!\n\n{line}\n\nThis most likely means your GPU is not compatible.", UiUtils.MessageType.Error);
            }

            if (!hasShownError && err && line.Contains("vkAllocateMemory failed"))
            {
                hasShownError = true;
                bool usingDain = (Interpolate.current.ai.AiName == Implementations.dainNcnn.AiName);
                string msg = usingDain ? "\n\nTry reducing the tile size in the AI settings." : "\n\nTry a lower resolution (Settings -> Max Video Size).";
                UiUtils.ShowMessageBox($"Vulkan ran out of memory!\n\n{line}{msg}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && err && line.Contains("invalid gpu device"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"A Vulkan error occured during interpolation!\n\n{line}\n\nAre your GPU IDs set correctly?", UiUtils.MessageType.Error);
            }

            if (!hasShownError && err && line.MatchesWildcard("vk* failed"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"A Vulkan error occured during interpolation!\n\n{line}", UiUtils.MessageType.Error);
            }

            if (hasShownError)
                Interpolate.Cancel();

            InterpolationProgress.UpdateLastFrameFromInterpOutput(line);
        }

        static string GetNcnnPattern()
        {
            return $"%0{Padding.interpFrames}d{Interpolate.current.interpExt}";
        }

        static string GetNcnnTilesize(int tilesize)
        {
            int gpusAmount = Config.Get(Config.Key.ncnnGpus).Split(',').Length;
            string tilesizeStr = $"{tilesize}";

            for (int i = 1; i < gpusAmount; i++)
                tilesizeStr += $",{tilesize}";

            return tilesizeStr;
        }

        static string GetNcnnThreads(bool forceSingleThread = false)
        {
            int gpusAmount = Config.Get(Config.Key.ncnnGpus).Split(',').Length;
            int procThreads = Config.GetInt(Config.Key.ncnnThreads);
            string progThreadsStr = $"{procThreads}";

            for (int i = 1; i < gpusAmount; i++)
                progThreadsStr += $",{procThreads}";

            return $"{(forceSingleThread ? 1 : (Interpolate.currentlyUsingAutoEnc ? 2 : 4))}:{progThreadsStr}:4"; // Read threads: 1 for singlethreaded, 2 for autoenc, 4 if order is irrelevant
        }

        static async Task DeleteNcnnDupes(string dir, float factor)
        {
            int dupeCount = InterpolateUtils.GetRoundedInterpFramesPerInputFrame(factor);
            var files = IoUtils.GetFileInfosSorted(dir, false).Reverse().Take(dupeCount).ToList();
            Logger.Log($"DeleteNcnnDupes: Calculated dupe count from factor; deleting last {dupeCount} interp frames of {IoUtils.GetAmountOfFiles(dir, false)} ({string.Join(", ", files.Select(x => x.Name))})", true);

            int attempts = 4;

            while (attempts > 0)
            {
                try
                {
                    files.ForEach(x => x.Delete());
                    break;
                }
                catch (Exception ex)
                {
                    attempts--;

                    if (attempts < 1)
                    {
                        Logger.Log($"DeleteNcnnDupes Error: {ex.Message}", true);
                        break;
                    }
                    else
                    {
                        await Task.Delay(500);
                    }
                }
            }
        }
    }
}
