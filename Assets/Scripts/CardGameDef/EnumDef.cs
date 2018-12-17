/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EnumDef
    {
        public const string Hex = "0x";
        public const string Delimiter = " | ";

        [JsonProperty]
        public string Property { get; private set; }

        [JsonProperty]
        public Dictionary<string, string> Values { get; private set; }

        public Dictionary<string, int> Lookups { get; } = new Dictionary<string, int>();

        public static bool TryParseInt(string number, out int intValue)
        {
            bool isHex = number.StartsWith(Hex);
            return int.TryParse(isHex ? number.Substring(Hex.Length) : number, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue);
        }

        public void InitializeLookups()
        {
            Lookups.Clear();
            foreach (string key in Values.Keys)
                CreateLookup(key);
        }

        public int CreateLookup(string key)
        {
            if (string.IsNullOrEmpty(key) || Lookups.ContainsKey(key))
                return 0;

            int intValue;
            if (!key.StartsWith(Hex) || !TryParseInt(key, out intValue))
                intValue = 1 << Lookups.Count;

            Lookups[key] = intValue;
            return intValue;
        }

        public string GetStringFromLookupFlags(int flags)
        {
            string stringValue = string.Empty;
            foreach (KeyValuePair<string, string> enumValue in Values)
            {
                int lookupValue;
                if (!Lookups.TryGetValue(enumValue.Key, out lookupValue) || (lookupValue & flags) == 0)
                    continue;
                if (!string.IsNullOrEmpty(stringValue))
                    stringValue += Delimiter;
                stringValue += enumValue.Value;
            }
            return stringValue;
        }

        public string GetStringFromPropertyValue(string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue))
                return string.Empty;

            string stringValue = string.Empty;
            foreach (string splitValue in propertyValue.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrEmpty(stringValue))
                    stringValue += Delimiter;
                int lookupFlags;
                string mappedValue;
                if (Lookups.TryGetValue(splitValue, out lookupFlags) || TryParseInt(splitValue, out lookupFlags))
                    stringValue += GetStringFromLookupFlags(lookupFlags);
                else
                    stringValue += Values.TryGetValue(splitValue, out mappedValue) ? mappedValue : splitValue;
            }
            return stringValue;
        }

        public int GetEnumFromPropertyValue(string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue))
                return 0;

            int enumValue = 0;
            foreach (string stringValue in propertyValue.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries))
            {
                int intValue;
                if (Lookups.TryGetValue(stringValue, out intValue) || TryParseInt(stringValue, out intValue))
                    enumValue |= intValue;
            }
            return enumValue;
        }
    }
}
