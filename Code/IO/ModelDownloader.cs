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
        static string baseUrl = "https://dl.nmkd.de/flowframes/mdl/";

        public static async Task<Dictionary<string, string>> GetFilelist (string ai, string model)
        {
            var client = new WebClient();
            string[] fileLines = client.DownloadString(GetMdlFileUrl(ai, model, "md5.txt")).SplitIntoLines();
            Dictionary<string, string> filesDict = GetDict(fileLines);
            return filesDict;
        }

        static string GetMdlUrl (string ai, string model)
        {
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

        public static async Task DownloadModelFiles (string ai, string model)
        {
            Logger.Log($"DownloadModelFiles(string ai = {ai}, string model = {model})", true);

            try
            {
                string mdlDir = GetLocalPath(ai, model);

                if (AreFilesValid(ai, model))
                    return;

                Logger.Log($"Downloading '{model}' model files...");
                Directory.CreateDirectory(mdlDir);
                await DownloadTo(GetMdlFileUrl(ai, model, "md5.txt"), mdlDir);

                Dictionary<string, string> fileList = await GetFilelist(ai, model);

                foreach (KeyValuePair<string, string> modelFile in fileList)
                {
                    //Logger.Log($"Downloading '{model}' model files: '{modelFile.Key}'...", false, true);
                    await DownloadTo(GetMdlFileUrl(ai, model, modelFile.Key), mdlDir);
                }

                Logger.Log($"Downloaded \"{model}\" model files.", false, true);
            }
            catch (Exception e)
            {
                Logger.Log($"DownloadModelFiles Error: {e.Message}\nStack Trace:\n{e.StackTrace}");
                Interpolate.Cancel($"Error downloading model files: {e.Message}");
            }
        }

        public static bool AreFilesValid (string ai, string model)
        {
            string mdlDir = GetLocalPath(ai, model);

            if (!Directory.Exists(mdlDir))
            {
                Logger.Log($"Files for model {model} not valid: {mdlDir} does not exist.", true);
                return false;
            }

            string md5FilePath = Path.Combine(mdlDir, "md5.txt");

            if (!File.Exists(md5FilePath))
            {
                Logger.Log($"Files for model {model} not valid: {mdlDir} does not exist.", true);
                return false;
            }

            string[] md5Lines = IOUtils.ReadLines(md5FilePath);
            Dictionary<string, string> filesDict = GetDict(md5Lines);

            foreach(KeyValuePair<string, string> file in filesDict)
            {
                string md5 = IOUtils.GetHash(Path.Combine(mdlDir, file.Key), IOUtils.Hash.MD5);
                if (md5.Trim() != file.Value.Trim())
                {
                    Logger.Log($"Files for model {model} not valid: Local MD5 ({md5.Trim()}) does not equal validation MD5 ({file.Value.Trim()}).", true);
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
