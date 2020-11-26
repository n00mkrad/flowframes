using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.OS;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using i = Flowframes.Interpolate;

namespace Flowframes.Main
{
    class InterpolateUtils
    {
        public static PictureBox preview;
        public static BigPreviewForm bigPreviewForm;

        public static string lastExt = "png";

        public static void UpdateInterpProgress(int frames, int target, string latestFramePath = "")
        {
            int percent = (int)Math.Round(((float)frames / target) * 100f);
            Program.mainForm.SetProgress(percent);

            float generousTime = ((AiProcess.processTime.ElapsedMilliseconds - AiProcess.lastStartupTimeMs) / 1000f);
            float fps = (float)frames / generousTime;
            string fpsIn = (fps / Interpolate.interpFactor).ToString("0.00");
            string fpsOut = fps.ToString("0.00");

            float secondsPerFrame = generousTime / (float)frames;
            int framesLeft = target - frames;
            float eta = framesLeft * secondsPerFrame;
            string etaStr = FormatUtils.Time(new TimeSpan(0, 0, eta.RoundToInt()));

            bool replaceLine = Regex.Split(Logger.textbox.Text, "\r\n|\r|\n").Last().Contains("Average Speed: ");
            Logger.Log($"Interpolated {frames}/{target} frames ({percent}%) - Average Speed: {fpsIn} FPS In / {fpsOut} FPS Out - Time: {FormatUtils.Time(AiProcess.processTime.Elapsed)} - ETA: {etaStr}", false, replaceLine);

            try
            {
                if (!string.IsNullOrWhiteSpace(latestFramePath) && frames > Interpolate.interpFactor)
                {
                    if (bigPreviewForm == null && !preview.Visible  /* ||Program.mainForm.WindowState != FormWindowState.Minimized */ /* || !Program.mainForm.IsInFocus()*/) return;        // Skip if the preview is not visible or the form is not in focus
                    Image img = IOUtils.GetImage(latestFramePath);
                    preview.Image = img;
                    if (bigPreviewForm != null)
                        bigPreviewForm.SetImage(img);
                }
            }
            catch { }
        }

        public static int GetProgressWaitTime(int numFrames)
        {
            float hddMultiplier = 2f;
            if (Program.lastInputPathIsSsd)
                hddMultiplier = 1f;

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

            return Path.Combine(basePath, Path.GetFileNameWithoutExtension(inPath).StripBadChars() + "-temp");
        }

        public static bool InputIsValid(string inDir, string outDir, float fpsOut, int interp, int tilesize)
        {
            bool passes = true;   

            bool isFile = !IOUtils.IsPathDirectory(inDir);

            if ((isFile && !IOUtils.IsFileValid(inDir)) || (!isFile && !IOUtils.IsDirValid(inDir)))
            {
                ShowMessage("Input path is not valid!");
                passes = false;
            }
            if (!IOUtils.IsDirValid(outDir))
            {
                ShowMessage("Output path is not valid!");
                passes = false;
            }
            if (interp != 2 && interp != 4 && interp != 8)
            {
                ShowMessage("Interpolation factor is not valid!");
                passes = false;
            }
            if (fpsOut < 1 || fpsOut > 500)
            {
                ShowMessage("Invalid target frame rate - Must be 1-500.");
                passes = false;
            }

            if (tilesize % 32 != 0 || tilesize < 128)
            {
                ShowMessage("Tile size is not valid - Must be a multiple of 32 and at least 128!");
                passes = false;
            }

            if (!passes)
                i.Cancel("Invalid settings detected.");
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
            if(!PkgUtils.IsUpToDate(ai.pkg, ai.minPkgVer))
            {
                ShowMessage("The selected AI is installed, but not up to date!\nYou can update it in the Package Installer.", "Error");
                i.Cancel("Selected AI is outdated.", true);
                return false;
            }
            return true;
        }

        public static bool CheckDeleteOldTempFolder ()
        {
            if (!IOUtils.TryDeleteIfExists(i.currentTempDir))
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
            string ext = Path.GetExtension(videoPath);
            if (Formats.supported.Contains(ext))
                return true;
            return false;
        }

        public static string GetExt(i.OutMode format)
        {
            if (format == i.OutMode.VidMp4)
                return ".mp4";
            if (format == i.OutMode.VidGif)
                return ".gif";
            return ".mp4";
        }

        public static void ShowMessage(string msg, string title = "Message")
        {
            if (!BatchProcessing.busy)
                MessageBox.Show(msg, title);
            Logger.Log("Message: " + msg, true);
        }
    }
}
