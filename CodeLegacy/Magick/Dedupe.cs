using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Flowframes.IO;
using ImageMagick;
using Flowframes.Os;

namespace Flowframes.Magick
{
    class Dedupe
    {
        public enum Mode { None, Info, Enabled, Auto }
        public static Mode currentMode;
        public static float currentThreshold;

        public static async Task Run(string path, bool testRun = false, bool setStatus = true)
        {
            if (path == null || !Directory.Exists(path) || Interpolate.canceled)
                return;

            currentMode = Mode.Auto;

            if (setStatus)
                Program.mainForm.SetStatus("Running frame de-duplication");

            currentThreshold = Config.GetFloat(Config.Key.dedupThresh);
            Logger.Log("Running accurate frame de-duplication...");

            if (currentMode == Mode.Enabled || currentMode == Mode.Auto)
                await RemoveDupeFrames(path, currentThreshold, "*", testRun, false, (currentMode == Mode.Auto));
        }

        static MagickImage GetImage(string path)
        {
            return new MagickImage(path);
        }

        public static async Task RemoveDupeFrames(string path, float threshold, string ext, bool testRun = false, bool debugLog = false, bool skipIfNoDupes = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            Logger.Log("Removing duplicate frames - Threshold: " + threshold.ToString("0.00"));

            FileInfo[] framePaths = IoUtils.GetFileInfosSorted(path, false, "*." + ext);
            List<string> framesToDelete = new List<string>();

            int statsFramesKept = framePaths.Length > 0 ? 1 : 0; // always keep at least one frame
            int statsFramesDeleted = 0;

            Mutex mtx_framesToDelete = new Mutex();
            Mutex mtx_debugLog = new Mutex();
            Task[] workTasks = new Task[Environment.ProcessorCount];

            bool threadAbort = false;

            Action<int, int> lamProcessFrames = (indStart, indEnd) =>
            {
                MagickImage img1 = null;
                MagickImage img2 = null;

                for (int i = indStart; i < indEnd; i++)     // Loop through frames
                {
                    string frame1_name = framePaths[i].FullName;

                    // its likely we carried over an already loaded image from a previous iteration
                    if (!(img1 != null && img1.FileName == frame1_name))
                        img1 = GetImage(framePaths[i].FullName);

                    if (img1 == null) continue;

                    for (int j = i + 1; j < framePaths.Length; j++)
                    {
                        if (threadAbort || Interpolate.canceled) return;

                        //if (j % 3 == 0)
                        //await Task.Delay(1);

                        string frame2_name = framePaths[j].FullName;

                        if (j >= indEnd)
                        {
                            // if we are already extending outside of this thread's range and j is already flagged, then we need to abort
                            bool isFlaggedForDeletion = false;
                            mtx_framesToDelete.WaitOne();
                            isFlaggedForDeletion = framesToDelete.Contains(frame2_name);
                            mtx_framesToDelete.ReleaseMutex();
                            if (isFlaggedForDeletion)
                                return;
                        }

                        img2 = GetImage(framePaths[j].FullName);
                        if (img2 == null) continue;


                        float diff = GetDifference(img1, img2);

                        if (diff < threshold)     // Is a duped frame.
                        {
                            if (!testRun)
                            {
                                mtx_framesToDelete.WaitOne();
                                framesToDelete.Add(frame2_name);
                                mtx_framesToDelete.ReleaseMutex();
                                if (debugLog)
                                {
                                    mtx_debugLog.WaitOne();
                                    Logger.Log("Deduplication: Deleted " + Path.GetFileName(frame2_name));
                                    mtx_debugLog.ReleaseMutex();
                                }
                            }

                            Interlocked.Increment(ref statsFramesDeleted);

                            if (j + 1 == framePaths.Length)
                                return;

                            continue; // test next frame
                        }


                        Interlocked.Increment(ref statsFramesKept);

                        // this frame is different, stop testing agaisnt 'i'
                        // all the frames between i and j are dupes, we can skip them
                        i = j - 1;
                        // keep the currently loaded in img for the next iteration
                        img1 = img2;
                        break;
                    }
                }
            };

            Action lamUpdateInfoBox = () =>
            {
                int framesProcessed = statsFramesKept + statsFramesDeleted;
                Logger.Log($"Deduplication: Running de-duplication ({framesProcessed}/{framePaths.Length}), deleted {statsFramesDeleted} ({(((float)statsFramesDeleted / framePaths.Length) * 100f).ToString("0")}%) duplicate frames so far...", false, true);
                Program.mainForm.SetProgress((int)Math.Round(((float)framesProcessed / framePaths.Length) * 100f));
            };

            // start the worker threads
            for (int i = 0; i < workTasks.Length; i++)
            {
                int chunkSize = framePaths.Length / workTasks.Length;
                int indStart = chunkSize * i;
                int indEnd = indStart + chunkSize;
                if (i + 1 == workTasks.Length) indEnd = framePaths.Length;

                workTasks[i] = Task.Run(() => lamProcessFrames(indStart, indEnd));
            }

            // wait for all the worker threads to finish and update the info box
            while (!Interpolate.canceled)
            {
                await Task.Delay(5);


                bool anyThreadStillWorking = false;
                // wait for the threads to finish
                for (int i = 0; i < workTasks.Length; i++)
                {
                    if (!workTasks[i].IsCompleted) anyThreadStillWorking = true;
                }

                if (sw.ElapsedMilliseconds >= 250 || !anyThreadStillWorking)   // Print every 0.25s (or when done)
                {
                    sw.Restart();
                    lamUpdateInfoBox();
                }

                if (!anyThreadStillWorking) break;
            }

            threadAbort = true;
            for (int i = 0; i < workTasks.Length; i++)
                await workTasks[i];

            lamUpdateInfoBox();

            // int oldIndex = -1; // TODO: Compare with 1st to fix loops?
            // if (i >= framePaths.Length)    // If this is the last frame, compare with 1st to avoid OutOfRange error
            // {
            //     oldIndex = i;
            //     i = 0;
            // }

            foreach (string frame in framesToDelete)
                IoUtils.TryDeleteIfExists(frame);

            string testStr = testRun ? "[TESTRUN] " : "";

            if (Interpolate.canceled) return;

            int framesLeft = IoUtils.GetAmountOfFiles(path, false, "*" + Interpolate.currentSettings.framesExt);
            int framesDeleted = framePaths.Length - framesLeft;
            float percentDeleted = ((float)framesDeleted / framePaths.Length) * 100f;
            string keptPercent = $"{(100f - percentDeleted).ToString("0.0")}%";

            if (framesDeleted <= 0)
            {
                Logger.Log($"Deduplication: No duplicate frames detected on this video.", false, true);
            }
            else if (statsFramesKept <= 0)
            {
                Interpolate.Cancel("No frames were left after de-duplication!\n\nTry lowering the de-duplication threshold.");
            }
            else
            {
                Logger.Log($"{testStr}Deduplication: Kept {framesLeft} ({keptPercent}) frames, deleted {framesDeleted} frames.", false, true);
            }
        }
        static float GetDifference(MagickImage img1, MagickImage img2)
        {
            double err = img1.Compare(img2, ErrorMetric.Fuzz);
            float errPercent = (float)err * 100f;
            return errPercent;
        }

