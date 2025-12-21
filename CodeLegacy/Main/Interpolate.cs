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
        public static bool currentlyUsingAutoEnc;
        public static InterpSettings currentSettings;
        public static MediaFile currentMediaFile;
        public static bool canceled = false;
        public static float InterpProgressMultiplier = 1f;
        private static Stopwatch sw = new Stopwatch();

        public static async Task Start()
        {
            if (!BatchProcessing.busy && Program.busy) return;
            canceled = false;
            Program.initialRun = false;
            Program.mainForm.Invoke(() => Program.mainForm.SetWorking(true));
            if (!Utils.InputIsValid(currentSettings)) return;     // General input checks
            if (!Utils.CheckPathValid(currentSettings.inPath)) return;           // Check if input path/file is valid
            if (!Utils.CheckAiAvailable(currentSettings.ai, currentSettings.model)) return;            // Check if selected AI pkg is installed
            if (!AutoEncodeResume.resumeNextRun && !Utils.CheckDeleteOldTempFolder()) return;      // Try to delete temp folder if an old one exists
            currentSettings.stepByStep = false;
            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Starting..."));
            sw.Restart();

            if (currentMediaFile.IsVfr)
            {
                TimestampUtils.CalcTimestamps(currentMediaFile, currentSettings);
            }

            if (!AutoEncodeResume.resumeNextRun && !(currentSettings.ai.Piped && !currentSettings.inputIsFrames /* && Config.GetInt(Config.Key.dedupMode) == 0) */))
            {
                await GetFrames();
                if (canceled) return;
                await PostProcessFrames(false);
            }

            currentSettings.RefreshAlpha(currentSettings.ai.Piped);
            if (canceled) return;
            bool skip = await AutoEncodeResume.PrepareResumedRun();
            if (skip || canceled) return;
            await RunAi(currentSettings.interpFolder, currentSettings.ai);
            if (canceled) return;
            Program.mainForm.Invoke(() => Program.mainForm.SetProgress(100));

            if (!currentlyUsingAutoEnc)
            {
                if (currentSettings.ai.Piped)
                {
                    if(!currentSettings.outSettings.Encoder.GetInfo().IsImageSequence)
                        await Export.MuxPipedVideo(currentSettings.inPath, currentSettings.FullOutPath);
                }
                else
                {
                    await Export.ExportFrames(currentSettings.interpFolder, currentSettings.outPath, currentSettings.outSettings, false);
                }
            }

            if (!AutoEncodeResume.resumeNextRun && Config.GetBool(Config.Key.keepTempFolder) && IoUtils.GetAmountOfFiles(currentSettings.framesFolder, false) > 0)
                await Task.Run(async () => { await FrameRename.Unrename(); });

            IoUtils.DeleteIfSmallerThanKb(currentSettings.FullOutPath);
            await Done();
        }

        public static async Task Done()
        {
            await Cleanup();
            Program.mainForm.Invoke(() => Program.mainForm.SetWorking(false));
            Logger.Log("Total processing time: " + FormatUtils.Time(sw.Elapsed));
            sw.Stop();

            if (!BatchProcessing.busy)
                OsUtils.ShowNotificationIfInBackground("Flowframes", $"Finished interpolation after {FormatUtils.Time(sw.Elapsed)}.");

            Program.mainForm.Invoke(() => Program.mainForm.InterpolationDone());
        }

        public static async Task Realtime ()
        {
            canceled = false;

            Program.mainForm.Invoke(() => Program.mainForm.SetWorking(true));

            if(currentSettings.ai.NameInternal != Implementations.rifeNcnnVs.NameInternal)
                Cancel($"Real-time interpolation is only available when using {Implementations.rifeNcnnVs.FriendlyName}.");

            if (canceled) return;

            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Downloading models..."));
            await ModelDownloader.DownloadModelFiles(currentSettings.ai, currentSettings.model.Dir);

            if (canceled) return;

            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Running real-time interpolation..."));
            await AiProcess.RunRifeNcnnVs(currentSettings.framesFolder, "", currentSettings.interpFactor, currentSettings.model.Dir, true);
            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Ready"));
            Program.mainForm.Invoke(() => Program.mainForm.SetWorking(false));
        }

        public static async Task GetFrames()
        {
            currentSettings.RefreshExtensions(InterpSettings.FrameType.Import);

            if (Config.GetBool(Config.Key.scnDetect) && !currentSettings.ai.Piped)
            {
                Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Extracting scenes from video..."));
                await FfmpegExtract.ExtractSceneChanges(currentSettings.inPath, Path.Combine(currentSettings.tempFolder, Paths.scenesDir), new Fraction(), currentSettings.inputIsFrames, currentSettings.framesExt);
            }

            if (!currentSettings.inputIsFrames)        // Extract if input is video, import if image sequence
                await ExtractFrames(currentSettings.inPath, currentSettings.framesFolder, currentSettings.alpha);
            else
                await FfmpegExtract.ImportImagesCheckCompat(currentSettings.inPath, currentSettings.framesFolder, currentSettings.alpha, currentSettings.OutputResolution, true, currentSettings.framesExt);
        }

        public static async Task ExtractFrames(string inPath, string outPath, bool alpha)
        {
            if (canceled) return;
            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Extracting frames from video..."));
            currentSettings.RefreshExtensions(InterpSettings.FrameType.Import);
            bool mpdecimate = Config.GetInt(Config.Key.dedupMode) == 2;
            Size res = await Utils.GetOutputResolution(FfmpegCommands.ModuloMode.ForEncoding, inPath, print: true);
            await FfmpegExtract.VideoToFrames(inPath, outPath, alpha, currentSettings.inFpsDetected, mpdecimate, false, res, currentSettings.framesExt);

            if (mpdecimate)
            {
                int framesLeft = IoUtils.GetAmountOfFiles(outPath, false, "*" + currentSettings.framesExt);
                int framesDeleted = currentMediaFile.FrameCount - framesLeft;
                float percentDeleted = ((float)framesDeleted / currentMediaFile.FrameCount) * 100f;
                string keptPercent = $"{(100f - percentDeleted).ToString("0.0")}%";

                if (framesDeleted > 0)
                {
                    if (QuickSettingsTab.trimEnabled)
                        Logger.Log($"Deduplication: Kept {framesLeft} frames.");
                    else
                        Logger.Log($"Deduplication: Kept {framesLeft} ({keptPercent}) frames, deleted {framesDeleted} frames.");
                }
            }

            if (!Config.GetBool("allowConsecutiveSceneChanges", true))
                Utils.FixConsecutiveSceneFrames(Path.Combine(currentSettings.tempFolder, Paths.scenesDir), currentSettings.framesFolder);
        }

        public static async Task PostProcessFrames(bool stepByStep)
        {
            if (canceled) return;

            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Processing frames..."));

            int extractedFrames = IoUtils.GetAmountOfFiles(currentSettings.framesFolder, false, "*" + currentSettings.framesExt);

            if (!Directory.Exists(currentSettings.framesFolder) || currentMediaFile.FrameCount <= 0 || extractedFrames < 2)
            {
                if (extractedFrames == 1)
                    Cancel("Only a single frame was extracted from your input file!\n\nPossibly your input is an image, not a video?");
                else
                    Cancel($"Frame extraction failed!\nExtracted {extractedFrames} frames - Frames Folder exists: {Directory.Exists(currentSettings.framesFolder)} - Current Frame Count = {currentMediaFile.FrameCount}.\n\nYour input file might be incompatible.");
            }

            if (Config.GetInt(Config.Key.dedupMode) == 1)
                await Dedupe.Run(currentSettings.framesFolder);

            if (!Config.GetBool(Config.Key.enableLoop))
            {
                // await Utils.CopyLastFrame(currentMediaFile.FrameCount);
            }
            else
            {
                FileInfo[] frameFiles = IoUtils.GetFileInfosSorted(currentSettings.framesFolder);
                string ext = frameFiles.First().Extension;
                int lastNum = frameFiles.Last().Name.GetInt() + 1;
                string loopFrameTargetPath = Path.Combine(currentSettings.framesFolder, lastNum.ToString().PadLeft(Padding.inputFrames, '0') + ext);
                File.Copy(frameFiles.First().FullName, loopFrameTargetPath, true);
                Logger.Log($"Copied loop frame to {loopFrameTargetPath}.", true);
            }
        }

        public static async Task RunAi(string outpath, AiInfo ai, bool stepByStep = false)
        {
            if (canceled) return;

            if (currentSettings.dedupe)
            {
                await Task.Run(async () => { await Dedupe.CreateDupesFile(currentSettings.framesFolder, currentSettings.framesExt); });
            }

            if (!ai.Piped || (ai.Piped && currentSettings.inputIsFrames))
            {
                await Task.Run(async () => { await FrameRename.Rename(); });
            }
            
            if (ai.Piped && currentSettings.dedupe)
            {
                string path = currentMediaFile.IsDirectory ? currentMediaFile.ImportPath : currentSettings.inPath;
                await Task.Run(async () => { await Dedupe.CreateFramesFileVideo(path, Config.GetBool(Config.Key.enableLoop)); });
            }

            if (!ai.Piped || (ai.Piped && currentSettings.dedupe))
                await Task.Run(async () => { await FrameOrder.CreateFrameOrderFile(currentSettings.tempFolder, Config.GetBool(Config.Key.enableLoop), currentSettings.interpFactor); });

            if (currentSettings.model.FixedFactors.Count() > 0 && (currentSettings.interpFactor != (int)currentSettings.interpFactor || !currentSettings.model.FixedFactors.Contains(currentSettings.interpFactor.RoundToInt())))
                Cancel($"The selected model does not support {currentSettings.interpFactor}x interpolation.\n\nSupported Factors: {currentSettings.model.GetFactorsString()}");

            if (canceled) return;

            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Downloading models..."));
            await ModelDownloader.DownloadModelFiles(ai, currentSettings.model.Dir);

            if (canceled) return;

            currentlyUsingAutoEnc = Utils.CanUseAutoEnc(stepByStep, currentSettings);
            IoUtils.CreateDir(outpath);

            List<Task> tasks = new List<Task>();

            if (ai.NameInternal == Implementations.rifeCuda.NameInternal)
                tasks.Add(AiProcess.RunRifeCuda(currentSettings.framesFolder, currentSettings.interpFactor, currentSettings.model.Dir));

            if (ai.NameInternal == Implementations.rifeNcnn.NameInternal)
                tasks.Add(AiProcess.RunRifeNcnn(currentSettings.framesFolder, outpath, currentSettings.interpFactor, currentSettings.model.Dir));

            if (ai.NameInternal == Implementations.rifeNcnnVs.NameInternal)
                tasks.Add(AiProcess.RunRifeNcnnVs(currentSettings.framesFolder, outpath, currentSettings.interpFactor, currentSettings.model.Dir));

            if (ai.NameInternal == Implementations.flavrCuda.NameInternal)
                tasks.Add(AiProcess.RunFlavrCuda(currentSettings.framesFolder, currentSettings.interpFactor, currentSettings.model.Dir));

            if (ai.NameInternal == Implementations.dainNcnn.NameInternal)
                tasks.Add(AiProcess.RunDainNcnn(currentSettings.framesFolder, outpath, currentSettings.interpFactor, currentSettings.model.Dir, Config.GetInt(Config.Key.dainNcnnTilesize)));

            if (ai.NameInternal == Implementations.xvfiCuda.NameInternal)
                tasks.Add(AiProcess.RunXvfiCuda(currentSettings.framesFolder, currentSettings.interpFactor, currentSettings.model.Dir));

            if(ai.NameInternal == Implementations.ifrnetNcnn.NameInternal)
                tasks.Add(AiProcess.RunIfrnetNcnn(currentSettings.framesFolder, outpath, currentSettings.interpFactor, currentSettings.model.Dir));

            if (currentlyUsingAutoEnc)
            {
                Logger.Log($"{Logger.GetLastLine()} (Using Auto-Encode)", true);
                tasks.Add(AutoEncode.MainLoop(outpath));
            }

            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Running AI..."));
            await Task.WhenAll(tasks);
        }

        public static void Cancel(string reason = "", bool noMsgBox = false)
        {
            if (currentSettings == null || canceled)
                return;

            canceled = true;

            Program.mainForm.Invoke(() => Program.mainForm.SetStatus("Canceled."));
            Program.mainForm.Invoke(() => Program.mainForm.SetProgress(0));
            AiProcess.Kill();
            AvProcess.Kill();

            if (!currentSettings.stepByStep && !Config.GetBool(Config.Key.keepTempFolder))
            {
                Task.Run(async () => { await IoUtils.TryDeleteIfExistsAsync(currentSettings.tempFolder); });

                // if (!BatchProcessing.busy && IoUtils.GetAmountOfFiles(Path.Combine(currentSettings.tempFolder, Paths.resumeDir), true) > 0)
                // {
                //     DialogResult dialogResult = UiUtils.ShowMessageBox($"Delete the temp folder (Yes) or keep it for resuming later (No)?", "Delete temporary files?", MessageBoxButtons.YesNo);
                // 
                //     if (dialogResult == DialogResult.Yes)
                //         Task.Run(async () => { await IoUtils.TryDeleteIfExistsAsync(currentSettings.tempFolder); });
                // }
                // else
                // {
                //     Task.Run(async () => { await IoUtils.TryDeleteIfExistsAsync(currentSettings.tempFolder); });
                // }
            }

            IoUtils.TryDeleteIfExists(currentSettings.FullOutPath); // Partial output file might exist

            AutoEncode.busy = false;
            Program.mainForm.Invoke(() => Program.mainForm.SetWorking(false));
            Program.mainForm.Invoke(() => Program.mainForm.SetTab(Program.mainForm.interpOptsTab.Name));
            Logger.LogIfLastLineDoesNotContainMsg("Canceled interpolation.");

            if(Cli.AutoRun)
                Application.Exit();

            if (!string.IsNullOrWhiteSpace(reason) && !noMsgBox)
                UiUtils.ShowMessageBox($"Canceled:\n\n{reason}");
        }

        public static async Task Cleanup(bool ignoreKeepSetting = false, int retriesLeft = 3, bool isRetry = false)
        {
            if ((!ignoreKeepSetting && Config.GetBool(Config.Key.keepTempFolder)) || !Program.busy) return;

            if (!isRetry)
                Logger.Log("Deleting temporary files...");

            try
            {
                await Task.Run(async () => { Directory.Delete(currentSettings.tempFolder, true); });
            }
            catch (Exception e)
            {
                Logger.Log("Cleanup Error: " + e.Message, true);
                if (retriesLeft > 0)
                {
                    await Task.Delay(1000);
                    await Cleanup(ignoreKeepSetting, retriesLeft - 1, true);
                }
            }
        }
    }
}
