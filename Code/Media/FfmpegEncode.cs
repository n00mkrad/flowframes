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
                Logger.Log((resampleFps <= 0) ? "Encoding video..." : $"Encoding video resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            
            Directory.CreateDirectory(outPath.GetParentDir());
            string encArgs = Utils.GetEncArgs(Utils.GetCodec(outMode));
            if (!isChunk) encArgs += $" -movflags +faststart";
            string inArg = $"-f concat -i {Path.GetFileName(framesFile)}";
            string linksDir = Path.Combine(framesFile + Paths.symlinksSuffix);

            if (Config.GetBool("allowSymlinkEncoding", true) && Symlinks.SymlinksAllowed())
            {
                if (await Symlinks.MakeSymlinksForEncode(framesFile, linksDir, Padding.interpFrames))
                    inArg = $"-i {Path.GetFileName(framesFile) + Paths.symlinksSuffix}/%{Padding.interpFrames}d.png";
            }

            string rate = fps.ToString().Replace(",", ".");
            string vf = (resampleFps <= 0) ? "" : $"-vf fps=fps={resampleFps.ToStringDot()}";
            string extraArgs = Config.Get("ffEncArgs");
            string args = $"-vsync 0 -r {rate} {inArg} {encArgs} {vf} {extraArgs} -threads {Config.GetInt("ffEncThreads")} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, "error", TaskType.Encode, !isChunk);
            IOUtils.TryDeleteIfExists(linksDir);
        }

        public static async Task FramesToFrames(string framesFile, string outDir, Fraction fps, float resampleFps, string format = "png", LogMode logMode = LogMode.OnlyLastLine)
        {
            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? "Exporting frames..." : $"Exporting frames resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");

            Directory.CreateDirectory(outDir);
            string inArg = $"-f concat -i {Path.GetFileName(framesFile)}";
            string linksDir = Path.Combine(framesFile + Paths.symlinksSuffix);

            if (Config.GetBool("allowSymlinkEncoding", true) && Symlinks.SymlinksAllowed())
            {
                if (await Symlinks.MakeSymlinksForEncode(framesFile, linksDir, Padding.interpFrames))
                    inArg = $"-i {Path.GetFileName(framesFile) + Paths.symlinksSuffix}/%{Padding.interpFrames}d.png";
            }

            string rate = fps.ToString().Replace(",", ".");
            string vf = (resampleFps <= 0) ? "" : $"-vf fps=fps={resampleFps.ToStringDot()}";
            string compression = format == "png" ? pngCompr : "-q:v 1";
            string args = $"-vsync 0 -r {rate} {inArg} {compression} {vf} \"{outDir}/%{Padding.interpFrames}d.{format}\"";
            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, "error", TaskType.Encode, true);
            IOUtils.TryDeleteIfExists(linksDir);
        }

        public static async Task FramesToGifConcat(string framesFile, string outPath, Fraction rate, bool palette, int colors = 64, float resampleFps = -1, LogMode logMode = LogMode.OnlyLastLine)
        {
            if (rate.GetFloat() > 50f && resampleFps < 50f)
                resampleFps = 50f;  // Force limit framerate as encoding above 50 will cause problems

            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding GIF..." : $"Encoding GIF resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            
            string framesFilename = Path.GetFileName(framesFile);
            string dither = Config.Get("gifDitherType").Split(' ').First();
            string paletteFilter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither={dither}\"" : "";
            string fpsFilter = (resampleFps <= 0) ? "" : $"fps=fps={resampleFps.ToStringDot()}";
            string vf = FormatUtils.ConcatStrings(new string[] { paletteFilter, fpsFilter });
            string args = $"-f concat -r {rate} -i {framesFilename.Wrap()} -gifflags -offsetting {vf} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), LogMode.OnlyLastLine, "error", TaskType.Encode);
        }
    }
}
