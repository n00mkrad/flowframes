using Flowframes.IO;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class DebugForm : Form
    {
        public bool configGridChanged;

        public DebugForm()
        {
            InitializeComponent();
        }

        private void DebugForm_Shown(object sender, EventArgs e)
        {
            configDataGrid.Font = new Font("Consolas", 9f);
            RefreshLogs();
        }

        void RefreshLogs ()
        {
            DebugFormHelper.FillLogDropdown(logFilesDropdown);
        }

        private void DebugForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!configGridChanged)
                return;

            DialogResult dialogResult = UiUtils.ShowMessageBox($"Save the modified configuration file?", "Save Configuration?", MessageBoxButtons.YesNo);


            if (dialogResult == DialogResult.Yes)
                DebugFormHelper.SaveGrid(configDataGrid);
        }

        private void configDataGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1)
                configGridChanged = true;
        }

        private void configDataGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            configGridChanged = true;
        }

        private void configDataGrid_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            configGridChanged = true;
        }

        private void logFilesDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            DebugFormHelper.RefreshLogBox(logBox, logFilesDropdown.Text);
        }

        private void textWrapBtn_Click(object sender, EventArgs e)
        {
            logBox.WordWrap = !logBox.WordWrap;
        }

        private void openLogFolderBtn_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Paths.GetLogPath());
        }

        private void clearLogsBtn_Click(object sender, EventArgs e)
        {
            foreach (string str in logFilesDropdown.Items)
                File.WriteAllText(Path.Combine(Paths.GetLogPath(), str), "");

            logFilesDropdown_SelectedIndexChanged(null, null);
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            RefreshLogs();
        }

        private void monospaceBtn_Click(object sender, EventArgs e)
        {
            DebugFormHelper.ToggleMonospace(logBox);
        }

        private void copyTextClipboardBtn_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(logBox.Text);
        }

        private void tabPage2_Enter(object sender, EventArgs e)
        {
            DebugFormHelper.LoadGrid(configDataGrid);
            configGridChanged = false;
        }
    }
}
