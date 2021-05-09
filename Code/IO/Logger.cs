using Flowframes.IO;
using Flowframes.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DT = System.DateTime;

namespace Flowframes
{
    class Logger
    {
        public static TextBox textbox;
        static string file;
        public const string defaultLogName = "sessionlog";
        public static long id;

        public struct LogEntry
        {
            public string logMessage;
            public bool hidden;
            public bool replaceLastLine;
            public string filename;

            public LogEntry(string logMessageArg, bool hiddenArg = false, bool replaceLastLineArg = false, string filenameArg = "")
            {
                logMessage = logMessageArg;
                hidden = hiddenArg;
                replaceLastLine = replaceLastLineArg;
                filename = filenameArg;
            }
        }

        private static ConcurrentQueue<LogEntry> logQueue = new ConcurrentQueue<LogEntry>();

        public static void Log(string msg, bool hidden = false, bool replaceLastLine = false, string filename = "")
        {
            logQueue.Enqueue(new LogEntry(msg, hidden, replaceLastLine, filename));
        }

        public static async Task Run()
        {
            while (true)
            {
                if (!logQueue.IsEmpty)
                {
                    LogEntry entry;

                    if (logQueue.TryDequeue(out entry))
                        Show(entry);
                }
            }
        }

        public static void Show(LogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.logMessage))
                return;

            Console.WriteLine(entry.logMessage);

            try
            {
                if (!entry.hidden && entry.replaceLastLine)
                {
                    textbox.Suspend();
                    string[] lines = textbox.Text.SplitIntoLines();
                    textbox.Text = string.Join(Environment.NewLine, lines.Take(lines.Count() - 1).ToArray());
                }
            }
            catch { }

            entry.logMessage = entry.logMessage.Replace("\n", Environment.NewLine);

            if (!entry.hidden && textbox != null)
                textbox.AppendText((textbox.Text.Length > 1 ? Environment.NewLine : "") + entry.logMessage);

            if (entry.replaceLastLine)
            {
                textbox.Resume();
                entry.logMessage = "[REPL] " + entry.logMessage;
            }

            if (!entry.hidden)
                entry.logMessage = "[UI] " + entry.logMessage;

            LogToFile(entry.logMessage, false, entry.filename);
        }

        public static void LogToFile(string logStr, bool noLineBreak, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = defaultLogName;

            if (Path.GetExtension(filename) != ".txt")
                filename = Path.ChangeExtension(filename, "txt");
            file = Path.Combine(Paths.GetLogPath(), filename);
            logStr = logStr.Replace(Environment.NewLine, " ").TrimWhitespaces();
            string time = DT.Now.Month + "-" + DT.Now.Day + "-" + DT.Now.Year + " " + DT.Now.Hour + ":" + DT.Now.Minute + ":" + DT.Now.Second;

            try
            {
                if (!noLineBreak)
                    File.AppendAllText(file, $"{Environment.NewLine}[{id}] [{time}]: {logStr}");
                else
                    File.AppendAllText(file, " " + logStr);
                id++;
            }
            catch
            {
                // this if fine, i forgot why
            }
        }

        public static void LogIfLastLineDoesNotContainMsg (string s, bool hidden = false, bool replaceLastLine = false, string filename = "")
        {
            if (!GetLastLine().Contains(s))
                Log(s, hidden, replaceLastLine, filename);
        }

        public static void WriteToFile (string content, bool append, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = defaultLogName;

            if (Path.GetExtension(filename) != ".txt")
                filename = Path.ChangeExtension(filename, "txt");

            file = Path.Combine(Paths.GetLogPath(), filename);

            string time = DT.Now.Month + "-" + DT.Now.Day + "-" + DT.Now.Year + " " + DT.Now.Hour + ":" + DT.Now.Minute + ":" + DT.Now.Second;

            try
            {
                if (append)
                    File.AppendAllText(file, Environment.NewLine + time + ":" + Environment.NewLine + content);
                else
                    File.WriteAllText(file, Environment.NewLine + time + ":" + Environment.NewLine + content);
            }
            catch
            {
                
            }
        }

        public static void ClearLogBox ()
        {
            textbox.Text = "";
        }

        public static string GetLastLine ()
        {
            string[] lines = textbox.Text.SplitIntoLines();
            if (lines.Length < 1)
                return "";
            return lines.Last();
        }

        public static void RemoveLastLine ()
        {
            textbox.Text = textbox.Text.Remove(textbox.Text.LastIndexOf(Environment.NewLine));
        }
    }
}
