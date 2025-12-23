using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Flowframes.Data;
using System.Management.Automation;
using System.Drawing;
using Flowframes.MiscUtils;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Flowframes.Ui;

namespace Flowframes
{
    public static class ExtensionMethods
    {
        /// <summary> Remove anything from a string that is not a number, optionally allowing scientific notation (<paramref name="allowScientific"/>) </summary>
        public static string TrimNumbers(this string s, bool allowDotComma = false, bool allowScientific = false)
        {
            if (s == null)
                return s;

            // string og = s;
            string regex = $@"[^{(allowDotComma ? ".," : "")}0-9\-{(allowScientific ? "e" : "")}]";
            s = Regex.Replace(s, regex, "").Trim();
            // Logger.Log($"Trimmed {og} -> {s} - Pattern: {regex}", true);
            return s;
        }

        public static int GetInt(this TextBox textbox)
        {
            return GetInt(textbox.Text);
        }

        public static int GetInt(this ComboBox combobox)
        {
            return GetInt(combobox.Text);
        }

        public static int GetInt(this string str)
        {
            if (str == null || str.Length < 1 || str.Contains("\n") || str == "N/A" || !str.Any(char.IsDigit))
                return 0;

            try
            {
                str = str.TrimNumbers(allowDotComma: false);
                return int.Parse(str);
            }
            catch (Exception e)
            {
                Logger.Log("Failed to parse \"" + str + "\" to int: " + e.Message, true);
                return 0;
            }
        }

        public static bool GetBool(this string str)
        {
            try
            {
                if (str.IsEmpty())
                    return false;

                return bool.Parse(str);
            }
            catch
            {
                return false;
            }
        }

        public static bool? GetBoolCli(this string str)
        {
            if (str == null)
                return null;

            str = str.Trim().Lower();

            if (str == "true" || str == "1" || str == "yes")
                return true;

            if (str == "false" || str == "0" || str == "no")
                return false;

            return null;
        }

        public static float GetFloat(this TextBox textbox)
        {
            return GetFloat(textbox.Text);
        }

        public static float GetFloat(this ComboBox combobox)
        {
            return GetFloat(combobox.Text);
        }

        public static float GetFloat(this string str)
        {
            if (str.Length < 1 || str == null)
                return 0f;

            string num = str.TrimNumbers(true).Replace(",", ".");
            float.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out float value);
            return value;
        }

        public static string Wrap(this string path, bool addSpaceFront = false, bool addSpaceEnd = false)
        {
            string s = "\"" + path + "\"";

            if (addSpaceFront)
                s = " " + s;

            if (addSpaceEnd)
                s = s + " ";

            return s;
        }

        public static string GetParentDir(this string path)
        {
            return Directory.GetParent(path).FullName;
        }

        public static int RoundToInt(this float f)
        {
            return (int)Math.Round(f);
        }

        public static int Clamp(this int i, int min, int max)
        {
            if (i < min)
                i = min;

            if (i > max)
                i = max;

            return i;
        }

        public static float Clamp(this float i, float min, float max)
        {
            if (i < min)
                i = min;

            if (i > max)
                i = max;

            return i;
        }

        public static string[] SplitIntoLines(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new string[0];

            return Regex.Split(str, "\r\n|\r|\n");
        }

        public static string Trunc(this string inStr, int maxChars, bool addEllipsis = true)
        {
            string str = inStr.Length <= maxChars ? inStr : inStr.Substring(0, maxChars);
            if (addEllipsis && inStr.Length > maxChars)
                str += "…";
            return str;
        }

        public static string StripBadChars(this string str)
        {
            string outStr = Regex.Replace(str, @"[^\u0020-\u007E]", string.Empty);
            outStr = outStr.Remove("(").Remove(")").Remove("[").Remove("]").Remove("{").Remove("}").Remove("%").Remove("'").Remove("~");
            return outStr;
        }

        public static string StripNumbers(this string str)
        {
            return new string(str.Where(c => c != '-' && (c < '0' || c > '9')).ToArray());
        }

        public static string Remove(this string str, string stringToRemove)
        {
            if (str == null || stringToRemove == null)
                return "";

            return str.Replace(stringToRemove, "");
        }

