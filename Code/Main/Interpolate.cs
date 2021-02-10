using Flowframes;
using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
using Flowframes.MiscUtils;
using Flowframes.OS;
using Flowframes.UI;
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
            if (!Utils.InputIsValid(current.inPath, current.outPath, current.outFps, current.interpFactor, current.outMode)) return;     // General input checks
            if (!Utils.CheckAiAvailable(current.ai)) return;            // Check if selected AI pkg is installed
            if (!ResumeUtils.resumeNextRun && !Utils.CheckDeleteOldTempFolder()) return;      // Try to delete temp folder if an old one exists
            if (!Utils.CheckPathValid(current.inPath)) return;           // Check if input path/file is valid
            Utils.PathAsciiCheck(current.outPath, "output path");
            currentInputFrameCount = await Utils.GetInputFrameCountAsync(current.inPath);
            current.stepByStep = false;
            Program.mainForm.SetStatus("Starting...");
            Program.mainForm.SetWorking(true);

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
                await CreateVideo.Export(current.interpFolder, current.outFilename, current.outMode, false);
            await IOUtils.ReverseRenaming(current.framesFolder, AiProcess.filenameMap);   // Get timestamps back
            AiProcess.filenameMap.Clear();
            await Cleanup();
            Program.mainForm.SetWorking(false);
            Logger.Log("Total processing time: " + FormatUtils.Time(sw.Elapsed));
            sw.Stop();
            Program.mainForm.SetStatus("Done interpolating!");
        }

        public static async Task GetFrames (bool stepByStep = false)
        {
            current.RefreshAlpha();

            if (!current.inputIsFrames)        // Extract if input is video, import if image sequence
                await ExtractFrames(current.inPath, current.framesFolder, current.alpha, !stepByStep);
            else
                await FfmpegExtract.ImportImages(current.inPath, current.framesFolder, current.alpha, await Utils.GetOutputResolution(current.inPath, true));
        }

        public static async Task ExtractFrames(string inPath, string outPath, bool alpha, bool sceneDetect)
        {
            if (sceneDetect && Config.GetBool("scnDetect"))
            {
                Program.mainForm.SetStatus("Extracting scenes from video...");
                await FfmpegExtract.ExtractSceneChanges(inPath, Path.Combine(current.tempFolder, Paths.scenesDir), current.inFps);
                await Task.Delay(10);
            }

            if (canceled) return;
            Program.mainForm.SetStatus("Extracting frames from video...");
            bool mpdecimate = Config.GetInt("dedupMode") == 2;
            await FfmpegExtract.VideoToFrames(inPath, outPath, alpha, current.inFps, mpdecimate, false, await Utils.GetOutputResolution(inPath, true, true));

            if (mpdecimate)
            {
                int framesLeft = IOUtils.GetAmountOfFiles(outPath, false, $"*.png");
                int framesDeleted = currentInputFrameCount - framesLeft;
                float percentDeleted = ((float)framesDeleted / currentInputFrameCount) * 100f;
                string keptPercent = $"{(100f - percentDeleted).ToString("0.0")}%";
                Logger.Log($"[Deduplication] Kept {framesLeft} ({keptPercent}) frames, deleted {framesDeleted} frames.");
            }

            if(!Config.GetBool("allowConsecutiveSceneChanges", true))
                Utils.FixConsecutiveSceneFrames(Path.Combine(current.tempFolder, Paths.scenesDir), current.framesFolder);

            if (canceled) return;
            Program.mainForm.SetStatus("Extracting audio from video...");
            string audioFile = Path.Combine(current.tempFolder, "audio");

            if (audioFile != null && !File.Exists(audioFile))
                await FfmpegAudioAndMetadata.ExtractAudio(inPath, audioFile);

            if (canceled) return;
            Program.mainForm.SetStatus("Extracting subtitles from video...");
            await FfmpegAudioAndMetadata.ExtractSubtitles(inPath, current.tempFolder, current.outMode);
        }

        public static async Task PostProcessFrames (bool stepByStep)
        {
            if (canceled) return;

            int extractedFrames = IOUtils.GetAmountOfFiles(current.framesFolder, false, "*.png");
            if (!Directory.Exists(current.framesFolder) || currentInputFrameCount <= 0 || extractedFrames < 2)
            {
                if(extractedFrames == 1)
                    Cancel("Only a single frame was extracted from your input file!\n\nPossibly your input is an image, not a video?");
                else
                    Cancel("Frame extraction failed!\n\nYour input file might be incompatible.");
            }  

            if (Config.GetInt("dedupMode") == 1)
                await Dedupe.Run(current.framesFolder);
            else
                Dedupe.ClearCache();

            if(!Config.GetBool("enableLoop"))
                await Utils.CopyLastFrame(currentInputFrameCount);

            if (Config.GetInt("dedupMode") > 0)
                await Dedupe.CreateDupesFile(current.framesFolder, currentInputFrameCount);

            if (canceled) return;

            await FrameOrder.CreateFrameOrderFile(current.framesFolder, Config.GetBool("enableLoop"), current.interpFactor);

            if (canceled) return;

            try
            {
                Dictionary<string, string> renamedFilesDict = await IOUtils.RenameCounterDirReversibleAsync(current.framesFolder, "png", 1, Padding.inputFramesRenamed);
                
                if(stepByStep)
                    AiProcess.filenameMap = renamedFilesDict.ToDictionary(x => Path.GetFileName(x.Key), x => Path.GetFileName(x.Value));    // Save rel paths
            }
            catch (Exception e)
            {
                Logger.Log($"Error renaming frame files: {e.Message}");
                Cancel("Error renaming frame files. Check the log for details.");
            }

            if (current.alpha)
            {
                Program.mainForm.SetStatus("Extracting transparency...");
                Logger.Log("Extracting transparency... (1/2)");
                await FfmpegAlpha.ExtractAlphaDir(current.framesFolder, current.framesFolder + Paths.alphaSuffix);
                Logger.Log("Extracting transparency... (2/2)", false, true);
                await FfmpegAlpha.RemoveAlpha(current.framesFolder, current.framesFolder);
            }
        }

        public static async Task RunAi(string outpath, AI ai, bool stepByStep = false)
        {
            Program.mainForm.SetStatus("Downloading models...");
            await ModelDownloader.DownloadModelFiles(Path.GetFileNameWithoutExtension(ai.pkg.fileName), current.model);
            if (canceled) return;

            currentlyUsingAutoEnc = Utils.CanUseAutoEnc(stepByStep, current);

            IOUtils.CreateDir(outpath);

            List<Task> tasks = new List<Task>();

            if (ai.aiName == Networks.rifeCuda.aiName)
                tasks.Add(AiProcess.RunRifeCuda(current.framesFolder, current.interpFactor, current.model));

            if (ai.aiName == Networks.rifeNcnn.aiName)
                tasks.Add(AiProcess.RunRifeNcnn(current.framesFolder, outpath, (int)current.interpFactor, current.model));

            if (ai.aiName == Networks.dainNcnn.aiName)
                tasks.Add(AiProcess.RunDainNcnn(current.framesFolder, outpath, current.interpFactor, current.model, Config.GetInt("dainNcnnTilesize", 512)));

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
            if (AiProcess.currentAiProcess != null && !AiProcess.currentAiProcess.HasExited)
                OSUtils.KillProcessTree(AiProcess.currentAiProcess.Id);

            if (AvProcess.lastProcess != null && !AvProcess.lastProcess.HasExited)
                OSUtils.KillProcessTree(AvProcess.lastProcess.Id);

            canceled = true;
            Program.mainForm.SetStatus("Canceled.");
            Program.mainForm.SetProgress(0);

            if (!current.stepByStep && !Config.GetBool("keepTempFolder"))
            {
                if(false /* IOUtils.GetAmountOfFiles(Path.Combine(current.tempFolder, Paths.resumeDir), true) > 0 */)   // TODO: Uncomment for 1.23
                {
                    DialogResult dialogResult = MessageBox.Show($"Delete the temp folder (Yes) or keep it for resuming later (No)?", "Delete temporary files?", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                        IOUtils.TryDeleteIfExists(current.tempFolder);
                }
                else
                {
                    IOUtils.TryDeleteIfExists(current.tempFolder);
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
            if ((!ignoreKeepSetting && Config.GetBool("keepTempFolder")) || !Program.busy) return;
            if (!isRetry)
                Logger.Log("Deleting temporary files...");
            try
            {
                Directory.Delete(current.tempFolder, true);
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
