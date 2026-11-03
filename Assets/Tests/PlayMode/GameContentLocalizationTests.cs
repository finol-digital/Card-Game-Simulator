/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.PlayMode
{
    public class GameContentLocalizationTests
    {
        private static UnityCardGame CreateGame()
        {
            var game = new UnityCardGame(null, "Localization boundary")
            {
                CardImageUrl = "https://example.invalid/{cardName}/{cardId}.png",
                AutoUpdate = -1
            };
            game.Add(new UnityCard(game, "1", "Flame", "BASE", new Dictionary<string, PropertyDefValuePair>(), false), false);
            game.Add(new UnityCard(game, "2", "Ash", "BASE", new Dictionary<string, PropertyDefValuePair>(), false), false);
            JsonConvert.PopulateObject(@"{""name"":""Localization boundary"",""localization"":{""defaultLocale"":""en"",""locales"":{""es"":{""inline"":{""game"":{""name"":""Juego""},""cardNames"":{""Flame"":""Ash"",""Ash"":""Ceniza""}}}}}}", game);
            return game;
        }

        [TestCase(DeckFileType.Txt)]
        [TestCase(DeckFileType.Dec)]
        public void DisplayCollisionDoesNotChangeActualDeckImportsOrCanonicalQueries(DeckFileType format)
        {
            var game = CreateGame();
            var card = game.Cards["1"];
            var image = card.ImageWebUrl;
            var gameId = game.Id;
            var before = UnityDeck.Parse(game, "User deck", format, "1 Ash\n");
            var export = before.ToString();
            var display = new LocalizationContext(new LocalizationTargets(game, game.Cards.Values, game.Sets.Values), "es");

            Assert.That(display.GameName, Is.EqualTo("Juego"));
            Assert.That(display.CardName(card), Is.EqualTo("Ash"));
            Assert.That(display.DeckName("User deck", false), Is.EqualTo("User deck"));
            Assert.That(UnityDeck.Parse(game, "User deck", format, "1 Ash\n").Cards.Single().Id, Is.EqualTo("2"));
            Assert.That(game.FilterCards(new CardSearchFilters { Name = "Ash" }).Single().Id, Is.EqualTo("2"));
            Assert.That(before.ToString(), Is.EqualTo(export));
            Assert.That(card.Name, Is.EqualTo("Flame"));
            Assert.That(card.ImageWebUrl, Is.EqualTo(image));
            Assert.That(game.Id, Is.EqualTo(gameId));
            Assert.That(game.Name, Is.EqualTo("Localization boundary"));
        }

        [Test]
        public void UnlocalizedUnityGamesHaveNoLocalizationCandidatesOrSerializedMetadata()
        {
            var game = new UnityCardGame(null, "元のゲーム");
            var display = new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "es");
            Assert.That(display.GameName, Is.EqualTo(game.Name));
            Assert.That(display.LocaleChain, Is.Empty);
            Assert.That(display.AssetCandidates("bannerImageUrl"), Is.Empty);
            Assert.That(JObject.Parse(JsonConvert.SerializeObject(game))["localization"], Is.Null);
        }

        [Test]
        public void InvalidOptionalManifestDoesNotPreventCanonicalUnityParsing()
        {
            var game = new UnityCardGame(null, "Base");
            JsonConvert.PopulateObject(@"{""name"":""Base"",""localization"":{""defaultLocale"":""bad_tag""},""copyright"":""Owner""}", game);
            Assert.That(game.Name, Is.EqualTo("Base"));
            Assert.That(game.Copyright, Is.EqualTo("Owner"));
            Assert.That(game.Localization.IsUsable, Is.False);
            Assert.That(new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "es").GameName,
                Is.EqualTo("Base"));
        }
    }
}
