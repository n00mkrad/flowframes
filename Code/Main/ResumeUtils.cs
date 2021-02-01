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
        public static int safetyDelayFrames = 50;
        public static string resumeFilename = "resume.ini";
        public static string interpSettingsFilename = "interpSettings.ini";
        public static string filenameMapFilename = "frameFilenames.ini";

        public static int currentOutFrames;
        public static Stopwatch timeSinceLastSave = new Stopwatch();

        public static void Save ()
        {
            if (timeSinceLastSave.IsRunning && timeSinceLastSave.ElapsedMilliseconds < (timeBetweenSaves * 1000f).RoundToInt()) return;
            int frames = (int)Math.Round((float)currentOutFrames / Interpolate.current.interpFactor) - safetyDelayFrames;
            if (frames < 1) return;
            timeSinceLastSave.Restart();
            Directory.CreateDirectory(Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir));
            SaveState(frames);
            SaveInterpSettings();
            SaveFilenameMap();
        }

        static void SaveState (int frames)
        {
            ResumeState state = new ResumeState(Interpolate.currentlyUsingAutoEnc, frames);
            string filePath = Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir, resumeFilename);
            File.WriteAllText(filePath, state.ToString());
        }

        static async Task SaveFilenameMap ()
        {
            string filePath = Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir, filenameMapFilename);

            if (File.Exists(filePath) && IOUtils.GetFilesize(filePath) > 0)
                return;

            string fileContent = "";
            int counter = 0;

            foreach (KeyValuePair<string, string> entry in AiProcess.filenameMap)
            {
                if (counter % 1000 == 0) await Task.Delay(1);
                fileContent += $"{entry.Key}|{entry.Value}\n";
                counter++;
            }

            File.WriteAllText(filePath, fileContent);
        }

        static void SaveInterpSettings ()
        {
            string filePath = Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir, interpSettingsFilename);
            File.WriteAllText(filePath, Interpolate.current.Serialize());
        }

        public static void LoadTempFolder (string tempFolderPath)
        {
            string resumeFolderPath = Path.Combine(tempFolderPath, Paths.resumeDir);
            string interpSettingsPath = Path.Combine(resumeFolderPath, interpSettingsFilename);
            InterpSettings interpSettings = new InterpSettings(File.ReadAllText(interpSettingsPath));
            Program.mainForm.LoadBatchEntry(interpSettings);
        }

        static void LoadFilenameMap()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            string filePath = Path.Combine(Interpolate.current.tempFolder, Paths.resumeDir, filenameMapFilename);
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
