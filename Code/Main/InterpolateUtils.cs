﻿using Flowframes.Data;
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
using i = Flowframes.Interpolate;
using Padding = Flowframes.Data.Padding;

namespace Flowframes.Main
{
    class InterpolateUtils
    {
        public static PictureBox preview;
        public static BigPreviewForm bigPreviewForm;

        public static async Task CopyLastFrame (int lastFrameNum)
        {
            try
            {
                lastFrameNum--;     // We have to do this as extracted frames start at 0, not 1
                bool frameFolderInput = IOUtils.IsPathDirectory(i.current.inPath);
                string targetPath = Path.Combine(i.current.framesFolder, lastFrameNum.ToString().PadLeft(Padding.inputFrames, '0') + ".png");
                if (File.Exists(targetPath)) return;

                Size res = IOUtils.GetImage(IOUtils.GetFilesSorted(i.current.framesFolder, false).First()).Size;

                if (frameFolderInput)
                {
                    string lastFramePath = IOUtils.GetFilesSorted(i.current.inPath, false).Last();
                    await FFmpegCommands.ExtractLastFrame(lastFramePath, targetPath, res);
                }
                else
                {
                    await FFmpegCommands.ExtractLastFrame(i.current.inPath, targetPath, res);
                }
            }
            catch (Exception e)
            {
                Logger.Log("CopyLastFrame Error: " + e.Message);
            }
        }

        public static string GetOutExt (bool withDot = false)
        {
            string dotStr = withDot ? "." : "";
            if (Config.GetBool("jpegInterp"))
                return dotStr + "jpg";
            return dotStr + "png";
        }

        public static int targetFrames;
        public static string currentOutdir;
        public static int currentFactor;
        public static bool progressPaused = false;
        public static bool progCheckRunning = false;

        public static async void GetProgressByFrameAmount(string outdir, int target)
        {
            progCheckRunning = true;
            targetFrames = target;
            currentOutdir = outdir;
            Logger.Log($"Starting GetProgressByFrameAmount() loop for outdir '{currentOutdir}', target is {target} frames", true);
            bool firstProgUpd = true;
            Program.mainForm.SetProgress(0);
            while (Program.busy)
            {
                if (!progressPaused && AiProcess.processTime.IsRunning && Directory.Exists(currentOutdir))
                {
                    if (firstProgUpd && Program.mainForm.IsInFocus())
                        Program.mainForm.SetTab("preview");
                    firstProgUpd = false;
                    string[] frames = IOUtils.GetFilesSorted(currentOutdir, $"*.{GetOutExt()}");
                    if (frames.Length > 1)
                        UpdateInterpProgress(frames.Length, targetFrames, frames[frames.Length - 1]);
                    if (frames.Length >= targetFrames)
                        break;
                    await Task.Delay(GetProgressWaitTime(frames.Length));
                }
                else
                {
                    await Task.Delay(200);
                }
            }
            progCheckRunning = false;
            if (i.canceled)
                Program.mainForm.SetProgress(0);
        }

        public static void UpdateInterpProgress(int frames, int target, string latestFramePath = "")
        {
            if (i.canceled) return;

            frames = frames.Clamp(0, target);
            int percent = (int)Math.Round(((float)frames / target) * 100f);
            Program.mainForm.SetProgress(percent);

            float generousTime = ((AiProcess.processTime.ElapsedMilliseconds - AiProcess.lastStartupTimeMs) / 1000f);
            float fps = (float)frames / generousTime;
            string fpsIn = (fps / currentFactor).ToString("0.00");
            string fpsOut = fps.ToString("0.00");

            float secondsPerFrame = generousTime / (float)frames;
            int framesLeft = target - frames;
            float eta = framesLeft * secondsPerFrame;
            string etaStr = FormatUtils.Time(new TimeSpan(0, 0, eta.RoundToInt()), false);

            bool replaceLine = Regex.Split(Logger.textbox.Text, "\r\n|\r|\n").Last().Contains("Average Speed: ");

            string logStr = $"Interpolated {frames}/{target} frames ({percent}%) - Average Speed: {fpsIn} FPS In / {fpsOut} FPS Out - ";
            logStr += $"Time: {FormatUtils.Time(AiProcess.processTime.Elapsed)} - ETA: {etaStr}";
            if (AutoEncode.busy) logStr += " - Encoding...";
            Logger.Log(logStr, false, replaceLine);

            try
            {
                if (!string.IsNullOrWhiteSpace(latestFramePath) && frames > currentFactor)
                {
                    if (bigPreviewForm == null && !preview.Visible  /* ||Program.mainForm.WindowState != FormWindowState.Minimized */ /* || !Program.mainForm.IsInFocus()*/) return;        // Skip if the preview is not visible or the form is not in focus
                    Image img = IOUtils.GetImage(latestFramePath);
                    SetPreviewImg(img);
                }
            }
            catch { }
        }

