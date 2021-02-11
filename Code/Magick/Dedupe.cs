using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flowframes.IO;
using ImageMagick;
using Flowframes.OS;
using Flowframes.Data;
using System.Drawing;
using Paths = Flowframes.IO.Paths;

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

            if(setStatus)
                Program.mainForm.SetStatus("Running frame de-duplication");

            currentThreshold = Config.GetFloat("dedupThresh");
            Logger.Log("Running accurate frame de-duplication...");

            if (currentMode == Mode.Enabled || currentMode == Mode.Auto)
                await RemoveDupeFrames(path, currentThreshold, "png", testRun, false, (currentMode == Mode.Auto));
        }

        public static Dictionary<string, MagickImage> imageCache = new Dictionary<string, MagickImage>();
        static MagickImage GetImage(string path)
        {
            bool allowCaching = true;

            if (!allowCaching)
                return new MagickImage(path);

            if (!imageCache.ContainsKey(path))
                imageCache.Add(path, new MagickImage(path));

            return imageCache[path];
        }

        public static void ClearCache ()
        {
            imageCache.Clear();
        }

        public static async Task RemoveDupeFrames(string path, float threshold, string ext, bool testRun = false, bool debugLog = false, bool skipIfNoDupes = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            Logger.Log("Removing duplicate frames - Threshold: " + threshold.ToString("0.00"));

            FileInfo[] framePaths = IOUtils.GetFileInfosSorted(path, false, "*." + ext);
            List<string> framesToDelete = new List<string>();

            int bufferSize = await GetBufferSize();

            int currentOutFrame = 1;
            int currentDupeCount = 0;

            int statsFramesKept = 0;
            int statsFramesDeleted = 0;

            int skipAfterNoDupesFrames = Config.GetInt("autoDedupFrames");
            bool hasEncounteredAnyDupes = false;
            bool skipped = false;

            bool hasReachedEnd = false;

            string fileContent = "";

            for (int i = 0; i < framePaths.Length; i++)     // Loop through frames
            {
                if (hasReachedEnd)
                    break;

                string frame1 = framePaths[i].FullName;

                int compareWithIndex = i + 1;

                while (true)   // Loop dupes
                {
                    //compareWithIndex++;
                    if (compareWithIndex >= framePaths.Length)
                    {
                        hasReachedEnd = true;
                        break;
                    }

                    if (framesToDelete.Contains(framePaths[compareWithIndex].FullName) || !File.Exists(framePaths[compareWithIndex].FullName))
                    {
                        //Logger.Log($"Frame {compareWithIndex} was already deleted - skipping");
                        compareWithIndex++;
                    }
                    else
                    {
                        string frame2 = framePaths[compareWithIndex].FullName;
                        float diff = GetDifference(frame1, frame2);

                        if (diff < threshold)     // Is a duped frame.
                        {
                            if (!testRun)
                            {
                                framesToDelete.Add(frame2);
                                if (debugLog) Logger.Log("[Deduplication] Deleted " + Path.GetFileName(frame2));
                                hasEncounteredAnyDupes = true;
                            }
                            statsFramesDeleted++;
                            currentDupeCount++;
                        }
                        else
                        {
                            fileContent += $"{Path.GetFileNameWithoutExtension(framePaths[i].Name)}:{currentDupeCount}\n";
                            statsFramesKept++;
                            currentOutFrame++;
                            currentDupeCount = 0;
                            break;
                        }
                    }
                }

                if (sw.ElapsedMilliseconds >= 500 || (i + 1) == framePaths.Length)   // Print every 0.5s (or when done)
                {
                    sw.Restart();
                    Logger.Log($"[Deduplication] Running de-duplication ({i}/{framePaths.Length}), deleted {statsFramesDeleted} ({(((float)statsFramesDeleted / framePaths.Length) * 100f).ToString("0")}%) duplicate frames so far...", false, true);
                    Program.mainForm.SetProgress((int)Math.Round(((float)i / framePaths.Length) * 100f));
                    if (imageCache.Count > bufferSize || (imageCache.Count > 50 && OSUtils.GetFreeRamMb() < 3500))
                        ClearCache();
                }

                // int oldIndex = -1; // TODO: Compare with 1st to fix loops?
                // if (i >= framePaths.Length)    // If this is the last frame, compare with 1st to avoid OutOfRange error
                // {
                //     oldIndex = i;
                //     i = 0;
                // }

                if (i % 3 == 0)
                    await Task.Delay(1);

                if (Interpolate.canceled) return;

                if (!testRun && skipIfNoDupes && !hasEncounteredAnyDupes && skipAfterNoDupesFrames > 0 && i >= skipAfterNoDupesFrames)
                {
                    skipped = true;
                    break;
                }
            }

            foreach (string frame in framesToDelete)
                IOUtils.TryDeleteIfExists(frame);

            string testStr = testRun ? " [TestRun]" : "";

            if (Interpolate.canceled) return;

            int framesLeft = IOUtils.GetAmountOfFiles(path, false, $"*.png");
            int framesDeleted = framePaths.Length - framesLeft;
            float percentDeleted = ((float)framesDeleted / framePaths.Length) * 100f;
            string keptPercent = $"{(100f - percentDeleted).ToString("0.0")}%";

            if (skipped)
                Logger.Log($"[Deduplication] First {skipAfterNoDupesFrames} frames did not have any duplicates - Skipping the rest!", false, true);
            else
                Logger.Log($"[Deduplication]{testStr} Done. Kept {framesLeft} ({keptPercent}) frames, deleted {framesDeleted} frames.", false, true);

            if (statsFramesKept <= 0)
                Interpolate.Cancel("No frames were left after de-duplication!\n\nTry decreasing the de-duplication threshold.");
        }

        static float GetDifference (string img1Path, string img2Path)
        {
            MagickImage img2 = GetImage(img2Path);
            MagickImage img1 = GetImage(img1Path);

            double err = img1.Compare(img2, ErrorMetric.Fuzz);
            float errPercent = (float)err * 100f;
            return errPercent;
        }

        static async Task<int> GetBufferSize ()
        {
            Size res = await Interpolate.current.GetScaledRes();
            long pixels = res.Width * res.Height;    // 4K = 8294400, 1440p = 3686400, 1080p = 2073600, 720p = 921600, 540p = 518400, 360p = 230400
            int bufferSize = 100;
            if (pixels < 518400) bufferSize = 1800;
            if (pixels >= 518400) bufferSize = 1400;
            if (pixels >= 921600) bufferSize = 800;
            if (pixels >= 2073600) bufferSize = 400;
            if (pixels >= 3686400) bufferSize = 200;
            if (pixels >= 8294400) bufferSize = 100;
            if (pixels == 0) bufferSize = 100;
            Logger.Log($"Using magick dedupe buffer size {bufferSize} for frame resolution {res.Width}x{res.Height}", true);
            return bufferSize;
        }

        public static async Task CreateDupesFile (string framesPath, int lastFrameNum)
        {
            string infoFile = Path.Combine(framesPath.GetParentDir(), $"dupes.ini");
            string fileContent = "";

            FileInfo[] frameFiles = IOUtils.GetFileInfosSorted(framesPath, false, "*.png");

            for(int i = 0; i < frameFiles.Length; i++)
            {
                bool isLastItem = (i + 1) == frameFiles.Length;

                int frameNum1 = frameFiles[i].Name.GetInt();
                int frameNum2 = isLastItem ? lastFrameNum : frameFiles[i+1].Name.GetInt();

                int diff = frameNum2 - frameNum1;
                int dupes = diff - 1;

                //if (File.Exists(Path.Combine(framesPath.GetParentDir(), Paths.scenesDir, frameFiles[i].Name)))
                //    dupes = 0;

                fileContent += $"{Path.GetFileNameWithoutExtension(frameFiles[i].Name)}:{dupes}\n";
            }

            File.WriteAllText(infoFile, fileContent);
        }
    }
}
