using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Flowframes.AvProcess;
using Utils = Flowframes.Media.FfmpegUtils;

namespace Flowframes.Media
{
    partial class FfmpegEncode : FfmpegCommands
    {
        public static async Task FramesToVideo(string framesFile, string outPath, OutputSettings settings, Fraction fps, Fraction resampleFps, float itsScale, VidExtraData extraData, LogMode logMode = LogMode.OnlyLastLine, bool isChunk = false)
        {
            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps.Float <= 0) ? "Encoding video..." : $"Encoding video resampled to {resampleFps.GetString()} FPS...");

            IoUtils.RenameExistingFileOrDir(outPath);
            Directory.CreateDirectory(outPath.GetParentDir());
            string[] encArgs = Utils.GetEncArgs(settings, (Interpolate.currentSettings.OutputResolution.IsEmpty ? Interpolate.currentSettings.InputResolution : Interpolate.currentSettings.OutputResolution), Interpolate.currentSettings.outFps.Float);

            string inArg = $"-f concat -i {Path.GetFileName(framesFile)}";
            string linksDir = Path.Combine(framesFile + Paths.symlinksSuffix);

            if (Config.GetBool(Config.Key.allowSymlinkEncoding, true) && Symlinks.SymlinksAllowed())
            {
                if (await Symlinks.MakeSymlinksForEncode(framesFile, linksDir, Padding.interpFrames))
                    inArg = $"-i \"{linksDir}/%{Padding.interpFrames}d{GetConcatFileExt(framesFile)}\"";
            }

            string args = "";

