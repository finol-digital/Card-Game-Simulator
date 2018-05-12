using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace CardGameDef
{
    public enum DeckFileType
    {
        Dec,
        Hsd,
        Txt,
        Ydk
    }

    public enum DeckFileTxtId
    {
        Id,
        Set
    }

    public class Deck : IEquatable<Deck>
    {
        public const string DefaultName = "Untitled";

        public string FilePath => CardGameManager.Current.DecksFilePath + "/" + UnityExtensionMethods.GetSafeFileName(Name + "." + FileType.ToString().ToLower());

        public string Name { get; set; }
        public DeckFileType FileType { get; private set; }
        public List<Card> Cards { get; private set; }

        public Deck() : this(DefaultName)
        {
        }

        public Deck(string name) : this(name, DeckFileType.Txt)
        {
        }

        public Deck(string name, DeckFileType fileType) : this(name, fileType, new List<Card>())
        {
        }

        public Deck(string name, DeckFileType fileType, List<Card> cards)
        {
            Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : DefaultName;
            FileType = fileType;
            Cards = new List<Card>(cards);
        }

        public static Deck Parse(string name, DeckFileType type, string text)
        {
            Deck newDeck = new Deck(name, type);
            if (string.IsNullOrEmpty(text))
                return newDeck;

            foreach (string line in text.Split('\n').Select(x => x.Trim()))
            {
                switch (type)
                {
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
            if (tokens.Count > 0 && int.TryParse(tokens[0], out cardCount))
                tokens.RemoveAt(0);
            string cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
            IEnumerable<Card> cards = CardGameManager.Current.FilterCards(null, cardName, null, null, null, null, null);
            foreach (Card card in cards)
            {
                if (!string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase))
                    continue;
                for (int i = 0; i < cardCount; i++)
                    Cards.Add(card);
                break;
            }
        }

        public void LoadHsd(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            if (line.StartsWith("#"))
            {
                if (line.StartsWith("###"))
                    Name = line.Substring(3).Trim();
                return;
            }

            byte[] bytes = Convert.FromBase64String(line);
            ulong offset = 3;
            int length;

            int numHeroes = (int)VarInt.Read(bytes, ref offset, out length);
            for (int i = 0; i < numHeroes; i++)
                AddCardsByPropertyInt(CardGameManager.Current.DeckFileHsdId, (int)VarInt.Read(bytes, ref offset, out length), 1);

            int numSingleCards = (int)VarInt.Read(bytes, ref offset, out length);
            for (int i = 0; i < numSingleCards; i++)
                AddCardsByPropertyInt(CardGameManager.Current.DeckFileHsdId, (int)VarInt.Read(bytes, ref offset, out length), 1);

            int numDoubleCards = (int)VarInt.Read(bytes, ref offset, out length);
            for (int i = 0; i < numDoubleCards; i++)
                AddCardsByPropertyInt(CardGameManager.Current.DeckFileHsdId, (int)VarInt.Read(bytes, ref offset, out length), 2);

            int numMultiCards = (int)VarInt.Read(bytes, ref offset, out length);
            for (int i = 0; i < numMultiCards; i++)
            {
                int id = (int)VarInt.Read(bytes, ref offset, out length);
                int count = (int)VarInt.Read(bytes, ref offset, out length);
                AddCardsByPropertyInt(CardGameManager.Current.DeckFileHsdId, id, count);
            }

            Sort();
        }

        public void AddCardsByPropertyInt(string propertyName, int propertyValue, int count)
        {
            Card card = CardGameManager.Current.Cards.Values.FirstOrDefault(currCard => currCard.GetPropertyValueInt(propertyName) == propertyValue);
            for (int i = 0; card != null && i < count; i++)
                Cards.Add(card);
        }

        public void LoadYdk(string line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.Equals("!side"))
                return;

            if (CardGameManager.Current.Cards.ContainsKey(line))
                Cards.Add(CardGameManager.Current.Cards[line]);
        }

        public void LoadTxt(string line)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.Equals("Sideboard") || line.Equals("sideboard") || line.Equals("Sideboard:"))
                return;

            int cardCount = 1;
            string cardName = line;
            string cardId = string.Empty;
            string cardSet = string.Empty;
            if (line.Contains(" "))
            {
                List<string> tokens = line.Split(' ').ToList();
                if (tokens.Count > 0 && int.TryParse((tokens[0].StartsWith("x") || tokens[0].EndsWith("x")) ? tokens[0].Replace("x", "") : tokens[0], out cardCount))
                    tokens.RemoveAt(0);

                if (tokens.Count > 0 && tokens[0].StartsWith("[") && tokens[0].EndsWith("]"))
                {
                    cardId = tokens[0].Substring(1, tokens[0].Length - 2);
                    tokens.RemoveAt(0);
                }

                if (tokens.Count > 0 && tokens[tokens.Count - 1].StartsWith("(") && tokens[tokens.Count - 1].EndsWith(")"))
                {
                    string inParens = tokens[tokens.Count - 1].Substring(1, tokens[tokens.Count - 1].Length - 2);
                    if (CardGameManager.Current.Sets.ContainsKey(inParens))
                    {
                        cardSet = inParens;
                        tokens.RemoveAt(tokens.Count - 1);
                    }
                }

                cardName = tokens.Count > 0 ? string.Join(" ", tokens.ToArray()) : string.Empty;
            }

            IEnumerable<Card> cards = CardGameManager.Current.FilterCards(cardId, cardName, cardSet, null, null, null, null);
            foreach (Card card in cards)
            {
                if (!card.Id.Equals(cardId) && (!string.Equals(card.Name, cardName, StringComparison.OrdinalIgnoreCase) ||
                                                (!string.IsNullOrEmpty(cardSet) && !card.SetCode.Equals(cardSet))))
                    continue;
                for (int i = 0; i < cardCount; i++)
                    Cards.Add(card);
                break;
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
            foreach (Card card in Cards)
            {
                int currentCount;
                cardCounts.TryGetValue(card, out currentCount);
                currentCount++;
                cardCounts[card] = currentCount;
            }
            return cardCounts;
        }

        public Dictionary<string, List<Card>> GetExtraGroups()
        {
            Dictionary<string, List<Card>> extraGroups = new Dictionary<string, List<Card>>();
            foreach (Card card in Cards)
            {
                foreach (ExtraDef extraDef in CardGameManager.Current.Extras)
                {
                    if (EnumDef.IsEnumProperty(extraDef.Property) ? !card.GetPropertyValueString(extraDef.Property).Contains(extraDef.Value)
                                                                    : !card.GetPropertyValueString(extraDef.Property).Equals(extraDef.Value))
                        continue;
                    string groupName = !string.IsNullOrEmpty(extraDef.Group) ? extraDef.Group : ExtraDef.DefaultExtraGroup;
                    if (!extraGroups.ContainsKey(groupName))
                        extraGroups[groupName] = new List<Card>();
                    extraGroups[groupName].Add(card);
                    break;
                }
            }
            return extraGroups;
        }

        public List<Card> GetExtraCards()
        {
            List<Card> extraCards = new List<Card>();
            foreach (KeyValuePair<string, List<Card>> cardGroup in GetExtraGroups())
                extraCards.AddRange(cardGroup.Value);
            return extraCards;
        }

        public string ToDec()
        {
            string text = string.Empty;
            Dictionary<Card, int> cardCounts = GetCardCounts();
            return cardCounts.Keys.Aggregate(text, (current, card) => current + (cardCounts[card] + " " + card.Name + Environment.NewLine));
        }

        public string ToHsd()
        {
            string text = "### " + Name + Environment.NewLine;
            List<Card> extraCards = GetExtraCards();
            if (extraCards.Count > 0 && !string.IsNullOrEmpty(extraCards[0].GetPropertyValueString("cardClass")))
                text += "# Class: " + extraCards[0].GetPropertyValueString("cardClass") + Environment.NewLine;
            text += "# Format: Wild" + Environment.NewLine;
            text += "#" + Environment.NewLine;

            Dictionary<Card, int> cardCounts = GetCardCounts();
            text = cardCounts.Keys.Where(card => !extraCards.Contains(card)).Aggregate(text, (current, card) => current + ("# " + cardCounts[card] + "x (" + card.GetPropertyValueString("cost") + ") " + card.Name + Environment.NewLine));
            text += "#" + Environment.NewLine;

            text += SerializeHsd() + Environment.NewLine;
            return text;
        }

        public string SerializeHsd()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte(0);
                VarInt.Write(ms, 1);
                VarInt.Write(ms, 1);

                Dictionary<Card, int> cardCounts = GetCardCounts();
                List<Card> extraCards = GetExtraCards();
                List<KeyValuePair<Card, int>> singleCopy = cardCounts.Where(x => x.Value == 1).ToList();
                List<KeyValuePair<Card, int>> doubleCopy = cardCounts.Where(x => x.Value == 2).ToList();
                List<KeyValuePair<Card, int>> nCopy = cardCounts.Where(x => x.Value > 2).ToList();
                singleCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));
                doubleCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));
                nCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));

                VarInt.Write(ms, extraCards.Count);
                foreach (Card card in extraCards)
                    VarInt.Write(ms, card.GetPropertyValueInt(CardGameManager.Current.DeckFileHsdId));

                VarInt.Write(ms, singleCopy.Count);
                foreach (KeyValuePair<Card, int> cardCount in singleCopy)
                    VarInt.Write(ms, cardCount.Key.GetPropertyValueInt(CardGameManager.Current.DeckFileHsdId));

                VarInt.Write(ms, doubleCopy.Count);
                foreach (KeyValuePair<Card, int> cardCount in doubleCopy)
                    VarInt.Write(ms, cardCount.Key.GetPropertyValueInt(CardGameManager.Current.DeckFileHsdId));

                VarInt.Write(ms, nCopy.Count);
                foreach (KeyValuePair<Card, int> cardCount in nCopy)
                {
                    VarInt.Write(ms, cardCount.Key.GetPropertyValueInt(CardGameManager.Current.DeckFileHsdId));
                    VarInt.Write(ms, cardCount.Value);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public string ToYdk()
        {
            string text = "#created by Card Game Simulator" + Environment.NewLine;
            List<Card> mainCards = new List<Card>(Cards);
            List<Card> extraCards = GetExtraCards();
            mainCards.RemoveAll(card => extraCards.Contains(card));

            text += "#main" + Environment.NewLine;
            text = mainCards.Aggregate(text, (current, card) => current + (card.Id + Environment.NewLine));
            text += "#extra" + Environment.NewLine;
            text = extraCards.Aggregate(text, (current, card) => current + (card.Id + Environment.NewLine));

            text += "!side" + Environment.NewLine;
            return text;
        }

        public string ToTxt()
        {
            StringBuilder text = new StringBuilder();
            text.AppendFormat("### {0} Deck: {1} {2}", CardGameManager.Current.Name, Name, Environment.NewLine);
            Dictionary<Card, int> cardCounts = GetCardCounts();
            foreach (KeyValuePair<Card, int> cardCount in cardCounts)
            {
                bool isDeckFileTxtIdRequired = CardGameManager.Current.DeckFileTxtIdRequired || cardCount.Key.IsReprint;
                text.Append(cardCount.Value);
                text.Append(" ");
                if (isDeckFileTxtIdRequired && CardGameManager.Current.DeckFileTxtId == DeckFileTxtId.Id)
                {
                    text.AppendFormat("[{0}]", cardCount.Key.Id);
                    text.Append(" ");
                }
                text.Append(cardCount.Key.Name);
                if (isDeckFileTxtIdRequired && CardGameManager.Current.DeckFileTxtId == DeckFileTxtId.Set)
                {
                    text.Append(" ");
                    text.AppendFormat("({0})", cardCount.Key.SetCode);
                }
                text.AppendLine();
            }
            return text.ToString();
        }

        public override string ToString()
        {
            string text;
            switch (FileType)
            {
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

        public bool Equals(Deck other)
        {
            return other != null && ToString().Equals(other.ToString());
        }
    }
}