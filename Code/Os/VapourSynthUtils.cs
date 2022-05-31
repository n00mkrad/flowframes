using Flowframes.Data;
using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Os
{
    class VapourSynthUtils
    {
        public static string CreateScript(InterpSettings set, string modelDir, float factor, Size res, bool uhd, float sceneDetectSens = 0.15f, int gpuId = 0, int gpuThreads = 3, bool tta = false)
        {
            string inputPath = set.inPath;
            bool resize = !set.ScaledResolution.IsEmpty && set.ScaledResolution != set.InputResolution;
            bool sc = sceneDetectSens >= 0.01f;

            string text = ""
                + $"import sys\n"
                + $"import vapoursynth as vs\n"
                + $"core = vs.core\n"
                + $"clip = core.ffms2.Source(source=r'{inputPath}')\n"
                + $"clip = core.resize.Bicubic(clip=clip, format=vs.RGBS, matrix_in_s=\"709\", range_s=\"limited\"{(resize ? $", width={set.ScaledResolution.Width}, height={set.ScaledResolution.Height}" : "")})\n"
                + $"{(sc ? $"clip = core.misc.SCDetect(clip=clip,threshold={sceneDetectSens.ToStringDot()})" : "# Scene detection disabled")}\n"
                + $"clip = core.rife.RIFE(clip, {GetModelNum(modelDir)}, {factor.ToStringDot()}, {gpuId}, {gpuThreads}, {tta}, {uhd}, {sc})\n"
                + $"clip = vs.core.resize.Bicubic(clip, format=vs.YUV444P16, matrix_s=\"709\")\n"
                + $"clip.set_output()\n";

            string pkgPath = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir);
            string vpyPath = Path.Combine(pkgPath, "rife.vpy");

            File.WriteAllText(vpyPath, text);

            return vpyPath;
        }

        private static int GetModelNum(string modelDir)
        {
            switch (modelDir)
            {
                case "rife": return 0;
                case "rife-HD": return 1;
                case "rife-UHD": return 2;
                case "rife-anime": return 3;
                case "rife-v2": return 4;
                case "rife-v2.3": return 5;
                case "rife-v2.4": return 6;
                case "rife-v3.0": return 7;
                case "rife-v3.1": return 8;
                case "rife-v4": return 9;
            }

            return 9;
        }
    }
}
