﻿using Flowframes.IO;
using Flowframes.MiscUtils;
using Flowframes.Ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Os
{
    class Python
    {
        static bool hasCheckedSysPy = false;
        static bool sysPyInstalled = false;

        public static string compactOutput;

        public static async Task CheckCompression ()
        {
            // Check if file exist, if not, then go as usual, if yes, the no compression is done.
            string filePath = (Paths.GetDataPath() + "\\FilesHaveBeenCompress.ini");  
            int pythonHasBeenCompressed = 0;
            if (System.IO.File.Exists(filePath))
            {
                 pythonHasBeenCompressed = 1;
            }

            if (HasEmbeddedPyFolder() && (Config.Get(Config.Key.compressedPyVersion) != Updater.GetInstalledVer().ToString()) && pythonHasBeenCompressed == 0)
            {
                Program.mainForm.SetWorking(true, false);
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                try
                {
                    bool shownPatienceMsg = false;
                    Logger.Log("Compressing python runtime. This only needs to be done once.");
                    compactOutput = "";
                    Process compact = OsUtils.NewProcess(true);
                    compact.StartInfo.Arguments = $"/C compact /C /S:{GetPyFolder().Wrap()} /EXE:LZX";
                    compact.OutputDataReceived += new DataReceivedEventHandler(CompactOutputHandler);
                    compact.ErrorDataReceived += new DataReceivedEventHandler(CompactOutputHandler);
                    compact.Start();
                    compact.BeginOutputReadLine();
                    compact.BeginErrorReadLine();

                    while (!compact.HasExited)
                    {
                        await Task.Delay(500);
                        if(sw.ElapsedMilliseconds > 10000)
                        {
                            Logger.Log($"This can take up to a few minutes, but only needs to be done once. (Elapsed: {FormatUtils.Time(sw.Elapsed)})", false, shownPatienceMsg);
                            shownPatienceMsg = true;
                            await Task.Delay(500);
                        }

                    }
                    Config.Set("compressedPyVersion", Updater.GetInstalledVer().ToString());
                    Logger.Log("Done compressing python runtime.");
                    // Create() creates a file at pathName if not existing
                    FileStream fs = File.Create(filePath);
                    Logger.WriteToFile(compactOutput, true, "compact");
                }
                catch { }
                Program.mainForm.SetWorking(false);
            }
        }

        static void CompactOutputHandler (object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine == null || outLine.Data == null)
                return;
            string line = outLine.Data;
            compactOutput = compactOutput + line + "\n";
        }

        public static string GetPyCmd (bool unbufferedStdOut = true, bool quiet = false)
        {
            if (HasEmbeddedPyFolder())
            {
                Logger.Log("Using embedded Python runtime.");
                return Path.Combine(GetPyFolder(), "python.exe").Wrap() + (unbufferedStdOut ? " -u " : "");
            }
            else
            {
                if (IsSysPyInstalled())
                {
                    return "python" + (unbufferedStdOut ? " -u " : "");
                }
                else
                {
                    if (!quiet)
                    {
                        UiUtils.ShowMessageBox("Neither the Flowframes Python Runtime nor System Python installation could be found!\nEither redownload Flowframes with the embedded Python runtime enabled or install Python/Pytorch yourself.");
                        Interpolate.Cancel("Neither the Flowframes Python Runtime nor System Python installation could be found!");
                    }
                }
            }

            return "";
        }

        public static bool HasEmbeddedPyFolder ()
        {
            return (Directory.Exists(GetPyFolder()) && IoUtils.GetDirSize(GetPyFolder(), false) > 1024 * 1024 * 5);
        }

        public static string GetPyFolder ()
        {
            if (Directory.Exists(Path.Combine(Paths.GetPkgPath(), "py-amp")))
                return Path.Combine(Paths.GetPkgPath(), "py-amp");

            if (Directory.Exists(Path.Combine(Paths.GetPkgPath(), "py-tu")))
                return Path.Combine(Paths.GetPkgPath(), "py-tu");

            return "";
        }

        private static bool? pytorchReadyCached = null;
        
        public static bool IsPytorchReady (bool clearCachedValue = false)
        {
            if (clearCachedValue)
                pytorchReadyCached = null;

            if (pytorchReadyCached != null)
                return (bool)pytorchReadyCached;

            bool pytorchReady = false;

            bool hasPyFolder = HasEmbeddedPyFolder();
            string torchVer = GetPytorchVer();

            pytorchReady = hasPyFolder || (!string.IsNullOrWhiteSpace(torchVer) && torchVer.Length <= 35 && !torchVer.Contains("ModuleNotFoundError"));
            pytorchReadyCached = pytorchReady;
            return pytorchReady;
        }

        static string GetPytorchVer()
        {
            try
            {
                Process py = OsUtils.NewProcess(true);
                py.StartInfo.Arguments = "\"/C\" " + GetPyCmd(true, true) + " -c \"import torch; print(torch.__version__)\"";
                Logger.Log($"[DepCheck] CMD: {py.StartInfo.Arguments}", true);
                py.Start();
                py.WaitForExit();
                string output = py.StandardOutput.ReadToEnd();
                string err = py.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
                Logger.Log("[DepCheck] Pytorch Check Output: " + output.Trim(), true);
                return output;
            }
            catch
            {
                return "";
            }
        }

        public static bool IsSysPyInstalled ()
        {
            if (hasCheckedSysPy)
                return sysPyInstalled;

            bool isInstalled = false;

            Logger.Log("Checking if system Python is available...", true);
            string sysPyVer = GetSysPyVersion();

            if (!string.IsNullOrWhiteSpace(sysPyVer) && !sysPyVer.ToLowerInvariant().Contains("not found") && sysPyVer.Length <= 35)
            {
                isInstalled = true;
                Logger.Log("Using Python installation: " + sysPyVer, true);
            }

            hasCheckedSysPy = true;
            sysPyInstalled = isInstalled;
            return sysPyInstalled;
        }

        static string GetSysPyVersion()
        {
            string pythonOut = GetSysPythonOutput();
            Logger.Log("[DepCheck] System Python Check Output: " + pythonOut.Trim(), true);
            try
            {
                string ver = pythonOut.Split('(')[0].Trim();
                Logger.Log("[DepCheck] Sys Python Ver: " + ver, true);
                return ver;
            }
            catch
            {
                return "";
            }
        }

        static string GetSysPythonOutput()
        {
            Process py = OsUtils.NewProcess(true);
            py.StartInfo.Arguments = "/C python -V";
            Logger.Log("[DepCheck] CMD: " + py.StartInfo.Arguments, true);
            py.Start();
            py.WaitForExit();
            string output = py.StandardOutput.ReadToEnd();
            string err = py.StandardError.ReadToEnd();
            return output + "\n" + err;
        }
    }
}