        static float GetDifference(string img1Path, string img2Path)
        {
            return GetDifference(GetImage(img1Path), GetImage(img2Path));
        }

        public static async Task CreateDupesFile(string framesPath, string ext)
        {
            bool debug = true; Config.GetBool("dupeScanDebug", false);

            FileInfo[] frameFiles = IoUtils.GetFileInfosSorted(framesPath, false, "*" + ext);

            if (debug)
                Logger.Log($"Running CreateDupesFile for '{framesPath}' ({frameFiles.Length} files), ext = {ext}.", true, false, "dupes");

            Dictionary<string, List<string>> frames = new Dictionary<string, List<string>>();


            for (int i = 0; i < frameFiles.Length; i++)
            {
                bool isLastItem = (i + 1) == frameFiles.Length;

                String fnameCur = Path.GetFileNameWithoutExtension(frameFiles[i].Name);
                int frameNumCur = fnameCur.GetInt();

                frames[fnameCur] = new List<string>();

                if (!isLastItem)
                {
                    String fnameNext = Path.GetFileNameWithoutExtension(frameFiles[i + 1].Name);
                    int frameNumNext = fnameNext.GetInt();

                    for (int j = frameNumCur + 1; j < frameNumNext; j++)
                    {
                        frames[fnameCur].Add(j.ToString().PadLeft(9, '0'));
                    }
                }
            }

            // string tempDir = 
            // File.WriteAllText(Path.Combine(framesPath.GetParentDir(), "dupes.json"), frames.ToJson(true));
        }

