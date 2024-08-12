using Flowframes.Media;
using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flowframes.Ui;
using Flowframes.Os;
using Padding = Flowframes.Data.Padding;

namespace Flowframes.Main
{
    class AutoEncode
    {
        static string interpFramesFolder;
        static string videoChunksFolder;
        public static int chunkSize;    // Encode every n frames
        public static int safetyBufferFrames;      // Ignore latest n frames to avoid using images that haven't been fully encoded yet
        public static string[] interpFramesLines;
        public static List<int> encodedFrameLines = new List<int>();
        public static List<int> unencodedFrameLines = new List<int>();

        public static bool debug;
        public static bool busy;
        public static bool paused;

        public static Task currentMuxTask;

        public static void UpdateChunkAndBufferSizes ()
        {
            chunkSize = GetChunkSize((IoUtils.GetAmountOfFiles(Interpolate.currentSettings.framesFolder, false, "*" + Interpolate.currentSettings.framesExt) * Interpolate.currentSettings.interpFactor).RoundToInt());

            safetyBufferFrames = 90;

            if (Interpolate.currentSettings.ai.Backend == AI.AiBackend.Ncnn)
                safetyBufferFrames = Config.GetInt(Config.Key.autoEncSafeBufferNcnn, 150);

            if (Interpolate.currentSettings.ai.Backend == AI.AiBackend.Pytorch)
                safetyBufferFrames = Config.GetInt(Config.Key.autoEncSafeBufferCuda, 90);
        }

        public static async Task MainLoop(string interpFramesPath)
        {
            if(!AutoEncodeResume.resumeNextRun)
                AutoEncodeResume.Reset();

            debug = Config.GetBool("autoEncDebug", false);

            try
            {
                UpdateChunkAndBufferSizes();

                bool imgSeq = Interpolate.currentSettings.outSettings.Encoder.GetInfo().IsImageSequence;
                interpFramesFolder = interpFramesPath;
                videoChunksFolder = Path.Combine(interpFramesPath.GetParentDir(), Paths.chunksDir);

                if (Interpolate.currentlyUsingAutoEnc)
                    Directory.CreateDirectory(videoChunksFolder);

                encodedFrameLines.Clear();
                unencodedFrameLines.Clear();

                Logger.Log($"[AE] Starting AutoEncode MainLoop - Chunk Size: {chunkSize} Frames - Safety Buffer: {safetyBufferFrames} Frames", true);
                int chunkNo = AutoEncodeResume.encodedChunks + 1;
                string encFile = Path.Combine(interpFramesPath.GetParentDir(), Paths.GetFrameOrderFilename(Interpolate.currentSettings.interpFactor));
                interpFramesLines = IoUtils.ReadLines(encFile).Select(x => x.Split('/').Last().Remove("'").Split('#').First()).ToArray();     // Array with frame filenames

                while (!Interpolate.canceled && GetInterpFramesAmount() < 2)
                    await Task.Delay(1000);

                int lastEncodedFrameNum = 0;

                while (HasWorkToDo())    // Loop while proc is running and not all frames have been encoded
                {
                    if (Interpolate.canceled) return;

                    if (paused)
                    {
                        await Task.Delay(200);
                        continue;
                    }

                    unencodedFrameLines.Clear();

                    bool aiRunning = !AiProcess.lastAiProcess.HasExited;

                    for (int frameLineNum = lastEncodedFrameNum; frameLineNum < interpFramesLines.Length; frameLineNum++)
                    {
                        if (aiRunning && interpFramesLines[frameLineNum].Contains(InterpolationProgress.lastFrame.ToString().PadLeft(Padding.interpFrames, '0')))
                            break;

                        unencodedFrameLines.Add(frameLineNum);
                    }

                    if (Config.GetBool(Config.Key.alwaysWaitForAutoEnc))
                    {
                        int maxFrames = chunkSize + (0.5f * chunkSize).RoundToInt() + safetyBufferFrames;
                        bool overwhelmed = unencodedFrameLines.Count > maxFrames;
                        
                        if(overwhelmed && !AiProcessSuspend.aiProcFrozen && OsUtils.IsProcessHidden(AiProcess.lastAiProcess))
                        {
                            string dirSize = FormatUtils.Bytes(IoUtils.GetDirSize(Interpolate.currentSettings.interpFolder, true));
                            Logger.Log($"AutoEnc is overwhelmed! ({unencodedFrameLines.Count} unencoded frames > {maxFrames}) - Pausing.", true);
                            AiProcessSuspend.SuspendResumeAi(true);
                        }
                        else if (!overwhelmed && AiProcessSuspend.aiProcFrozen)
                        {
                            AiProcessSuspend.SuspendResumeAi(false);
                        }
                    }

                    if (unencodedFrameLines.Count > 0 && (unencodedFrameLines.Count >= (chunkSize + safetyBufferFrames) || !aiRunning))     // Encode every n frames, or after process has exited
                    {
                        try
                        {
                            List<int> frameLinesToEncode = aiRunning ? unencodedFrameLines.Take(chunkSize).ToList() : unencodedFrameLines;     // Take all remaining frames if process is done
                            string lastOfChunk = Path.Combine(interpFramesPath, interpFramesLines[frameLinesToEncode.Last()]);

                            if (!File.Exists(lastOfChunk))
                            {
                                if(debug)
                                    Logger.Log($"[AE] Last frame of chunk doesn't exist; skipping loop iteration ({lastOfChunk})", true);

                                await Task.Delay(500);
                                continue;
                            }

                            busy = true;
                            string outpath = Path.Combine(videoChunksFolder, "chunks", $"{chunkNo.ToString().PadLeft(4, '0')}{FfmpegUtils.GetExt(Interpolate.currentSettings.outSettings)}");
                            string firstFile = Path.GetFileName(interpFramesLines[frameLinesToEncode.First()].Trim());
                            string lastFile = Path.GetFileName(interpFramesLines[frameLinesToEncode.Last()].Trim());
                            Logger.Log($"[AE] Encoding Chunk #{chunkNo} to using line {frameLinesToEncode.First()} ({firstFile}) through {frameLinesToEncode.Last()} ({lastFile}) - {unencodedFrameLines.Count} unencoded frames left in total", true, false, "ffmpeg");

                            await Export.EncodeChunk(outpath, Interpolate.currentSettings.interpFolder, chunkNo, Interpolate.currentSettings.outSettings, frameLinesToEncode.First(), frameLinesToEncode.Count);

                            if (Interpolate.canceled) return;

                            if (aiRunning && Config.GetInt(Config.Key.autoEncMode) == 2)
                                Task.Run(() => DeleteOldFramesAsync(interpFramesPath, frameLinesToEncode));

                            if (Interpolate.canceled) return;

                            encodedFrameLines.AddRange(frameLinesToEncode);
                            Logger.Log("[AE] Done Encoding Chunk #" + chunkNo, true, false, "ffmpeg");
                            lastEncodedFrameNum = (frameLinesToEncode.Last() + 1);
                            chunkNo++;
                            AutoEncodeResume.Save();

                            if(!imgSeq && Config.GetInt(Config.Key.autoEncBackupMode) > 0)
                            {
                                if (aiRunning && (currentMuxTask == null || (currentMuxTask != null && currentMuxTask.IsCompleted)))
                                    currentMuxTask = Task.Run(() => Export.ChunksToVideo(Interpolate.currentSettings.tempFolder, videoChunksFolder, Interpolate.currentSettings.outPath, true));
                                else
                                    Logger.Log($"[AE] Skipping backup because {(!aiRunning ? "this is the final chunk" : "previous mux task has not finished yet")}!", true, false, "ffmpeg");
                            }
                            
                            busy = false;
                        }
                        catch (Exception e)
                        {
                            Logger.Log($"AutoEnc Chunk Encoding Error: {e.Message}. Stack Trace:\n{e.StackTrace}");
                            Interpolate.Cancel("Auto-Encode encountered an error.");
                        }
                    }

                    await Task.Delay(50);
                }

                if (Interpolate.canceled) return;

                while (currentMuxTask != null && !currentMuxTask.IsCompleted)
                    await Task.Delay(100);

                if (imgSeq)
                    return;

                await Export.ChunksToVideo(Interpolate.currentSettings.tempFolder, videoChunksFolder, Interpolate.currentSettings.outPath);
            }
            catch (Exception e)
            {
                Logger.Log($"AutoEnc Error: {e.Message}. Stack Trace:\n{e.StackTrace}");
                Interpolate.Cancel("Auto-Encode encountered an error.");
            }
        }

