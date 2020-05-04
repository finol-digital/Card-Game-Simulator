/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CardGameDef.Decks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardGameDef
{
    public delegate string OnNameChangeDelegate(string newName);

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeckFileType
    {
        [EnumMember(Value = "dec")] Dec,
        [EnumMember(Value = "hsd")] Hsd,
        [EnumMember(Value = "txt")] Txt,
        [EnumMember(Value = "ydk")] Ydk
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeckFileTxtId
    {
        [EnumMember(Value = "id")] Id,
        [EnumMember(Value = "set")] Set
    }

    public class Deck : IEquatable<Deck>
    {
        public const string DefaultName = "Untitled";

        public string Name { get; set; }

        protected DeckFileType FileType { get; }

        public virtual IReadOnlyCollection<Card> Cards { get; }

        protected CardGame SourceGame { get; set; }

        protected Deck(CardGame sourceGame, string name = DefaultName, DeckFileType fileType = DeckFileType.Txt,
            IReadOnlyCollection<Card> cards = null)
        {
            SourceGame = sourceGame ?? CardGame.Invalid;
            Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : DefaultName;
            FileType = fileType;
            Cards = cards;
        }

        public Dictionary<Card, int> GetCardCounts()
        {
            Dictionary<Card, int> cardCounts = new Dictionary<Card, int>();
            foreach (Card card in Cards)
            {
                cardCounts.TryGetValue(card, out int currentCount);
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
                foreach (ExtraDef extraDef in SourceGame.Extras)
                {
                    if (SourceGame.IsEnumProperty(extraDef.Property)
                        ? !card.GetPropertyValueString(extraDef.Property).Contains(extraDef.Value)
                        : !card.GetPropertyValueString(extraDef.Property).Equals(extraDef.Value))
                        continue;
                    string groupName = !string.IsNullOrEmpty(extraDef.Group)
                        ? extraDef.Group
                        : ExtraDef.DefaultExtraGroup;
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
            var text = string.Empty;
            Dictionary<Card, int> cardCounts = GetCardCounts();
            return cardCounts.Keys.Aggregate(text,
                (current, card) => current + (cardCounts[card] + " " + card.Name + Environment.NewLine));
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
            text = cardCounts.Keys.Where(card => !extraCards.Contains(card)).Aggregate(text,
                (current, card) => current + ("# " + cardCounts[card] + "x (" + card.GetPropertyValueString("cost") +
                                              ") " + card.Name + Environment.NewLine));
            text += "#" + Environment.NewLine;

            text += SerializeHsd() + Environment.NewLine;
            return text;
        }

        public string SerializeHsd()
        {
            using (var memoryStream = new MemoryStream())
            {
                memoryStream.WriteByte(0);
                VarInt.Write(memoryStream, 1);
                VarInt.Write(memoryStream, 1);

                Dictionary<Card, int> cardCounts = GetCardCounts();
                List<Card> extraCards = GetExtraCards();
                List<KeyValuePair<Card, int>> singleCopy = cardCounts.Where(x => x.Value == 1).ToList();
                List<KeyValuePair<Card, int>> doubleCopy = cardCounts.Where(x => x.Value == 2).ToList();
                List<KeyValuePair<Card, int>> nCopy = cardCounts.Where(x => x.Value > 2).ToList();
                singleCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));
                doubleCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));
                nCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));

                VarInt.Write(memoryStream, extraCards.Count);
                foreach (Card card in extraCards)
                    VarInt.Write(memoryStream, card.GetPropertyValueInt(SourceGame.DeckFileAltId));

                VarInt.Write(memoryStream, singleCopy.Count);
                foreach (KeyValuePair<Card, int> cardCount in singleCopy)
                    VarInt.Write(memoryStream, cardCount.Key.GetPropertyValueInt(SourceGame.DeckFileAltId));

                VarInt.Write(memoryStream, doubleCopy.Count);
                foreach (KeyValuePair<Card, int> cardCount in doubleCopy)
                    VarInt.Write(memoryStream, cardCount.Key.GetPropertyValueInt(SourceGame.DeckFileAltId));

                VarInt.Write(memoryStream, nCopy.Count);
                foreach (KeyValuePair<Card, int> cardCount in nCopy)
                {
                    VarInt.Write(memoryStream, cardCount.Key.GetPropertyValueInt(SourceGame.DeckFileAltId));
                    VarInt.Write(memoryStream, cardCount.Value);
                }

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public string ToYdk()
        {
            string text = "#created by Card Game Simulator" + Environment.NewLine;
            List<Card> mainCards = new List<Card>(Cards);
            List<Card> extraCards = GetExtraCards();
            mainCards.RemoveAll(card => extraCards.Contains(card));

            text += "#main" + Environment.NewLine;
            text = mainCards.Aggregate(text,
                (current, card) =>
                    current + (card.GetPropertyValueString(SourceGame.DeckFileAltId) + Environment.NewLine));
            text += "#extra" + Environment.NewLine;
            text = extraCards.Aggregate(text,
                (current, card) =>
                    current + (card.GetPropertyValueString(SourceGame.DeckFileAltId) + Environment.NewLine));

            text += "!side" + Environment.NewLine;
            return text;
        }

        public string ToTxt()
        {
            StringBuilder text = new StringBuilder();
            text.AppendFormat("### {0} Deck: {1} {2}", SourceGame.Name, Name, Environment.NewLine);
            Dictionary<Card, int> cardCounts = GetCardCounts();
            foreach (KeyValuePair<Card, int> cardCount in cardCounts)
            {
                bool isDeckFileTxtIdRequired = !SourceGame.CardNameIsUnique || cardCount.Key.IsReprint;
                text.Append(cardCount.Value);
                text.Append(" ");
                if (isDeckFileTxtIdRequired && SourceGame.DeckFileTxtId == DeckFileTxtId.Id)
                {
                    text.AppendFormat("[{0}]", cardCount.Key.Id);
                    text.Append(" ");
                }

                text.Append(cardCount.Key.Name);
                if (isDeckFileTxtIdRequired && SourceGame.DeckFileTxtId == DeckFileTxtId.Set)
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
