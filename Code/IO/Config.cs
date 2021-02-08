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
            IOUtils.CreateFileIfNotExists(configPath);
            Reload();
        }

        public static void Set(string key, string value)
        {
            string[] lines = new string[1];
            try
            {
                lines = File.ReadAllLines(configPath);
            }
            catch
            {
                MessageBox.Show("Failed to read config file!\nFlowframes will try to re-create the file if it does not exist.", "Error");
                if(!File.Exists(configPath))
                    Init();
            }
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
            list = list.OrderBy(p => p).ToList();

            string newFileContent = "";
            foreach(string line in list)
                newFileContent += line + "\n";

            File.WriteAllText(configPath, newFileContent.Trim());

            cachedLines = list.ToArray();
        }

        public static string Get(string key, string defaultVal)
        {
            WriteIfDoesntExist(key, defaultVal);
            return Get(key);
        }

        public static string Get(string key, Type type = Type.String)
        {
            try
            {
                for (int i = 0; i < cachedLines.Length; i++)
                {
                    string[] keyValuePair = cachedLines[i].Split('|');
                    if (keyValuePair[0] == key && !string.IsNullOrWhiteSpace(keyValuePair[1]))
                        return keyValuePair[1];
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

        public static bool GetBool(string key, bool defaultVal)
        {
            WriteIfDoesntExist(key, (defaultVal ? "True" : "False"));
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
            if (key == "clearLogOnInput")   return WriteDefault(key, "True");
            if (key == "tempDirCustom")     return WriteDefault(key, "C:/");
            // Interpolation
            if (key == "dedupMode")         return WriteDefault(key, "2");
            if (key == "dedupThresh")       return WriteDefault(key, "2");
            if (key == "keepAudio")         return WriteDefault(key, "True");
            if (key == "keepSubs")          return WriteDefault(key, "True");
            if (key == "autoDedupFrames")   return WriteDefault(key, "100");
            if (key == "scnDetectValue")    return WriteDefault(key, "0.2");
            if (key == "autoEncMode")       return WriteDefault(key, "2");
            // Video Export
            if (key == "minOutVidLength")   return WriteDefault(key, "5");
            if (key == "h264Crf")           return WriteDefault(key, "20");
            if (key == "h265Crf")           return WriteDefault(key, "24");
            if (key == "vp9Crf")            return WriteDefault(key, "32");
            if (key == "proResProfile")     return WriteDefault(key, "2");
            if (key == "aviCodec")          return WriteDefault(key, "ffv1");
            if (key == "aviColors")         return WriteDefault(key, "yuv420p");
            if (key == "gifColors")         return WriteDefault(key, "128 (High)");
            if (key == "minVidLength")      return WriteDefault(key, "2");
            // AI
            if (key == "uhdThresh")         return WriteDefault(key, "1440");
            if (key == "ncnnThreads")       return WriteDefault(key, "1");
            if (key == "dainNcnnTilesize")  return WriteDefault(key, "768");
            // Debug / Other / Experimental
            if (key == "modelsBaseUrl")     return WriteDefault(key, "https://dl.nmkd.de/flowframes/mdl/");
            if (key == "ffEncPreset")       return WriteDefault(key, "medium");
            if (key == "ffEncArgs")         return WriteDefault(key, "");

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
            List<string> validLines = new List<string>();
            string[] lines = File.ReadAllLines(configPath);
            foreach (string line in lines)
            {
                if(line != null && !string.IsNullOrWhiteSpace(line) && line.Length > 3)
                    validLines.Add(line);
            }
            cachedLines = validLines.ToArray();
        }
    }
}
