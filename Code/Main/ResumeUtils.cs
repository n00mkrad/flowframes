using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flowframes.MiscUtils;
using Flowframes.UI;
using I = Flowframes.Interpolate;

namespace Flowframes.Main
{

    class ResumeUtils
    {
        public static bool resumeNextRun;

        public static float timeBetweenSaves = 10;
        public static int minFrames = 100;
        public static int safetyDelayFrames = 50;
        public static string resumeFilename = "resume.ini";
        public static string interpSettingsFilename = "interpSettings.ini";
        public static string filenameMapFilename = "frameFilenames.ini";

        public static Stopwatch timeSinceLastSave = new Stopwatch();

        public static void Save ()
        {
            if (timeSinceLastSave.IsRunning && timeSinceLastSave.ElapsedMilliseconds < (timeBetweenSaves * 1000f).RoundToInt()) return;
            int frames = (int)Math.Round((float)InterpolationProgress.interpolatedInputFramesCount / I.current.interpFactor) - safetyDelayFrames;
            if (frames < 1) return;
            timeSinceLastSave.Restart();
            Directory.CreateDirectory(Path.Combine(I.current.tempFolder, Paths.resumeDir));
            SaveState(frames);
            SaveInterpSettings();
            SaveFilenameMap();
        }

        static void SaveState (int frames)
        {
            ResumeState state = new ResumeState(I.currentlyUsingAutoEnc, frames);
            string filePath = Path.Combine(I.current.tempFolder, Paths.resumeDir, resumeFilename);
            File.WriteAllText(filePath, state.ToString());
        }

        static async Task SaveFilenameMap ()
        {
            string filePath = Path.Combine(I.current.tempFolder, Paths.resumeDir, filenameMapFilename);

            if (File.Exists(filePath) && IOUtils.GetFilesize(filePath) > 0)
                return;

            string fileContent = "";
            int counter = 0;

            foreach (string file in FrameRename.importFilenames)
            {
                if (counter % 1000 == 0) await Task.Delay(1);
                fileContent += $"{file}\n";
                counter++;
            }

            File.WriteAllText(filePath, fileContent);
        }

        static void SaveInterpSettings ()
        {
            string filepath = Path.Combine(I.current.tempFolder, Paths.resumeDir, interpSettingsFilename);
            File.WriteAllText(filepath, I.current.Serialize());
        }

        public static void LoadTempFolder (string tempFolderPath)
        {
            string resumeFolderPath = Path.Combine(tempFolderPath, Paths.resumeDir);
            string interpSettingsPath = Path.Combine(resumeFolderPath, interpSettingsFilename);
            InterpSettings interpSettings = new InterpSettings(File.ReadAllText(interpSettingsPath));
            Program.mainForm.LoadBatchEntry(interpSettings);
        }

        public static async Task PrepareResumedRun ()
        {
            if (!resumeNextRun) return;

            string stateFilepath = Path.Combine(I.current.tempFolder, Paths.resumeDir, resumeFilename);
            ResumeState state = new ResumeState(File.ReadAllText(stateFilepath));

            string fileMapFilepath = Path.Combine(I.current.tempFolder, Paths.resumeDir, filenameMapFilename);
            List<string> inputFrameLines = File.ReadAllLines(fileMapFilepath).Where(l => l.Trim().Length > 3).ToList();
            List<string> inputFrames = inputFrameLines.Select(l => Path.Combine(I.current.framesFolder, l.Split('|')[1])).ToList();

            for (int i = 0; i < state.interpolatedInputFrames; i++)
            {
                IOUtils.TryDeleteIfExists(inputFrames[i]);
                if (i % 1000 == 0) await Task.Delay(1);
            }

            Directory.Move(I.current.interpFolder, I.current.interpFolder + Paths.prevSuffix);  // Move existing interp frames
            Directory.CreateDirectory(I.current.interpFolder);  // Re-create empty interp folder

            LoadFilenameMap();
        }

        static void LoadFilenameMap()
        {
            List<string> files = new List<string>();
            string filePath = Path.Combine(I.current.tempFolder, Paths.resumeDir, filenameMapFilename);
            string[] fileLines = File.ReadAllLines(filePath);

            foreach (string line in fileLines)
            {
                if (line.Trim().Length < 3) continue;
                files.Add(line.Trim());
            }

            FrameRename.importFilenames = files.ToArray();
        }
    }
}
