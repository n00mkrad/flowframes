using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Os;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Main
{
    class BatchProcessing
    {
        public static bool stopped = false;

        public static BatchForm currentBatchForm;
        public static bool busy = false;

        public static async void Start()
        {
            if (busy)
            {
                Logger.Log("Queue: Start() has been called, but I'm already busy - Returning!", true);
                return;
            }

            SetBusy(true);

            if (Config.GetBool(Config.Key.clearLogOnInput))
                Logger.ClearLogBox();

            stopped = false;
            Program.mainForm.SetTab(Program.mainForm.previewTab.Name);
            int initTaskCount = Program.batchQueue.Count;

            for (int i = 0; i < initTaskCount; i++)
            {
                if (!stopped && Program.batchQueue.Count > 0)
                {
                    try
                    {
                        Logger.Log($"Queue: Running queue task {i + 1}/{initTaskCount}, {Program.batchQueue.Count} tasks left.");
                        await RunEntry(Program.batchQueue.Peek());

                        if (currentBatchForm != null)
                            currentBatchForm.RefreshGui();
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Failed to run batch queue entry. If this happened after force stopping the queue, it's non-critical. {e.Message}", true);
                    }
                }

                await Task.Delay(500);
            }

            Logger.Log("Queue: Finished queue processing.");
            OsUtils.ShowNotificationIfInBackground("Flowframes Queue", "Finished queue processing.");
            SetBusy(false);
            Program.mainForm.SetWorking(false);
            Program.mainForm.SetTab(Program.mainForm.interpOptsTab.Name);
            Program.mainForm.CompletionAction();
        }

        public static void Stop()
        {
            stopped = true;
        }

        static async Task RunEntry(InterpSettings entry)
        {
            SetBusy(true);
            Program.mainForm.SetWorking(true);

            if (!EntryIsValid(entry))
            {
                Logger.Log("Queue: Skipping entry because it's invalid.");
                Program.batchQueue.Dequeue();
                return;
            }

            MediaFile mf = new MediaFile(entry.inPath, false);
            mf.InputRate = entry.inFps;
            await mf.Initialize();
            Interpolate.currentMediaFile = mf;

            Logger.Log($"Queue: Processing {mf.Name} ({entry.interpFactor}x {entry.ai.NameShort}).");

            Program.mainForm.LoadBatchEntry(entry);     // Load entry into GUI
            Interpolate.currentSettings = entry;
            Program.mainForm.runBtn_Click(null, null);

            while (Program.busy)
                await Task.Delay(500);

            Program.batchQueue.Dequeue();
            Program.mainForm.SetWorking(false);
            Logger.Log($"Queue: Done processing {mf.Name} ({entry.interpFactor}x {entry.ai.NameShort}).");
        }

        static void SetBusy(bool state)
        {
            busy = state;

            if (currentBatchForm != null)
                currentBatchForm.SetWorking(state);

            Program.mainForm.GetMainTabControl().Enabled = !state;   // Lock GUI
        }

        static bool EntryIsValid(InterpSettings entry)
        {

            if (entry.inPath == null || (IoUtils.IsPathDirectory(entry.inPath) && !Directory.Exists(entry.inPath)) || (!IoUtils.IsPathDirectory(entry.inPath) && !File.Exists(entry.inPath)))
            {
                Logger.Log("Queue: Can't process queue entry: Input path is invalid.");
                return false;
            }

            if (entry.outPath == null || (!Directory.Exists(entry.outPath) && Config.GetInt("outFolderLoc") != 1))
            {
                Logger.Log("Queue: Can't process queue entry: Output path is invalid.");
                return false;
            }

            if (IoUtils.GetAmountOfFiles(Path.Combine(Paths.GetPkgPath(), entry.ai.PkgDir), true) < 1)
            {
                Logger.Log("Queue: Can't process queue entry: Selected AI is not available.");
                return false;
            }

            return true;
        }
    }
}
