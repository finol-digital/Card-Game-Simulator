/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EnumDef
    {
        public const string Hex = "0x";
        public const string Delimiter = " | ";

        [JsonProperty]
        [Description("Refers to a *Property:Name* in <cardProperties>")]
        public string Property { get; private set; }

        [JsonProperty]
        [Description("Dictionary with string key-value pairs")]
        public Dictionary<string, string> Values { get; private set; }

        public Dictionary<string, int> Lookups { get; } = new Dictionary<string, int>();

        public static bool TryParseInt(string number, out int intValue)
        {
            var isHex = number.StartsWith(Hex);
            return int.TryParse(isHex ? number.Substring(Hex.Length) : number,
                isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer, CultureInfo.InvariantCulture,
                out intValue);
        }

        [JsonConstructor]
        public EnumDef(string property, Dictionary<string, string> values)
        {
            Property = property ?? string.Empty;
            Values = values ?? new Dictionary<string, string>();
        }

        public void InitializeLookups()
        {
            Lookups.Clear();
            foreach (var key in Values.Keys)
                CreateLookup(key);
        }

        public int CreateLookup(string key)
        {
            if (string.IsNullOrEmpty(key) || Lookups.ContainsKey(key))
                return 0;

            if (!key.StartsWith(Hex) || !TryParseInt(key, out var intValue))
                intValue = 1 << Lookups.Count;

            Lookups[key] = intValue;
            return intValue;
        }

        public string GetStringFromLookupFlags(int flags)
        {
            var stringBuilder = new StringBuilder();
            var hasValue = false;
            foreach (var enumValue in Values)
            {
                if (!Lookups.TryGetValue(enumValue.Key, out var lookupValue) || ((lookupValue & flags) == 0))
                    continue;
                if (hasValue)
                    stringBuilder.Append(Delimiter);
                stringBuilder.Append(enumValue.Value);
                hasValue = true;
            }

            return stringBuilder.ToString();
        }

        public string GetStringFromPropertyValue(string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue))
                return string.Empty;

            var stringBuilder = new StringBuilder();
            var hasValue = false;
            foreach (var splitValue in propertyValue.Split(new[] {Delimiter}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (hasValue)
                    stringBuilder.Append(Delimiter);
                if (Lookups.TryGetValue(splitValue, out var lookupFlags) || TryParseInt(splitValue, out lookupFlags))
                    stringBuilder.Append(GetStringFromLookupFlags(lookupFlags));
                else
                    stringBuilder.Append(Values.TryGetValue(splitValue, out var mappedValue)
                        ? mappedValue
                        : splitValue);
                hasValue = true;
            }

            return stringBuilder.ToString();
        }

        public int GetEnumFromPropertyValue(string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue))
                return 0;

            var enumValue = 0;
            foreach (var stringValue in propertyValue.Split(new[] {Delimiter}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Lookups.TryGetValue(stringValue, out var intValue) || TryParseInt(stringValue, out intValue))
                    enumValue |= intValue;
            }

            return enumValue;
        }
    }
}
