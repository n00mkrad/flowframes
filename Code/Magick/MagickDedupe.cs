using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.UI;
using Microsoft.VisualBasic.Devices;
using ImageMagick;
using Flowframes.OS;
using Flowframes.Data;

namespace Flowframes.Magick
{
    class MagickDedupe
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
                await RemoveDupeFrames(path, currentThreshold, "png", testRun, true, (currentMode == Mode.Auto));
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

            int currentOutFrame = 1;
            int currentDupeCount = 0;

            int statsFramesKept = 0;
            int statsFramesDeleted = 0;

            int skipAfterNoDupesFrames = Config.GetInt("autoDedupFrames");
            bool hasEncounteredAnyDupes = false;
            bool skipped = false;

            bool hasReachedEnd = false;

            for (int i = 0; i < framePaths.Length; i++)     // Loop through frames
            {
                if (hasReachedEnd)
                    break;

                Logger.Log("Base Frame:  #" + i);
                //int thisFrameDupeCount = 0;

                string frame1 = framePaths[i].FullName;
                //if (!File.Exists(framePaths[i].FullName))   // Skip if file doesn't exist (already deleted / used to be a duped frame)
                //    continue;

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
                        Logger.Log($"Frame {compareWithIndex} was already deleted - skipping");
                        compareWithIndex++;
                    }
                    else
                    {
                        //if (compareWithIndex >= framePaths.Length)
                        //    hasReachedEnd = true;

                        Logger.Log("Compare With:  #" + compareWithIndex);

                        string frame2 = framePaths[compareWithIndex].FullName;
                        // if (oldIndex >= 0)
                        //     i = oldIndex;

                        float diff = GetDifference(frame1, frame2);
                        Logger.Log("Diff: " + diff);

                        string delStr = "Keeping";
                        if (diff < threshold)     // Is a duped frame.
                        {
                            if (!testRun)
                            {
                                delStr = "Deleting";
                                //File.Delete(frame2);
                                framesToDelete.Add(frame2);
                                if (debugLog) Logger.Log("[FrameDedup] Deleted " + Path.GetFileName(frame2));
                                hasEncounteredAnyDupes = true;
                            }
                            statsFramesDeleted++;
                            currentDupeCount++;
                            Logger.Log($"Frame {i} has {currentDupeCount} dupes");
                        }
                        else
                        {
                            statsFramesKept++;
                            currentOutFrame++;
                            currentDupeCount = 0;
                            break;
                        }

                        if (i % 15 == 0 || true)
                        {
                            Logger.Log($"[FrameDedup] Difference from {Path.GetFileName(frame1)} to {Path.GetFileName(frame2)}: {diff.ToString("0.00")}% - {delStr}. Total: {statsFramesKept} kept / {statsFramesDeleted} deleted.", false, true);
                            Program.mainForm.SetProgress((int)Math.Round(((float)i / framePaths.Length) * 100f));
                            if (imageCache.Count > 750 || (imageCache.Count > 50 && OSUtils.GetFreeRamMb() < 2500))
                                ClearCache();
                        }
                    }
                }

                // int oldIndex = -1;
                // if (i >= framePaths.Length)    // If this is the last frame, compare with 1st to avoid OutOfRange error
                // {
                //     oldIndex = i;
                //     i = 0;
                // }

                // while (!File.Exists(framePaths[i+1].FullName))   // If frame2 doesn't exist, keep stepping thru the array
                // {
                //     if (i >= framePaths.Length)
                //         break;
                //     i++;
                // }

                

                if(i % 5 == 0)
                    await Task.Delay(1);

                if (Interpolate.canceled) return;

                foreach (string frame in framesToDelete)
                    IOUtils.TryDeleteIfExists(frame);

                if (!testRun && skipIfNoDupes && !hasEncounteredAnyDupes && skipAfterNoDupesFrames > 0 && i >= skipAfterNoDupesFrames)
                {
                    skipped = true;
                    break;
                }
            }

            string testStr = testRun ? " [TestRun]" : "";

            if (Interpolate.canceled) return;
            if (skipped)
            {
                Logger.Log($"[FrameDedup] First {skipAfterNoDupesFrames} frames did not have any duplicates - Skipping the rest!", false, true);
            }
            else
            {
                Logger.Log($"[FrameDedup]{testStr} Done. Kept {statsFramesKept} frames, deleted {statsFramesDeleted} frames.", false, true);
            }

            if (statsFramesKept <= 0)
                Interpolate.Cancel("No frames were left after de-duplication.");
        }

        static float GetDifference (string img1Path, string img2Path)
        {
            MagickImage img2 = GetImage(img2Path);
            MagickImage img1 = GetImage(img1Path);

            double err = img1.Compare(img2, ErrorMetric.Fuzz);
            float errPercent = (float)err * 100f;
            return errPercent;
        }
    }
}
