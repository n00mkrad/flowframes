using Flowframes.Data;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
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
            public bool Alpha { get; set; } = false;
            public Size Res { get; set; } = new Size();
            public bool Uhd { get; set; } = false;
            public float SceneDetectSensitivity { get; set; } = 0.15f;
            public int GpuId { get; set; } = 0;
            public int GpuThreads { get; set; } = 2;
            public bool Tta { get; set; } = false;
            public bool Loop { get; set; } = false;
            public bool MatchDuration { get; set; } = false;
            public bool Dedupe { get; set; } = false;
            public bool Realtime { get; set; } = false;
            public bool Osd { get; set; } = true;
            public int PadX { get; set; } = 0;
            public int PadY { get; set; } = 0;
        }

        public static string GetVsPipeArgs(VsSettings s)
        {
            Logger.Log($"Preparing RIFE VS args. Model: {s.ModelDir}, Factor: {s.Factor}, Res: {s.Res.Width}x{s.Res.Height}, UHD: {s.Uhd}, SC Sens: {s.SceneDetectSensitivity}, " +
                $"GPU ID: {s.GpuId}, GPU Threads: {s.GpuThreads}, TTA: {s.Tta}, Loop: {s.Loop}, Match Duration: {s.MatchDuration}, Dedupe: {s.Dedupe}, RT: {s.Realtime}{(s.Osd ? $", OSD: {s.Osd}" : "")}", true);
            bool debug = Program.Debug && !(System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift);

            var args = new List<(string, object)>()
            {
                ("input", s.InterpSettings.inPath), // Input path
                ("tmpDir", s.InterpSettings.tempFolder), // Temp dir path
                ("cache", Path.Combine(Paths.GetCachePath(), PseudoHash.GetHash(s.InterpSettings.inPath) + ".lwi")), // File PseudoHash to allow caching
                ("inFps", s.InterpSettings.inFps), // Input FPS
                ("outFps", !s.InterpSettings.inputIsFrames ? Interpolate.currentMediaFile.VideoStreams.First().FpsInfo.SpecifiedFps * s.Factor : s.InterpSettings.inFps * s.Factor), // Output FPS
                ("outFpsRes", s.InterpSettings.outFpsResampled),
                ("resSc", s.InterpSettings.ScaledResolution.ToStringShort()),
                ("pad", $"{s.InterpSettings.InterpResolution.Width - s.InterpSettings.ScaledResolution.Width}x{s.InterpSettings.InterpResolution.Height - s.InterpSettings.ScaledResolution.Height}"), // Padding
                ("frames", s.InterpSettings.inputIsFrames), // Input is frames?
                ("dedupe", s.InterpSettings.dedupe), // Dedupe?
                ("redupe", !s.InterpSettings.noRedupe), // Allow redupe?
                ("matchDur", s.MatchDuration), // Match duration?
                ("sc", s.SceneDetectSensitivity), // Scene change detection sensitivity
                ("loop", s.Loop), // Loop?
                ("factor", new Fraction(s.Factor)),
                ("mdl", s.ModelDir), // Path.Combine(Paths.GetPkgPath(), Implementations.rifeNcnnVs.PkgDir, s.ModelDir).Replace(@"\", "/")), // Model path
                ("gpuThrds", s.GpuThreads), // GPU threads
                ("uhd", s.Uhd), // UHD?
                ("tta", s.Tta), // TTA?
                ("gpu", s.GpuId), // GPU ID
                ("rt", s.Realtime), // Realtime?
                ("osd", debug), // OSD?
                ("debugFrNums", debug), // Show debug overlay with frame nums?
                ("debugVars", debug), // Show debug overlay with variables?
            };

            long frameCount = Interpolate.currentMediaFile.FrameCount;
            bool trim = QuickSettingsTab.trimEnabled;
            long srcTrimStartFrame = trim ? (long)Math.Round(FormatUtils.TimestampToMs(QuickSettingsTab.trimStart) / 1000f * s.InterpSettings.inFps.Float) : 0;
            long srcTrimEndFrame = trim && QuickSettingsTab.doTrimEnd ? (long)(Math.Round(FormatUtils.TimestampToMs(QuickSettingsTab.trimEnd) / 1000f * s.InterpSettings.inFps.Float)) - 1 : frameCount - 1;
            int endDupeCount = s.Factor.RoundToInt() - 1;
            int targetFrameCountMatchDuration = (frameCount * s.Factor).RoundToInt(); // Target frame count to match original duration (and for loops)

            if (trim)
            {
                frameCount = srcTrimEndFrame - srcTrimStartFrame;
                args.Add(("trim", $"{srcTrimStartFrame}/{srcTrimEndFrame}"));
            }
            else
            {
                args.Add(("trim", ""));
            }

            string frameIndexesJsonPath = Path.Combine(s.InterpSettings.tempFolder, "frameIndexes.json");

            if (Interpolate.currentMediaFile.IsVfr && Interpolate.currentMediaFile.OutputFrameIndexes != null && Interpolate.currentMediaFile.OutputFrameIndexes.Count > 0 && s.InterpSettings.outFpsResampled.Float > 0.1f)
            {
                File.WriteAllText(frameIndexesJsonPath, Interpolate.currentMediaFile.OutputFrameIndexes.ToJson());
            }
            else
            {
                IoUtils.TryDeleteIfExists(frameIndexesJsonPath);
            }

            bool use470bg = s.InterpSettings.inputIsFrames && !new[] { "GIF", "EXR" }.Contains(Interpolate.currentMediaFile.Format.Upper());
            args.Add(("cMatrix", use470bg ? "470bg" : ""));
            args.Add(("targetMatch", targetFrameCountMatchDuration));

            args = args.Where(a => a.Item2.ToString() != "False" && a.Item2.ToString() != "").ToList();
            return string.Join(" ", args.Select(a => $"--arg {a.Item1}={a.Item2.ToString().Wrap()}"));
        }

        public static int GetSeekSeconds(long videoLengthSeconds)
        {
            if (videoLengthSeconds > 15 * 60) return 60;
            if (videoLengthSeconds > 5 * 60) return 30;
            if (videoLengthSeconds > 2 * 60) return 20;
            return 10;
        }
    }
}
