using Flowframes;
using Flowframes.Data;
using Flowframes.FFmpeg;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
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
        public enum OutMode { VidMp4, VidGif, ImgPng }

        public static string currentTempDir;
        public static string currentFramesPath;
        public static int interpFactor;
        public static float currentInFps;
        public static float currentOutFps;
        public static OutMode currentOutMode;
        public static bool currentInputIsFrames;
        public static bool currentlyUsingAutoEnc;
        public static int lastInterpFactor;
        public static string lastInputPath;
        public static string nextOutPath;
        public static AI lastAi;

        public static bool canceled = false;

        static Stopwatch sw = new Stopwatch();


        public static void SetFps(float inFps)
        {
            currentInFps = inFps;
            currentOutFps = inFps * interpFactor;
        }

        public static async void Start(string inPath, string outDir, int tilesize, OutMode outMode, AI ai)
        {
            canceled = false;
            if (!Utils.InputIsValid(inPath, outDir, currentOutFps, interpFactor, tilesize)) return;     // General input checks
            if (!Utils.CheckAiAvailable(ai)) return;            // Check if selected AI pkg is installed
            lastInterpFactor = interpFactor;
            lastInputPath = inPath;
            currentTempDir = Utils.GetTempFolderLoc(inPath, outDir);
            currentFramesPath = Path.Combine(currentTempDir, Paths.framesDir);
            currentOutMode = outMode;
            if (!Utils.CheckDeleteOldTempFolder()) return;      // Try to delete temp folder if an old one exists
            if(!Utils.CheckPathValid(inPath)) return;           // Check if input path/file is valid
            Utils.PathAsciiCheck(inPath, outDir);
            lastAi = ai;
            currentInputIsFrames = IOUtils.IsPathDirectory(inPath);
            Program.mainForm.SetStatus("Starting...");
            Program.mainForm.SetWorking(true);
            await Task.Delay(10);
            if (!currentInputIsFrames)        // Input is video - extract frames first
                await ExtractFrames(inPath, currentFramesPath);
            else
                await FFmpegCommands.ImportImages(inPath, currentFramesPath);
            if (canceled) return;
            sw.Restart();
            await Task.Delay(10);
            await PostProcessFrames();
            if (canceled) return;
            string interpFramesDir = Path.Combine(currentTempDir, Paths.interpDir);
            nextOutPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(inPath) + IOUtils.GetAiSuffix(ai, interpFactor) + Utils.GetExt(outMode));
            int frames = IOUtils.GetAmountOfFiles(currentFramesPath, false, "*.png");
            int targetFrameCount = frames * interpFactor;
            GetProgressByFrameAmount(interpFramesDir, targetFrameCount);
            if (canceled) return;
            Program.mainForm.SetStatus("Running AI...");
            await RunAi(interpFramesDir, targetFrameCount, tilesize, ai);
            if (canceled) return;
            Program.mainForm.SetProgress(100);
            if(!currentlyUsingAutoEnc)
                await CreateVideo.Export(interpFramesDir, nextOutPath, outMode);
            IOUtils.ReverseRenaming(AiProcess.filenameMap, true);   // Get timestamps back
            Cleanup(interpFramesDir);
            Program.mainForm.SetWorking(false);
            Logger.Log("Total processing time: " + FormatUtils.Time(sw.Elapsed));
            sw.Stop();
            Program.mainForm.SetStatus("Done interpolating!");
        }

        public static async Task ExtractFrames(string inPath, string outPath, bool extractAudio = true)
        {
            await Task.Delay(10);
            if (Config.GetBool("scnDetect"))
            {
                Program.mainForm.SetStatus("Extracting scenes from video...");
                await FFmpegCommands.ExtractSceneChanges(inPath, Path.Combine(currentTempDir, Paths.scenesDir));
                await Task.Delay(10);
            }
            Program.mainForm.SetStatus("Extracting frames from video...");
            Size resolution = IOUtils.GetVideoRes(inPath);
            int maxHeight = Config.GetInt("maxVidHeight");
            if (resolution.Height > maxHeight)
            {
                float factor = (float)maxHeight / resolution.Height;
                int width = (resolution.Width * factor).RoundToInt();
                Logger.Log($"Video is bigger than the maximum - Downscaling to {width}x{maxHeight}.");
                await FFmpegCommands.VideoToFrames(inPath, outPath, Config.GetInt("dedupMode") == 2, false, new Size(width, maxHeight));
            }
            else
            {
                await FFmpegCommands.VideoToFrames(inPath, outPath, Config.GetInt("dedupMode") == 2, false);
            }
            /*
            if (AvProcess.lastOutputFfmpeg.ToLower().Contains("invalid"))
            {
                Cancel("Failed to read input video.");
                return;
            }
            */
            if (extractAudio)
            {
                string audioFile = Path.Combine(currentTempDir, "audio.m4a");
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
                Logger.Log("Copied loop frame.");
            }
        }

        public static async Task PostProcessFrames (bool sbsMode = false)
        {
            bool firstFrameFix = (!sbsMode && lastAi.aiName == Networks.rifeCuda.aiName) || (sbsMode && InterpolateSteps.currentAi.aiName == Networks.rifeCuda.aiName);
            firstFrameFix = false; // TODO: Remove firstframefix if new rife code works

            if (!Directory.Exists(currentFramesPath) || IOUtils.GetAmountOfFiles(currentFramesPath, false, "*.png") <= 0)
            {
                Cancel("Input frames folder is empty!");
            }

            if (Config.GetInt("dedupMode") == 1)
                await MagickDedupe.Run(currentFramesPath);
            else
                MagickDedupe.ClearCache();

            if (canceled) return;

            bool useTimestamps = Config.GetInt("timingMode") == 1;  // TODO: Auto-Disable timestamps if input frames are sequential, not timestamped

            if(sbsMode)
                await VfrDedupe.CreateTimecodeFiles(currentFramesPath, Config.GetBool("enableLoop"), firstFrameFix, -1, !useTimestamps);
            else
                await VfrDedupe.CreateTimecodeFiles(currentFramesPath, Config.GetBool("enableLoop"), firstFrameFix, lastInterpFactor, !useTimestamps);

            if (canceled) return;

            AiProcess.filenameMap = IOUtils.RenameCounterDirReversible(currentFramesPath, "png", 1, 8);

            //string hasPreprocessedFile = Path.Combine(currentTempDir, ".preprocessed");
            //if (File.Exists(hasPreprocessedFile)) return;

            if (firstFrameFix)
            {
                bool s = IOUtils.TryCopy(new DirectoryInfo(currentFramesPath).GetFiles("*.png")[0].FullName, Path.Combine(currentFramesPath, "00000000.png"), true);
                Logger.Log("FirstFrameFix TryCopy Success: " + s, true);
            }

            //File.Create(hasPreprocessedFile);
        }

        public static async Task RunAi(string outpath, int targetFrames, int tilesize, AI ai)
        {
            currentlyUsingAutoEnc = IOUtils.GetAmountOfFiles(currentFramesPath, false) * lastInterpFactor >= (AutoEncode.chunkSize + AutoEncode.safetyBufferFrames) * 1.2f;
            //Logger.Log("Using autoenc if there's more than " + (AutoEncode.chunkSize + AutoEncode.safetyBufferFrames) * 1.2f + " input frames, got " + IOUtils.GetAmountOfFiles(currentFramesPath, false) * lastInterpFactor);
            //currentlyUsingAutoEnc = false;

            Directory.CreateDirectory(outpath);

            List<Task> tasks = new List<Task>();

            if (ai.aiName == Networks.dainNcnn.aiName)
                tasks.Add(AiProcess.RunDainNcnn(currentFramesPath, outpath, targetFrames, tilesize));

            if (ai.aiName == Networks.cainNcnn.aiName)
                tasks.Add(AiProcess.RunCainNcnnMulti(currentFramesPath, outpath, tilesize, interpFactor));

            if (ai.aiName == Networks.rifeCuda.aiName)
                tasks.Add(AiProcess.RunRifeCuda(currentFramesPath, interpFactor));

            if (ai.aiName == Networks.rifeNcnn.aiName)
                tasks.Add(AiProcess.RunRifeNcnnMulti(currentFramesPath, outpath, tilesize, interpFactor));

            if(currentlyUsingAutoEnc)
                tasks.Add(AutoEncode.MainLoop(outpath));
            await Task.WhenAll(tasks);
        }

        public static async void GetProgressByFrameAmount(string outdir, int target)
        {
            bool firstProgUpd = true;
            Program.mainForm.SetProgress(0);
            while (Program.busy)
            {
                if (AiProcess.processTime.IsRunning && Directory.Exists(outdir))
                {
                    if (firstProgUpd && Program.mainForm.IsInFocus())
                        Program.mainForm.SetTab("preview");
                    firstProgUpd = false;
                    string[] frames = Directory.GetFiles(outdir, $"*.{Utils.lastExt}");
                    if (frames.Length > 1)
                        Utils.UpdateInterpProgress(frames.Length, target, frames[frames.Length - 1]);
                    await Task.Delay(Utils.GetProgressWaitTime(frames.Length));
                }
                else
                {
                    await Task.Delay(200);
                }
            }
            Program.mainForm.SetProgress(-1);
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
                IOUtils.TryDeleteIfExists(currentTempDir);
            Program.mainForm.SetWorking(false);
            Program.mainForm.SetTab("interpolation");
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
                    IOUtils.Copy(interpFramesDir, Path.Combine(currentTempDir.GetParentDir(), Path.GetFileName(currentTempDir).Replace("-temp", "-interpframes")));
                Directory.Delete(currentTempDir, true);
            }
            catch (Exception e)
            {
                Logger.Log("Cleanup Error: " + e.Message, true);
            }
        }
    }
}
