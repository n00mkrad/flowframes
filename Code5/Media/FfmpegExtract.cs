using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.AvProcess;

namespace Flowframes.Media
{
    partial class FfmpegExtract : FfmpegCommands
    {
        public static async Task ExtractSceneChanges(string inputFile, string frameFolderPath, float rate)
        {
            Logger.Log("Extracting scene changes...");
            await VideoToFrames(inputFile, frameFolderPath, false, rate, false, false, new Size(320, 180), true);
            bool hiddenLog = Interpolate.currentInputFrameCount <= 50;
            int amount = IOUtils.GetAmountOfFiles(frameFolderPath, false);
            Logger.Log($"Detected {amount} scene {(amount == 1 ? "change" : "changes")}.".Replace(" 0 ", " no "), false, !hiddenLog);
        }

        public static async Task VideoToFrames(string inputFile, string framesDir, bool alpha, float rate, bool deDupe, bool delSrc)
        {
            await VideoToFrames(inputFile, framesDir, alpha, rate, deDupe, delSrc, new Size());
        }

        public static async Task VideoToFrames(string inputFile, string framesDir, bool alpha, float rate, bool deDupe, bool delSrc, Size size, bool sceneDetect = false)
        {
            if (!sceneDetect) Logger.Log("Extracting video frames from input video...");
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            IOUtils.CreateDir(framesDir);
            string timecodeStr = /* timecodes ? $"-copyts -r {FrameOrder.timebase} -frame_pts true" : */ "-copyts -frame_pts true";
            string scnDetect = sceneDetect ? $"\"select='gt(scene,{Config.GetFloatString("scnDetectValue")})'\"" : "";
            string mpStr = deDupe ? ((Config.GetInt("mpdecimateMode") == 0) ? mpDecDef : mpDecAggr) : "";
            string filters = FormatUtils.ConcatStrings(new string[] { divisionFilter, scnDetect, mpStr });
            string vf = filters.Length > 2 ? $"-vf {filters}" : "";
            string rateArg = (rate > 0) ? $" -r {rate.ToStringDot()}" : "";
            string pixFmt = alpha ? "-pix_fmt rgba" : "-pix_fmt rgb24";    // Use RGBA for GIF for alpha support
            string args = $"{rateArg} -i {inputFile.Wrap()} {pngComprArg} -vsync 0 {pixFmt} {timecodeStr} {vf} {sizeStr} \"{framesDir}/%{Padding.inputFrames}d.png\"";
            LogMode logMode = Interpolate.currentInputFrameCount > 50 ? LogMode.OnlyLastLine : LogMode.Hidden;
            await AvProcess.RunFfmpeg(args, logMode, TaskType.ExtractFrames, true);
            int amount = IOUtils.GetAmountOfFiles(framesDir, false, "*.png");
            if (!sceneDetect) Logger.Log($"Extracted {amount} {(amount == 1 ? "frame" : "frames")} from input.", false, true);
            await Task.Delay(1);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ImportImages(string inpath, string outpath, bool alpha, Size size, bool delSrc = false, bool showLog = true)
        {
            if (showLog) Logger.Log("Importing images...");
            Logger.Log($"Importing images from {inpath} to {outpath}.", true);
            IOUtils.CreateDir(outpath);
            string concatFile = Path.Combine(Paths.GetDataPath(), "png-concat-temp.ini");
            string concatFileContent = "";
            string[] files = IOUtils.GetFilesSorted(inpath);
            foreach (string img in files)
                concatFileContent += $"file '{img.Replace(@"\", "/")}'\n";
            File.WriteAllText(concatFile, concatFileContent);

            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            string pixFmt = alpha ? "-pix_fmt rgba" : "-pix_fmt rgb24";    // Use RGBA for GIF for alpha support
            string vf = alpha ? $"-filter_complex \"[0:v]{divisionFilter},split[a][b];[a]palettegen=reserve_transparent=on:transparency_color=ffffff[p];[b][p]paletteuse\"" : $"-vf {divisionFilter}";
            string args = $" -loglevel panic -f concat -safe 0 -i {concatFile.Wrap()} {pngComprArg} {sizeStr} {pixFmt} -vsync 0 {vf} \"{outpath}/%{Padding.inputFrames}d.png\"";
            LogMode logMode = IOUtils.GetAmountOfFiles(inpath, false) > 50 ? LogMode.OnlyLastLine : LogMode.Hidden;
            await AvProcess.RunFfmpeg(args, logMode, TaskType.ExtractFrames);
            if (delSrc)
                DeleteSource(inpath);
        }

        public static async Task ImportSingleImage(string inputFile, string outPath, Size size)
        {
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            bool isPng = (Path.GetExtension(outPath).ToLower() == ".png");
            string comprArg = isPng ? pngComprArg : "";
            string pixFmt = "-pix_fmt " + (isPng ? $"rgb24 {comprArg}" : "yuvj420p");
            string args = $"-i {inputFile.Wrap()} {comprArg} {sizeStr} {pixFmt} -vf {divisionFilter} {outPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, LogMode.Hidden, TaskType.ExtractFrames);
        }

        public static async Task ExtractSingleFrame(string inputFile, string outputPath, int frameNum)
        {
            bool isPng = (Path.GetExtension(outputPath).ToLower() == ".png");
            string comprArg = isPng ? pngComprArg : "";
            string pixFmt = "-pix_fmt " + (isPng ? $"rgb24 {comprArg}" : "yuvj420p");
            string args = $"-i {inputFile.Wrap()} -vf \"select=eq(n\\,{frameNum})\" -vframes 1 {pixFmt} {outputPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, LogMode.Hidden, TaskType.ExtractFrames);
        }

        public static async Task ExtractLastFrame(string inputFile, string outputPath, Size size)
        {
            if (IOUtils.IsPathDirectory(outputPath))
                outputPath = Path.Combine(outputPath, "last.png");
            bool isPng = (Path.GetExtension(outputPath).ToLower() == ".png");
            string comprArg = isPng ? pngComprArg : "";
            string pixFmt = "-pix_fmt " + (isPng ? $"rgb24 {comprArg}" : "yuvj420p");
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            string args = $"-sseof -1 -i {inputFile.Wrap()} -update 1 {pixFmt} {sizeStr} {outputPath.Wrap()}";
            await AvProcess.RunFfmpeg(args, LogMode.Hidden, TaskType.ExtractFrames);
        }
    }
}
