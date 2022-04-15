using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Newtonsoft.Json;
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
        static List<string> inputFilenames = new List<string>();
        static int lastOutFileCount;

        public static async Task CreateFrameOrderFile(string framesPath, bool loopEnabled, float times)
        {
            Logger.Log("Generating frame order information...");

            try
            {
                foreach (FileInfo file in IoUtils.GetFileInfosSorted(framesPath.GetParentDir(), false, $"{Paths.frameOrderPrefix}*.*"))
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
            string[] dupesFileLines = IoUtils.ReadLines(path);
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

            bool loop = Config.GetBool(Config.Key.enableLoop);
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

            inputFilenames.Clear();
            bool debug = Config.GetBool("frameOrderDebug", false);
            List<Task> tasks = new List<Task>();
            int linesPerTask = (400 / interpFactor).RoundToInt();
            int num = 0;

            int targetFrameCount = (frameFiles.Length * interpFactor).RoundToInt() - InterpolateUtils.GetRoundedInterpFramesPerInputFrame(interpFactor);

            if(interpFactor == (int)interpFactor) // Use old multi-threaded code if factor is not fractional
            {
                for (int i = 0; i < frameFilesWithoutLast.Length; i += linesPerTask)
                {
                    tasks.Add(GenerateFrameLines(num, i, linesPerTask, (int)interpFactor, sceneDetection, debug));
                    num++;
                }
            }
            else
            {
                await GenerateFrameLinesFloat(targetFrameCount, interpFactor, sceneDetection, debug);
            }

            await Task.WhenAll(tasks);

            for (int x = 0; x < frameFileContents.Count; x++)
                fileContent += frameFileContents[x];

            lastOutFileCount++;

            if (Config.GetBool(Config.Key.fixOutputDuration)) // Match input duration by padding duping last frame until interp frames == (inputframes * factor)
            {
                int neededFrames = (frameFiles.Length * interpFactor).RoundToInt() - fileContent.SplitIntoLines().Where(x => x.StartsWith("'file ")).Count();

                for (int i = 0; i < neededFrames; i++)
                    fileContent += fileContent.SplitIntoLines().Where(x => x.StartsWith("'file ")).Last();
            }

            //int lastFrameTimes = Config.GetBool(Config.Key.fixOutputDuration) ? (int)interpFactor : 1;
            //
            //for (int i = 0; i < lastFrameTimes; i++)
            //{
            //    fileContent += $"{(i > 0 ? "\n" : "")}file '{Paths.interpDir}/{lastOutFileCount.ToString().PadLeft(Padding.interpFrames, '0')}{ext}'";     // Last frame (source)
            //    inputFilenames.Add(frameFiles.Last().Name);
            //}

            if (loop)
            {
                fileContent = fileContent.Remove(fileContent.LastIndexOf("\n"));
                //inputFilenames.Remove(inputFilenames.Last());
            }

            File.WriteAllText(framesFile, fileContent);
            File.WriteAllText(framesFile + ".inputframes.json", JsonConvert.SerializeObject(inputFilenames, Formatting.Indented));
        }

        static async Task GenerateFrameLinesFloat(int targetFrameCount, float factor, bool sceneDetection, bool debug)
        {
            int totalFileCount = 0;
            string ext = Interpolate.current.interpExt;
            float step = 1 / factor;

            string fileContent = "";
            //float currentFrameTime = 1f; // Start at 1 because there are no interp frames before the first input frame

            for (int i = 0; i < targetFrameCount; i++)
            {
                if (Interpolate.canceled) return;
                //if (i >= frameFilesWithoutLast.Length) break;

                //string frameName = GetNameNoExt(frameFilesWithoutLast[i].Name);

                float currentFrameTime = 1 + (step * i);
                string filename = $"{Paths.interpDir}/{(i + 1).ToString().PadLeft(Padding.interpFrames, '0')}{ext}";
                string note = $"Frame {currentFrameTime.ToString("0.0000")}";
                fileContent += $"file '{filename}' # {note}\n";
            }

            if (totalFileCount > lastOutFileCount)
                lastOutFileCount = totalFileCount;

            frameFileContents[0] = fileContent;
            //frameFileContents[number] = fileContent;
        }

        static async Task GenerateFrameLines(int number, int startIndex, int count, int factor, bool sceneDetection, bool debug)
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

                        if (Config.GetInt(Config.Key.sceneChangeFillMode) == 0)      // Duplicate last frame
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

                    inputFilenames.Add(frameFilesWithoutLast[i].Name);
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

        static string GetNameNoExt(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}
