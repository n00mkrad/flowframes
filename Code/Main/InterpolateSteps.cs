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
using Flowframes.MiscUtils;

namespace Flowframes.Main
{
    using static Interpolate;

    class InterpolateSteps
    {

        public static async Task Run(string step)
        {
            Logger.Log($"[SBS] Running step '{step}'", true);
            canceled = false;
            Program.mainForm.SetWorking(true);
            current = Program.mainForm.GetCurrentSettings();
            current.RefreshAlpha();
            current.stepByStep = true;

            if (!InterpolateUtils.InputIsValid(current.inPath, current.outPath, current.outFps, current.interpFactor, current.outMode)) return;     // General input checks
            if (!InterpolateUtils.CheckPathValid(current.inPath)) return;           // Check if input path/file is valid

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

        public static async Task ExtractFramesStep()
        {
            // if (Config.GetBool("scnDetect") && !current.inputIsFrames)        // Input is video - extract frames first
            //     await ExtractSceneChanges();

            if (!IOUtils.TryDeleteIfExists(current.framesFolder))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }

            currentInputFrameCount = await GetFrameCountCached.GetFrameCountAsync(current.inPath);

            await GetFrames();
            await PostProcessFrames(true);
        }

        public static async Task DoInterpolate()
        {
            if (!InterpolateUtils.CheckAiAvailable(current.ai)) return;

            current.framesFolder = Path.Combine(current.tempFolder, Paths.framesDir);

            if (IOUtils.GetAmountOfFiles(current.framesFolder, false, "*" + current.framesExt) < 2)
            {
                InterpolateUtils.ShowMessage("There are no extracted frames that can be interpolated!\nDid you run the extraction step?", "Error");
                return;
            }
            if (!IOUtils.TryDeleteIfExists(current.interpFolder))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }

            currentInputFrameCount = await GetFrameCountCached.GetFrameCountAsync(current.inPath);

            if (Config.GetBool("sbsAllowAutoEnc") && !(await InterpolateUtils.CheckEncoderValid())) return;

            if (canceled) return;
            Program.mainForm.SetStatus("Running AI...");
            await RunAi(current.interpFolder, current.ai, true);
            await FrameRename.Unrename();   // Get timestamps back
            Program.mainForm.SetProgress(0);
        }

        public static async Task CreateOutputVid()
        {
            if (IOUtils.GetAmountOfFiles(current.interpFolder, false) < 2)
            {
                Cancel($"There are no interpolated frames to encode!\n\nDid you delete the folder?");
                return;
            }

            if (!(await InterpolateUtils.CheckEncoderValid())) return;

            string[] outFrames = IOUtils.GetFilesSorted(current.interpFolder, current.interpExt);

            if (outFrames.Length > 0 && !IOUtils.CheckImageValid(outFrames[0]))
            {
                InterpolateUtils.ShowMessage("Invalid frame files detected!\n\nIf you used Auto-Encode, this is normal, and you don't need to run " +
                    "this step as the video was already created in the \"Interpolate\" step.", "Error");
                return;
            }

            await CreateVideo.Export(current.interpFolder, current.outPath, current.outMode, true);
        }

        public static async Task Reset()
        {
            await Cleanup(true);
        }
    }
}
