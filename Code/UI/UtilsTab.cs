﻿using Flowframes.IO;
using Flowframes.Main;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.UI
{
    class UtilsTab
    {


        public static async Task ExtractVideo(string videoPath, bool withAudio)
        {
            string outPath = Path.ChangeExtension(videoPath, null) + "-extracted";
            Program.mainForm.SetWorking(true);
            await FFmpegCommands.VideoToFrames(videoPath, Path.Combine(outPath, Paths.framesDir), false, Interpolate.current.inFps, false, false, false);
            File.WriteAllText(Path.Combine(outPath, "fps.ini"), Interpolate.current.inFps.ToString());
            if (withAudio)
                await FFmpegCommands.ExtractAudio(videoPath, Path.Combine(outPath, "audio"));
            Program.mainForm.SetWorking(false);
            Logger.Log("Done.");
        }

        public static async Task LoopVideo(string inputFile, ComboBox loopTimes)
        {
            if (!InputIsValid(inputFile))
                return;
            int times = loopTimes.GetInt();
            Logger.Log("Lopping video " + times + "x...", true);
            await FFmpegCommands.LoopVideo(inputFile, times, false);
            Logger.Log("Done", true);
        }

        public static async Task ChangeSpeed(string inputFile, ComboBox speed)
        {
            if (!InputIsValid(inputFile))
                return;
            float speedFloat = speed.GetFloat();
            Logger.Log("Creating video with " + speed + "% speed...", true);
            await FFmpegCommands.ChangeSpeed(inputFile, speedFloat, false);
            Logger.Log("Done", true);
        }

        public static async Task Convert(string inputFile, ComboBox crfBox)
        {
            if (!InputIsValid(inputFile))
                return;
            int crf = crfBox.GetInt();
            Logger.Log("Creating MP4 with CRF " + crf + "...", true);
            if (Path.GetExtension(inputFile).ToUpper() != ".MP4")
                await FFmpegCommands.Encode(inputFile, "libx264", "aac", crf, 128);
            else
                await FFmpegCommands.Encode(inputFile, "libx264", "copy", crf);      // Copy audio if input is MP4
            Logger.Log("Done", true);
        }

        static bool InputIsValid(string inPath)
        {
            bool isFile = !IOUtils.IsPathDirectory(inPath);
            if ((isFile && !IOUtils.IsFileValid(inPath)) || (!isFile && !IOUtils.IsDirValid(inPath)))
            {
                MessageBox.Show("Input path is not valid!");
                return false;
            }
            return true;
        }

        public static async void Dedupe(string inPath, bool testRun)
        {
            bool isFile = !IOUtils.IsPathDirectory(inPath);
            if ((isFile && !IOUtils.IsFileValid(inPath)) || (!isFile && !IOUtils.IsDirValid(inPath)))
            {
                MessageBox.Show("Input path is not valid!");
                return;
            }

            string framesPath;

            if (isFile)
            {
                Logger.Log("Input is a file, not directory");
                if (!InterpolateUtils.IsVideoValid(inPath))
                {
                    MessageBox.Show("Input file is not valid!", "Error");
                    return;
                }
                Program.mainForm.SetWorking(true);
                await Task.Delay(10);
                framesPath = Path.ChangeExtension(inPath, null) + "-frames";
                Directory.CreateDirectory(framesPath);
                await Interpolate.ExtractFrames(inPath, framesPath, false);
            }
            else
            {
                framesPath = inPath;
            }
            Program.mainForm.SetWorking(true);
            Logger.Log("Running frame de-duplication", true);
            await Task.Delay(10);
            await Magick.Dedupe.Run(framesPath, testRun);
            IOUtils.TryDeleteIfExists(framesPath);
            Program.mainForm.SetProgress(0);
            Program.mainForm.SetWorking(false);
        }
    }
}
