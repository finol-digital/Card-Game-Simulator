using System;
using Newtonsoft.Json;

public enum PropertyType
{
    String,
    Integer,
    Enum,
    EnumList
}

[JsonObject(MemberSerialization.OptIn)]
public class PropertyDef : ICloneable
{
    [JsonProperty]
    public string Name { get; set; }

    [JsonProperty]
    public PropertyType Type { get; set; }

    [JsonProperty]
    public string Empty { get; set; }

    public object Clone()
    {
        PropertyDef ret = new PropertyDef() { Name = Name.Clone() as string, Type = Type, Empty = Empty  };
        return ret;
    }
}

public class PropertyDefValuePair : ICloneable
{
    public PropertyDef Def { get; set; }

    public string Value { get; set; }

    public object Clone()
    {
        PropertyDefValuePair ret = new PropertyDefValuePair() {
            Def = Def.Clone() as PropertyDef,
            Value = Value.Clone() as string
        };
        return ret;
    }
}
