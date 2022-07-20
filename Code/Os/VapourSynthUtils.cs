using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Ui;
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
            public bool Osd { get; set; } = true;
        }

        public static string CreateScript(VsSettings s)
        {
            Logger.Log($"Creating RIFE VS script. Model: {s.ModelDir}, Factor: {s.Factor}, Res: {s.Res.Width}x{s.Res.Height}, UHD: {s.Uhd}, SC Sens: {s.SceneDetectSensitivity}, " +
                $"GPU ID: {s.GpuId}, GPU Threads: {s.GpuThreads}, TTA: {s.Tta}, Loop: {s.Loop}, Match Duration: {s.MatchDuration}, Dedupe: {s.Dedupe}, RT: {s.Realtime}{(s.Osd ? $", OSD: {s.Osd}" : "")}", true);

            string inputPath = s.InterpSettings.inPath;
            string mdlPath = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir, s.ModelDir).Replace(@"\", "/").Wrap();

            bool sc = s.SceneDetectSensitivity >= 0.01f;
            long frameCount = (long)Interpolate.currentMediaFile.FrameCount;

            bool trim = QuickSettingsTab.trimEnabled;
            long srcTrimStartFrame = trim ? (long)(Math.Round(FormatUtils.TimestampToMs(QuickSettingsTab.trimStart) / 1000f * s.InterpSettings.inFps.GetFloat())) : 0;
            long srcTrimEndFrame = trim && QuickSettingsTab.doTrimEnd ? (long)(Math.Round(FormatUtils.TimestampToMs(QuickSettingsTab.trimEnd) / 1000f * s.InterpSettings.inFps.GetFloat())) - 1 : frameCount - 1;

            if(trim)
                frameCount = srcTrimEndFrame - srcTrimStartFrame;

            int endDupeCount = s.Factor.RoundToInt() - 1;
            int targetFrameCountMatchDuration = (frameCount * s.Factor).RoundToInt(); // Target frame count to match original duration (and for loops)
            int targetFrameCountTrue = targetFrameCountMatchDuration - endDupeCount; // Target frame count without dupes at the end (only in-between frames added)

            List<string> l = new List<string> { "import sys", "import os", "import json", "import time", "import functools", "import vapoursynth as vs", "core = vs.core", "" }; // Imports
            l.Add($"inputPath = r'{inputPath}'");
            l.Add($"");

            if (s.InterpSettings.inputIsFrames || (s.Dedupe && !s.Realtime))
            {
                string first = Path.GetFileNameWithoutExtension(IoUtils.GetFileInfosSorted(s.InterpSettings.framesFolder, false).FirstOrDefault().FullName);
                l.Add($"clip = core.imwri.Read(r'{Path.Combine(s.InterpSettings.framesFolder, $"%0{first.Length}d.png")}', firstnum={first.GetInt()})"); // Load image sequence with imwri
                l.Add($"clip = core.std.AssumeFPS(clip, fpsnum={s.InterpSettings.inFps.Numerator}, fpsden={s.InterpSettings.inFps.Denominator})"); // Set frame rate for img seq
            }
            else
            {
                l.Add("indexFilePath = f'{inputPath}.cache.lwi'");
                l.Add($"if os.path.isdir(r'{s.InterpSettings.tempFolder}'):");
                l.Add($"\tindexFilePath = r'{Path.Combine(s.InterpSettings.tempFolder, "cache.lwi")}'");
                l.Add($"clip = core.lsmas.LWLibavSource(inputPath, cachefile=indexFilePath)"); // Load video with lsmash
            }

            if (trim)
                l.Add($"clip = clip.std.Trim({srcTrimStartFrame}, {srcTrimEndFrame})");

            l.Add($"");

            if (s.Loop && !s.InterpSettings.inputIsFrames)
            {
                l.Add($"firstFrame = clip[0]"); // Grab first frame
                l.Add($"clip = clip + firstFrame"); // Add to end (for seamless loop interpolation)
            }

            l.AddRange(GetScaleLines(s));

            if (sc)
                l.Add($"clip = core.misc.SCDetect(clip=clip, threshold={s.SceneDetectSensitivity.ToStringDot()})"); // Scene detection

            l.Add($"clip = core.rife.RIFE(clip, multiplier={s.Factor.ToStringDot()}, model_path={mdlPath}, gpu_id={s.GpuId}, gpu_thread={s.GpuThreads}, tta={s.Tta}, uhd={s.Uhd}, sc={sc})"); // Interpolate

            if (s.Dedupe && !s.Realtime)
                l.AddRange(GetRedupeLines(s));

            l.Add($"clip = vs.core.resize.Bicubic(clip, format=vs.YUV444P16, matrix_s=cMatrix)"); // Convert RGB to YUV

            if (!s.Dedupe) // Ignore trimming code when using deduping that that already handles trimming in the frame order file
            {
                if (s.Loop)
                {
                    l.Add($"clip = clip.std.Trim(0, {targetFrameCountMatchDuration - 1})"); // -1 because we use index, not count
                }
                else
                {
                    if (!s.MatchDuration)
                        l.Add($"clip = clip.std.Trim(0, {targetFrameCountTrue - 1})"); // -1 because we use index, not count
                }
            }

            if(s.Realtime && s.Loop)
                l.AddRange(new List<string> { $"clip = clip.std.Loop(0)", "" }); // Can't loop piped video so we loop it before piping it to ffplay

            if(s.Realtime && s.Osd)
                l.AddRange(GetOsdLines(s));

            l.Add($"clip.set_output()"); // Set output
            l.Add("");

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
                l.Add($"colRange = 'limited'");
                l.Add($"");
                l.Add($"try:");
                l.Add($"\tif clip.get_frame(0).props._ColorRange == 0: colRange = 'full'");
                l.Add($"except:");
                l.Add($"\tcolRange = 'limited'");
                l.Add($"");
            }

            l.Add($"if clip.format.color_family == vs.YUV:");
            l.Add($"\tclip = core.resize.Bicubic(clip=clip, format=vs.RGBS, matrix_in_s=cMatrix, range_s=colRange{(resize ? $", width={s.InterpSettings.ScaledResolution.Width}, height={s.InterpSettings.ScaledResolution.Height}" : "")})");
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

        static List<string> GetOsdLines(VsSettings s)
        {
            List<string> l = new List<string>();

            l.Add($"framesProducedPrevious = 0");
            l.Add($"framesProducedCurrent = 0");
            l.Add($"lastFpsUpdateTime = time.time()");
            l.Add($"startTime = time.time()");
            l.Add($"");
            l.Add($"def onFrame(n, clip):");
            l.Add($"\tglobal startTime");
            l.Add($"\tfpsAvgTime = 1");
            l.Add($"\t");
            l.Add($"\tif time.time() - startTime > fpsAvgTime:");
            l.Add($"\t\tglobal framesProducedPrevious");
            l.Add($"\t\tglobal framesProducedCurrent");
            l.Add($"\t\tglobal lastFpsUpdateTime");
            l.Add($"\t\t");
            l.Add($"\t\tfpsFloat = (clip.fps.numerator / clip.fps.denominator)");
            l.Add($"\t\tvideoTimeFloat = (1 / fpsFloat) * n");
            l.Add($"\t\tframesProducedCurrent+=1");
            l.Add($"\t\t");
            l.Add($"\t\tif time.time() - lastFpsUpdateTime > fpsAvgTime:");
            l.Add($"\t\t\tlastFpsUpdateTime = time.time()");
            l.Add($"\t\t\tframesProducedPrevious = framesProducedCurrent / fpsAvgTime");
            l.Add($"\t\t\tframesProducedCurrent = 0");
            l.Add($"\t\t");
            l.Add($"\t\tspeed = (framesProducedPrevious / fpsFloat) * 100");
            l.Add($"\t\tosdString = f\"Time: {{time.strftime(\'%H:%M:%S\', time.gmtime(videoTimeFloat))}} - FPS: {{framesProducedPrevious:.2f}}/{{fpsFloat:.2f}} ({{speed:.0f}}%){{\' [!]\' if speed < 95 else \'\'}}\"");
            l.Add($"\t\tclip = core.text.Text(clip, text=osdString, alignment=7, scale=1)");
            l.Add($"\treturn clip");
            l.Add($"");
            l.Add($"clip = core.std.FrameEval(clip, functools.partial(onFrame, clip=clip))");
            l.Add($"");

            return l;
        }

        public static int GetSeekSeconds(long videoLengthSeconds)
        {
            int seekStep = 10;

            if(videoLengthSeconds >  2 * 60) seekStep = 20;
            if(videoLengthSeconds >  5 * 60) seekStep = 30;
            if(videoLengthSeconds > 15 * 60) seekStep = 60;

            return seekStep;
        }
    }
}
