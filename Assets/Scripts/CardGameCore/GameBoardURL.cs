using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class GameBoardUrl
{
    [JsonProperty]
    public string Id { get; private set; }

    [JsonProperty]
    public string Url { get; private set; }
}
