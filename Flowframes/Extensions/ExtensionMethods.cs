using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Flowframes.Data;
using System.Drawing;
using Flowframes.MiscUtils;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Win32Interop.Structs;

namespace Flowframes
{
    public static class ExtensionMethods
    {
        public static string TrimNumbers(this string s, bool allowDotComma = false)
        {
            if (!allowDotComma)
                s = Regex.Replace(s, "[^0-9]", "");
            else
                s = Regex.Replace(s, "[^.,0-9]", "");
            return s.Trim();
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
                return int.Parse(str.TrimNumbers());
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
                return bool.Parse(str);
            }
            catch
            {
                return false;
            }
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
            if (str == null || str.Length < 1)
                return 0f;

            string num = str.TrimNumbers(true).Replace(",", ".");
            float.TryParse(num, out float value);
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
            if(addEllipsis && inStr.Length > maxChars)
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

        public static string ReplaceLast (this string str, string stringToReplace, string replaceWith)
        {
            int place = str.LastIndexOf(stringToReplace);

            if (place == -1)
                return str;

            return str.Remove(place, stringToReplace.Length).Insert(place, replaceWith);
        }

        public static string[] SplitBy (this string str, string splitBy)
        {
            return str.Split(new string[] { splitBy }, StringSplitOptions.None);
        }

        public static string RemoveComments (this string str)
        {
            return str.Split('#')[0].SplitBy("//")[0];
        }

        public static string FilenameSuffix(this string path, string suffix)
        {
            string filename = Path.ChangeExtension(path, null);
            string ext = Path.GetExtension(path);
            return filename + suffix + ext;
        }

        public static string ToStringDot (this float f, string format = "")
        {
            if(string.IsNullOrWhiteSpace(format))
                return f.ToString().Replace(",", ".");
            else
                return f.ToString(format).Replace(",", ".");
        }

        public static string[] Split(this string str, string trimStr)
        {
            return str?.Split(new string[] { trimStr }, StringSplitOptions.None);
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

        public static string GetConcStr(this string filePath, int rate = -1)
        {
            string rateStr = rate >= 0 ? $"-r {rate} " : "";
            return filePath.IsConcatFile() ? $"{rateStr}-safe 0 -f concat " : "";
        }

        public static string GetFfmpegInputArg(this string filePath)
        {
            return $"{(filePath.IsConcatFile() ? filePath.GetConcStr() : "")} -i {filePath.Wrap()}";
        }

        public static string Get(this Dictionary<string, string> dict, string key, bool returnKeyInsteadOfEmptyString = false, bool ignoreCase = false)
        {
            if (key == null)
                key = "";

            for (int i = 0; i < dict.Count; i++)
            {
                if (ignoreCase)
                {
                    if (key.Lower() == dict.ElementAt(i).Key.Lower())
                        return dict.ElementAt(i).Value;
                }
                else
                {
                    if (key == dict.ElementAt(i).Key)
                        return dict.ElementAt(i).Value;
                }
            }

            if (returnKeyInsteadOfEmptyString)
                return key;
            else
                return "";
        }

        public static void FillFromEnum<TEnum>(this ComboBox comboBox, Dictionary<string, string> stringMap = null, int defaultIndex = -1, List<TEnum> exclusionList = null) where TEnum : Enum
        {
            if (exclusionList == null)
                exclusionList = new List<TEnum>();

            var entriesToAdd = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Except(exclusionList);
            var strings = entriesToAdd.Select(x => stringMap.Get(x.ToString(), true));
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

            return s.Lower();
        }

        public static string Upper(this string s)
        {
            if (s == null)
                return s;

            return s.ToUpperInvariant();
        }

        public static EncoderInfoVideo GetInfo (this Enums.Encoding.Encoder enc)
        {
            return OutputUtils.GetEncoderInfoVideo(enc);
        }

        public static bool IsEmpty (this string s)
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
    }
}
