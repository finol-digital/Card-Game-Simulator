using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

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
    public const string HsdInstructions = "#Paste the deck string/code here";
    public const string YdkInstructions = "#On each line, enter <Card Id>\n#It is recommended to paste from another program or website";
    public const string TxtInstructions = "#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";

    public string Name { get; set; }

    public DeckFileType FileType { get; set; }

    public List<Card> Cards { get; set; }

    public Deck(string name) : this(name, DeckFileType.Txt)
    {
    }

    public Deck(string name, DeckFileType fileType)
    {
        Name = name.Clone() as string;
        FileType = fileType;
        Cards = new List<Card>();
    }

    public void Sort()
    {
        Cards.Sort();
    }

    public void Load(string text)
    {
        if (text == null)
            return;

        if (Cards == null)
            Cards = new List<Card>();
        Cards.Clear();

        switch (FileType) {
            case DeckFileType.Dec:
                LoadDec(text);
                break;
            case DeckFileType.Hsd:
                LoadHsd(text);
                break;
            case DeckFileType.Ydk:
                LoadYdk(text);
                break;
            case DeckFileType.Txt:
            default:
                LoadTxt(text);
                break;
        }
    }

    private void LoadDec(string text)
    {
    }

    private void LoadHsd(string text)
    {
    }

    private void LoadYdk(string text)
    {
        foreach (string rawLine in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
            String line = rawLine.Trim();
            if (line.Equals("!side"))
                break;
            if (line.Length == 0 || line [0] == '#')
                continue;
            
            List<Card> results = CardGameManager.Current.Cards.Where((card) => card.Id.Equals(line)).ToList();
            if (results.Count > 0)
                Cards.Add(results [0]);
        }
    }

    private void LoadTxt(string text)
    {
        foreach (string rawLine in text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)) {
            String line = rawLine.Trim();
            if (line.Equals("Sideboard") || line.Equals("sideboard") || line.Equals("Sideboard:"))
                break;
            if (line.Length == 0 || line [0] == '#')
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
                        Cards.Add(card);
                break;
            }
        }
    }

    private string ToDec()
    {
        return null;
    }

    private string ToHsd()
    {
        return null;
    }

    private string ToYdk()
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

    private string ToTxt()
    {
        string text = "# " + CardGameManager.Current.Name + " Deck List: " + Name + System.Environment.NewLine;
        Dictionary<string, int> cardCounts = new Dictionary<string, int>();
        foreach (Card card in Cards) {
            int currentCount = 0;
            cardCounts.TryGetValue(card.Name, out currentCount);
            currentCount++;
            cardCounts [card.Name] = currentCount;
        }

        foreach (string cardName in cardCounts.Keys)
            text += cardCounts [cardName] + " " + cardName + System.Environment.NewLine;

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
