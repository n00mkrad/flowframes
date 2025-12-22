using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using Flowframes.Utilities;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Paths = Flowframes.IO.Paths;

namespace Flowframes.Os
{
    class AiProcess
    {
        public static bool hasShownError;
        public static string logName;
        public static Process lastAiProcess;
        public static Stopwatch processTime = new Stopwatch();
        public static Stopwatch processTimeMulti = new Stopwatch();

        public static int lastStartupTimeMs = 1000;
        private static string lastInPath;
        private static string NcnnGpuIds => Config.Get(Config.Key.ncnnGpus).Trim();

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
                Logger.Log($"Failed to kill process tree: {e.Message}", true);
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
                lastAiProcess = null;
                return;
            }

            int interpFramesFiles = IoUtils.GetAmountOfFiles(Interpolate.currentSettings.interpFolder, false, "*" + Interpolate.currentSettings.interpExt);
            int interpFramesCount = interpFramesFiles + InterpolationProgress.deletedFramesCount;

            if (!Interpolate.currentSettings.ai.Piped)
                InterpolationProgress.UpdateInterpProgress(interpFramesCount, InterpolationProgress.targetFrames);

            string logStr = $"Done running {aiName} - Interpolation took {FormatUtils.Time(processTime.Elapsed)}.";

            if (InterpolationProgress.LastFps > 0.0001)
            {
                logStr += $" Output FPS: {InterpolationProgress.LastFps.ToString("0.0")}";
            }

            if (Interpolate.currentlyUsingAutoEnc && AutoEncode.HasWorkToDo())
            {
                logStr += " - Waiting for encoding to finish...";
                Program.mainForm.SetStatus("Creating output video from frames...");
            }

            if (Interpolate.currentSettings.outSettings.Format != Enums.Output.Format.Realtime)
                Logger.Log(logStr, replaceLastLine: Logger.LastUiLine.Contains("FPS"));

            processTime.Stop();

            if (!Interpolate.currentSettings.ai.Piped && interpFramesCount < 3)
            {
                string amount = interpFramesCount > 0 ? $"Only {interpFramesCount}" : "No";

                if (logName.IsEmpty())
                {
                    Interpolate.Cancel($"Interpolation failed - {amount} interpolated frames were created, and no log was written.");
                    return;
                }

                string[] logLines = File.ReadAllLines(Path.Combine(Paths.GetLogPath(), logName + ".txt"));
                string log = string.Join("\n", logLines.Reverse().Take(10).Reverse().Select(x => x.Split("]: ").Last()).ToList());
                Interpolate.Cancel($"Interpolation failed - {amount} interpolated frames were created.\n\n\nLast 10 log lines:\n{log}\n\nCheck the log '{logName}' for more details.");
                return;
            }

