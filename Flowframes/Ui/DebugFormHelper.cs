using Flowframes.IO;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Ui
{
    class DebugFormHelper
    {
        #region Log Viewer

        public static void FillLogDropdown(ComboBox dd)
        {
            int oldIndex = dd.SelectedIndex;
            dd.Items.Clear();

            FileInfo[] logFiles = IoUtils.GetFileInfosSorted(Paths.GetLogPath(), false, "*.txt");

            foreach (FileInfo file in logFiles)
                dd.Items.Add(file.Name);

            if (oldIndex < 0)
            {
                if (dd.Items.Count > 0)
                    dd.SelectedIndex = 0;

                for (int i = 0; i < dd.Items.Count; i++)
                {
                    if (((string)dd.Items[i]).Split('.').FirstOrDefault() == Logger.defaultLogName)
                        dd.SelectedIndex = i;
                }
            }
            else
            {
                dd.SelectedIndex = oldIndex;
            }
        }

        public static void RefreshLogBox(TextBox logBox, string logFilename)
        {
            //bool wrap = logBox.WordWrap;
            //logBox.WordWrap = true;
            logBox.Text = File.ReadAllText(Path.Combine(Paths.GetLogPath(), logFilename)).Trim('\r', '\n');
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
            //logBox.WordWrap = wrap;
        }

        public static void ToggleMonospace(TextBox logBox)
        {
            bool isMonospace = logBox.Font.Name.Lower().Contains("consolas");

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
            if (grid.Columns.Count < 2)
            {
                grid.Columns.Add("keys", "Key Name");
                grid.Columns.Add("vals", "Saved Value");
            }

            grid.Rows.Clear();

            foreach (KeyValuePair<string, string> keyValuePair in Config.cachedValues)
            {
                grid.Rows.Add(keyValuePair.Key, keyValuePair.Value);
            }

            grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[0].FillWeight = 50;
            grid.Columns[1].FillWeight = 50;
        }

        public static void SaveGrid(DataGridView grid)
        {
            NmkdStopwatch sw = new NmkdStopwatch();
            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (DataGridViewRow row in grid.Rows)
            {
                string key = row.Cells[0].Value?.ToString();
                string val = row.Cells[1].Value?.ToString();

                if (key == null || val == null || string.IsNullOrWhiteSpace(key.Trim()) || string.IsNullOrWhiteSpace(val.Trim()))
                    continue;

                dict.Add(key, val);
            }

            Config.Set(dict);
            Logger.Log($"Config Editor: Saved {grid.Rows.Count} config keys in {sw}", true);
        }

        #endregion
    }
}
