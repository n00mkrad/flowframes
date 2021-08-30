using Flowframes;
using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
using Flowframes.MiscUtils;
using Flowframes.Os;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Padding = Flowframes.Data.Padding;
using Utils = Flowframes.Main.InterpolateUtils;

namespace Flowframes
{
    public class Interpolate
    {
        public enum OutMode { VidMp4, VidMkv, VidWebm, VidProRes, VidAvi, VidGif, ImgPng }

        public static int currentInputFrameCount;
        public static bool currentlyUsingAutoEnc;
        public static InterpSettings current;
        public static bool canceled = false;
        static Stopwatch sw = new Stopwatch();

        public static async Task Start()
        {
            if (!BatchProcessing.busy && Program.busy) return;
            canceled = false;
            Program.initialRun = false;
            Program.mainForm.SetWorking(true);
            if (!Utils.InputIsValid(current.inPath, current.outPath, current.outFps, current.interpFactor, current.outMode)) return;     // General input checks
            if (!Utils.CheckAiAvailable(current.ai)) return;            // Check if selected AI pkg is installed
            if (!ResumeUtils.resumeNextRun && !Utils.CheckDeleteOldTempFolder()) return;      // Try to delete temp folder if an old one exists
            if (!Utils.CheckPathValid(current.inPath)) return;           // Check if input path/file is valid
            if (!(await Utils.CheckEncoderValid())) return;           // Check NVENC compat
            Utils.ShowWarnings(current.interpFactor, current.ai);
            currentInputFrameCount = await GetFrameCountCached.GetFrameCountAsync(current.inPath);
            current.stepByStep = false;
            Program.mainForm.SetStatus("Starting...");

            if (!ResumeUtils.resumeNextRun)
            {
                await GetFrames();
                if (canceled) return;
                sw.Restart();
                await PostProcessFrames(false);
            }

            if (canceled) return;
            await ResumeUtils.PrepareResumedRun();
            //Task.Run(() => Utils.DeleteInterpolatedInputFrames());
            await RunAi(current.interpFolder, current.ai);
            if (canceled) return;
            Program.mainForm.SetProgress(100);

            if(!currentlyUsingAutoEnc)
                await CreateVideo.Export(current.interpFolder, current.outPath, current.outMode, false);

            if (Config.GetBool(Config.Key.keepTempFolder))
                await Task.Run(async () => { await FrameRename.Unrename(); });

            await Cleanup();
            Program.mainForm.SetWorking(false);
            Logger.Log("Total processing time: " + FormatUtils.Time(sw.Elapsed));
            sw.Stop();

            if(!BatchProcessing.busy)
                OsUtils.ShowNotificationIfInBackground("Flowframes", $"Finished interpolation after {FormatUtils.Time(sw.Elapsed)}.");
            
            Program.mainForm.InterpolationDone();
        }

        public static async Task<int> GetCurrentInputFrameCount ()
        {
            if (currentInputFrameCount < 2)
                currentInputFrameCount = await GetFrameCountCached.GetFrameCountAsync(current.inPath);

            return currentInputFrameCount;
        }

        public static async Task GetFrames ()
        {
            current.RefreshAlpha();
            current.RefreshExtensions(InterpSettings.FrameType.Import);

            if (Config.GetBool(Config.Key.scnDetect))
            {
                Program.mainForm.SetStatus("Extracting scenes from video...");
                await FfmpegExtract.ExtractSceneChanges(current.inPath, Path.Combine(current.tempFolder, Paths.scenesDir), current.inFpsDetected, current.inputIsFrames, current.framesExt);
            }

            if (!current.inputIsFrames)        // Extract if input is video, import if image sequence
                await ExtractFrames(current.inPath, current.framesFolder, current.alpha);
            else
                await FfmpegExtract.ImportImagesCheckCompat(current.inPath, current.framesFolder, current.alpha, (await current.GetScaledRes()), true, current.framesExt);
        }

        public static async Task ExtractFrames(string inPath, string outPath, bool alpha)
        {
            if (canceled) return;
            Program.mainForm.SetStatus("Extracting frames from video...");
            current.RefreshExtensions(InterpSettings.FrameType.Import);
            bool mpdecimate = Config.GetInt(Config.Key.dedupMode) == 2;
            Size res = await Utils.GetOutputResolution(inPath, true, true);
            await FfmpegExtract.VideoToFrames(inPath, outPath, alpha, current.inFpsDetected, mpdecimate, false, res, current.framesExt);

            if (mpdecimate)
            {
                int framesLeft = IoUtils.GetAmountOfFiles(outPath, false, "*" + current.framesExt);
                int framesDeleted = currentInputFrameCount - framesLeft;
                float percentDeleted = ((float)framesDeleted / currentInputFrameCount) * 100f;
                string keptPercent = $"{(100f - percentDeleted).ToString("0.0")}%";

                if(QuickSettingsTab.trimEnabled)
                    Logger.Log($"Deduplication: Kept {framesLeft} frames.");
                else
                    Logger.Log($"Deduplication: Kept {framesLeft} ({keptPercent}) frames, deleted {framesDeleted} frames.");
            }

            if(!Config.GetBool("allowConsecutiveSceneChanges", true))
                Utils.FixConsecutiveSceneFrames(Path.Combine(current.tempFolder, Paths.scenesDir), current.framesFolder);
        }

