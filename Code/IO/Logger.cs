using Flowframes.IO;
using Flowframes.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public static void Log(string s, bool hidden = false, bool replaceLastLine = false, string filename = "")
        {
            if (string.IsNullOrWhiteSpace(s))
                return;

            Console.WriteLine(s);

            try
            {
                if (replaceLastLine)
                {
                    textbox.Suspend();
                    textbox.Text = textbox.Text.Remove(textbox.Text.LastIndexOf(Environment.NewLine));
                }
            }
            catch { }

            s = s.Replace("\n", Environment.NewLine);

            if (!hidden && textbox != null)
                textbox.AppendText((textbox.Text.Length > 1 ? Environment.NewLine : "") + s);

            textbox.Resume();

            LogToFile(s, false, filename);
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
