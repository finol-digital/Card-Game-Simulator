using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Linq;

[JsonObject(MemberSerialization.OptIn)]
public class EnumDef
{
    [JsonProperty]
    public string Property { get; private set; }

    [JsonProperty]
    public Dictionary<string, string> Values { get; private set; }

    public Dictionary<int, string> Lookup {
        get {
            if (_lookup == null)
                _lookup = new Dictionary<int, string>();
            return _lookup;
        }
    }

    public Dictionary<string, int> ReverseLookup {
        get {
            if (_reverseLookup == null)
                _reverseLookup = new Dictionary<string, int>();
            return _reverseLookup;
        }
    }

    public bool LookupEqualsValue { get; private set; }

    private Dictionary<int, string> _lookup;
    private Dictionary<string, int> _reverseLookup;

    public static bool IsEnumProperty(string propertyName)
    {
        return CardGameManager.Current.Enums.Where(def => def.Property.Equals(propertyName)).ToList().Count > 0;
    }

    public static bool TryParseInt(string number, out int intValue)
    {
        bool isHex = number.StartsWith("0x");
        return int.TryParse(isHex ? number.Substring(2) : number, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue);
    }

    public int CreateLookup(string key)
    {
        if (ReverseLookup.ContainsKey(key))
            return 0;
        
        int intValue;
        if (!key.StartsWith("0x") || !EnumDef.TryParseInt(key, out intValue))
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

        int lookupValue;
        foreach (KeyValuePair<string, string> enumValue in Values) {
            if (ReverseLookup.TryGetValue(enumValue.Key, out lookupValue) && (lookupValue & keys) != 0) {
                if (!string.IsNullOrEmpty(result))
                    result += "|";
                result += enumValue.Value;
            }
        }

        return result;
    }
}
