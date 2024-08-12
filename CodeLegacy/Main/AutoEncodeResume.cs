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
using Flowframes.Ui;
using I = Flowframes.Interpolate;
using Newtonsoft.Json;

namespace Flowframes.Main
{
    class AutoEncodeResume
    {
        public static List<string> processedInputFrames = new List<string>();
        public static int encodedChunks = 0;
        public static int encodedFrames = 0;

        public static bool resumeNextRun;
        public static string interpSettingsFilename = "settings.json";
        public static string chunksFilename = "chunks.json";
        public static string inputFramesFilename = "input-frames.json";

        public static void Reset ()
        {
            processedInputFrames = new List<string>();
            encodedChunks = 0;
            encodedFrames = 0;
        }

        public static void Save ()
        {
            string saveDir = Path.Combine(I.currentSettings.tempFolder, Paths.resumeDir);
            Directory.CreateDirectory(saveDir);

            string chunksJsonPath = Path.Combine(saveDir, chunksFilename);
            Dictionary<string, string> saveData = new Dictionary<string, string>();
            saveData.Add("encodedChunks", encodedChunks.ToString());
            saveData.Add("encodedFrames", encodedFrames.ToString());
            File.WriteAllText(chunksJsonPath, JsonConvert.SerializeObject(saveData, Formatting.Indented));

            string inputFramesJsonPath = Path.Combine(saveDir, inputFramesFilename);
            File.WriteAllText(inputFramesJsonPath, JsonConvert.SerializeObject(processedInputFrames, Formatting.Indented));

            string settingsJsonPath = Path.Combine(saveDir, interpSettingsFilename);
            File.WriteAllText(settingsJsonPath, JsonConvert.SerializeObject(I.currentSettings, Formatting.Indented));
        }

        public static void LoadTempFolder(string tempFolderPath)
        {
            try
            {
                string resumeFolderPath = Path.Combine(tempFolderPath, Paths.resumeDir);
                string settingsJsonPath = Path.Combine(resumeFolderPath, interpSettingsFilename);
                InterpSettings interpSettings = JsonConvert.DeserializeObject<InterpSettings>(File.ReadAllText(settingsJsonPath));
                Program.mainForm.LoadBatchEntry(interpSettings);
            }
            catch(Exception e)
            {
                Logger.Log($"Failed to load resume data: {e.Message}\n{e.StackTrace}");
                resumeNextRun = false;
            }
        }

        public static async Task<bool> PrepareResumedRun() // Remove already interpolated frames, return true if interpolation should be skipped
        {
            if (!resumeNextRun) return false;

            try
            {
                string chunkJsonPath = Path.Combine(I.currentSettings.tempFolder, Paths.resumeDir, chunksFilename);
                string inFramesJsonPath = Path.Combine(I.currentSettings.tempFolder, Paths.resumeDir, inputFramesFilename);

                dynamic chunksData = JsonConvert.DeserializeObject(File.ReadAllText(chunkJsonPath));
                encodedChunks = chunksData.encodedChunks;
                encodedFrames = chunksData.encodedFrames;

                List<string> processedInputFrames = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(inFramesJsonPath));
                int uniqueInputFrames = processedInputFrames.Distinct().Count();

                foreach (string inputFrameName in processedInputFrames)
                {
                    string inputFrameFullPath = Path.Combine(I.currentSettings.tempFolder, Paths.framesDir, inputFrameName);
                    IoUtils.TryDeleteIfExists(inputFrameFullPath);
                }

                string videoChunksFolder = Path.Combine(I.currentSettings.tempFolder, Paths.chunksDir);

                FileInfo[] invalidChunks = IoUtils.GetFileInfosSorted(videoChunksFolder, true, "????.*").Skip(encodedChunks).ToArray();

                foreach (FileInfo chunk in invalidChunks)
                    chunk.Delete();

                int inputFramesLeft = IoUtils.GetAmountOfFiles(Path.Combine(I.currentSettings.tempFolder, Paths.framesDir), false);

                Logger.Log($"Resume: Already encoded {encodedFrames} frames in {encodedChunks} chunks. There are now {inputFramesLeft} input frames left to interpolate.");

                if(inputFramesLeft < 2)
                {
                    if(IoUtils.GetAmountOfFiles(videoChunksFolder, true, "*.*") > 0)
                    {
                        Logger.Log($"No more frames left to interpolate - Merging existing video chunks instead.");
                        await Export.ChunksToVideo(I.currentSettings.tempFolder, videoChunksFolder, I.currentSettings.outPath);
                        await I.Done();
                    }
                    else
                    {
                        I.Cancel("There are no more frames left to interpolate in this temp folder!");
                    }

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to prepare resumed run: {e.Message}\n{e.StackTrace}");
                I.Cancel("Failed to resume interpolation. Check the logs for details.");
                resumeNextRun = false;
                return true;
            }

            // string stateFilepath = Path.Combine(I.current.tempFolder, Paths.resumeDir, resumeFilename);
            // ResumeState state = new ResumeState(File.ReadAllText(stateFilepath));
            // 
            // string fileMapFilepath = Path.Combine(I.current.tempFolder, Paths.resumeDir, filenameMapFilename);
            // List<string> inputFrameLines = File.ReadAllLines(fileMapFilepath).Where(l => l.Trim().Length > 3).ToList();
            // List<string> inputFrames = inputFrameLines.Select(l => Path.Combine(I.current.framesFolder, l.Split('|')[1])).ToList();
            // 
            // for (int i = 0; i < state.interpolatedInputFrames; i++)
            // {
            //     IoUtils.TryDeleteIfExists(inputFrames[i]);
            //     if (i % 1000 == 0) await Task.Delay(1);
            // }
            // 
            // Directory.Move(I.current.interpFolder, I.current.interpFolder + Paths.prevSuffix);  // Move existing interp frames
            // Directory.CreateDirectory(I.current.interpFolder);  // Re-create empty interp folder
        }
    }
}
