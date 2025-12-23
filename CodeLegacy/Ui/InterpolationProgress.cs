using Flowframes.IO;
using Flowframes.MiscUtils;
using System;
using System.Diagnostics;
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
            LastFps = 0f;
            _framesAtTime = null;
            _fpsRollAvg.Reset();
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
                    // if (firstProgUpd && Program.mainForm.IsInFocus())
                    //     Program.mainForm.SetTab(Program.mainForm.previewTab.Name);

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
                    string lastLogLine = Logger.GetSessionLogLastLines(logFile, 1)?.Where(x => x != null && x.Contains("frame=")).LastOrDefault();
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
        public static float LastFps;
        private static Tuple<int, DateTime> _framesAtTime = null;
        private static Utilities.RollingAverage<float> _fpsRollAvg = new Utilities.RollingAverage<float>(10);

        public static void UpdateInterpProgress(int frames, int target, string latestFramePath = "")
        {
            if (I.canceled) return;
            interpolatedInputFramesCount = ((frames / I.currentSettings.interpFactor).RoundToInt() - 1);
            //ResumeUtils.Save();
            target = (target / I.InterpProgressMultiplier).RoundToInt();
            frames = frames.Clamp(0, target);

            if (_framesAtTime == null)
            {
                _framesAtTime = new Tuple<int, DateTime>(frames, DateTime.Now);
            }

            if (frames > _framesAtTime.Item1 && frames > 0)
            {
                float fpsCurrent = (frames - _framesAtTime.Item1) / (float)(DateTime.Now - _framesAtTime.Item2).TotalSeconds;
                _fpsRollAvg.AddDataPoint(fpsCurrent);
                _framesAtTime = new Tuple<int, DateTime>(frames, DateTime.Now);
            }

            int percent = (((float)frames / target) * 100f).RoundToInt();
            Program.mainForm.SetProgress(percent);

            float fps = _fpsRollAvg.CurrentSize > 2 ? (float)_fpsRollAvg.Average : 0f;
            string fpsIn = (fps / currentFactor).ToString("0.0");
            string fpsOut = fps.ToString("0.0");
            LastFps = fps;

            float eta = fps == 0f ? 0f : (target - frames) * (1f / fps); // ETA = Remaining frames * seconds per frame. Set to 0 if FPS is 0 to avoid div. by zero
            string etaStr = eta > 3f ? $" - ETA: {FormatUtils.Time(TimeSpan.FromSeconds(eta), false)}" : "";
            string timeStr = AiProcess.processTime.ElapsedMilliseconds > 0 ? $" - Time: {FormatUtils.Time(AiProcess.processTime.Elapsed)}" : "";

            bool replaceLine = Logger.LastUiLine.MatchesWildcard("Interpolated*/* Frames *");

            string logStr = $"Interpolated {frames}/{target} Frames ({percent}%) - Speed: {fpsIn} FPS In / {fpsOut} FPS Out{timeStr}{etaStr}";
            if (AutoEncode.busy) logStr += " - Encoding...";
            Logger.Log(logStr, false, replaceLine);

            // try
            // {
            //     if (latestFramePath.IsNotEmpty() && frames > currentFactor)
            //     {
            //         if (bigPreviewForm == null && (preview == null || !preview.Visible)  /* ||Program.mainForm.WindowState != FormWindowState.Minimized */ /* || !Program.mainForm.IsInFocus()*/) return;        // Skip if the preview is not visible or the form is not in focus
            //     }
            // }
            // catch (Exception e)
            // {
            //     //Logger.Log("Error updating preview: " + e.Message, true);
            // }
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
    }
}
