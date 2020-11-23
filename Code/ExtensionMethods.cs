using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes
{
	public static class ExtensionMethods
    {
		public static string TrimNumbers(this string s, bool allowDotComma = false)
		{
			if(!allowDotComma)
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

		public static int GetInt (this string str)
        {
			if (str.Length < 1 || str == null)
				return 0;
			try { return int.Parse(str.TrimNumbers()); }
			catch (Exception e)
			{
				Logger.Log("Failed to parse \"" + str + "\" to int: " + e, true);
				return 0;
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

		public static float GetFloat (this string str)
        {
			if (str.Length < 1 || str == null)
				return 0f;
			string num = str.TrimNumbers(true).Replace(",", ".");
			float value;
			float.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
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

		public static string[] SplitIntoLines (this string str)
        {
			return Regex.Split(str, "\r\n|\r|\n");
		}

		public static string Trunc (this string value, int maxChars)
		{
			return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "…";
		}

		public static string StripBadChars (this string str)
        {
			string outStr = Regex.Replace(str, @"[^\u0020-\u007E]", string.Empty);
			outStr = outStr.Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "").Replace("%", "");
			return outStr;
		}

		public static string StripNumbers (this string str)
        {
			return new string(str.Where(c => c != '-' && (c < '0' || c > '9')).ToArray());
		}
	}
}
