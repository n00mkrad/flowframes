using Flowframes.Media;
using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Os;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using I = Flowframes.Interpolate;
using Padding = Flowframes.Data.Padding;

namespace Flowframes.Main
{
    class InterpolateUtils
    {
        public static async Task CopyLastFrame(int lastFrameNum)
        {
            if (I.canceled) return;

            try
            {
                lastFrameNum--; // We have to do this as extracted frames start at 0, not 1
                bool frameFolderInput = IoUtils.IsPathDirectory(I.currentSettings.inPath);
                string targetPath = Path.Combine(I.currentSettings.framesFolder, lastFrameNum.ToString().PadLeft(Padding.inputFrames, '0') + I.currentSettings.framesExt);
                if (File.Exists(targetPath)) return;

                Size res = IoUtils.GetImage(IoUtils.GetFilesSorted(I.currentSettings.framesFolder, false).First()).Size;

                if (frameFolderInput)
                {
                    string lastFramePath = IoUtils.GetFilesSorted(I.currentSettings.inPath, false).Last();
                    await FfmpegExtract.ExtractLastFrame(lastFramePath, targetPath, res);
                }
                else
                {
                    await FfmpegExtract.ExtractLastFrame(I.currentSettings.inPath, targetPath, res);
                }
            }
            catch (Exception e)
            {
                Logger.Log("CopyLastFrame Error: " + e.Message);
            }
        }

        public static int GetProgressWaitTime(int numFrames)
        {
            float hddMultiplier = !Program.lastInputPathIsSsd ? 2f : 1f;

            int waitMs = 200;

            if (numFrames > 100)
                waitMs = 500;

            if (numFrames > 1000)
                waitMs = 1000;

            if (numFrames > 2500)
                waitMs = 1500;

            if (numFrames > 5000)
                waitMs = 2500;

            return (waitMs * hddMultiplier).RoundToInt();
        }

        public static string GetTempFolderLoc(string inPath, string outPath)
        {
            string basePath = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "Temp", "Flowframes");
            int tempFolderLoc = Config.GetInt(Config.Key.tempFolderLoc);

            switch (tempFolderLoc)
            {
                case 1:
                    basePath = inPath.GetParentDir();
                    break;

                case 2:
                    basePath = outPath;
                    break;

                case 3:
                    basePath = Paths.GetSessionDataPath();
                    break;

                case 4:
                    string custPath = Config.Get(Config.Key.tempDirCustom);
                    if (IoUtils.IsDirValid(custPath))
                    {
                        basePath = custPath;
                    }
                    break;
            }

            string folderName = Path.GetFileNameWithoutExtension(inPath).StripBadChars().Remove(" ").Trunc(30, false) + ".tmp";
            return Path.Combine(basePath, folderName);
        }

        public static bool InputIsValid(InterpSettings s)
        {
            try
            {
                bool passes = true;
                bool isFile = !IoUtils.IsPathDirectory(s.inPath);

                if ((passes && isFile && !IoUtils.IsFileValid(s.inPath)) || (!isFile && !IoUtils.IsDirValid(s.inPath)))
                {
                    UiUtils.ShowMessageBox("Input path is not valid!");
                    passes = false;
                }

                if (passes && !IoUtils.IsDirValid(s.outPath))
                {
                    UiUtils.ShowMessageBox("Output path is not valid!");
                    passes = false;
                }

                if (passes && s.tempFolder.StartsWith(@"\\"))
                {
                    UiUtils.ShowMessageBox("Flowframes does not support UNC/Network paths as a temp folder!\nPlease use a local path instead.");
                    passes = false;
                }

                string fpsLimitValue = Config.Get(Config.Key.maxFps);
                float fpsLimit = (fpsLimitValue.Contains("/") ? new Fraction(fpsLimitValue).GetFloat() : fpsLimitValue.GetFloat());
                int maxFps = s.outSettings.Encoder.GetInfo().MaxFramerate;

                if (passes && s.outFps.GetFloat() < 1f || (s.outFps.GetFloat() > maxFps && !(fpsLimit > 0 && fpsLimit <= maxFps)))
                {
                    string imgSeqNote = isFile ? "" : "\n\nWhen using an image sequence as input, you always have to specify the frame rate manually.";
                    UiUtils.ShowMessageBox($"Invalid output frame rate ({s.outFps.GetFloat()}).\nMust be 1-{maxFps}. Either lower the interpolation factor or use the \"Maximum Output Frame Rate\" option.{imgSeqNote}");
                    passes = false;
                }

                float fpsLimitFloat = fpsLimitValue.GetFloat();

                if (fpsLimitFloat > 0 && fpsLimitFloat < s.outFps.GetFloat())
                    Interpolate.InterpProgressMultiplier = s.outFps.GetFloat() / fpsLimitFloat;
                else
                    Interpolate.InterpProgressMultiplier = 1f;

                if (!passes)
                    I.Cancel("Invalid settings detected.", true);

                return passes;
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to run InputIsValid: {e.Message}\n{e.StackTrace}", true);
                return false;
            }
        }

