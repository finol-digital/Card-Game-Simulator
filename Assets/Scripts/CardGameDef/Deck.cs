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
using JetBrains.Annotations;
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
        [EnumMember(Value = "lor")] Lor,
        [EnumMember(Value = "txt")] Txt,
        [EnumMember(Value = "ydk")] Ydk
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeckFileTxtId
    {
        [EnumMember(Value = "id")] Id,
        [EnumMember(Value = "set")] Set
    }

    [PublicAPI]
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

        public Dictionary<Card, int> DetermineCardCounts(IReadOnlyCollection<Card> cards)
        {
            var cardCounts = new Dictionary<Card, int>();
            foreach (var card in cards)
            {
                cardCounts.TryGetValue(card, out var currentCount);
                currentCount++;
                cardCounts[card] = currentCount;
            }

            return cardCounts;
        }

        public Dictionary<string, List<Card>> GetExtraGroups()
        {
            var extraGroups = new Dictionary<string, List<Card>>();
            foreach (var card in Cards)
            {
                var applicableExtraGroups = from extraDef in SourceGame.Extras
                    where SourceGame.IsEnumProperty(extraDef.Property)
                        ? card.GetPropertyValueString(extraDef.Property).Contains(extraDef.Value)
                        : card.GetPropertyValueString(extraDef.Property).Equals(extraDef.Value)
                    select !string.IsNullOrEmpty(extraDef.Group)
                        ? extraDef.Group
                        : ExtraDef.DefaultExtraGroup;
                var cardExtraGroup = applicableExtraGroups.FirstOrDefault();
                if (string.IsNullOrEmpty(cardExtraGroup))
                    continue;
                if (!extraGroups.ContainsKey(cardExtraGroup))
                    extraGroups[cardExtraGroup] = new List<Card>();
                extraGroups[cardExtraGroup].Add(card);
            }

            return extraGroups;
        }

        public List<Card> GetExtraCards()
        {
            var extraCards = new List<Card>();
            foreach (var cardGroup in GetExtraGroups())
                extraCards.AddRange(cardGroup.Value);
            return extraCards;
        }

        public string ToDec()
        {
            var text = string.Empty;
            var cardCounts = DetermineCardCounts(Cards);
            return cardCounts.Keys.Aggregate(text,
                (current, card) => current + (cardCounts[card] + " " + card.Name + Environment.NewLine));
        }

        public string ToHsd()
        {
            var text = "### " + Name + Environment.NewLine;
            var extraCards = GetExtraCards();
            if (extraCards.Count > 0 && !string.IsNullOrEmpty(extraCards[0].GetPropertyValueString("cardClass")))
                text += "# Class: " + extraCards[0].GetPropertyValueString("cardClass") + Environment.NewLine;
            text += "# Format: Wild" + Environment.NewLine;
            text += "#" + Environment.NewLine;

            var cardCounts = DetermineCardCounts(Cards);
            text = cardCounts.Keys.Where(card => !extraCards.Contains(card)).Aggregate(text,
                (current, card) => current + ("# " + cardCounts[card] + "x (" + card.GetPropertyValueString("cost") +
                                              ") " + card.Name + Environment.NewLine));
            text += "#" + Environment.NewLine;

            text += SerializeHsd() + Environment.NewLine;
            return text;
        }

        public string SerializeHsd()
        {
            using var memoryStream = new MemoryStream();
            memoryStream.WriteByte(0);
            Varint.Write(memoryStream, 1);
            Varint.Write(memoryStream, 1);

            var cardCounts = DetermineCardCounts(Cards);
            var extraCards = GetExtraCards();
            var singleCopy = cardCounts.Where(x => x.Value == 1).ToList();
            var doubleCopy = cardCounts.Where(x => x.Value == 2).ToList();
            var nCopy = cardCounts.Where(x => x.Value > 2).ToList();
            singleCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));
            doubleCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));
            nCopy.RemoveAll(cardCount => extraCards.Contains(cardCount.Key));

            Varint.Write(memoryStream, extraCards.Count);
            foreach (var card in extraCards)
                Varint.Write(memoryStream, card.GetPropertyValueInt(SourceGame.DeckFileAltId));

            Varint.Write(memoryStream, singleCopy.Count);
            foreach (var cardCount in singleCopy)
                Varint.Write(memoryStream, cardCount.Key.GetPropertyValueInt(SourceGame.DeckFileAltId));

            Varint.Write(memoryStream, doubleCopy.Count);
            foreach (var cardCount in doubleCopy)
                Varint.Write(memoryStream, cardCount.Key.GetPropertyValueInt(SourceGame.DeckFileAltId));

            Varint.Write(memoryStream, nCopy.Count);
            foreach (var cardCount in nCopy)
            {
                Varint.Write(memoryStream, cardCount.Key.GetPropertyValueInt(SourceGame.DeckFileAltId));
                Varint.Write(memoryStream, cardCount.Value);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public string ToLor()
        {
            var cardCounts = DetermineCardCounts(Cards);
            var cardCodeAndCounts = cardCounts.Select(
                cardCount => new CardCodeAndCount()
                {
                    CardCode = cardCount.Key.Id, Count = cardCount.Value
                }).ToList();
            return LoRDeckEncoder.GetCodeFromDeck(cardCodeAndCounts) + Environment.NewLine;
        }

        public string ToYdk()
        {
            var text = "#created by Card Game Simulator" + Environment.NewLine;
            var mainCards = new List<Card>(Cards);
            var extraCards = GetExtraCards();
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
            var text = new StringBuilder();
            text.AppendFormat("### {0} Deck: {1}{2}", SourceGame.Name, Name, Environment.NewLine);
            var mainCards = new List<Card>(Cards);
            var extraGroups = GetExtraGroups();
            var extraCards = new List<Card>();
            foreach (var cardGroup in GetExtraGroups())
                extraCards.AddRange(cardGroup.Value);
            mainCards.RemoveAll(card => extraCards.Contains(card));

            if (extraGroups.Count > 0)
            {
                foreach (var extraGroup in extraGroups)
                {
                    text.AppendFormat("## {0}{1}", extraGroup.Key, Environment.NewLine);
                    var extraCardCounts = DetermineCardCounts(extraGroup.Value);
                    foreach (var cardCount in extraCardCounts)
                        BuildTxtLine(text, cardCount);
                }

                text.AppendFormat("## Main Deck{0}", Environment.NewLine);
            }

            var mainCardCounts = DetermineCardCounts(mainCards);
            foreach (var cardCount in mainCardCounts)
                BuildTxtLine(text, cardCount);

            return text.ToString();
        }

        private void BuildTxtLine(StringBuilder text, KeyValuePair<Card, int> cardCount)
        {
            var isDeckFileTxtIdRequired = !SourceGame.CardNameIsUnique || cardCount.Key.IsReprint;
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
                case DeckFileType.Lor:
                    text = ToLor();
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

        public virtual bool Equals(Deck other)
        {
            return other != null && ToString().Equals(other.ToString());
        }
    }
}
