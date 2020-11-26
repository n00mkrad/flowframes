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
using i = Flowframes.Interpolate;

namespace Flowframes.Main
{
    class CreateVideo
    {
        public static async Task FramesToVideo(string path, string outPath, i.OutMode mode)
        {
            if (!mode.ToString().ToLower().Contains("vid"))     // Skip output mode is not a video (e.g. image sequence)
                return;
            if (IOUtils.GetAmountOfFiles(path, false, $"*.{InterpolateUtils.lastExt}") <= 1)
            {
                i.Cancel("Output folder does not contain frames - An error must have occured during interpolation!", AiProcess.hasShownError);
                return;
            }
            await Task.Delay(10);
            //if(Config.GetInt("timingMode") == 1)
            //    await MagickDedupe.Reduplicate(path);
            Program.mainForm.SetStatus("Creating output video from frames...");
            try
            {
                int maxFps = Config.GetInt("maxFps");
                if (maxFps == 0) maxFps = 500;

                if (mode == i.OutMode.VidMp4 && i.currentOutFps > maxFps)
                {
                    bool createSecondVid = (Config.GetInt("maxFpsMode") == 1);
                    await Encode(mode, path, outPath, i.currentOutFps, maxFps, createSecondVid);
                }
                else
                {
                    await Encode(mode, path, outPath, i.currentOutFps);
                }
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
                string ext = InterpolateUtils.lastExt;
                int looptimes = GetLoopTimes(framesPath);

                bool h265 = Config.GetInt("mp4Enc") == 1;
                int crf = h265 ? Config.GetInt("h265Crf") : Config.GetInt("h264Crf");

                if (Config.GetInt("timingMode") == 1 && Config.GetInt("dedupMode") != 0)
                {
                    string vfrFile = Path.Combine(framesPath.GetParentDir(), "vfr.ini");
                    await FFmpegCommands.FramesToMp4Vfr(vfrFile, outPath, h265, crf, fps, looptimes);
                }
                else
                {
                    await FFmpegCommands.FramesToMp4(framesPath, outPath, h265, crf, fps, "", false, -1, ext);   // Create video
                }

                if (looptimes > 0)
                    await Loop(outPath, looptimes);
                await MergeAudio(i.lastInputPath, outPath);

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

        static async Task Loop (string outPath, int looptimes)
        {
            Logger.Log($"Looping {looptimes} times to reach target length");
            await FFmpegCommands.LoopVideo(outPath, looptimes, Config.GetInt("loopMode") == 0);
        }


        static int GetLoopTimes(string framesOutPath)
        {
            int times = -1;
            int minLength = Config.GetInt("minOutVidLength");
            int minFrameCount = (minLength * i.currentOutFps).RoundToInt();
            int outFrames = new DirectoryInfo(framesOutPath).GetFiles($"*.{InterpolateUtils.lastExt}", SearchOption.TopDirectoryOnly).Length;
            if (outFrames / i.currentOutFps < minLength)
                times = (int)Math.Ceiling((double)minFrameCount / (double)outFrames);
            times--;    // Account for this calculation not counting the 1st play (0 loops)
            if (times <= 0) return -1;      // Never try to loop 0 times, idk what would happen, probably nothing
            return times;
        }

        public static async Task MergeAudio(string sourceVideo, string outVideo, int looptimes = -1)
        {
            if (!Config.GetBool("enableAudio")) return;
            try
            {
                Logger.Log("Adding input audio to output video...");
                string audioFileBasePath = Path.Combine(i.currentTempDir, "audio");
                if(IOUtils.IsPathDirectory(sourceVideo) && !File.Exists(IOUtils.GetAudioFile(audioFileBasePath)))   // Try loading out of same folder as input if input is a folder
                        audioFileBasePath = Path.Combine(i.currentTempDir.GetParentDir(), "audio");
                if (!File.Exists(IOUtils.GetAudioFile(audioFileBasePath)))
                    await FFmpegCommands.ExtractAudio(sourceVideo, audioFileBasePath);      // Extract from sourceVideo to audioFile unless it already exists
                if (!File.Exists(IOUtils.GetAudioFile(audioFileBasePath)) || new FileInfo(IOUtils.GetAudioFile(audioFileBasePath)).Length < 4096)
                {
                    Logger.Log("No compatible audio stream found.");
                    return;
                }
                await FFmpegCommands.MergeAudio(outVideo, IOUtils.GetAudioFile(audioFileBasePath));        // Merge from audioFile into outVideo
            }
            catch
            {
                Logger.Log("Failed to copy audio!");
            }
        }
    }
}
