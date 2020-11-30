using Flowframes.IO;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class VfrDedupe
    {
        public static async Task CreateTimecodeFiles(string framesPath, bool loopEnabled, bool firstFrameFix, int times)
        {
            Logger.Log("Generating timecodes...");
            if(times <= 0)
            {
                await CreateTimecodeFile(framesPath, loopEnabled, 2, firstFrameFix);
                await CreateTimecodeFile(framesPath, loopEnabled, 4, firstFrameFix);
                await CreateTimecodeFile(framesPath, loopEnabled, 8, firstFrameFix);
            }
            else
            {
                await CreateTimecodeFile(framesPath, loopEnabled, times, firstFrameFix);
            }
            frameFiles = null;
            Logger.Log($"Generating timecodes... Done.", false, true);
        }

        static FileInfo[] frameFiles;

        public static async Task CreateTimecodeFile(string framesPath, bool loopEnabled, int interpFactor, bool firstFrameFix)
        {
            if (Interpolate.canceled) return;
            Logger.Log($"Generating timecodes for {interpFactor}x...", false, true);

            bool sceneDetection = true;

            if(frameFiles == null || frameFiles.Length < 1)
                frameFiles = new DirectoryInfo(framesPath).GetFiles("*.png");
            string vfrFile = Path.Combine(framesPath.GetParentDir(), $"vfr-x{interpFactor}.ini");
            string fileContent = "";

            string scnFramesPath = Path.Combine(framesPath.GetParentDir(), Paths.scenesDir);
            string interpPath = Paths.interpDir; // framesPath.Replace(@"\", "/") + "-interpolated";

            List<string> sceneFrames = Directory.GetFiles(scnFramesPath).Select(file => Path.GetFileName(file)).ToList();

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

                bool discardThisFrame = (sceneDetection && (i + 2) < frameFiles.Length && sceneFrames.Contains(frameFiles[i + 1].Name));     // i+2 is in scene detection folder, means i+1 is ugly interp frame


                // If loop is enabled, account for the extra frame added to the end for loop continuity
                if (loopEnabled && i == (frameFiles.Length - 2))
                    interpFramesAmount = interpFramesAmount * 2;

                // Generate frames file lines
                for (int frm = 0; frm < interpFramesAmount; frm++)
                {
                    string durationStr = ((durationPerInterpFrame / 1000f) * 1).ToString("0.00000", CultureInfo.InvariantCulture);

                    if (discardThisFrame)
                    {
                        int lastNum = totalFileCount - 1;
                        for (int dupeCount = 1; dupeCount < interpFramesAmount; dupeCount++)
                        {
                            fileContent += $"file '{interpPath}/{lastNum.ToString().PadLeft(8, '0')}.png'\nduration {durationStr}\n";
                            totalFileCount++;
                        }
                        frm = interpFramesAmount - 1;
                    }

                    fileContent += $"file '{interpPath}/{totalFileCount.ToString().PadLeft(8, '0')}.png'\nduration {durationStr}\n";
                    totalFileCount++;
                }

                if ((i + 1) % 100 == 0)
                    await Task.Delay(1);
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
