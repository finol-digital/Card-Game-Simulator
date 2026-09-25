/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class JsonParsingTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LoadCardsAndSets_PreserveOriginalsAndReprintsWhenEntriesOverlap(bool setsFirst)
        {
            var game = new UnityCardGame(null, "overlapping_cards_test_" + Guid.NewGuid())
            {
                CardNameIsUnique = true,
                CardProperties = new List<PropertyDef> { new("rulesText", PropertyType.String) }
            };
            Directory.CreateDirectory(game.GameDirectoryPath);

            try
            {
                var cards = new JArray
                {
                    new JObject { ["id"] = "original", ["name"] = "Shared Name", ["set"] = "DR1" },
                    new JObject { ["id"] = "reprint", ["name"] = "Shared Name", ["set"] = "DR1" },
                    new JObject { ["id"] = "unique", ["name"] = "Unique Name", ["set"] = "DR1" }
                };
                File.WriteAllText(game.CardsFilePath, cards.ToString(Formatting.None));
                File.WriteAllText(game.SetsFilePath, new JArray
                {
                    new JObject { ["code"] = "DR1", ["name"] = "Card Pool", ["cards"] = cards.DeepClone() }
                }.ToString(Formatting.None));

                // Both source orders and repeated refreshes must retain the same visible originals.
                for (var pass = 0; pass < 2; pass++)
                {
                    if (setsFirst)
                        game.LoadSets();
                    game.LoadCards(game.CardsFilePath, Set.DefaultCode);
                    game.LoadSets();

                    Assert.IsTrue(string.IsNullOrEmpty(game.Error), game.Error);
                    Assert.AreEqual(3, game.Cards.Count);
                    Assert.IsFalse(game.Cards["original"].IsReprint);
                    Assert.IsTrue(game.Cards["reprint"].IsReprint);
                    Assert.IsFalse(game.Cards["unique"].IsReprint);
                }

                // Duplicate entries may contain updated data; do not simply skip them.
                cards[0]["rulesText"] = "Updated rules";
                File.WriteAllText(game.CardsFilePath, cards.ToString(Formatting.None));
                game.LoadCards(game.CardsFilePath, Set.DefaultCode);
                Assert.AreEqual("Updated rules", game.Cards["original"].GetPropertyValueString("rulesText"));
                Assert.IsFalse(game.Cards["original"].IsReprint);
            }
            finally
            {
                Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LoadCards_PreservesMultipleSetPrintingsWhenReloaded(bool uniqueNames)
        {
            var game = new UnityCardGame(null, "multiple_set_reprints_test_" + Guid.NewGuid())
            {
                CardNameIsUnique = uniqueNames,
                CardSetsInList = true,
                CardProperties = new List<PropertyDef>()
            };
            Directory.CreateDirectory(game.GameDirectoryPath);

            try
            {
                File.WriteAllText(game.CardsFilePath, new JArray
                {
                    new JObject
                    {
                        ["id"] = "multi_set", ["name"] = "Multiple Printings",
                        ["set"] = new JArray("DR1", "DR2")
                    }
                }.ToString(Formatting.None));

                for (var pass = 0; pass < 2; pass++)
                {
                    game.LoadCards(game.CardsFilePath, Set.DefaultCode);
                    Assert.IsTrue(string.IsNullOrEmpty(game.Error), game.Error);
                    Assert.IsFalse(game.Cards["multi_set"].IsReprint);
                    Assert.AreEqual(uniqueNames ? 2 : 1, game.Cards.Count);
                    if (uniqueNames)
                    {
                        Assert.AreEqual("DR1", game.Cards["multi_set"].SetCode);
                        Assert.AreEqual("DR2", game.Cards["multi_set.DR2"].SetCode);
                        Assert.IsTrue(game.Cards["multi_set.DR2"].IsReprint);
                    }
                }
            }
            finally
            {
                Directory.Delete(game.GameDirectoryPath, true);
            }
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

            try
            {
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
            }
            finally
            {
                Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        [Test]
        public void LoadCards_WarnsWhenCardImageUrlHasUnresolvableProperty()
        {
            const string cardImageUrl = "https://cgs.games/api/proxy/{card.image_url_unresolvable}";
            var game = NewCardImageUrlGame("unresolvable_card_image_url_test_", cardImageUrl);

            try
            {
                LogAssert.Expect(LogType.Warning,
                    "LoadCardFromJToken::UnresolvedCardImageUrlProperty:card.image_url_unresolvable" +
                    " in cardImageUrl " + cardImageUrl +
                    " is not a cardProperty, so it will be replaced with an empty string");

                WriteAndLoadCardImageUrlCard(game);

                Assert.IsTrue(game.Cards.TryGetValue("image_url_card", out var imageUrlCard));
                Assert.AreEqual("https://cgs.games/api/proxy/", imageUrlCard.ImageWebUrl);
            }
            finally
            {
                Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        [Test]
        public void LoadCards_DoesNotWarnWhenCardImageUrlPropertyResolves()
        {
            var game = NewCardImageUrlGame("resolvable_card_image_url_test_",
                "https://cgs.games/api/proxy/{image_url}");

            try
            {
                WriteAndLoadCardImageUrlCard(game);

                Assert.IsTrue(game.Cards.TryGetValue("image_url_card", out var imageUrlCard));
                Assert.AreEqual("https://cgs.games/api/proxy/https://example.com/image_url_card.png",
                    imageUrlCard.ImageWebUrl);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Directory.Delete(game.GameDirectoryPath, true);
            }
        }

        private static UnityCardGame NewCardImageUrlGame(string idPrefix, string cardImageUrl)
        {
            var game = new UnityCardGame(null, idPrefix + Guid.NewGuid())
            {
                CardImageUrl = cardImageUrl,
                CardProperties = new List<PropertyDef>
                {
                    new("image_url", PropertyType.String)
                }
            };

            if (Directory.Exists(game.GameDirectoryPath))
                Directory.Delete(game.GameDirectoryPath, true);
            Directory.CreateDirectory(game.GameDirectoryPath);

            return game;
        }

        private static void WriteAndLoadCardImageUrlCard(UnityCardGame game)
        {
            var allCards = new JArray
            {
                new JObject
                {
                    ["id"] = "image_url_card",
                    ["name"] = "Image Url Card",
                    ["set"] = Set.DefaultCode,
                    ["image_url"] = "https://example.com/image_url_card.png"
                }
            };

            File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
            game.LoadCards(game.CardsFilePath, Set.DefaultCode);
        }
    }
}
