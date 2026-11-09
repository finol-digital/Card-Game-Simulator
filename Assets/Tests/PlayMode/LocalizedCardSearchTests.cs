/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;

namespace Tests.PlayMode
{
    public class LocalizedCardSearchTests
    {
        private static UnityCardGame Game()
        {
            var game = new UnityCardGame(null, "Search boundary");
            var rules = new PropertyDef("rules", PropertyType.String);
            var power = new PropertyDef("power", PropertyType.Integer);
            var enabled = new PropertyDef("enabled", PropertyType.Boolean);
            game.CardProperties.AddRange(new[] { rules, power, enabled });
            foreach (var id in new[] { "1", "2" })
                game.Add(new UnityCard(game, id, id == "1" ? "Flame" : "Ash", "BASE",
                    new Dictionary<string, PropertyDefValuePair>
                    {
                        ["rules"] = new() { Def = rules, Value = "Deal damage" },
                        ["power"] = new() { Def = power, Value = id },
                        ["enabled"] = new() { Def = enabled, Value = id == "1" ? "true" : "false" }
                    }, false), false);
            game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""en"",""locales"":{""es"":{""inline"":{""cardNames"":{""Flame"":""Ash""},""cards"":{""1"":{""properties"":{""rules"":""Inflige daño""}}}}}}}").Value;
            return game;
        }

        [Test]
        public void OnlyInteractiveSearchUnionsCanonicalAndTranslatedNames()
        {
            var game = Game();
            var display = new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "es");
            var filters = new CardSearchFilters { Name = "Ash" };
            Assert.That(LocalizedCardSearch.Search(game, filters, display).Select(c => c.Id), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(game.FilterCards(filters).Single().Id, Is.EqualTo("2"));
            Assert.That(UnityDeck.Parse(game, "User", DeckFileType.Txt, "1 Ash").Cards.Single().Id, Is.EqualTo("2"));
            filters.Name = "Flame";
            Assert.That(LocalizedCardSearch.Search(game, filters, display).Single().Id, Is.EqualTo("1"));
            filters.Name = "Ash";
            var japanese = new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "ja");
            Assert.That(LocalizedCardSearch.Search(game, filters, japanese).Single().Id, Is.EqualTo("2"));
        }

        [Test]
        public void TranslatedTextHonorsTypedFiltersAndNeverMutatesControls()
        {
            var game = Game();
            var display = new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "es");
            var filters = new CardSearchFilters { SetCode = "BASE", Id = "1" };
            filters.StringProperties.Add("rules", "daño");
            filters.IntMinProperties.Add("power", 1);
            filters.IntMaxProperties.Add("power", 1);
            filters.BoolProperties.Add("enabled", true);
            var before = filters.ToString();
            Assert.That(LocalizedCardSearch.Search(game, filters, display).Single().Id, Is.EqualTo("1"));
            Assert.That(filters.ToString(), Is.EqualTo(before));
            Assert.That(game.FilterCards(filters), Is.Empty);
            filters.IntMinProperties["power"] = 2;
            Assert.That(LocalizedCardSearch.Search(game, filters, display), Is.Empty);
            filters.IntMinProperties["power"] = 1;
            filters.StringProperties["rules"] = "damage";
            Assert.That(LocalizedCardSearch.Search(game, filters, display).Single().Id, Is.EqualTo("1"));
        }

        [Test]
        public void UnlocalizedSearchUsesExistingBehaviorAndOrder()
        {
            var game = Game();
            game.Localization = null;
            var filters = new CardSearchFilters { Name = "Ash" };
            Assert.That(LocalizedCardSearch.Search(game, filters, null), Is.EqualTo(game.FilterCards(filters)));
        }
    }
}
