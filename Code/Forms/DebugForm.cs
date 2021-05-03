using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class DebugForm : Form
    {
        public DebugForm()
        {
            InitializeComponent();
        }

        private void DebugForm_Load(object sender, EventArgs e)
        {
            Dictionary<string, string> configDict = new Dictionary<string, string>();

            configDataGrid.Columns.Add("keys", "Key Name");
            configDataGrid.Columns.Add("vals", "Saved Value");

            foreach (string entry in Config.cachedLines)
            {
                string[] data = entry.Split('|');
                configDict.Add(data[0], data[1]);
                configDataGrid.Rows.Add(data[0], data[1]);
            }

            configDataGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            configDataGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        void Save ()
        {
            foreach(DataGridViewRow row in configDataGrid.Rows)
            {
                string key = row.Cells[0].Value?.ToString();
                string val = row.Cells[1].Value?.ToString();

                if (key == null || val == null || string.IsNullOrWhiteSpace(key.Trim()) || string.IsNullOrWhiteSpace(val.Trim()))
                    continue;

                Config.Set(key, val);
                Logger.Log($"Config Editor: Saved Key '{key}' with value '{val}'", true);
            }
        }

        private void DebugForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Save();
        }
    }
}
