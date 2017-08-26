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

    public IDictionary<string , CardPropertySet> Properties { get; set; }

    public Card(string id, string name, string setCode, IDictionary<string,CardPropertySet> properties)
    {
        Id = id.Clone() as string;
        Name = name.Clone() as string;
        SetCode = setCode.Clone() as string;
        Properties = properties;
        this.Properties = this.CloneProperties();
    }

    public IDictionary<string, CardPropertySet> CloneProperties()
    {
        var ret = new Dictionary<string, CardPropertySet>();
        foreach (var p in Properties) {
            ret.Add((string)p.Key.Clone(), p.Value.Clone() as CardPropertySet);
        }
        return ret;
    }
}

public class CardPropertySet : ICloneable
{
    public PropertyDef Key { get; set; }

    public PropertyDefValue Value { get; set; }

    public object Clone()
    {
        var ret = new CardPropertySet() {
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