using Flowframes.Media;
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

        public static async Task Run(string step)
        {
            Logger.Log($"[SBS] Running step '{step}'", true);
            canceled = false;
            Program.mainForm.SetWorking(true);
            current = Program.mainForm.GetCurrentSettings();
            current.RefreshAlpha();
            current.stepByStep = true;

            if (!InterpolateUtils.InputIsValid(current.inPath, current.outPath, current.outFps, current.interpFactor, current.outMode)) return;     // General input checks

            if (step.Contains("Extract Scene Changes"))
            {
                if (!current.inputIsFrames)        // Input is video - extract frames first
                    await ExtractSceneChanges();
                else
                    InterpolateUtils.ShowMessage("Scene changes can only be extracted from videos, not frames!", "Error");
            }

            if (step.Contains("Extract Frames"))
                await ExtractFramesStep();

            if (step.Contains("Run Interpolation"))
                await DoInterpolate();

            if (step.Contains("Export"))
                await CreateOutputVid();

            if (step.Contains("Cleanup"))
                await Reset();

            Program.mainForm.SetWorking(false);
            Program.mainForm.SetStatus("Done running step.");
            Logger.Log("Done running this step.");
        }

        public static async Task ExtractSceneChanges()
        {
            string scenesPath = Path.Combine(current.tempFolder, Paths.scenesDir);
            if (!IOUtils.TryDeleteIfExists(scenesPath))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing scenes folder - Make sure no file is opened in another program!", "Error");
                return;
            }
            Program.mainForm.SetStatus("Extracting scenes from video...");
            await FfmpegExtract.ExtractSceneChanges(current.inPath, scenesPath, current.inFps);
            await Task.Delay(10);
        }

        public static async Task ExtractFramesStep()
        {
            if (!IOUtils.TryDeleteIfExists(current.framesFolder))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }

            currentInputFrameCount = await InterpolateUtils.GetInputFrameCountAsync(current.inPath);
            AiProcess.filenameMap.Clear();

            await GetFrames(true);
        }

        public static async Task DoInterpolate()
        {
            current.framesFolder = Path.Combine(current.tempFolder, Paths.framesDir);

            if (!Directory.Exists(current.framesFolder) || IOUtils.GetAmountOfFiles(current.framesFolder, false, "*.png") < 2)
            {
                InterpolateUtils.ShowMessage("There are no extracted frames that can be interpolated!\nDid you run the extraction step?", "Error");
                return;
            }
            if (!IOUtils.TryDeleteIfExists(current.interpFolder))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }

            currentInputFrameCount = await InterpolateUtils.GetInputFrameCountAsync(current.inPath);

            // TODO: Check if this works lol, remove if it does
            //if (Config.GetBool("sbsAllowAutoEnc"))
            //    nextOutPath = Path.Combine(currentOutPath, Path.GetFileNameWithoutExtension(current.inPath) + IOUtils.GetAiSuffix(current.ai, current.interpFactor) + InterpolateUtils.GetExt(current.outMode));

            await PostProcessFrames(true);

            if (canceled) return;
            Program.mainForm.SetStatus("Running AI...");
            await RunAi(current.interpFolder, current.ai, true);
            await IOUtils.ReverseRenaming(current.framesFolder, AiProcess.filenameMap);   // Get timestamps back
            AiProcess.filenameMap.Clear();
            Program.mainForm.SetProgress(0);
        }

        public static async Task CreateOutputVid()
        {
            if (!Directory.Exists(current.interpFolder) || IOUtils.GetAmountOfFiles(current.interpFolder, false) < 2)
            {
                Cancel($"There are no interpolated frames to encode!\n\nDid you delete the folder?");
                return;
            }

            string[] outFrames = IOUtils.GetFilesSorted(current.interpFolder, $"*.{InterpolateUtils.GetOutExt()}");

            if (outFrames.Length > 0 && !IOUtils.CheckImageValid(outFrames[0]))
            {
                InterpolateUtils.ShowMessage("Invalid frame files detected!\n\nIf you used Auto-Encode, this is normal, and you don't need to run " +
                    "this step as the video was already created in the \"Interpolate\" step.", "Error");
                return;
            }

            string outPath = Path.Combine(current.outPath, Path.GetFileNameWithoutExtension(current.inPath) + IOUtils.GetCurrentExportSuffix() + FFmpegUtils.GetExt(current.outMode));
            await CreateVideo.Export(current.interpFolder, outPath, current.outMode, true);
        }

        public static async Task Reset()
        {
            await Cleanup(true);
        }
    }
}
