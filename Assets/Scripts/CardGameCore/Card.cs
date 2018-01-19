using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

public class Card : IComparable<Card>, IEquatable<Card>
{
    public static readonly Card Blank = new Card(string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>());

    public string Id { get; private set; }
    public string Name { get; private set; }
    public string SetCode { get; private set; }
    protected Dictionary<string, PropertyDefValuePair> Properties { get; private set; }

    public HashSet<CardModel> ModelsUsingImage { get; private set; }
    public bool IsLoadingImage { get; set; }

    public string ImageFileName => UnityExtensionMethods.GetSafeFileName(Id + "." + CardGameManager.Current.CardImageFileType);
    public string ImageFilePath => UnityExtensionMethods.GetSafeFilePath(CardGameManager.Current.FilePathBase + "/sets/" + SetCode + "/") + ImageFileName;
    public string ImageWebUrl {
        get { string url = CardGameManager.Current.CardImageUrl;
            url = url.Replace("{cardId}", Id);
            url = url.Replace("{cardName}", Name);
            url = url.Replace("{cardSet}", SetCode);
            Regex rgx = new Regex("{card\\.(?<property>\\w+)}");
            foreach (Match match in rgx.Matches(url))
                url = url.Replace(match.Value, GetPropertyValueString(match.Groups["property"].Value));
            url = url.Replace("{cardImageFileType}", CardGameManager.Current.CardImageFileType);
            return url;
        }
    }

    private UnityEngine.Sprite _imageSprite;

    public Card(string id, string name, string setCode, Dictionary<string,PropertyDefValuePair> properties)
    {
        Id = id.Clone() as string;
        Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : string.Empty;
        SetCode = !string.IsNullOrEmpty(setCode) ? setCode.Clone() as string : Set.DefaultCode;
        Properties = properties ?? new Dictionary<string, PropertyDefValuePair>();
        Properties = CloneProperties();
        ModelsUsingImage = new HashSet<CardModel>();
        IsLoadingImage = false;
    }

    public Dictionary<string, PropertyDefValuePair> CloneProperties()
    {
        return Properties.ToDictionary(property => (string) property.Key.Clone(), property => property.Value.Clone() as PropertyDefValuePair);
    }

    public string GetPropertyValueString(string propertyName)
    {
        PropertyDefValuePair property;
        if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
            return string.Empty;

        EnumDef enumDef = CardGameManager.Current.Enums.FirstOrDefault(def => def.Property.Equals(propertyName));
        if (enumDef == null)
            return property.Value;
        return enumDef.GetStringFromPropertyValue(property.Value);
    }

    public int GetPropertyValueInt(string propertyName)
    {
        PropertyDefValuePair property;
        if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
            return 0;

        int intValue;
        if (!EnumDef.TryParseInt(property.Value, out intValue)
            return 0;
        return intValue;
    }

    public int GetPropertyValueEnum(string propertyName)
    {
        PropertyDefValuePair property;
        if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
            return 0;

        EnumDef enumDef = CardGameManager.Current.Enums.FirstOrDefault(def => def.Property.Equals(propertyName));
        if (enumDef == null)
            return 0;
        return enumDef.GetEnumFromPropertyValue(property.Value);
    }

    public int CompareTo(Card other)
    {
        if (other == null)
            return -1;

        foreach (PropertyDefValuePair property in Properties.Values) {
            switch (property.Def.Type) {
                case PropertyType.Enum:
                case PropertyType.EnumList:
                case PropertyType.Integer:
                    int thisValue = GetPropertyValueInt(property.Def.Name);
                    int otherValue = other.GetPropertyValueInt(property.Def.Name);
                    return thisValue.CompareTo(otherValue);
                case PropertyType.String:
                default:
                    return string.Compare(property.Value, other.Properties [property.Def.Name].Value, StringComparison.Ordinal);
            }
        }
        return 0;
    }

    public bool Equals(Card other)
    {
        return other != null && Id.Equals(other.Id);
    }

    public UnityEngine.Sprite ImageSprite {
        get { return _imageSprite; }
        set {
            if (_imageSprite != null) {
                UnityEngine.Object.Destroy(_imageSprite.texture);
                UnityEngine.Object.Destroy(_imageSprite);
            }
            _imageSprite = value;
        }
    }
}
