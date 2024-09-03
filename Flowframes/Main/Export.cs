using Flowframes.IO;
using Flowframes.Magick;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Padding = Flowframes.Data.Padding;
using I = Flowframes.Interpolate;
using System.Diagnostics;
using Flowframes.Data;
using Flowframes.Media;
using Flowframes.MiscUtils;
using Flowframes.Os;
using System.Collections.Generic;
using Newtonsoft.Json;
using Flowframes.Ui;

namespace Flowframes.Main
{
    class Export
    {


        public static async Task ExportFrames(string path, string outFolder, OutputSettings exportSettings, bool stepByStep)
        {
            if (Config.GetInt(Config.Key.sceneChangeFillMode) == 1)
            {
                string frameFile = Path.Combine(I.currentSettings.tempFolder, Paths.GetFrameOrderFilename(I.currentSettings.interpFactor));
                await Blend.BlendSceneChanges(frameFile);
            }

            if (exportSettings.Encoder.GetInfo().IsImageSequence)     // Copy interp frames out of temp folder and skip video export for image seq export
            {
                try
                {
                    await ExportImageSequence(path, stepByStep);
                }
                catch (Exception e)
                {
                    Logger.Log("Failed to move interpolated frames: " + e.Message);
                    Logger.Log("Stack Trace:\n " + e.StackTrace, true);
                }

                return;
            }

            if (IoUtils.GetAmountOfFiles(path, false, "*" + I.currentSettings.interpExt) <= 1)
            {
                I.Cancel("Output folder does not contain frames - An error must have occured during interpolation!", AiProcess.hasShownError);
                return;
            }

            Program.mainForm.SetStatus("Creating output video from frames...");

            try
            {
                string max = Config.Get(Config.Key.maxFps);
                Fraction maxFps = max.Contains("/") ? new Fraction(max) : new Fraction(max.GetFloat());
                bool fpsLimit = maxFps.GetFloat() > 0f && I.currentSettings.outFps.GetFloat() > maxFps.GetFloat();
                bool dontEncodeFullFpsVid = fpsLimit && Config.GetInt(Config.Key.maxFpsMode) == 0;

                if (!dontEncodeFullFpsVid)
                    await Encode(exportSettings, path, Path.Combine(outFolder, await IoUtils.GetCurrentExportFilename(false, true)), I.currentSettings.outFps, new Fraction());

                if (fpsLimit)
                    await Encode(exportSettings, path, Path.Combine(outFolder, await IoUtils.GetCurrentExportFilename(true, true)), I.currentSettings.outFps, maxFps);
            }
            catch (Exception e)
            {
                Logger.Log("FramesToVideo Error: " + e.Message, false);
                UiUtils.ShowMessageBox("An error occured while trying to convert the interpolated frames to a video.\nCheck the log for details.", UiUtils.MessageType.Error);
            }
        }

