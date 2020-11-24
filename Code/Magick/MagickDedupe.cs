using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flowframes.IO;
using Flowframes.Main;
using ImageMagick;

namespace Flowframes.Magick
{
    class MagickDedupe
    {
        public enum Mode { None, Info, Enabled, Auto }
        public static Mode currentMode;
        public static float currentThreshold;

        public static async Task Run(string path, bool testRun = false, bool setStatus = true)
        {
            UpdateCurrentMode();

            if (currentMode == Mode.None)
                return;

            Program.mainForm.SetStatus("Running frame de-duplication");

            currentThreshold = Config.GetFloat("dedupThresh");
            Logger.Log("Running frame de-duplication with mode " + currentMode.ToString().Wrap());

            if (currentMode == Mode.Enabled || currentMode == Mode.Auto)
                await RemoveDupeFrames(path, currentThreshold, "png", testRun, false, (currentMode == Mode.Auto));
        }

        static void UpdateCurrentMode ()
        {
            switch (Config.GetInt("dedupMode"))
            {
                case 0: currentMode = Mode.None; break;
                case 1: currentMode = Mode.Enabled; break;
                case 2: currentMode = Mode.Auto; break;
            }
        }


        public static async Task RemoveDupeFrames(string path, float threshold, string ext, bool testRun = false, bool debugLog = false, bool skipIfNoDupes = false)
        {
            Logger.Log("Removing duplicate frames - Threshold: " + threshold.ToString("0.00"));
            //Logger.Log("Analyzing frames...");
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            FileInfo[] framePaths = dirInfo.GetFiles("*." + ext, SearchOption.TopDirectoryOnly);

            Dictionary<int, int> framesDupesDict = new Dictionary<int, int>();

            int currentOutFrame = 1;
            int currentDupeCount = 0;
            string dupeInfoFile = Path.Combine(path, "..", "dupes.ini");
            int lastFrameNum = 0;

            int statsFramesKept = 0;
            int statsFramesDeleted = 0;

            IOUtils.TryDeleteIfExists(dupeInfoFile);

            bool loopMode = Config.GetBool("enableLoop");

            int skipAfterNoDupesFrames = Config.GetInt("autoDedupFrames");
            bool hasEncounteredAnyDupes = false;
            bool skipped = false;

            int i = 0;
            while (i < framePaths.Length)
            {
                string frame1 = framePaths[i].FullName;
                if (!File.Exists(framePaths[i].FullName))   // Skip if file doesn't exist (used to be a duped frame)
                {
                    i++;
                    continue;
                }

                i++;
                string frame2;

                int oldIndex = -1;
                if (i >= framePaths.Length)    // If this is the last frame, compare with 1st to avoid OutOfRange error
                {
                    if (loopMode)
                    {
                        framesDupesDict = UpdateDupeDict(framesDupesDict, currentOutFrame, 0);
                        break;
                    }
                    oldIndex = i;
                    i = 0;
                }

                while (!File.Exists(framePaths[i].FullName))   // If frame2 doesn't exist, keep stepping thru the array
                {
                    if (i >= framePaths.Length)
                        break;
                    i++;
                }

                frame2 = framePaths[i].FullName;
                if (oldIndex >= 0)
                    i = oldIndex;
                MagickImage img1 = new MagickImage(frame1);
                MagickImage img2 = new MagickImage(frame2);
                double err = img1.Compare(img2, ErrorMetric.Fuzz);
                float errPercent = (float)err * 100f;

                if (debugLog) Logger.Log("[dedup] current in frame: " + i);
                if (debugLog) Logger.Log("[dedup] current out frame: " + currentOutFrame);

                framesDupesDict = UpdateDupeDict(framesDupesDict, currentOutFrame, currentDupeCount);

                lastFrameNum = currentOutFrame;

                string delStr = "Keeping";
                if (errPercent < threshold)     // Is a duped frame.
                {
                    if (!testRun)
                    {
                        delStr = "Deleting";
                        File.Delete(frame1);
                        if(debugLog) Logger.Log("[FrameDedup] Deleted " + Path.GetFileName(frame1));
                        hasEncounteredAnyDupes = true;
                        i--;    // Turn the index back so we compare the same frame again, this time to the next one after the deleted frame
                    }
                    statsFramesDeleted++;
                    currentDupeCount++;
                }
                else
                {
                    statsFramesKept++;
                    currentOutFrame++;
                    currentDupeCount = 0;
                }

                Logger.Log($"[FrameDedup] Difference from {Path.GetFileName(img1.FileName)} to {Path.GetFileName(img2.FileName)}: {errPercent.ToString("0.00")}% - {delStr}. Total: {statsFramesKept} kept / {statsFramesDeleted} deleted.", false, true);

                img1.Dispose();
                img2.Dispose();

                Program.mainForm.SetProgress((int)Math.Round(((float)i / framePaths.Length) * 100f));
                await Task.Delay(10);

                if(!testRun && skipIfNoDupes && !hasEncounteredAnyDupes && i >= skipAfterNoDupesFrames)
                {
                    skipped = true;
                    break;
                }
            }

            string testStr = "";
            if (testRun) testStr = " [TestRun]";

            if (Interpolate.cancelled) return;
            if (skipped)
            {
                Logger.Log($"[FrameDedup] First {skipAfterNoDupesFrames} frames did not have any duplicates - Skipping the rest!");
            }
            else
            {
                if(!testRun)
                    File.WriteAllLines(dupeInfoFile, framesDupesDict.Select(x => "frm" + x.Key + ":dup" + x.Value).ToArray());
                Logger.Log($"[FrameDedup]{testStr} Done. Kept {statsFramesKept} frames, deleted {statsFramesDeleted} frames.", false, true);
            }

            RenameCounterDir(path, "png");
            ZeroPadDir(path, ext, 8);
        }

