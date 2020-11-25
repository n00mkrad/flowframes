using Flowframes.IO;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class VfrDedupe
    {
        public static async Task CreateTimecodeFile(string framesPath, bool loopEnabled, int interpFactor, bool firstFrameFix)
        {
            Logger.Log("Generating timecodes...");

            FileInfo[] frameFiles = new DirectoryInfo(framesPath).GetFiles("*.png");
            string vfrFile = Path.Combine(framesPath.GetParentDir(), "vfr.ini");
            string fileContent = "";

            string interpPath = framesPath.Replace(@"\", "/") + "-interpolated";

            int lastFrameDuration = 1;

            // Calculate time duration between frames
            int totalFileCount = 1;
            for (int i = 0; i < (frameFiles.Length - 1); i++)
            {
                if (Interpolate.canceled) return;

                string filename1 = frameFiles[i].Name;
                string filename2 = frameFiles[i + 1].Name;

                int durationTotal = Path.GetFileNameWithoutExtension(filename2).GetInt() - Path.GetFileNameWithoutExtension(filename1).GetInt();
                lastFrameDuration = durationTotal;
                float durationPerInterpFrame = (float)durationTotal / interpFactor;

                int interpolatedFrameCount = interpFactor;

                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpolatedFrameCount = interpolatedFrameCount * 2;

                // Generate frames file lines
                for (int frm = 0; frm < interpolatedFrameCount; frm++)
                {
                    string durationStr = ((durationPerInterpFrame / 1000f) * 1).ToString("0.00000").Replace(",", ".");
                    fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(8, '0')}.png'\nduration {durationStr}\n";
                    totalFileCount++;
                }

                if((i+1) % 50 == 0)
                {
                    Logger.Log($"Generating timecodes... {i + 1}/{frameFiles.Length}", false, true);
                    await Task.Delay(1);
                }
            }

            File.WriteAllText(vfrFile, fileContent);
            Logger.Log($"Generating timecodes... Done.", false, true);

            if (firstFrameFix)
            {
                string[] lines = IOUtils.ReadLines(vfrFile);
                File.WriteAllText(vfrFile, lines[0].Replace("00000001.png", "00000000.png"));
                File.AppendAllText(vfrFile, "\n" + lines[1] + "\n");
                File.AppendAllLines(vfrFile, lines);
            }

            if (Config.GetBool("enableLoop"))
            {
                int lastFileNumber = frameFiles.Last().Name.GetInt();
                lastFileNumber += lastFrameDuration;
                File.Copy(frameFiles.First().FullName, Path.Combine(frameFiles.First().FullName.GetParentDir(), lastFileNumber + ".png"));
            }
        }
    }
}
