using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

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

            bool loadFrames = s.InterpSettings.inputIsFrames;

            if (loadFrames)
            {
                FileInfo[] frames = IoUtils.GetFileInfosSorted(s.InterpSettings.framesFolder, false, "*.*");
                string ext = frames.FirstOrDefault().Extension;
                string first = Path.GetFileNameWithoutExtension(frames.FirstOrDefault().FullName);
                l.Add($"clip = core.imwri.Read(r'{Path.Combine(s.InterpSettings.framesFolder, $"%0{first.Length}d{ext}")}', firstnum={first.GetInt()})"); // Load image sequence with imwri
                l.Add($"clip = core.std.AssumeFPS(clip, fpsnum={s.InterpSettings.inFps.Numerator}, fpsden={s.InterpSettings.inFps.Denominator})"); // Set frame rate for img seq
            }
            else
            {
                l.Add("indexFilePath = f'{inputPath}.cache.lwi'");
                l.Add($"if os.path.isdir(r'{s.InterpSettings.tempFolder}'):");
                l.Add($"    indexFilePath = r'{Path.Combine(s.InterpSettings.tempFolder, "cache.lwi")}'");
                l.Add($"clip = core.lsmas.LWLibavSource(inputPath, cachefile=indexFilePath)"); // Load video with lsmash
                l.Add(Debugger.IsAttached ? "clip = core.text.FrameNum(clip, alignment=7)" : "");
                l.Add(GetDedupeLines(s));
            }

            if (trim)
                l.Add($"clip = clip.std.Trim({srcTrimStartFrame}, {srcTrimEndFrame})");

            l.Add($"");

            if (s.Loop && !s.InterpSettings.inputIsFrames)
            {
                l.Add($"firstFrame = clip[0]"); // Grab first frame
                l.Add($"clip = clip + firstFrame"); // Add to end (for seamless loop interpolation)
            }

            l.Add(GetScaleLines(s, loadFrames));

            if (sc)
                l.Add($"clip = core.misc.SCDetect(clip=clip, threshold={s.SceneDetectSensitivity.ToStringDot()})"); // Scene detection

            Fraction outFps = s.InterpSettings.inFps * s.Factor;

            if (!loadFrames)
            {
                outFps = Interpolate.currentMediaFile.VideoStreams.First().FpsInfo.SpecifiedFps * s.Factor;
            }

            l.Add($"clip = core.rife.RIFE(clip, fps_num={outFps.Numerator}, fps_den={outFps.Denominator}, model_path={mdlPath}, gpu_id={s.GpuId}, gpu_thread={s.GpuThreads}, tta={s.Tta}, uhd={s.Uhd}, sc={sc})"); // Interpolate

            if (s.Dedupe && !s.Realtime)
                l.Add(GetRedupeLines(s));

            Console.WriteLine($"In Format: {Interpolate.currentMediaFile.Format.Upper()}");
            bool use470bg = loadFrames && !new[] { "GIF", "EXR" }.Contains(Interpolate.currentMediaFile.Format.Upper());
            l.Add($"clip = vs.core.resize.Bicubic(clip, format=vs.YUV444P16, matrix_s={(use470bg ? "'470bg'" : "cMatrix")})"); // Convert RGB to YUV. Always use 470bg if input is YUV frames

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
                l.Add(GetOsdLines());

            l.Add($"clip.set_output()"); // Set output
            l.Add("");

            l.Add($"if os.path.isfile(r'{inputPath}.cache.lwi'):");
            l.Add($"    os.remove(r'{inputPath}.cache.lwi')");

            string pkgPath = Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir);
            string vpyPath = Path.Combine(pkgPath, "rife.vpy");

            File.WriteAllText(vpyPath, string.Join("\n", l));

            return vpyPath;
        }

        static string GetScaleLines(VsSettings settings, bool loadFrames)
        {
            InterpSettings interp = settings.InterpSettings;
            bool resize = !interp.ScaledResolution.IsEmpty && interp.ScaledResolution != interp.InputResolution;
            string s = "";

            s += $"\n";
            s += $"cMatrix = '709'\n";
            s += $"\n";

            if (!loadFrames)
            {
                s += "try:\n";
                s += "    m = clip.get_frame(0).props._Matrix\n";
                s += "    if m == 0:    cMatrix = 'rgb'\n";
                s += "    elif m == 4:  cMatrix = 'fcc'\n";
                s += "    elif m == 5:  cMatrix = '470bg'\n";
                s += "    elif m == 6:  cMatrix = '170m'\n";
                s += "    elif m == 7:  cMatrix = '240m'\n";
                s += "    elif m == 8:  cMatrix = 'ycgco'\n";
                s += "    elif m == 9:  cMatrix = '2020ncl'\n";
                s += "    elif m == 10: cMatrix = '2020cl'\n";
                s += "    elif m == 12: cMatrix = 'chromancl'\n";
                s += "    elif m == 13: cMatrix = 'chromacl'\n";
                s += "    elif m == 14: cMatrix = 'ictcp'\n";
                s += $"except:\n";
                s += $"    cMatrix = '709'\n";
                s += $"\n";
                s += $"colRange = 'limited'\n";
                s += $"\n";
                s += $"try:\n";
                s += $"    if clip.get_frame(0).props._ColorRange == 0: colRange = 'full'\n";
                s += $"except:\n";
                s += $"    colRange = 'limited'\n";
                s += $"\n";
            }

            s += $"if clip.format.color_family == vs.YUV:\n";
            s += $"    clip = core.resize.Bicubic(clip=clip, format=vs.RGBS, matrix_in_s=cMatrix, range_s=colRange{(resize ? $", width={interp.ScaledResolution.Width}, height={interp.ScaledResolution.Height}" : "")})\n";
            s += $"\n";
            s += $"if clip.format.color_family == vs.RGB:\n";
            s += $"    clip = core.resize.Bicubic(clip=clip, format=vs.RGBS{(resize ? $", width={interp.ScaledResolution.Width}, height={interp.ScaledResolution.Height}" : "")})\n";
            s += $"\n";

            return s;
        }

        static string GetDedupeLines(VsSettings settings)
        {
            string s = "";

            string inputJsonPath = Path.Combine(settings.InterpSettings.tempFolder, "input.json");

            if (!File.Exists(inputJsonPath))
                return s;

            s += "reorderedClip = clip[0]\n";
            s += "\n";
            s += $"with open(r'{inputJsonPath}') as json_file:\n";
            s += "    frameList = json.load(json_file)\n";
            s += "    \n";
            s += "    for i in frameList:\n";
            s += "        reorderedClip = reorderedClip + clip[i]\n";
            s += "\n";
            s += "clip = reorderedClip.std.Trim(1, reorderedClip.num_frames - 1)\n";
            s += Debugger.IsAttached ? "clip = core.text.FrameNum(clip, alignment=4)\n" : "";
            s += "\n";

            return s;
        }

        static string GetRedupeLines(VsSettings settings)
        {
            string s = "";

            s += "reorderedClip = clip[0]\n";
            s += "\n";
            s += $"with open(r'{Path.Combine(settings.InterpSettings.tempFolder, "frames.vs.json")}') as json_file:\n";
            s += "    frameList = json.load(json_file)\n";
            s += "    \n";
            s += "    for i in frameList:\n";
            s += "        if(i < clip.num_frames):\n";
            s += "            reorderedClip = reorderedClip + clip[i]\n";
            s += "\n";
            s += "clip = reorderedClip.std.Trim(1, reorderedClip.num_frames - 1)\n";
            s += Debugger.IsAttached ? "clip = core.text.FrameNum(clip, alignment=1)\n" : "";
            s += "\n";

            return s;
        }

        static string GetOsdLines()
        {
            string s = "";

            s += $"framesProducedPrevious = 0 \n";
            s += $"framesProducedCurrent = 0 \n";
            s += $"lastFpsUpdateTime = time.time() \n";
            s += $"startTime = time.time() \n";
            s += $" \n";
            s += $"def onFrame(n, clip): \n";
            s += $"    global startTime \n";
            s += $"    fpsAvgTime = 1 \n";
            s += $"     \n";
            s += $"    if time.time() - startTime > fpsAvgTime: \n";
            s += $"        global framesProducedPrevious \n";
            s += $"        global framesProducedCurrent \n";
            s += $"        global lastFpsUpdateTime \n";
            s += $"         \n";
            s += $"        fpsFloat = (clip.fps.numerator / clip.fps.denominator) \n";
            s += $"        videoTimeFloat = (1 / fpsFloat) * n \n";
            s += $"        framesProducedCurrent+=1 \n";
            s += $"         \n";
            s += $"        if time.time() - lastFpsUpdateTime > fpsAvgTime: \n";
            s += $"            lastFpsUpdateTime = time.time() \n";
            s += $"            framesProducedPrevious = framesProducedCurrent / fpsAvgTime \n";
            s += $"            framesProducedCurrent = 0 \n";
            s += $"         \n";
            s += $"        speed = (framesProducedPrevious / fpsFloat) * 100 \n";
            s += $"        osdString = f\"Time: {{time.strftime(\'%H:%M:%S\', time.gmtime(videoTimeFloat))}} - FPS: {{framesProducedPrevious:.2f}}/{{fpsFloat:.2f}} ({{speed:.0f}}%){{\' [!]\' if speed < 95 else \'\'}}\" \n";
            s += $"        clip = core.text.Text(clip, text=osdString, alignment=7, scale=1) \n";
            s += $"    return clip \n";
            s += $" \n";
            s += $"clip = core.std.FrameEval(clip, functools.partial(onFrame, clip=clip)) \n";
            s += $" \n";

            return s;
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
