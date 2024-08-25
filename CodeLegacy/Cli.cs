using Flowframes.Data;
using Flowframes.Extensions;
using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Os;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Flowframes
{
    public class Cli
    {
        public static bool ShowConsole = false;
        public static bool DisablePython = false;
        public static bool ShowMdlDownloader = false;
        public static bool DontSaveConfig = false;
        public static bool ExitWhenDone = false;
        public static bool InterpStart = false;
        public static float InterpFactor = -1f;
        public static Implementations.Ai InterpAi = (Implementations.Ai)(-1);
        public static Enums.Output.Format OutputFormat = Enums.Output.Format.Mp4;
        public static Enums.Encoding.Encoder Encoder = (Enums.Encoding.Encoder)(-1);
        public static Enums.Encoding.PixelFormat PixFmt = (Enums.Encoding.PixelFormat)(-1);
        public static string InterpModel = "";
        public static string OutputDir = "";
        public static int MaxHeight = -1;
        public static bool? Loop = false;
        public static bool? FixSceneChanges = false;
        public static float FixSceneChangeVal = -1f;
        public static float MaxOutFps = -1f;
        public static List<string> ValidFiles = new List<string>();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int FreeConsole();

        public static void HandleCli()
        {
            string GetEnums<T>() => string.Join(", ", Enum.GetNames(typeof(T)));

            var opts = new OptionSet
            {
                {
                    "c|console", "Show console",
                    v => ShowConsole = v != null
                },
                {
                    "np|no_python", "Disable Python implementations",
                    v => DisablePython = v != null
                },
                {
                    "md|open_model_downloader", "Open model downloader GUI on startup",
                    v => ShowMdlDownloader = v != null
                },
                {
                    "nc|no_config_save", "Do not save anything in config during this session",
                    v => DontSaveConfig = v != null
                },
                {
                    "e|exit", "Exit automatically after interpolation has finished",
                    v => ExitWhenDone = v != null
                },
                {
                    "s|start", "Start interpolation automatically if valid parameters are provided",
                    v => InterpStart = v != null
                },
                {
                    "f|factor=", "Interpolation factor",
                    v => InterpFactor = v.GetFloat()
                },
                {
                    "a|ai=", $"Interpolation AI implementation to use (Option: {GetEnums<Implementations.Ai>()})",
                    v => InterpAi = ParseUtils.GetEnum<Implementations.Ai>(v.Trim().Replace("_", ""))
                },
                {
                    "m|model=", "AI model to use",
                    v => InterpModel = v.Trim()
                },
                {
                    "vf|video_format=", $"Output video format to use (Options: {GetEnums<Enums.Output.Format>()})",
                    v => OutputFormat = ParseUtils.GetEnum<Enums.Output.Format>(v.Trim())
                },
                {
                    "ve|video_encoder=", $"Output video encoder to use (Options: {GetEnums<Enums.Encoding.Encoder>()})",
                    v => Encoder = ParseUtils.GetEnum<Enums.Encoding.Encoder>(v.Trim())
                },
                {
                    "pf|pixel_format=", $"Output pixel format to use (Options: {GetEnums<Enums.Encoding.PixelFormat>()})",
                    v => PixFmt = ParseUtils.GetEnum<Enums.Encoding.PixelFormat>(v.Trim())
                },
                {
                    "h|max_height=", $"Max video size (pixels height). Larger videos will be downscaled.",
                    v => MaxHeight = v.GetInt()
                },
                {
                    "l|loop", $"Enable loop output mode",
                    v => Loop = v != null ? true : (bool?)null
                },
                {
                    "scn|fix_scene_changes", $"Do not interpolate scene cuts to avoid artifacts",
                    v => FixSceneChanges = v != null ? true : (bool?)null
                },
                {
                    "scnv|scene_change_sensitivity=", $"Scene change sensitivity, lower is more sensitive (e.g. 0.18)",
                    v => FixSceneChangeVal = v.GetFloat()
                },
                {
                    "fps|max_fps=", $"Maximum FPS of output video, if the interpolation factor results in a higher FPS, it will be reduced to this value",
                    v => MaxOutFps = v.GetFloat()
                },
                {
                    "o|output_dir=", "Output folder to save the interpolated video in",
                    v => OutputDir = v.Trim()
                },
                {
                    "<>", "Input file(s)",
                    ValidFiles.Add
                },
            };

            try
            {
                if (!opts.TryParseOptions(Environment.GetCommandLineArgs()))
                    return;

                if (!ShowConsole)
                    FreeConsole();

                ValidFiles = ValidFiles.Where(f => File.Exists(f) && Path.GetExtension(f).Lower() != ".exe").Distinct().ToList();

                Python.DisablePython = DisablePython;
                Config.NoWrite = DontSaveConfig;
            }
            catch (OptionException e)
            {
                Logger.Log($"Error parsing CLI options: {e.Message}", true);
            }
        }
    }
}
