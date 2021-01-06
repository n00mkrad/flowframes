using Flowframes.AudioVideo;
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
        public static int safetyBufferFrames = 90;      // Ignore latest n frames to avoid using images that haven't been fully encoded yet
        public static string[] interpFramesLines;
        public static List<int> encodedFrameLines = new List<int>();
        public static List<int> unencodedFrameLines = new List<int>();

        public static bool busy;

        public static bool paused;

        public static void UpdateChunkAndBufferSizes ()
        {
            chunkSize = GetChunkSize(IOUtils.GetAmountOfFiles(Interpolate.current.framesFolder, false, "*.png") * Interpolate.current.interpFactor);
            safetyBufferFrames = Interpolate.current.ai.aiName.ToUpper().Contains("NCNN") ? 60 : 30;    // Use bigger safety buffer for NCNN
        }

        public static async Task MainLoop(string interpFramesPath)
        {
            UpdateChunkAndBufferSizes();

            interpFramesFolder = interpFramesPath;
            videoChunksFolder = Path.Combine(interpFramesPath.GetParentDir(), Paths.chunksDir);
            if (Interpolate.currentlyUsingAutoEnc)
                Directory.CreateDirectory(videoChunksFolder);

            encodedFrameLines.Clear();
            unencodedFrameLines.Clear();

            Logger.Log($"Starting AutoEncode MainLoop - Chunk Size: {chunkSize} Frames - Safety Buffer: {safetyBufferFrames} Frames", true);
            int videoIndex = 1;
            string encFile = Path.Combine(interpFramesPath.GetParentDir(), $"vfr-{Interpolate.current.interpFactor}x.ini");
            interpFramesLines = IOUtils.ReadLines(encFile).Select(x => x.Split('/').Last().Remove("'")).ToArray();     // Array with frame filenames

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

                //IOUtils.ZeroPadDir(Directory.GetFiles(interpFramesFolder, $"*.{InterpolateUtils.GetOutExt()}").ToList(), Padding.interpFrames, encodedFrames);
                //string[] interpFrames = IOUtils.GetFilesSorted(interpFramesFolder, $"*.{InterpolateUtils.GetOutExt()}");

                //unencodedFrameLines = interpFramesLines.Select(x => x.GetInt()).ToList().Except(encodedFrameLines).ToList();

                //Logger.Log($"{unencodedFrameLines.Count} unencoded frame lines, {encodedFrameLines.Count} encoded frame lines", true, false, "ffmpeg");

                unencodedFrameLines.Clear();
                for(int vfrLine = 0; vfrLine < interpFramesLines.Length; vfrLine++)
                {
                    if (File.Exists(Path.Combine(interpFramesPath, interpFramesLines[vfrLine])) && !encodedFrameLines.Contains(vfrLine))
                        unencodedFrameLines.Add(vfrLine);
                }

                bool aiRunning = !AiProcess.currentAiProcess.HasExited;

                if (unencodedFrameLines.Count >= (chunkSize + safetyBufferFrames) || !aiRunning)     // Encode every n frames, or after process has exited
                {
                    busy = true;

                    List<int> frameLinesToEncode = aiRunning ? unencodedFrameLines.Take(chunkSize).ToList() : unencodedFrameLines;     // Take all remaining frames if process is done
                    //Logger.Log($"{unencodedFrameLines.Count} unencoded frame lines, {IOUtils.GetAmountOfFiles(interpFramesFolder, false)} frames in interp folder", true, false, "ffmpeg");

                    string outpath = Path.Combine(videoChunksFolder, "chunks", $"{videoIndex.ToString().PadLeft(4, '0')}{FFmpegUtils.GetExt(Interpolate.current.outMode)}");
                    int firstFrameNum = frameLinesToEncode[0];
                    Logger.Log($"Encoding Chunk #{videoIndex} to '{outpath}' using {Path.GetFileName(interpFramesLines[frameLinesToEncode.First()])} through {Path.GetFileName(Path.GetFileName(interpFramesLines[frameLinesToEncode.Last()]))}", true, false, "ffmpeg");
                    await CreateVideo.EncodeChunk(outpath, Interpolate.current.outMode, firstFrameNum, frameLinesToEncode.Count);

                    if(Interpolate.canceled) return;

                    if (Config.GetInt("autoEncMode") == 2)
                    {
                        foreach (int frame in frameLinesToEncode)
                        {
                            // TODO: Make sure frames are no longer needed (e.g. for dupes) before deleting!!
                            continue;

                            string framePath = Path.Combine(interpFramesPath, interpFramesLines[frame]);
                            File.WriteAllText(framePath, "THIS IS A DUMMY FILE - DO NOT DELETE ME");    // Overwrite to save space without breaking progress counter
                        }
                    }

                    encodedFrameLines.AddRange(frameLinesToEncode);
                    //Logger.Log($"Adding {frameLinesToEncode.Count} frameLinesToEncode to encodedFrameLines, new count is {encodedFrameLines.Count}");

                    Logger.Log("Done Encoding Chunk #" + videoIndex, true, false, "ffmpeg");
                    videoIndex++;
                    busy = false;
                }
                await Task.Delay(50);
            }

            if (Interpolate.canceled) return;

            IOUtils.ReverseRenaming(AiProcess.filenameMap, true);   // Get timestamps back

            await CreateVideo.ChunksToVideos(Interpolate.current.tempFolder, videoChunksFolder, Interpolate.current.outFilename);
        }

        public static bool HasWorkToDo ()
        {
            if (Interpolate.canceled || interpFramesFolder == null) return false;
            //Logger.Log($"HasWorkToDo - Process Running: {(AiProcess.currentAiProcess != null && !AiProcess.currentAiProcess.HasExited)} - encodedFrameLines.Count: {encodedFrameLines.Count} - interpFramesLines.Length: {interpFramesLines.Length}");
            return ((AiProcess.currentAiProcess != null && !AiProcess.currentAiProcess.HasExited) || encodedFrameLines.Count < interpFramesLines.Length);
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
            return IOUtils.GetAmountOfFiles(interpFramesFolder, false, $"*.{InterpolateUtils.GetOutExt()}");
        }
    }
}