        public static async Task<string> GetPipedFfmpegCmd(bool ffplay = false)
        {
            InterpSettings s = I.currentSettings;
            string encArgs = FfmpegUtils.GetEncArgs(s.outSettings, (s.ScaledResolution.IsEmpty ? s.InputResolution : s.ScaledResolution), s.outFps.GetFloat(), true).FirstOrDefault();

            string max = Config.Get(Config.Key.maxFps);
            Fraction maxFps = max.Contains("/") ? new Fraction(max) : new Fraction(max.GetFloat());
            bool fpsLimit = maxFps.GetFloat() > 0f && s.outFps.GetFloat() > maxFps.GetFloat();

            // Logger.Log($"VFR Ratio: {I.currentMediaFile.VideoStreams.First().FpsInfo.VfrRatio} ({I.currentMediaFile.VideoStreams.First().FpsInfo.Fps} FPS Specified, {I.currentMediaFile.VideoStreams.First().FpsInfo.SpecifiedFps} FPS Avg)");

            bool gifInput = I.currentMediaFile.Format.Upper() == "GIF"; // If input is GIF, we don't need to check the color space etc
            VidExtraData extraData = gifInput ? new VidExtraData() : await FfmpegCommands.GetVidExtraInfo(s.inPath);
            string extraArgsIn = await FfmpegEncode.GetFfmpegExportArgsIn(s.outFps, s.outItsScale);
            string extraArgsOut = await FfmpegEncode.GetFfmpegExportArgsOut(fpsLimit ? maxFps : new Fraction(), extraData, s.outSettings);

            if(s.outSettings.Encoder == Enums.Encoding.Encoder.Exr)
            {
                extraArgsIn += " -color_trc bt709 -color_primaries bt709 -colorspace bt709";
            }


            if (ffplay)
            {
                bool useNutPipe = true; // TODO: Make this bool a config flag
                encArgs = useNutPipe ? "-c:v rawvideo -pix_fmt rgba" : $"-pix_fmt yuv444p16";
                string format = useNutPipe ? "nut" : "yuv4mpegpipe";

                return
                    $"{extraArgsIn} -i pipe: {encArgs} {extraArgsOut} -f {format} - | ffplay - " +
                    $"-autoexit -seek_interval {VapourSynthUtils.GetSeekSeconds(Program.mainForm.currInDuration)} " +
                    $"-window_title \"Flowframes Realtime Interpolation ({s.inFps.GetString()} FPS x{s.interpFactor} = {s.outFps.GetString()} FPS) ({s.model.Name})\" ";
            }
            else
            {
                bool imageSequence = s.outSettings.Encoder.GetInfo().IsImageSequence;
                s.FullOutPath = Path.Combine(s.outPath, await IoUtils.GetCurrentExportFilename(fpsLimit, !imageSequence));
                IoUtils.RenameExistingFileOrDir(s.FullOutPath);

                if (imageSequence)
                {
                    Directory.CreateDirectory(s.FullOutPath);
                    s.FullOutPath += $"/%{Padding.interpFrames}d.{s.outSettings.Encoder.GetInfo().OverideExtension}";
                }

                return $"{extraArgsIn} -i pipe: {extraArgsOut} {encArgs} {s.FullOutPath.Wrap()}";
            }
        }

        static async Task ExportImageSequence(string framesPath, bool stepByStep)
        {
            Program.mainForm.SetStatus("Copying output frames...");
            Enums.Encoding.Encoder desiredFormat = I.currentSettings.outSettings.Encoder;
            string availableFormat = Path.GetExtension(IoUtils.GetFilesSorted(framesPath, "*.*")[0]).Remove(".").Upper();
            string max = Config.Get(Config.Key.maxFps);
            Fraction maxFps = max.Contains("/") ? new Fraction(max) : new Fraction(max.GetFloat());
            bool fpsLimit = maxFps.GetFloat() > 0f && I.currentSettings.outFps.GetFloat() > maxFps.GetFloat();
            bool dontEncodeFullFpsSeq = fpsLimit && Config.GetInt(Config.Key.maxFpsMode) == 0;
            string framesFile = Path.Combine(framesPath.GetParentDir(), Paths.GetFrameOrderFilename(I.currentSettings.interpFactor));

            if (!dontEncodeFullFpsSeq)
            {
                string outputFolderPath = Path.Combine(I.currentSettings.outPath, await IoUtils.GetCurrentExportFilename(false, false));
                IoUtils.RenameExistingFolder(outputFolderPath);
                Logger.Log($"Exporting {desiredFormat.ToString().Upper()} frames to '{Path.GetFileName(outputFolderPath)}'...");

                if (desiredFormat.GetInfo().OverideExtension.Upper() == availableFormat.Upper())   // Move if frames are already in the desired format
                    await CopyOutputFrames(framesPath, framesFile, outputFolderPath, 1, fpsLimit, false);
                else    // Encode if frames are not in desired format
                    await FfmpegEncode.FramesToFrames(framesFile, outputFolderPath, 1, I.currentSettings.outFps, new Fraction(), desiredFormat, OutputUtils.GetImgSeqQ(I.currentSettings.outSettings));
            }

            if (fpsLimit)
            {
                string outputFolderPath = Path.Combine(I.currentSettings.outPath, await IoUtils.GetCurrentExportFilename(true, false));
                Logger.Log($"Exporting {desiredFormat.ToString().Upper()} frames to '{Path.GetFileName(outputFolderPath)}' (Resampled to {maxFps} FPS)...");
                await FfmpegEncode.FramesToFrames(framesFile, outputFolderPath, 1, I.currentSettings.outFps, maxFps, desiredFormat, OutputUtils.GetImgSeqQ(I.currentSettings.outSettings));
            }

            if (!stepByStep)
                await IoUtils.DeleteContentsOfDirAsync(I.currentSettings.interpFolder);
        }

