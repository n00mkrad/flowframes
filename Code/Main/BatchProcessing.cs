using Flowframes.Forms;
using Flowframes.IO;
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
            if (Config.GetBool("clearLogOnInput"))
                Logger.ClearLogBox();

            stopped = false;
            Program.mainForm.SetTab("preview");
            int initTaskCount = Program.batchQueue.Count;

            for (int i = 0; i < initTaskCount; i++)
            {
                if (!stopped && Program.batchQueue.Count > 0)
                {
                    Logger.Log($"[Queue] Running queue task {i + 1}/{initTaskCount}, {Program.batchQueue.Count} tasks left.");
                    await RunEntry(Program.batchQueue.Peek());
                    if (currentBatchForm != null)
                        currentBatchForm.RefreshGui();
                }
                await Task.Delay(1000);
            }
            Logger.Log("[Queue] Finished queue processing.");
            SetBusy(false);
            Program.mainForm.SetTab("interpolation");
        }

        public static void Stop()
        {
            stopped = true;
        }

        static async Task RunEntry(InterpSettings entry)
        {
            if (!EntryIsValid(entry))
            {
                Logger.Log("[Queue] Skipping entry because it's invalid.");
                Program.batchQueue.Dequeue();
                return;
            }

            string fname = Path.GetFileName(entry.inPath);
            if (IOUtils.IsPathDirectory(entry.inPath)) fname = Path.GetDirectoryName(entry.inPath);
            Logger.Log($"[Queue] Processing {fname} ({entry.interpFactor}x {entry.ai.aiNameShort}).");

            SetBusy(true);
            Program.mainForm.LoadBatchEntry(entry);     // Load entry into GUI
            Interpolate.current = entry;
            Program.mainForm.runBtn_Click(null, null);

            await Task.Delay(2000);
            while (Program.busy)
                await Task.Delay(1000);

            SetBusy(false);

            Program.batchQueue.Dequeue();
            Logger.Log($"[Queue] Done processing {fname} ({entry.interpFactor}x {entry.ai.aiNameShort}).");
        }

        static void SetBusy(bool state)
        {
            busy = state;
            if (currentBatchForm != null)
                currentBatchForm.SetWorking(state);
            Program.mainForm.SetWorking(state);
            Program.mainForm.GetMainTabControl().Enabled = !state;   // Lock GUI
        }

        static bool EntryIsValid(InterpSettings entry)
        {

            if (entry.inPath == null || (IOUtils.IsPathDirectory(entry.inPath) && !Directory.Exists(entry.inPath)) || (!IOUtils.IsPathDirectory(entry.inPath) && !File.Exists(entry.inPath)))
            {
                Logger.Log("[Queue] Can't process queue entry: Input path is invalid.");
                return false;
            }

            if (entry.outPath == null || !Directory.Exists(entry.outPath))
            {
                Logger.Log("[Queue] Can't process queue entry: Output path is invalid.");
                return false;
            }

            if (IOUtils.GetAmountOfFiles(Path.Combine(Paths.GetPkgPath(), entry.ai.pkgDir), true) < 1)
            {
                Logger.Log("[Queue] Can't process queue entry: Selected AI is not available.");
                return false;
            }

            return true;
        }
    }
}