        public static bool CheckAiAvailable(AI ai, ModelCollection.ModelInfo model)
        {
            if (IoUtils.GetAmountOfFiles(Path.Combine(Paths.GetPkgPath(), ai.PkgDir), true) < 1)
            {
                UiUtils.ShowMessageBox("The selected AI is not installed!", UiUtils.MessageType.Error);
                I.Cancel("Selected AI not available.", true);
                return false;
            }

            if (model == null || model.Dir.Trim() == "")
            {
                UiUtils.ShowMessageBox("No valid AI model has been selected!", UiUtils.MessageType.Error);
                I.Cancel("No valid model selected.", true);
                return false;
            }

            if (I.currentSettings.ai.NameInternal.Upper().Contains("CUDA") && NvApi.gpuList.Count < 1)
            {
                UiUtils.ShowMessageBox("Warning: No Nvidia GPU was detected. CUDA might fall back to CPU!\n\nTry an NCNN implementation instead if you don't have an Nvidia GPU.", UiUtils.MessageType.Error);

                if (!Config.GetBool("allowCudaWithoutDetectedGpu", true))
                {
                    I.Cancel("No CUDA-capable graphics card available.", true);
                    return false;
                }
            }

            return true;
        }

        public static bool CheckDeleteOldTempFolder()
        {
            if (!IoUtils.TryDeleteIfExists(I.currentSettings.tempFolder))
            {
                UiUtils.ShowMessageBox("Failed to remove an existing temp folder of this video!\nMake sure you didn't open any frames in an editor.", UiUtils.MessageType.Error);
                I.Cancel();
                return false;
            }
            return true;
        }

        public static void ShowWarnings(float factor, AI ai)
        {
            if (Config.GetInt(Config.Key.cmdDebugMode) > 0)
                Logger.Log($"Warning: The CMD window for interpolation is enabled. This will disable Auto-Encode and the progress bar!");
        }

        public static bool CheckPathValid(string path)
        {
            if (path.StartsWith(@"\\"))
            {
                UiUtils.ShowMessageBox("Input path is not valid.\nFlowframes does not support UNC/Network paths.");
                I.Cancel();
                return false;
            }

            if (IoUtils.IsPathDirectory(path))
            {
                if (!IoUtils.IsDirValid(path))
                {
                    UiUtils.ShowMessageBox("Input directory is not valid.\nMake sure it still exists and hasn't been renamed or moved!");
                    I.Cancel();
                    return false;
                }
            }
            else
            {
                if (!IsVideoValid(path))
                {
                    UiUtils.ShowMessageBox("Input video file is not valid.\nMake sure it still exists and hasn't been renamed or moved!");
                    return false;
                }
            }
            return true;
        }

        public static async Task<bool> CheckEncoderValid()
        {
            string enc = I.currentSettings.outSettings.Encoder.GetInfo().Name;

            if (enc.Lower().Contains("nvenc") && !(await FfmpegCommands.IsEncoderCompatible(enc)))
            {
                UiUtils.ShowMessageBox("NVENC encoding is not available on your hardware!\nPlease use a different encoder.", UiUtils.MessageType.Error);
                I.Cancel();
                return false;
            }

            return true;
        }

        public static bool IsVideoValid(string videoPath)
        {
            if (videoPath == null || !IoUtils.IsFileValid(videoPath))
                return false;
            return true;
        }

        public static async Task<Size> GetOutputResolution(string inputPath, bool pad, bool print = false)
        {
            Size resolution = await GetMediaResolutionCached.GetSizeAsync(inputPath);
            return GetOutputResolution(resolution, pad, print);
        }

