using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class DeckUrl
{
    [JsonProperty]
    public string Name { get; private set; }

    [JsonProperty]
    public string Url { get; private set; }
}
