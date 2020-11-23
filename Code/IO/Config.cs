using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Flowframes.IO
{
    internal class Config
    {
        private static string configPath;

        private static string[] cachedLines;

        public static void Init()
        {
            configPath = Path.Combine(Paths.GetDataPath(), "config.ini");
            if (!File.Exists(configPath))
            {
                File.Create(configPath).Close();
            }
            Reload();
        }

        public static void Set(string key, string value)
        {
            string[] lines = File.ReadAllLines(configPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Split('|')[0] == key)
                {
                    lines[i] = key + "|" + value;
                    File.WriteAllLines(configPath, lines);
                    cachedLines = lines;
                    return;
                }
            }
            List<string> list = lines.ToList();
            list.Add(key + "|" + value);
            File.WriteAllLines(configPath, list.ToArray());
            cachedLines = list.ToArray();
        }

        public static string Get(string key)
        {
            try
            {
                for (int i = 0; i < cachedLines.Length; i++)
                {
                    string[] keyValuePair = cachedLines[i].Split('|');
                    if (keyValuePair[0] == key)
                        return keyValuePair[1];
                }
                return WriteDefaultValIfExists(key);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to get {key.Wrap()} from config! {e.Message}");
            }
            return null;
        }

        public static bool GetBool(string key)
        {
            return bool.Parse(Get(key));
        }

        public static int GetInt(string key)
        {
            return int.Parse(Get(key));
        }

        public static float GetFloat(string key)
        {
            return float.Parse(Get(key));
        }

        private static string WriteDefaultValIfExists(string key)
        {
            if (key == "dedupMode") return WriteDefault("dedupMode", "2");
            if (key == "dedupThresh") return WriteDefault("dedupThresh", "2");
            if (key == "keepFrames") return WriteDefault("keepFrames", "False");
            if (key == "enableAudio") return WriteDefault("enableAudio", "True");
            if (key == "logProcessOutput") return WriteDefault("logProcessOutput", "False");
            if (key == "cmdDebugMode") return WriteDefault("cmdDebugMode", "0");
            if (key == "enableLoop") return WriteDefault("enableLoop", "False");
            if (key == "ncnnGpus") return WriteDefault("ncnnGpus", "0");
            if (key == "torchGpus") return WriteDefault("torchGpus", "0");
            if (key == "keepTempFolder") return WriteDefault("keepTempFolder", "False");
            if (key == "deleteLogsOnStartup") return WriteDefault("deleteLogsOnStartup", "True");
            if (key == "autoDedupFrames") return WriteDefault("autoDedupFrames", "10");
            if (key == "minOutVidLength") return WriteDefault("minOutVidLength", "2");
            if (key == "mp4Enc") return WriteDefault("mp4Enc", "0");
            if (key == "h264Crf") return WriteDefault("h264Crf", "20");
            if (key == "h265Crf") return WriteDefault("h265Crf", "22");
            if (key == "gifskiQ") return WriteDefault("gifskiQ", "95");
            if (key == "maxFps") return WriteDefault("maxFps", "0");
            if (key == "maxFpsMode") return WriteDefault("maxFpsMode", "0");
            if (key == "jpegInterps") return WriteDefault("jpegInterps", "False");
            if (key == "rifeMode") return WriteDefault("rifeMode", ((NvApi.GetVramGb() > 5f) ? 1 : 0).ToString()); // Enable by default if GPU has >5gb VRAM
            if (key == "maxVidHeight") return WriteDefault("maxVidHeight", "2160");
            if (key == "timingMode") return WriteDefault("timingMode", "1");
            if (key == "tempDirCustom") return WriteDefault("tempDirCustom", "C:/");
            if (key == "ffprobeCountFrames") return WriteDefault("ffprobeCountFrames", "False");
            return WriteDefault(key, "0");
        }

        private static string WriteDefault(string key, string def)
        {
            Set(key, def);
            return def;
        }

        private static void Reload()
        {
            cachedLines = File.ReadAllLines(configPath);
        }
    }
}
