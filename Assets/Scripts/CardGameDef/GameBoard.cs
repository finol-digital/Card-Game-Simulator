using Newtonsoft.Json;
using UnityEngine;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoard
    {
        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public Vector2 OffsetMin { get; private set; }

        [JsonProperty]
        public Vector2 Size { get; private set; }
    }
}
