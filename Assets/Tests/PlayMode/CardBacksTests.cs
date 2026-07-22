/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.PlayMode
{
    public class CardBacksTests
    {
        [Test]
        public void WriteAllCardsJson_WritesSchemaAlignedBacks()
        {
            var game = new UnityCardGame(null, "write_backs_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>()
            };

            if (Directory.Exists(game.GameDirectoryPath))
                Directory.Delete(game.GameDirectoryPath, true);
            Directory.CreateDirectory(game.GameDirectoryPath);

            var withBack = new UnityCard(game, "card_with_back", "With Back", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false, false, "DARK_BACK");
            var defaultBack = new UnityCard(game, "card_default_back", "Default Back", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false);

            game.Add(withBack, false);
            game.Add(defaultBack, false);
            game.WriteAllCardsJson();

            var allCards = JArray.Parse(File.ReadAllText(game.CardsFilePath));
            var withBackToken = allCards.Children<JObject>().First(token =>
                string.Equals(token.Value<string>("id"), "card_with_back", StringComparison.Ordinal));
            var defaultBackToken = allCards.Children<JObject>().First(token =>
                string.Equals(token.Value<string>("id"), "card_default_back", StringComparison.Ordinal));

            Assert.AreEqual("DARK_BACK", withBackToken["backs"]?[0]?.Value<string>());
            Assert.IsNull(withBackToken["backFaceId"]);
            Assert.IsNull(defaultBackToken["backs"]);
            Assert.IsNull(defaultBackToken["backFaceId"]);

            Directory.Delete(game.GameDirectoryPath, true);
        }

        [Test]
        public void LoadCards_UsesBacksAndFallsBackToBackFaceId()
        {
            var game = new UnityCardGame(null, "load_backs_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>()
            };

            if (Directory.Exists(game.GameDirectoryPath))
                Directory.Delete(game.GameDirectoryPath, true);
            Directory.CreateDirectory(game.GameDirectoryPath);

            var allCards = new JArray
            {
                new JObject
                {
                    ["id"] = "canonical_back",
                    ["name"] = "Canonical",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray("CANON_BACK")
                },
                new JObject
                {
                    ["id"] = "both_back",
                    ["name"] = "Both",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray("BACK_1", "BACK_2"),
                    ["backFaceId"] = "LEGACY_SHOULD_NOT_WIN"
                },
                new JObject
                {
                    ["id"] = "legacy_back",
                    ["name"] = "Legacy",
                    ["set"] = Set.DefaultCode,
                    ["backFaceId"] = "LEGACY_BACK"
                }
            };

            File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
            game.LoadCards(game.CardsFilePath, Set.DefaultCode);

            Assert.IsTrue(game.Cards.TryGetValue("canonical_back", out var canonicalCard));
            Assert.AreEqual("CANON_BACK", canonicalCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("both_back", out var bothCard));
            Assert.AreEqual("BACK_1", bothCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("both_back.BACK_2", out var bothCard2));
            Assert.AreEqual("BACK_2", bothCard2.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("legacy_back", out var legacyBack));
            Assert.AreEqual("LEGACY_BACK", legacyBack.BackFaceId);

            Directory.Delete(game.GameDirectoryPath, true);
        }

        [Test]
        public void LoadCards_UsesDefaultBackWhenBacksAreUnusable()
        {
            var game = new UnityCardGame(null, "load_unusable_backs_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>()
            };

            if (Directory.Exists(game.GameDirectoryPath))
                Directory.Delete(game.GameDirectoryPath, true);
            Directory.CreateDirectory(game.GameDirectoryPath);

            var allCards = new JArray
            {
                new JObject
                {
                    ["id"] = "default_back",
                    ["name"] = "Default",
                    ["set"] = Set.DefaultCode
                },
                new JObject
                {
                    ["id"] = "null_only_back",
                    ["name"] = "Null Only",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray(JValue.CreateNull()),
                    ["backFaceId"] = "LEGACY_NULL_BACK"
                },
                new JObject
                {
                    ["id"] = "string_empty_back",
                    ["name"] = "String Empty",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray(string.Empty),
                    ["backFaceId"] = "LEGACY_STRING_EMPTY_BACK"
                },
                new JObject
                {
                    ["id"] = "double_back",
                    ["name"] = "Double Empty",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray(string.Empty, "BACK_2"),
                    ["backFaceId"] = "LEGACY_DOUBLE_BACK"
                }
            };

            File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
            game.LoadCards(game.CardsFilePath, Set.DefaultCode);

            Assert.IsTrue(game.Cards.TryGetValue("default_back", out var defaultCard));
            Assert.AreEqual("", defaultCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("null_only_back", out var nullOnlyCard));
            Assert.AreEqual("", nullOnlyCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("string_empty_back", out var stringEmptyCard));
            Assert.AreEqual("", stringEmptyCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("double_back", out var doubleBackCard));
            Assert.AreEqual("", doubleBackCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("double_back.BACK_2", out var doubleBackCard2));
            Assert.AreEqual("BACK_2", doubleBackCard2.BackFaceId);

            Directory.Delete(game.GameDirectoryPath, true);
        }

        [Test]
        public void LoadCards_NormalizesJsonLineBreakTokensInStringProperties()
        {
            var game = new UnityCardGame(null, "load_line_break_tokens_test_" + Guid.NewGuid())
            {
                CardProperties = new List<PropertyDef>
                {
                    new("rulesText", PropertyType.String)
                }
            };

            if (Directory.Exists(game.GameDirectoryPath))
                Directory.Delete(game.GameDirectoryPath, true);
            Directory.CreateDirectory(game.GameDirectoryPath);

            var allCards = new JArray
            {
                new JObject
                {
                    ["id"] = "line_break_card",
                    ["name"] = "Line Break Card",
                    ["set"] = Set.DefaultCode,
                    ["rulesText"] = "One[br]Two<br>Three<br/>Four<br />Five"
                }
            };

            File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
            game.LoadCards(game.CardsFilePath, Set.DefaultCode);

            Assert.IsTrue(game.Cards.TryGetValue("line_break_card", out var lineBreakCard));
            Assert.AreEqual("One\nTwo\nThree\nFour\nFive", lineBreakCard.GetPropertyValueString("rulesText"));

            Directory.Delete(game.GameDirectoryPath, true);
        }
    }
}
