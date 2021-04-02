using Flowframes.IO;
using Flowframes.Main;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.Data;

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
            Program.batchQueue.Enqueue(Program.mainForm.GetCurrentSettings());
            RefreshGui();
        }

        public void RefreshGui ()
        {
            taskList.Items.Clear();
            string nl = Environment.NewLine;
            for (int i = 0; i < Program.batchQueue.Count; i++)
            {
                InterpSettings entry = Program.batchQueue.ElementAt(i);
                string niceOutMode = entry.outMode.ToString().ToUpper().Replace("VID", "").Replace("IMG", "");
                string str = $"#{i}: {Path.GetFileName(entry.inPath).Trunc(45)} - {entry.inFps} FPS => {entry.interpFactor}x{nl} {entry.ai.aiNameShort} => {niceOutMode}";
                taskList.Items.Add(str);
            }
        }

        public void SetWorking (bool working)
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

        private void runBtn_Click(object sender, EventArgs e)
        {
            stopBtn.Enabled = true;
            BatchProcessing.Start();
            //WindowState = FormWindowState.Minimized;
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

            for(int i = 0; i < Program.batchQueue.Count; i++)
            {
                if (i != taskList.SelectedIndex)
                    temp.Enqueue(Program.batchQueue.ElementAt(i));
            }

            Program.batchQueue = temp;

            RefreshGui();
        }

        private void taskList_SelectedIndexChanged(object sender, EventArgs e)
        {
            clearSelectedBtn.Enabled = taskList.SelectedItem != null;
        }

        private void taskList_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private async void taskList_DragDrop(object sender, DragEventArgs e)
        {
            string[] droppedPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            await LoadDroppedPaths(droppedPaths);
        }

        public async Task LoadDroppedPaths (string[] droppedPaths)
        {                
            foreach (string path in droppedPaths)
            {
                Logger.Log($"Dropped file: '{path}'", true);
                string frame1 = Path.Combine(path, "00000001.png");
                if (IOUtils.IsPathDirectory(path) && !File.Exists(frame1))
                {
                    InterpolateUtils.ShowMessage($"Can't find frames in this folder:\n\n{frame1} does not exist.", "Error");
                    continue;
                }

                InterpSettings current = Program.mainForm.GetCurrentSettings();
                current.UpdatePaths(path, path.GetParentDir());
                current.inFps = (await GetFramerate(path));
                current.outFps = current.inFps * current.interpFactor;
                Program.batchQueue.Enqueue(current);
                RefreshGui();
                await Task.Delay(100);
            }
        }

        async Task<Fraction> GetFramerate (string path)
        {
            Fraction fps = Interpolate.current.inFps;
            Fraction fpsFromFile = await IOUtils.GetFpsFolderOrVideo(path);
            if (fpsFromFile.GetFloat() > 0)
                return fpsFromFile;

            return fps;
        }
    }
}
