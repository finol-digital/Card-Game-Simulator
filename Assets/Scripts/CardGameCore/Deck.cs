using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.IO;

[JsonConverter(typeof(StringEnumConverter))]
public enum DeckFileType
{
    [EnumMember(Value = "dec")]
    Dec,
    [EnumMember(Value = "hsd")]
    Hsd,
    [EnumMember(Value = "txt")]
    Txt,
    [EnumMember(Value = "ydk")]
    Ydk
}

public class Deck
{
    public const string DefaultName = "Untitled";
    public const string DecInstructions = "//On each line, enter:\n//<Quantity> <Card Name>\n//For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
    public const string HsdInstructions = "#Paste the deck string/code here";
    public const string YdkInstructions = "#On each line, enter <Card Id>\n#Copy/Paste recommended";
    public const string TxtInstructions = "#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";

    public string Name { get; set; }

    public DeckFileType FileType { get; set; }

    public List<Card> Cards { get; set; }

    public Deck() : this(DefaultName)
    {
    }

    public Deck(string name) : this(name, DeckFileType.Txt)
    {
    }

    public Deck(string name, DeckFileType fileType)
    {
        Name = name.Clone() as string;
        FileType = fileType;
        Cards = new List<Card>();
    }

    public static Deck Parse(string name, DeckFileType type, string text)
    {
        Deck newDeck = new Deck(name);
        switch (type) {
            case DeckFileType.Dec:
                newDeck = ParseDec(name, text);
                break;
            case DeckFileType.Hsd:
                newDeck = ParseHsd(name, text);
                break;
            case DeckFileType.Ydk:
                newDeck = ParseYdk(name, text);
                break;
            case DeckFileType.Txt:
            default:
                newDeck = ParseTxt(name, text);
                break;
        }
        return newDeck;
    }