        public static void SetPreviewImg (Image img)
        {
            if (img == null)
                return;
            preview.Image = img;
            if (bigPreviewForm != null)
                bigPreviewForm.SetImage(img);
        }

        public static Dictionary<string, int> frameCountCache = new Dictionary<string, int>();
        public static async Task<int> GetInputFrameCountAsync (string path)
        {
            string hash = await IOUtils.GetHashAsync(path, IOUtils.Hash.xxHash);     // Get checksum for caching
            if (hash.Length > 1 && frameCountCache.ContainsKey(hash))
            {
                Logger.Log($"FrameCountCache contains this hash ({hash}), using cached frame count.", true);
                return frameCountCache[hash];
            }
            else
            {
                Logger.Log($"Hash ({hash}) not cached, reading frame count.", true);
            }

            int frameCount = 0;
            if (IOUtils.IsPathDirectory(path))
                frameCount = IOUtils.GetAmountOfFiles(path, false);
            else
                frameCount = await FFmpegCommands.GetFrameCountAsync(path);

            if (hash.Length > 1 && frameCount > 5000)     // Cache if >5k frames to avoid re-reading it every single time
            {
                Logger.Log($"Adding hash ({hash}) with frame count {frameCount} to cache.", true);
                frameCountCache[hash] = frameCount;      // Use CRC32 instead of path to avoid using cached value if file was changed
            }
            return frameCount;
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

        public static string GetTempFolderLoc (string inPath, string outPath)
        {
            string basePath = inPath.GetParentDir();

            if(Config.GetInt("tempFolderLoc") == 1)
                basePath = outPath.GetParentDir();

            if (Config.GetInt("tempFolderLoc") == 2)
                basePath = outPath;

            if (Config.GetInt("tempFolderLoc") == 3)
                basePath = IOUtils.GetExeDir();

            if (Config.GetInt("tempFolderLoc") == 4)
            {
                string custPath = Config.Get("tempDirCustom");
                if(IOUtils.IsDirValid(custPath))
                    basePath = custPath;
            }

            return Path.Combine(basePath, Path.GetFileNameWithoutExtension(inPath).StripBadChars().Remove(" ").Trunc(30, false) + "-temp");
        }

        public static bool InputIsValid(string inDir, string outDir, float fpsOut, int interp, Interpolate.OutMode outMode)
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
            if (passes && interp != 2 && interp != 4 && interp != 8)
            {
                ShowMessage("Interpolation factor is not valid!");
                passes = false;
            }
            if (passes && outMode == i.OutMode.VidGif && fpsOut > 50)
            {
                ShowMessage("Invalid output frame rate!\nGIF does not properly support frame rates above 40 FPS.\nPlease use MP4, WEBM or another video format.");
                passes = false;
            }
            if (passes && fpsOut < 1 || fpsOut > 500)
            {
                ShowMessage("Invalid output frame rate - Must be 1-500.");
                passes = false;
            }
            if (!passes)
                i.Cancel("Invalid settings detected.", true);
            return passes;
        }

        public static void PathAsciiCheck (string inpath, string outpath)
        {
            bool shownMsg = false;
            
            if (OSUtils.HasNonAsciiChars(inpath))
            {
                ShowMessage("Warning: Input path includes non-ASCII characters. This might cause problems.");
                shownMsg = true;
            }

            if (!shownMsg && OSUtils.HasNonAsciiChars(outpath))
                ShowMessage("Warning: Output path includes non-ASCII characters. This might cause problems.");
        }

