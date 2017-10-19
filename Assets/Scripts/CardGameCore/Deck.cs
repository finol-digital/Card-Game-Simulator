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
    public const string TxtInstructions = "#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
    public const string YdkInstructions = "#On each line, enter <Card Id>\n#Copy/Paste recommended";

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
        Name = name != null ? name.Clone() as string : string.Empty;
        FileType = fileType;
        Cards = new List<Card>();
    }

    public static Deck Parse(string name, DeckFileType type, string text)
    {
        Deck newDeck = new Deck(name, type);
        if (string.IsNullOrEmpty(text))
            return newDeck;

        foreach (string line in text.Split('\n').Select(x => x.Trim())) {
            switch (type) {
                case DeckFileType.Dec:
                    newDeck.LoadDec(line);
                    break;
                case DeckFileType.Hsd:
                    newDeck.LoadHsd(line);
                    break;
                case DeckFileType.Ydk:
                    if (line.Equals("!side"))
                        return newDeck;
                    newDeck.LoadYdk(line);
                    break;
                case DeckFileType.Txt:
                default:
                    if (line.Equals("Sideboard") || line.Equals("sideboard") || line.Equals("Sideboard:"))
                        return newDeck;
                    newDeck.LoadTxt(line);
                    break;
            }
        }
        return newDeck;
    }

    public void LoadDec(string line)
    {
        if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("SB:"))
            return;

        int cardCount = 1;
        List<string> tokens = line.Split(' ').ToList();
        if (tokens.Count > 0 && int.TryParse(tokens [0], out cardCount))
            tokens.RemoveAt(0);
        string cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
        IEnumerable<Card> cards = CardGameManager.Current.FilterCards(null, cardName, null, null, null, null, null);
        foreach (Card card in cards) {
            if (string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase)) {
                for (int i = 0; i < cardCount; i++)
                    Cards.Add(card);
                break;
            }
        }
    }

    public void LoadHsd(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        if (line.StartsWith("#")) {
            if (line.StartsWith("###"))
                Name = line.Substring(3).Trim();
            return;
        }

        byte[] bytes = Convert.FromBase64String(line);
        ulong offset = 3;
        int length;

        int numHeroes = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numHeroes; i++)
            AddCardsByPropertyInt(CardGameManager.Current.HsdPropertyId, (int)VarInt.Read(bytes, ref offset, out length), 1);

        int numSingleCards = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numSingleCards; i++)
            AddCardsByPropertyInt(CardGameManager.Current.HsdPropertyId, (int)VarInt.Read(bytes, ref offset, out length), 1);

        int numDoubleCards = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numDoubleCards; i++)
            AddCardsByPropertyInt(CardGameManager.Current.HsdPropertyId, (int)VarInt.Read(bytes, ref offset, out length), 2);

        int numMultiCards = (int)VarInt.Read(bytes, ref offset, out length);
        for (int i = 0; i < numMultiCards; i++) {
            int id = (int)VarInt.Read(bytes, ref offset, out length);
            int count = (int)VarInt.Read(bytes, ref offset, out length);
            AddCardsByPropertyInt(CardGameManager.Current.HsdPropertyId, id, count);
        }

        Sort();
    }

    public void AddCardsByPropertyInt(string propertyName, int propertyValue, int count)
    {
        Card card = CardGameManager.Current.Cards.Where((curr) => curr.GetPropertyValueInt(propertyName) == propertyValue).ToList().FirstOrDefault();
        for (int i = 0; card != null && i < count; i++)
            Cards.Add(card);
    }

    public void LoadYdk(string line)
    {
        if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.Equals("!side"))
            return;
            
        List<Card> results = CardGameManager.Current.Cards.Where((card) => card.Id.Equals(line)).ToList();
        if (results.Count > 0)
            Cards.Add(results [0]);
    }

    public void LoadTxt(string line)
    {
        if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.Equals("Sideboard") || line.Equals("sideboard") || line.Equals("Sideboard:"))
            return;

        int cardCount = 1;
        string cardName = line;
        string cardId = string.Empty;
        string cardSet = string.Empty;
        if (line.Contains(" ")) {
            List<string> tokens = line.Split(' ').ToList();
            if (tokens.Count > 0 && int.TryParse((tokens [0].StartsWith("x") || tokens [0].EndsWith("x")) ? tokens [0].Replace("x", "") : tokens [0], out cardCount))
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
            if (card.Id.Equals(cardId) || (string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrEmpty(cardSet) || card.SetCode.Equals(cardSet)))) {
                for (int i = 0; i < cardCount; i++)
                    Cards.Add(card);
                break;
            }
        }
    }

    public void Shuffle()
    {
        Cards.Shuffle();
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

    public List<Card> GetExtraCards()
    {
        List<Card> extraCards = new List<Card>();
        foreach (ExtraDef extraDef in CardGameManager.Current.Extras)
            extraCards.AddRange(Cards.Where(
                (card) => EnumDef.IsEnumProperty(extraDef.Property) ?
                card.GetPropertyValueString(extraDef.Property).Contains(extraDef.Value) :
                card.GetPropertyValueString(extraDef.Property).Equals(extraDef.Value)).ToList());
        return extraCards;
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
        string text = "### " + Name + System.Environment.NewLine;
        List<Card> extraCards = GetExtraCards();
        if (extraCards.Count > 0 && !string.IsNullOrEmpty(extraCards [0].GetPropertyValueString("cardClass")))
            text += "# Class: " + extraCards [0].GetPropertyValueString("cardClass") + System.Environment.NewLine;
        text += "# Format: Wild" + System.Environment.NewLine;
        text += "#" + System.Environment.NewLine;

        Dictionary<Card, int> cardCounts = GetCardCounts();
        foreach (Card card in cardCounts.Keys)
            if (!extraCards.Contains(card))
                text += "# " + cardCounts [card] + "x (" + card.GetPropertyValueString("cost") + ") " + card.Name + System.Environment.NewLine;
        text += "#" + System.Environment.NewLine;

        text += SerializeHsd() + System.Environment.NewLine;
        return text;
    }

    public string SerializeHsd()
    {
        using (MemoryStream ms = new MemoryStream()) {
            ms.WriteByte(0);
            VarInt.Write(ms, 1);
            VarInt.Write(ms, 1);

            Dictionary<Card, int> cardCounts = GetCardCounts();
            List<Card> extraCards = GetExtraCards();
            List<KeyValuePair<Card, int>> singleCopy = cardCounts.Where(x => x.Value == 1).ToList();
            List<KeyValuePair<Card, int>> doubleCopy = cardCounts.Where(x => x.Value == 2).ToList();
            List<KeyValuePair<Card, int>> nCopy = cardCounts.Where(x => x.Value > 2).ToList();
            singleCopy.RemoveAll((cardCount) => extraCards.Contains(cardCount.Key));
            doubleCopy.RemoveAll((cardCount) => extraCards.Contains(cardCount.Key));
            nCopy.RemoveAll((cardCount) => extraCards.Contains(cardCount.Key));

            VarInt.Write(ms, extraCards.Count);
            foreach (Card card in extraCards)
                VarInt.Write(ms, card.GetPropertyValueInt(CardGameManager.Current.HsdPropertyId));

            VarInt.Write(ms, singleCopy.Count);
            foreach (KeyValuePair<Card, int> cardCount in singleCopy)
                VarInt.Write(ms, cardCount.Key.GetPropertyValueInt(CardGameManager.Current.HsdPropertyId));

            VarInt.Write(ms, doubleCopy.Count);
            foreach (KeyValuePair<Card, int> cardCount in doubleCopy)
                VarInt.Write(ms, cardCount.Key.GetPropertyValueInt(CardGameManager.Current.HsdPropertyId));

            VarInt.Write(ms, nCopy.Count);
            foreach (KeyValuePair<Card, int> cardCount in nCopy) {
                VarInt.Write(ms, cardCount.Key.GetPropertyValueInt(CardGameManager.Current.HsdPropertyId));
                VarInt.Write(ms, cardCount.Value);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
    }

    public string ToYdk()
    {
        string text = "#created by Card Game Simulator" + System.Environment.NewLine;
        List<Card> mainCards = new List<Card>(Cards);
        List<Card> extraCards = GetExtraCards();
        mainCards.RemoveAll((card) => extraCards.Contains(card));

        text += "#main" + System.Environment.NewLine;
        foreach (Card card in mainCards)
            text += card.Id + System.Environment.NewLine;
        text += "#extra" + System.Environment.NewLine;
        foreach (Card card in extraCards)
            text += card.Id + System.Environment.NewLine;

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