    public static Deck ParseDec(string name, string text)
    {
        Deck newDeck = new Deck(name, DeckFileType.Dec);
        if (text == null)
            return newDeck;

        foreach (string line in text.Split('\n').Select(x => x.Trim())) {
            if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("SB:"))
                continue;

            int cardCount = 1;
            List<string> tokens = line.Split(' ').ToList();
            if (tokens.Count > 0 && int.TryParse(tokens [0], out cardCount))
                tokens.RemoveAt(0);
            string cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
            IEnumerable<Card> cards = CardGameManager.Current.FilterCards(null, cardName, null, null, null, null, null);
            foreach (Card card in cards) {
                if (string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase))
                    for (int i = 0; i < cardCount; i++)
                        newDeck.Cards.Add(card);
                break;
            }
        }
        return newDeck;
    }

    public static Deck ParseHsd(string name, string text)
    {
        Deck newDeck = new Deck(name, DeckFileType.Hsd);
        if (text == null)
            return newDeck;
        
        foreach (string line in text.Split('\n').Select(x => x.Trim())) {
            if (string.IsNullOrEmpty(line))
                continue;
            if (line.StartsWith("#")) {
                if (line.StartsWith("###"))
                    name = line.Substring(3).Trim();
                continue;
            }

            return DeserializeHsd(name, line);
        }
        return newDeck;
    }

    public static Deck DeserializeHsd(string name, string deckString)
    {
        Deck newDeck = new Deck(name, DeckFileType.Hsd);
        byte[] bytes = Convert.FromBase64String(deckString);
        ulong offset = 3;
        int length;

        int numHeroes = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numHeroes; i++)
            newDeck.AddCardsByPropertyInt("dbfId", (int)VarInt.Read(bytes, ref offset, out length), 1);

        int numSingleCards = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numSingleCards; i++)
            newDeck.AddCardsByPropertyInt("dbfId", (int)VarInt.Read(bytes, ref offset, out length), 1);

        int numDoubleCards = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numDoubleCards; i++)
            newDeck.AddCardsByPropertyInt("dbfId", (int)VarInt.Read(bytes, ref offset, out length), 2);

        int numMultiCards = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numMultiCards; i++) {
            int dbfId = (int)VarInt.Read(bytes, ref offset, out length);
            int count = (int)VarInt.Read(bytes, ref offset, out length);
            newDeck.AddCardsByPropertyInt("dbfId", dbfId, count);
        }
        return newDeck;
    }

    public void AddCardsByPropertyInt(string propertyName, int propertyValue, int count)
    {
        Card card = CardGameManager.Current.Cards.Where((curr) => curr.GetPropertyValueInt(propertyName) == propertyValue).ToList().FirstOrDefault();
        for (int i = 0; card != null && i < count; i++)
            Cards.Add(card);
    }

    public static Deck ParseYdk(string name, string text)
    {
        Deck newDeck = new Deck(name);
        newDeck.FileType = DeckFileType.Hsd;
        if (text == null)
            return newDeck;

        foreach (string line in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Select(x => x.Trim())) {
            if (line.Equals("!side"))
                break;
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;
            
            List<Card> results = CardGameManager.Current.Cards.Where((card) => card.Id.Equals(line)).ToList();
            if (results.Count > 0)
                newDeck.Cards.Add(results [0]);
        }
        return newDeck;
    }

    public static Deck ParseTxt(string name, string text)
    {
        Deck newDeck = new Deck(name, DeckFileType.Txt);
        if (text == null)
            return newDeck;

        foreach (string line in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Select(x => x.Trim())) {
            if (line.Equals("Sideboard") || line.Equals("sideboard") || line.Equals("Sideboard:"))
                break;
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            int cardCount = 1;
            string cardName = line;
            string cardId = string.Empty;
            string cardSet = string.Empty;
            if (line.Contains(" ")) {
                List<string> tokens = line.Split(' ').ToList();
                if (tokens.Count > 0 && int.TryParse(tokens [0].EndsWith("x") ? tokens [0].Remove(tokens [0].Length - 1) : tokens [0], out cardCount))
                    tokens.RemoveAt(0);

                if (tokens.Count > 0 && tokens [0].StartsWith("[") && tokens [0].EndsWith("]")) {
                    cardId = tokens [0].Substring(1, tokens [0].Length - 2);
                    tokens.RemoveAt(0);
                }

                if (tokens.Count > 0 && tokens [tokens.Count - 1].StartsWith("(") && tokens [tokens.Count - 1].EndsWith(")")) {
                    string inParens = tokens [tokens.Count - 1].Substring(1, tokens [tokens.Count - 1].Length - 2);
                    if (CardGameManager.Current.Sets.Where((currSet) => currSet.Code.Equals(inParens)).ToList().Count > 0) {
                        cardSet = inParens;
                        tokens.RemoveAt(tokens.Count - 1);
                    }
                }

                cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
            }

            IEnumerable<Card> cards = CardGameManager.Current.FilterCards(cardId, cardName, cardSet, null, null, null, null);
            foreach (Card card in cards) {
                if (card.Id.Equals(cardId) || (string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrEmpty(cardSet) || card.SetCode.Equals(cardSet))))
                    for (int i = 0; i < cardCount; i++)
                        newDeck.Cards.Add(card);
                break;
            }
        }
        return newDeck;
    }

    public void Sort()
    {
        Cards.Sort();
    }

    public Dictionary<Card, int> GetCardCounts()
    {
        Dictionary<Card, int> cardCounts = new Dictionary<Card, int>();
        foreach (Card card in Cards) {
            int currentCount = 0;
            cardCounts.TryGetValue(card, out currentCount);
            currentCount++;
            cardCounts [card] = currentCount;
        }
        return cardCounts;
    }

    public string ToDec()
    {
        string text = string.Empty;
        Dictionary<Card, int> cardCounts = GetCardCounts();
        foreach (Card card in cardCounts.Keys)
            text += cardCounts [card] + " " + card.Name + System.Environment.NewLine;
        return text;
    }

    public string ToHsd()
    {
        string cardClass = "UNKNOWN";
        List<int> heroDBFIds = new List<int>();
        foreach (Card card in Cards) {
            if ("HERO".Equals(card.GetPropertyValueString("type"))) {
                cardClass = card.GetPropertyValueString("cardClass");
                heroDBFIds.Add(card.GetPropertyValueInt("dbfId"));
            }
        }
        string text = "### " + Name + System.Environment.NewLine;
        text += "# Class: " + cardClass + System.Environment.NewLine;
        text += "# Format: Wild" + System.Environment.NewLine;
        text += "#" + System.Environment.NewLine;
        Dictionary<Card, int> cardCounts = GetCardCounts();
        foreach (Card card in cardCounts.Keys)
            if (!heroDBFIds.Contains(card.GetPropertyValueInt("dbfId")))
                text += "# " + cardCounts [card] + "x (" + card.GetPropertyValueString("cost") + ") " + card.Name + System.Environment.NewLine;
        text += "#" + System.Environment.NewLine;
        text += SerializeHsd(heroDBFIds) + System.Environment.NewLine;
        text += "#" + System.Environment.NewLine;
        text += "#To use this deck, copy it to your clipboard and paste it where it can be loaded" + System.Environment.NewLine;
        return text;
    }

    public string SerializeHsd(List<int> heroDBFIds)
    {
        if (heroDBFIds == null || heroDBFIds.Count < 1)
            return string.Empty;
        
        using (MemoryStream ms = new MemoryStream()) {
            ms.WriteByte(0);
            VarInt.Write(ms, 1);
            VarInt.Write(ms, 1);

            Dictionary<Card, int> cardCounts = GetCardCounts();
            List<KeyValuePair<Card, int>> singleCopy = cardCounts.Where(x => x.Value == 1).ToList();
            List<KeyValuePair<Card, int>> doubleCopy = cardCounts.Where(x => x.Value == 2).ToList();
            List<KeyValuePair<Card, int>> nCopy = cardCounts.Where(x => x.Value > 2).ToList();
            singleCopy.RemoveAll((cardCount) => heroDBFIds.Contains(cardCount.Key.GetPropertyValueInt("dbfId")));
            doubleCopy.RemoveAll((cardCount) => heroDBFIds.Contains(cardCount.Key.GetPropertyValueInt("dbfId")));
            nCopy.RemoveAll((cardCount) => heroDBFIds.Contains(cardCount.Key.GetPropertyValueInt("dbfId")));

            VarInt.Write(ms, heroDBFIds.Count);
            foreach (int heroDbfId in heroDBFIds)
                VarInt.Write(ms, heroDbfId);

            VarInt.Write(ms, singleCopy.Count);
            foreach (KeyValuePair<Card, int> cardCount in singleCopy)
                VarInt.Write(ms, cardCount.Key.GetPropertyValueInt("dbfId"));

            VarInt.Write(ms, doubleCopy.Count);
            foreach (KeyValuePair<Card, int> cardCount in doubleCopy)
                VarInt.Write(ms, cardCount.Key.GetPropertyValueInt("dbfId"));

            VarInt.Write(ms, nCopy.Count);
            foreach (KeyValuePair<Card, int> cardCount in nCopy) {
                VarInt.Write(ms, cardCount.Key.GetPropertyValueInt("dbfId"));
                VarInt.Write(ms, cardCount.Value);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
    }

    public string ToYdk()
    {
        string text = "#created by Card Game Simulator" + System.Environment.NewLine;
        text += "#main" + System.Environment.NewLine;

        EnumDef enumDef = CardGameManager.Current.Enums.Where((def) => def.Property.Equals("type")).First();
        bool missingExtra = enumDef != null;
        PropertyDefValuePair property;
        int propertyIntValue;
        foreach (Card card in Cards) {
            if (missingExtra && card.Properties.TryGetValue("type", out property)) {
                EnumDef.TryParse(property.Value, out propertyIntValue);
                string enumValue = enumDef.GetStringFromFlags(propertyIntValue);
                if (enumValue.Contains("Fusion") || enumValue.Contains("Synchro") || enumValue.Contains("XYZ") || enumValue.Contains("Link")) {
                    text += "#extra" + System.Environment.NewLine;
                    missingExtra = false;
                }
            }
            text += card.Id + System.Environment.NewLine;
        }

        text += "!side" + System.Environment.NewLine;
        return text;
    }

    public string ToTxt()
    {
        string text = "# " + CardGameManager.Current.Name + " Deck List: " + Name + System.Environment.NewLine;
        Dictionary<Card, int> cardCounts = GetCardCounts();
        foreach (Card card in cardCounts.Keys)
            text += cardCounts [card] + " " + card.Name + System.Environment.NewLine;
        return text;
    }

    public override string ToString()
    {
        string text = string.Empty;
        switch (FileType) {
            case DeckFileType.Dec:
                text = ToDec();
                break;
            case DeckFileType.Hsd:
                text = ToHsd();
                break;
            case DeckFileType.Ydk:
                text = ToYdk();
                break;
            case DeckFileType.Txt:
            default:
                text = ToTxt();
                break;
        }
        return text;
    }

    public string FilePath {
        get {
            return CardGameManager.Current.DecksFilePath + "/" + UnityExtensionMethods.GetSafeFileName(Name + "." + FileType.ToString().ToLower());
        }
    }
}
