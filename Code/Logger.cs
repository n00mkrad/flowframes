using Flowframes.IO;
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

        public static void Log(string s, bool hidden = false, bool replaceLastLine = false, string filename = "")
        {
            if (s == null)
                return;

            Console.WriteLine(s);

            try
            {
                if (replaceLastLine)
                    textbox.Text = textbox.Text.Remove(textbox.Text.LastIndexOf(Environment.NewLine));
            }
            catch { }

            s = s.Replace("\n", Environment.NewLine);

            if (!hidden && textbox != null)
                textbox.AppendText(Environment.NewLine + s);

            LogToFile(s, false, filename);
        }

        public static void LogToFile(string s, bool noLineBreak, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = "sessionlog.txt";
            
            file = Path.Combine(Paths.GetLogPath(), filename);

            string time = DT.Now.Month + "-" + DT.Now.Day + "-" + DT.Now.Year + " " + DT.Now.Hour + ":" + DT.Now.Minute + ":" + DT.Now.Second;

            try
            {
                if (!noLineBreak)
                    File.AppendAllText(file, Environment.NewLine + time + ": " + s);
                else
                    File.AppendAllText(file, " " + s);
            }
            catch
            {
                // this if fine, i forgot why
            }
        }
    }
}