        static async Task DeleteOldFramesAsync (string interpFramesPath, List<int> frameLinesToEncode)
        {
            if(debug)
                Logger.Log("[AE] Starting DeleteOldFramesAsync.", true, false, "ffmpeg");

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            foreach (int frame in frameLinesToEncode)
            {
                if (!FrameIsStillNeeded(interpFramesLines[frame], frame))    // Make sure frames are no longer needed (for dupes) before deleting!
                {
                    string framePath = Path.Combine(interpFramesPath, interpFramesLines[frame]);
                    //IOUtils.OverwriteFileWithText(framePath);    // Overwrite to save space without breaking progress counter
                    IoUtils.TryDeleteIfExists(framePath);
                    InterpolationProgress.deletedFramesCount++;
                }
            }

            if (debug)
                Logger.Log("[AE] DeleteOldFramesAsync finished in " + FormatUtils.TimeSw(sw), true, false, "ffmpeg");
        }

        static bool FrameIsStillNeeded (string frameName, int frameIndex)
        {
            if ((frameIndex + 1) < interpFramesLines.Length && interpFramesLines[frameIndex+1].Contains(frameName))
                return true;
            return false;
        }

        public static bool HasWorkToDo ()
        {
            if (Interpolate.canceled || interpFramesFolder == null) return false;

            if(debug)
                Logger.Log($"[AE] HasWorkToDo - Process Running: {(AiProcess.lastAiProcess != null && !AiProcess.lastAiProcess.HasExited)} - encodedFrameLines.Count: {encodedFrameLines.Count} - interpFramesLines.Length: {interpFramesLines.Length}", true);
            
            return ((AiProcess.lastAiProcess != null && !AiProcess.lastAiProcess.HasExited) || encodedFrameLines.Count < interpFramesLines.Length);
        }

        static int GetChunkSize(int targetFramesAmount)
        {
            if (targetFramesAmount > 100000) return 4800;
            if (targetFramesAmount > 50000) return 2400;
            if (targetFramesAmount > 20000) return 1200;
            if (targetFramesAmount > 5000) return 600;
            if (targetFramesAmount > 1000) return 300;
            return 150;
        }

        static int GetInterpFramesAmount()
        {
            return IoUtils.GetAmountOfFiles(interpFramesFolder, false, "*" + Interpolate.currentSettings.interpExt);
        }
    }
}
