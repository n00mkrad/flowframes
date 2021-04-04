using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flowframes.Data;
using Flowframes.IO;

namespace Flowframes.MiscUtils
{
    class FrameRename
    {
        public static bool framesAreRenamed;
        public static string[] importFilenames;   // index=renamed, value=original TODO: Store on disk instead for crashes?

        public static async Task Rename()
        {
            importFilenames = IOUtils.GetFilesSorted(Interpolate.current.framesFolder).Select(x => Path.GetFileName(x)).ToArray();
            await IOUtils.RenameCounterDir(Interpolate.current.framesFolder, 0, Padding.inputFramesRenamed);
            framesAreRenamed = true;
        }

        public static async Task Unrename()
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();

            string[] files = IOUtils.GetFilesSorted(Interpolate.current.framesFolder);

            for (int i = 0; i < files.Length; i++)
            {
                string movePath = Path.Combine(Interpolate.current.framesFolder, importFilenames[i]);
                File.Move(files[i], movePath);

                if (sw.ElapsedMilliseconds > 100)
                {
                    await Task.Delay(1);
                    sw.Restart();
                }
            }

            framesAreRenamed = false;
        }
    }
}
