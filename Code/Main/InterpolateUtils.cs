using Flowframes.Media;
using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Os;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
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
                lastFrameNum--;     // We have to do this as extracted frames start at 0, not 1
                bool frameFolderInput = IoUtils.IsPathDirectory(I.current.inPath);
                string targetPath = Path.Combine(I.current.framesFolder, lastFrameNum.ToString().PadLeft(Padding.inputFrames, '0') + I.current.framesExt);
                if (File.Exists(targetPath)) return;

                Size res = IoUtils.GetImage(IoUtils.GetFilesSorted(I.current.framesFolder, false).First()).Size;

                if (frameFolderInput)
                {
                    string lastFramePath = IoUtils.GetFilesSorted(I.current.inPath, false).Last();
                    await FfmpegExtract.ExtractLastFrame(lastFramePath, targetPath, res);
                }
                else
                {
                    await FfmpegExtract.ExtractLastFrame(I.current.inPath, targetPath, res);
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
            string basePath = inPath.GetParentDir();

            if (Config.GetInt(Config.Key.tempFolderLoc) == 1)
                basePath = outPath.GetParentDir();

            if (Config.GetInt(Config.Key.tempFolderLoc) == 2)
                basePath = outPath;

            if (Config.GetInt(Config.Key.tempFolderLoc) == 3)
                basePath = Paths.GetExeDir();

            if (Config.GetInt(Config.Key.tempFolderLoc) == 4)
            {
                string custPath = Config.Get(Config.Key.tempDirCustom);
                if (IoUtils.IsDirValid(custPath))
                    basePath = custPath;
            }

            return Path.Combine(basePath, Path.GetFileNameWithoutExtension(inPath).StripBadChars().Remove(" ").Trunc(30, false) + "-temp");
        }

        public static bool InputIsValid(string inDir, string outDir, Fraction fpsIn, float factor, I.OutMode outMode)
        {
            try
            {
                bool passes = true;
                bool isFile = !IoUtils.IsPathDirectory(inDir);
                float fpsOut = fpsIn.GetFloat() * factor;

                if ((passes && isFile && !IoUtils.IsFileValid(inDir)) || (!isFile && !IoUtils.IsDirValid(inDir)))
                {
                    ShowMessage("Input path is not valid!");
                    passes = false;
                }

                if (passes && !IoUtils.IsDirValid(outDir))
                {
                    ShowMessage("Output path is not valid!");
                    passes = false;
                }

                if (passes && fpsOut < 1f || fpsOut > 1000f)
                {
                    string imgSeqNote = isFile ? "" : "\n\nWhen using an image sequence as input, you always have to specify the frame rate manually.";
                    ShowMessage($"Invalid output frame rate ({fpsOut}).\nMust be 1-1000.{imgSeqNote}");
                    passes = false;
                }

                string fpsLimitValue = Config.Get(Config.Key.maxFps);
                float fpsLimit = (fpsLimitValue.Contains("/") ? new Fraction(Config.Get(Config.Key.maxFps)).GetFloat() : fpsLimitValue.GetFloat());

                if (outMode == I.OutMode.VidGif && fpsOut > 50 && !(fpsLimit > 0 && fpsLimit <= 50))
                    Logger.Log($"Warning: GIF will be encoded at 50 FPS instead of {fpsOut} as the format doesn't support frame rates that high.");

                if (!passes)
                    I.Cancel("Invalid settings detected.", true);

                return passes;
            }
            catch(Exception e)
            {
                Logger.Log($"Failed to run InputIsValid: {e.Message}\n{e.StackTrace}", true);
                return false;
            }
        }

        public static bool CheckAiAvailable(AI ai, ModelCollection.ModelInfo model)
        {
            if (IoUtils.GetAmountOfFiles(Path.Combine(Paths.GetPkgPath(), ai.pkgDir), true) < 1)
            {
                ShowMessage("The selected AI is not installed!", "Error");
                I.Cancel("Selected AI not available.", true);
                return false;
            }

            if (model == null || model.dir.Trim() == "")
            {
                ShowMessage("No valid AI model has been selected!", "Error");
                I.Cancel("No valid model selected.", true);
                return false;
            }

            if (I.current.ai.aiName.ToUpper().Contains("CUDA") && NvApi.gpuList.Count < 1)
            {
                ShowMessage("Warning: No Nvidia GPU was detected. CUDA might fall back to CPU!\n\nTry an NCNN implementation instead if you don't have an Nvidia GPU.", "Error");
                
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
            if (!IoUtils.TryDeleteIfExists(I.current.tempFolder))
            {
                ShowMessage("Failed to remove an existing temp folder of this video!\nMake sure you didn't open any frames in an editor.", "Error");
                I.Cancel();
                return false;
            }
            return true;
        }

        public static void ShowWarnings (float factor, AI ai)
        {
            string aiName = ai.aiName.Replace("_", "-");

            if (factor > 2 && ai.multiPass && Config.GetInt(Config.Key.autoEncMode) > 0)
            {
                int times = (int)Math.Log(factor, 2);
                Logger.Log($"Warning: {aiName} can only do 2x at a time and will run {times} times for {factor}x. Auto-Encode will only work on the last run.");
            }

            if (Config.GetInt(Config.Key.cmdDebugMode) > 0)
                Logger.Log($"Warning: The CMD window for interpolation is enabled. This will disable Auto-Encode and the progress bar!");
        }

        public static bool CheckPathValid(string path)
        {
            if (IoUtils.IsPathDirectory(path))
            {
                if (!IoUtils.IsDirValid(path))
                {
                    ShowMessage("Input directory is not valid.\nMake sure it still exists and hasn't been renamed or moved!");
                    I.Cancel();
                    return false;
                }
            }
            else
            {
                if (!IsVideoValid(path))
                {
                    ShowMessage("Input video file is not valid.\nMake sure it still exists and hasn't been renamed or moved!");
                    return false;
                }
            }
            return true;
        }

        public static async Task<bool> CheckEncoderValid ()
        {
            string enc = FfmpegUtils.GetEnc(FfmpegUtils.GetCodec(I.current.outMode));

            if (!enc.ToLower().Contains("nvenc"))
                return true;

            if (!(await FfmpegCommands.IsEncoderCompatible(enc)))
            {
                ShowMessage("NVENC encoding is not available on your hardware!\nPlease use a different encoder.", "Error");
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

        public static void ShowMessage(string msg, string title = "Message")
        {
            if (!BatchProcessing.busy)
                MessageBox.Show(msg, title);

            Logger.Log("Message: " + msg, true);
        }

        public static async Task<Size> GetOutputResolution(string inputPath, bool print, bool returnZeroIfUnchanged = false)
        {
            Size resolution = await GetMediaResolutionCached.GetSizeAsync(inputPath);
            return GetOutputResolution(resolution, print, returnZeroIfUnchanged);
        }

        public static Size GetOutputResolution(Size inputRes, bool print = false, bool returnZeroIfUnchanged = false)
        {
            int maxHeightValue = Config.GetInt(Config.Key.maxVidHeight);
            int maxHeight = RoundDivisibleBy(maxHeightValue, FfmpegCommands.GetPadding());

            if (inputRes.Height > maxHeight)
            {
                float factor = (float)maxHeight / inputRes.Height;
                Logger.Log($"Un-rounded downscaled size: {(inputRes.Width * factor).ToString("0.00")}x{maxHeightValue}", true);
                int width = RoundDivisibleBy((inputRes.Width * factor).RoundToInt(), FfmpegCommands.GetPadding());
                if (print)
                    Logger.Log($"Video is bigger than the maximum - Downscaling to {width}x{maxHeight}.");
                return new Size(width, maxHeight);
            }
            else
            {
                if (returnZeroIfUnchanged)
                    return new Size();
                else
                    return inputRes;
            }
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

            if (Config.GetInt(Config.Key.cmdDebugMode) > 0)
            {
                Logger.Log($"Not Using AutoEnc: CMD window is shown (cmdDebugMode > 0)", true);
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
            return (await GetOutputResolution(I.current.inPath, false)).Height >= Config.GetInt(Config.Key.uhdThresh);
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
                IoUtils.TryDeleteIfExists(Path.Combine(sceneFramesPath, frame + I.current.framesExt));
        }
    }
}
