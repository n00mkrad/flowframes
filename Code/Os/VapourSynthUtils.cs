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
            public bool Dedupe { get; set; } = false;
            public bool Realtime { get; set; } = false;
        }

        public static string CreateScript(VsSettings s)
        {
            string inputPath = s.InterpSettings.inPath;
            string mdlPath = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir, s.ModelDir).Replace(@"\", "/").Wrap();

            bool sc = s.SceneDetectSensitivity >= 0.01f;

            int endDupeCount = s.Factor.RoundToInt() - 1;
            int targetFrameCountMatchDuration = (Interpolate.currentInputFrameCount * s.Factor).RoundToInt(); // Target frame count to match original duration (and for loops)
            int targetFrameCountTrue = targetFrameCountMatchDuration - endDupeCount; // Target frame count without dupes at the end (only in-between frames added)

            List<string> l = new List<string> { "import sys", "import os", "import json", "import vapoursynth as vs", "core = vs.core", "" }; // Imports

            if (s.InterpSettings.inputIsFrames || s.Dedupe)
            {
                string first = Path.GetFileNameWithoutExtension(IoUtils.GetFileInfosSorted(s.InterpSettings.framesFolder, false).FirstOrDefault().FullName);
                l.Add($"clip = core.imwri.Read(r'{Path.Combine(s.InterpSettings.framesFolder, $"%0{first.Length}d.png")}', firstnum={first.GetInt()})"); // Load image sequence with imwri
                l.Add($"clip = core.std.AssumeFPS(clip, fpsnum={s.InterpSettings.inFps.Numerator}, fpsden={s.InterpSettings.inFps.Denominator})"); // Set frame rate for img seq
            }
            else
            {
                l.Add($"indexFilePath = r'{inputPath}.cache.lwi'");
                l.Add($"if os.path.isdir(r'{s.InterpSettings.tempFolder}'):");
                l.Add($"\tindexFilePath = r'{Path.Combine(s.InterpSettings.tempFolder, "cache.lwi")}'");
                l.Add($"clip = core.lsmas.LWLibavSource(r'{inputPath}', cachefile=indexFilePath)"); // Load video with lsmash
            }

            if (s.Loop && !s.InterpSettings.inputIsFrames)
            {
                l.Add($"firstFrame = clip[0]"); // Grab first frame
                l.Add($"clip = clip + firstFrame"); // Add to end (for seamless loop interpolation)
            }

            l.AddRange(GetScaleLines(s));

            if (sc)
                l.Add($"clip = core.misc.SCDetect(clip=clip,threshold={s.SceneDetectSensitivity.ToStringDot()})"); // Scene detection

            l.Add($"clip = core.rife.RIFE(clip, {9}, {s.Factor.ToStringDot()}, {mdlPath}, {s.GpuId}, {s.GpuThreads}, {s.Tta}, {s.Uhd}, {sc})"); // Interpolate

            if (s.Dedupe)
                l.AddRange(GetRedupeLines(s));

            l.Add($"clip = vs.core.resize.Bicubic(clip, format=vs.YUV444P16, matrix_s=cMatrix)"); // Convert RGB to YUV

            if (s.Loop)
            {
                l.Add($"clip = clip.std.Trim(0, {targetFrameCountMatchDuration - 1})"); // -1 because we use index, not count
            }
            else
            {
                if (!s.MatchDuration)
                    l.Add($"clip = clip.std.Trim(0, {targetFrameCountTrue - 1})"); // -1 because we use index, not count
            }

            if(s.Realtime && s.Loop)
                l.Add($"clip = clip.std.Loop(0)"); // Can't loop piped video so we loop it before piping it to ffplay

            l.Add($"clip.set_output()"); // Set output

            l.Add($"if os.path.isfile(r'{inputPath}.cache.lwi'):");
            l.Add($"\tos.remove(r'{inputPath}.cache.lwi')");

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
            l.Add($"cMatrix = '709'");
            l.Add($"");

            if (!s.InterpSettings.inputIsFrames)
            {
                l.Add("try:");
                l.Add("\tm = clip.get_frame(0).props._Matrix");
                l.Add("\tif m == 0:    cMatrix = 'rgb'");
                l.Add("\telif m == 4:  cMatrix = 'fcc'");
                l.Add("\telif m == 5:  cMatrix = '470bg'");
                l.Add("\telif m == 6:  cMatrix = '170m'");
                l.Add("\telif m == 7:  cMatrix = '240m'");
                l.Add("\telif m == 8:  cMatrix = 'ycgco'");
                l.Add("\telif m == 9:  cMatrix = '2020ncl'");
                l.Add("\telif m == 10: cMatrix = '2020cl'");
                l.Add("\telif m == 12: cMatrix = 'chromancl'");
                l.Add("\telif m == 13: cMatrix = 'chromacl'");
                l.Add("\telif m == 14: cMatrix = 'ictcp'");
                l.Add($"except:");
                l.Add($"\tcMatrix = '709'");
                l.Add($"");
            }

            l.Add($"if clip.format.color_family == vs.YUV:");
            l.Add($"\tclip = core.resize.Bicubic(clip=clip, format=vs.RGBS, matrix_in_s=cMatrix, range_s=('full' if clip.get_frame(0).props._ColorRange == 2 else 'limited'){(resize ? $", width={s.InterpSettings.ScaledResolution.Width}, height={s.InterpSettings.ScaledResolution.Height}" : "")})");
            l.Add($"");
            l.Add($"if clip.format.color_family == vs.RGB:");
            l.Add($"\tclip = core.resize.Bicubic(clip=clip, format=vs.RGBS{(resize ? $", width={s.InterpSettings.ScaledResolution.Width}, height={s.InterpSettings.ScaledResolution.Height}" : "")})");
            l.Add($"");

            return l;
        }

        static List<string> GetRedupeLines(VsSettings s)
        {
            List<string> l = new List<string>();

            l.Add(@"reorderedClip = clip[0]");
            l.Add("");
            l.Add($"with open(r'{Path.Combine(s.InterpSettings.tempFolder, "frames.vs.json")}') as json_file:");
            l.Add("\tframeList = json.load(json_file)");
            l.Add("\t");
            l.Add("\tfor i in frameList:");
            l.Add("\t\tif(i < clip.num_frames):");
            l.Add("\t\t\treorderedClip = reorderedClip + clip[i]");
            l.Add("");
            l.Add("clip = reorderedClip.std.Trim(1, reorderedClip.num_frames - 1)");
            l.Add("");

            return l;
        }
    }
}