            for (int i = 0; i < encArgs.Length; i++)
            {
                string pre = i == 0 ? "" : $" && ffmpeg {AvProcess.GetFfmpegDefaultArgs()}";
                string post = (i == 0 && encArgs.Length > 1) ? $"-f null -" : outPath.Wrap();
                args += $"{pre} {GetFfmpegExportArgsIn(fps, itsScale)} {inArg} {encArgs[i]} {await GetFfmpegExportArgsOut(resampleFps, extraData, settings, isChunk)} {post} ";
            }

            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, !isChunk);
            IoUtils.TryDeleteIfExists(linksDir);
        }

        public static string GetFfmpegExportArgsIn(Fraction fps, float itsScale, int rotation = 0)
        {
            var args = new List<string>();
            fps = fps / new Fraction(itsScale);
            args.AddIf($"-r {fps}", fps > 0.1f);
            return string.Join(" ", args);
        }

        public static async Task<string> GetFfmpegExportArgsOut(Fraction resampleFps, VidExtraData extraData, OutputSettings settings, bool isChunk = false, string alphaPassFile = "", bool allowPad = true)
        {
            var beforeArgs = new List<string>();
            var filters = new List<string>();
            var extraArgs = new List<string> { Config.Get(Config.Key.ffEncArgs) };
            var mf = Interpolate.currentMediaFile;
            int inputs = 1;

            if (Config.GetBool(Config.Key.keepColorSpace) && extraData.HasAnyColorValues)
            {
                Logger.Log($"Using color data: {extraData.ColorsStr}", true, false, "ffmpeg");
                extraArgs.AddIf($"-colorspace {extraData.ColSpace}", extraData.ColSpace.IsNotEmpty());
                extraArgs.AddIf($"-color_primaries {extraData.ColPrimaries}", extraData.ColPrimaries.IsNotEmpty());
                extraArgs.AddIf($"-color_trc {extraData.ColTransfer}", extraData.ColTransfer.IsNotEmpty());
                extraArgs.AddIf($"-color_range:v {extraData.ColRange.Wrap()}", extraData.ColRange.IsNotEmpty());
            }

            if (!isChunk && (settings.Format == Enums.Output.Format.Mp4 || settings.Format == Enums.Output.Format.Mov))
                extraArgs.Add($"-movflags +faststart");

            if (resampleFps.Float >= 0.1f)
            {
                if (Interpolate.currentMediaFile.IsVfr && !Interpolate.currentSettings.dedupe)
                {
                    Logger.Log($"Won't add fps filter as VFR handling already outputs at desired frame rate ({resampleFps.Float} FPS)", true);
                }
                else
                {
                    filters.Add($"fps={resampleFps}");
                }
            }

            if (alphaPassFile.IsNotEmpty())
            {
                beforeArgs.Add($"-i {alphaPassFile.Wrap()}");
                filters.Add($"[{inputs}:v]alphamerge");
                inputs++;
            }

            if (settings.Format == Enums.Output.Format.Gif)
            {
                string dither = Config.Get(Config.Key.gifDitherType).Split(' ').First();
                int colors = OutputUtils.GetGifColors(ParseUtils.GetEnum<Enums.Encoding.Quality.GifColors>(settings.Quality, true, Strings.VideoQuality));
                string palettePath = Path.Combine(Paths.GetSessionDataPath(), "palette.png");
                await FfmpegExtract.GeneratePalette(mf.ImportPath, palettePath, colors);

                if (File.Exists(palettePath))
                {
                    beforeArgs.Add($"-i {palettePath.Wrap()}");
                    inputs++;
                    filters.Add($"[{inputs - 1}:v]paletteuse=dither={dither}");
                }
            }
            else if (settings.Encoder == Enums.Encoding.Encoder.Exr)
            {
                if (mf.Format.Upper() != "EXR")
                    filters.Add($"zscale=transfer=linear,format={settings.PixelFormat.ToString().Lower()}".Wrap());
            }

            filters.AddIf(GetPadFilter(Interpolate.currentSettings.ScaledResolution.Width, Interpolate.currentSettings.ScaledResolution.Height), allowPad);
            filters = filters.Where(f => f.IsNotEmpty()).ToList();

            return filters.Count > 0 ?
                $"{string.Join(" ", beforeArgs)} -filter_complex [0:v]{string.Join("[vf],[vf]", filters)}[vf] -map [vf] {string.Join(" ", extraArgs)}" :
                $"{string.Join(" ", beforeArgs)} {string.Join(" ", extraArgs)}";
        }

        public static string GetConcatFileExt(string concatFilePath)
        {
            return Path.GetExtension(File.ReadAllLines(concatFilePath).FirstOrDefault().Split('\'')[1]);
        }

        public static async Task FramesToFrames(string framesFile, string outDir, int startNo, Fraction fps, Fraction resampleFps, Enums.Encoding.Encoder format = Enums.Encoding.Encoder.Png, int lossyQ = 1, LogMode logMode = LogMode.OnlyLastLine)
        {
            Directory.CreateDirectory(outDir);
            string inArg = $"-f concat -i {Path.GetFileName(framesFile)}";
            string linksDir = Path.Combine(framesFile + Paths.symlinksSuffix);

            if (Config.GetBool(Config.Key.allowSymlinkEncoding, true) && Symlinks.SymlinksAllowed())
            {
                if (await Symlinks.MakeSymlinksForEncode(framesFile, linksDir, Padding.interpFrames))
                    inArg = $"-i {Path.GetFileName(framesFile) + Paths.symlinksSuffix}/%{Padding.interpFrames}d{GetConcatFileExt(framesFile)}";
            }

            var ffArgs = new List<string>()
            {
                $"-r {fps.ToString().Replace(",", ".")}", // Rate
                inArg,
                format == Enums.Encoding.Encoder.Webp ? "-c:v libwebp" : "", // Codec - Specify libwebp to avoid putting all frames into single animated WEBP
                format == Enums.Encoding.Encoder.Png ? pngCompr : $"-q:v {lossyQ}", // Compression
                $"-start_number {startNo}",
                resampleFps.Float < 0.1f ? "" : $"-vf fps=fps={resampleFps}", // FPS Resample
                "-fps_mode passthrough",
                $"{outDir}/%{Padding.interpFrames}d.{format.GetInfo().OverideExtension}".Wrap(),
            };

            await RunFfmpeg(string.Join(" ", ffArgs.Where(s => s.IsNotEmpty())), framesFile.GetParentDir(), logMode, "error", true);
            IoUtils.TryDeleteIfExists(linksDir);
        }

        public static async Task FramesToGifConcat(string framesFile, string outPath, Fraction rate, bool palette, int colors, Fraction resampleFps, float itsScale, LogMode logMode = LogMode.OnlyLastLine)
        {
            if (rate.Float > 50f && (resampleFps.Float > 50f || resampleFps.Float < 1))
                resampleFps = new Fraction(50, 1);  // Force limit framerate as encoding above 50 will cause problems

            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps.Float <= 0) ? $"Encoding GIF..." : $"Encoding GIF resampled to {resampleFps.Float.ToString().Replace(",", ".")} FPS...");

            string framesFilename = Path.GetFileName(framesFile);
            string dither = Config.Get(Config.Key.gifDitherType).Split(' ').First();
            string paletteFilter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither={dither}\"" : "";
            string fpsFilter = (resampleFps.Float <= 0) ? "" : $"fps=fps={resampleFps}";
            string vf = FormatUtils.ConcatStrings(new string[] { paletteFilter, fpsFilter });
            string extraArgs = Config.Get(Config.Key.ffEncArgs);
            rate = rate / new Fraction(itsScale);
            string args = $"-f concat -r {rate} -i {framesFilename.Wrap()} -gifflags -offsetting {vf} {extraArgs} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), LogMode.OnlyLastLine, "error");
        }
    }
}
