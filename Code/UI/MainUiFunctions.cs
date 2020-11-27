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
            outputTbox.Text = inputTbox.Text.Trim().GetParentDir();
            string path = inputTbox.Text.Trim();
            Program.lastInputPath = path;
            string fpsStr = "Not Found";
            float fps = IOUtils.GetFpsFolderOrVideo(path);
            if (fps > 0)
            {
                fpsStr = fps.ToString();
                fpsInTbox.Text = fpsStr;
            }
            Interpolate.SetFps(fps);
            Program.lastInputPathIsSsd = OSUtils.DriveIsSSD(path);
            if (!Program.lastInputPathIsSsd)
                Logger.Log("Your file seems to be on an HDD or USB device. It is recommended to interpolate videos on an SSD drive for best performance.");
            if (IOUtils.IsPathDirectory(path))
                Logger.Log($"Video FPS (Loaded from fps.ini): {fpsStr} - Total Number Of Frames: {IOUtils.GetAmountOfFiles(path, false)}");
            else
                Logger.Log($"Video FPS: {fpsStr} - Total Number Of Frames: {FFmpegCommands.GetFrameCount(path)}");
            await Task.Delay(10);
            Size res = FFmpegCommands.GetSize(path);
            Logger.Log($"Video Resolution: {res.Width}x{res.Height}");
            MagickDedupe.ClearCache();
            await Task.Delay(10);
            InterpolateUtils.SetPreviewImg(await GetThumbnail(path));
        }

        static async Task<Image> GetThumbnail (string videoPath)
        {
            string imgOnDisk = Path.Combine(Paths.GetDataPath(), "thumb-temp.png");
            try
            {
                await FFmpegCommands.ExtractSingleFrame(videoPath, imgOnDisk, 1, false, false);
                return IOUtils.GetImage(imgOnDisk);
            }
            catch (Exception e)
            {
                Logger.Log("GetThumbnail Error: " + e.Message, true);
                return null;
            }
        }
    }
}
