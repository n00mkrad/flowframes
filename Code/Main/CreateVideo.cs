using Flowframes;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
using Flowframes.OS;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Padding = Flowframes.Data.Padding;
using i = Flowframes.Interpolate;
using System.Diagnostics;
using Flowframes.AudioVideo;

namespace Flowframes.Main
{
    class CreateVideo
    {
        static string currentOutFile;   // Keeps track of the out file, in case it gets renamed (FPS limiting, looping, etc) before finishing export

        public static async Task Export(string path, string outPath, i.OutMode mode)
        {
            if (!mode.ToString().ToLower().Contains("vid"))     // Copy interp frames out of temp folder and skip video export for image seq export
            {
                try
                {
                    await CopyOutputFrames(path, Path.GetFileNameWithoutExtension(outPath));
                }
                catch(Exception e)
                {
                    Logger.Log("Failed to move interp frames folder: " + e.Message);
                }
                return;
            }

            if (IOUtils.GetAmountOfFiles(path, false, $"*.{InterpolateUtils.GetOutExt()}") <= 1)
            {
                i.Cancel("Output folder does not contain frames - An error must have occured during interpolation!", AiProcess.hasShownError);
                return;
            }
            await Task.Delay(10);
            Program.mainForm.SetStatus("Creating output video from frames...");
            try
            {
                float maxFps = Config.GetFloat("maxFps");
                bool fpsLimit = maxFps != 0 && i.current.outFps > maxFps;

                bool dontEncodeFullFpsVid = fpsLimit && Config.GetInt("maxFpsMode") == 0;

                if(!dontEncodeFullFpsVid)
                    await Encode(mode, path, outPath, i.current.outFps);

                if (fpsLimit)
                    await Encode(mode, path, outPath.FilenameSuffix($"-{maxFps.ToStringDot("0.00")}fps"), i.current.outFps, maxFps);
            }
            catch (Exception e)
            {
                Logger.Log("FramesToVideo Error: " + e.Message, false);
                MessageBox.Show("An error occured while trying to convert the interpolated frames to a video.\nCheck the log for details.");
            }
        }

        static async Task CopyOutputFrames (string framesPath, string folderName)
        {
            Program.mainForm.SetStatus("Copying output frames...");
            string copyPath = Path.Combine(i.current.outPath, folderName);
            Logger.Log($"Copying interpolated frames to '{copyPath}'");
            IOUtils.TryDeleteIfExists(copyPath);
            IOUtils.CreateDir(copyPath);
            Stopwatch sw = new Stopwatch();
            sw.Restart();

            string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-{i.current.interpFactor}x.ini");
            string[] vfrLines = IOUtils.ReadLines(vfrFile);

            for (int idx = 1; idx <= vfrLines.Length; idx++)
            {
                string line = vfrLines[idx-1];
                string inFilename = line.Split('/').Last().Remove("'").RemoveComments();
                string framePath = Path.Combine(framesPath, inFilename);
                string outFilename = Path.Combine(copyPath, idx.ToString().PadLeft(Padding.interpFrames, '0')) + Path.GetExtension(framePath);

                if ((idx < vfrLines.Length) && vfrLines[idx].Contains(inFilename))   // If file is re-used in the next line, copy instead of move
                    File.Copy(framePath, outFilename);
                else
                    File.Move(framePath, outFilename);

                if (sw.ElapsedMilliseconds >= 500 || idx == vfrLines.Length)
                {
                    sw.Restart();
                    Logger.Log($"Copying interpolated frames to '{Path.GetFileName(copyPath)}' - {idx}/{vfrLines.Length}", false, true);
                    await Task.Delay(1);
                }
            }
        }

        static async Task Encode(i.OutMode mode, string framesPath, string outPath, float fps, float resampleFps = -1)
        {
            currentOutFile = outPath;
            string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-{i.current.interpFactor}x.ini");

            if (mode == i.OutMode.VidGif)
            {
                await FFmpegCommands.FramesToGifConcat(vfrFile, outPath, fps, true, Config.GetInt("gifColors"), resampleFps);
            }
            else
            {
                await FFmpegCommands.FramesToVideoConcat(vfrFile, outPath, mode, fps, resampleFps);
                await MergeAudio(i.current.inPath, outPath);

                int looptimes = GetLoopTimes();
                if (looptimes > 0)
                    await Loop(currentOutFile, looptimes);
            }
        }

