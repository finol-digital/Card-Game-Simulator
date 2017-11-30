using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    public object Clone()
    {
        PropertyDef ret = new PropertyDef() { Name = this.Name, Type = this.Type  };
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
            Def = this.Def.Clone() as PropertyDef,
            Value = this.Value.Clone() as string
        };
        return ret;
    }
}