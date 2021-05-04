using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.UI
{
    class DebugFormHelper
    {
        #region Log Viewer

        public static void FillLogDropdown(ComboBox dd)
        {
            dd.Items.Clear();

            FileInfo[] logFiles = IOUtils.GetFileInfosSorted(Paths.GetLogPath(), false, "*.txt");

            foreach (FileInfo file in logFiles)
                dd.Items.Add(file.Name);
        }

        public static void RefreshLogBox(TextBox logBox, string logFilename)
        {
            bool wrap = logBox.WordWrap;
            logBox.WordWrap = true;
            logBox.Text = File.ReadAllText(Path.Combine(Paths.GetLogPath(), logFilename)).Trim('\r', '\n');
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
            logBox.WordWrap = wrap;
        }

        public static void ToggleMonospace(TextBox logBox)
        {
            bool isMonospace = logBox.Font.Name.ToLower().Contains("consolas");

            if (isMonospace)
                logBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5f);
            else
                logBox.Font = new System.Drawing.Font("Consolas", 8.0f);
        }

        public static void CopyLogToClipboard(string logFilename)
        {
            StringCollection paths = new StringCollection();
            string path = Path.Combine(Paths.GetLogPath(), logFilename);
            paths.Add(path);
            Clipboard.SetFileDropList(paths);
        }

        #endregion

        #region Config Editor Grid

        public static void LoadGrid(DataGridView grid)
        {
            Dictionary<string, string> configDict = new Dictionary<string, string>();

            if (grid.Columns.Count < 2)
            {
                grid.Columns.Add("keys", "Key Name");
                grid.Columns.Add("vals", "Saved Value");
            }

            grid.Rows.Clear();

            foreach (string entry in Config.cachedLines)
            {
                string[] data = entry.Split('|');
                configDict.Add(data[0], data[1]);
                grid.Rows.Add(data[0], data[1]);
            }

            grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[0].FillWeight = 50;
            grid.Columns[1].FillWeight = 50;
        }

        public static void SaveGrid(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                string key = row.Cells[0].Value?.ToString();
                string val = row.Cells[1].Value?.ToString();

                if (key == null || val == null || string.IsNullOrWhiteSpace(key.Trim()) || string.IsNullOrWhiteSpace(val.Trim()))
                    continue;

                Config.Set(key, val);
                Logger.Log($"Config Editor: Saved Key '{key}' with value '{val}'", true);
            }
        }

        #endregion
    }
}
