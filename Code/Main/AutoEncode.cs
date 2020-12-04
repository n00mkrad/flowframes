using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class AutoEncode
    {
        static string interpFramesFolder;
        static string videoChunksFolder;
        public static int chunkSize = 125;    // Encode every n frames
        public static int safetyBufferFrames = 25;
        public static List<string> encodedFrames = new List<string>();
        public static List<string> unencodedFrames = new List<string>();

        public static bool busy;

        public static bool paused;


        public static async Task MainLoop(string interpFramesPath)
        {
            interpFramesFolder = interpFramesPath;
            videoChunksFolder = Path.Combine(interpFramesPath.GetParentDir(), Paths.chunksDir);

            encodedFrames.Clear();
            unencodedFrames.Clear();

            chunkSize = GetChunkSize(IOUtils.GetAmountOfFiles(Interpolate.currentFramesPath, false, "*.png") * Interpolate.lastInterpFactor);

            int videoIndex = 1;

            while (!Interpolate.canceled && GetInterpFramesAmount() < 2)
                await Task.Delay(1000);

            while (HasWorkToDo())    // Loop while proc is running and not all frames have been encoded
            {
                if (Interpolate.canceled) return;

                if (paused)
                {
                    await Task.Delay(100);
                    continue;
                }

                IOUtils.ZeroPadDir(Directory.GetFiles(interpFramesFolder, $"*.{InterpolateUtils.lastExt}").ToList(), Padding.interpFrames, encodedFrames);
                string[] interpFrames = Directory.GetFiles(interpFramesFolder, $"*.{InterpolateUtils.lastExt}");
                unencodedFrames = interpFrames.ToList().Except(encodedFrames).ToList();

                Directory.CreateDirectory(videoChunksFolder);

                bool aiRunning = !AiProcess.currentAiProcess.HasExited;

                if (unencodedFrames.Count >= (chunkSize + safetyBufferFrames) || !aiRunning)     // Encode every n frames, or after process has exited
                {
                    busy = true;
                    Logger.Log("Encoding Chunk #" + videoIndex, true, false, "ffmpeg.txt");

                    List<string> framesToEncode = aiRunning ? unencodedFrames.Take(chunkSize).ToList() : unencodedFrames;     // Take all remaining frames if process is done
                    IOUtils.ZeroPadDir(framesToEncode, Padding.interpFrames);   // Zero-pad frames before encoding to make sure filenames match with VFR file

                    string outpath = Path.Combine(videoChunksFolder, $"{videoIndex.ToString().PadLeft(4, '0')}{InterpolateUtils.GetExt(Interpolate.currentOutMode)}");
                    int firstFrameNum = Path.GetFileNameWithoutExtension(framesToEncode[0]).GetInt();
                    await CreateVideo.EncodeChunk(outpath, firstFrameNum, framesToEncode.Count);

                    if(Interpolate.canceled) return;

                    foreach (string frame in framesToEncode)
                        File.WriteAllText(frame, "THIS IS A DUMMY FILE - DO NOT DELETE ME");    // Overwrite to save disk space without breaking progress counter

                    encodedFrames.AddRange(framesToEncode);

                    Logger.Log("Done Encoding Chunk #" + videoIndex, true, false, "ffmpeg.txt");
                    videoIndex++;
                    busy = false;
                }
                await Task.Delay(50);
            }

            if (Interpolate.canceled) return;

            string concatFile = Path.Combine(interpFramesPath.GetParentDir(), "chunks-concat.ini");
            string concatFileContent = "";
            foreach (string vid in Directory.GetFiles(videoChunksFolder))
                concatFileContent += $"file '{Paths.chunksDir}/{Path.GetFileName(vid)}'\n";
            File.WriteAllText(concatFile, concatFileContent);

            IOUtils.ReverseRenaming(AiProcess.filenameMap, true);   // Get timestamps back

            await CreateVideo.ChunksToVideo(videoChunksFolder, concatFile, Interpolate.nextOutPath);
        }

        public static bool HasWorkToDo ()
        {
            if (Interpolate.canceled || interpFramesFolder == null) return false;
            return ((AiProcess.currentAiProcess != null && !AiProcess.currentAiProcess.HasExited) || encodedFrames.Count < GetInterpFramesAmount());
        }

        static int GetChunkSize(int targetFramesAmount)
        {
            if (targetFramesAmount > 50000) return 2000;
            if (targetFramesAmount > 5000) return 500;
            if (targetFramesAmount > 1000) return 200;
            return 100;
        }

        static int GetInterpFramesAmount()
        {
            return IOUtils.GetAmountOfFiles(interpFramesFolder, false, $"*.{InterpolateUtils.lastExt}");
        }
    }
}
