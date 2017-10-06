using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[JsonObject(MemberSerialization.OptIn)]
public class EnumDef
{
    [JsonProperty]
    public string Property { get; set; }

    [JsonProperty]
    public Dictionary<string, string> Values { get; set; }
}
