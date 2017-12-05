using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class GameBoardURL
{
    [JsonProperty]
    public string Id { get; private set; }

    [JsonProperty]
    public string URL { get; private set; }
}
