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
        public void LoadCards_UsesBacksThenBackFaceIdFallback()
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
                    ["id"] = "legacy_back",
                    ["name"] = "Legacy",
                    ["set"] = Set.DefaultCode,
                    ["backFaceId"] = "LEGACY_BACK"
                },
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
                    ["backs"] = new JArray("PREFERRED_BACK"),
                    ["backFaceId"] = "LEGACY_SHOULD_NOT_WIN"
                },
                new JObject
                {
                    ["id"] = "default_back",
                    ["name"] = "Default",
                    ["set"] = Set.DefaultCode
                }
            };

            File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
            game.LoadCards(game.CardsFilePath, Set.DefaultCode);

            Assert.IsTrue(game.Cards.TryGetValue("legacy_back", out var legacyCard));
            Assert.AreEqual("LEGACY_BACK", legacyCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("canonical_back", out var canonicalCard));
            Assert.AreEqual("CANON_BACK", canonicalCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("both_back", out var bothCard));
            Assert.AreEqual("PREFERRED_BACK", bothCard.BackFaceId);

            Assert.IsTrue(game.Cards.TryGetValue("default_back", out var defaultCard));
            Assert.IsTrue(string.IsNullOrEmpty(defaultCard.BackFaceId));

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
                    ["id"] = "null_only_back",
                    ["name"] = "Null Only",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray(JValue.CreateNull()),
                    ["backFaceId"] = "LEGACY_NULL_BACK"
                },
                new JObject
                {
                    ["id"] = "mixed_empty_back",
                    ["name"] = "Mixed Empty",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray(string.Empty, JValue.CreateNull()),
                    ["backFaceId"] = "LEGACY_MIXED_BACK"
                },
                new JObject
                {
                    ["id"] = "half_empty_back",
                    ["name"] = "Half Empty",
                    ["set"] = Set.DefaultCode,
                    ["backs"] = new JArray(string.Empty, "BACK_2")
                }
            };

            File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
            game.LoadCards(game.CardsFilePath, Set.DefaultCode);

            Assert.IsTrue(game.Cards.TryGetValue("null_only_back", out var nullOnlyCard));
            Assert.IsTrue(string.IsNullOrEmpty(nullOnlyCard.BackFaceId));

            Assert.IsTrue(game.Cards.TryGetValue("mixed_empty_back", out var mixedEmptyCard));
            Assert.IsTrue(string.IsNullOrEmpty(mixedEmptyCard.BackFaceId));

            Assert.IsTrue(game.Cards.TryGetValue("half_empty_back", out var halfEmptyCard));
            Assert.IsTrue(string.IsNullOrEmpty(halfEmptyCard.BackFaceId));

            Directory.Delete(game.GameDirectoryPath, true);
        }
    }
}

