using Flowframes;
using Flowframes.Data;
using Flowframes.FFmpeg;
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
        public enum OutMode { VidMp4, VidWebm, VidProRes, VidAviRaw, VidGif, ImgPng }

        public static int currentInputFrameCount;
        public static bool currentlyUsingAutoEnc;

        public static InterpSettings current;

        public static bool canceled = false;

        static Stopwatch sw = new Stopwatch();

        public static async Task Start()
        {
            canceled = false;
            if (!Utils.InputIsValid(current.inPath, current.outPath, current.outFps, current.interpFactor, current.outMode)) return;     // General input checks
            if (!Utils.CheckAiAvailable(current.ai)) return;            // Check if selected AI pkg is installed
            if (!Utils.CheckDeleteOldTempFolder()) return;      // Try to delete temp folder if an old one exists
            if(!Utils.CheckPathValid(current.inPath)) return;           // Check if input path/file is valid
            Utils.PathAsciiCheck(current.inPath, current.outPath);
            currentInputFrameCount = await Utils.GetInputFrameCountAsync(current.inPath);
            Program.mainForm.SetStatus("Starting...");
            Program.mainForm.SetWorking(true);
            await Task.Delay(10);
            if (!current.inputIsFrames)        // Input is video - extract frames first
                await ExtractFrames(current.inPath, current.framesFolder);
            else
                await FFmpegCommands.ImportImages(current.inPath, current.framesFolder);
            if (canceled) return;
            sw.Restart();
            await Task.Delay(10);
            await PostProcessFrames();
            if (canceled) return;
            Program.mainForm.SetStatus("Running AI...");
            await RunAi(current.interpFolder, current.ai);
            if (canceled) return;
            Program.mainForm.SetProgress(100);
            if(!currentlyUsingAutoEnc)
                await CreateVideo.Export(current.interpFolder, current.outFilename, current.outMode);
            IOUtils.ReverseRenaming(AiProcess.filenameMap, true);   // Get timestamps back
            Cleanup(current.interpFolder);
            Program.mainForm.SetWorking(false);
            Logger.Log("Total processing time: " + FormatUtils.Time(sw.Elapsed));
            sw.Stop();
            Program.mainForm.SetStatus("Done interpolating!");
        }

        public static async Task ExtractFrames(string inPath, string outPath, bool allowSceneDetect = true, bool extractAudio = true)
        {
            if (Config.GetBool("scnDetect"))
            {
                Program.mainForm.SetStatus("Extracting scenes from video...");
                await FFmpegCommands.ExtractSceneChanges(inPath, Path.Combine(current.tempFolder, Paths.scenesDir), current.inFps);
                await Task.Delay(10);
            }

            Program.mainForm.SetStatus("Extracting frames from video...");
            bool mpdecimate = Config.GetInt("dedupMode") == 2;
            await FFmpegCommands.VideoToFrames(inPath, outPath, current.inFps, mpdecimate, false, Utils.GetOutputResolution(inPath, true), false);

            if (mpdecimate)
            {
                int framesLeft = IOUtils.GetAmountOfFiles(outPath, false, $"*.png");
                int framesDeleted = currentInputFrameCount - framesLeft;
                float percentDeleted = ((float)framesDeleted / currentInputFrameCount) * 100f;
                string keptPercent = $"{(100f - percentDeleted).ToString("0.0")}%";
                Logger.Log($"[Deduplication] Kept {framesLeft} ({keptPercent}) frames, deleted {framesDeleted} frames.");
            }

            Utils.FixConsecutiveSceneFrames(Path.Combine(current.tempFolder, Paths.scenesDir), current.framesFolder);

            if (extractAudio)
            {
                string audioFile = Path.Combine(current.tempFolder, "audio");
                if (audioFile != null && !File.Exists(audioFile))
                    await FFmpegCommands.ExtractAudio(inPath, audioFile);
            }

            if (!canceled && Config.GetBool("enableLoop") && Config.GetInt("timingMode") != 1)
            {
                string lastFrame = IOUtils.GetHighestFrameNumPath(outPath);
                int newNum = Path.GetFileName(lastFrame).GetInt() + 1;
                string newFilename = Path.Combine(lastFrame.GetParentDir(), newNum.ToString().PadLeft(Padding.inputFrames, '0') + ".png");
                string firstFrame = new DirectoryInfo(outPath).GetFiles("*.png")[0].FullName;
                File.Copy(firstFrame, newFilename);
            }
        }

        public static async Task PostProcessFrames (bool sbsMode = false)
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

            if (Config.GetInt("dedupMode") == 2 || Config.GetInt("dedupMode") == 1)
                await Dedupe.CreateDupesFile(current.framesFolder, currentInputFrameCount);

                if (canceled) return;

            bool useTimestamps = Config.GetInt("timingMode") == 1;  // TODO: Auto-Disable timestamps if input frames are sequential, not timestamped
            await FrameTiming.CreateTimecodeFiles(current.framesFolder, FrameTiming.Mode.CFR, Config.GetBool("enableLoop"), current.interpFactor, !useTimestamps);

            if (canceled) return;

            AiProcess.filenameMap = IOUtils.RenameCounterDirReversible(current.framesFolder, "png", 1, 8);
        }

        public static async Task RunAi(string outpath, AI ai, bool stepByStep = false)
        {
            await ModelDownloader.DownloadModelFiles(Path.GetFileNameWithoutExtension(ai.pkg.fileName), current.model);
            if (canceled) return;

            currentlyUsingAutoEnc = Utils.UseAutoEnc(stepByStep, current);

            IOUtils.CreateDir(outpath);

            List<Task> tasks = new List<Task>();

            if (ai.aiName == Networks.rifeCuda.aiName)
                tasks.Add(AiProcess.RunRifeCuda(current.framesFolder, current.interpFactor, current.model));

            if (ai.aiName == Networks.rifeNcnn.aiName)
                tasks.Add(AiProcess.RunRifeNcnnMulti(current.framesFolder, outpath, current.interpFactor, current.model));

            if (currentlyUsingAutoEnc)
            {
                Logger.Log($"{Logger.GetLastLine()} (Using Auto-Encode)", true);
                tasks.Add(AutoEncode.MainLoop(outpath));
            }

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
            if (Config.GetInt("processingMode") == 0 && !Config.GetBool("keepTempFolder"))
                IOUtils.TryDeleteIfExists(current.tempFolder);
            AutoEncode.busy = false;
            Program.mainForm.SetWorking(false);
            Program.mainForm.SetTab("interpolation");
            if(!Logger.GetLastLine().Contains("Canceled interpolation."))
                Logger.Log("Canceled interpolation.");
            if (!string.IsNullOrWhiteSpace(reason) && !noMsgBox)
                Utils.ShowMessage($"Canceled:\n\n{reason}");
        }

        public static void Cleanup(string interpFramesDir, bool ignoreKeepSetting = false)
        {
            if (!ignoreKeepSetting && Config.GetBool("keepTempFolder")) return;
            Logger.Log("Deleting temporary files...");
            try
            {
                if (Config.GetBool("keepFrames"))
                    IOUtils.Copy(interpFramesDir, Path.Combine(current.tempFolder.GetParentDir(), Path.GetFileName(current.tempFolder).Replace("-temp", "-interpframes")));
                Directory.Delete(current.tempFolder, true);
            }
            catch (Exception e)
            {
                Logger.Log("Cleanup Error: " + e.Message, true);
            }
        }
    }
}
