using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class FrameOrder
    {
        static Stopwatch benchmark = new Stopwatch();
        static FileInfo[] frameFiles;
        static FileInfo[] frameFilesWithoutLast;
        static List<string> sceneFrames = new List<string>();
        static Dictionary<int, string> frameFileContents = new Dictionary<int, string>();
        static int lastOutFileCount;

        public static async Task CreateFrameOrderFile(string framesPath, bool loopEnabled, float times)
        {
            Logger.Log("Generating frame order information...");

            try
            {
                foreach (FileInfo file in IOUtils.GetFileInfosSorted(framesPath.GetParentDir(), false, $"{Paths.frameOrderPrefix}*.*"))
                    file.Delete();

                benchmark.Restart();
                await CreateEncFile(framesPath, loopEnabled, times);
                Logger.Log($"Generating frame order information... Done.", false, true);
                Logger.Log($"Generated frame order info file in {benchmark.ElapsedMilliseconds} ms", true);
            }
            catch (Exception e)
            {
                Logger.Log($"Error generating frame order information: {e.Message}\n{e.StackTrace}");
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

        public static async Task CreateEncFile(string framesPath, bool loopEnabled, float interpFactor)
        {
            if (Interpolate.canceled) return;
            Logger.Log($"Generating frame order information for {interpFactor}x...", false, true);

            bool loop = Config.GetBool("enableLoop");
            bool sceneDetection = true;
            string ext = Interpolate.current.interpExt;

            frameFileContents.Clear();
            lastOutFileCount = 0;

            frameFiles = new DirectoryInfo(framesPath).GetFiles("*" + Interpolate.current.framesExt);
            frameFilesWithoutLast = frameFiles;
            Array.Resize(ref frameFilesWithoutLast, frameFilesWithoutLast.Length - 1);
            string framesFile = Path.Combine(framesPath.GetParentDir(), Paths.GetFrameOrderFilename(interpFactor));
            string fileContent = "";
            string dupesFile = Path.Combine(framesPath.GetParentDir(), "dupes.ini");
            LoadDupesFile(dupesFile);

            string scnFramesPath = Path.Combine(framesPath.GetParentDir(), Paths.scenesDir);

            sceneFrames.Clear();

            if (Directory.Exists(scnFramesPath))
                sceneFrames = Directory.GetFiles(scnFramesPath).Select(file => GetNameNoExt(file)).ToList();

            bool debug = Config.GetBool("frameOrderDebug", false);
            List<Task> tasks = new List<Task>();
            int linesPerTask = 400 / (int)interpFactor;
            int num = 0;

            for (int i = 0; i < frameFilesWithoutLast.Length; i += linesPerTask)
            {
                tasks.Add(GenerateFrameLines(num, i, linesPerTask, (int)interpFactor, loopEnabled, sceneDetection, debug));
                num++;
            }

            await Task.WhenAll(tasks);

            for (int x = 0; x < frameFileContents.Count; x++)
                fileContent += frameFileContents[x];

            lastOutFileCount++;
            int lastFrameTimes = Config.GetBool("fixOutputDuration") ? (int)interpFactor : 1;

            for(int i = 0; i < lastFrameTimes; i++)
                fileContent += $"{(i > 0 ? "\n" : "")}file '{Paths.interpDir}/{lastOutFileCount.ToString().PadLeft(Padding.interpFrames, '0')}{ext}'";     // Last frame (source)

            if (loop)
                fileContent = fileContent.Remove(fileContent.LastIndexOf("\n"));

            File.WriteAllText(framesFile, fileContent);
        }

        static async Task GenerateFrameLines(int number, int startIndex, int count, int factor, bool loopEnabled, bool sceneDetection, bool debug)
        {
            int totalFileCount = (startIndex) * factor;
            int interpFramesAmount = factor;
            string ext = Interpolate.current.interpExt;

            string fileContent = "";

            for (int i = startIndex; i < (startIndex + count); i++)
            {
                if (Interpolate.canceled) return;
                if (i >= frameFilesWithoutLast.Length) break;

                string frameName = GetNameNoExt(frameFilesWithoutLast[i].Name);
                string frameNameImport = GetNameNoExt(FrameRename.importFilenames[i]);
                int dupesAmount = dupesDict.ContainsKey(frameNameImport) ? dupesDict[frameNameImport] : 0;
                bool discardThisFrame = (sceneDetection && i < frameFilesWithoutLast.Length && sceneFrames.Contains(GetNameNoExt(FrameRename.importFilenames[i + 1])));     // i+2 is in scene detection folder, means i+1 is ugly interp frame

                for (int frm = 0; frm < interpFramesAmount; frm++)  // Generate frames file lines
                {
                    if (discardThisFrame)     // If frame is scene cut frame
                    {
                        string frameBeforeScn = i.ToString().PadLeft(Padding.inputFramesRenamed, '0') + Path.GetExtension(FrameRename.importFilenames[i]);
                        string frameAfterScn = (i + 1).ToString().PadLeft(Padding.inputFramesRenamed, '0') + Path.GetExtension(FrameRename.importFilenames[i + 1]);
                        string scnChangeNote = $"SCN:{frameBeforeScn}>{frameAfterScn}";

                        totalFileCount++;
                        fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, ext, debug, $"[In: {frameName}] [{((frm == 0) ? " Source " : $"Interp {frm}")}]", scnChangeNote);

                        if (Config.GetInt("sceneChangeFillMode") == 0)      // Duplicate last frame
                        {
                            int lastNum = totalFileCount;

                            for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                            {
                                totalFileCount++;
                                fileContent = WriteFrameWithDupes(dupesAmount, fileContent, lastNum, ext, debug, $"[In: {frameName}] [DISCARDED]");
                            }
                        }
                        else
                        {
                            for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                            {
                                totalFileCount++;
                                fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, ext, debug, $"[In: {frameName}] [BLEND FRAME]");
                            }
                        }

                        frm = interpFramesAmount;
                    }
                    else
                    {
                        totalFileCount++;
                        fileContent = WriteFrameWithDupes(dupesAmount, fileContent, totalFileCount, ext, debug, $"[In: {frameName}] [{((frm == 0) ? " Source " : $"Interp {frm}")}]");
                    }
                }
            }

            if (totalFileCount > lastOutFileCount)
                lastOutFileCount = totalFileCount;

            frameFileContents[number] = fileContent;
        }

        static string WriteFrameWithDupes(int dupesAmount, string fileContent, int frameNum, string ext, bool debug, string debugNote = "", string forcedNote = "")
        {
            for (int writtenDupes = -1; writtenDupes < dupesAmount; writtenDupes++)      // Write duplicates
                fileContent += $"file '{Paths.interpDir}/{frameNum.ToString().PadLeft(Padding.interpFrames, '0')}{ext}' # {(debug ? ($"Dupe {(writtenDupes + 1).ToString("000")} {debugNote}").Replace("Dupe 000", "        ") : "")}{forcedNote}\n";

            return fileContent;
        }

        static string GetNameNoExt (string path) { return Path.GetFileNameWithoutExtension(path); }
    }
}
