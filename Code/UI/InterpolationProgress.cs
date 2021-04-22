using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.OS;
using Flowframes.UI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.Forms;
using Flowframes.Main;
using I = Flowframes.Interpolate;
using Padding = Flowframes.Data.Padding;

namespace Flowframes.UI
{
    class InterpolationProgress
    {
        public static int lastFrame;
        public static int targetFrames;
        public static string currentOutdir;
        public static float currentFactor;
        public static bool progressPaused = false;
        public static bool progCheckRunning = false;

        public static PictureBox preview;
        public static BigPreviewForm bigPreviewForm;

        public static async void GetProgressByFrameAmount(string outdir, int target)
        {
            progCheckRunning = true;
            targetFrames = target;
            currentOutdir = outdir;
            Logger.Log($"Starting GetProgressByFrameAmount() loop for outdir '{currentOutdir}', target is {target} frames", true);
            bool firstProgUpd = true;
            Program.mainForm.SetProgress(0);
            lastFrame = 0;
            peakFpsOut = 0f;

            while (Program.busy)
            {
                if (!progressPaused && AiProcess.processTime.IsRunning && Directory.Exists(currentOutdir))
                {
                    if (firstProgUpd && Program.mainForm.IsInFocus())
                        Program.mainForm.SetTab("preview");

                    firstProgUpd = false;
                    string lastFramePath = currentOutdir + "\\" + lastFrame.ToString("00000000") + I.current.interpExt;

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
                Regex frameRegex = new Regex($@"(?<=.)\d*(?={I.current.interpExt}{ncnnStr})");
                if (!frameRegex.IsMatch(output)) return;
                lastFrame = Math.Max(int.Parse(frameRegex.Match(output).Value), lastFrame);
            }
            catch
            {
                Logger.Log($"UpdateLastFrameFromInterpOutput: Failed to get progress from '{output}' even though Regex matched!");
            }
        }

        public static int interpolatedInputFramesCount;
        public static float peakFpsOut;

        public static int previewUpdateRateMs = 200;

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

            if (fps > peakFpsOut)
                peakFpsOut = fps;

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
                    if (timeSinceLastPreviewUpdate.IsRunning && timeSinceLastPreviewUpdate.ElapsedMilliseconds < previewUpdateRateMs) return;
                    Image img = IOUtils.GetImage(latestFramePath);
                    SetPreviewImg(img);
                }
            }
            catch (Exception e)
            {
                //Logger.Log("Error updating preview: " + e.Message, true);
            }
        }

        public static async Task DeleteInterpolatedInputFrames()
        {
            interpolatedInputFramesCount = 0;
            string[] inputFrames = IOUtils.GetFilesSorted(I.current.framesFolder);

            for (int i = 0; i < inputFrames.Length; i++)
            {
                while (Program.busy && (i + 10) > interpolatedInputFramesCount) await Task.Delay(1000);
                if (!Program.busy) break;
                if (i != 0 && i != inputFrames.Length - 1)
                    IOUtils.OverwriteFileWithText(inputFrames[i]);
                if (i % 10 == 0) await Task.Delay(10);
            }
        }

        public static Stopwatch timeSinceLastPreviewUpdate = new Stopwatch();

        public static void SetPreviewImg(Image img)
        {
            if (img == null)
                return;

            timeSinceLastPreviewUpdate.Restart();

            preview.Image = img;

            if (bigPreviewForm != null)
                bigPreviewForm.SetImage(img);
        }
    }
}
