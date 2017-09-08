using System;
using System.Collections.Generic;
using System.Linq;

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

    public string StripNameToLowerAlphaNum()
    {
        char[] cardNameAlphaNum = Name.Where(c => (char.IsLetterOrDigit(c) ||
                                  char.IsWhiteSpace(c) ||
                                  c == '-')).ToArray(); 
        string cardImageName = new string(cardNameAlphaNum);
        cardImageName = cardImageName.Replace(" ", "_").Replace("-", "_").ToLower();
        return cardImageName;
    }
    // TODO: BETTER MANAGEMENT OF GETTING IMAGEFILENAME
    public string ImageFileName {
        get { 
            return UnityExtensionMethods.GetSafeFilename(string.Format(CardGameManager.Current.CardImageFileNameFormat, Id, Name, SetCode, StripNameToLowerAlphaNum()) + "." + CardGameManager.Current.CardImageType);
        }
    }

    public string ImageFilePath {
        get { 
            return UnityExtensionMethods.GetSafeFilepath(CardGameManager.Current.FilePathBase + "/sets/" + SetCode + "/") + ImageFileName;
        }
    }

    public string ImageWebURL {
        get { 
            return CardGameManager.Current.CardImageURLBase + ImageFileName;
        }
    }

}