        public static Size GetOutputResolution(Size inputRes, bool pad, bool print = false)
        {
            Size res = new Size(inputRes.Width, inputRes.Height);
            int maxHeight = Config.GetInt(Config.Key.maxVidHeight);
            int mod = pad ? FfmpegCommands.GetModulo() : 1;
            float factor = res.Height > maxHeight ? (float)maxHeight / res.Height : 1f; // Calculate downscale factor if bigger than max, otherwise just use 1x
            Logger.Log($"Un-rounded downscaled size: {(res.Width * factor).ToString("0.###")}x{(res.Height * factor).ToString("0.###")}", true);
            int width = RoundDivisibleBy((res.Width * factor).RoundToInt(), mod);
            int height = RoundDivisibleBy((res.Height * factor).RoundToInt(), mod);
            res = new Size(width, height);

            if (print && factor < 1f)
                Logger.Log($"Video is bigger than the maximum - Downscaling to {width}x{height}.");

            if (res != inputRes)
                Logger.Log($"Scaled {inputRes.Width}x{inputRes.Height} to {res.Width}x{res.Height}", true);

            return res;
        }

        public static int RoundDivisibleBy(int number, int divisibleBy)     // Round to a number that's divisible by 2 (for h264 etc)
        {
            int a = (number / divisibleBy) * divisibleBy;    // Smaller multiple
            int b = a + divisibleBy;   // Larger multiple
            return (number - a > b - number) ? b : a; // Return of closest of two
        }

        public static bool CanUseAutoEnc(bool stepByStep, InterpSettings current)
        {
            AutoEncode.UpdateChunkAndBufferSizes();

            if (current.ai.Piped)
            {
                Logger.Log($"Not Using AutoEnc: Using piped encoding.", true);
                return false;
            }

            if (Config.GetInt(Config.Key.cmdDebugMode) > 0)
            {
                Logger.Log($"Not Using AutoEnc: CMD window is shown (cmdDebugMode > 0)", true);
                return false;
            }

            if (current.outSettings.Format == Enums.Output.Format.Gif)
            {
                Logger.Log($"Not Using AutoEnc: Using GIF output", true);
                return false;
            }

            if (stepByStep && !Config.GetBool(Config.Key.sbsAllowAutoEnc))
            {
                Logger.Log($"Not Using AutoEnc: Using step-by-step mode, but 'sbsAllowAutoEnc' is false", true);
                return false;
            }

            if (!stepByStep && Config.GetInt(Config.Key.autoEncMode) == 0)
            {
                Logger.Log($"Not Using AutoEnc: 'autoEncMode' is 0", true);
                return false;
            }

            int inFrames = IoUtils.GetAmountOfFiles(current.framesFolder, false);
            if (inFrames * current.interpFactor < (AutoEncode.chunkSize + AutoEncode.safetyBufferFrames) * 1.2f)
            {
                Logger.Log($"Not Using AutoEnc: Input frames ({inFrames}) * factor ({current.interpFactor}) is smaller than (chunkSize ({AutoEncode.chunkSize}) + safetyBufferFrames ({AutoEncode.safetyBufferFrames}) * 1.2f)", true);
                return false;
            }

            return true;
        }

        public static async Task<bool> UseUhd()
        {
            return UseUhd(await GetOutputResolution(I.currentSettings.inPath, false));
        }

        public static bool UseUhd(Size outputRes)
        {
            return outputRes.Height >= Config.GetInt(Config.Key.uhdThresh);
        }

        public static void FixConsecutiveSceneFrames(string sceneFramesPath, string sourceFramesPath)
        {
            if (!Directory.Exists(sceneFramesPath) || IoUtils.GetAmountOfFiles(sceneFramesPath, false) < 1)
                return;

            List<string> sceneFrames = IoUtils.GetFilesSorted(sceneFramesPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
            List<string> sourceFrames = IoUtils.GetFilesSorted(sourceFramesPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
            List<string> sceneFramesToDelete = new List<string>();

            foreach (string scnFrame in sceneFrames)
            {
                if (sceneFramesToDelete.Contains(scnFrame))
                    continue;

                int sourceIndexForScnFrame = sourceFrames.IndexOf(scnFrame);            // Get source index of scene frame
                if ((sourceIndexForScnFrame + 1) == sourceFrames.Count)
                    continue;
                string followingFrame = sourceFrames[sourceIndexForScnFrame + 1];       // Get filename/timestamp of the next source frame

                if (sceneFrames.Contains(followingFrame))                               // If next source frame is in scene folder, add to deletion list
                    sceneFramesToDelete.Add(followingFrame);
            }

            foreach (string frame in sceneFramesToDelete)
                IoUtils.TryDeleteIfExists(Path.Combine(sceneFramesPath, frame + I.currentSettings.framesExt));
        }

        public static int GetRoundedInterpFramesPerInputFrame(float factor, bool roundDown = true)
        {
            if (roundDown)
                return (int)Math.Floor(factor) - 1;
            else
                return factor.RoundToInt();
        }

        public static Fraction AskForFramerate(string mediaName, bool isImageSequence = true)
        {
            string text = $"Please enter an input frame rate to use for{(isImageSequence ? " the image sequence" : "")} '{mediaName.Trunc(80)}'.";
            PromptForm form = new PromptForm("Enter Frame Rate", text, "15");
            form.ShowDialog();
            return new Fraction(form.EnteredText);
        }
    }
}
