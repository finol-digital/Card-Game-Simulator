using System.Collections.Generic;
using Newtonsoft.Json;
using System.Globalization;
using System.Linq;

[JsonObject(MemberSerialization.OptIn)]
public class EnumDef
{
    public const string Hex = "0x";
    public const string Delimiter = " | ";

    [JsonProperty]
    public string Property { get; private set; }

    [JsonProperty]
    public Dictionary<string, string> Values { get; private set; }

    public Dictionary<int, string> Lookup { get; } = new Dictionary<int, string>();
    public Dictionary<string, int> ReverseLookup { get; } = new Dictionary<string, int>();
    public bool LookupEqualsValue { get; private set; }

    public static bool IsEnumProperty(string propertyName)
    {
        return CardGameManager.Current.Enums.Where(def => def.Property.Equals(propertyName)).ToList().Count > 0;
    }

    public static bool TryParseInt(string number, out int intValue)
    {
        bool isHex = number.StartsWith(Hex);
        return int.TryParse(isHex ? number.Substring(Hex.Length) : number, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue);
    }

    public int CreateLookup(string key)
    {
        if (ReverseLookup.ContainsKey(key))
            return 0;

        int intValue;
        if (!key.StartsWith(Hex) || !TryParseInt(key, out intValue))
            intValue = 1 << Lookup.Count;
        else
            LookupEqualsValue = true;

        Lookup [intValue] = key;
        ReverseLookup [key] = intValue;
        return intValue;
    }

    public string GetStringFromLookupKeys(int keys)
    {
        string stringValue = string.Empty;
        foreach (KeyValuePair<string, string> enumValue in Values) {
            int lookupValue;
            if (!ReverseLookup.TryGetValue(enumValue.Key, out lookupValue) || (lookupValue & keys) == 0)
                continue;
            if (!string.IsNullOrEmpty(stringValue))
                stringValue += Delimiter;
            stringValue += enumValue.Value;
        }
        return stringValue;
    }
    
    public string GetStringFromPropertyValue(string propertyValue, bool isPropertyList = false)
    {
        if (string.IsNullOrEmpty(propertyValue)
            return 0;

        string stringValue = string.Empty;
        foreach(string splitValue in propertyValue.Split(Delimiter, StringSplitOptions.RemoveEmptyEntries)) {
            if (!string.IsNullOrEmpty(stringValue))
                stringValue += Delimiter;
            int lookupKeys;
            string mappedValue;
            if ((LookupEqualsValue && TryParseInt(splitValue, out lookupKeys)) 
                || ReverseLookup.TryGetValue(splitValue, out lookupKeys))
                stringValue += GetStringFromLookupKeys(lookupKeys);
            else
                stringValue += Values.TryGetValue(splitValue, out mappedValue) ? mappedValue : splitValue;
        }
        return stringValue;
    }
    
    public int GetEnumFromPropertyValue(string propertyValue)
    {
        if (string.IsNullOrEmpty(propertyValue)
            return 0;

        int enumValue = 0;
        foreach(string stringValue in propertyValue.Split(Delimiter, StringSplitOptions.RemoveEmptyEntries)) {
            int intValue;
            if ((LookupEqualsValue && TryParseInt(stringValue, out intValue)) 
                || ReverseLookup.TryGetValue(stringValue, out intValue))
                enumValue |= intValue;
        }
        return enumValue;
    }
}
