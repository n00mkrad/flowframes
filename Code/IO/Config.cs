using Flowframes.OS;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public static string Get(string key, Type type = Type.String)
        {
            try
            {
                for (int i = 0; i < cachedLines.Length; i++)
                {
                    string[] keyValuePair = cachedLines[i].Split('|');
                    if (keyValuePair[0] == key && !string.IsNullOrWhiteSpace(keyValuePair[1]))
                    {
                        return keyValuePair[1];
                    }
                }
                return WriteDefaultValIfExists(key, type);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to get {key.Wrap()} from config! {e.Message}");
            }
            return null;
        }

        public static bool GetBool(string key)
        {
            return bool.Parse(Get(key, Type.Bool));
        }

        public static int GetInt(string key)
        {
            return Get(key, Type.Int).GetInt();
        }

        public static int GetInt(string key, int defaultVal)
        {
            WriteIfDoesntExist(key, defaultVal.ToString());
            return GetInt(key);
        }

        public static float GetFloat(string key)
        {
            return float.Parse(Get(key, Type.Float), CultureInfo.InvariantCulture);
        }

        public static string GetFloatString (string key)
        {
            return Get(key, Type.Float).Replace(",", ".");
        }

        static void WriteIfDoesntExist (string key, string val)
        {
            foreach (string line in cachedLines)
                if (line.Contains(key + "|"))
                    return;
            Set(key, val);
        }

        public enum Type { String, Int, Float, Bool }
        private static string WriteDefaultValIfExists(string key, Type type)
        {
            if (key == "maxVidHeight")      return WriteDefault(key, "2160");
            if (key == "delLogsOnStartup")  return WriteDefault(key, "True");
            if (key == "tempDirCustom")     return WriteDefault(key, "C:/");
            // Interpolation
            if (key == "dedupMode")         return WriteDefault(key, "2");
            if (key == "dedupThresh")       return WriteDefault(key, "2");
            if (key == "enableAudio")       return WriteDefault(key, "True");
            if (key == "autoDedupFrames")   return WriteDefault(key, "100");
            if (key == "vfrDedupe")         return WriteDefault(key, "True");
            if (key == "timingMode")        return WriteDefault(key, "1");
            if (key == "scnDetectValue")    return WriteDefault(key, "0.2");
            if (key == "autoEncMode")       return WriteDefault(key, "2");
            // Video Export
            if (key == "h264Crf")       return WriteDefault(key, "20");
            if (key == "h265Crf")       return WriteDefault(key, "24");
            if (key == "vp9Crf")        return WriteDefault(key, "28");
            if (key == "proResProfile") return WriteDefault(key, "2");
            if (key == "gifColors")     return WriteDefault(key, "128 (High)");
            if (key == "minVidLength")  return WriteDefault(key, "2");
            // AI
            if (key == "uhdThresh") return WriteDefault(key, "1440");
            if (key == "ncnnThreads")   return WriteDefault(key, "1");
            // Debug / Other / Experimental
            if (key == "ffEncPreset")           return WriteDefault(key, "medium");
            if (key == "ffEncArgs")             return WriteDefault(key, "");
            // Tile Sizes
            if (key == "tilesize_RIFE_NCNN")    return WriteDefault(key, "2048");
            if (key == "tilesize_DAIN_NCNN")    return WriteDefault(key, "512");
            if (key == "tilesize_CAIN_NCNN")    return WriteDefault(key, "2048");

            if (type == Type.Int || type == Type.Float) return WriteDefault(key, "0");     // Write default int/float (0)
            if (type == Type.Bool)                      return WriteDefault(key, "False");     // Write default bool (False)
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
