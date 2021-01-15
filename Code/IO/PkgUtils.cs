using Flowframes.Data;
using Flowframes.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Flowframes.IO
{
    class PkgUtils
    {

        public static string GetPkgFolder (FlowPackage pkg)
        {
            return Path.Combine(Paths.GetPkgPath(), Path.GetFileNameWithoutExtension(pkg.fileName));
        }

        public static bool IsInstalled(FlowPackage pkg)
        {
            string path = GetPkgFolder(pkg);
            return (Directory.Exists(path) && IOUtils.GetAmountOfFiles(path, true) > 0);
        }

        public static bool IsUpToDate(FlowPackage pkg, int minVersion)
        {
            return (GetVersion(pkg) >= minVersion);
        }

        public static int GetVersion (FlowPackage pkg)
        {
            string versionFilePath = Path.Combine(GetPkgFolder(pkg), "ver.ini");
            if (!File.Exists(versionFilePath))
                return 0;
            return IOUtils.ReadLines(versionFilePath)[0].Split('#')[0].GetInt();
        }

        public static bool IsAiAvailable(AI ai, bool msg = true)
        {
            Logger.Log("PkgInstaller.IsAiAvailable - Checking for AI " + ai.aiName, true);
            return IsInstalled(ai.pkg);
        }
    }
}
