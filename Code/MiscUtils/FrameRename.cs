﻿using Flowframes.Data;
using Flowframes.IO;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flowframes.MiscUtils
{
    class FrameRename
    {
        public static bool framesAreRenamed;
        public static string[] importFilenames;   // index=renamed, value=original TODO: Store on disk instead for crashes?

        public static async Task Rename()
        {
            importFilenames = IoUtils.GetFilesSorted(Interpolate.currentSettings.framesFolder).Select(x => Path.GetFileName(x)).ToArray();
            await IoUtils.RenameCounterDir(Interpolate.currentSettings.framesFolder, 0, Padding.inputFramesRenamed);
            framesAreRenamed = true;
        }

        public static async Task Unrename()
        {
            if (!framesAreRenamed) return;

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            string[] files = IoUtils.GetFilesSorted(Interpolate.currentSettings.framesFolder);

            for (int i = 0; i < files.Length; i++)
            {
                string movePath = Path.Combine(Interpolate.currentSettings.framesFolder, importFilenames[i]);
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
