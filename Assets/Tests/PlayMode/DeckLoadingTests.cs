/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cgs;
using Cgs.Decks;
using Cgs.Localization;
using Cgs.Menu;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Decks;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class DeckLoadingTests
    {
        private static UnityCardGame NewGame(PropertyType idType = PropertyType.String)
        {
            var game = new UnityCardGame(null, "deck_loading_test")
            {
                DeckFileAltId = "id",
                CardProperties = new List<PropertyDef> { new("id", idType) }
            };
            var properties = new Dictionary<string, PropertyDefValuePair>
            {
                ["id"] = new() { Def = game.CardProperties[0], Value = "1" }
            };
            game.Add(new UnityCard(game, "1", "Known Card", Set.DefaultCode, properties, false), false);
            return game;
        }

        [Test]
        public void Ydk_ReportsMissingCopiesAndPreservesKnownCardsAndExtraDeck()
        {
            var deck = UnityDeck.Parse(NewGame(), "Kaiba", DeckFileType.Ydk,
                "#main\r\n1\r\n97590748\r\n97590748\r\n#extra\r\n1\r\n!side\r\n999\r\n");
            Assert.That(deck.Cards.Count, Is.EqualTo(2));
            Assert.That(deck.MissingCards.Count, Is.EqualTo(1));
            Assert.That(deck.MissingCards["97590748"], Is.EqualTo(2));
        }

        [TestCase(DeckFileType.Txt)]
        [TestCase(DeckFileType.Dec)]
        public void NamedDeck_ReportsQuantitiesAndNamesWithoutQuantities(DeckFileType type)
        {
            var deck = UnityDeck.Parse(NewGame(), "Named deck", type,
                "// Comment\n2 Known Card\nKnown Card\n3 Missing Card\nMissing Card\n0 Excluded Card\n");
            Assert.That(deck.Cards.Count, Is.EqualTo(3));
            Assert.That(deck.MissingCards.Count, Is.EqualTo(1));
            Assert.That(deck.MissingCards["Missing Card"], Is.EqualTo(4));
        }

        [TestCase(DeckFileType.Txt, "# Comment\n1 Known Card\nSideboard\n2 Missing Card\n")]
        [TestCase(DeckFileType.Dec, "// Comment\n1 Known Card\nSB: 2 Missing Card\n")]
        [TestCase(DeckFileType.Ydk, "#main\n1\n!side\n999\n")]
        public void CompleteDeck_DoesNotReportCommentsOrSideboard(DeckFileType type, string text)
        {
            var deck = UnityDeck.Parse(NewGame(), "Complete", type, text);
            Assert.That(deck.Cards.Count, Is.EqualTo(1));
            Assert.That(deck.MissingCards, Is.Empty);
        }

        [Test]
        public void Txt_ReportsMissingExplicitId()
        {
            var deck = UnityDeck.Parse(NewGame(), "Named deck", DeckFileType.Txt, "2 [999] Missing Card");
            Assert.That(deck.Cards, Is.Empty);
            Assert.That(deck.MissingCards["999"], Is.EqualTo(2));
        }

        [Test]
        public void Hsd_ReportsMissingCopiesFromEncodedDeck()
        {
            // Header, one hero, one single card, one double card, no multi-copy cards.
            var code = Convert.ToBase64String(new byte[] { 0, 1, 2, 1, 0, 1, 1, 1, 99, 0 });
            var deck = UnityDeck.Parse(NewGame(PropertyType.Integer), "Encoded", DeckFileType.Hsd, code);
            Assert.That(deck.Cards.Count, Is.EqualTo(1));
            Assert.That(deck.MissingCards["99"], Is.EqualTo(2));
        }

        [Test]
        public void Lor_ReportsMissingCopiesFromEncodedDeck()
        {
            var code = LoRDeckEncoder.GetCodeFromDeck(new List<CardCodeAndCount>
            {
                new() { CardCode = "01DE001", Count = 3 }
            });
            var deck = UnityDeck.Parse(NewGame(), "Encoded", DeckFileType.Lor, code);
            Assert.That(deck.Cards, Is.Empty);
            Assert.That(deck.MissingCards["01DE001"], Is.EqualTo(3));
        }

        [Test]
        public void MissingCardsWarning_QueuesOneMessageAndCompleteDeckStaysQuiet()
        {
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            var instance = typeof(CardGameManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            var root = new GameObject("Inactive deck warning test");
            root.SetActive(false);
            try
            {
                var manager = root.AddComponent<CardGameManager>();
                instance.SetValue(null, manager);
                var dialogObject = new GameObject("Queued deck warning");
                dialogObject.transform.SetParent(root.transform);
                var dialog = dialogObject.AddComponent<Dialog>();
                typeof(CardGameManager).GetField("messenger", privateInstance).SetValue(manager, dialog);
                DeckLoadMenu.ShowMissingCardsWarning(UnityDeck.Parse(NewGame(), "Complete", DeckFileType.Ydk, "1"));
                DeckLoadMenu.ShowMissingCardsWarning(UnityDeck.Parse(NewGame(), "Kaiba", DeckFileType.Ydk,
                    "1\n97590748\n97590748\n999"));
                var messages = ((IEnumerable)typeof(Dialog).GetProperty("MessageQueue", privateInstance)
                    .GetValue(dialog)).Cast<object>().ToArray();
                Assert.That(messages, Has.Length.EqualTo(1));
                var message = (UiMessage)messages[0].GetType().GetField("Localized").GetValue(messages[0]);
                Assert.That(message.Key, Is.EqualTo("decks.load.missing"));
                StringAssert.Contains("Kaiba", message.English);
                StringAssert.Contains("Loaded cards: 1", message.English);
                StringAssert.Contains("Missing cards: 3", message.English);
                StringAssert.Contains("2 × 97590748", message.English);
                StringAssert.Contains("1 × 999", message.English);
            }
            finally
            {
                Object.DestroyImmediate(root);
                instance.SetValue(null, previous);
            }
        }
    }
}