        public static async Task ChunksToVideos(string tempFolder, string chunksFolder, string baseOutPath)
        {
            if (IOUtils.GetAmountOfFiles(chunksFolder, true, $"*{FFmpegUtils.GetExt(i.current.outMode)}") < 1)
            {
                i.Cancel("No video chunks found - An error must have occured during chunk encoding!", AiProcess.hasShownError);
                return;
            }

            await Task.Delay(10);
            Program.mainForm.SetStatus("Merging video chunks...");
            try
            {
                DirectoryInfo chunksDir = new DirectoryInfo(chunksFolder);
                foreach(DirectoryInfo dir in chunksDir.GetDirectories())
                {
                    string suffix = dir.Name.Replace("chunks", "");
                    string tempConcatFile = Path.Combine(tempFolder, $"chunks-concat{suffix}.ini");
                    string concatFileContent = "";
                    foreach (string vid in IOUtils.GetFilesSorted(dir.FullName))
                        concatFileContent += $"file '{Paths.chunksDir}/{dir.Name}/{Path.GetFileName(vid)}'\n";
                    File.WriteAllText(tempConcatFile, concatFileContent);

                    Logger.Log($"CreateVideo: Running MergeChunks() for vfrFile '{Path.GetFileName(tempConcatFile)}'", true);
                    await MergeChunks(tempConcatFile, baseOutPath.FilenameSuffix(suffix));
                }
            }
            catch (Exception e)
            {
                Logger.Log("ChunksToVideo Error: " + e.Message, false);
                MessageBox.Show("An error occured while trying to merge the video chunks.\nCheck the log for details.");
            }
        }

        static async Task MergeChunks(string vfrFile, string outPath)
        {
            await FFmpegCommands.ConcatVideos(vfrFile, outPath, -1);
            await MergeAudio(i.current.inPath, outPath);

            int looptimes = GetLoopTimes();
            if (looptimes > 0)
                await Loop(outPath, looptimes);
        }

        public static async Task EncodeChunk(string outPath, i.OutMode mode, int firstFrameNum, int framesAmount)
        {
            string vfrFileOriginal = Path.Combine(i.current.tempFolder, $"vfr-{i.current.interpFactor}x.ini");
            string vfrFile = Path.Combine(i.current.tempFolder, $"vfr-chunk-{firstFrameNum}-{firstFrameNum + framesAmount}.ini");
            File.WriteAllLines(vfrFile, IOUtils.ReadLines(vfrFileOriginal).Skip(firstFrameNum).Take(framesAmount));

            float maxFps = Config.GetFloat("maxFps");
            bool fpsLimit = maxFps != 0 && i.current.outFps > maxFps;

            bool dontEncodeFullFpsVid = fpsLimit && Config.GetInt("maxFpsMode") == 0;

            if(!dontEncodeFullFpsVid)
                await FFmpegCommands.FramesToVideoConcat(vfrFile, outPath, mode, i.current.outFps, AvProcess.LogMode.Hidden, true);     // Encode

            if (fpsLimit)
            {
                string filename = Path.GetFileName(outPath);
                string newParentDir = outPath.GetParentDir() + "-" + maxFps.ToStringDot("0.00") + "fps";
                outPath = Path.Combine(newParentDir, filename);
                await FFmpegCommands.FramesToVideoConcat(vfrFile, outPath, mode, i.current.outFps, maxFps, AvProcess.LogMode.Hidden, true);     // Encode with limited fps
            }
        }

        static async Task Loop(string outPath, int looptimes)
        {
            Logger.Log($"Looping {looptimes} {(looptimes == 1 ? "time" : "times")} to reach target length of {Config.GetInt("minOutVidLength")}s...");
            await FFmpegCommands.LoopVideo(outPath, looptimes, Config.GetInt("loopMode") == 0);
        }

        static int GetLoopTimes()
        {
            int times = -1;
            int minLength = Config.GetInt("minOutVidLength");
            int minFrameCount = (minLength * i.current.outFps).RoundToInt();
            int outFrames = i.currentInputFrameCount * i.current.interpFactor;
            if (outFrames / i.current.outFps < minLength)
                times = (int)Math.Ceiling((double)minFrameCount / (double)outFrames);
            times--;    // Not counting the 1st play (0 loops)
            if (times <= 0) return -1;      // Never try to loop 0 times, idk what would happen, probably nothing
            return times;
        }

        public static async Task MergeAudio(string inputPath, string outVideo, int looptimes = -1)
        {
            if (!Config.GetBool("enableAudio")) return;
            try
            {
                string audioFileBasePath = Path.Combine(i.current.tempFolder, "audio");

                if (inputPath != null && IOUtils.IsPathDirectory(inputPath) && !File.Exists(IOUtils.GetAudioFile(audioFileBasePath)))   // Try loading out of same folder as input if input is a folder
                    audioFileBasePath = Path.Combine(i.current.tempFolder.GetParentDir(), "audio");

                if (!File.Exists(IOUtils.GetAudioFile(audioFileBasePath)))
                    await FFmpegCommands.ExtractAudio(inputPath, audioFileBasePath);      // Extract from sourceVideo to audioFile unless it already exists
                
                if (!File.Exists(IOUtils.GetAudioFile(audioFileBasePath)) || new FileInfo(IOUtils.GetAudioFile(audioFileBasePath)).Length < 4096)
                {
                    Logger.Log("No compatible audio stream found.", true);
                    return;
                }

                await FFmpegCommands.MergeAudio(outVideo, IOUtils.GetAudioFile(audioFileBasePath));        // Merge from audioFile into outVideo
            }
            catch (Exception e)
            {
                Logger.Log("Failed to copy audio!");
                Logger.Log("MergeAudio() Exception: " + e.Message, true);
            }
        }
    }
}
