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
    }
}
