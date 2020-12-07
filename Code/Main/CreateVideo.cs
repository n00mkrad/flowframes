using Flowframes;
using Flowframes.FFmpeg;
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

namespace Flowframes.Main
{
    class CreateVideo
    {
        public static async Task Export(string path, string outPath, i.OutMode mode)
        {
            if (!mode.ToString().ToLower().Contains("vid"))     // Copy interp frames out of temp folder and skip video export for image seq export
            {
                try
                {
                    Logger.Log("Moving interpolated frames out of temp folder...");
                    string copyPath = Path.Combine(i.currentTempDir.ReplaceLast("-temp", "-interpolated"));
                    Logger.Log($"{path} -> {copyPath}");
                    IOUtils.CreateDir(copyPath);
                    IOUtils.Copy(path, copyPath, true);
                }
                catch(Exception e)
                {
                    Logger.Log("Failed to move interp frames folder: " + e.Message);
                }
                return;
            }

            //Logger.Log("zero-padding " + path);
            //IOUtils.ZeroPadDir(path, $"*.{InterpolateUtils.lastExt}", Padding.interpFrames);

            if (IOUtils.GetAmountOfFiles(path, false, $"*.{InterpolateUtils.lastExt}") <= 1)
            {
                i.Cancel("Output folder does not contain frames - An error must have occured during interpolation!", AiProcess.hasShownError);
                return;
            }
            await Task.Delay(10);
            Program.mainForm.SetStatus("Creating output video from frames...");
            try
            {
                int maxFps = Config.GetInt("maxFps");
                if (maxFps == 0) maxFps = 500;

                if (mode == i.OutMode.VidMp4 && i.currentOutFps > maxFps)
                    await Encode(mode, path, outPath, i.currentOutFps, maxFps, (Config.GetInt("maxFpsMode") == 1));
                else
                    await Encode(mode, path, outPath, i.currentOutFps);
            }
            catch (Exception e)
            {
                Logger.Log("FramesToVideo Error: " + e.Message, false);
                MessageBox.Show("An error occured while trying to convert the interpolated frames to a video.\nCheck the log for details.");
            }
        }

        static async Task Encode(i.OutMode mode, string framesPath, string outPath, float fps, float changeFps = -1, bool keepOriginalFpsVid = true)
        {
            if (mode == i.OutMode.VidGif)
            {
                if (new DirectoryInfo(framesPath).GetFiles()[0].Extension != ".png")
                {
                    Logger.Log("Converting output frames to PNG to encode with Gifski...");
                    await Converter.Convert(framesPath, ImageMagick.MagickFormat.Png00, 20, "png", false);
                }
                await GifskiCommands.CreateGifFromFrames(i.currentOutFps.RoundToInt(), Config.GetInt("gifskiQ"), framesPath, outPath);
            }

            if (mode == i.OutMode.VidMp4)
            {
                int looptimes = GetLoopTimes();
                bool h265 = Config.GetInt("mp4Enc") == 1;
                int crf = h265 ? Config.GetInt("h265Crf") : Config.GetInt("h264Crf");

                string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-{i.lastInterpFactor}x.ini");
                await FFmpegCommands.FramesToMp4Vfr(vfrFile, outPath, h265, crf, fps, i.constantFrameRate);

                /*      DELETE THIS AS SOON AS I'M SURE I CAN USE VFR WITH TIMING DISABLED
                if (Config.GetInt("timingMode") == 1 && Config.GetInt("dedupMode") != 0)
                {
                    string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-x{i.lastInterpFactor}.ini");
                    await FFmpegCommands.FramesToMp4Vfr(vfrFile, outPath, h265, crf, fps, -1);
                }
                else
                {
                    await FFmpegCommands.FramesToMp4(framesPath, outPath, h265, crf, fps, "", false, -1, InterpolateUtils.lastExt);   // Create video
                }
                */

                await MergeAudio(i.lastInputPath, outPath);

                if (looptimes > 0)
                    await Loop(outPath, looptimes);

                if (changeFps > 0)
                {
                    string newOutPath = IOUtils.FilenameSuffix(outPath, $"-{changeFps.ToString("0")}fps");
                    Program.mainForm.SetStatus("Creating video with desired frame rate...");
                    await FFmpegCommands.ConvertFramerate(outPath, newOutPath, h265, crf, changeFps, !keepOriginalFpsVid);
                    await MergeAudio(i.lastInputPath, newOutPath);
                    if (looptimes > 0)
                        await Loop(newOutPath, looptimes);
                }
            }
        }

