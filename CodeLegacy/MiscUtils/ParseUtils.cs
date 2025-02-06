using System;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.MiscUtils
{
    internal class ParseUtils
    {
        public static TEnum GetEnum<TEnum>(string str, bool ignoreCase = true, Dictionary<string, string> stringMap = null) where TEnum : Enum
        {
            if (stringMap == null)
                stringMap = new Dictionary<string, string>();

            str = stringMap.Get(str, true, true);
            var values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();

            foreach (var entry in values)
            {
                string entryString = stringMap.Get(entry.ToString(), true);

                if (ignoreCase)
                {
                    if (entryString.Lower() == str.Lower())
                        return entry;
                }
                else
                {
                    if (entryString == str)
                        return entry;
                }
            }

            return (TEnum)(object)(-1);
        }

        public static List<string> GetEnumStrings<TEnum>() where TEnum : Enum
        {
            var entries = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
            return entries.Select(e => e.ToString()).ToList();
        }
    }
}