        static async Task CopyOutputFrames(string framesPath, string framesFile, string outputFolderPath, int startNo, bool dontMove, bool hideLog)
        {
            IoUtils.CreateDir(outputFolderPath);
            Stopwatch sw = new Stopwatch();
            sw.Restart();

            string[] framesLines = IoUtils.ReadLines(framesFile);

            for (int idx = 1; idx <= framesLines.Length; idx++)
            {
                string line = framesLines[idx - 1];
                string inFilename = line.RemoveComments().Split('/').Last().Remove("'").Trim();
                string framePath = Path.Combine(framesPath, inFilename);
                string outFilename = Path.Combine(outputFolderPath, startNo.ToString().PadLeft(Padding.interpFrames, '0')) + Path.GetExtension(framePath);
                startNo++;

                if (dontMove || ((idx < framesLines.Length) && framesLines[idx].Contains(inFilename)))   // If file is re-used in the next line, copy instead of move
                    File.Copy(framePath, outFilename);
                else
                    File.Move(framePath, outFilename);

                if (sw.ElapsedMilliseconds >= 500 || idx == framesLines.Length)
                {
                    sw.Restart();
                    Logger.Log($"Moving output frames... {idx}/{framesLines.Length}", hideLog, true);
                    await Task.Delay(1);
                }
            }
        }

        static async Task Encode(OutputSettings settings, string framesPath, string outPath, Fraction fps, Fraction resampleFps)
        {
            string framesFile = Path.Combine(framesPath.GetParentDir(), Paths.GetFrameOrderFilename(I.currentSettings.interpFactor));

            if (!File.Exists(framesFile))
            {
                bool sbs = Config.GetInt(Config.Key.processingMode) == 1;
                I.Cancel($"Frame order file for this interpolation factor not found!{(sbs ? "\n\nDid you run the interpolation step with the current factor?" : "")}");
                return;
            }

            if (settings.Format == Enums.Output.Format.Gif)
            {
                int paletteColors = OutputUtils.GetGifColors(ParseUtils.GetEnum<Enums.Encoding.Quality.GifColors>(settings.Quality, true, Strings.VideoQuality));
                await FfmpegEncode.FramesToGifConcat(framesFile, outPath, fps, true, paletteColors, resampleFps, I.currentSettings.outItsScale);
            }
            else
            {
                VidExtraData extraData = await FfmpegCommands.GetVidExtraInfo(I.currentSettings.inPath);
                await FfmpegEncode.FramesToVideo(framesFile, outPath, settings, fps, resampleFps, I.currentSettings.outItsScale, extraData);
                await MuxOutputVideo(I.currentSettings.inPath, outPath);
                await Loop(outPath, await GetLoopTimes());
            }
        }

        public static async Task MuxPipedVideo(string inputVideo, string outputPath)
        {
            await MuxOutputVideo(inputVideo, Path.Combine(outputPath, outputPath));
            await Loop(outputPath, await GetLoopTimes());
        }

