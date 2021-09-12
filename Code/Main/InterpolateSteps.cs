using Flowframes.Media;
using Flowframes.IO;
using System;
using System.IO;
using System.Threading.Tasks;
using Flowframes.MiscUtils;
using System.Windows.Forms;

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

            if(current == null)
            {
                Logger.Log($"[SBS] Getting new current settings", true);
                current = Program.mainForm.GetCurrentSettings();
            }
            else
            {
                Logger.Log($"[SBS] Updating current settings", true);
                current = Program.mainForm.UpdateCurrentSettings(current);
            }

            current.RefreshAlpha();
            current.stepByStep = true;

            if (!InterpolateUtils.InputIsValid(current.inPath, current.outPath, current.outFps, current.interpFactor, current.outMode)) return;     // General input checks
            if (!InterpolateUtils.CheckPathValid(current.inPath)) return;           // Check if input path/file is valid

            if (step.Contains("Extract Frames"))
                await ExtractFramesStep();

            if (step.Contains("Run Interpolation"))
                await InterpolateStep();

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
            if (!(await IoUtils.TryDeleteIfExistsAsync(current.framesFolder)))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }

            currentInputFrameCount = await GetFrameCountCached.GetFrameCountAsync(current.inPath);

            await GetFrames();
            await PostProcessFrames(true);
        }

        public static async Task InterpolateStep()
        {
            if (!InterpolateUtils.CheckAiAvailable(current.ai, current.model)) return;

            current.framesFolder = Path.Combine(current.tempFolder, Paths.framesDir);

            if (IoUtils.GetAmountOfFiles(current.framesFolder, false, "*") < 2)
            {
                if (Config.GetBool(Config.Key.sbsRunPreviousStepIfNeeded))
                {
                    Logger.Log($"There are no extracted frames to interpolate - Running extract step first...");
                    await ExtractFramesStep();
                }

                if (IoUtils.GetAmountOfFiles(current.framesFolder, false, "*") < 2)
                {
                    InterpolateUtils.ShowMessage("There are no extracted frames that can be interpolated!\nDid you run the extraction step?", "Error");
                    return;
                }
            }

            if (!(await IoUtils.TryDeleteIfExistsAsync(current.interpFolder)))
            {
                InterpolateUtils.ShowMessage("Failed to delete existing frames folder - Make sure no file is opened in another program!", "Error");
                return;
            }

            currentInputFrameCount = await GetFrameCountCached.GetFrameCountAsync(current.inPath);

            if (Config.GetBool(Config.Key.sbsAllowAutoEnc) && !(await InterpolateUtils.CheckEncoderValid())) return;

            if (canceled) return;
            Program.mainForm.SetStatus("Running AI...");
            await RunAi(current.interpFolder, current.ai, true);
            await Task.Run(async () => { await FrameRename.Unrename(); });   // Get timestamps back
            Program.mainForm.SetProgress(0);
        }

        public static async Task CreateOutputVid()
        {
            if (IoUtils.GetAmountOfFiles(current.interpFolder, false) < 2)
            {
                if (Config.GetBool(Config.Key.sbsRunPreviousStepIfNeeded))
                {
                    Logger.Log($"There are no interpolated frames to export - Running interpolation step first...");
                    await InterpolateStep();
                }

                if (IoUtils.GetAmountOfFiles(current.interpFolder, false) < 2)
                {
                    Cancel($"There are no interpolated frames to encode!\n\nDid you delete the folder?");
                    return;
                }
            }

            if (!(await InterpolateUtils.CheckEncoderValid())) return;

            string[] outFrames = IoUtils.GetFilesSorted(current.interpFolder, current.interpExt);

            if (outFrames.Length > 0 && !IoUtils.CheckImageValid(outFrames[0]))
            {
                InterpolateUtils.ShowMessage("Invalid frame files detected!\n\nIf you used Auto-Encode, this is normal, and you don't need to run " +
                    "this step as the video was already created in the \"Interpolate\" step.", "Error");
                return;
            }

            await Export.ExportFrames(current.interpFolder, current.outPath, current.outMode, true);
        }

        public static async Task Reset()
        {
            DialogResult dialog = MessageBox.Show($"Are you sure you want to remove all temporary files?", "Are you sure?", MessageBoxButtons.YesNo);

            if (dialog == DialogResult.Yes)
                await Cleanup(true);
        }
    }
}
