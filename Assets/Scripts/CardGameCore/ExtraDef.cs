using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class ExtraDef
{
    [JsonProperty]
    public string Property { get; set; }

    [JsonProperty]
    public string Value { get; set; }
    
}