        static Dictionary<int, int> UpdateDupeDict(Dictionary<int, int> dict, int frame, int amount)
        {
            if (dict.ContainsKey(frame))
                dict[frame] = amount;
            else
                dict.Add(frame, amount);
            return dict;
        }

        public static async Task Reduplicate(string path, bool debugLog = false)
        {
            if (currentMode == Mode.None)
                return;

            string ext = InterpolateUtils.lastExt;

            string dupeInfoFile = Path.Combine(Interpolate.currentTempDir, "dupes.ini");
            if (!File.Exists(dupeInfoFile)) return;

            Logger.Log("Re-Duplicating frames to fix timing...");
            RenameCounterDir(path, ext);
            ZeroPadDir(path, ext, 8);

            string[] dupeFrameLines = IOUtils.ReadLines(dupeInfoFile);
            string tempSubFolder = Path.Combine(path, "temp");
            Directory.CreateDirectory(tempSubFolder);

            int interpFramesPerRealFrame = Interpolate.interpFactor - 1;

            int sourceFrameNum = 0;
            int outFrameNum = 1;

            for (int i = 0; i < dupeFrameLines.Length; i++)
            {
                string line = dupeFrameLines[i];
                sourceFrameNum++;

                string paddedFilename = "";
                string sourceFramePath = "";

                string[] kvp = line.Split(':');
                int currentInFrame = kvp[0].GetInt();
                int currentDupesAmount = kvp[1].GetInt();

                // Copy Source Frame
                paddedFilename = sourceFrameNum.ToString().PadLeft(8, '0') + $".{ext}";
                sourceFramePath = Path.Combine(path, paddedFilename);
                if(debugLog) Logger.Log("[Source] Moving " + Path.GetFileName(sourceFramePath) + " => " + outFrameNum + $".{ext}");
                if (!TryCopy(sourceFramePath, Path.Combine(tempSubFolder, outFrameNum + $".{ext}")))
                    break;
                outFrameNum++;

                // Insert dupes for source frame
                for (int copyTimes = 0; copyTimes < currentDupesAmount; copyTimes++)
                {
                    paddedFilename = sourceFrameNum.ToString().PadLeft(8, '0') + $".{ext}";
                    sourceFramePath = Path.Combine(path, paddedFilename);
                    if (debugLog) Logger.Log("[Source Dupes] Moving " + Path.GetFileName(sourceFramePath) + " => " + outFrameNum + $".{ext}");
                    if (!TryCopy(sourceFramePath, Path.Combine(tempSubFolder, outFrameNum + $".{ext}")))
                        break;
                    outFrameNum++;
                }

                if (i == dupeFrameLines.Length - 1)         // Break loop if this is the last input frame (as it has no interps)
                    break;

                for(int interpFrames = 0; interpFrames < interpFramesPerRealFrame; interpFrames++)
                {
                    sourceFrameNum++;

                    // Copy Interp Frame
                    paddedFilename = sourceFrameNum.ToString().PadLeft(8, '0') + $".{ext}";
                    sourceFramePath = Path.Combine(path, paddedFilename);
                    if (debugLog) Logger.Log("[Interp] Moving " + Path.GetFileName(sourceFramePath) + " => " + outFrameNum + $".{ext}");
                    if (!TryCopy(sourceFramePath, Path.Combine(tempSubFolder, outFrameNum + $".{ext}")))
                        break;
                    outFrameNum++;

                    // Insert dupes for interp frame
                    for (int copyTimes = 0; copyTimes < currentDupesAmount; copyTimes++)
                    {
                        paddedFilename = sourceFrameNum.ToString().PadLeft(8, '0') + $".{ext}";
                        sourceFramePath = Path.Combine(path, paddedFilename);
                        if (debugLog) if (debugLog) Logger.Log("[Interp Dupes] Moving " + Path.GetFileName(sourceFramePath) + " => " + outFrameNum + $".{ext}");
                        if (!TryCopy(sourceFramePath, Path.Combine(tempSubFolder, outFrameNum + $".{ext}")))
                            break;
                        outFrameNum++;
                    }
                }
            }
            ZeroPadDir(tempSubFolder, ext, 8);

            foreach (FileInfo file in new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.TopDirectoryOnly))
                file.Delete();

