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
        public static async Task FramesToVideo(string framesFile, string outPath, Interpolate.OutMode outMode, Fraction fps, Fraction resampleFps, VidExtraData extraData, LogMode logMode = LogMode.OnlyLastLine, bool isChunk = false)
        {
            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps.GetFloat() <= 0) ? "Encoding video..." : $"Encoding video resampled to {resampleFps.GetString()} FPS...");
            
            Directory.CreateDirectory(outPath.GetParentDir());
            string encArgs = Utils.GetEncArgs(Utils.GetCodec(outMode));
            if (!isChunk && outMode == Interpolate.OutMode.VidMp4) encArgs += $" -movflags +faststart";
            string inArg = $"-f concat -i {Path.GetFileName(framesFile)}";
            string linksDir = Path.Combine(framesFile + Paths.symlinksSuffix);

            if (Config.GetBool("allowSymlinkEncoding", true) && Symlinks.SymlinksAllowed())
            {
                if (await Symlinks.MakeSymlinksForEncode(framesFile, linksDir, Padding.interpFrames))
                    inArg = $"-i {Path.GetFileName(framesFile) + Paths.symlinksSuffix}/%{Padding.interpFrames}d.png";
            }

            string extraArgs = Config.Get("ffEncArgs");
            string rate = fps.ToString().Replace(",", ".");

            List<string> filters = new List<string>();

            if (resampleFps.GetFloat() >= 0.1f)
                filters.Add($"fps=fps={resampleFps}");

            if (extraData.HasAllValues())
            {
                Logger.Log($"Applying color transfer ({extraData.colorSpace}).", true, false, "ffmpeg");
                filters.Add($"scale=out_color_matrix={extraData.colorSpace}");
                extraArgs += $" -colorspace {extraData.colorSpace} -color_primaries {extraData.colorPrimaries} -color_trc {extraData.colorTransfer} -color_range:v \"{extraData.colorRange}\"";
            }

            string vf = filters.Count > 0 ? $"-vf {string.Join(",", filters)}" : "";
            string args = $"-vsync 0 -r {rate} {inArg} {encArgs} {vf} {GetAspectArg(extraData)} {extraArgs} -threads {Config.GetInt("ffEncThreads")} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, "error", TaskType.Encode, !isChunk);
            IOUtils.TryDeleteIfExists(linksDir);
        }

        static string GetAspectArg (VidExtraData extraData)
        {
            if (!string.IsNullOrWhiteSpace(extraData.displayRatio))
                return $"-aspect {extraData.displayRatio}";
            else
                return "";
        }

        public static async Task FramesToFrames(string framesFile, string outDir, Fraction fps, Fraction resampleFps, string format = "png", LogMode logMode = LogMode.OnlyLastLine)
        {
            Directory.CreateDirectory(outDir);
            string inArg = $"-f concat -i {Path.GetFileName(framesFile)}";
            string linksDir = Path.Combine(framesFile + Paths.symlinksSuffix);

            if (Config.GetBool("allowSymlinkEncoding", true) && Symlinks.SymlinksAllowed())
            {
                if (await Symlinks.MakeSymlinksForEncode(framesFile, linksDir, Padding.interpFrames))
                    inArg = $"-i {Path.GetFileName(framesFile) + Paths.symlinksSuffix}/%{Padding.interpFrames}d.png";
            }

            string rate = fps.ToString().Replace(",", ".");
            string vf = (resampleFps.GetFloat() < 0.1f) ? "" : $"-vf fps=fps={resampleFps}";
            string compression = format == "png" ? pngCompr : "-q:v 1";
            string args = $"-vsync 0 -r {rate} {inArg} {compression} {vf} \"{outDir}/%{Padding.interpFrames}d.{format}\"";
            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, "error", TaskType.Encode, true);
            IOUtils.TryDeleteIfExists(linksDir);
        }

        public static async Task FramesToGifConcat(string framesFile, string outPath, Fraction rate, bool palette, int colors, Fraction resampleFps, LogMode logMode = LogMode.OnlyLastLine)
        {
            if (rate.GetFloat() > 50f && resampleFps.GetFloat() < 50f)
                resampleFps = new Fraction(50, 1);  // Force limit framerate as encoding above 50 will cause problems

            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps.GetFloat() <= 0) ? $"Encoding GIF..." : $"Encoding GIF resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            
            string framesFilename = Path.GetFileName(framesFile);
            string dither = Config.Get("gifDitherType").Split(' ').First();
            string paletteFilter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither={dither}\"" : "";
            string fpsFilter = (resampleFps.GetFloat() <= 0) ? "" : $"fps=fps={resampleFps}";
            string vf = FormatUtils.ConcatStrings(new string[] { paletteFilter, fpsFilter });
            string args = $"-f concat -r {rate} -i {framesFilename.Wrap()} -gifflags -offsetting {vf} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), LogMode.OnlyLastLine, "error", TaskType.Encode);
        }
    }
}
