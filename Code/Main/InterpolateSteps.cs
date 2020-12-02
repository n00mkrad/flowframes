using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    using static Interpolate;

    class InterpolateSteps
    {
        public enum Step { ExtractScnChanges, ExtractFrames, Interpolate, CreateVid, Reset }

        public static string currentInPath;
        public static string currentOutPath;
        public static string currentInterpFramesDir;
        public static AI currentAi;
        public static OutMode currentOutMode;

        public static async Task Run(string step)
        {
            canceled = false;
            Program.mainForm.SetWorking(true);
            InitState();

            if (step.Contains("Extract Scene Changes"))
            {
                if (!currentInputIsFrames)        // Input is video - extract frames first
                    await ExtractSceneChanges();
                else
                    InterpolateUtils.ShowMessage("Scene changes can only be extracted from videos, not frames!", "Error");
            }

            if (step.Contains("Extract Video Frames"))
            {
                if (!currentInputIsFrames)        // Input is video - extract frames first
                    await ExtractVideoFrames();
                else
                    await FFmpegCommands.ImportImages(currentInPath, currentFramesPath);
            }

            if (step.Contains("Run Interpolation"))
                await DoInterpolate();

            if (step.Contains("Create Output Video"))
                await CreateOutputVid();

            if (step.Contains("Cleanup"))
                await Reset();

            Program.mainForm.SetWorking(false);
            Logger.Log("Done running this step.");
        }

        static void InitState ()
        {
            BatchEntry e = Program.mainForm.GetBatchEntry();
            interpFactor = e.interpFactor;
            currentAi = e.ai;

            if (string.IsNullOrWhiteSpace(currentInPath))
                currentInPath = e.inPath;

            if (string.IsNullOrWhiteSpace(currentOutPath))
                currentOutPath = e.outPath;

            if (string.IsNullOrWhiteSpace(currentTempDir))
                currentTempDir = InterpolateUtils.GetTempFolderLoc(currentInPath, currentOutPath);

            if (string.IsNullOrWhiteSpace(currentFramesPath))
                currentFramesPath = Path.Combine(currentTempDir, Paths.framesDir);

            currentInterpFramesDir = Path.Combine(currentTempDir, Paths.interpDir);

            currentInputIsFrames = IOUtils.IsPathDirectory(currentInPath);
        }

        public static async Task ExtractSceneChanges ()
        {
            string scenesPath = Path.Combine(currentTempDir, Paths.scenesDir);
            if (!IOUtils.TryDeleteIfExists(scenesPath))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing scenes folder - Make sure no file is opened in another program!", "Error");
                return;
            }
            Program.mainForm.SetStatus("Extracting scenes from video...");
            await FFmpegCommands.ExtractSceneChanges(currentInPath, scenesPath);
            await Task.Delay(10);
        }

        public static async Task ExtractVideoFrames ()
        {
            currentFramesPath = Path.Combine(currentTempDir, Paths.framesDir);
            if (!IOUtils.TryDeleteIfExists(currentFramesPath))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }
            bool extractAudio = true;
            Program.mainForm.SetStatus("Extracting frames from video...");
            Size resolution = IOUtils.GetVideoRes(currentInPath);
            int maxHeight = Config.GetInt("maxVidHeight");
            if (resolution.Height > maxHeight)
            {
                float factor = (float)maxHeight / resolution.Height;
                int width = (resolution.Width * factor).RoundToInt();
                Logger.Log($"Video is bigger than the maximum - Downscaling to {width}x{maxHeight}.");
                await FFmpegCommands.VideoToFrames(currentInPath, currentFramesPath, Config.GetInt("dedupMode") == 2, false, new Size(width, maxHeight));
            }
            else
            {
                await FFmpegCommands.VideoToFrames(currentInPath, currentFramesPath, Config.GetInt("dedupMode") == 2, false);
            }
            if (extractAudio)
            {
                string audioFile = Path.Combine(currentTempDir, "audio.m4a");
                if (audioFile != null && !File.Exists(audioFile))
                    await FFmpegCommands.ExtractAudio(currentInPath, audioFile);
            }
            if (!canceled && Config.GetBool("enableLoop") && Config.GetInt("timingMode") != 1)
            {
                string lastFrame = IOUtils.GetHighestFrameNumPath(currentOutPath);
                int newNum = Path.GetFileName(lastFrame).GetInt() + 1;
                string newFilename = Path.Combine(lastFrame.GetParentDir(), newNum.ToString().PadLeft(8, '0') + ".png");
                string firstFrame = new DirectoryInfo(currentOutPath).GetFiles("*.png")[0].FullName;
                File.Copy(firstFrame, newFilename);
                Logger.Log("Copied loop frame.");
            }
        }

        public static async Task DoInterpolate ()
        {
            currentFramesPath = Path.Combine(currentTempDir, Paths.framesDir);
            if (!Directory.Exists(currentFramesPath))
            {
                InterpolateUtils.ShowMessage("There are no extracted frames that can be interpolated!\nDid you run the extraction step?", "Error");
                return;
            }
            if (!IOUtils.TryDeleteIfExists(currentInterpFramesDir))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }

            foreach (string ini in Directory.GetFiles(currentTempDir, "*.ini", SearchOption.TopDirectoryOnly))
                IOUtils.TryDeleteIfExists(ini);

            IOUtils.ReverseRenaming(AiProcess.filenameMap, true);   // Get timestamps back
            await PostProcessFrames(true);

            lastInterpFactor = interpFactor;
            int frames = IOUtils.GetAmountOfFiles(currentFramesPath, false, "*.png");
            int targetFrameCount = frames * lastInterpFactor;
            GetProgressByFrameAmount(currentInterpFramesDir, targetFrameCount);
            if (canceled) return;
            Program.mainForm.SetStatus("Running AI...");
            int tilesize = currentAi.supportsTiling ? Config.GetInt($"tilesize_{currentAi.aiName}") : 512;
            await RunAi(currentInterpFramesDir, targetFrameCount, tilesize, currentAi);
            Program.mainForm.SetProgress(0);
        }

        public static async Task CreateOutputVid ()
        {
            currentOutMode = Program.mainForm.GetBatchEntry().outMode;
            string outPath = Path.Combine(currentOutPath, Path.GetFileNameWithoutExtension(currentInPath) + IOUtils.GetAiSuffix(currentAi, lastInterpFactor) + InterpolateUtils.GetExt(currentOutMode));
            await CreateVideo.Export(currentInterpFramesDir, outPath, currentOutMode);
        }

        public static async Task Reset ()
        {
            Cleanup(currentInterpFramesDir, true);
        }
    }
}
