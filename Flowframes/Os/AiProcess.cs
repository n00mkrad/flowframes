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
using Flowframes.Utilities;
using static NmkdUtils.StringExtensions;

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
            lastInPath = string.IsNullOrWhiteSpace(inPath) ? Interpolate.currentSettings.framesFolder : inPath;
            hasShownError = false;
        }

        static void AiStartedRt(Process proc, string inPath = "")
        {
            lastAiProcess = proc;
            AiProcessSuspend.SetRunning(true);
            lastInPath = string.IsNullOrWhiteSpace(inPath) ? Interpolate.currentSettings.framesFolder : inPath;
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

            int interpFramesFiles = IoUtils.GetAmountOfFiles(Interpolate.currentSettings.interpFolder, false, "*" + Interpolate.currentSettings.interpExt);
            int interpFramesCount = interpFramesFiles + InterpolationProgress.deletedFramesCount;

            if (!Interpolate.currentSettings.ai.Piped)
                InterpolationProgress.UpdateInterpProgress(interpFramesCount, InterpolationProgress.targetFrames);

            string logStr = $"Done running {aiName} - Interpolation took {FormatUtils.Time(processTime.Elapsed)}. Peak Output FPS: {InterpolationProgress.peakFpsOut.ToString("0.00")}";

            if (Interpolate.currentlyUsingAutoEnc && AutoEncode.HasWorkToDo())
            {
                logStr += " - Waiting for encoding to finish...";
                Program.mainForm.SetStatus("Creating output video from frames...");
            }

            if (Interpolate.currentSettings.outSettings.Format != Enums.Output.Format.Realtime)
                Logger.Log(logStr);

            processTime.Stop();

            if (!Interpolate.currentSettings.ai.Piped && interpFramesCount < 3)
            {
                string amount = interpFramesCount > 0 ? $"Only {interpFramesCount}" : "No";

                if (lastLogName.IsEmpty())
                {
                    Interpolate.Cancel($"Interpolation failed - {amount} interpolated frames were created, and no log was written.");
                    return;
                }

                string[] logLines = File.ReadAllLines(Path.Combine(Paths.GetLogPath(), lastLogName + ".txt"));
                string log = string.Join("\n", logLines.Reverse().Take(10).Reverse().Select(x => x.Split("]: ").Last()).ToList());
                Interpolate.Cancel($"Interpolation failed - {amount} interpolated frames were created.\n\n\nLast 10 log lines:\n{log}\n\nCheck the log '{lastLogName}' for more details.");
                return;
            }

            try
            {
                while (Interpolate.currentlyUsingAutoEnc && Program.busy)
                {
                    if (AvProcess.lastAvProcess != null && !AvProcess.lastAvProcess.HasExited)
                    {
                        if (Logger.LastLogLine.Lower().Contains("frame: "))
                            Logger.Log(FormatUtils.BeautifyFfmpegStats(Logger.LastLogLine), false, Logger.LastUiLine.Lower().Contains("frame"));
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
            AI ai = Implementations.rifeCuda;

            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            try
            {
                string rifeDir = Path.Combine(Paths.GetPkgPath(), ai.PkgDir);
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
                Logger.Log($"Error running {ai.FriendlyName}: {e.Message}");
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished(ai.NameShort);
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
            SetProgressCheck(Path.Combine(Interpolate.currentSettings.tempFolder, outDir), interpFactor);
            rifePy.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.rifeCuda.PkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running RIFE (CUDA){(await InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                rifePy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, Implementations.rifeCuda); };
                rifePy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.rifeCuda, true); };
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
            AI ai = Implementations.flavrCuda;

            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            try
            {
                string flavDir = Path.Combine(Paths.GetPkgPath(), ai.PkgDir);
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
                Logger.Log($"Error running {ai.FriendlyName}: {e.Message}");
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished(ai.NameShort);
        }

        public static async Task RunFlavrCudaProcess(string inPath, string outDir, string script, float interpFactor, string mdl)
        {
            string outPath = Path.Combine(inPath.GetParentDir(), outDir);
            Directory.CreateDirectory(outPath);
            string args = $" --input {inPath.Wrap()} --output {outPath.Wrap()} --model {mdl}/{mdl}.pth --factor {interpFactor}";

            Process flavrPy = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(flavrPy, 4000);
            SetProgressCheck(Path.Combine(Interpolate.currentSettings.tempFolder, outDir), interpFactor);
            flavrPy.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.flavrCuda.PkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running FLAVR (CUDA)...", false);
            Logger.Log("cmd.exe " + flavrPy.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                flavrPy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, Implementations.flavrCuda); };
                flavrPy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.flavrCuda, true); };
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
            AI ai = Implementations.rifeNcnn;
            processTimeMulti.Restart();

            try
            {
                Logger.Log($"Running RIFE (NCNN){(await InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);

                await RunRifeNcnnProcess(framesPath, factor, outPath, mdl);
                await NcnnUtils.DeleteNcnnDupes(outPath, factor);
            }
            catch (Exception e)
            {
                Logger.Log($"Error running {ai.FriendlyName}: {e.Message}");
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished(ai.NameShort);
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
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} {frames} -m {mdl.Lower()} {ttaStr} {uhdStr} -g {Config.Get(Config.Key.ncnnGpus)} -f {NcnnUtils.GetNcnnPattern()} -j {NcnnUtils.GetNcnnThreads(Implementations.rifeNcnn)}";

            Logger.Log("cmd.exe " + rifeNcnn.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                rifeNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.rifeNcnn); };
                rifeNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.rifeNcnn, true); };
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
            if (Interpolate.canceled) return;

            AI ai = Implementations.rifeNcnnVs;
            processTimeMulti.Restart();

            try
            {
                Size scaledSize = await InterpolateUtils.GetOutputResolution(Interpolate.currentSettings.inPath, false, false);
                Logger.Log($"Running RIFE (NCNN-VS){(InterpolateUtils.UseUhd(scaledSize) ? " (UHD Mode)" : "")}...", false);

                await RunRifeNcnnVsProcess(framesPath, factor, outPath, mdl, scaledSize, rt);
            }
            catch (Exception e)
            {
                Logger.Log($"Error running {ai.FriendlyName}: {e.Message}");
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished(ai.NameShort);
        }

        static async Task RunRifeNcnnVsProcess(string inPath, float factor, string outPath, string mdl, Size res, bool rt = false)
        {
            IoUtils.CreateDir(outPath);
            Process rifeNcnnVs = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            string avDir = Path.Combine(Paths.GetPkgPath(), Paths.audioVideoDir);
            string pipedTargetArgs = $"{Path.Combine(avDir, "ffmpeg").Wrap()} -y {await Export.GetPipedFfmpegCmd(rt)}";
            string pkgDir = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir);
            int gpuId = Config.Get(Config.Key.ncnnGpus).Split(',')[0].GetInt();

            VapourSynthUtils.VsSettings vsSettings = new VapourSynthUtils.VsSettings()
            {
                InterpSettings = Interpolate.currentSettings,
                ModelDir = mdl,
                Factor = factor,
                Res = res,
                Uhd = InterpolateUtils.UseUhd(res),
                GpuId = gpuId,
                GpuThreads = NcnnUtils.GetRifeNcnnGpuThreads(res, gpuId, Implementations.rifeNcnnVs),
                SceneDetectSensitivity = Config.GetBool(Config.Key.scnDetect) ? Config.GetFloat(Config.Key.scnDetectValue) * 0.7f : 0f,
                Loop = Config.GetBool(Config.Key.enableLoop),
                MatchDuration = Config.GetBool(Config.Key.fixOutputDuration),
                Dedupe = Config.GetInt(Config.Key.dedupMode) != 0,
                Realtime = rt,
                Osd = Config.GetBool(Config.Key.vsRtShowOsd),
            };

            if (rt)
            {
                Logger.Log($"Starting. Use Space to pause, Left Arrow and Right Arrow to seek, though seeking can be slow.");
                AiStartedRt(rifeNcnnVs, inPath);
            }
            else
            {
                SetProgressCheck(Interpolate.currentMediaFile.FrameCount, factor, Implementations.rifeNcnnVs.LogFilename);
                AiStarted(rifeNcnnVs, 1000, inPath);
            }

            rifeNcnnVs.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {pkgDir.Wrap()} & vspipe {VapourSynthUtils.CreateScript(vsSettings).Wrap()} -c y4m - | {pipedTargetArgs}";

            Logger.Log("cmd.exe " + rifeNcnnVs.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                rifeNcnnVs.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.rifeNcnnVs); };
                rifeNcnnVs.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.rifeNcnnVs, true); };
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
            AI ai = Implementations.dainNcnn;

            if (Interpolate.currentlyUsingAutoEnc)      // Ensure AutoEnc is not paused
                AutoEncode.paused = false;

            try
            {
                await RunDainNcnnProcess(framesPath, outPath, factor, mdl, tilesize);
                await NcnnUtils.DeleteNcnnDupes(outPath, factor);
            }
            catch (Exception e)
            {
                Logger.Log($"Error running {ai.FriendlyName}: {e.Message}");
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished(ai.NameShort);
        }

        public static async Task RunDainNcnnProcess(string framesPath, string outPath, float factor, string mdl, int tilesize)
        {
            string dainDir = Path.Combine(Paths.GetPkgPath(), Implementations.dainNcnn.PkgDir);
            Directory.CreateDirectory(outPath);
            Process dain = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(dain, 1500);
            SetProgressCheck(outPath, factor);
            int targetFrames = ((IoUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt());

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -n {targetFrames} -m {mdl.Lower()}" +
                $" -t {NcnnUtils.GetNcnnTilesize(tilesize)} -g {Config.Get(Config.Key.ncnnGpus)} -f {NcnnUtils.GetNcnnPattern()} -j 2:1:2";

            dain.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args}";
            Logger.Log("Running DAIN...", false);
            Logger.Log("cmd.exe " + dain.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                dain.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.dainNcnn); };
                dain.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.dainNcnn, true); };
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
            AI ai = Implementations.xvfiCuda;

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
                Logger.Log($"Error running {ai.FriendlyName}: {e.Message}");
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished(ai.NameShort);
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
            SetProgressCheck(Path.Combine(Interpolate.currentSettings.tempFolder, outDir), interpFactor);
            xvfiPy.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {pkgPath.Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running XVFI (CUDA)...", false);
            Logger.Log("cmd.exe " + xvfiPy.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                xvfiPy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, Implementations.xvfiCuda); };
                xvfiPy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.xvfiCuda, true); };
            }

            xvfiPy.Start();

            if (!OsUtils.ShowHiddenCmd())
            {
                xvfiPy.BeginOutputReadLine();
                xvfiPy.BeginErrorReadLine();
            }

            while (!xvfiPy.HasExited) await Task.Delay(1);
        }

        public static async Task RunIfrnetNcnn(string framesPath, string outPath, float factor, string mdl)
        {
            AI ai = Implementations.ifrnetNcnn;

            processTimeMulti.Restart();

            try
            {
                Logger.Log($"Running IFRNet (NCNN){(await InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);

                await RunIfrnetNcnnProcess(framesPath, factor, outPath, mdl);
                await NcnnUtils.DeleteNcnnDupes(outPath, factor);
            }
            catch (Exception e)
            {
                Logger.Log($"Error running {ai.FriendlyName}: {e.Message}");
                Logger.Log("Stack Trace: " + e.StackTrace, true);
            }

            await AiFinished(ai.NameShort);
        }

        static async Task RunIfrnetNcnnProcess(string inPath, float factor, string outPath, string mdl)
        {
            Directory.CreateDirectory(outPath);
            Process ifrnetNcnn = OsUtils.NewProcess(!OsUtils.ShowHiddenCmd());
            AiStarted(ifrnetNcnn, 1500, inPath);
            SetProgressCheck(outPath, factor);
            //int targetFrames = ((IoUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt()); // TODO: Maybe won't work with fractional factors ??
            //string frames = mdl.Contains("v4") ? $"-n {targetFrames}" : "";
            string uhdStr = ""; // await InterpolateUtils.UseUhd() ? "-u" : "";
            string ttaStr = ""; // Config.GetBool(Config.Key.rifeNcnnUseTta, false) ? "-x" : "";

            ifrnetNcnn.StartInfo.Arguments = $"{OsUtils.GetCmdArg()} cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.ifrnetNcnn.PkgDir).Wrap()} & ifrnet-ncnn-vulkan.exe " +
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} -m {mdl} {ttaStr} {uhdStr} -g {Config.Get(Config.Key.ncnnGpus)} -f {NcnnUtils.GetNcnnPattern()} -j {NcnnUtils.GetNcnnThreads(Implementations.ifrnetNcnn)}";

            Logger.Log("cmd.exe " + ifrnetNcnn.StartInfo.Arguments, true);

            if (!OsUtils.ShowHiddenCmd())
            {
                ifrnetNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.ifrnetNcnn); };
                ifrnetNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.ifrnetNcnn, true); };
            }

            ifrnetNcnn.Start();

            if (!OsUtils.ShowHiddenCmd())
            {
                ifrnetNcnn.BeginOutputReadLine();
                ifrnetNcnn.BeginErrorReadLine();
            }

            while (!ifrnetNcnn.HasExited) await Task.Delay(1);
        }

        static void LogOutput(string line, AI ai, bool err = false)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 6)
                return;

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            lastLogName = ai.LogFilename;
            Logger.Log(line, true, false, ai.LogFilename);

            string lastLogLines = string.Join("\n", Logger.GetSessionLogLastLines(lastLogName, 6).Select(x => $"[{x.Split("]: [").Skip(1).FirstOrDefault()}"));

            if (ai.Backend == AI.AiBackend.Pytorch) // Pytorch specific
            {
                if (line.Contains("ff:nocuda-cpu"))
                    Logger.Log("WARNING: CUDA-capable GPU device is not available, running on CPU instead!");

                if (!hasShownError && err && line.Lower().Contains("modulenotfounderror"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"A python module is missing.\nCheck {ai.LogFilename} for details.\n\n{line}", UiUtils.MessageType.Error);
                }

                if (!hasShownError && line.Lower().Contains("no longer supports this gpu"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"Your GPU seems to be outdated and is not supported!\n\n{line}", UiUtils.MessageType.Error);
                }

                if (!hasShownError && line.Lower().Contains("error(s) in loading state_dict"))
                {
                    hasShownError = true;
                    string msg = (Interpolate.currentSettings.ai.NameInternal == Implementations.flavrCuda.NameInternal) ? "\n\nFor FLAVR, you need to select the correct model for each scale!" : "";
                    UiUtils.ShowMessageBox($"Error loading the AI model!\n\n{line}{msg}", UiUtils.MessageType.Error);
                }

                if (!hasShownError && line.Lower().Contains("unicodeencodeerror"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"It looks like your path contains invalid characters - remove them and try again!\n\n{line}", UiUtils.MessageType.Error);
                }

                if (!hasShownError && err && (line.Contains("RuntimeError") || line.Contains("ImportError") || line.Contains("OSError")))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"A python error occured during interpolation!\nCheck the log for details:\n\n{lastLogLines}", UiUtils.MessageType.Error);
                }
            }

            if (ai.Backend == AI.AiBackend.Ncnn) // NCNN specific
            {
                if (!hasShownError && err && line.MatchesWildcard("vk*Instance* failed"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"Vulkan failed to start up!\n\n{line}\n\nThis most likely means your GPU is not compatible.", UiUtils.MessageType.Error);
                }

                if (!hasShownError && err && line.Contains("vkAllocateMemory failed"))
                {
                    hasShownError = true;
                    bool usingDain = (Interpolate.currentSettings.ai.NameInternal == Implementations.dainNcnn.NameInternal);
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
                    UiUtils.ShowMessageBox($"A Vulkan error occured during interpolation!\n\n{lastLogLines}", UiUtils.MessageType.Error);
                }
            }

            if (ai.Piped) // VS specific
            {
                if (!hasShownError && Interpolate.currentSettings.outSettings.Format != Enums.Output.Format.Realtime && line.Lower().Contains("fwrite() call failed"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"VapourSynth interpolation failed with an unknown error. Check the log for details:\n\n{lastLogLines}", UiUtils.MessageType.Error);
                }

                if (!hasShownError && line.Lower().Contains("allocate memory failed"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"Out of memory!\nTry reducing your RAM usage by closing some programs.\n\n{line}", UiUtils.MessageType.Error);
                }

                if (!hasShownError && line.Lower().Contains("vapoursynth.error:"))
                {
                    hasShownError = true;
                    UiUtils.ShowMessageBox($"VapourSynth Error:\n\n{line}", UiUtils.MessageType.Error);
                }
            }

            if (!hasShownError && err && line.Lower().Contains("out of memory"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"Your GPU ran out of VRAM! Please try a video with a lower resolution or use the Max Video Size option in the settings.\n\n{line}", UiUtils.MessageType.Error);
            }

            if (!hasShownError && line.Lower().Contains("illegal memory access"))
            {
                hasShownError = true;
                UiUtils.ShowMessageBox($"Your GPU appears to be unstable! If you have an overclock enabled, please disable it!\n\n{line}", UiUtils.MessageType.Error);
            }

            if (hasShownError)
                Interpolate.Cancel();

            InterpolationProgress.UpdateLastFrameFromInterpOutput(line);
        }


    }
}
