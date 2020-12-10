using Flowframes.IO;
using Flowframes.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Flowframes.Forms;
using Flowframes.Main;

namespace Flowframes
{
    class Setup
    {

		public static void Init()
		{
			Console.WriteLine("Setup Init()");
			if (!InstallIsValid())
			{
				Logger.Log("No valid installation detected");
				InterpolateUtils.ShowMessage($"Some packages are missing!\n\nCheck the log ({Path.GetFileName(Paths.GetDataPath())}/{Path.GetFileName(Paths.GetLogPath())}/{Logger.defaultLogName}).", "Error");
				Application.Exit();
				//new InstallerForm().ShowDialog();
			}
            else
            {
				
			}
		}


		public static bool InstallIsValid ()
        {
			if (!Directory.Exists(Paths.GetPkgPath()))
            {
				Logger.Log("Install invalid - Reason: " + Paths.GetPkgPath() + " does not exist.", true);
				return false;
			}

			foreach(FlowPackage pkg in PkgInstaller.packages)
            {
				// if pkg is required and not installed, return false
				if (pkg.friendlyName.ToLower().Contains("required") && !PkgUtils.IsInstalled(pkg))
                {
					Logger.Log($"Required packages \"{pkg.friendlyName}\" was not found!", true);
					return false;
				}
            }

			return true;
        }
	}
}
