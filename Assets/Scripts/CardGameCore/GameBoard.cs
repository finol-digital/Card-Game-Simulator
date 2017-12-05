using Newtonsoft.Json;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class GameBoard
{
    [JsonProperty]
    public string Id { get; private set; }

    [JsonProperty]
    public Vector2 OffsetMax { get; private set; }

    [JsonProperty]
    public Vector2 OffsetMin { get; private set; }
}