            try
            {
                while (Interpolate.currentlyUsingAutoEnc && Program.busy)
                {
                    if (AvProcess.lastAvProcess != null && !AvProcess.lastAvProcess.HasExited)
                    {
                        if (Logger.LastLogLine.Contains("frame: "))
                            Logger.Log(FormatUtils.BeautifyFfmpegStats(Logger.LastLogLine), false, Logger.LastUiLine.Contains("frame"));
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

            lastAiProcess = null;
        }

        public static async Task RunRifeCuda(string framesPath, float interpFactor, string mdl)
        {
            AiInfo ai = Implementations.rifeCuda;

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
            string uhdStr = InterpolateUtils.UseUhd() ? "--UHD" : "";
            string wthreads = $"--wthreads {2 * (int)interpFactor}";
            string rbuffer = $"--rbuffer {Config.GetInt(Config.Key.rifeCudaBufferSize, 200)}";
            //string scale = $"--scale {Config.GetFloat("rifeCudaScale", 1.0f).ToString()}";
            string prec = Config.GetBool(Config.Key.rifeCudaFp16) ? "--fp16" : "";
            string args = $" --input {inPath.Wrap()} --output {outDir} --model {mdl} --multi {interpFactor} {uhdStr} {wthreads} {rbuffer} {prec}";

            Process rifePy = OsUtils.NewProcess(true);
            AiStarted(rifePy, 3500);
            SetProgressCheck(Path.Combine(Interpolate.currentSettings.tempFolder, outDir), interpFactor);
            rifePy.StartInfo.Arguments = $"/C cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.rifeCuda.PkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running RIFE (CUDA){(InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);
            Logger.Log("cmd.exe " + rifePy.StartInfo.Arguments, true);

            rifePy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, Implementations.rifeCuda); };
            rifePy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.rifeCuda, true); };
            rifePy.Start();
            rifePy.BeginOutputReadLine();
            rifePy.BeginErrorReadLine();

            while (!rifePy.HasExited) await Task.Delay(1);
        }

        public static async Task RunFlavrCuda(string framesPath, float interpFactor, string mdl)
        {
            AiInfo ai = Implementations.flavrCuda;

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

            Process flavrPy = OsUtils.NewProcess(true);
            AiStarted(flavrPy, 4000);
            SetProgressCheck(Path.Combine(Interpolate.currentSettings.tempFolder, outDir), interpFactor);
            flavrPy.StartInfo.Arguments = $"/C cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.flavrCuda.PkgDir).Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running FLAVR (CUDA)...", false);
            Logger.Log("cmd.exe " + flavrPy.StartInfo.Arguments, true);

            flavrPy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, Implementations.flavrCuda); };
            flavrPy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.flavrCuda, true); };
            flavrPy.Start();
            flavrPy.BeginOutputReadLine();
            flavrPy.BeginErrorReadLine();

            while (!flavrPy.HasExited) await Task.Delay(1);
        }

        public static async Task RunRifeNcnn(string framesPath, string outPath, float factor, string mdl)
        {
            AiInfo ai = Implementations.rifeNcnn;
            processTimeMulti.Restart();

            try
            {
                Logger.Log($"Running RIFE (NCNN){(InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);

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
            Process rifeNcnn = OsUtils.NewProcess(true);
            AiStarted(rifeNcnn, 1500, inPath);
            SetProgressCheck(outPath, factor);
            int targetFrames = ((IoUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt()); // TODO: Maybe won't work with fractional factors ??

            string frames = mdl.Contains("v4") ? $"-n {targetFrames}" : "";
            string uhdStr = InterpolateUtils.UseUhd() ? "-u" : "";
            string ttaStr = Config.GetBool(Config.Key.rifeNcnnUseTta, false) ? "-x" : "";

            rifeNcnn.StartInfo.Arguments = $"/C cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnn.PkgDir).Wrap()} & rife-ncnn-vulkan.exe " +
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} {frames} -m {mdl.Lower()} {ttaStr} {uhdStr} -g {NcnnGpuIds} -f {NcnnUtils.GetNcnnPattern()} -j {NcnnUtils.GetNcnnThreads(Implementations.rifeNcnn)}";

            Logger.Log("cmd.exe " + rifeNcnn.StartInfo.Arguments, true);

            rifeNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.rifeNcnn); };
            rifeNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.rifeNcnn, true); };
            rifeNcnn.Start();
            rifeNcnn.BeginOutputReadLine();
            rifeNcnn.BeginErrorReadLine();

            while (!rifeNcnn.HasExited) await Task.Delay(1);
        }

        public static async Task RunRifeNcnnVs(string framesPath, string outPath, float factor, string mdl, bool rt = false)
        {
            if (Interpolate.canceled) return;

            AiInfo ai = Implementations.rifeNcnnVs;
            processTimeMulti.Restart();

            try
            {
                Logger.Log($"Running RIFE (NCNN-VS){(InterpolateUtils.UseUhd(Interpolate.currentSettings.OutputResolution) ? " (UHD Mode)" : "")}...", false);
                await RunRifeNcnnVsProcess(framesPath, factor, outPath, mdl, Interpolate.currentSettings.OutputResolution, rt);
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
            Process rifeNcnnVs = OsUtils.NewProcess(true);
            string avDir = Path.Combine(Paths.GetPkgPath(), Paths.audioVideoDir);
            string pkgDir = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir);
            int gpuId = NcnnGpuIds.Split(',')[0].GetInt();

            var vsSettings = new VapourSynthUtils.VsSettings()
            {
                InterpSettings = Interpolate.currentSettings,
                Alpha = Interpolate.currentSettings.alpha,
                ModelDir = mdl,
                Factor = factor,
                Res = res,
                Uhd = InterpolateUtils.UseUhd(res),
                GpuId = gpuId,
                GpuThreads = NcnnUtils.GetRifeNcnnGpuThreads(res, gpuId, Implementations.rifeNcnnVs),
                SceneDetectSensitivity = Config.GetBool(Config.Key.scnDetect) ? Config.GetFloat(Config.Key.scnDetectValue) * 0.7f : 0f,
                Loop = Config.GetBool(Config.Key.enableLoop),
                MatchDuration = Config.GetBool(Config.Key.fixOutputDuration),
                Dedupe = Interpolate.currentSettings.dedupe,
                Realtime = rt,
                Osd = Config.GetBool(Config.Key.vsRtShowOsd),
            };

            if (rt)
            {
                Logger.ClearLogBox();
                Logger.Log($"Starting. Use Space to pause, Left Arrow and Right Arrow to seek, though seeking can be slow.");
                AiStartedRt(rifeNcnnVs, inPath);
            }
            else
            {
                SetProgressCheck(Interpolate.currentMediaFile.FrameCount, factor, Implementations.rifeNcnnVs.LogFilename);
                AiStarted(rifeNcnnVs, 1000, inPath);
            }

            IoUtils.TryDeleteIfExists(Path.Combine(Interpolate.currentSettings.tempFolder, "alpha.mkv"));
            string vspipe = $"vspipe rife.py {VapourSynthUtils.GetVsPipeArgs(vsSettings)}";
            string ffmpeg = $"{Path.Combine(avDir, "ffmpeg").Wrap()} -loglevel warning -stats -y";
            string baseArgs = $"/C cd /D {pkgDir.Wrap()}";

            if (vsSettings.Alpha)
            {
                rifeNcnnVs.StartInfo.Arguments = $"{baseArgs} && {vspipe} --arg alpha=\"True\" -c y4m - | {ffmpeg} {await Export.GetPipedFfmpegCmd(alpha: Export.AlphaMode.AlphaOut)} && {vspipe} -c y4m - | {ffmpeg} {await Export.GetPipedFfmpegCmd(alpha: Export.AlphaMode.AlphaIn)}";
            }
            else
            {
                rifeNcnnVs.StartInfo.Arguments = $"{baseArgs} && {vspipe} -c y4m - | {ffmpeg} {await Export.GetPipedFfmpegCmd(rt)}";
            }

            Logger.Log($"cmd.exe {rifeNcnnVs.StartInfo.Arguments}", true);
            rifeNcnnVs.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.rifeNcnnVs); };
            rifeNcnnVs.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.rifeNcnnVs, true); };
            rifeNcnnVs.Start();
            rifeNcnnVs.BeginOutputReadLine();
            rifeNcnnVs.BeginErrorReadLine();
            while (!rifeNcnnVs.HasExited) await Task.Delay(1);
        }

        public static async Task RunDainNcnn(string framesPath, string outPath, float factor, string mdl, int tilesize)
        {
            AiInfo ai = Implementations.dainNcnn;

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
            Process dain = OsUtils.NewProcess(true);
            AiStarted(dain, 1500);
            SetProgressCheck(outPath, factor);
            int targetFrames = ((IoUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt());

            string args = $" -v -i {framesPath.Wrap()} -o {outPath.Wrap()} -n {targetFrames} -m {mdl.Lower()}" +
                $" -t {NcnnUtils.GetNcnnTilesize(tilesize)} -g {NcnnGpuIds} -f {NcnnUtils.GetNcnnPattern()} -j 2:1:2";

            dain.StartInfo.Arguments = $"/C cd /D {dainDir.Wrap()} & dain-ncnn-vulkan.exe {args}";
            Logger.Log("Running DAIN...", false);
            Logger.Log("cmd.exe " + dain.StartInfo.Arguments, true);

            dain.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.dainNcnn); };
            dain.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.dainNcnn, true); };
            dain.Start();
            dain.BeginOutputReadLine();
            dain.BeginErrorReadLine();

            while (!dain.HasExited)
                await Task.Delay(100);
        }

        public static async Task RunXvfiCuda(string framesPath, float interpFactor, string mdl)
        {
            AiInfo ai = Implementations.xvfiCuda;

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

            Process xvfiPy = OsUtils.NewProcess(true);
            AiStarted(xvfiPy, 3500);
            SetProgressCheck(Path.Combine(Interpolate.currentSettings.tempFolder, outDir), interpFactor);
            xvfiPy.StartInfo.Arguments = $"/C cd /D {pkgPath.Wrap()} & " +
                $"set CUDA_VISIBLE_DEVICES={Config.Get(Config.Key.torchGpus)} & {Python.GetPyCmd()} {script} {args}";
            Logger.Log($"Running XVFI (CUDA)...", false);
            Logger.Log("cmd.exe " + xvfiPy.StartInfo.Arguments, true);

            xvfiPy.OutputDataReceived += (sender, outLine) => { LogOutput(outLine.Data, Implementations.xvfiCuda); };
            xvfiPy.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.xvfiCuda, true); };
            xvfiPy.Start();
            xvfiPy.BeginOutputReadLine();
            xvfiPy.BeginErrorReadLine();

            while (!xvfiPy.HasExited) await Task.Delay(1);
        }

        public static async Task RunIfrnetNcnn(string framesPath, string outPath, float factor, string mdl)
        {
            AiInfo ai = Implementations.ifrnetNcnn;

            processTimeMulti.Restart();

            try
            {
                Logger.Log($"Running IFRNet (NCNN){(InterpolateUtils.UseUhd() ? " (UHD Mode)" : "")}...", false);

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
            Process ifrnetNcnn = OsUtils.NewProcess(true);
            AiStarted(ifrnetNcnn, 1500, inPath);
            SetProgressCheck(outPath, factor);
            //int targetFrames = ((IoUtils.GetAmountOfFiles(lastInPath, false, "*.*") * factor).RoundToInt()); // TODO: Maybe won't work with fractional factors ??
            //string frames = mdl.Contains("v4") ? $"-n {targetFrames}" : "";
            string uhdStr = ""; // InterpolateUtils.UseUhd() ? "-u" : "";
            string ttaStr = ""; // Config.GetBool(Config.Key.rifeNcnnUseTta, false) ? "-x" : "";

            ifrnetNcnn.StartInfo.Arguments = $"/C cd /D {Path.Combine(Paths.GetPkgPath(), Implementations.ifrnetNcnn.PkgDir).Wrap()} & ifrnet-ncnn-vulkan.exe " +
                $" -v -i {inPath.Wrap()} -o {outPath.Wrap()} -m {mdl} {ttaStr} {uhdStr} -g {NcnnGpuIds} -f {NcnnUtils.GetNcnnPattern()} -j {NcnnUtils.GetNcnnThreads(Implementations.ifrnetNcnn)}";

            Logger.Log("cmd.exe " + ifrnetNcnn.StartInfo.Arguments, true);

            ifrnetNcnn.OutputDataReceived += (sender, outLine) => { LogOutput("[O] " + outLine.Data, Implementations.ifrnetNcnn); };
            ifrnetNcnn.ErrorDataReceived += (sender, outLine) => { LogOutput("[E] " + outLine.Data, Implementations.ifrnetNcnn, true); };
            ifrnetNcnn.Start();
            ifrnetNcnn.BeginOutputReadLine();
            ifrnetNcnn.BeginErrorReadLine();

            while (!ifrnetNcnn.HasExited) await Task.Delay(1);
        }

        private static readonly Regex FfmpegLogMemAddr = new Regex(@" @\s*(?:0x)?(?<addr>[0-9A-Fa-f]{8,16})(?=\])", RegexOptions.Compiled);

        private static string GetLastLogLines(string logName, int lineCount = 6, bool beautify = true)
        {
            var lll = Logger.GetSessionLogLastLines(logName, lineCount);

            if (!beautify)
                return string.Join("\n", lll);

            var beautified = lll.Select(l => $"[{string.Join(" ", l.Split(' ').Skip(2))}".Replace("]: [E]", "]").Replace("]: [O]", "]"));
            beautified = beautified.Select(l => FfmpegLogMemAddr.Replace(l, ""));
            return string.Join("\n", beautified);
        }

        private static void LogOutput(string line, AiInfo ai, bool err = false)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 6)
                return;

            logName = ai.LogFilename;
            Logger.Log(line, true, false, ai.LogFilename, toConsole: Cli.Verbose);
            void ShowErrorBox(string msg) => UiUtils.ShowMessageBox(msg, UiUtils.MessageType.Error, monospace: true);
            string lineLow = line.Lower();

            if (ai.Backend == AiInfo.AiBackend.Pytorch) // Pytorch specific
            {
                if (line.Contains("ff:nocuda-cpu"))
                    Logger.Log("WARNING: CUDA-capable GPU device is not available, running on CPU instead!");

                if (!hasShownError && err && line.Contains("ModuleNotFoundError"))
                {
                    hasShownError = true;
                    ShowErrorBox($"A python module is missing.\nCheck {ai.LogFilename} for details.\n\n{line}");
                }

                if (!hasShownError && lineLow.Contains("no longer supports this gpu"))
                {
                    hasShownError = true;
                    ShowErrorBox($"Your GPU seems to be outdated and is not supported!\n\n{line}");
                }

                if (!hasShownError && lineLow.Contains("error(s) in loading state_dict"))
                {
                    hasShownError = true;
                    string msg = (Interpolate.currentSettings.ai.NameInternal == Implementations.flavrCuda.NameInternal) ? "\n\nFor FLAVR, you need to select the correct model for each scale!" : "";
                    ShowErrorBox($"Error loading the AI model!\n\n{line}{msg}");
                }

                if (!hasShownError && line.Contains("UnicodeEncodeError"))
                {
                    hasShownError = true;
                    ShowErrorBox($"It looks like your path contains invalid characters - remove them and try again!\n\n{line}");
                }

                if (!hasShownError && err && (line.Contains("RuntimeError") || line.Contains("ImportError") || line.Contains("OSError")))
                {
                    hasShownError = true;
                    ShowErrorBox($"A python error occured during interpolation!\nCheck the log for details:\n\n{GetLastLogLines(logName)}");
                }
            }

            if (ai.Backend == AiInfo.AiBackend.Ncnn) // NCNN specific
            {
                if (!hasShownError && err && line.Contains("vkCreateInstance failed"))
                {
                    hasShownError = true;
                    ShowErrorBox($"Vulkan failed to start up!\n\n{line}\n\nThis most likely means your GPU is not compatible.");
                }

                if (!hasShownError && err && line.Contains("vkAllocateMemory failed"))
                {
                    hasShownError = true;
                    bool usingDain = (Interpolate.currentSettings.ai.NameInternal == Implementations.dainNcnn.NameInternal);
                    string msg = usingDain ? "\n\nTry reducing the tile size in the AI settings." : "\n\nTry a lower resolution (Settings -> Max Video Size).";
                    ShowErrorBox($"Vulkan ran out of memory!\n\n{line}{msg}");
                }

                if (!hasShownError && err && line.Contains("invalid gpu device"))
                {
                    hasShownError = true;
                    ShowErrorBox($"A Vulkan error occured during interpolation!\n\n{line}\n\nAre your GPU IDs set correctly?");
                }

                if (!hasShownError && err && line.Contains(" failed") && line.Contains("vk"))
                {
                    hasShownError = true;
                    ShowErrorBox($"A Vulkan error occured during interpolation!\n\n{GetLastLogLines(logName)}");
                }
            }

            if (ai.Piped) // VS specific
            {
                if (!hasShownError && Interpolate.currentSettings.outSettings.Format != Enums.Output.Format.Realtime && (line.Contains("Task finished with error code") || line.Contains("fwrite() call failed")))
                {
                    hasShownError = true;
                    ShowErrorBox($"VapourSynth interpolation failed with an unknown error. Check the log for details:\n\n{GetLastLogLines(logName)}");
                }

                if (!hasShownError && lineLow.Contains("allocate memory failed"))
                {
                    hasShownError = true;
                    ShowErrorBox($"Out of memory!\nTry reducing your RAM usage by closing some programs.\n\n{line}");
                }

                if (!hasShownError && line.Contains("vapoursynth.Error:"))
                {
                    hasShownError = true;
                    ShowErrorBox($"VapourSynth Error:\n\n{line}");
                }
            }

            if (!hasShownError && err && lineLow.Contains("out of memory"))
            {
                hasShownError = true;
                ShowErrorBox($"Your GPU ran out of VRAM! Please try a video with a lower resolution or use the Max Video Size option in the settings.\n\n{line}");
            }

            if (!hasShownError && lineLow.Contains("illegal memory access"))
            {
                hasShownError = true;
                ShowErrorBox($"Your GPU appears to be unstable! If you have an overclock enabled, please disable it!\n\n{line}");
            }

            if (hasShownError)
                Interpolate.Cancel();

            InterpolationProgress.UpdateLastFrameFromInterpOutput(line);
        }


    }
}
