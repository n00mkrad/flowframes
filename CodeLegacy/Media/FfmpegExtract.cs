using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.AvProcess;

namespace Flowframes.Media
{
    partial class FfmpegExtract : FfmpegCommands
    {
        public static async Task ExtractSceneChanges(string inPath, string outDir, Fraction rate, bool inputIsFrames, string format)
        {
            Logger.Log("Extracting scene changes...");
            Directory.CreateDirectory(outDir);

            string inArg = $"-i {inPath.Wrap()}";

            if (inputIsFrames)
            {
                string concatFile = Path.Combine(Paths.GetSessionDataPath(), "png-scndetect-concat-temp.ini");
                FfmpegUtils.CreateConcatFile(inPath, concatFile, Filetypes.imagesInterpCompat.ToList());
                inArg = $"-f concat -safe 0 -i {concatFile.Wrap()}";
            }

            string scnDetect = $"-vf \"select='gt(scene,{Config.GetFloatString(Config.Key.scnDetectValue)})'\"";
            string rateArg = (rate.Float > 0) ? $"-fps_mode cfr -r {rate}" : "-fps_mode passthrough";
            string args = $"{GetTrimArg(true)} {inArg} {GetImgArgs(format)} {rateArg} {scnDetect} -frame_pts 1 -s 256x144 {GetTrimArg(false)} \"{outDir}/%{Padding.inputFrames}d{format}\"";

            LogMode logMode = Interpolate.currentMediaFile.FrameCount > 50 ? LogMode.OnlyLastLine : LogMode.Hidden;
            await RunFfmpeg(args, logMode, inputIsFrames ? "panic" : "warning", true);

            bool hiddenLog = Interpolate.currentMediaFile.FrameCount <= 50;
            int amount = IoUtils.GetAmountOfFiles(outDir, false);
            Logger.Log($"Detected {amount} scene {(amount == 1 ? "change" : "changes")}.".Replace(" 0 ", " no "), false, !hiddenLog);
        }

        static string GetImgArgs(string extension, bool includePixFmt = true, bool alpha = false)
        {
            extension = extension.Lower().Remove(".").Replace("jpeg", "jpg");

            string pixFmt = "yuv420p";

            if (Interpolate.currentMediaFile != null && Interpolate.currentMediaFile.VideoStreams.Any())
            {
                pixFmt = Interpolate.currentMediaFile.VideoStreams.First().PixelFormat;
            }

            bool inputHighBitDepth = pixFmt.Contains("p10") || pixFmt.Contains("p16");
            bool outputHighBitDepth = Interpolate.currentSettings.outSettings.PixelFormat.ToString().Lower().Contains("p10");

            string args = "";

            if (extension == "png")
            {
                pixFmt = alpha ? "rgba" : "rgb24"; // PNG can't use YUV so we overwrite it with RGB
                args = pngCompr;
            }
            else if (extension == "jpg")
            {
                // Fallback to YUV420P if not in list of supported formats
                if (!new[] { "yuv420p", "yuv422p", "yuv444p" }.Contains(pixFmt.Replace("yuvj", "yuv")))
                {
                    pixFmt = "yuv420p";
                }

                args = $"-q:v 1 -qmin 1 -color_range pc";
            }
            else if (extension == "tiff")
            {
                // Fallback to YUV420P if not in list of supported formats
                if (!new[] { "rgb24", "rgb48le", "pal8", "rgba", "yuv420p", "yuv422p", "yuv440p", "yuv444p" }.Contains(pixFmt))
                {
                    pixFmt = inputHighBitDepth && outputHighBitDepth ? "rgb48le" : "yuv420p";
                }
            }
            else if (extension == "webp")
            {
                // Fallback to YUV420P if not in list of supported formats
                if (!new[] { "bgra", "yuv420p", "yuva420p" }.Contains(pixFmt))
                {
                    pixFmt = "yuv420p";
                }

                args = $"-q:v 100";
            }

            if (includePixFmt)
                args += $" -pix_fmt {pixFmt}";

            return args;
        }

