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
            await PrintResolution(path);
            MagickDedupe.ClearCache();
            await Task.Delay(10);
            InterpolateUtils.SetPreviewImg(await GetThumbnail(path));
        }

        static async Task PrintResolution (string path)
        {
            Size res = new Size();
            if (!IOUtils.IsPathDirectory(path))     // If path is video
            {
                res = FFmpegCommands.GetSize(path);
            }
            else     // Path is frame folder
            {
                Image thumb = await GetThumbnail(path);
                res = new Size(thumb.Width, thumb.Height);
            }
            if (res.Width > 1 && res.Height > 1)
                Logger.Log($"Input Resolution: {res.Width}x{res.Height}");
        }

        static async Task<Image> GetThumbnail (string path)
        {
            string imgOnDisk = Path.Combine(Paths.GetDataPath(), "thumb-temp.png");
            try
            {
                if (!IOUtils.IsPathDirectory(path))     // If path is video - Extract first frame
                {
                    await FFmpegCommands.ExtractSingleFrame(path, imgOnDisk, 1, false, false);
                    return IOUtils.GetImage(imgOnDisk);
                }
                else     // Path is frame folder - Get first frame
                {
                    return IOUtils.GetImage(Directory.GetFiles(path)[0]);
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
