using Flowframes.IO;
using Flowframes.MiscUtils;
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
using Flowframes.Os;

namespace Flowframes.Ui
{
    class InterpolationProgress
    {
        public static int deletedFramesCount;
        public static int lastFrame;
        public static int targetFrames;
        public static string currentOutdir;
        public static float currentFactor;
        public static bool progressPaused = false;
        public static bool progCheckRunning = false;

        public static PictureBox preview;
        public static BigPreviewForm bigPreviewForm;

        public static void Restart ()
        {
            progCheckRunning = true;
            deletedFramesCount = 0;
            lastFrame = 0;
            peakFpsOut = 0f;
            Program.mainForm.SetProgress(0);
        }

        public static async void GetProgressByFrameAmount(string outdir, int target)
        {
            targetFrames = target;
            currentOutdir = outdir;
            Restart();
            Logger.Log($"Starting GetProgressByFrameAmount() loop for outdir '{currentOutdir}', target is {target} frames", true);
            bool firstProgUpd = true;

            while (Program.busy)
            {
                if (!progressPaused && AiProcess.processTime.IsRunning && Directory.Exists(currentOutdir))
                {
                    if (firstProgUpd && Program.mainForm.IsInFocus())
                        Program.mainForm.SetTab(Program.mainForm.previewTab.Name);

                    firstProgUpd = false;
                    string lastFramePath = currentOutdir + "\\" + lastFrame.ToString("00000000") + I.currentSettings.interpExt;

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
                string ncnnStr = I.currentSettings.ai.NameInternal.Contains("NCNN") ? " done" : "";
                Regex frameRegex = new Regex($@"(?<=.)\d*(?={I.currentSettings.interpExt}{ncnnStr})");
                if (!frameRegex.IsMatch(output)) return;
                lastFrame = Math.Max(int.Parse(frameRegex.Match(output).Value), lastFrame);
            }
            catch
            {
                Logger.Log($"UpdateLastFrameFromInterpOutput: Failed to get progress from '{output}' even though Regex matched!", true);
            }
        }

        public static async void GetProgressFromFfmpegLog(string logFile, int target)
        {
            targetFrames = target;
            Restart();
            Logger.Log($"Starting GetProgressFromFfmpegLog() loop for log '{logFile}', target is {target} frames", true);
            UpdateInterpProgress(0, targetFrames);

            while (Program.busy)
            {
                if (!progressPaused && AiProcess.processTime.IsRunning)
                {
                    string lastLogLine = Logger.GetSessionLogLastLines(logFile, 1).Where(x => x.Contains("frame=")).LastOrDefault();
                    int num = lastLogLine == null ? 0 : lastLogLine.Split("frame=")[1].Split("fps=")[0].GetInt();

                    if(num > 0)
                        UpdateInterpProgress(num, targetFrames);

                    await Task.Delay(500);

                    if (num >= targetFrames)
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

        public static int interpolatedInputFramesCount;
        public static float peakFpsOut;

        public static int previewUpdateRateMs = 200;

        public static void UpdateInterpProgress(int frames, int target, string latestFramePath = "")
        {
            if (I.canceled) return;
            interpolatedInputFramesCount = ((frames / I.currentSettings.interpFactor).RoundToInt() - 1);
            //ResumeUtils.Save();
            target = (target / Interpolate.InterpProgressMultiplier).RoundToInt();
            frames = frames.Clamp(0, target);
            int percent = (int)Math.Round(((float)frames / target) * 100f);
            Program.mainForm.SetProgress(percent);

            float generousTime = ((AiProcess.processTime.ElapsedMilliseconds - AiProcess.lastStartupTimeMs) / 1000f);
            float fps = ((float)frames / generousTime).Clamp(0, 9999);
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
                    Image img = IoUtils.GetImage(latestFramePath, false, false);
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
            string[] inputFrames = IoUtils.GetFilesSorted(I.currentSettings.framesFolder);

            for (int i = 0; i < inputFrames.Length; i++)
            {
                while (Program.busy && (i + 10) > interpolatedInputFramesCount) await Task.Delay(1000);
                if (!Program.busy) break;

                if (i != 0 && i != inputFrames.Length - 1)
                    IoUtils.OverwriteFileWithText(inputFrames[i]);

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