        public static string TrimWhitespaces(this string str)
        {
            if (str == null) return "";
            var newString = new StringBuilder();
            bool previousIsWhitespace = false;
            for (int i = 0; i < str.Length; i++)
            {
                if (Char.IsWhiteSpace(str[i]))
                {
                    if (previousIsWhitespace)
                        continue;
                    previousIsWhitespace = true;
                }
                else
                {
                    previousIsWhitespace = false;
                }
                newString.Append(str[i]);
            }
            return newString.ToString();
        }

        public static string ReplaceLast(this string str, string stringToReplace, string replaceWith)
        {
            int place = str.LastIndexOf(stringToReplace);

            if (place == -1)
                return str;

            return str.Remove(place, stringToReplace.Length).Insert(place, replaceWith);
        }

        public static string[] SplitBy(this string str, string splitBy)
        {
            return str.Split(new string[] { splitBy }, StringSplitOptions.None);
        }

        public static string RemoveComments(this string str)
        {
            return str.Split('#')[0].SplitBy("//")[0];
        }

        public static string FilenameSuffix(this string path, string suffix)
        {
            string filename = Path.ChangeExtension(path, null);
            string ext = Path.GetExtension(path);
            return filename + suffix + ext;
        }

        public static string[] Split(this string str, string trimStr)
        {
            return str?.Split(new string[] { trimStr }, StringSplitOptions.None);
        }

        public static bool MatchesWildcard(this string str, string wildcard)
        {
            WildcardPattern pattern = new WildcardPattern(wildcard);
            return pattern.IsMatch(str);
        }

        public static int RoundMod(this int n, int mod = 2)     // Round to a number that's divisible by 2 (for h264 etc)
        {
            int a = (n / 2) * 2;    // Smaller multiple
            int b = a + 2;   // Larger multiple
            return (n - a > b - n) ? b : a; // Return of closest of two
        }

        public static string ToTitleCase(this string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
        }

        public static string ToStringShort(this Size s, string separator = "x")
        {
            return $"{s.Width}{separator}{s.Height}";
        }

        public static bool IsConcatFile(this string filePath)
        {
            try
            {
                return Path.GetExtension(filePath)?.Lower() == ".concat";
            }
            catch
            {
                return false;
            }
        }

        public static string GetConcStr(this string filePath, float rate = -1)
        {
            string rateStr = rate >= 0 ? $"-r {rate} " : "";
            return filePath.IsConcatFile() ? $"{rateStr}-safe 0 -f concat " : "";
        }

        public static string Get<TKey>(this Dictionary<TKey, string> dict, string key, bool returnKeyInsteadOfEmptyString = false, bool ignoreCase = false)
        {
            if (key == null)
                key = "";

            // Safely handle a null dictionary
            for (int i = 0; i < (dict == null ? 0 : dict.Count); i++)
            {
                var elementKey = dict.ElementAt(i).Key;
                var elementValue = dict.ElementAt(i).Value;

                // Convert the dictionary key to string (handling null)
                string dictKeyString = (elementKey == null ? "" : elementKey.ToString());

                if (ignoreCase)
                {
                    if (key.Lower() == dictKeyString.Lower())
                        return elementValue;
                }
                else
                {
                    if (key == dictKeyString)
                        return elementValue;
                }
            }

            return returnKeyInsteadOfEmptyString ? key : "";
        }

