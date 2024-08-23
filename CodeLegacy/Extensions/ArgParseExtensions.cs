using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flowframes.Extensions
{
    public static class ArgParseExtensions
    {
        public class Options
        {
            public NDesk.Options.OptionSet OptionsSet { get; set; }
            public string BasicUsage { get; set; } = "";
            public string AdditionalHelpText { get; set; } = "";
            public bool AddHelpArg { get; set; } = true;
            public bool AlwaysPrintHelp { get; set; } = false;
            public bool PrintLooseArgs { get; set; } = true;
        }

        public static void PrintHelp(this Options opts)
        {
            opts.OptionsSet.PrintHelp(opts.BasicUsage, opts.PrintLooseArgs);
        }

        public static void PrintHelp(this NDesk.Options.OptionSet opts, string basicUsage, bool printLooseArgs)
        {
            var lines = new List<string>();
            var lengths = new List<int>();

            foreach (var opt in opts)
            {
                var names = string.Join(", ", opt.GetNames().Select(s => s == opt.GetNames().First() ? $"-{s}" : $"--{s}"));
                lengths.Add(names.Length);
            }

            string looseStr = "<Loose Arguments>";
            var maxLen = Math.Max(lengths.Max(), looseStr.Length);

            foreach (var opt in opts)
            {
                var names = opt.GetNames().Select(s => s == opt.GetNames().First() ? $"-{s}".Replace("-<>", looseStr) : $"--{s}").ToList();
                if (names.Contains(looseStr) && !printLooseArgs) continue;
                string desc = opt.Description.IsEmpty() ? "?" : opt.Description;
                lines.Add($"{string.Join(", ", names).PadRight(maxLen)} : {desc}");
            }

            string prog = Path.GetFileNameWithoutExtension(Paths.GetExe());
            Logger.Log($"Usage:\n{prog} {basicUsage}\n{string.Join("\n", lines)}\n");
        }

        public static void AddHelpArgIfNotPresent(this Options opts)
        {
            bool alreadyHasHelpArg = false;

            foreach (var opt in opts.OptionsSet)
            {
                if (opt.Description.Lower() == "show help" || opt.GetNames().Contains("help") || opt.GetNames().Any(n => n == "h"))
                {
                    alreadyHasHelpArg = true;
                    break;
                }
            }

            if (!alreadyHasHelpArg)
            {
                opts.OptionsSet.Add("h|help", "Show help", v => opts.OptionsSet.PrintHelp(opts.BasicUsage, opts.PrintLooseArgs));
            }
        }

        public static bool TryParseOptions(this Options opts, IEnumerable<string> args)
        {
            if (opts.AddHelpArg)
            {
                opts.AddHelpArgIfNotPresent();
            }

            return opts.OptionsSet.TryParseOptions(args, opts.AlwaysPrintHelp, opts.BasicUsage, opts.PrintLooseArgs);
        }

        public static bool TryParseOptions(this NDesk.Options.OptionSet opts, IEnumerable<string> args, bool alwaysPrintHelp = false, string basicUsage = "", bool printLooseArgs = true)
        {
            try
            {
                bool canParse = args.Where(a => a.Trim() != "/?").Any();

                if (canParse)
                {
                    opts.Parse(args);
                }

                if (alwaysPrintHelp || !canParse)
                {
                    opts.PrintHelp(basicUsage, printLooseArgs);
                }

                return canParse;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to parse options! {ex}", true);
                return false;
            }
        }
    }
}
