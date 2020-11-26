using Flowframes.IO;
using Flowframes.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Flowframes.Forms;

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
				new InstallerForm().ShowDialog();
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
					return false;
            }

			return true;
        }
	}
}
