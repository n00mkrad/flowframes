using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class VfrDedupe
    {
        public static void CreateTimecodeFile(string framesPath, bool loopEnabled, int interpFactor)
        {
            FileInfo[] frameFiles = new DirectoryInfo(framesPath).GetFiles("*.png");
            string vfrFile = Path.Combine(framesPath.GetParentDir(), "vfr.ini");

            int lastFrameDuration = 1;

            // Calculate time duration between frames
            int totalFileCount = 1;
            for (int i = 0; i < (frameFiles.Length - 1); i++)
            {
                string filename1 = frameFiles[i].Name;
                string filename2 = frameFiles[i + 1].Name;
                //Logger.Log("durationTotal = " + Path.GetFileNameWithoutExtension(filename2).GetInt() + " - " + Path.GetFileNameWithoutExtension(filename1).GetInt());
                int durationTotal = Path.GetFileNameWithoutExtension(filename2).GetInt() - Path.GetFileNameWithoutExtension(filename1).GetInt();
                lastFrameDuration = durationTotal;
                float durationPerInterpFrame = (float)durationTotal / interpFactor;
                //Logger.Log("durationPerInterpFrame = " + durationPerInterpFrame);
                int interpolatedFrameCount = interpFactor;

                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpolatedFrameCount = interpolatedFrameCount * 2;

                // Generate frames file lines
                for (int frm = 0; frm < interpolatedFrameCount; frm++)
                {
                    string durationStr = (durationPerInterpFrame / 1000f).ToString("0.00000").Replace(",", ".");
                    File.AppendAllText(vfrFile, $"{totalFileCount.ToString().PadLeft(8, '0')}.png\nduration {durationStr}\n");
                    totalFileCount++;
                }
            }
        }
    }
}
