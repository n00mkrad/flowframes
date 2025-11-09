using Flowframes.Forms;
using Flowframes.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Flowframes.IO
{
    class Config
    {
        private static string configPath;
        public static Dictionary<string, string> CachedValues = new Dictionary<string, string>();
        public static bool NoWrite = false;

        public static void Init()
        {
            configPath = Path.Combine(Paths.GetDataPath(), "config.json");
            IoUtils.CreateFileIfNotExists(configPath);
            Reload();
        }

        public static async Task Reset(int retries = 3, SettingsForm settingsForm = null)
        {
            try
            {
                if (settingsForm != null)
                    settingsForm.Enabled = false;

                File.Delete(configPath);
                await Task.Delay(100);
                CachedValues.Clear();
                await Task.Delay(100);

                if (settingsForm != null)
                    settingsForm.Enabled = true;
            }
            catch(Exception e)
            {
                retries -= 1;
                Logger.Log($"Failed to reset config: {e.Message}. Retrying ({retries} attempts left).", true);
                await Task.Delay(500);
                await Reset(retries, settingsForm);
            }
        }

        public static void Set(Key key, string value)
        {
            Set(key.ToString(), value);
        }

        public static void Set(string str, string value)
        {
            // Reload();
            CachedValues[str] = value;
            WriteConfig();
        }

        public static void Set(Dictionary<string, string> keyValuePairs)
        {
            // Reload();

            foreach(KeyValuePair<string, string> entry in keyValuePairs)
                CachedValues[entry.Key] = entry.Value;

            WriteConfig();
        }

        private static void WriteConfig()
        {
            if (NoWrite)
                return;

            SortedDictionary<string, string> cachedValuesSorted = new SortedDictionary<string, string>(CachedValues);

            // Retry every 100ms up to 10 times in case of an exception
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(cachedValuesSorted, Formatting.Indented));
                    return;
                }
                catch (Exception e)
                {
                    Logger.Log($"Failed to write config{(i < 10 ? ", will retry." : " and out of retries!")} {e.Message}", true);
                    Task.Delay(100).Wait();
                }
            }
        }

        private static void Reload()
        {
            try
            {
                Dictionary<string, string> newDict = new Dictionary<string, string>();
                Dictionary<string, string> deserializedConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(configPath));

                if (deserializedConfig == null)
                    deserializedConfig = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> entry in deserializedConfig)
                    newDict.Add(entry.Key, entry.Value);

                CachedValues = newDict; // Use temp dict and only copy it back if no exception was thrown
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to reload config! {e.Message}", true);
            }
        }

        public static string Get(Key key, Type type = Type.String)
        {
            return Get(key.ToString(), type);
        }

        public static string Get(string key, Type type = Type.String)
        {
            string keyStr = key.ToString();

            try
            {
                if (CachedValues.ContainsKey(keyStr))
                    return CachedValues[keyStr];

                return WriteDefaultValIfExists(key.ToString(), type);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to get {keyStr.Wrap()} from config! {e.Message}");
            }

            return null;
        }

        #region Get Bool

        public static bool GetBool(Key key)
        {
            return Get(key, Type.Bool).GetBool();
        }

        public static bool GetBool(Key key, bool defaultVal = false)
        {
            WriteIfDoesntExist(key.ToString(), (defaultVal ? true : false).ToString());
            return Get(key, Type.Bool).GetBool();
        }

        public static bool GetBool(string key)
        {
            return Get(key, Type.Bool).GetBool();
        }

        public static bool GetBool(string key, bool defaultVal)
        {
            WriteIfDoesntExist(key, (defaultVal ? true : false).ToString());
            return bool.Parse(Get(key, Type.Bool));
        }

        #endregion

        #region Get Int

        public static int GetInt(Key key)
        {
            return Get(key, Type.Int).GetInt();
        }

        public static int GetInt(Key key, int defaultVal)
        {
            WriteIfDoesntExist(key.ToString(), defaultVal.ToString());
            return GetInt(key);
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

        #endregion

        #region Get Float

        public static float GetFloat(Key key)
        {
            return Get(key, Type.Float).GetFloat();
        }

        public static float GetFloat(Key key, float defaultVal)
        {
            WriteIfDoesntExist(key.ToString(), defaultVal.ToString());
            return Get(key, Type.Float).GetFloat();
        }

        public static float GetFloat(string key)
        {
            return Get(key, Type.Float).GetFloat();
        }

        public static float GetFloat(string key, float defaultVal)
        {
            WriteIfDoesntExist(key, defaultVal.ToString());
            return Get(key, Type.Float).GetFloat();
        }

        public static string GetFloatString (Key key)
        {
            return Get(key, Type.Float).Replace(",", ".");
        }

        public static string GetFloatString(string key)
        {
            return Get(key, Type.Float).Replace(",", ".");
        }

        #endregion

        static void WriteIfDoesntExist (string key, string val)
        {
            if (CachedValues.ContainsKey(key))
                return;

            Set(key, val);
        }

        public enum Type { String, Int, Float, Bool }
        private static string WriteDefaultValIfExists(string keyStr, Type type)
        {
            Key key;

            try
            {
                key = (Key)Enum.Parse(typeof(Key), keyStr);
            }
            catch
            {
                return WriteDefault(keyStr, "");
            }

            if (key == Key.onlyShowRelevantSettings) return WriteDefault(key, true);
            if (key == Key.disablePreview)        return WriteDefault(key, true);
            if (key == Key.maxVidHeight)          return WriteDefault(key, "2160");
            if (key == Key.clearLogOnInput)       return WriteDefault(key, true);
            if (key == Key.tempDirCustom)         return WriteDefault(key, "D:/");
            if (key == Key.exportNamePattern)     return WriteDefault(key, "[NAME]-[FACTOR]x-[MODEL]-[FPS]fps");
            if (key == Key.exportNamePatternLoop) return WriteDefault(key, "-Loop[LOOPS]");
            // Interpolation
            if (key == Key.dedupThresh)           return WriteDefault(key, "2");
            if (key == Key.keepAudio)             return WriteDefault(key, true);
            if (key == Key.keepSubs)              return WriteDefault(key, true);
            if (key == Key.keepMeta)              return WriteDefault(key, true);
            if (key == Key.scnDetect)             return WriteDefault(key, true);
            if (key == Key.scnDetectValue)        return WriteDefault(key, "0.2");
            if (key == Key.sceneChangeFillMode)   return WriteDefault(key, "0");
            if (key == Key.autoEncMode)           return WriteDefault(key, "2");
            if (key == Key.jpegFrames)            return WriteDefault(key, true);
            if (key == Key.enableAlpha)           return WriteDefault(key, true);
            // Video Export
            if (key == Key.minOutVidLength)   return WriteDefault(key, "5");
            if (key == Key.gifDitherType)     return WriteDefault(key, "bayer");
            if (key == Key.minVidLength)      return WriteDefault(key, "5");
            // AI
            if (key == Key.uhdThresh)         return WriteDefault(key, "1600");
            if (key == Key.torchGpus)         return WriteDefault(key, "0");
            if (key == Key.ncnnGpus)          return WriteDefault(key, "0");
            if (key == Key.ncnnThreads)       return WriteDefault(key, "0");
            if (key == Key.dainNcnnTilesize)  return WriteDefault(key, NcnnUtils.GetDainNcnnTileSizeBasedOnVram(768).ToString());
            // Debug / Other / Experimental
            if (key == Key.ffEncPreset)   return WriteDefault(key, "fast");
            if (key == Key.sbsRunPreviousStepIfNeeded) return WriteDefault(key, true);
            if (keyStr == "mpdecimateMode") return WriteDefault(keyStr, "1");
            if (type == Type.Int || type == Type.Float) return WriteDefault(key, "0");     // Write default int/float (0)
            if (type == Type.Bool)                      return WriteDefault(key, false);     // Write default bool (False)
            return WriteDefault(key, "");
        }

        private static string WriteDefault(Key key, object def)
        {
            Set(key, def.ToString());
            return def.ToString();
        }

        private static string WriteDefault(Key key, string def)
        {
            Set(key, def);
            return def;
        }

        private static string WriteDefault(string key, string def)
        {
            Set(key, def);
            return def;
        }

        public enum Key
        {
            aacBitrate,
            aiCombox,
            allowConsecutiveSceneChanges,
            allowCustomInputRate,
            allowOpusInMp4,
            allowSymlinkEncoding,
            allowSymlinkImport,
            alwaysWaitForAutoEnc,
            askedForDevModeVersion,
            autoEncBackupMode,
            autoEncDebug,
            autoEncMode,
            autoEncSafeBufferCuda,
            autoEncSafeBufferNcnn,
            clearLogOnInput,
            compressedPyVersion,
            customServer,
            dainNcnnTilesize,
            dedupMode,
            dedupThresh,
            disablePreview,
            dupeScanDebug,
            enableLoop,
            exportNamePattern,
            exportNamePatternLoop,
            fetchModelsFromRepo,
            ffEncArgs,
            ffEncPreset,
            ffEncThreads,
            ffprobeFrameCount,
            fixOutputDuration,
            frameOrderDebug,
            gifDitherType,
            imgSeqSampleCount,
            jpegFrames,
            jpegInterp,
            keepAspectRatio,
            keepAudio,
            keepColorSpace,
            keepMeta,
            keepSubs,
            keepTempFolder,
            lastUsedAiName,
            loopMode,
            lowDiskSpaceCancelGb,
            lowDiskSpacePauseGb,
            maxFps,
            maxFpsMode,
            maxVidHeight,
            minOutVidLength,
            minVidLength,
            mpdecimateMode,
            ncnnGpus,
            ncnnThreads,
            onlyShowRelevantSettings,
            opusBitrate,
            processingMode,
            rifeCudaBufferSize,
            rifeCudaFp16,
            rifeNcnnUseTta,
            sbsAllowAutoEnc,
            sbsRunPreviousStepIfNeeded,
            sceneChangeFillMode,
            scnDetect,
            scnDetectValue,
            silentDevmodeCheck,
            tempDirCustom,
            tempFolderLoc,
            torchGpus,
            uhdThresh,
            vsRtShowOsd,
            vsUseLsmash,
            lastOutputSettings,
            PerformedHwEncCheck,
            SupportedHwEncoders,
            vfrHandling,
            enableAlpha,
        }
    }
}