        public static async Task PostProcessFrames (bool stepByStep)
        {
            if (canceled) return;

            Program.mainForm.SetStatus("Processing frames...");

            int extractedFrames = IoUtils.GetAmountOfFiles(current.framesFolder, false, "*" + current.framesExt);
            if (!Directory.Exists(current.framesFolder) || currentInputFrameCount <= 0 || extractedFrames < 2)
            {
                if(extractedFrames == 1)
                    Cancel("Only a single frame was extracted from your input file!\n\nPossibly your input is an image, not a video?");
                else
                    Cancel("Frame extraction failed!\n\nYour input file might be incompatible.");
            }  

            if (Config.GetInt(Config.Key.dedupMode) == 1)
                await Dedupe.Run(current.framesFolder);
            else
                Dedupe.ClearCache();

            if (!Config.GetBool(Config.Key.enableLoop))
            {
                await Utils.CopyLastFrame(currentInputFrameCount);
            }
            else
            {
                FileInfo[] frameFiles = IoUtils.GetFileInfosSorted(current.framesFolder);
                string ext = frameFiles.First().Extension;
                int lastNum = frameFiles.Last().Name.GetInt() + 1;
                string loopFrameTargetPath = Path.Combine(current.framesFolder, lastNum.ToString().PadLeft(Padding.inputFrames, '0') + ext);
                File.Copy(frameFiles.First().FullName, loopFrameTargetPath, true);
                Logger.Log($"Copied loop frame to {loopFrameTargetPath}.", true);
            }
        }

        public static async Task RunAi(string outpath, AI ai, bool stepByStep = false)
        {
            if (canceled) return;

            await Task.Run(async () => { await Dedupe.CreateDupesFile(current.framesFolder, currentInputFrameCount, current.framesExt); });
            await Task.Run(async () => { await FrameRename.Rename(); });
            await Task.Run(async () => { await FrameOrder.CreateFrameOrderFile(current.framesFolder, Config.GetBool(Config.Key.enableLoop), current.interpFactor); });

            Program.mainForm.SetStatus("Downloading models...");
            await ModelDownloader.DownloadModelFiles(ai, current.model.dir);
            if (canceled) return;

            currentlyUsingAutoEnc = Utils.CanUseAutoEnc(stepByStep, current);

            IoUtils.CreateDir(outpath);

            List<Task> tasks = new List<Task>();

            if (ai.aiName == Implementations.rifeCuda.aiName)
                tasks.Add(AiProcess.RunRifeCuda(current.framesFolder, current.interpFactor, current.model.dir));

            if (ai.aiName == Implementations.rifeNcnn.aiName)
                tasks.Add(AiProcess.RunRifeNcnn(current.framesFolder, outpath, (int)current.interpFactor, current.model.dir));

            if (ai.aiName == Implementations.flavrCuda.aiName)
                tasks.Add(AiProcess.RunFlavrCuda(current.framesFolder, current.interpFactor, current.model.dir));

            if (ai.aiName == Implementations.dainNcnn.aiName)
                tasks.Add(AiProcess.RunDainNcnn(current.framesFolder, outpath, current.interpFactor, current.model.dir, Config.GetInt(Config.Key.dainNcnnTilesize, 512)));

            if (ai.aiName == Implementations.xvfiCuda.aiName)
                tasks.Add(AiProcess.RunXvfiCuda(current.framesFolder, current.interpFactor, current.model.dir));

            if (currentlyUsingAutoEnc)
            {
                Logger.Log($"{Logger.GetLastLine()} (Using Auto-Encode)", true);
                tasks.Add(AutoEncode.MainLoop(outpath));
            }

            Program.mainForm.SetStatus("Running AI...");
            await Task.WhenAll(tasks);
        }

        public static void Cancel(string reason = "", bool noMsgBox = false)
        {
            if (current == null)
                return;

            canceled = true;
            Program.mainForm.SetStatus("Canceled.");
            Program.mainForm.SetProgress(0);
            AiProcess.Kill();
            AvProcess.Kill();

            if (!current.stepByStep && !Config.GetBool(Config.Key.keepTempFolder))
            {
                if(false /* IOUtils.GetAmountOfFiles(Path.Combine(current.tempFolder, Paths.resumeDir), true) > 0 */)   // TODO: Uncomment for 1.23
                {
                    DialogResult dialogResult = MessageBox.Show($"Delete the temp folder (Yes) or keep it for resuming later (No)?", "Delete temporary files?", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                        Task.Run(async () => { await IoUtils.TryDeleteIfExistsAsync(current.tempFolder); });
                }
                else
                {
                    Task.Run(async () => { await IoUtils.TryDeleteIfExistsAsync(current.tempFolder); });
                }
            }

            AutoEncode.busy = false;
            Program.mainForm.SetWorking(false);
            Program.mainForm.SetTab("interpolation");
            Logger.LogIfLastLineDoesNotContainMsg("Canceled interpolation.");

            if (!string.IsNullOrWhiteSpace(reason) && !noMsgBox)
                Utils.ShowMessage($"Canceled:\n\n{reason}");
        }

        public static async Task Cleanup(bool ignoreKeepSetting = false, int retriesLeft = 3, bool isRetry = false)
        {
            if ((!ignoreKeepSetting && Config.GetBool(Config.Key.keepTempFolder)) || !Program.busy) return;
            if (!isRetry)
                Logger.Log("Deleting temporary files...");
            try
            {
                await Task.Run(async () => { Directory.Delete(current.tempFolder, true); });
            }
            catch (Exception e)
            {
                Logger.Log("Cleanup Error: " + e.Message, true);
                if(retriesLeft > 0)
                {
                    await Task.Delay(1000);
                    await Cleanup(ignoreKeepSetting, retriesLeft - 1, true);
                }
            }
        }
    }
}
