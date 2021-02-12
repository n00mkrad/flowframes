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
        public static PictureBox preview;
        public static BigPreviewForm bigPreviewForm;

        public static async Task CopyLastFrame (int lastFrameNum)
        {
            try
            {
                lastFrameNum--;     // We have to do this as extracted frames start at 0, not 1
                bool frameFolderInput = IOUtils.IsPathDirectory(I.current.inPath);
                string targetPath = Path.Combine(I.current.framesFolder, lastFrameNum.ToString().PadLeft(Padding.inputFrames, '0') + ".png");
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

        public static string GetOutExt (bool withDot = false)
        {
            string dotStr = withDot ? "." : "";
            if (Config.GetBool("jpegInterp"))
                return dotStr + "jpg";
            return dotStr + "png";
        }

        public static int lastFrame;
        public static int targetFrames;
        public static string currentOutdir;
        public static float currentFactor;
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
            lastFrame = 0;
            while (Program.busy)
            {
                if (!progressPaused && AiProcess.processTime.IsRunning && Directory.Exists(currentOutdir))
                {
                    if (firstProgUpd && Program.mainForm.IsInFocus())
                        Program.mainForm.SetTab("preview");

                    firstProgUpd = false;
                    string lastFramePath = currentOutdir + "\\" + lastFrame.ToString("00000000") + $".{GetOutExt()}";

                    if (lastFrame > 1)
                        UpdateInterpProgress(lastFrame, targetFrames, lastFramePath);

                    await Task.Delay((target < 1000) ? 100 : 200);  // Update 10x/sec if interpolating <1k frames, otherwise 5x/sec

                    if (lastFrame >= targetFrames)
                        break;
                }
                else
                {
                    await Task.Delay(100);
                }
            }
            progCheckRunning = false;
            if (I.canceled)
                Program.mainForm.SetProgress(0);
        }

        public static void UpdateLastFrameFromInterpOutput(string output)
        {
            try
            {
                string ncnnStr = I.current.ai.aiName.Contains("NCNN") ? " done" : "";
                Regex frameRegex = new Regex($@"(?<=.)\d*(?=.{GetOutExt()}{ncnnStr})");
                if (!frameRegex.IsMatch(output)) return;
                lastFrame = Math.Max(int.Parse(frameRegex.Match(output).Value), lastFrame);
            }
            catch
            {
                Logger.Log($"UpdateLastFrameFromInterpOutput: Failed to get progress from '{output}' even though Regex matched!");
            }
        }

        public static int interpolatedInputFramesCount;

        public static void UpdateInterpProgress(int frames, int target, string latestFramePath = "")
        {
            if (I.canceled) return;
            interpolatedInputFramesCount = ((frames / I.current.interpFactor).RoundToInt() - 1);
            ResumeUtils.Save();
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

            string logStr = $"Interpolated {frames}/{target} Frames ({percent}%) - Average Speed: {fpsIn} FPS In / {fpsOut} FPS Out - ";
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

        public static async Task DeleteInterpolatedInputFrames()
        {
            interpolatedInputFramesCount = 0;
            string[] inputFrames = IOUtils.GetFilesSorted(I.current.framesFolder);

            for (int i = 0; i < inputFrames.Length; i++)
            {
                while (Program.busy && (i + 10) > interpolatedInputFramesCount) await Task.Delay(1000);
                if (!Program.busy) break;
                if(i != 0 && i != inputFrames.Length - 1)
                    IOUtils.OverwriteFileWithText(inputFrames[i]);
                if (i % 10 == 0) await Task.Delay(10);
            }
        }

        public static void SetPreviewImg(Image img)
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
            int maxMb = Config.GetInt("storeHashedFramecountMaxSizeMb", 256);
            string hash = "";

            if (IOUtils.GetFilesize(path) >= 0 && IOUtils.GetFilesize(path) < maxMb * 1024 * 1024)
                hash = await IOUtils.GetHashAsync(path, IOUtils.Hash.xxHash);     // Get checksum for caching
            else
                Logger.Log($"GetInputFrameCountAsync: File bigger than {maxMb}mb, won't hash.", true);

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
                frameCount = await FfmpegCommands.GetFrameCountAsync(path);

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

        public static bool InputIsValid(string inDir, string outDir, float fpsOut, float factor, Interpolate.OutMode outMode)
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
            if (passes && /*factor != 2 && factor != 4 && factor != 8*/ factor > 16)
            {
                ShowMessage("Interpolation factor is not valid!");
                passes = false;
            }
            if (passes && outMode == I.OutMode.VidGif && fpsOut > 50 && Config.GetFloat("maxFps") != 0 && Config.GetFloat("maxFps") > 50)
            {
                ShowMessage("Invalid output frame rate!\nGIF does not properly support frame rates above 50 FPS.\nPlease use MP4, WEBM or another video format.");
                passes = false;
            }
            if (passes && fpsOut < 1 || fpsOut > 1000)
            {
                ShowMessage("Invalid output frame rate - Must be 1-1000.");
                passes = false;
            }
            if (!passes)
                I.Cancel("Invalid settings detected.", true);
            return passes;
        }

        public static void PathAsciiCheck (string path, string pathTitle)
        {            
            if (IOUtils.HasBadChars(path) || OSUtils.HasNonAsciiChars(path))
                ShowMessage($"Warning: Your {pathTitle} includes special characters. This might cause problems.");
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
                I.Cancel($"Can't use GIF encoding with more than {maxGifFrames} frames!");
            }
        }

        public static bool CheckAiAvailable (AI ai)
        {
            if (!PkgUtils.IsAiAvailable(ai))
            {
                ShowMessage("The selected AI is not installed!\nYou can download it from the Package Installer.", "Error");
                I.Cancel("Selected AI not available.", true);
                return false;
            }
            return true;
        }

        public static bool CheckDeleteOldTempFolder ()
        {
            if (!IOUtils.TryDeleteIfExists(I.current.tempFolder))
            {
                ShowMessage("Failed to remove an existing temp folder of this video!\nMake sure you didn't open any frames in an editor.", "Error");
                I.Cancel();
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
            return (await GetOutputResolution(I.current.inPath, false)).Height >= Config.GetInt("uhdThresh");
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
    }
}
