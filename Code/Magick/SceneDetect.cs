using Flowframes.IO;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Magick
{
    class SceneDetect
    {
        public static async Task RunSceneDetection (string path)
        {
            string outFolder = path + "-analyzed";
            Directory.CreateDirectory(outFolder);
            string ext = "png";
            FileInfo[] frames = IOUtils.GetFileInfosSorted(path, false, "*." + ext);

            for (int i = 1; i < frames.Length; i++)
            {
                FileInfo frame = frames[i];
                FileInfo lastFrame = frames[i - 1];
                Task.Run(() => ProcessFrame(frame, lastFrame, outFolder));
            }
        }

        public static Dictionary<string, MagickImage> imageCache = new Dictionary<string, MagickImage>();
        static MagickImage GetImage(string path, bool allowCaching = true)
        {
            if (!allowCaching)
                return new MagickImage(path);

            if (imageCache.Count >= 30)
                ClearCache();

            if (!imageCache.ContainsKey(path))
                imageCache.Add(path, new MagickImage(path));

            return imageCache[path];
        }

        public static void ClearCache()
        {
            imageCache.Clear();
        }

        static async Task ProcessFrame (FileInfo frame, FileInfo lastFrame, string outFolder)
        {
            MagickImage prevFrame = GetImage(lastFrame.FullName, false);
            MagickImage currFrame = GetImage(frame.FullName, false);

            Size originalSize = new Size(currFrame.Width, currFrame.Height);
            int downscaleHeight = 144;
            prevFrame.Scale(currFrame.Width / (currFrame.Height / downscaleHeight), downscaleHeight);
            currFrame.Scale(currFrame.Width / (currFrame.Height / downscaleHeight), downscaleHeight);

            double errNormalizedCrossCorrelation = currFrame.Compare(prevFrame, ErrorMetric.NormalizedCrossCorrelation);
            double errRootMeanSquared = currFrame.Compare(prevFrame, ErrorMetric.RootMeanSquared);

            string str = $"\nMetrics of {frame.Name.Split('.')[0]} against {lastFrame.Name.Split('.')[0]}:\n";
            str += $"NormalizedCrossCorrelation: {errNormalizedCrossCorrelation.ToString("0.000")}\n";
            str += $"RootMeanSquared: {errRootMeanSquared.ToString("0.000")}\n";
            str += "\n\n";

            bool nccTrigger = errNormalizedCrossCorrelation < 0.45f;
            bool rMeanSqrTrigger = errRootMeanSquared > 0.18f;
            bool rmsNccTrigger = errRootMeanSquared > 0.18f && errNormalizedCrossCorrelation < 0.6f;
            bool nccRmsTrigger = errNormalizedCrossCorrelation < 0.45f && errRootMeanSquared > 0.11f;

            if (rmsNccTrigger) str += "\n\nRMS -> NCC DOUBLE SCENE CHANGE TRIGGER!";
            if (nccRmsTrigger) str += "\n\nNCC -> RMS DOUBLE SCENE CHANGE TRIGGER!";

            currFrame.Scale(originalSize.Width / 2, originalSize.Height / 2);

            new Drawables()
            .FontPointSize(12)
            .Font("Consolas", FontStyleType.Normal, FontWeight.Bold, FontStretch.Normal)
            .FillColor(MagickColors.Red)
            .TextAlignment(TextAlignment.Left)
            .Text(1, 10, str)
            .Draw(currFrame);

            currFrame.Write(Path.Combine(outFolder, frame.Name));

            prevFrame.Dispose();
            currFrame.Dispose();

            await Task.Delay(1);
        }
    }
}
