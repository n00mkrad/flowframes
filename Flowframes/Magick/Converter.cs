using Flowframes;
using Flowframes.IO;
using Flowframes.Ui;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Magick
{

    class Converter
    {
        public static async Task Convert (string dir, MagickFormat format, int quality, string ext = "", bool print = true, bool setProgress = true)
        {
            var files = IoUtils.GetFilesSorted(dir);
            if(print) Logger.Log($"Converting {files.Length} files in {dir}");
            int counter = 0;
            foreach (string file in files)
            {
                if (print) Logger.Log("Converting " + Path.GetFileName(file) + " to " + format.ToString().StripNumbers().Upper(), false, true);
                MagickImage img = new MagickImage(file);
                img.Format = format;
                img.Quality = quality;
                string outpath = file;
                if (!string.IsNullOrWhiteSpace(ext)) outpath = Path.ChangeExtension(outpath, ext);
                img.Write(outpath);
                counter++;
                if(setProgress)
                    Program.mainForm.SetProgress((int)Math.Round(((float)counter / files.Length) * 100f));
                await Task.Delay(1);
            }
        }

        public static async Task MakeBinary (string inputDir, string outputDir, bool print = true, bool setProgress = true)
        {
            try
            {
                var files = IoUtils.GetFilesSorted(inputDir);
                if (print) Logger.Log($"Processing alpha channel...");
                Directory.CreateDirectory(outputDir);
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                int counter = 0;
                foreach (string file in files)
                {
                    MagickImage img = new MagickImage(file);
                    img.Format = MagickFormat.Png24;
                    img.Quality = 10;
                    img.Threshold(new Percentage(75));

                    string outPath = Path.Combine(outputDir, Path.GetFileName(file));
                    img.Write(outPath);
                    counter++;
                    if (sw.ElapsedMilliseconds > 250)
                    {
                        if (setProgress)
                            Program.mainForm.SetProgress((int)Math.Round(((float)counter / files.Length) * 100f));
                        await Task.Delay(1);
                        sw.Restart();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("MakeBinary Error: " + e.Message);
            }
        }

        public static async Task ExtractAlpha (string inputDir, string outputDir, bool print = true, bool setProgress = true, bool removeInputAlpha = true)
        {
            try
            {
                var files = IoUtils.GetFilesSorted(inputDir);
                if (print) Logger.Log($"Extracting alpha channel from images...");
                Directory.CreateDirectory(outputDir);
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                int counter = 0;
                foreach (string file in files)
                {
                    MagickImage alphaImg = new MagickImage(file);

                    if (removeInputAlpha)
                    {
                        MagickImage rgbImg = alphaImg;
                        rgbImg.Format = MagickFormat.Png24;
                        rgbImg.Quality = 10;
                        MagickImage bg = new MagickImage(MagickColors.Black, rgbImg.Width, rgbImg.Height);
                        bg.Composite(rgbImg, CompositeOperator.Over);
                        rgbImg = bg;
                        rgbImg.Write(file);
                    }

                    alphaImg.Format = MagickFormat.Png24;
                    alphaImg.Quality = 10;
                    
                    alphaImg.FloodFill(MagickColors.None, 0, 0);     // Fill the image with a transparent background
                    alphaImg.InverseOpaque(MagickColors.None, MagickColors.White);   // Change all the pixels that are not transparent to white.
                    alphaImg.ColorAlpha(MagickColors.Black);     // Change the transparent pixels to black.

                    string outPath = Path.Combine(outputDir, Path.GetFileName(file));
                    alphaImg.Write(outPath);
                    counter++;
                    if (sw.ElapsedMilliseconds > 250)
                    {
                        if (setProgress)
                            Program.mainForm.SetProgress((int)Math.Round(((float)counter / files.Length) * 100f));
                        await Task.Delay(1);
                        sw.Restart();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("ExtractAlpha Error: " + e.Message);
            }
        }

        public static async Task Preprocess (string dir, bool setProgress = true)
        {
            var files = IoUtils.GetFilesSorted(dir);
            Logger.Log($"Preprocessing {files} files in {dir}");
            int counter = 0;
            foreach (string file in files)
            {
                //Logger.Log("Converting " + Path.GetFileName(file) + " to " + format, false, true);
                MagickImage img = new MagickImage(file);
                //img.Format = MagickFormat.Bmp;
                //img.Write(file);
                //img = new MagickImage(file);
                img.Format = MagickFormat.Png24;
                img.Quality = 10;
                counter++;
                if (setProgress)
                    Program.mainForm.SetProgress((int)Math.Round(((float)counter / files.Length) * 100f));
                await Task.Delay(1);
            }
        }
    }
}
