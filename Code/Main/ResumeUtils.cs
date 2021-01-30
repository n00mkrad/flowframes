using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{

    class ResumeUtils
    {
        public static float timeBetweenSaves = 10;
        public static int minFrames = 100;
        public static int safetyDelayFrames = 100;
        public static string resumeFileName = "resume.ini";
        public static string filenameMapFileName = "frameFilenames.ini";

        public static int currentOutFrames;
        public static Stopwatch timeSinceLastSave = new Stopwatch();

        public static void Save ()
        {
            if (timeSinceLastSave.IsRunning && timeSinceLastSave.ElapsedMilliseconds < (timeBetweenSaves * 1000f).RoundToInt()) return;
            timeSinceLastSave.Restart();
            Directory.CreateDirectory(Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir));
            SaveState();
        }

        public static async Task SaveState ()
        {
            int frames = GetSafeInterpolatedInputFramesAmount();
            if (frames < 1) return;
            ResumeState state = new ResumeState(Interpolate.currentlyUsingAutoEnc, frames);
            string stateFilePath = Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir, resumeFileName);
            File.WriteAllText(stateFilePath, state.ToString());
            await SaveFilenameMap();
        }

        public static int GetSafeInterpolatedInputFramesAmount()
        {
            return (int)Math.Round((float)currentOutFrames / Interpolate.current.interpFactor) - safetyDelayFrames;
        }

        static async Task SaveFilenameMap ()
        {
            string filePath = Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir, filenameMapFileName);

            if (File.Exists(filePath) && IOUtils.GetFilesize(filePath) > 0)
                return;

            string fileContent = "";
            int counter = 0;

            foreach (KeyValuePair<string, string> entry in AiProcess.filenameMap)
            {
                if (counter % 500 == 0) await Task.Delay(1);
                fileContent += $"{entry.Key}|{entry.Value}\n";
            }

            File.WriteAllText(filePath, fileContent);
        }

        static void LoadFilenameMap()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            string filePath = Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir, filenameMapFileName);
            string[] dictLines = File.ReadAllLines(filePath);

            foreach (string line in dictLines)
            {
                if (line.Length < 5) continue;
                string[] keyValuePair = line.Split('|');
                dict.Add(keyValuePair[0].Trim(), keyValuePair[1].Trim());
            }

            AiProcess.filenameMap = dict;
        }
    }
}
