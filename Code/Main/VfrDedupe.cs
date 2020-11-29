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

        public static async Task CreateTimecodeFiles(string framesPath, bool loopEnabled, bool firstFrameFix)
        {
            Logger.Log("Generating timecodes...");
            await CreateTimecodeFile(framesPath, loopEnabled, 2, firstFrameFix);
            await CreateTimecodeFile(framesPath, loopEnabled, 4, firstFrameFix);
            await CreateTimecodeFile(framesPath, loopEnabled, 8, firstFrameFix);
            frameFiles = null;
            Logger.Log($"Generating timecodes... Done.", false, true);
        }

        static FileInfo[] frameFiles;

        public static async Task CreateTimecodeFile(string framesPath, bool loopEnabled, int interpFactor, bool firstFrameFix)
        {
            bool sceneDetection = true;

            if(frameFiles == null || frameFiles.Length < 1)
                frameFiles = new DirectoryInfo(framesPath).GetFiles("*.png");
            string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-x{interpFactor}.ini");
            string fileContent = "";

            string scnFramesPath = Path.Combine(framesPath.GetParentDir(), "scenes");
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

                int interpFramesAmount = interpFactor;


                bool discardThisFrame = (sceneDetection && (i + 2) < frameFiles.Length && File.Exists(Path.Combine(scnFramesPath, frameFiles[i + 1].Name)));     // i+2 is in scene detection folder, means i+1 is ugly interp frame
                //if (discardThisFrame) Logger.Log("Will discard " + totalFileCount);

                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpFramesAmount = interpFramesAmount * 2;

                // Generate frames file lines
                for (int frm = 0; frm < interpFramesAmount; frm++)
                {
                    string durationStr = ((durationPerInterpFrame / 1000f) * 1).ToString("0.00000").Replace(",", ".");

                    if (discardThisFrame)
                    {
                        int lastNum = totalFileCount - 1;
                        for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                        {
                            //Logger.Log($"-> Writing #{frm + 1}: {lastNum}");
                            fileContent += $"file '{interpPath}/{lastNum.ToString().PadLeft(8, '0')}.png'\nduration {durationStr}\n";
                            totalFileCount++;
                        }
                        frm = interpFramesAmount - 1;
                    }

                    //Logger.Log($"-> Writing #{frm+1}: {totalFileCount}");
                    fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(8, '0')}.png'\nduration {durationStr}\n";
                    totalFileCount++;   // Don't increment, so we dupe the last frame and ignore the interp frame
                }

                if ((i + 1) % 100 == 0)
                {
                    Logger.Log($"Generating timecodes for {interpFactor}x...", false, true);
                    await Task.Delay(1);
                }
            }

            File.WriteAllText(vfrFile, fileContent);

            if (interpFactor > 2)       // Skip all steps that only need to be done once
                return;

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
                string loopFrameTargetPath = Path.Combine(frameFiles.First().FullName.GetParentDir(), lastFileNumber + ".png");
                if (File.Exists(loopFrameTargetPath))
                    return;
                File.Copy(frameFiles.First().FullName, loopFrameTargetPath);
                //Logger.Log("Copied loop frame to " + loopFrameTargetPath);
            }
        }
    }
}
