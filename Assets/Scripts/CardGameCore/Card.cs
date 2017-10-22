using System;
using System.Collections.Generic;
using System.Linq;

public class Card : IComparable<Card>, IEquatable<Card>
{
    public static Card Blank {
        get { return new Card(string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>()); }
    }

    public string Id { get; private set; }

    public string Name { get; private set; }

    public string SetCode { get; private set; }

    public Dictionary<string, PropertyDefValuePair> Properties { get; private set; }

    public Card(string id, string name, string setCode, Dictionary<string,PropertyDefValuePair> properties)
    {
        Id = id.Clone() as string;
        Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : string.Empty;
        SetCode = !string.IsNullOrEmpty(setCode) ? setCode.Clone() as string : Set.DefaultCode;
        if (properties == null)
            properties = new Dictionary<string, PropertyDefValuePair>();
        Properties = properties;
        this.Properties = this.CloneProperties();
    }

    public Dictionary<string, PropertyDefValuePair> CloneProperties()
    {
        Dictionary<string, PropertyDefValuePair> clone = new Dictionary<string, PropertyDefValuePair>();
        foreach (KeyValuePair<string, PropertyDefValuePair> property in Properties)
            clone.Add((string)property.Key.Clone(), property.Value.Clone() as PropertyDefValuePair);
        return clone;
    }

    public string GetPropertyValueString(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !Properties.ContainsKey(propertyName))
            return string.Empty;

        EnumDef enumDef = CardGameManager.Current.Enums.Where(def => def.Property.Equals(propertyName)).FirstOrDefault();
        if (enumDef != null)
            return enumDef.GetStringFromIntFlags(GetPropertyValueInt(propertyName));
        return Properties [propertyName] != null ? Properties [propertyName].Value : string.Empty;
    }

    public int GetPropertyValueInt(string propertyName)
    {
        int intValue;
        if (Properties.ContainsKey(propertyName) && Properties [propertyName] != null && int.TryParse(Properties [propertyName].Value, out intValue))
            return intValue;
        return 0;
    }

    public int CompareTo(Card other)
    {
        if (other == null)
            return -1;

        foreach (PropertyDefValuePair property in Properties.Values) {
            int comparison = 0;
            switch (property.Def.Type) {
                case PropertyType.Enum:
                case PropertyType.Integer:
                    int thisValue = GetPropertyValueInt(property.Def.Name);
                    int otherValue = other.GetPropertyValueInt(property.Def.Name);
                    comparison = thisValue.CompareTo(otherValue);
                    break;
                case PropertyType.String:
                default:
                    comparison = property.Value.CompareTo(other.Properties [property.Def.Name].Value);
                    break;
            }
            if (comparison != 0)
                return comparison;
        }
        return 0;
    }

    public bool Equals(Card other)
    {
        return Id.Equals(other.Id);
    }

    public string NameStrippedToLowerAlphaNum {
        get { 
            char[] cardNameAlphaNum = Name.Where(c => (char.IsLetterOrDigit(c) ||
                                      char.IsWhiteSpace(c) ||
                                      c == '-')).ToArray(); 
            string cardImageName = new string(cardNameAlphaNum);
            cardImageName = cardImageName.Replace(" ", "_").Replace("-", "_").ToLower();
            return cardImageName;
        }
    }

    public string ImageFileName {
        get { 
            return UnityExtensionMethods.GetSafeFileName(Id + "." + CardGameManager.Current.CardImageFileType);
        }
    }

    public string ImageFilePath {
        get { 
            return UnityExtensionMethods.GetSafeFilePath(CardGameManager.Current.FilePathBase + "/sets/" + SetCode + "/") + ImageFileName;
        }
    }

    public string ImageWebURL {
        get {
            return string.Format(CardGameManager.Current.CardImageURLFormat, CardGameManager.Current.CardImageURLBase, Id, GetPropertyValueString(CardGameManager.Current.CardImageURLName), Name, NameStrippedToLowerAlphaNum, SetCode)
            + "." + CardGameManager.Current.CardImageFileType;
        }
    }

}
