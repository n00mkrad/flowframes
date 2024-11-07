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
            string[] encArgs = Utils.GetEncArgs(settings, (Interpolate.currentSettings.ScaledResolution.IsEmpty ? Interpolate.currentSettings.InputResolution : Interpolate.currentSettings.ScaledResolution), Interpolate.currentSettings.outFps.Float);

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
                args += $"{pre} {await GetFfmpegExportArgsIn(fps, itsScale)} {inArg} {encArgs[i]} {await GetFfmpegExportArgsOut(resampleFps, extraData, settings, isChunk)} {post} ";
            }

            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, !isChunk);
            IoUtils.TryDeleteIfExists(linksDir);
        }

        public static async Task<string> GetFfmpegExportArgsIn(Fraction fps, float itsScale, int rotation = 0)
        {
            var args = new List<string>();

            fps = fps / new Fraction(itsScale);
            args.Add($"-r {fps}");
            
            if(rotation != 0)
            {
                args.Add($"-display_rotation {rotation}");
            }

            return string.Join(" ", args);
        }

        public static async Task<string> GetFfmpegExportArgsOut(Fraction resampleFps, VidExtraData extraData, OutputSettings settings, bool isChunk = false)
        {
            var beforeArgs = new List<string>();
            var filters = new List<string>();
            var extraArgs = new List<string> { Config.Get(Config.Key.ffEncArgs) };
            var mf = Interpolate.currentMediaFile;

            if (resampleFps.Float >= 0.1f)
                filters.Add($"fps={resampleFps}");

            if (Config.GetBool(Config.Key.keepColorSpace) && extraData.HasAllColorValues())
            {
                Logger.Log($"Using color data: Space {extraData.colorSpace}; Primaries {extraData.colorPrimaries}; Transfer {extraData.colorTransfer}; Range {extraData.colorRange}", true, false, "ffmpeg");
                extraArgs.Add($"-colorspace {extraData.colorSpace} -color_primaries {extraData.colorPrimaries} -color_trc {extraData.colorTransfer} -color_range:v {extraData.colorRange.Wrap()}");
            }

            if (!string.IsNullOrWhiteSpace(extraData.displayRatio) && !extraData.displayRatio.MatchesWildcard("*N/A*"))
                extraArgs.Add($"-aspect {extraData.displayRatio}");

            if (!isChunk && settings.Format == Enums.Output.Format.Mp4 || settings.Format == Enums.Output.Format.Mov)
                extraArgs.Add($"-movflags +faststart");

            if (settings.Format == Enums.Output.Format.Gif)
            {
                string dither = Config.Get(Config.Key.gifDitherType).Split(' ').First();
                string palettePath = Path.Combine(Paths.GetSessionDataPath(), "palette.png");
                string paletteFilter = $"[1:v]paletteuse=dither={dither}";

                int colors = OutputUtils.GetGifColors(ParseUtils.GetEnum<Enums.Encoding.Quality.GifColors>(settings.Quality, true, Strings.VideoQuality));
                await FfmpegExtract.GeneratePalette(mf.ImportPath, palettePath, colors);

                if (File.Exists(palettePath))
                {
                    beforeArgs.Add($"-i {palettePath.Wrap()}");
                    filters.Add(paletteFilter);
                }
            }
            else if (settings.Encoder == Enums.Encoding.Encoder.Exr)
            {
                if(mf.Format.Upper() != "EXR")
                    filters.Add($"zscale=transfer=linear,format={settings.PixelFormat.ToString().Lower()}".Wrap());
            }

            // Only if encoder is not GIF and width and height are not divisible by 2
            if (settings.Encoder != Enums.Encoding.Encoder.Gif && (mf.VideoStreams[0].Resolution.Width % 2 != 0 || mf.VideoStreams[0].Resolution.Height % 2 != 0))
            {
                filters.Add(GetPadFilter());
            }
            
            filters = filters.Where(f => f.IsNotEmpty()).ToList();

            return filters.Count > 0 ?
                $"{string.Join(" ", beforeArgs)} -filter_complex [0:v]{string.Join("[vf],[vf]", filters.Where(f => f.IsNotEmpty()))}[vf] -map [vf] {string.Join(" ", extraArgs)}" :
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

            string sn = $"-start_number {startNo}";
            string rate = fps.ToString().Replace(",", ".");
            string vf = (resampleFps.Float < 0.1f) ? "" : $"-vf fps=fps={resampleFps}";
            string compression = format == Enums.Encoding.Encoder.Png ? pngCompr : $"-q:v {lossyQ}";
            string codec = format == Enums.Encoding.Encoder.Webp ? "-c:v libwebp" : ""; // Specify libwebp to avoid putting all frames into single animated WEBP
            string args = $"-r {rate} {inArg} {codec} {compression} {sn} {vf} -fps_mode passthrough \"{outDir}/%{Padding.interpFrames}d.{format.GetInfo().OverideExtension}\"";
            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, "error", true);
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
