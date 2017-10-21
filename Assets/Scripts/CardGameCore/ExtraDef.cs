using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class ExtraDef
{
    [JsonProperty]
    public string Property { get; private set; }

    [JsonProperty]
    public string Value { get; private set; }
    
}
