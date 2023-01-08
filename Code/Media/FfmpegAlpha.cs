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
    partial class FfmpegAlpha : FfmpegCommands
    {
        public static async Task ExtractAlphaDir(string rgbDir, string alphaDir)
        {
            Directory.CreateDirectory(alphaDir);

            foreach (FileInfo file in IoUtils.GetFileInfosSorted(rgbDir))
            {
                string args = $"-i {file.FullName.Wrap()} -vf \"format=yuva444p16le,alphaextract,format=yuv420p,{GetPadFilter()}\" {Path.Combine(alphaDir, file.Name).Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden);
            }
        }

        public static async Task RemoveAlpha(string inputDir, string outputDir, string fillColor = "black")
        {
            Directory.CreateDirectory(outputDir);
            foreach (FileInfo file in IoUtils.GetFileInfosSorted(inputDir))
            {
                string outPath = Path.Combine(outputDir, "_" + file.Name);
                Size s = IoUtils.GetImage(file.FullName).Size;
                string args = $" -f lavfi -i color={fillColor}:s={s.Width}x{s.Height} -i {file.FullName.Wrap()} -filter_complex overlay=0:0:shortest=1 {outPath.Wrap()}";
                await RunFfmpeg(args, LogMode.Hidden);
                file.Delete();
                File.Move(outPath, file.FullName);
            }
        }

        public static async Task MergeAlphaIntoRgb(string rgbDir, int rgbPad, string alphaDir, int aPad, bool deleteAlphaDir)
        {
            string filter = "-filter_complex [0:v:0][1:v:0]alphamerge[out] -map [out]";
            string args = $"-i \"{rgbDir}/%{rgbPad}d.png\" -i \"{alphaDir}/%{aPad}d.png\" {filter} \"{rgbDir}/%{rgbPad}d.png\"";
            await RunFfmpeg(args, LogMode.Hidden);

            if (deleteAlphaDir)
                await IoUtils.TryDeleteIfExistsAsync(alphaDir);
        }
    }
}
