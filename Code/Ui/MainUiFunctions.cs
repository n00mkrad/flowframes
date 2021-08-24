using Flowframes.Media;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
using Flowframes.Os;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.Data;

namespace Flowframes.Ui
{
    class MainUiFunctions
    {
        public static async Task InitInput (TextBox outputTbox, TextBox inputTbox, TextBox fpsInTbox, bool start = false)
        {
            Program.mainForm.SetTab("interpolate");
            Program.mainForm.ResetInputInfo();
            string path = inputTbox.Text.Trim();

            if (Config.GetBool(Config.Key.clearLogOnInput))
                Logger.ClearLogBox();

            outputTbox.Text = (Config.GetInt("outFolderLoc") == 0) ? inputTbox.Text.Trim().GetParentDir() : Config.Get("custOutDir").Trim();

            Program.lastInputPath = path;
            Program.lastInputPathIsSsd = OsUtils.DriveIsSSD(path);

            if (!Program.lastInputPathIsSsd)
                Logger.Log("Your file seems to be on an HDD or USB device. It is recommended to interpolate videos on an SSD drive for best performance.");

            Logger.Log("Loading metadata...");
            Program.mainForm.currInDuration = FfmpegCommands.GetDuration(path);
            Program.mainForm.currInDurationCut = Program.mainForm.currInDuration;
            int frameCount = await GetFrameCountCached.GetFrameCountAsync(path);
            string fpsStr = "Not Found";
            Fraction fps = (await IoUtils.GetFpsFolderOrVideo(path));
            Program.mainForm.currInFpsDetected = fps;
            fpsInTbox.Text = fps.GetString();

            if (fps.GetFloat() > 0)
                fpsStr = $"{fps} (~{fps.GetFloat()})";

            Logger.Log($"Video FPS: {fpsStr} - Total Number Of Frames: {frameCount}", false, true);
            Program.mainForm.GetInputFpsTextbox().ReadOnly = (fps.GetFloat() > 0 && !Config.GetBool("allowCustomInputRate", false));
            Program.mainForm.currInFps = fps;
            Program.mainForm.currInFrames = frameCount;
            Program.mainForm.UpdateInputInfo();
            CheckExistingFolder(path, outputTbox.Text.Trim());
            await Task.Delay(10);
            await PrintResolution(path);
            Dedupe.ClearCache();
            await Task.Delay(10);
            InterpolationProgress.SetPreviewImg(await GetThumbnail(path));

            if (start)
                Program.mainForm.runBtn_Click(null, null);
                
        }

        static void CheckExistingFolder (string inpath, string outpath)
        {
            if (Interpolate.current == null || !Interpolate.current.stepByStep) return;
            string tmpFolder = InterpolateUtils.GetTempFolderLoc(inpath, outpath);
            if (Directory.Exists(tmpFolder))
            {
                int scnFrmAmount = IoUtils.GetAmountOfFiles(Path.Combine(tmpFolder, Paths.scenesDir), false, "*" + Interpolate.current.interpExt);  // TODO: Make this work if the frames extension was changed
                string scnFrames = scnFrmAmount > 0 ? $"{scnFrmAmount} scene frames" : "no scene frames";
                int srcFrmAmount = IoUtils.GetAmountOfFiles(Path.Combine(tmpFolder, Paths.framesDir), false, "*" + Interpolate.current.interpExt);
                string srcFrames = srcFrmAmount > 1 ? $"{srcFrmAmount} source frames" : "no source frames";
                int interpFrmAmount = IoUtils.GetAmountOfFiles(Path.Combine(tmpFolder, Paths.interpDir), false);
                string interpFrames = interpFrmAmount > 2 ? $"{interpFrmAmount} interpolated frames" : "no interpolated frames";
                string msg = $"A temporary folder for this video already exists. It contains {scnFrames}, {srcFrames}, {interpFrames}.";

                DialogResult dialogResult = MessageBox.Show($"{msg}\n\nClick \"Yes\" to use the existing files or \"No\" to delete them.", "Use files from existing temp folder?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    IoUtils.TryDeleteIfExists(tmpFolder);
                    Logger.Log("Deleted old temp folder.");
                }
            }
        }

        static async Task PrintResolution (string path)
        {
            Size res = new Size();

            if(path == Interpolate.current?.inPath)
                res = await Interpolate.current.GetInputRes();
            else
                res = await GetMediaResolutionCached.GetSizeAsync(path);

            if (res.Width > 1 && res.Height > 1)
                Logger.Log($"Input Resolution: {res.Width}x{res.Height}");

            Program.mainForm.currInRes = res;
            Program.mainForm.UpdateInputInfo();
        }

        public static async Task<Image> GetThumbnail (string path)
        {
            string imgOnDisk = Path.Combine(Paths.GetDataPath(), "thumb-temp.jpg");
            try
            {
                if (!IoUtils.IsPathDirectory(path))     // If path is video - Extract first frame
                {
                    await FfmpegExtract.ExtractSingleFrame(path, imgOnDisk, 1);
                    return IoUtils.GetImage(imgOnDisk);
                }
                else     // Path is frame folder - Get first frame
                {
                    return IoUtils.GetImage(IoUtils.GetFilesSorted(path)[0]);
                }
            }
            catch (Exception e)
            {
                Logger.Log("GetThumbnail Error: " + e.Message, true);
                return null;
            }
        }

        public static int ValidateInterpFactor (int factor)
        {
            AI ai = Program.mainForm.GetAi();

            if (factor > 256)
                return 256;

            if (ai.aiName == Implementations.rifeCuda.aiName)
            {
                if (!IsPowerOfTwo(factor))
                {
                    return Implementations.rifeCuda.supportedFactors.Last();
                }
            }

            return factor;
        }

        static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }
}
