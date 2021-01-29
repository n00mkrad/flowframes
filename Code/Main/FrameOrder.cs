﻿using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class FrameOrder
    {
        public enum Mode { CFR, VFR }
        public static int timebase = 10000;

        public static async Task CreateFrameOrderFile(string framesPath, bool loopEnabled, int times)
        {
            Logger.Log("Generating frame order information...");
            try
            {
                await CreateEncFile(framesPath, loopEnabled, times, false);
                Logger.Log($"Generating frame order information... Done.", false, true);
            }
            catch (Exception e)
            {
                Logger.Log($"Error generating frame order information: {e.Message}");
            }
        }

        static Dictionary<string, int> dupesDict = new Dictionary<string, int>();

        static void LoadDupesFile(string path)
        {
            dupesDict.Clear();
            if (!File.Exists(path)) return;
            string[] dupesFileLines = IOUtils.ReadLines(path);
            foreach (string line in dupesFileLines)
            {
                string[] values = line.Split(':');
                dupesDict.Add(values[0], values[1].GetInt());
            }
        }

        public static async Task CreateEncFile(string framesPath, bool loopEnabled, int interpFactor, bool notFirstRun)
        {
            if (Interpolate.canceled) return;
            Logger.Log($"Generating frame order information for {interpFactor}x...", false, true);

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

            bool debug = Config.GetBool("frameOrderDebug", false);

            int totalFileCount = 0;
            for (int i = 0; i < (frameFiles.Length - 1); i++)
            {
                if (Interpolate.canceled) return;

                int interpFramesAmount = interpFactor;
                string inputFilenameNoExt = Path.GetFileNameWithoutExtension(frameFiles[i].Name);
                int dupesAmount = dupesDict.ContainsKey(inputFilenameNoExt) ? dupesDict[inputFilenameNoExt] : 0;

                if (debug) Logger.Log($"{Path.GetFileNameWithoutExtension(frameFiles[i].Name)} has {dupesAmount} dupes", true);

                bool discardThisFrame = (sceneDetection && (i + 2) < frameFiles.Length && sceneFrames.Contains(Path.GetFileNameWithoutExtension(frameFiles[i + 1].Name)));     // i+2 is in scene detection folder, means i+1 is ugly interp frame

                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpFramesAmount = interpFramesAmount * 2;

                if (debug) Logger.Log($"Writing out frames for in frame {i} which has {dupesAmount} dupes", true);
                // Generate frames file lines
                for (int frm = 0; frm < interpFramesAmount; frm++)
                {
                    //if (debug) Logger.Log($"Writing out frame {frm+1}/{interpFramesAmount}", true);

                    if (discardThisFrame)     // If frame is scene cut frame
                    {
                        //if (debug) Logger.Log($"Writing frame {totalFileCount} [Discarding Next]", true);
                        totalFileCount++;
                        int lastNum = totalFileCount;
                        fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, interpPath, ext, debug, $"[In: {inputFilenameNoExt}] [{((frm == 0) ? " Source " : $"Interp {frm}")}] [DiscardNext]");

                        //if (debug) Logger.Log("Discarding interp frames with out num " + totalFileCount);
                        for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                        {
                            totalFileCount++;
                            if (debug) Logger.Log($"Writing frame {totalFileCount} which is actually repeated frame {lastNum}", true);
                            fileContent = WriteFrameWithDupes(dupesAmount, fileContent, lastNum, interpPath, ext, debug, $"[In: {inputFilenameNoExt}] [DISCARDED]");
                        }

                        frm = interpFramesAmount;
                    }
                    else
                    {
                        totalFileCount++;
                        fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, interpPath, ext, debug, $"[In: {inputFilenameNoExt}] [{((frm == 0) ? " Source " : $"Interp {frm}")}]");
                    }
                }

                if ((i + 1) % 100 == 0)
                    await Task.Delay(1);
            }

            // if(debug) Logger.Log("target: " + ((frameFiles.Length * interpFactor) - (interpFactor - 1)), true);
            // if(debug) Logger.Log("totalFileCount: " + totalFileCount, true);

            totalFileCount++;
            fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'\n";

            string finalFileContent = fileContent.Trim();
            if (loop)
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

        static string WriteFrameWithDupes(int dupesAmount, string fileContent, int frameNum, string interpPath, string ext, bool debug, string note = "")
        {
            for (int writtenDupes = -1; writtenDupes < dupesAmount; writtenDupes++)      // Write duplicates
            {
                if (debug) Logger.Log($"Writing frame {frameNum} (writtenDupes {writtenDupes})", true, false);
                fileContent += $"file '{interpPath}/{frameNum.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'{(debug ? ($" # Dupe {(writtenDupes + 1).ToString("000")} {note}").Replace("Dupe 000", "        ") : "")}\n";
            }
            return fileContent;
        }
    }
}
