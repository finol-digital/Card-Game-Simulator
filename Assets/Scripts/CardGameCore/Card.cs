using System;
using System.Collections.Generic;
using System.Linq;

public class Card : IComparable<Card>
{
    public static Card Blank {
        get { return new Card(string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>()); }
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string SetCode { get; set; }

    public Dictionary<string, PropertyDefValuePair> Properties { get; set; }

    public Card(string id, string name, string setCode, Dictionary<string,PropertyDefValuePair> properties)
    {
        Id = id.Clone() as string;
        Name = name.Clone() as string;
        SetCode = setCode.Clone() as string;
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
        if (string.IsNullOrEmpty(propertyName))
            return string.Empty;
        if (Properties.ContainsKey(propertyName))
            return Properties [propertyName] != null ? Properties [propertyName].Value : string.Empty;
        return string.Empty;
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
            return CardGameManager.Current.CardImageURLBase
            + string.Format(CardGameManager.Current.CardImageURLFormat, Id, Name, SetCode, NameStrippedToLowerAlphaNum, GetPropertyValueString(CardGameManager.Current.CardImageURLName))
            + "." + CardGameManager.Current.CardImageFileType;
        }
    }

}
