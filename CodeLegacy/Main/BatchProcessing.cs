using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.IO;
using Flowframes.Os;
using Flowframes.Ui;
using System;
using System.IO;
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

        public static async Task RunEntry(InterpSettings entry)
        {
            SetBusy(true);
            Program.mainForm.SetWorking(true);

            if (!EntryIsValid(entry))
            {
                Logger.Log("Queue: Skipping entry because it's invalid.");
                Program.batchQueue.Dequeue();
                return;
            }

            var mf = new MediaFile(entry.inPath, false);
            Interpolate.currentMediaFile = mf;
            mf.InputRate = entry.inFps;
            await mf.Initialize();

            Logger.Log($"Queue: Processing {mf.Name} ({entry.interpFactor}x {entry.ai.NameShort}).");

            Program.mainForm.LoadBatchEntry(entry); // Load entry into GUI
            Interpolate.currentSettings = entry;
            Program.mainForm.runBtn_Click(null, null);

            while (Program.busy)
                await Task.Delay(500);

            Program.batchQueue.Dequeue();
            Program.mainForm.SetWorking(false);
            Logger.Log($"Queue: Done processing {mf.Name} ({entry.interpFactor}x {entry.ai.NameShort}).");
        }

        private static void SetBusy(bool state)
        {
            busy = state;
            currentBatchForm?.SetWorking(state);
            Program.mainForm.GetMainTabControl().Enabled = !state;   // Lock GUI
        }

        public static bool EntryIsValid(InterpSettings entry, bool msgBox = false)
        {
            bool Fail(string err)
            {
                Logger.Log($"Queue: Can't process queue entry: {err}");
                if (msgBox) UiUtils.ShowMessageBox(err, UiUtils.MessageType.Error);
                return false;
            }

            if (entry.inPath == null || (IoUtils.IsPathDirectory(entry.inPath) && !Directory.Exists(entry.inPath)) || (!IoUtils.IsPathDirectory(entry.inPath) && !File.Exists(entry.inPath)))
            {
                return Fail("Input path is invalid.");
            }

            if (entry.outPath == null || (!Directory.Exists(entry.outPath) && Config.GetInt("outFolderLoc") != 1))
            {
                return Fail("Output path is invalid.");
            }

            if (IoUtils.GetAmountOfFiles(Path.Combine(Paths.GetPkgPath(), entry.ai.PkgDir), true) < 1)
            {
                return Fail("Selected AI is not available.");
            }

            return true;
        }
    }
}
