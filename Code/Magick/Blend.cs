using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
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
    class Blend
    {
        public static async Task BlendSceneChanges(string framesFilePath)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            int totalFrames = 0;

            string keyword = "SCN:";
            string[] framesLines = IOUtils.ReadLines(framesFilePath);     // Array with frame filenames
            int amountOfBlendFrames = (int)Interpolate.current.interpFactor - 1;
            //Logger.Log($"BlendSceneChanges: Blending with {amountOfBlendFrames} frames", true);

            foreach (string line in framesLines)
            {
                try
                {
                    if (line.Contains(keyword))
                    {
                        string trimmedLine = line.Split(keyword).Last();
                        string[] inputFrameNames = trimmedLine.Split('>');
                        string img1 = Path.Combine(Interpolate.current.framesFolder, inputFrameNames[0]);
                        string img2 = Path.Combine(Interpolate.current.framesFolder, inputFrameNames[1]);

                        string firstOutputFrameName = line.Split('/').Last().Remove("'").Split('#').First();
                        string ext = Path.GetExtension(firstOutputFrameName);
                        int firstOutputFrameNum = firstOutputFrameName.GetInt();
                        List<string> outputFilenames = new List<string>();
                        //Logger.Log("BlendSceneChanges: 1 = " + img1, true);
                        //Logger.Log("BlendSceneChanges: 2 = " + img2, true);

                        for (int blendFrameNum = 1; blendFrameNum <= amountOfBlendFrames; blendFrameNum++)
                        {
                            int outputNum = firstOutputFrameNum + blendFrameNum + 1;
                            string outputPath = Path.Combine(Interpolate.current.interpFolder, outputNum.ToString().PadLeft(Padding.interpFrames, '0'));
                            outputPath = Path.ChangeExtension(outputPath, ext);
                            outputFilenames.Add(outputPath);
                            //Logger.Log("BlendSceneChanges: Added output path " + outputPath, true);
                        }

                        BlendImages(img1, img2, outputFilenames.ToArray());
                        totalFrames += outputFilenames.Count;

                        await Task.Delay(1);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Failed to blend scene changes: " + e.Message, true);
                }
            }

            Logger.Log($"Created {totalFrames} blend frames in {FormatUtils.TimeSw(sw)} ({(totalFrames / (sw.ElapsedMilliseconds / 1000f)).ToString("0.00")} FPS)", true);
        }

        public static void BlendImages(string img1Path, string img2Path, string imgOutPath)
        {
            MagickImage img1 = new MagickImage(img1Path);
            MagickImage img2 = new MagickImage(img2Path);
            img2.Alpha(AlphaOption.Opaque);
            img2.Evaluate(Channels.Alpha, EvaluateOperator.Set, new Percentage(50));
            img1.Composite(img2, Gravity.Center, CompositeOperator.Over);
            img1.Format = MagickFormat.Png24;
            img1.Quality = 10;
            img1.Write(imgOutPath);
        }

        public static void BlendImages (string img1Path, string img2Path, string[] imgOutPaths)
        {
            MagickImage img1 = new MagickImage(img1Path);
            MagickImage img2 = new MagickImage(img2Path);

            int alphaFraction = (100f / (imgOutPaths.Length + 1)).RoundToInt();   // Alpha percentage per image
            int currentAlpha = alphaFraction;

            foreach (string imgOutPath in imgOutPaths)
            {
                MagickImage img1Inst = new MagickImage(img1);
                MagickImage img2Inst = new MagickImage(img2);

                img2Inst.Alpha(AlphaOption.Opaque);
                img2Inst.Evaluate(Channels.Alpha, EvaluateOperator.Set, new Percentage(currentAlpha));
                currentAlpha += alphaFraction;

                img1Inst.Composite(img2Inst, Gravity.Center, CompositeOperator.Over);
                img1Inst.Format = MagickFormat.Png24;
                img1Inst.Quality = 10;
                img1Inst.Write(imgOutPath);
            }
        }
    }
}