        public static void GifCompatCheck (Interpolate.OutMode outMode, float fpsOut, int targetFrameCount)
        {
            if (outMode != Interpolate.OutMode.VidGif)
                return;

            if(fpsOut >= 50f)
                Logger.Log("Warning: GIFs above 50 FPS might play slower on certain software/hardware! MP4 is recommended for higher frame rates.");

            int maxGifFrames = 200;
            if (targetFrameCount > maxGifFrames)
            {
                ShowMessage($"You can't use GIF with more than {maxGifFrames} output frames!\nPlease use MP4 for this.", "Error");
                i.Cancel($"Can't use GIF encoding with more than {maxGifFrames} frames!");
            }
        }

        public static bool CheckAiAvailable (AI ai)
        {
            if (!PkgUtils.IsAiAvailable(ai))
            {
                ShowMessage("The selected AI is not installed!\nYou can download it from the Package Installer.", "Error");
                i.Cancel("Selected AI not available.", true);
                return false;
            }
            return true;
        }

        public static bool CheckDeleteOldTempFolder ()
        {
            if (!IOUtils.TryDeleteIfExists(i.current.tempFolder))
            {
                ShowMessage("Failed to remove an existing temp folder of this video!\nMake sure you didn't open any frames in an editor.", "Error");
                i.Cancel();
                return false;
            }
            return true;
        }

        public static bool CheckPathValid (string path)
        {
            if (IOUtils.IsPathDirectory(path))
            {
                if (!IOUtils.IsDirValid(path))
                {
                    ShowMessage("Input directory is not valid.");
                    i.Cancel();
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

        public static async Task<Size> GetOutputResolution (string inputPath, bool print, bool returnZeroIfUnchanged = false)
        {
            Size resolution = await IOUtils.GetVideoOrFramesRes(inputPath);
            return GetOutputResolution(resolution, print, returnZeroIfUnchanged);
        }

        public static Size GetOutputResolution(Size inputRes, bool print = false, bool returnZeroIfUnchanged = false)
        {
            int maxHeight = RoundDiv2(Config.GetInt("maxVidHeight"));
            if (inputRes.Height > maxHeight)
            {
                float factor = (float)maxHeight / inputRes.Height;
                Logger.Log($"Un-rounded downscaled size: {(inputRes.Width * factor).ToString("0.00")}x{Config.GetInt("maxVidHeight")}", true);
                int width = RoundDiv2((inputRes.Width * factor).RoundToInt());
                if (print)
                    Logger.Log($"Video is bigger than the maximum - Downscaling to {width}x{maxHeight}.");
                return new Size(width, maxHeight);
            }
            else
            {
                //return new Size(RoundDiv2(inputRes.Width), RoundDiv2(inputRes.Height));
                if (returnZeroIfUnchanged)
                    return new Size();
                else
                    return inputRes;
            }
        }

        public static int RoundDiv2(int n)     // Round to a number that's divisible by 2 (for h264 etc)
        {
            int a = (n / 2) * 2;    // Smaller multiple
            int b = a + 2;   // Larger multiple
            return (n - a > b - n) ? b : a; // Return of closest of two
        }

        public static bool CanUseAutoEnc (bool stepByStep, InterpSettings current)
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

            if(stepByStep && !Config.GetBool("sbsAllowAutoEnc"))
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

        public static async Task<bool> UseUHD ()
        {
            return (await GetOutputResolution(i.current.inPath, false)).Height >= Config.GetInt("uhdThresh");
        }

        public static void FixConsecutiveSceneFrames (string sceneFramesPath, string sourceFramesPath)
        {
            if (!Directory.Exists(sceneFramesPath) || IOUtils.GetAmountOfFiles(sceneFramesPath, false) < 1)
                return;

            List<string> sceneFrames = IOUtils.GetFilesSorted(sceneFramesPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
            List<string> sourceFrames = IOUtils.GetFilesSorted(sourceFramesPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();
            List<string> sceneFramesToDelete = new List<string>();

            foreach(string scnFrame in sceneFrames)
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
                IOUtils.TryDeleteIfExists(Path.Combine(sceneFramesPath, frame + ".png"));
        }

        public static void UpdateVideoDuration(string path)
        {
            Program.mainForm.currInDuration = FFmpegCommands.GetDuration(path);
        }
    }
}
