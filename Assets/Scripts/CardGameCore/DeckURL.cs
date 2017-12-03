using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class DeckURL
{
    [JsonProperty]
    public string Name { get; private set; }

    [JsonProperty]
    public string URL { get; private set; }
}
