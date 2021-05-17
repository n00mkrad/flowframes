using Flowframes.OS;
using Newtonsoft.Json;
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
        public static Dictionary<string, string> cachedValues = new Dictionary<string, string>();

        public static void Init()
        {
            configPath = Path.Combine(Paths.GetDataPath(), "config.json");
            IOUtils.CreateFileIfNotExists(configPath);
            Reload();
        }

        public static void Set(string key, string value)
        {
            Reload();
            cachedValues[key] = value;
            WriteConfig();
        }

        public static void Set(Dictionary<string, string> keyValuePairs)
        {
            Reload();

            foreach(KeyValuePair<string, string> entry in keyValuePairs)
                cachedValues[entry.Key] = entry.Value;

            WriteConfig();
        }

        private static void WriteConfig()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(cachedValues, Formatting.Indented));
        }

        private static void Reload()
        {
            try
            {
                Dictionary<string, string> newDict = new Dictionary<string, string>();
                Dictionary<string, string> deserializedConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(configPath));

                foreach (KeyValuePair<string, string> entry in deserializedConfig)
                    newDict.Add(entry.Key, entry.Value);

                cachedValues = newDict; // Use temp dict and only copy it back if no exception was thrown
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to reload config! {e.Message}");
            }
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
                if (cachedValues.ContainsKey(key))
                    return cachedValues[key];

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

        public static float GetFloat(string key, float defaultVal)
        {
            WriteIfDoesntExist(key, defaultVal.ToStringDot());
            return float.Parse(Get(key, Type.Float), CultureInfo.InvariantCulture);
        }

        public static string GetFloatString (string key)
        {
            return Get(key, Type.Float).Replace(",", ".");
        }

        static void WriteIfDoesntExist (string key, string val)
        {
            if (cachedValues.ContainsKey(key))
                return;

            Set(key, val);
        }

        public enum Type { String, Int, Float, Bool }
        private static string WriteDefaultValIfExists(string key, Type type)
        {
            if (key == "maxVidHeight")          return WriteDefault(key, "2160");
            if (key == "delLogsOnStartup")      return WriteDefault(key, "True");
            if (key == "clearLogOnInput")       return WriteDefault(key, "True");
            if (key == "tempDirCustom")         return WriteDefault(key, "D:/");
            if (key == "exportNamePattern")     return WriteDefault(key, "[NAME]-[FACTOR]x-[AI]-[MODEL]-[FPS]fps");
            if (key == "exportNamePatternLoop") return WriteDefault(key, "-Loop[LOOPS]");
            // Interpolation
            if (key == "dedupThresh")           return WriteDefault(key, "2");
            if (key == "keepAudio")             return WriteDefault(key, "True");
            if (key == "keepSubs")              return WriteDefault(key, "True");
            if (key == "keepMeta")              return WriteDefault(key, "True");
            if (key == "autoDedupFrames")       return WriteDefault(key, "100");
            if (key == "scnDetect")             return WriteDefault(key, "True");
            if (key == "scnDetectValue")        return WriteDefault(key, "0.2");
            if (key == "sceneChangeFillMode")   return WriteDefault(key, "1");
            if (key == "autoEncMode")           return WriteDefault(key, "2");
            if (key == "jpegFrames")            return WriteDefault(key, "True");
            // Video Export
            if (key == "minOutVidLength")   return WriteDefault(key, "5");
            if (key == "h264Crf")           return WriteDefault(key, "20");
            if (key == "h265Crf")           return WriteDefault(key, "24");
            if (key == "av1Crf")            return WriteDefault(key, "27");
            if (key == "vp9Crf")            return WriteDefault(key, "28");
            if (key == "proResProfile")     return WriteDefault(key, "2");
            if (key == "aviCodec")          return WriteDefault(key, "ffv1");
            if (key == "imgSeqFormat")      return WriteDefault(key, "PNG");
            if (key == "aviColors")         return WriteDefault(key, "yuv420p");
            if (key == "gifColors")         return WriteDefault(key, "128 (High)");
            if (key == "gifDitherType")     return WriteDefault(key, "bayer (Recommended)");
            if (key == "minVidLength")      return WriteDefault(key, "5");
            // AI
            if (key == "uhdThresh")         return WriteDefault(key, "1600");
            if (key == "rifeCudaFp16")      return WriteDefault(key, NvApi.HasTensorCores().ToString());
            if (key == "torchGpus")         return WriteDefault(key, "0");
            if (key == "ncnnGpus")          return WriteDefault(key, "0");
            if (key == "ncnnThreads")       return WriteDefault(key, "1");
            if (key == "dainNcnnTilesize")  return WriteDefault(key, "768");
            // Debug / Other / Experimental
            if (key == "mdlBaseUrl")    return WriteDefault(key, "https://dl.nmkd.de/flowframes/mdl/");
            if (key == "ffEncPreset")   return WriteDefault(key, "medium");

            if (type == Type.Int || type == Type.Float) return WriteDefault(key, "0");     // Write default int/float (0)
            if (type == Type.Bool)                      return WriteDefault(key, "False");     // Write default bool (False)
            return WriteDefault(key, "");
        }

        private static string WriteDefault(string key, string def)
        {
            Set(key, def);
            return def;
        }
    }
}
