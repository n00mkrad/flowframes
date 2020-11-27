using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Flowframes.OS
{
    class Pytorch
    {
        static bool hasCheckedSysPy = false;
        static bool sysPyInstalled = false;

        public static string GetPyCmd ()
        {
            if (PkgUtils.IsInstalled(Packages.python))
            {
                Logger.Log("Using embedded Python runtime.");
                string pyPkgDir = Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(Packages.python.fileName));
                return Path.Combine(pyPkgDir, "python.exe").Wrap();
            }
            else
            {
                if (IsSysPyInstalled())
                {
                    return "python";
                }
                else
                {
                    MessageBox.Show("System python installation not found!\nPlease install Python or download the package from the package installer.");
                    Interpolate.Cancel("Neither the Flowframes Python Runtime nor System Python installation could be found!");
                }
            }
            return "";
        }

        public static bool IsPytorchReady ()
        {
            string torchVer = GetPytorchVer();
            if (!string.IsNullOrWhiteSpace(torchVer) && torchVer.Length <= 35)
                return true;
            else
                return false;
        }

        static string GetPytorchVer()
        {
            try
            {
                Process py = OSUtils.NewProcess(true);
                py.StartInfo.Arguments = "\"/C\" " + GetPyCmd() + " -c \"import torch; print(torch.__version__)\"";
                Logger.Log("[DepCheck] CMD: " + py.StartInfo.Arguments);
                py.Start();
                py.WaitForExit();
                string output = py.StandardOutput.ReadToEnd();
                string err = py.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
                Logger.Log("[DepCheck] Pytorch Check Output: " + output.Trim());
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

            Logger.Log("Checking if system Python is available...");
            string sysPyVer = GetSysPyVersion();

            if (!string.IsNullOrWhiteSpace(sysPyVer) && !sysPyVer.ToLower().Contains("not found") && sysPyVer.Length <= 35)
                isInstalled = true;

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
            Process py = OSUtils.NewProcess(true);
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
