using Flowframes.IO;
using Flowframes.Main;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.Data;
using static Flowframes.Ui.ControlExtensions;

namespace Flowframes.Forms
{
    public partial class BatchForm : Form
    {
        public BatchForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            InitializeComponent();
            BatchProcessing.currentBatchForm = this;
        }

        private void addToQueue_Click(object sender, EventArgs e)
        {
            var entry = Program.mainForm.GetCurrentSettings();

            if (!BatchProcessing.EntryIsValid(entry, msgBox: true))
            {
                BringToFront();
                return;
            }

            Program.batchQueue.Enqueue(Program.mainForm.GetCurrentSettings());
            RefreshGui();
        }

        public void RefreshGui()
        {
            taskList.Items.Clear();

            for (int i = 0; i < Program.batchQueue.Count; i++)
            {
                InterpSettings entry = Program.batchQueue.ElementAt(i);
                string outFormat = Strings.OutputFormat.Get(entry.outSettings.Format.ToString());
                string inPath = string.IsNullOrWhiteSpace(entry.inPath) ? "No Path" : Path.GetFileName(entry.inPath).Trunc(40);
                string str = $"#{i + 1}: {inPath} - {entry.inFps.Float} FPS => {entry.interpFactor}x {entry.ai.NameShort} ({entry.model.Name}) => {outFormat}";
                taskList.Items.Add(str);
            }
        }

        private void RefreshIndex()
        {
            for (int i = 0; i < taskList.Items.Count; i++)
            {
                string[] split = taskList.Items[i].ToString().Split(':');
                split[0] = $"#{i + 1}";
                taskList.Items[i] = string.Join(":", split);
            }
        }

        public void SetWorking(bool working)
        {
            runBtn.Enabled = !working;
            addToQueue.Enabled = !working;
            stopBtn.Visible = working;
            forceStopBtn.Visible = working;
            stopBtn.Enabled = working;
        }

        private void BatchForm_Load(object sender, EventArgs e)
        {
            SetWorking(BatchProcessing.busy);
            RefreshGui();
        }

        private void MoveListItem(int direction)
        {
            if (taskList.SelectedItem == null || taskList.SelectedIndex < 0)
                return;

            int newIndex = taskList.SelectedIndex + direction;

            if (newIndex < 0 || newIndex >= taskList.Items.Count)
                return; // Index out of range - nothing to do

            object selected = taskList.SelectedItem;

            taskList.Items.Remove(selected);
            taskList.Items.Insert(newIndex, selected);
            RefreshIndex();
            taskList.SetSelected(newIndex, true);
        }

        private void runBtn_Click(object sender, EventArgs e)
        {
            stopBtn.Enabled = true;
            BatchProcessing.Start();
            Program.mainForm.WindowState = FormWindowState.Normal;
            Program.mainForm.BringToFront();
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            Program.batchQueue.Clear();
            RefreshGui();
        }

        private void stopBtn_Click(object sender, EventArgs e)
        {
            stopBtn.Enabled = false;
            BatchProcessing.stopped = true;
        }

        private void forceStopBtn_Click(object sender, EventArgs e)
        {
            Interpolate.Cancel("Force stopped by user.");
            BatchProcessing.stopped = true;
        }

        private void BatchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            BatchProcessing.currentBatchForm = null;
        }

        private void clearSelectedBtn_Click(object sender, EventArgs e)
        {
            if (taskList.SelectedItem == null) return;

            Queue<InterpSettings> temp = new Queue<InterpSettings>();

            for (int i = 0; i < Program.batchQueue.Count; i++)
            {
                if (i != taskList.SelectedIndex)
                    temp.Enqueue(Program.batchQueue.ElementAt(i));
            }

            Program.batchQueue = temp;

            RefreshGui();
        }

        private void taskList_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool sel = taskList.SelectedItem != null;
            clearSelectedBtn.Enabled = sel;
            moveUpBtn.Visible = sel;
            moveDownBtn.Visible = sel;
        }

        private void taskList_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private async void taskList_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            await LoadDroppedPaths(droppedPaths);
        }

        public async Task LoadDroppedPaths(string[] droppedPaths, bool start = false)
        {
            if (droppedPaths == null || droppedPaths.Length < 1)
                return;

            var prevOpacity = Program.mainForm.Opacity;
            Program.mainForm.Invoke(() => Program.mainForm.Opacity = 0f);

            try
            {
                foreach (string path in droppedPaths)
                {
                    Logger.Log($"BatchForm: Dropped path: '{path}'", true);

                    InterpSettings current = Program.mainForm.GetCurrentSettings(path);
                    current.inFpsDetected = await IoUtils.GetFpsFolderOrVideo(path);
                    current.inFps = current.inFpsDetected;

                    if (current.inFps.Float <= 0)
                        current.inFps = InterpolateUtils.AskForFramerate(new DirectoryInfo(path).Name, false);

                    current.outFps = current.inFps * current.interpFactor;

                    Program.batchQueue.Enqueue(current);
                    RefreshGui();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"BatchForm: Error while loading dropped paths: {ex.Message}", true);
            }

            Program.mainForm.Invoke(() => Program.mainForm.Opacity = prevOpacity);
            BringToFront();

            if (start)
                runBtn_Click(null, null);
        }

        private void moveUpBtn_Click(object sender, EventArgs e)
        {
            MoveListItem(-1);
        }

        private void moveDownBtn_Click(object sender, EventArgs e)
        {
            MoveListItem(1);
        }
    }
}
