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

        [Test]
        public void LoadCards_ResolvesCardImageUrlJsonPathProperty()
        {
            const string cardImageUrl = "https://api.gatcg.com{editions[0].image}";
            var game = NewCardImageUrlGame("json_path_card_image_url_test_", cardImageUrl);

            try
            {
                var allCards = new JArray
                {
                    new JObject
                    {
                        ["id"] = "json_path_image_card",
                        ["name"] = "Json Path Image Card",
                        ["set"] = Set.DefaultCode,
                        ["editions"] = new JArray
                        {
                            new JObject
                            {
                                ["image"] = "/cards/json_path_image_card.png"
                            }
                        }
                    }
                };

                File.WriteAllText(game.CardsFilePath, allCards.ToString(Formatting.None));
                game.LoadCards(game.CardsFilePath, Set.DefaultCode);

                Assert.IsTrue(game.Cards.TryGetValue("json_path_image_card", out var imageUrlCard));
                Assert.AreEqual("https://api.gatcg.com/cards/json_path_image_card.png", imageUrlCard.ImageWebUrl);
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