        /// <summary> Get a value from a dictionary, returns <paramref name="fallback"/> if not found. For strings, the default fallback is an empty string instead of null. </summary>
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue fallback = default)
        {
            // For string values, use empty string instead of null as default value
            if (typeof(TValue) == typeof(string) && fallback != null)
            {
                fallback = (TValue)(object)string.Empty;
            }

            if (dictionary == null)
                return fallback;

            return dictionary.TryGetValue(key, out var value) ? value : fallback;
        }

        public static void FillFromEnum<TEnum>(this ComboBox comboBox, Dictionary<string, string> stringMap = null, int defaultIndex = -1, List<TEnum> exclusionList = null, bool useKeyNames = false) where TEnum : Enum
        {
            if (exclusionList == null)
                exclusionList = new List<TEnum>();

            var entriesToAdd = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Except(exclusionList);
            var strings = useKeyNames ? entriesToAdd.Select(x => UiUtils.PascalCaseToText($"{x}")) : entriesToAdd.Select(x => stringMap.Get($"{x}", true));
            comboBox.FillFromStrings(strings, stringMap, defaultIndex);
        }

        public static void FillFromEnum<TEnum>(this ComboBox comboBox, IEnumerable<TEnum> entries, Dictionary<string, string> stringMap = null, int defaultIndex = -1) where TEnum : Enum
        {
            var strings = entries.Select(x => stringMap.Get(x.ToString(), true));
            comboBox.FillFromStrings(strings, stringMap, defaultIndex);
        }

        public static void FillFromEnum<TEnum>(this ComboBox comboBox, IEnumerable<TEnum> entries, Dictionary<string, string> stringMap, TEnum defaultEntry) where TEnum : Enum
        {
            if (stringMap == null)
                stringMap = new Dictionary<string, string>();

            comboBox.Items.Clear();
            comboBox.Items.AddRange(entries.Select(x => stringMap.Get(x.ToString(), true)).ToArray());
            comboBox.Text = stringMap.Get(defaultEntry.ToString(), true);
        }

        public static void FillFromStrings(this ComboBox comboBox, IEnumerable<string> entries, Dictionary<string, string> stringMap = null, int defaultIndex = -1, IEnumerable<string> exclusionList = null)
        {
            if (stringMap == null)
                stringMap = new Dictionary<string, string>();

            if (exclusionList == null)
                exclusionList = new List<string>();

            comboBox.Items.Clear();
            comboBox.Items.AddRange(entries.Select(x => stringMap.Get(x, true)).Except(exclusionList).ToArray());

            if (defaultIndex >= 0 && comboBox.Items.Count > 0)
                comboBox.SelectedIndex = defaultIndex;
        }

        public static void SetIfTextMatches(this ComboBox comboBox, string str, bool ignoreCase = true, Dictionary<string, string> stringMap = null)
        {
            if (stringMap == null)
                stringMap = new Dictionary<string, string>();

            str = stringMap.Get(str, true, true);

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (ignoreCase)
                {
                    if (comboBox.Items[i].ToString().Lower() == str.Lower())
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
                else
                {
                    if (comboBox.Items[i].ToString() == str)
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        public static string Lower(this string s)
        {
            if (s == null)
                return s;

            return s.ToLowerInvariant();
        }

        public static string Upper(this string s)
        {
            if (s == null)
                return s;

            return s.ToUpperInvariant();
        }

        public static EncoderInfoVideo GetInfo(this Enums.Encoding.Encoder enc)
        {
            return OutputUtils.GetEncoderInfoVideo(enc);
        }

        public static bool IsEmpty(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static bool IsNotEmpty(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        public static string ToJson(this object o, bool indent = false, bool ignoreErrors = true)
        {
            var settings = new JsonSerializerSettings();

            if (ignoreErrors)
                settings.Error = (s, e) => { e.ErrorContext.Handled = true; };

            // Serialize enums as strings.
            settings.Converters.Add(new StringEnumConverter());

            return JsonConvert.SerializeObject(o, indent ? Formatting.Indented : Formatting.None, settings);
        }

        // TODO: Remove once NmkdUtils has been adopted
        public static bool EqualsRoughly(this float a, float b, float tolerance = 0.0001f)
        {
            return Math.Abs(a - b) < tolerance;
        }

        public static float GetVramGb(this NvAPIWrapper.GPU.PhysicalGPU gpu)
        {
            try
            {
                return gpu.MemoryInformation.AvailableDedicatedVideoMemoryInkB / 1024f / 1000f;
            }
            catch
            {
                return 0f;
            }
        }

        public static float GetFreeVramGb(this NvAPIWrapper.GPU.PhysicalGPU gpu)
        {
            try
            {
                return gpu.MemoryInformation.CurrentAvailableDedicatedVideoMemoryInkB / 1024f / 1000f;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary> Add <paramref name="item"/> to list if <paramref name="condition"/> is true </summary>
        public static void AddIf<T>(this IList<T> list, T item, bool condition = true)
        {
            if (list == null || !condition || item == null)
                return;

            list.Add(item);
        }

        /// <summary> Checks if a string is equal to any of the provided <paramref name="strings"/>, optionally case-insensitive. </summary>
        public static bool IsOneOf(this object s, bool caseSensitive, params object[] strings)
        {
            StringComparison strComp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (strings.Length == 0)
                return false;

            if (strings.Length == 1 && strings[0] is IEnumerable<string> col)
                return col.Any(v => s.ToString().Equals(v, strComp));

            return strings.Any(v => s.ToString().Equals(v.ToString(), strComp));
        }
    }
}
