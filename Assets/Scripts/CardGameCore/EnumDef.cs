using System.Collections.Generic;
using Newtonsoft.Json;
using System.Globalization;
using System.Linq;

[JsonObject(MemberSerialization.OptIn)]
public class EnumDef
{
    public const string Hex = "0x";
    public const string Delimiter = "|";

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
        string result = string.Empty;

        foreach (KeyValuePair<string, string> enumValue in Values) {
            int lookupValue;
            if (!ReverseLookup.TryGetValue(enumValue.Key, out lookupValue) || (lookupValue & keys) == 0)
                continue;
            if (!string.IsNullOrEmpty(result))
                result += Delimiter;
            result += enumValue.Value;
        }

        return result;
    }
}
