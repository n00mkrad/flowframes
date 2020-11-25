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

        private static string WriteDefaultValIfExists(string key)   // DEFAULTS TO 0, which means key that use 0 as default are not listed here
        {
            if (key == "maxVidHeight") return WriteDefault("maxVidHeight", "2160");
            if (key == "keepTempFolder") return WriteDefault("keepTempFolder", "False");
            if (key == "deleteLogsOnStartup") return WriteDefault("deleteLogsOnStartup", "True");
            if (key == "keepFrames") return WriteDefault("keepFrames", "False");
            if (key == "tempDirCustom") return WriteDefault("tempDirCustom", "C:/");
            // Interpolation
            if (key == "dedupMode") return WriteDefault("dedupMode", "2");
            if (key == "dedupThresh") return WriteDefault("dedupThresh", "2");
            if (key == "enableAudio") return WriteDefault("enableAudio", "True");
            if (key == "enableLoop") return WriteDefault("enableLoop", "False");
            if (key == "autoDedupFrames") return WriteDefault("autoDedupFrames", "15");
            if (key == "vfrDedupe") return WriteDefault("vfrDedupe", "True");
            if (key == "jpegInterps") return WriteDefault("jpegInterps", "False");
            if (key == "timingMode") return WriteDefault("timingMode", "1");
            // Video Export
            if (key == "h264Crf") return WriteDefault("h264Crf", "20");
            if (key == "h265Crf") return WriteDefault("h265Crf", "22");
            if (key == "gifskiQ") return WriteDefault("gifskiQ", "95");
            if (key == "minOutVidLength") return WriteDefault("minOutVidLength", "2");
            // AI
            if (key == "rifeMode") return WriteDefault("rifeMode", ((NvApi.GetVramGb() > 5f) ? 1 : 0).ToString()); // Enable by default if GPU has >5gb VRAM
            if (key == "ncnnThreads") return WriteDefault("ncnnThreads", "2");
            // Debug / Other / Experimental
            if (key == "ffprobeCountFrames") return WriteDefault("ffprobeCountFrames", "False");
            if (key == "ffEncPreset") return WriteDefault("ffEncPreset", "medium");
            // Tile Sizes
            if (key == "tilesize_RIFE_NCNN") return WriteDefault("tilesize_RIFE_NCNN", "2048");
            if (key == "tilesize_DAIN_NCNN") return WriteDefault("tilesize_DAIN_NCNN", "512");
            if (key == "tilesize_CAIN_NCNN") return WriteDefault("tilesize_CAIN_NCNN", "2048");
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
