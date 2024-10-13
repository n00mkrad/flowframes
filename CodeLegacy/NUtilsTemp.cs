using Flowframes.IO;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flowframes
{
    public class NUtilsTemp
    {
        public class OsUtils
        {
            public static List<Process> SessionProcesses = new List<Process>();

            public delegate void OutputDelegate(string s);

            public class RunConfig
            {
                public string Command { get; set; } = "";
                public int PrintOutputLines { get; set; } = 0;
                public ProcessPriorityClass? Priority { get; set; } = null;
                public Func<bool> Killswitch { get; set; } = null;
                public int KillswitchCheckIntervalMs = 1000;
                public OutputDelegate OnStdout;
                public OutputDelegate OnStderr;
                public OutputDelegate OnOutput;

                public RunConfig() { }

                public RunConfig(string cmd, int printOutputLines = 0)
                {
                    Command = cmd;
                    PrintOutputLines = printOutputLines;
                }
            }

            public class CommandResult
            {
                public string Output { get; set; } = "";
                public string StdOut { get; set; } = "";
                public string StdErr { get; set; } = "";
                public int ExitCode { get; set; } = 0;
                public TimeSpan RunTime { get; set; }
            }

            public static string RunCommand(string command, int printOutputLines = 0)
            {
                return Run(new RunConfig(command, printOutputLines)).Output;
            }

            public static CommandResult RunCommandShell(string cmd, int printOutputLines = 0)
            {
                var cfg = new RunConfig(cmd, printOutputLines);
                return Run(cfg);
            }

            public static CommandResult Run(RunConfig cfg)
            {
                var sw = new NmkdStopwatch();
                CommandResult result = null;

                try
                {
                    string tempScript = "";

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {cfg.Command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    Logger.Log($"{startInfo.FileName} {startInfo.Arguments}", hidden: true);

                    using (Process process = new Process { StartInfo = startInfo })
                    {
                        var output = new StringBuilder();
                        var stdout = new StringBuilder();
                        var stderr = new StringBuilder();
                        var outputClosed = new TaskCompletionSource<bool>();
                        var errorClosed = new TaskCompletionSource<bool>();

                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputClosed.SetResult(true);
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                                stdout.AppendLine(e.Data);
                                // Log($"[STDOUT] {e.Data}", Level.Debug);
                                cfg.OnStdout?.Invoke($"{e.Data}");
                                cfg.OnOutput?.Invoke($"{e.Data}");
                            }
                        };

                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorClosed.SetResult(true);
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                                stderr.AppendLine(e.Data);
                                // Log($"[STDERR] {e.Data}", Level.Debug);
                                cfg.OnStderr?.Invoke($"{e.Data}");
                                cfg.OnOutput?.Invoke($"{e.Data}");
                            }
                        };

                        ProcessPriorityClass? previousParentPrio = GetOwnProcessPriority();
                        SetOwnProcessPriority(cfg.Priority); // The only reliable way of setting the new child proc's priority is by changing the parent's priority...
                        process.Start();
                        SessionProcesses.Add(process);
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        SetOwnProcessPriority(previousParentPrio); // ...and afterwards changing the parent's priority back to what it was

                        if (cfg.Killswitch == null)
                        {
                            process.WaitForExit();
                        }
                        else
                        {
                            while (cfg.Killswitch() == false)
                            {
                                Thread.Sleep(cfg.KillswitchCheckIntervalMs);

                                if (process.HasExited)
                                    break;
                            }

                            // Killswitch true
                            if (!process.HasExited)
                            {
                                Logger.Log("Killswitch true, killing process.", hidden: true);
                                process.Kill();
                            }
                        }

                        // Ensure output and error streams have finished processing
                        Task.WhenAll(outputClosed.Task, errorClosed.Task).Wait();

                        result = new CommandResult { Output = output.ToString(), StdOut = stdout.ToString(), StdErr = stderr.ToString(), ExitCode = process.ExitCode, RunTime = sw.Elapsed };

                        if (tempScript.IsNotEmpty())
                        {
                            IoUtils.TryDeleteIfExists(tempScript);
                        }

                        // if (cfg.PrintOutputLines > 0)
                        // {
                        //     Logger.Log($"Finished (Code {result.ExitCode}). Output:{Environment.NewLine}...{Environment.NewLine}{string.Join(Environment.NewLine, result.Output.SplitIntoLines().TakeLast(cfg.PrintOutputLines))}", hidden: true);
                        // }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error running command: {ex.Message}", hidden: true);
                    result = new CommandResult { ExitCode = 1, RunTime = sw.Elapsed };
                }

                return result;
            }

            public static Process NewProcess(bool hidden, string filename = "cmd.exe", Action<string> logAction = null, bool redirectStdin = false, Encoding outputEnc = null)
            {
                var p = new Process();
                p.StartInfo.UseShellExecute = !hidden;
                p.StartInfo.RedirectStandardOutput = hidden;
                p.StartInfo.RedirectStandardError = hidden;
                p.StartInfo.CreateNoWindow = hidden;
                p.StartInfo.FileName = filename;
                p.StartInfo.RedirectStandardInput = redirectStdin;

                if (outputEnc != null)
                {
                    p.StartInfo.StandardOutputEncoding = outputEnc;
                    p.StartInfo.StandardErrorEncoding = outputEnc;
                }

                if (hidden && logAction != null)
                {
                    p.OutputDataReceived += (sender, line) => { logAction(line.Data); };
                    p.ErrorDataReceived += (sender, line) => { logAction(line.Data); };
                }

                return p;
            }

            public static ProcessPriorityClass GetOwnProcessPriority()
            {
                using (Process self = Process.GetCurrentProcess())
                {
                    return self.PriorityClass;
                }
            }

            public static void SetOwnProcessPriority(ProcessPriorityClass? priority = ProcessPriorityClass.BelowNormal)
            {
                if (priority == null)
                    return;

                using (Process self = Process.GetCurrentProcess())
                {
                    self.PriorityClass = priority == null ? ProcessPriorityClass.BelowNormal : (ProcessPriorityClass)priority;
                    // Logger.Log($"Process priority changed to {self.PriorityClass}", hidden: true);
                }
            }

        }
    }
}
