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
    class FrameOrder
    {
        static Stopwatch benchmark = new Stopwatch();

        public static async Task CreateFrameOrderFile(string framesPath, bool loopEnabled, float times)
        {
            Logger.Log("Generating frame order information...");

            try
            {
                foreach (FileInfo file in IOUtils.GetFileInfosSorted(framesPath.GetParentDir(), false, $"{Paths.frameOrderPrefix}*.*"))
                    file.Delete();

                benchmark.Restart();
                await CreateEncFile(framesPath, loopEnabled, times, false);
                Logger.Log($"Generating frame order information... Done.", false, true);
                Logger.Log($"Generated frame order info file in {benchmark.ElapsedMilliseconds} ms", true);
            }
            catch (Exception e)
            {
                Logger.Log($"Error generating frame order information: {e.Message}");
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

        public static async Task CreateEncFile (string framesPath, bool loopEnabled, float interpFactor, bool notFirstRun)
        {
            if (Interpolate.canceled) return;
            Logger.Log($"Generating frame order information for {interpFactor}x...", false, true);

            bool loop = Config.GetBool("enableLoop");
            bool sceneDetection = true;
            string ext = InterpolateUtils.GetOutExt();

            FileInfo[] frameFiles = new DirectoryInfo(framesPath).GetFiles($"*.png");
            string vfrFile = Path.Combine(framesPath.GetParentDir(), Paths.GetFrameOrderFilename(interpFactor));
            string fileContent = "";
            string dupesFile = Path.Combine(framesPath.GetParentDir(), $"dupes.ini");
            LoadDupesFile(dupesFile);

            string scnFramesPath = Path.Combine(framesPath.GetParentDir(), Paths.scenesDir);
            string interpPath = Paths.interpDir;

            List<string> sceneFrames = new List<string>();
            if (Directory.Exists(scnFramesPath))
                sceneFrames = Directory.GetFiles(scnFramesPath).Select(file => Path.GetFileNameWithoutExtension(file)).ToList();

            bool debug = Config.GetBool("frameOrderDebug", false);

            int interpFramesAmount = (int)interpFactor;     // TODO: This code won't work with fractional factors
            int totalFileCount = 0;

            for (int i = 0; i < (frameFiles.Length - 1); i++)
            {
                if (Interpolate.canceled) return;
                
                string inputFilenameNoExt = Path.GetFileNameWithoutExtension(frameFiles[i].Name);
                int dupesAmount = dupesDict.ContainsKey(inputFilenameNoExt) ? dupesDict[inputFilenameNoExt] : 0;
                bool discardThisFrame = (sceneDetection && (i + 2) < frameFiles.Length && sceneFrames.Contains(Path.GetFileNameWithoutExtension(frameFiles[i + 1].Name)));     // i+2 is in scene detection folder, means i+1 is ugly interp frame

                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpFramesAmount = interpFramesAmount * 2;
                
                for (int frm = 0; frm < interpFramesAmount; frm++)  // Generate frames file lines
                {
                    if (discardThisFrame)     // If frame is scene cut frame
                    {
                        totalFileCount++;
                        int lastNum = totalFileCount;
                        fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, interpPath, ext, debug, $"[In: {inputFilenameNoExt}] [{((frm == 0) ? " Source " : $"Interp {frm}")}] [DiscardNext]");

                        for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                        {
                            totalFileCount++;
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

                if (i % 250 == 0)
                {
                    if (i % 1000 == 0)
                        Logger.Log($"Generating frame order information... {i}/{frameFiles.Length}.", false, true);
                    await Task.Delay(1);
                }
            }

            totalFileCount++;
            fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'";     // Last frame (source)

            if(loop)
                fileContent = fileContent.Remove(fileContent.LastIndexOf("\n"));

            File.WriteAllText(vfrFile, fileContent);

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

        static string WriteFrameWithDupes (int dupesAmount, string fileContent, int frameNum, string interpPath, string ext, bool debug, string note = "")
        {
            for (int writtenDupes = -1; writtenDupes < dupesAmount; writtenDupes++)      // Write duplicates
            {
                if (debug) Logger.Log($"Writing frame {frameNum} (writtenDupes {writtenDupes})", true, false);
                fileContent += $"file '{interpPath}/{frameNum.ToString().PadLeft(Padding.interpFrames, '0')}.{ext}'{(debug ? ($" # Dupe {(writtenDupes+1).ToString("000")} {note}").Replace("Dupe 000", "        ") : "" )}\n";
            }
            return fileContent;
        }
    }
}
