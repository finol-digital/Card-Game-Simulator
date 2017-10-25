using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class ExtraDef
{
    public const string DefaultExtraGroup = "Extras";

    [JsonProperty]
    public string Group { get; private set; }

    [JsonProperty]
    public string Property { get; private set; }

    [JsonProperty]
    public string Value { get; private set; }
    
}
