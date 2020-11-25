using Flowframes.Data;
using Flowframes.Forms;
using Flowframes.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.IO
{
    class PkgInstaller
    {
        public static List<FlowPackage> packages = new List<FlowPackage>();
        public static InstallerForm installerForm;

        static string path7za = "";

        public static void Init()
        {
            packages.Clear();
            packages.Add(Packages.dainNcnn);
            packages.Add(Packages.cainNcnn);
            packages.Add(Packages.rifeCuda);
            packages.Add(Packages.audioVideo);
            packages.Add(Packages.python);
            packages.Add(Packages.licenses);
        }

        static Stopwatch sw = new Stopwatch();
        public static async Task DownloadAndInstall(string filename, bool showDialog = true)
        {
            string savePath = Path.Combine(Paths.GetPkgPath(), filename);
            string url = $"https://dl.nmkd.de/flowframes/pkgs/{filename}";
            Logger.Log($"[PkgInstaller] Downloading {url}", true);
            var client = new WebClient();
            //client.Proxy = WebRequest.DefaultWebProxy;
            Print($"Downloading {filename}...");
            sw.Restart();
            client.DownloadProgressChanged += (sender, args) =>
            {
                if (sw.ElapsedMilliseconds > 200)
                {
                    sw.Restart();
                    Print($"Downloading {filename}... {args.ProgressPercentage}%", true);
                }
            };
            client.DownloadFileCompleted += (sender, args) =>
            {
                Print($"Downloading {filename}... 100%", true);
            };
            await client.DownloadFileTaskAsync(new Uri(url), savePath);
            try
            {
                if (Path.GetExtension(filename).ToLower() == ".7z")     // Only run extractor if it's a 7z archive
                {
                    Print($"Installing {filename}...");
                    await UnSevenzip(Path.Combine(Paths.GetPkgPath(), filename));
                }
                Print("Done installing.");
                installerForm.Refresh();
            }
            catch (Exception e)
            {
                Print("Failed to install package: " + e.Message);
                Logger.Log($"Failed to uninstall package {Path.GetFileNameWithoutExtension(filename)}: {e.Message}");
            }
        }

        static async Task UnSevenzip(string path)
        {
            path7za = Path.Combine(Paths.GetDataPath(), "7za.exe");
            if (!File.Exists(path7za))
                File.WriteAllBytes(path7za, Resources.x64_7za);
            Logger.Log("[PkgInstaller] Extracting " + path, true);
            await Task.Delay(20);
            SevenZipNET.SevenZipExtractor.Path7za = path7za;
            SevenZipNET.SevenZipExtractor extractor = new SevenZipNET.SevenZipExtractor(path);
            extractor.ExtractAll(Paths.GetPkgPath(), true, true);
            File.Delete(path);
            await Task.Delay(10);
        }

        public static void Uninstall(string filename)
        {
            try
            {
                string pkgName = Path.GetFileNameWithoutExtension(filename);
                Logger.Log("[PkgInstaller] Uninstalling " + pkgName, true);
                Print("Uninstalling " + pkgName);
                Directory.Delete(Path.Combine(Paths.GetPkgPath(), pkgName), true);
                Print("Done uninstalling.");
            }
            catch (Exception e)
            {
                Print("Failed to uninstall package!");
                Logger.Log($"Failed to uninstall package {Path.GetFileNameWithoutExtension(filename)}: {e.Message}");
            }
        }

        static void Print(string s, bool replaceLastLine = false)
        {
            if (installerForm != null)
                installerForm.Print(s, replaceLastLine);
        }
    }
}
