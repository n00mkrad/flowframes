using Flowframes.Media;
using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.OS;
using Flowframes.UI;
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
                bool frameFolderInput = IOUtils.IsPathDirectory(I.current.inPath);
                string targetPath = Path.Combine(I.current.framesFolder, lastFrameNum.ToString().PadLeft(Padding.inputFrames, '0') + I.current.framesExt);
                if (File.Exists(targetPath)) return;

                Size res = IOUtils.GetImage(IOUtils.GetFilesSorted(I.current.framesFolder, false).First()).Size;

                if (frameFolderInput)
                {
                    string lastFramePath = IOUtils.GetFilesSorted(I.current.inPath, false).Last();
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

            if (Config.GetInt("tempFolderLoc") == 1)
                basePath = outPath.GetParentDir();

            if (Config.GetInt("tempFolderLoc") == 2)
                basePath = outPath;

            if (Config.GetInt("tempFolderLoc") == 3)
                basePath = Paths.GetExeDir();

            if (Config.GetInt("tempFolderLoc") == 4)
            {
                string custPath = Config.Get("tempDirCustom");
                if (IOUtils.IsDirValid(custPath))
                    basePath = custPath;
            }

            return Path.Combine(basePath, Path.GetFileNameWithoutExtension(inPath).StripBadChars().Remove(" ").Trunc(30, false) + "-temp");
        }

        public static bool InputIsValid(string inDir, string outDir, Fraction fpsOut, float factor, I.OutMode outMode)
        {
            bool passes = true;

            bool isFile = !IOUtils.IsPathDirectory(inDir);

            if ((passes && isFile && !IOUtils.IsFileValid(inDir)) || (!isFile && !IOUtils.IsDirValid(inDir)))
            {
                ShowMessage("Input path is not valid!");
                passes = false;
            }

            if (passes && !IOUtils.IsDirValid(outDir))
            {
                ShowMessage("Output path is not valid!");
                passes = false;
            }

            if (passes && fpsOut.GetFloat() < 1f || fpsOut.GetFloat() > 1000f)
            {
                ShowMessage($"Invalid output frame rate ({fpsOut.GetFloat()}).\nMust be 1-1000.");
                passes = false;
            }

            if (outMode == I.OutMode.VidGif && fpsOut.GetFloat() > 50 && !(Config.GetFloat("maxFps") != 0 && Config.GetFloat("maxFps") <= 50))
                Logger.Log($"Warning: GIF will be encoded at 50 FPS instead of {fpsOut.GetFloat()} as the format doesn't support frame rates that high.");

            if (!passes)
                I.Cancel("Invalid settings detected.", true);

            return passes;
        }

        public static bool CheckAiAvailable(AI ai)
        {
            if (IOUtils.GetAmountOfFiles(Path.Combine(Paths.GetPkgPath(), ai.pkgDir), true) < 1)
            {
                ShowMessage("The selected AI is not installed!", "Error");
                I.Cancel("Selected AI not available.", true);
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
            if (!IOUtils.TryDeleteIfExists(I.current.tempFolder))
            {
                ShowMessage("Failed to remove an existing temp folder of this video!\nMake sure you didn't open any frames in an editor.", "Error");
                I.Cancel();
                return false;
            }
            return true;
        }

        public static bool CheckPathValid(string path)
        {
            if (IOUtils.IsPathDirectory(path))
            {
                if (!IOUtils.IsDirValid(path))
                {
                    ShowMessage("Input directory is not valid.");
                    I.Cancel();
                    return false;
                }
            }
            else
            {
                if (!IsVideoValid(path))
                {
                    ShowMessage("Input video file is not valid.");
                    return false;
                }
            }
            return true;
        }

        public static async Task<bool> CheckEncoderValid ()
        {
            string enc = FFmpegUtils.GetEnc(FFmpegUtils.GetCodec(I.current.outMode));

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
            if (videoPath == null || !IOUtils.IsFileValid(videoPath))
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
            int maxHeight = RoundDivisibleBy(Config.GetInt("maxVidHeight"), FfmpegCommands.GetPadding());
            if (inputRes.Height > maxHeight)
            {
                float factor = (float)maxHeight / inputRes.Height;
                Logger.Log($"Un-rounded downscaled size: {(inputRes.Width * factor).ToString("0.00")}x{Config.GetInt("maxVidHeight")}", true);
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

            if (current.alpha)
            {
                Logger.Log($"Not Using AutoEnc: Alpha mode is enabled.", true);
                return false;
            }

            if (!current.outMode.ToString().ToLower().Contains("vid") || current.outMode.ToString().ToLower().Contains("gif"))
            {
                Logger.Log($"Not Using AutoEnc: Out Mode is not video ({current.outMode.ToString()})", true);
                return false;
            }

            if (stepByStep && !Config.GetBool("sbsAllowAutoEnc"))
            {
                Logger.Log($"Not Using AutoEnc: Using step-by-step mode, but 'sbsAllowAutoEnc' is false.", true);
                return false;
            }

            if (!stepByStep && Config.GetInt("autoEncMode") == 0)
            {
                Logger.Log($"Not Using AutoEnc: 'autoEncMode' is 0.", true);
                return false;
            }

            int inFrames = IOUtils.GetAmountOfFiles(current.framesFolder, false);
            if (inFrames * current.interpFactor < (AutoEncode.chunkSize + AutoEncode.safetyBufferFrames) * 1.2f)
            {
                Logger.Log($"Not Using AutoEnc: Input frames ({inFrames}) * factor ({current.interpFactor}) is smaller than (chunkSize ({AutoEncode.chunkSize}) + safetyBufferFrames ({AutoEncode.safetyBufferFrames}) * 1.2f)", true);
                return false;
            }

            return true;
        }

        public static async Task<bool> UseUhd()
        {
            return (await GetOutputResolution(I.current.inPath, false)).Height >= Config.GetInt("uhdThresh");
        }

        public static void FixConsecutiveSceneFrames(string sceneFramesPath, string sourceFramesPath)
        {
            if (!Directory.Exists(sceneFramesPath) || IOUtils.GetAmountOfFiles(sceneFramesPath, false) < 1)
                return;

            List<string> sceneFrames = IOUtils.GetFilesSorted(sceneFramesPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
            List<string> sourceFrames = IOUtils.GetFilesSorted(sourceFramesPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
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
                IOUtils.TryDeleteIfExists(Path.Combine(sceneFramesPath, frame + I.current.framesExt));
        }
    }
}