        public static async Task ChunksToVideo(string tempFolder, string chunksFolder, string baseOutPath, bool isBackup = false)
        {
            if (IoUtils.GetAmountOfFiles(chunksFolder, true, "*" + FfmpegUtils.GetExt(I.currentSettings.outSettings)) < 1)
            {
                I.Cancel("No video chunks found - An error must have occured during chunk encoding!", AiProcess.hasShownError);
                return;
            }

            NmkdStopwatch sw = new NmkdStopwatch();

            if (!isBackup)
                Program.mainForm.SetStatus("Merging video chunks...");

            try
            {
                DirectoryInfo chunksDir = new DirectoryInfo(chunksFolder);
                foreach (DirectoryInfo dir in chunksDir.GetDirectories())
                {
                    string suffix = dir.Name.Replace("chunks", "");
                    string tempConcatFile = Path.Combine(tempFolder, $"chunks-concat{suffix}.ini");
                    string concatFileContent = "";

                    foreach (string vid in IoUtils.GetFilesSorted(dir.FullName))
                        concatFileContent += $"file '{Paths.chunksDir}/{dir.Name}/{Path.GetFileName(vid)}'\n";

                    File.WriteAllText(tempConcatFile, concatFileContent);
                    Logger.Log($"CreateVideo: Running MergeChunks() for frames file '{Path.GetFileName(tempConcatFile)}'", true);
                    bool fpsLimit = dir.Name.Contains(Paths.fpsLimitSuffix);
                    string outPath = Path.Combine(baseOutPath, await IoUtils.GetCurrentExportFilename(fpsLimit, true));
                    await MergeChunks(tempConcatFile, outPath, isBackup);

                    if (!isBackup)
                        Task.Run(async () => { await IoUtils.TryDeleteIfExistsAsync(IoUtils.FilenameSuffix(outPath, Paths.backupSuffix)); });
                }
            }
            catch (Exception e)
            {
                Logger.Log("ChunksToVideo Error: " + e.Message, isBackup);

                if (!isBackup)
                    UiUtils.ShowMessageBox("An error occured while trying to merge the video chunks.\nCheck the log for details.", UiUtils.MessageType.Error);
            }

            Logger.Log($"Merged video chunks in {sw}", true);
        }

        static async Task MergeChunks(string framesFile, string outPath, bool isBackup = false)
        {
            if (isBackup)
            {
                outPath = IoUtils.FilenameSuffix(outPath, Paths.backupSuffix);
                await IoUtils.TryDeleteIfExistsAsync(outPath);
            }

            await FfmpegCommands.ConcatVideos(framesFile, outPath, -1, !isBackup);

            if (!isBackup || (isBackup && Config.GetInt(Config.Key.autoEncBackupMode) == 2))     // Mux if no backup, or if backup AND muxing is enabled for backups
                await MuxOutputVideo(I.currentSettings.inPath, outPath, isBackup, !isBackup);

            if (!isBackup)
                await Loop(outPath, await GetLoopTimes());
        }

        public static async Task EncodeChunk(string outPath, string interpDir, int chunkNo, OutputSettings settings, int firstFrameNum, int framesAmount)
        {
            string framesFileFull = Path.Combine(I.currentSettings.tempFolder, Paths.GetFrameOrderFilename(I.currentSettings.interpFactor));
            string concatFile = Path.Combine(I.currentSettings.tempFolder, Paths.GetFrameOrderFilenameChunk(firstFrameNum, firstFrameNum + framesAmount));
            File.WriteAllLines(concatFile, IoUtils.ReadLines(framesFileFull).Skip(firstFrameNum).Take(framesAmount));

            List<string> inputFrames = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(framesFileFull + ".inputframes.json")).Skip(firstFrameNum).Take(framesAmount).ToList();

            if (Config.GetInt(Config.Key.sceneChangeFillMode) == 1)
                await Blend.BlendSceneChanges(concatFile, false);

            string max = Config.Get(Config.Key.maxFps);
            Fraction maxFps = max.Contains("/") ? new Fraction(max) : new Fraction(max.GetFloat());
            bool fpsLimit = maxFps.GetFloat() != 0 && I.currentSettings.outFps.GetFloat() > maxFps.GetFloat();
            VidExtraData extraData = await FfmpegCommands.GetVidExtraInfo(I.currentSettings.inPath);

            bool dontEncodeFullFpsVid = fpsLimit && Config.GetInt(Config.Key.maxFpsMode) == 0;

