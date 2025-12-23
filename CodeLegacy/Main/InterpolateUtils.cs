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
            string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "Flowframes");
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

            string folderName = Path.GetFileNameWithoutExtension(inPath).StripBadChars().Remove(" ").Trunc(35, false) + "_tmp";
            return Path.Combine(basePath, folderName);
        }

        public static bool InputIsValid(InterpSettings s)
        {
            try
            {
                bool passes = true;
                bool isFile = !IoUtils.IsPathDirectory(s.inPath);

                if (passes && (IoUtils.IsPathOneDrive(s.inPath) || IoUtils.IsPathOneDrive(s.outPath)))
                {
                    UiUtils.ShowMessageBox("OneDrive paths are not supported. Please use a local path instead.");
                    passes = false;
                }

                if ((passes && (isFile && !IoUtils.IsFileValid(s.inPath)) || (!isFile && !IoUtils.IsDirValid(s.inPath))))
                {
                    UiUtils.ShowMessageBox("Input path is not valid!");
                    passes = false;
                }

                if (passes && !IoUtils.IsDirValid(s.outPath))
                {
                    UiUtils.ShowMessageBox("Output path is not valid!");
                    passes = false;
                }

                if (passes && (s.tempFolder.StartsWith(@"\\") || IoUtils.IsPathOneDrive(s.tempFolder)))
                {
                    UiUtils.ShowMessageBox("Flowframes does not support network paths as a temp folder!\nPlease use a local path instead.");
                    passes = false;
                }

                float fpsLimit = s.outFpsResampled.Float;
                int maxEncoderFps = s.outSettings.Encoder.GetInfo().MaxFramerate;

                if (passes && (s.outFps.Float < 1f || (s.outFps.Float > maxEncoderFps && !(fpsLimit > 0 && fpsLimit <= maxEncoderFps))))
                {
                    string imgSeqNote = isFile ? "" : "\n\nWhen using an image sequence as input, you always have to specify the frame rate manually.";
                    UiUtils.ShowMessageBox($"Invalid output frame rate ({s.outFps.Float}).\nMust be 1-{maxEncoderFps}. Either lower the interpolation factor or use the \"Maximum Output Frame Rate\" option.{imgSeqNote}");
                    passes = false;
                }

                if (passes && s.dedupe && I.currentMediaFile.IsVfr)
                {
                    UiUtils.ShowMessageBox($"Using de-duplication on VFR videos is currently not supported.\n\nGo to Quick Settings and either disable De-Duplication, or force VFR off.");
                    passes = false;
                }

                if(passes && I.currentMediaFile.VideoExtraData.IsHdr)
                {
                    string badHdrSettingsStr = I.currentSettings.outSettings.GetHdrNotSuitableReason();

                    if(badHdrSettingsStr.IsNotEmpty())
                    {
                        Logger.Log(badHdrSettingsStr);
                    }
                }

                I.InterpProgressMultiplier = s.FpsResampling ? s.outFps.Float / fpsLimit : 1f;

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

        public static bool CheckAiAvailable(AiInfo ai, ModelCollection.ModelInfo model, bool allowNullModel = false)
        {
            if (IoUtils.GetAmountOfFiles(Path.Combine(Paths.GetPkgPath(), ai.PkgDir), true) < 1)
            {
                UiUtils.ShowMessageBox("The selected AI is not installed!", UiUtils.MessageType.Error);
                I.Cancel("Selected AI not available.", true);
                return false;
            }

            if (!allowNullModel && (model == null || model.Dir.Trim() == ""))
            {
                UiUtils.ShowMessageBox("No valid AI model has been selected!", UiUtils.MessageType.Error);
                I.Cancel("No valid model selected.", true);
                return false;
            }

            if (I.currentSettings.ai.NameInternal.Upper().Contains("CUDA") && NvApi.NvGpus.Count < 1)
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

        public static bool CheckPathValid(string path)
        {
            if (path.StartsWith(@"\\"))
            {
                UiUtils.ShowMessageBox("Input path is not valid.\nFlowframes does not support UNC/Network paths.");
                I.Cancel();
                return false;
            }

            bool isDir = IoUtils.IsPathDirectory(path);

            if (isDir && !IoUtils.IsDirValid(path))
            {
                UiUtils.ShowMessageBox("Input directory is not valid.\nMake sure it still exists and hasn't been renamed or moved!");
                I.Cancel();
                return false;
            }

            if (!isDir && !IoUtils.IsFileValid(path))
            {
                UiUtils.ShowMessageBox("Input video file is not valid.\nMake sure it still exists and hasn't been renamed or moved!");
                return false;
            }

            return true;
        }

        public static async Task<Size> GetOutputResolution(FfmpegCommands.ModuloMode moduloMode, string inputPath, bool print = false)
        {
            Size resolution = await GetMediaResolutionCached.GetSizeAsync(inputPath);
            return GetInterpolationResolution(moduloMode, resolution, print);
        }

        public static Size GetInterpolationResolution(FfmpegCommands.ModuloMode moduloMode, Size inputRes, bool onlyRoundUp = true, bool print = false)
        {
            Size res = new Size(inputRes.Width, inputRes.Height);
            int maxHeight = Config.GetInt(Config.Key.maxVidHeight);
            int modulo = FfmpegCommands.GetModulo(moduloMode);
            float factor = res.Height > maxHeight ? (float)maxHeight / res.Height : 1f; // Calculate downscale factor if bigger than max, otherwise just use 1x
            int width = RoundDivisibleBy((res.Width * factor).RoundToInt(), modulo, onlyRoundUp);
            int height = RoundDivisibleBy((res.Height * factor).RoundToInt(), modulo, onlyRoundUp);
            res = new Size(width, height);

            if (print && factor < 1f)
                Logger.Log($"Video is bigger than the maximum - Downscaling to {width}x{height}.");

            Logger.Log($"Scaled input res {inputRes.Width}x{inputRes.Height} to {res.Width}x{res.Height} ({moduloMode})", true);
            return res;
        }

        public static int RoundDivisibleBy(float number, int divisibleBy, bool onlyRoundUp = false)
        {
            int numberInt = number.RoundToInt();

            if (divisibleBy == 0)
                return numberInt;

            return onlyRoundUp
                ? (int)Math.Ceiling((double)number / divisibleBy) * divisibleBy
                : (int)Math.Round((double)number / divisibleBy) * divisibleBy;
        }

        public static bool CanUseAutoEnc(bool stepByStep, InterpSettings current)
        {
            AutoEncode.UpdateChunkAndBufferSizes();

            if (current.ai.Piped)
            {
                Logger.Log($"Not Using AutoEnc: Using piped encoding.", true);
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

        public static bool UseUhd()
        {
            return UseUhd(I.currentSettings.OutputResolution);
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

                int sourceIndexForScnFrame = sourceFrames.IndexOf(scnFrame); // Get source index of scene frame
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
            string text = $"Please enter the source frame rate for{(isImageSequence ? " the image sequence" : "")} '{mediaName.Trunc(80)}'.";
            var form = new PromptForm("Enter Frame Rate", text, "15");
            form.ShowDialog();
            return new Fraction(form.EnteredText);
        }
    }
}
