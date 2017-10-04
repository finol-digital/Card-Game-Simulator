using System;
using System.Collections.Generic;
using System.Linq;

public class Card : IComparable<Card>
{
    public static Card Blank {
        get { return new Card(string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertySet>()); }
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string SetCode { get; set; }

    public Dictionary<string , PropertySet> Properties { get; set; }

    public Card(string id, string name, string setCode, Dictionary<string,PropertySet> properties)
    {
        Id = id.Clone() as string;
        Name = name.Clone() as string;
        SetCode = setCode.Clone() as string;
        Properties = properties;
        this.Properties = this.CloneProperties();
    }

    public Dictionary<string, PropertySet> CloneProperties()
    {
        var ret = new Dictionary<string, PropertySet>();
        foreach (var p in Properties) {
            ret.Add((string)p.Key.Clone(), p.Value.Clone() as PropertySet);
        }
        return ret;
    }

    public int CompareTo(Card other)
    {
        if (other == null)
            return -1;

        foreach (string propName in Properties.Keys) {
            int comparison = Properties [propName].Value.Value.CompareTo(other.Properties [propName].Value.Value);
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
            PropertySet firstProp;
            if (!Properties.TryGetValue(Properties.Keys.FirstOrDefault(), out firstProp))
                firstProp = new PropertySet();
            return CardGameManager.Current.CardImageURLBase + string.Format(CardGameManager.Current.CardImageURLFormat, Id, Name, SetCode.ToLower(), NameStrippedToLowerAlphaNum, firstProp.Value.Value) + "." + CardGameManager.Current.CardImageFileType;
        }
    }

}