        public static async Task VideoToFrames(string inputFile, string framesDir, bool alpha, Fraction rate, bool deDupe, bool delSrc, Size size, string format)
        {
            Logger.Log("Extracting video frames from input video...");
            Logger.Log($"VideoToFrames() - Alpha: {alpha} - Rate: {rate} - Size: {size} - Format: {format}", true, false, "ffmpeg");
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            IoUtils.CreateDir(framesDir);
            string mpStr = deDupe ? GetMpdecimate(true) : "";
            string filters = FormatUtils.ConcatStrings(new[] { GetPadFilter(), mpStr });
            string vf = filters.Length > 2 ? $"-vf {filters}" : "";
            bool allowCfr = rate.Float > 0 && !deDupe && Path.GetExtension(inputFile).Lower() != ".gif"; // Forcing CFR on GIFs causes issues // TODO: Maybe never use CFR???
            string rateArg = allowCfr ? $" -fps_mode cfr -r {rate}" : "-fps_mode passthrough";
            string args = $"{GetTrimArg(true)} -itsscale {Interpolate.currentMediaFile.VideoStreams.First().FpsInfo.VfrRatio} -i {inputFile.Wrap()} {GetImgArgs(format, true, alpha)} {rateArg} -frame_pts 1 {vf} {sizeStr} {GetTrimArg(false)} \"{framesDir}/%{Padding.inputFrames}d{format}\""; LogMode logMode = Interpolate.currentMediaFile.FrameCount > 50 ? LogMode.OnlyLastLine : LogMode.Hidden;
            await RunFfmpeg(args, logMode, true);
            int amount = IoUtils.GetAmountOfFiles(framesDir, false, "*" + format);
            Logger.Log($"Extracted {amount} {(amount == 1 ? "frame" : "frames")} from input.", false, true);
            await Task.Delay(1);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ImportImagesCheckCompat(string inPath, string outPath, bool alpha, Size size, bool showLog, string format)
        {
            bool compatible = await Task.Run(async () => { return AreImagesCompatible(inPath, Config.GetInt(Config.Key.maxVidHeight)); });

            if (!alpha && compatible)
            {
                await CopyImages(inPath, outPath, showLog);
            }
            else
            {
                await ImportImages(inPath, outPath, alpha, size, showLog, format);
            }
        }

        public static async Task CopyImages(string inpath, string outpath, bool showLog)
        {
            if (showLog) Logger.Log($"Loading images from {new DirectoryInfo(inpath).Name}...");
            Directory.CreateDirectory(outpath);

            Dictionary<string, string> moveFromTo = new Dictionary<string, string>();
            int counter = 0;

            foreach (FileInfo file in IoUtils.GetFileInfosSorted(inpath))
            {
                string newFilename = counter.ToString().PadLeft(Padding.inputFrames, '0') + file.Extension;
                moveFromTo.Add(file.FullName, Path.Combine(outpath, newFilename));
                counter++;
            }

            if (Config.GetBool(Config.Key.allowSymlinkEncoding) && Config.GetBool(Config.Key.allowSymlinkImport, true))
            {
                Logger.Log($"Symlink Import enabled, creating symlinks for input frames...", true);
                Dictionary<string, string> moveFromToSwapped = moveFromTo.ToDictionary(x => x.Value, x => x.Key);   // From/To => To/From (Link/Target)
                await Symlinks.CreateSymlinksParallel(moveFromToSwapped);
            }
            else
            {
                Logger.Log($"Symlink Import disabled, copying input frames...", true);
                await Task.Run(async () =>
                {
                    foreach (KeyValuePair<string, string> moveFromToPair in moveFromTo)
                        File.Copy(moveFromToPair.Key, moveFromToPair.Value);
                });
            }
        }

        private static bool AreImagesCompatible(string inpath, int maxHeight)
        {
            NmkdStopwatch sw = new NmkdStopwatch();
            string[] validExtensions = Filetypes.imagesInterpCompat; // = new string[] { ".jpg", ".jpeg", ".png" };
            FileInfo[] files = IoUtils.GetFileInfosSorted(inpath);

            if (files.Length < 1)
            {
                Logger.Log("[AreImagesCompatible] Sequence not compatible: No files found.", true);
                return false;
            }

            bool allSameExtension = files.All(x => x.Extension == files.First().Extension);

            if (!allSameExtension)
            {
                Logger.Log($"Sequence not compatible: Not all files have the same extension.", true);
                return false;
            }

            bool allValidExtension = files.All(x => validExtensions.Contains(x.Extension));

            if (!allValidExtension)
            {
                Logger.Log($"Sequence not compatible: Not all files have a valid extension ({string.Join(", ", validExtensions)}).", true);
                return false;
            }

            int sampleCount = Config.GetInt(Config.Key.imgSeqSampleCount, 10);
            Image[] randomSamples = files.OrderBy(arg => Guid.NewGuid()).Take(sampleCount).Select(x => IoUtils.GetImage(x.FullName)).ToArray();

            if(files.All(f => f != null))
            {
                bool allSameSize = randomSamples.All(i => i.Size == randomSamples.First().Size);

                if (!allSameSize)
                {
                    Logger.Log($"Sequence not compatible: Not all images have the same dimensions.", true);
                    return false;
                }

                bool allSmallEnough = randomSamples.All(i => (i.Height <= maxHeight));

                if (!allSmallEnough)
                {
                    Logger.Log($"Sequence not compatible: Image dimensions above max size.", true);
                    return false;
                }

                // bool all24Bit = randomSamples.All(i => (i.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb));
                // 
                // if (!all24Bit)
                // {
                //     Logger.Log($"Sequence not compatible: Some images are not 24-bit (8bpp).", true);
                //     return false;
                // }
            }

            Interpolate.currentSettings.framesExt = files.First().Extension;
            Logger.Log($"Sequence compatible!", true);
            return true;
        }

        public static async Task ImportImages(string inPath, string outPath, bool alpha, Size size, bool showLog, string format)
        {
            if (showLog) Logger.Log($"Importing images from {new DirectoryInfo(inPath).Name}...");
            Logger.Log($"ImportImages() - Alpha: {alpha} - Size: {size} - Format: {format}", true, false, "ffmpeg");
            IoUtils.CreateDir(outPath);
            string concatFile = Path.Combine(Paths.GetSessionDataPath(), "import-concat-temp.ini");
            FfmpegUtils.CreateConcatFile(inPath, concatFile, Filetypes.imagesInterpCompat.ToList());

            string inArg = $"-f concat -safe 0 -i {concatFile.Wrap()}";
            string linksDir = Path.Combine(concatFile + Paths.symlinksSuffix);

            if (Config.GetBool(Config.Key.allowSymlinkEncoding, true) && Symlinks.SymlinksAllowed())
            {
                if (await Symlinks.MakeSymlinksForEncode(concatFile, linksDir, Padding.interpFrames))
                    inArg = $"-i \"{linksDir}/%{Padding.interpFrames}d{FfmpegEncode.GetConcatFileExt(concatFile)}\"";
            }

            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            string vf = $"-vf {GetPadFilter()}";
            string args = $"-r 25 {inArg} {GetImgArgs(format, true, alpha)} {sizeStr} -fps_mode passthrough -start_number 0 {vf} \"{outPath}/%{Padding.inputFrames}d{format}\"";
            LogMode logMode = IoUtils.GetAmountOfFiles(inPath, false) > 50 ? LogMode.OnlyLastLine : LogMode.Hidden;
            await RunFfmpeg(args, logMode, "panic");
        }

        public static string[] GetTrimArgs()
        {
            return new string[] { GetTrimArg(true), GetTrimArg(false) };
        }

        public static string GetTrimArg(bool input)
        {
            if (!QuickSettingsTab.trimEnabled)
                return "";

            int fastSeekThresh = 180;
            bool fastSeek = QuickSettingsTab.trimStartSecs > fastSeekThresh;
            string arg = "";

            if (input)
            {
                if (fastSeek)
                    arg += $"-ss {QuickSettingsTab.trimStartSecs - fastSeekThresh}";
                else
                    return arg;
            }
            else
            {
                if (fastSeek)
                {
                    arg += $"-ss {fastSeekThresh}";

                    long trimTimeSecs = QuickSettingsTab.trimEndSecs - QuickSettingsTab.trimStartSecs;

                    if (QuickSettingsTab.doTrimEnd)
                        arg += $" -to {fastSeekThresh + trimTimeSecs}";
                }
                else
                {
                    arg += $"-ss {QuickSettingsTab.trimStart}";

                    if (QuickSettingsTab.doTrimEnd)
                        arg += $" -to {QuickSettingsTab.trimEnd}";
                }
            }

            return arg;
        }

        public static async Task ImportSingleImage(string inputFile, string outPath, Size size)
        {
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            bool isPng = (Path.GetExtension(outPath).Lower() == ".png");
            string comprArg = isPng ? pngCompr : "";
            string pixFmt = isPng ? $"rgb24 {comprArg}" : "yuv420p -color_range full";
            string args = $"-i {inputFile.Wrap()} {comprArg} {sizeStr} -pix_fmt {pixFmt} -vf {GetPadFilter()} {outPath.Wrap()}";
            await RunFfmpeg(args, LogMode.Hidden);
        }

        public static async Task ExtractSingleFrame(string inputFile, string outputPath, int frameNum)
        {
            bool isPng = (Path.GetExtension(outputPath).Lower() == ".png");
            string comprArg = isPng ? pngCompr : "";
            string pixFmt = isPng ? $"rgb24 {comprArg}" : "yuv420p -color_range full";
            string args = $"-i {inputFile.Wrap()} -vf \"select=eq(n\\,{frameNum})\" -vframes 1 -update 1 -pix_fmt {pixFmt} {outputPath.Wrap()}";
            await RunFfmpeg(args, LogMode.Hidden);
        }

        public static async Task ExtractLastFrame(string inputFile, string outputPath, Size size)
        {
            if (QuickSettingsTab.trimEnabled)
                return;

            if (IoUtils.IsPathDirectory(outputPath))
                outputPath = Path.Combine(outputPath, "last.png");

            bool isPng = (Path.GetExtension(outputPath).Lower() == ".png");
            string comprArg = isPng ? pngCompr : "";
            string pixFmt = isPng ? $"rgb24 {comprArg}" : "yuv420p -color_range full";
            string sizeStr = (size.Width > 1 && size.Height > 1) ? $"-s {size.Width}x{size.Height}" : "";
            string trim = QuickSettingsTab.trimEnabled ? $"-ss {QuickSettingsTab.GetTrimEndMinusOne()} -to {QuickSettingsTab.trimEnd}" : "";
            string sseof = string.IsNullOrWhiteSpace(trim) ? "-sseof -1" : "";
            string args = $"{sseof} -i {inputFile.Wrap()} -update 1 -pix_fmt {pixFmt} {sizeStr} {trim} {outputPath.Wrap()}";
            await RunFfmpeg(args, LogMode.Hidden);
        }

        public static async Task GeneratePalette(string inputFile, string outputPath, int colors = 256)
        {
            string args = $"-i {inputFile.Wrap()} -vf palettegen={colors} {outputPath.Wrap()}";
            await Task.Run(() => AvProcess.RunFfmpegSync(args));
        }
    }
}
