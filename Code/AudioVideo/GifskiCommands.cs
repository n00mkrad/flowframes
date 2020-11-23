using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.FFmpeg
{
    class GifskiCommands
    {
        public static async Task CreateGifFromFrames (int fps, int quality, string framespath, string outpath)
        {
            Logger.Log($"Creating GIF from frames using quality {quality}...");
            await AvProcess.RunGifski($" -r {fps} -W 4096 -Q {quality} -o \"{outpath}\" \"{framespath}/\"*.\"png\"", AvProcess.LogMode.OnlyLastLine);
        }
    }
}
