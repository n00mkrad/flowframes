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
using Utils = Flowframes.Media.FFmpegUtils;

namespace Flowframes.Media
{
    partial class FfmpegEncode : FfmpegCommands
    {
        public static async Task FramesToVideoConcat(string framesFile, string outPath, Interpolate.OutMode outMode, Fraction fps, LogMode logMode = LogMode.OnlyLastLine, bool isChunk = false)
        {
            await FramesToVideoConcat(framesFile, outPath, outMode, fps, 0, logMode, isChunk);
        }

        public static async Task FramesToVideoConcat(string framesFile, string outPath, Interpolate.OutMode outMode, Fraction fps, float resampleFps, LogMode logMode = LogMode.OnlyLastLine, bool isChunk = false)
        {
            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding video..." : $"Encoding video resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            Directory.CreateDirectory(outPath.GetParentDir());
            string encArgs = Utils.GetEncArgs(Utils.GetCodec(outMode));
            if (!isChunk) encArgs += $" -movflags +faststart";
            string vfrFilename = Path.GetFileName(framesFile);
            string rate = fps.ToString().Replace(",", ".");
            string vf = (resampleFps <= 0) ? "" : $"-vf fps=fps={resampleFps.ToStringDot()}";
            string extraArgs = Config.Get("ffEncArgs");
            string args = $"-vsync 0 -f concat -r {rate} -i {vfrFilename} {encArgs} {vf} {extraArgs} -threads {Config.GetInt("ffEncThreads")} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), logMode, "error", TaskType.Encode, !isChunk);
        }

        public static async Task FramesToGifConcat(string framesFile, string outPath, Fraction rate, bool palette, int colors = 64, float resampleFps = -1, LogMode logMode = LogMode.OnlyLastLine)
        {
            if (logMode != LogMode.Hidden)
                Logger.Log((resampleFps <= 0) ? $"Encoding GIF..." : $"Encoding GIF resampled to {resampleFps.ToString().Replace(",", ".")} FPS...");
            string vfrFilename = Path.GetFileName(framesFile);
            string paletteFilter = palette ? $"-vf \"split[s0][s1];[s0]palettegen={colors}[p];[s1][p]paletteuse=dither=floyd_steinberg\"" : "";
            string fpsFilter = (resampleFps <= 0) ? "" : $"fps=fps={resampleFps.ToStringDot()}";
            string vf = FormatUtils.ConcatStrings(new string[] { paletteFilter, fpsFilter });
            string args = $"-f concat -r {rate} -i {vfrFilename.Wrap()} -f gif {vf} {outPath.Wrap()}";
            await RunFfmpeg(args, framesFile.GetParentDir(), LogMode.OnlyLastLine, "error", TaskType.Encode);
        }

        public static async Task Encode(string inputFile, string vcodec, string acodec, int crf, int audioKbps = 0, bool delSrc = false)
        {
            string outPath = Path.ChangeExtension(inputFile, null) + "-convert.mp4";
            string args = $" -i {inputFile.Wrap()} -c:v {vcodec} -crf {crf} -pix_fmt yuv420p -c:a {acodec} -b:a {audioKbps}k -vf {GetPadFilter()} {outPath.Wrap()}";
            if (string.IsNullOrWhiteSpace(acodec))
                args = args.Replace("-c:a", "-an");
            if (audioKbps < 0)
                args = args.Replace($" -b:a {audioKbps}", "");
            await RunFfmpeg(args, LogMode.OnlyLastLine, TaskType.Encode, true);
            if (delSrc)
                DeleteSource(inputFile);
        }
    }
}
