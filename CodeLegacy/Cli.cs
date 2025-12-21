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
        public static bool Debug = false;
        public static bool ShowHelp = false;
        public static bool CanStart = false;
        public static bool DisablePython = true;
        public static bool ShowMdlDownloader = false;
        public static bool CloseMdlDownloaderWhenDone = false;
        public static bool DontSaveConfig = false;
        public static bool AutoRun = false;
        public static bool Verbose = false;
        public static float InterpFactor = -1f;
        public static Implementations.Ai InterpAi = (Implementations.Ai)(-1);
        public static Enums.Output.Format OutputFormat = (Enums.Output.Format)(-1);
        public static Enums.Encoding.Encoder Encoder = (Enums.Encoding.Encoder)(-1);
        public static Enums.Encoding.PixelFormat PixFmt = (Enums.Encoding.PixelFormat)(-1);
        public static string InterpModel = "";
        public static string OutputDir = "";
        public static int MaxHeight = -1;
        public static bool? Loop = null;
        public static bool? FixSceneChanges = null;
        public static float FixSceneChangeVal = -1f;
        public static float MaxOutFps = -1f;
        public static List<string> ValidFiles = new List<string>();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int FreeConsole();

        /// <summary> Parses command line args. Returns if the program should continue (true) or exit (false). </summary>
        public static bool HandleCli()
        {
            string GetEnums<T>() => string.Join(", ", Enum.GetNames(typeof(T)));

            var optsSet = new OptionSet
            {
                {
                    "h|help", "Show CLI help",
                    v => ShowHelp = v != null
                },
                {
                    "d|debug", "Enable debug/developer features and experimental or deprecated options",
                    v => Debug = v != null
                },
                {
                    "np|no_python", "Disable Python implementations",
                    v => DisablePython = v != null
                },
                {
                    "py|enable_python", "Enable Python implementations",
                    v => DisablePython = !(v != null)
                },
                {
                    "md|open_model_downloader", "Open model downloader GUI on startup",
                    v => ShowMdlDownloader = v != null
                },
                {
                    "mdc|close_model_downloader", "Close model downloader GUI after downloads have finished",
                    v => CloseMdlDownloaderWhenDone = v != null
                },
                {
                    "nc|no_config_save", "Do not save anything in config during this session",
                    v => DontSaveConfig = v != null
                },
                {
                    "a|autorun", "Start interpolation automatically if valid parameters are provided and exit afterwards",
                    v => AutoRun = v != null
                },
                {
                    "f|factor=", "Interpolation factor",
                    v => InterpFactor = v.GetFloat()
                },
                {
                    "ai=", $"Interpolation AI implementation to use (Option: {GetEnums<Implementations.Ai>()})",
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
                    "mh|max_height=", $"Max video size in height pixels. Larger videos will be downscaled. (Example: 720)",
                    v => MaxHeight = v.GetInt()
                },
                {
                    "l|loop=", $"Enable loop output mode",
                    v => Loop = v.GetBoolCli()
                },
                {
                    "scn|fix_scene_changes=", $"Do not interpolate scene cuts to avoid artifacts",
                    v => FixSceneChanges = v.GetBoolCli()
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
                    "v|verbose", "Log process outputs",
                    v => Verbose = v != null
                },
                {
                    "<>", "Input file(s)",
                    ValidFiles.Add
                },
            };

            var opts = new ArgParseExtensions.Options() { OptionsSet = optsSet, BasicUsage = "<OPTIONS> <FILE(S)>", AddHelpArg = false };

            try
            {
                var args = Environment.GetCommandLineArgs();

                if (!opts.TryParseOptions(args))
                    return false;

                bool noArgs = args.Select(a => a.TrimStart('-').Lower().Trim('"')).Where(a => !a.EndsWith(".exe") && a != "h" && a != "help").Count() < 1; // No args passed (apart from program exe path and/or help flag)
                
                if (Program.CmdMode && (noArgs || ShowHelp))
                {
                    opts.PrintHelp();
                }
                
                Python.DisablePython = DisablePython;
                Config.NoWrite = DontSaveConfig;

                ValidFiles = ValidFiles.Where(f => File.Exists(f) && Path.GetExtension(f).Lower() != ".exe").Distinct().ToList();
                AutoRun = AutoRun && ValidFiles.Any(); // Only AutoRun if valid files are provided
                DontSaveConfig = DontSaveConfig || AutoRun; // Never save config in AutoRun mode
                return CanStart;
            }
            catch (OptionException e)
            {
                Logger.Log($"Error parsing CLI options: {e.Message}", true);
                return false;
            }
        }
    }
}
