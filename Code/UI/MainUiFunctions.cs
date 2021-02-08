using Flowframes.Media;
using Flowframes.IO;
using Flowframes.Magick;
using Flowframes.Main;
using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.UI
{
    class MainUiFunctions
    {
        public static async Task InitInput (TextBox outputTbox, TextBox inputTbox, TextBox fpsInTbox)
        {
            Program.mainForm.SetTab("interpolate");
            Program.mainForm.ResetInputInfo();
            string path = inputTbox.Text.Trim();
            InterpolateUtils.PathAsciiCheck(path, "input path");

            if (Config.GetBool("clearLogOnInput"))
                Logger.ClearLogBox();

            outputTbox.Text = inputTbox.Text.Trim().GetParentDir();
            Program.lastInputPath = path;
            Program.lastInputPathIsSsd = OSUtils.DriveIsSSD(path);

            if (!Program.lastInputPathIsSsd)
                Logger.Log("Your file seems to be on an HDD or USB device. It is recommended to interpolate videos on an SSD drive for best performance.");

            Logger.Log("Loading metadata...");
            Program.mainForm.currInDuration = FfmpegCommands.GetDuration(path);
            int frameCount = await InterpolateUtils.GetInputFrameCountAsync(path);
            string fpsStr = "Not Found";
            float fps = await IOUtils.GetFpsFolderOrVideo(path);
            fpsInTbox.Text = fps.ToString();

            if (fps > 0)
                fpsStr = fps.ToString();

            Logger.Log($"Video FPS: {fpsStr} - Total Number Of Frames: {frameCount}", false, true);

            Program.mainForm.currInFps = fps;
            Program.mainForm.currInFrames = frameCount;
            Program.mainForm.UpdateInputInfo();
            CheckExistingFolder(path, outputTbox.Text.Trim());
            await Task.Delay(10);
            await PrintResolution(path);
            Dedupe.ClearCache();
            await Task.Delay(10);
            InterpolateUtils.SetPreviewImg(await GetThumbnail(path));
        }

        static void CheckExistingFolder (string inpath, string outpath)
        {
            if (!Interpolate.current.stepByStep) return;
            string tmpFolder = InterpolateUtils.GetTempFolderLoc(inpath, outpath);
            if (Directory.Exists(tmpFolder))
            {
                int scnFrmAmount = IOUtils.GetAmountOfFiles(Path.Combine(tmpFolder, Paths.scenesDir), false, "*.png");
                string scnFrames = scnFrmAmount > 0 ? $"{scnFrmAmount} scene frames" : "no scene frames";
                int srcFrmAmount = IOUtils.GetAmountOfFiles(Path.Combine(tmpFolder, Paths.framesDir), false, "*.png");
                string srcFrames = srcFrmAmount > 1 ? $"{srcFrmAmount} source frames" : "no source frames";
                int interpFrmAmount = IOUtils.GetAmountOfFiles(Path.Combine(tmpFolder, Paths.interpDir), false);
                string interpFrames = interpFrmAmount > 2 ? $"{interpFrmAmount} interpolated frames" : "no interpolated frames";
                string msg = $"A temporary folder for this video already exists. It contains {scnFrames}, {srcFrames}, {interpFrames}.";

                DialogResult dialogResult = MessageBox.Show($"{msg}\n\nClick \"Yes\" to use the existing files or \"No\" to delete them.", "Use files from existing temp folder?", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No)
                {
                    IOUtils.TryDeleteIfExists(tmpFolder);
                    Logger.Log("Deleted old temp folder.");
                }
            }
        }

        static async Task PrintResolution (string path)
        {
            Size res = new Size();

            if(path == Interpolate.current.inPath)
                res = await Interpolate.current.GetInputRes();
            else
                res = await IOUtils.GetVideoOrFramesRes(path);

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
                if (!IOUtils.IsPathDirectory(path))     // If path is video - Extract first frame
                {
                    await FfmpegExtract.ExtractSingleFrame(path, imgOnDisk, 1);
                    return IOUtils.GetImage(imgOnDisk);
                }
                else     // Path is frame folder - Get first frame
                {
                    return IOUtils.GetImage(IOUtils.GetFilesSorted(path)[0]);
                }
            }
            catch (Exception e)
            {
                Logger.Log("GetThumbnail Error: " + e.Message, true);
                return null;
            }
        }
    }
}
