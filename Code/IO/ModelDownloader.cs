using Flowframes.Data;
using Flowframes.Main;
using Flowframes.MiscUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.IO
{
    class ModelDownloader
    {

        public static async Task<Dictionary<string, string>> GetFilelist (string ai, string model)
        {
            var client = new WebClient();
            string[] fileLines = client.DownloadString(GetMdlFileUrl(ai, model, "md5.txt")).SplitIntoLines();
            Dictionary<string, string> filesDict = GetDict(fileLines);
            return filesDict;
        }

        static string GetMdlUrl (string ai, string model)
        {
            string baseUrl = Config.Get(Config.Key.mdlBaseUrl);
            return Path.Combine(baseUrl, ai.ToLower(), model);
        }

        static string GetMdlFileUrl(string ai, string model, string file)
        {
            return Path.Combine(GetMdlUrl(ai, model), file);
        }

        static string GetLocalPath(string ai, string model)
        {
            return Path.Combine(Paths.GetPkgPath(), ai, model);
        }

        static async Task DownloadTo (string url, string saveDir, int retries = 3)
        {
            string savePath = Path.Combine(saveDir, Path.GetFileName(url));
            IOUtils.TryDeleteIfExists(savePath);
            Logger.Log($"Downloading '{url}' to '{savePath}'", true);
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            bool completed = false;
            int lastProgPercentage = -1;
            var client = new WebClient();
            client.DownloadProgressChanged += (sender, args) =>
            {
                if (sw.ElapsedMilliseconds > 200 && args.ProgressPercentage != lastProgPercentage)
                {
                    sw.Restart();
                    lastProgPercentage = args.ProgressPercentage;
                    Logger.Log($"Downloading model file '{Path.GetFileName(url)}'... {args.ProgressPercentage}%", false, true);
                }
            };
            client.DownloadFileCompleted += (sender, args) =>
            {
                if (args.Error != null)
                    Logger.Log("Download failed: " + args.Error.Message);
                completed = true;
            };
            client.DownloadFileTaskAsync(url, savePath).ConfigureAwait(false);
            while (!completed)
            {
                if (Interpolate.canceled)
                {
                    client.CancelAsync();
                    client.Dispose();
                    return;
                }
                if (sw.ElapsedMilliseconds > 6000)
                {
                    client.CancelAsync();
                    if(retries > 0)
                    {
                        await DownloadTo(url, saveDir, retries--);
                    }
                    else
                    {
                        Interpolate.Cancel("Model download failed.");
                        return;
                    }
                }
                await Task.Delay(500);
            }
            Logger.Log($"Downloaded '{Path.GetFileName(url)}' ({IOUtils.GetFilesize(savePath) / 1024} KB)", true);
        }

        public static async Task DownloadModelFiles (AI ai, string modelDir)
        {
            string aiDir = ai.pkgDir;
            Logger.Log($"DownloadModelFiles(string ai = {ai}, string model = {modelDir})", true);

            try
            {
                string mdlDir = GetLocalPath(aiDir, modelDir);

                if (AreFilesValid(aiDir, modelDir))
                    return;

                Logger.Log($"Downloading '{modelDir}' model files...");
                Directory.CreateDirectory(mdlDir);
                await DownloadTo(GetMdlFileUrl(aiDir, modelDir, "md5.txt"), mdlDir);
                Dictionary<string, string> fileList = await GetFilelist(aiDir, modelDir);

                foreach (KeyValuePair<string, string> modelFile in fileList)
                    await DownloadTo(GetMdlFileUrl(aiDir, modelDir, modelFile.Key), mdlDir);
 
                Logger.Log($"Downloaded \"{modelDir}\" model files.", false, true);
            }
            catch (Exception e)
            {
                Logger.Log($"DownloadModelFiles Error: {e.Message}\nStack Trace:\n{e.StackTrace}");
                Interpolate.Cancel($"Error downloading model files: {e.Message}");
            }
        }

        public static void DeleteAllModels ()
        {
            foreach(string modelFolder in GetAllModelFolders())
            {
                string size = FormatUtils.Bytes(IOUtils.GetDirSize(modelFolder, true));
                if (IOUtils.TryDeleteIfExists(modelFolder))
                    Logger.Log($"Deleted cached model '{Path.GetFileName(modelFolder.GetParentDir())}/{Path.GetFileName(modelFolder)}' ({size})");
            }
        }

        public static List<string> GetAllModelFolders()
        {
            List<string> modelPaths = new List<string>();

            foreach (AI ai in Networks.networks)
            {
                string aiPkgFolder = Path.Combine(Paths.GetPkgPath(), ai.pkgDir);
                ModelCollection aiModels = AiModels.GetModels(ai);

                foreach(ModelCollection.ModelInfo model in aiModels.models)
                {
                    string mdlFolder = Path.Combine(aiPkgFolder, model.dir);

                    if (Directory.Exists(mdlFolder))
                        modelPaths.Add(mdlFolder);
                }
            }

            return modelPaths;
        }

        public static bool AreFilesValid (string ai, string model)
        {
            string mdlDir = GetLocalPath(ai, model);

            if (!Directory.Exists(mdlDir))
            {
                Logger.Log($"Files for model {model} not valid: {mdlDir} does not exist.", true);
                return false;
            }

            if (Debugger.IsAttached)    // Disable MD5 check in dev environment
                return true;

            string md5FilePath = Path.Combine(mdlDir, "md5.txt");

            if (!File.Exists(md5FilePath) || IOUtils.GetFilesize(md5FilePath) < 32)
            {
                Logger.Log($"Files for model {model} not valid: {mdlDir} does not exist or is incomplete.", true);
                return false;
            }

            string[] md5Lines = IOUtils.ReadLines(md5FilePath);
            Dictionary<string, string> filesDict = GetDict(md5Lines);

            foreach(KeyValuePair<string, string> file in filesDict)
            {
                string md5 = IOUtils.GetHash(Path.Combine(mdlDir, file.Key), IOUtils.Hash.MD5);
                if (md5.Trim() != file.Value.Trim())
                {
                    Logger.Log($"Files for model {model} not valid: MD5 of {file.Key} ({md5.Trim()}) does not equal validation MD5 ({file.Value.Trim()}).", true);
                    return false;
                }
            }

            return true;
        }

        static Dictionary<string, string> GetDict (string[] lines, char sep = ':')
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (string line in lines)
            {
                if (line.Length < 3) continue;
                string[] keyValuePair = line.Split(':');
                dict.Add(keyValuePair[0], keyValuePair[1]);
            }

            return dict;
        }
    }
}
