using Flowframes.Data;
using Flowframes.IO;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class FrameTiming
    {
        public enum Mode { CFR, VFR }
        public static int timebase = 10000;

        public static async Task CreateTimecodeFiles(string framesPath, Mode mode, bool loopEnabled, int times, bool noTimestamps)
        {
            Logger.Log("Generating timecodes...");
            try
            {
                if (mode == Mode.VFR)
                    await CreateTimecodeFile(framesPath, loopEnabled, times, false, noTimestamps);
                if (mode == Mode.CFR)
                    await CreateEncFile(framesPath, loopEnabled, times, false);
                Logger.Log($"Generating timecodes... Done.", false, true);
            }
            catch (Exception e)
            {
                Logger.Log($"Error generating timecodes: {e.Message}");
            }
        }

        public static async Task CreateTimecodeFile(string framesPath, bool loopEnabled, int interpFactor, bool notFirstRun, bool noTimestamps)
        {
            if (Interpolate.canceled) return;
            Logger.Log($"Generating timecodes for {interpFactor}x...", false, true);

            if(noTimestamps)
                Logger.Log("Timestamps are disabled, using static frame rate.");

            bool sceneDetection = true;
            string ext = InterpolateUtils.GetOutExt();

            FileInfo[] frameFiles = new DirectoryInfo(framesPath).GetFiles($"*.png");
            string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-{interpFactor}x.ini");
            string fileContent = "";

            string scnFramesPath = Path.Combine(framesPath.GetParentDir(), Paths.scenesDir);
            string interpPath = Paths.interpDir;

            List<string> sceneFrames = new List<string>();
            if (Directory.Exists(scnFramesPath))
                sceneFrames = Directory.GetFiles(scnFramesPath).Select(file => Path.GetFileNameWithoutExtension(file)).ToList();

            float totalDuration = 0f;
            //int totalFrames = 0;
            int lastFrameDuration = 1;

            // Calculate time duration between frames
            int totalFileCount = 1;
            for (int i = 0; i < (frameFiles.Length - 1); i++)
            {
                if (Interpolate.canceled) return;

                int frameDuration = 100;   // Default for no timestamps in input filenames (divided by output fps later)

                if (!noTimestamps)  // Get timings from frame filenames
                {
                    string filename1 = frameFiles[i].Name;
                    string filename2 = frameFiles[i + 1].Name;
                    frameDuration = Path.GetFileNameWithoutExtension(filename2).GetInt() - Path.GetFileNameWithoutExtension(filename1).GetInt();
                }

                lastFrameDuration = frameDuration;
                float durationPerInterpFrame = (float)frameDuration / interpFactor;

                int interpFramesAmount = interpFactor;

                bool discardThisFrame = (sceneDetection && (i + 2) < frameFiles.Length && sceneFrames.Contains(Path.GetFileNameWithoutExtension(frameFiles[i + 1].Name)));     // i+2 is in scene detection folder, means i+1 is ugly interp frame

                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpFramesAmount = interpFramesAmount * 2;

                //Logger.Log("Writing out frames for in frame " + i, true);
                // Generate frames file lines
                for (int frm = 0; frm < interpFramesAmount; frm++)
                {
                    //Logger.Log($"Writing out frame {frm+1}/{interpFramesAmount}", true);

                    string durationStr = (durationPerInterpFrame / timebase).ToString("0.0000000", CultureInfo.InvariantCulture);

                    if (discardThisFrame && totalFileCount > 1)     // Never discard 1st frame
                    {
                        int lastNum = totalFileCount;

                        // Logger.Log($"Writing frame {totalFileCount} [Discarding Next]", true);
                        fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\nduration {durationStr}\n";
                        totalFileCount++;
                        totalDuration += durationPerInterpFrame;

                        // Logger.Log("Discarding interp frames with out num " + totalFileCount);
                        for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                        {
                            // Logger.Log($"Writing frame {totalFileCount} which is actually repeated frame {lastNum}");
                            fileContent += $"file '{interpPath}/{lastNum.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\nduration {durationStr}\n";
                            totalFileCount++;
                            totalDuration += durationPerInterpFrame;
                        }

                        frm = interpFramesAmount;
                    }
                    else
                    {
                        //Logger.Log($"Writing frame {totalFileCount}", true, false);
                        fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\nduration {durationStr}\n";
                        totalFileCount++;
                        totalDuration += durationPerInterpFrame;
                    }
                }

                if ((i + 1) % 100 == 0)
                    await Task.Delay(1);
            }

            // Use average frame duration for last frame - TODO: Use real duration??
            string durationStrLast = ((totalDuration / (totalFileCount - 1)) / timebase).ToString("0.0000000", CultureInfo.InvariantCulture);
            fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\nduration {durationStrLast}\n";
            totalFileCount++;

            File.WriteAllText(vfrFile, fileContent);

            if (notFirstRun) return;    // Skip all steps that only need to be done once

            if (Config.GetBool("enableLoop"))
            {
                int lastFileNumber = frameFiles.Last().Name.GetInt();
                lastFileNumber += lastFrameDuration;
                string loopFrameTargetPath = Path.Combine(frameFiles.First().FullName.GetParentDir(), lastFileNumber.ToString().PadLeft(Padding.inputFrames, '0') + $".png");
                if (File.Exists(loopFrameTargetPath))
                {
                    Logger.Log($"Won't copy loop frame - {Path.GetFileName(loopFrameTargetPath)} already exists.", true);
                    return;
                }
                File.Copy(frameFiles.First().FullName, loopFrameTargetPath);
            }
        }

        static Dictionary<string, int> dupesDict = new Dictionary<string, int>();

        static void LoadDupesFile (string path)
        {
            dupesDict.Clear();
            if (!File.Exists(path)) return;
            string[] dupesFileLines = IOUtils.ReadLines(path);
            foreach(string line in dupesFileLines)
            {
                string[] values = line.Split(':');
                dupesDict.Add(values[0], values[1].GetInt());
            }
        }

        public static async Task CreateEncFile (string framesPath, bool loopEnabled, int interpFactor, bool notFirstRun)
        {
            if (Interpolate.canceled) return;
            Logger.Log($"Generating timecodes for {interpFactor}x...", false, true);

            bool loop = Config.GetBool("enableLoop");
            bool sceneDetection = true;
            string ext = InterpolateUtils.GetOutExt();

            FileInfo[] frameFiles = new DirectoryInfo(framesPath).GetFiles($"*.png");
            string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-{interpFactor}x.ini");
            string fileContent = "";
            string dupesFile = Path.Combine(framesPath.GetParentDir(), $"dupes.ini");
            LoadDupesFile(dupesFile);

            string scnFramesPath = Path.Combine(framesPath.GetParentDir(), Paths.scenesDir);
            string interpPath = Paths.interpDir;

            List<string> sceneFrames = new List<string>();
            if (Directory.Exists(scnFramesPath))
                sceneFrames = Directory.GetFiles(scnFramesPath).Select(file => Path.GetFileNameWithoutExtension(file)).ToList();

            bool debug = false;

            int totalFileCount = 1;
            for (int i = 0; i < (frameFiles.Length - 1); i++)
            {
                if (Interpolate.canceled) return;

                int interpFramesAmount = interpFactor;
                string inputFilenameNoExt = Path.GetFileNameWithoutExtension(frameFiles[i].Name);
                int dupesAmount = dupesDict.ContainsKey(inputFilenameNoExt) ? dupesDict[inputFilenameNoExt] : 0;
                
                if(debug) Logger.Log($"{Path.GetFileNameWithoutExtension(frameFiles[i].Name)} has {dupesAmount} dupes", true);

                bool discardThisFrame = (sceneDetection && (i + 2) < frameFiles.Length && sceneFrames.Contains(Path.GetFileNameWithoutExtension(frameFiles[i + 1].Name)));     // i+2 is in scene detection folder, means i+1 is ugly interp frame

                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpFramesAmount = interpFramesAmount * 2;

                if (debug) Logger.Log($"Writing out frames for in frame {i} which has {dupesAmount} dupes", true);
                // Generate frames file lines
                for (int frm = 0; frm < interpFramesAmount; frm++)
                {
                    if (debug) Logger.Log($"Writing out frame {frm+1}/{interpFramesAmount}", true);

                    if (discardThisFrame && totalFileCount > 1)     // If frame is scene cut frame
                    {
                        int lastNum = totalFileCount;

                        if (debug) Logger.Log($"Writing frame {totalFileCount} [Discarding Next]", true);
                        //fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\n";
                        fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, interpPath, ext, debug);
                        totalFileCount++;

                        if (debug) Logger.Log("Discarding interp frames with out num " + totalFileCount);
                        for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                        {
                            if (debug) Logger.Log($"Writing frame {totalFileCount} which is actually repeated frame {lastNum}");
                            //fileContent += $"file '{interpPath}/{lastNum.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\n";
                            fileContent = WriteFrameWithDupes(dupesAmount, fileContent, lastNum, interpPath, ext, debug);
                            totalFileCount++;
                        }

                        frm = interpFramesAmount;
                    }
                    else
                    {
                        fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, interpPath, ext, debug);
                        totalFileCount++;
                    }
                }

                if ((i + 1) % 100 == 0)
                    await Task.Delay(1);
            }

            // Use average frame duration for last frame - TODO: Use real duration??
            //string durationStrLast = ((totalDuration / (totalFileCount - 1)) / timebase).ToString("0.0000000", CultureInfo.InvariantCulture);
            fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\n";
            totalFileCount++;

            string finalFileContent = fileContent.Trim();
            if(loop)
                finalFileContent = finalFileContent.Remove(finalFileContent.LastIndexOf("\n"));
            File.WriteAllText(vfrFile, finalFileContent);

            if (notFirstRun) return;    // Skip all steps that only need to be done once

            if (loop)
            {
                int lastFileNumber = frameFiles.Last().Name.GetInt() + 1;
                string loopFrameTargetPath = Path.Combine(frameFiles.First().FullName.GetParentDir(), lastFileNumber.ToString().PadLeft(Padding.inputFrames, '0') + $".png");
                if (File.Exists(loopFrameTargetPath))
                {
                    if (debug) Logger.Log($"Won't copy loop frame - {Path.GetFileName(loopFrameTargetPath)} already exists.", true);
                    return;
                }
                File.Copy(frameFiles.First().FullName, loopFrameTargetPath);
                if (debug) Logger.Log($"Copied loop frame to {loopFrameTargetPath}.", true);
            }
        }

        static string WriteFrameWithDupes (int dupesAmount, string fileContent, int frameNum, string interpPath, string ext, bool debug)
        {
            for (int writtenDupes = -1; writtenDupes < dupesAmount; writtenDupes++)      // Write duplicates
            {
                if (debug) Logger.Log($"Writing frame {frameNum} (writtenDupes {writtenDupes})", true, false);
                fileContent += $"file '{interpPath}/{frameNum.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\n";
            }
            return fileContent;
        }
    }
}