        public static async Task CreateFramesFileVideo(string videoPath, bool loop)
        {
            Directory.CreateDirectory(Interpolate.currentSettings.tempFolder);
            Process ffmpeg = OsUtils.NewProcess(true);
            string baseCmd = $"/C cd /D {Path.Combine(IO.Paths.GetPkgPath(), IO.Paths.audioVideoDir).Wrap()}";
            string mpDec = FfmpegCommands.GetMpdecimate(wrap: false); // FfmpegCommands.GetMpdecimate((int)FfmpegCommands.MpDecSensitivity.Normal, false);
            ffmpeg.StartInfo.Arguments = $"{baseCmd} & ffmpeg -loglevel debug -y {videoPath.GetConcStr(Interpolate.currentSettings.inFps.Float)} -i {videoPath.Wrap()} -fps_mode vfr -vf {mpDec} -f null NUL 2>&1 | findstr keep_count:";
            var ffmpegOutputLines = (await Task.Run(() => OsUtils.GetProcStdOut(ffmpeg, true))).SplitIntoLines();
            ffmpegOutputLines = ffmpegOutputLines.Where(l => l.Contains("keep_count")).Select(l => l.Split(']').Last()).ToArray();
            var test = string.Join("\n", ffmpegOutputLines);

            var frames = new Dictionary<int, List<int>>();
            int frameCount = 0;
            int lastKeepFrameNum = 0;

            for (int frameIdx = 0; frameIdx < ffmpegOutputLines.Length; frameIdx++)
            {
                string line = ffmpegOutputLines[frameIdx];

                if (line.Contains(" keep pts:"))
                {
                    if (!frames.ContainsKey(frameIdx) || frames[frameIdx] == null)
                    {
                        frames[frameIdx] = new List<int>();
                    }

                    lastKeepFrameNum = frameIdx;
                    frameCount++;
                }
                else if (line.Contains(" drop pts:"))
                {
                    frames[lastKeepFrameNum].Add(frameIdx);
                    frameCount++;
                }
            }

            var inputFrames = new List<int>(frames.Keys);
            float keepPercentage = (float)inputFrames.Count / frameCount * 100f;
            Logger.Log($"Deduplication: Kept {inputFrames.Count}/{frameCount} frames ({keepPercentage.ToString("0.#")}%)");

            if(Interpolate.currentSettings != null && Interpolate.currentSettings.interpFactor == 1)
            {
                Interpolate.currentSettings.interpFactor = ((float)frameCount / inputFrames.Count);
                Logger.Log($"Dedupe: Factor is 1, will not redupe; overriding factor to {frameCount}/{inputFrames.Count} = {Interpolate.currentSettings.interpFactor.ToString("0.######")}", true);
            }

            if (inputFrames.Count > 240 && keepPercentage > 85f)
            {
                Logger.Log("Deduplication: Less than 15% duplicate frames detected; disabling for this video.");
                Interpolate.currentSettings.dedupe = false;
            }

            if (loop)
            {
                inputFrames.Add(inputFrames.First());
            }

            File.WriteAllText(Path.Combine(Interpolate.currentSettings.tempFolder, "input.json"), inputFrames.ToJson(true));

            if (Interpolate.currentSettings.dedupe)
            {
                File.WriteAllText(Path.Combine(Interpolate.currentSettings.tempFolder, "dupes.test.json"), frames.ToJson(true));
            }
        }
    }
}
