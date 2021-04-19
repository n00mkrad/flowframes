using Flowframes.Data;
using Flowframes.IO;
using Flowframes.Main;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.AvProcess;
using Utils = Flowframes.Media.FFmpegUtils;

namespace Flowframes.Media
{
    partial class FfmpegEncode : FfmpegCommands
    {
        public static async Task FramesToVideoConcat(string framesFile, string outPath, Interpolate.OutMode outMode, Fraction fps, LogMode logMode = LogMode.OnlyLastLine, bool isChunk = false)
        {
            await FramesToVideo(framesFile, outPath, outMode, fps, 0, logMode, isChunk);
        }

        public static async Task FramesToVideo(string framesFile, string outPath, Interpolate.OutMode outMode, Fraction fps, float resampleFps, LogMode logMode = LogMode.OnlyLastLine, bool isChunk = false)
        {
            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding video..." : $"Encoding video resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            Directory.CreateDirectory(outPath.GetParentDir());
            string encArgs = Utils.GetEncArgs(Utils.GetCodec(outMode));
            if (!isChunk) encArgs += $" -movflags +faststart";
            string inArg = $"-f concat -i {Path.GetFileName(framesFile)}";
            string linksDir = Path.Combine(framesFile + Paths.symlinksSuffix);

            if (Config.GetBool("allowSymlinkEncoding", true) && Symlinks.SymlinksAllowed())
            {
                await MakeSymlinks(framesFile, linksDir, Padding.interpFrames);

                if(IOUtils.GetAmountOfFiles(linksDir, false) > 1)
                    inArg = $"-i {Path.GetFileName(framesFile) + Paths.symlinksSuffix}/%{Padding.interpFrames}d.png";
                else
                    Logger.Log("Symlink creation seems to have failed even though SymlinksAllowed was true! Encoding ini with concat demuxer instead.", true);
            }

            string rate = fps.ToString().Replace(",", ".");
            string vf = (resampleFps <= 0) ? "" : $"-vf fps=fps={resampleFps.ToStringDot()}";
            string extraArgs = Config.Get("ffEncArgs");
            string args = $"-vsync 0 -r {rate} {inArg} {encArgs} {vf} {extraArgs} -threads {Config.GetInt("ffEncThreads")} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, "error", TaskType.Encode, !isChunk);
            IOUtils.TryDeleteIfExists(linksDir);
        }

        static async Task MakeSymlinks(string framesFile, string linksDir, int zPad = 8)
        {
            try
            {
                Directory.CreateDirectory(linksDir);
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                Logger.Log($"Creating symlinks for '{framesFile}' in '{linksDir} with zPadding {zPad}'", true);

                int counter = 0;

                Dictionary<string, string> pathsLinkTarget = new Dictionary<string, string>();

                foreach (string line in File.ReadAllLines(framesFile))
                {
                    string relTargetPath =
                        line.Remove("file '").Split('\'').FirstOrDefault(); // Relative path in frames file
                    string absTargetPath = Path.Combine(framesFile.GetParentDir(), relTargetPath); // Full path to frame
                    string linkPath = Path.Combine(linksDir,
                        counter.ToString().PadLeft(zPad, '0') + Path.GetExtension(relTargetPath));
                    pathsLinkTarget.Add(linkPath, absTargetPath);
                    counter++;
                }

                await Symlinks.CreateSymlinksParallel(pathsLinkTarget);
            }
            catch (Exception e)
            {
                Logger.Log("MakeSymlinks Exception: " + e.Message);
            }
        }

        public static async Task FramesToGifConcat(string framesFile, string outPath, Fraction rate, bool palette, int colors = 64, float resampleFps = -1, LogMode logMode = LogMode.OnlyLastLine)
        {
            if (rate.GetFloat() > 50f && resampleFps < 50f)
                resampleFps = 50f;  // Force limit framerate as encoding above 50 will cause problems

            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding GIF..." : $"Encoding GIF resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            
            string vfrFilename = Path.GetFileName(framesFile);
            string dither = Config.Get("gifDitherType").Split(' ').First();
            string paletteFilter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither={dither}\"" : "";
            string fpsFilter = (resampleFps <= 0) ? "" : $"fps=fps={resampleFps.ToStringDot()}";
            string vf = FormatUtils.ConcatStrings(new string[] { paletteFilter, fpsFilter });
            string args = $"-f concat -r {rate} -i {vfrFilename.Wrap()} -f gif {vf} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), LogMode.OnlyLastLine, "error", TaskType.Encode);
        }
    }
}