            if (settings.Encoder.GetInfo().IsImageSequence)    // Image Sequence output mode, not video
            {
                string desiredFormat = settings.Encoder.GetInfo().OverideExtension;
                string availableFormat = Path.GetExtension(IoUtils.GetFilesSorted(interpDir)[0]).Remove(".").Upper();

                if (!dontEncodeFullFpsVid)
                {
                    string outFolderPath = Path.Combine(I.currentSettings.outPath, await IoUtils.GetCurrentExportFilename(false, false));
                    int startNo = IoUtils.GetAmountOfFiles(outFolderPath, false) + 1;

                    if (chunkNo == 1)    // Only check for existing folder on first chunk, otherwise each chunk makes a new folder
                        IoUtils.RenameExistingFolder(outFolderPath);

                    if (desiredFormat.Upper() == availableFormat.Upper())   // Move if frames are already in the desired format
                        await CopyOutputFrames(interpDir, concatFile, outFolderPath, startNo, fpsLimit, true);
                    else    // Encode if frames are not in desired format
                        await FfmpegEncode.FramesToFrames(concatFile, outFolderPath, startNo, I.currentSettings.outFps, new Fraction(), settings.Encoder, OutputUtils.GetImgSeqQ(settings), AvProcess.LogMode.Hidden);
                }

                if (fpsLimit)
                {
                    string outputFolderPath = Path.Combine(I.currentSettings.outPath, await IoUtils.GetCurrentExportFilename(true, false));
                    int startNumber = IoUtils.GetAmountOfFiles(outputFolderPath, false) + 1;
                    await FfmpegEncode.FramesToFrames(concatFile, outputFolderPath, startNumber, I.currentSettings.outFps, maxFps, settings.Encoder, OutputUtils.GetImgSeqQ(settings), AvProcess.LogMode.Hidden);
                }
            }
            else
            {
                if (!dontEncodeFullFpsVid)
                    await FfmpegEncode.FramesToVideo(concatFile, outPath, settings, I.currentSettings.outFps, new Fraction(), I.currentSettings.outItsScale, extraData, AvProcess.LogMode.Hidden, true);     // Encode

                if (fpsLimit)
                {
                    string filename = Path.GetFileName(outPath);
                    string newParentDir = outPath.GetParentDir() + Paths.fpsLimitSuffix;
                    outPath = Path.Combine(newParentDir, filename);
                    await FfmpegEncode.FramesToVideo(concatFile, outPath, settings, I.currentSettings.outFps, maxFps, I.currentSettings.outItsScale, extraData, AvProcess.LogMode.Hidden, true);     // Encode with limited fps
                }
            }

            AutoEncodeResume.encodedChunks += 1;
            AutoEncodeResume.encodedFrames += framesAmount;
            AutoEncodeResume.processedInputFrames.AddRange(inputFrames);
        }

        static async Task Loop(string outPath, int looptimes)
        {
            if (looptimes < 1 || !Config.GetBool(Config.Key.enableLoop)) return;
            Logger.Log($"Looping {looptimes} {(looptimes == 1 ? "time" : "times")} to reach target length of {Config.GetInt(Config.Key.minOutVidLength)}s...");
            await FfmpegCommands.LoopVideo(outPath, looptimes, Config.GetInt(Config.Key.loopMode) == 0);
        }

        static async Task<int> GetLoopTimes()
        {
            int times = -1;
            int minLength = Config.GetInt(Config.Key.minOutVidLength);
            int minFrameCount = (minLength * I.currentSettings.outFps.GetFloat()).RoundToInt();
            int outFrames = (I.currentMediaFile.FrameCount * I.currentSettings.interpFactor).RoundToInt();
            if (outFrames / I.currentSettings.outFps.GetFloat() < minLength)
                times = (int)Math.Ceiling((double)minFrameCount / (double)outFrames);
            times--;    // Not counting the 1st play (0 loops)
            if (times <= 0) return -1;      // Never try to loop 0 times, idk what would happen, probably nothing
            return times;
        }

        public static async Task MuxOutputVideo(string inputPath, string outVideo, bool shortest = false, bool showLog = true)
        {
            if (!File.Exists(outVideo))
            {
                I.Cancel($"No video was encoded!\n\nFFmpeg Output:\n{AvProcess.lastOutputFfmpeg}");
                return;
            }

            if (!Config.GetBool(Config.Key.keepAudio) && !Config.GetBool(Config.Key.keepAudio))
                return;

            if (showLog)
                Program.mainForm.SetStatus("Muxing audio/subtitles into video...");

            if (I.currentSettings.inputIsFrames)
            {
                Logger.Log("Skipping muxing from input step as there is no input video, only frames.", true);
                return;
            }

            try
            {
                await FfmpegAudioAndMetadata.MergeStreamsFromInput(inputPath, outVideo, I.currentSettings.tempFolder, shortest);
            }
            catch (Exception e)
            {
                Logger.Log("Failed to merge audio/subtitles with output video!", !showLog);
                Logger.Log("MergeAudio() Exception: " + e.Message, true);
            }
        }
    }
}
