using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Card
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string SetCode { get; set; }

    public IDictionary<string , PropertySet> Properties { get; set; }

    public Card(string id, string name, string setCode, IDictionary<string,PropertySet> properties)
    {
        Id = id.Clone() as string;
        Name = name.Clone() as string;
        SetCode = setCode.Clone() as string;
        Properties = properties;
        this.Properties = this.CloneProperties();
    }

    public IDictionary<string, PropertySet> CloneProperties()
    {
        var ret = new Dictionary<string, PropertySet>();
        foreach (var p in Properties) {
            ret.Add((string)p.Key.Clone(), p.Value.Clone() as PropertySet);
        }
        return ret;
    }
}

public class PropertySet : ICloneable
{
    public PropertyDef Key { get; set; }

    public PropertyDefValue Value { get; set; }

    public object Clone()
    {
        var ret = new PropertySet() {
            Key = this.Key.Clone() as PropertyDef,
            Value = this.Value.Clone() as PropertyDefValue
        };
        return ret;
    }
}

public enum PropertyType
{
    String,
    Integer,
};

[JsonObject(MemberSerialization.OptIn)]
public class PropertyDef : ICloneable
{
    [JsonProperty]
    public string Name { get; set; }

    [JsonProperty]
    public PropertyType Type { get; set; }

    public object Clone()
    {
        var ret = new PropertyDef() { Name = this.Name, Type = this.Type  };
        return ret;
    }
}

public class PropertyDefValue : ICloneable
{
    public string Value { get; set; }

    public object Clone()
    {
        var ret = new PropertyDefValue() { Value = this.Value };
        return ret;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

}