        public static async Task ChunksToVideo(string chunksPath, string vfrFile, string outPath)
        {
            if (IOUtils.GetAmountOfFiles(chunksPath, false, "*.mp4") < 1)
            {
                i.Cancel("No video chunks found - An error must have occured during chunk encoding!", AiProcess.hasShownError);
                return;
            }
            await Task.Delay(10);
            Program.mainForm.SetStatus("Merging video chunks...");
            try
            {
                int maxFps = Config.GetInt("maxFps");
                if (maxFps == 0) maxFps = 500;

                if (i.currentOutFps > maxFps)
                    await MergeChunks(chunksPath, vfrFile, outPath, i.currentOutFps, maxFps, (Config.GetInt("maxFpsMode") == 1));
                else
                    await MergeChunks(chunksPath, vfrFile, outPath, i.currentOutFps);
            }
            catch (Exception e)
            {
                Logger.Log("ChunksToVideo Error: " + e.Message, false);
                MessageBox.Show("An error occured while trying to merge the video chunks.\nCheck the log for details.");
            }
        }

        static async Task MergeChunks(string chunksPath, string vfrFile, string outPath, float fps, float changeFps = -1, bool keepOriginalFpsVid = true)
        {
            int looptimes = GetLoopTimes();

            bool h265 = Config.GetInt("mp4Enc") == 1;
            int crf = h265 ? Config.GetInt("h265Crf") : Config.GetInt("h264Crf");

            await FFmpegCommands.ConcatVideos(vfrFile, outPath, fps, -1);
            await MergeAudio(i.lastInputPath, outPath);

            if (looptimes > 0)
                await Loop(outPath, looptimes);

            if (changeFps > 0)
            {
                string newOutPath = IOUtils.FilenameSuffix(outPath, $"-{changeFps.ToString("0")}fps");
                Program.mainForm.SetStatus("Creating video with desired frame rate...");
                await FFmpegCommands.ConvertFramerate(outPath, newOutPath, h265, crf, changeFps, !keepOriginalFpsVid);
                await MergeAudio(i.lastInputPath, newOutPath);
                if (looptimes > 0)
                    await Loop(newOutPath, looptimes);
            }
        }

        public static async Task EncodeChunk(string outPath, int firstFrameNum, int framesAmount)
        {
            bool h265 = Config.GetInt("mp4Enc") == 1;
            int crf = h265 ? Config.GetInt("h265Crf") : Config.GetInt("h264Crf");

            string vfrFileOriginal = Path.Combine(i.currentTempDir, $"vfr-{i.lastInterpFactor}x.ini");
            string vfrFile = Path.Combine(i.currentTempDir, $"vfr-chunk-temp.ini");
            File.WriteAllLines(vfrFile, IOUtils.ReadLines(vfrFileOriginal).Skip(firstFrameNum * 2).Take(framesAmount * 2));

            await FFmpegCommands.FramesToMp4VfrChunk(vfrFile, outPath, h265, crf, i.currentOutFps, i.constantFrameRate);
            IOUtils.TryDeleteIfExists(vfrFile);
        }

        static async Task Loop(string outPath, int looptimes)
        {
            Logger.Log($"Looping {looptimes} times to reach target length...");
            await FFmpegCommands.LoopVideo(outPath, looptimes, Config.GetInt("loopMode") == 0);
        }

        static int GetLoopTimes()
        {
            //Logger.Log("Getting loop times for path " + framesOutPath);
            int times = -1;
            int minLength = Config.GetInt("minOutVidLength");
            //Logger.Log("minLength: " + minLength);
            int minFrameCount = (minLength * i.currentOutFps).RoundToInt();
            //Logger.Log("minFrameCount: " + minFrameCount);
            //int outFrames = new DirectoryInfo(framesOutPath).GetFiles($"*.{InterpolateUtils.lastExt}", SearchOption.TopDirectoryOnly).Length;
            int outFrames = i.currentInputFrameCount * i.lastInterpFactor;
            //Logger.Log("outFrames: " + outFrames);
            if (outFrames / i.currentOutFps < minLength)
                times = (int)Math.Ceiling((double)minFrameCount / (double)outFrames);
            //Logger.Log("times: " + times);
            times--;    // Account for this calculation not counting the 1st play (0 loops)
            if (times <= 0) return -1;      // Never try to loop 0 times, idk what would happen, probably nothing
            return times;
        }

        public static async Task MergeAudio(string inputPath, string outVideo, int looptimes = -1)
        {
            if (!Config.GetBool("enableAudio")) return;
            try
            {
                string audioFileBasePath = Path.Combine(i.currentTempDir, "audio");

                if (inputPath != null && IOUtils.IsPathDirectory(inputPath) && !File.Exists(IOUtils.GetAudioFile(audioFileBasePath)))   // Try loading out of same folder as input if input is a folder
                    audioFileBasePath = Path.Combine(i.currentTempDir.GetParentDir(), "audio");

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
