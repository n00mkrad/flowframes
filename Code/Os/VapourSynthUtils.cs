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
        public class VsSettings
        {
            public InterpSettings InterpSettings { get; set; }
            public string ModelDir { get; set; } = "";
            public float Factor { get; set; } = 2.0f;
            public Size Res { get; set; } = new Size();
            public bool Uhd { get; set; } = false;
            public float SceneDetectSensitivity { get; set; } = 0.15f;
            public int GpuId { get; set; } = 0;
            public int GpuThreads { get; set; } = 3;
            public bool Tta { get; set; } = false;
            public bool Loop { get; set; } = false;
            public bool MatchDuration { get; set; } = false;
        }

        public static string CreateScript(VsSettings s)
        {
            string inputPath = s.InterpSettings.inPath;
            bool sc = s.SceneDetectSensitivity >= 0.01f;

            int endDupeCount = s.Factor.RoundToInt() - 1;

            List<string> l = new List<string> { "import sys", "import vapoursynth as vs", "core = vs.core" }; // Imports

            if (s.InterpSettings.inputIsFrames)
            {
                string first = Path.GetFileNameWithoutExtension(IoUtils.GetFileInfosSorted(s.InterpSettings.framesFolder, false).FirstOrDefault().FullName);
                l.Add($"clip = core.imwri.Read(r'{Path.Combine(s.InterpSettings.framesFolder, $"%0{first.Length}d.png")}', firstnum={first.GetInt()})"); // Load image sequence with imwri
                l.Add($"clip = core.std.AssumeFPS(clip, fpsnum={s.InterpSettings.inFps.Numerator}, fpsden={s.InterpSettings.inFps.Denominator})"); // Set frame rate for img seq
            }
            else
            {
                l.Add($"clip = core.lsmas.LWLibavSource(r'{inputPath}', cachefile=r'{Path.Combine(s.InterpSettings.tempFolder, "lsmash.cache.lwi")}')"); // Load video with lsmash
            }
                
            l.Add($"targetFrameCountMatchDuration = round((clip.num_frames*{s.Factor.ToStringDot()}), 1)"); // Target frame count to match original duration (and for loops)
            l.Add($"targetFrameCountTrue = targetFrameCountMatchDuration-{endDupeCount}"); // Target frame count without dupes at the end (only in-between frames added)

            if (s.Loop)
            {
                l.Add($"firstFrame = clip[0]"); // Grab first frame
                l.Add($"clip = clip + firstFrame"); // Add to end (for seamless loop interpolation)
            }

            l.AddRange(GetScaleLines(s));

            if (sc)
                l.Add($"clip = core.misc.SCDetect(clip=clip,threshold={s.SceneDetectSensitivity.ToStringDot()})"); // Scene detection

            l.Add($"clip = core.rife.RIFE(clip, {9}, {s.Factor.ToStringDot()}, {s.GpuId}, {s.GpuThreads}, {s.Tta}, {s.Uhd}, {sc})"); // Interpolate
            l.Add($"clip = vs.core.resize.Bicubic(clip, format=vs.YUV444P16, matrix_s=\"709\")"); // Convert RGB to YUV

            if (s.Loop)
            {
                l.Add($"clip = clip.std.Trim(0, targetFrameCountMatchDuration-1)");
            }
            else
            {
                if (!s.MatchDuration)
                    l.Add($"clip = clip.std.Trim(0, targetFrameCountTrue-1)");
            }

            l.Add($"clip.set_output()"); // Set output

            string pkgPath = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir);
            string vpyPath = Path.Combine(pkgPath, "rife.vpy");

            File.WriteAllText(vpyPath, string.Join("\n", l));

            return vpyPath;
        }

        static List<string> GetScaleLines(VsSettings s)
        {
            bool resize = !s.InterpSettings.ScaledResolution.IsEmpty && s.InterpSettings.ScaledResolution != s.InterpSettings.InputResolution;
            List<string> l = new List<string>();

            l.Add($"");
            l.Add($"if clip.format.color_family == vs.YUV:");
            l.Add($"\tclip = core.resize.Bicubic(clip=clip, format=vs.RGBS, matrix_in_s=\"709\", range_s=\"limited\"{(resize ? $", width={s.InterpSettings.ScaledResolution.Width}, height={s.InterpSettings.ScaledResolution.Height}" : "")})");
            l.Add($"");
            l.Add($"if clip.format.color_family == vs.RGB:");
            l.Add($"\tclip = core.resize.Bicubic(clip=clip, format=vs.RGBS{(resize ? $", width={s.InterpSettings.ScaledResolution.Width}, height={s.InterpSettings.ScaledResolution.Height}" : "")})");
            l.Add($"");

            return l;
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