            foreach (FileInfo file in new DirectoryInfo(tempSubFolder).GetFiles($"*.{ext}", SearchOption.TopDirectoryOnly))
                file.MoveTo(Path.Combine(path, file.Name));
        }

        static bool TryCopy(string source, string target)
        {
            try
            {
                File.Copy(source, target);
            }
            catch
            {
                return false;
            }
            return true;
        }

        static bool TryMove(string source, string target)
        {
            try
            {
                if (File.Exists(target))
                    File.Delete(target);
                File.Move(source, target);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static void RenameCounterDir(string path, string ext, int sortMode = 0)
        {
            int counter = 1;
            FileInfo[] files = new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.TopDirectoryOnly);
            var filesSorted = files.OrderBy(n => n);

            if (sortMode == 1)
                filesSorted.Reverse();

            foreach (FileInfo file in files)
            {
                string dir = new DirectoryInfo(file.FullName).Parent.FullName;
                int filesDigits = (int)Math.Floor(Math.Log10((double)files.Length) + 1);
                File.Move(file.FullName, Path.Combine(dir, counter.ToString()/*.PadLeft(filesDigits, '8')*/ + Path.GetExtension(file.FullName)));
                counter++;
            }
        }

        static void ZeroPadDir(string path, string ext, int targetLength, bool recursive = false)
        {
            FileInfo[] files;
            if (recursive)
                files = new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.AllDirectories);
            else
                files = new DirectoryInfo(path).GetFiles($"*.{ext}", SearchOption.TopDirectoryOnly);

            foreach (FileInfo file in files)
            {
                string fnameNoExt = Path.GetFileNameWithoutExtension(file.Name);
                string ext2 = Path.GetExtension(file.Name); ;
                File.Move(file.FullName, Path.Combine(Path.GetDirectoryName(file.FullName), fnameNoExt.PadLeft(targetLength, '0') + ext2));
            }
        }
    }
}
