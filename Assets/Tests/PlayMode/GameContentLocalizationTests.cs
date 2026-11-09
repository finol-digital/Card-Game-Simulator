/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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

        [Test]
        public void DuplicateInlineMembersKeepSiblingTranslationsAndAuthoredSourceInUnity()
        {
            const string broken = @"{""game"":{""name"":""a"",""name"":""b""}}";
            var game = new UnityCardGame(null, "Base");
            JsonConvert.PopulateObject("{\"name\":\"Base\",\"localization\":{\"defaultLocale\":\"es\",\"locales\":{\"es\":{\"inline\":{\"game\":{\"name\":\"Juego\"}}},\"fr\":{\"inline\":" + broken + "}}}}", game);
            Assert.That(game.Localization.IsUsable, Is.True);
            var display = new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "fr");
            Assert.That(display.GameName, Is.EqualTo("Juego"));
            Assert.That(JsonConvert.SerializeObject(game), Does.Contain(broken));
        }

        [TestCase("rules.html#setup", "https://example.invalid/game/rules.html#setup")]
        [TestCase("#setup", "https://example.invalid/game/cgs.json#setup")]
        [TestCase("rules.html?lang=es#setup", "https://example.invalid/game/rules.html?lang=es#setup")]
        [TestCase("es%20%23%20%E6%97%A5%E6%9C%AC%E8%AA%9E.json", "https://example.invalid/game/es%20%23%20%E6%97%A5%E6%9C%AC%E8%AA%9E.json")]
        public void UnityResolvesRelativeFragments(string reference, string expected)
        {
            var origin = LocalizationOrigin.Remote(new Uri("https://example.invalid/game/cgs.json"));
            Assert.That(LocalizationOrigin.Resolve(reference, origin)?.AbsoluteUri, Is.EqualTo(expected));
        }

        [TestCase("a\n")]
        [TestCase("http:///foo")]
        [TestCase("https://?x")]
        [TestCase("http://example.invalid:99999/x")]
        [TestCase("es%2.json")]
        [TestCase("es%GG.json")]
        public void UnityRejectsMalformedResourceReferences(string reference)
        {
            var origin = LocalizationOrigin.Remote(new Uri("https://example.invalid/game/cgs.json"));
            Assert.That(LocalizationOrigin.Resolve(reference, origin), Is.Null);
        }

        [Test]
        public void UnityBundledFragmentsRetainTheContainingFile()
        {
            var root = Path.Combine(Path.GetTempPath(), "cgs-fragment-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var document = Path.Combine(root, "cgs.json");
                File.WriteAllText(document, "{}");
                var origin = LocalizationOrigin.Bundled(root, document,
                    path => LocalizationFiles.ExistingPhysicalPath(path, root));
                var resolved = LocalizationOrigin.Resolve("#setup", origin);
                Assert.That(resolved.LocalPath, Is.EqualTo(document));
                Assert.That(resolved.Fragment, Is.EqualTo("#setup"));
            }
            finally { Directory.Delete(root, true); }
        }

        [TestCase("https:es.json")]
        [TestCase("HTTPS:es.json")]
        [TestCase("https:/es.json")]
        [TestCase("https:?x")]
        [TestCase("https:#x")]
        [TestCase("http:es.json")]
        [TestCase("1a:b")]
        public void UnityRejectsSchemeLikeRelativeReferences(string reference)
        {
            var manifest = LocalizationJson.ReadManifest("{\"defaultLocale\":\"en\",\"locales\":{\"es\":{\"url\":\"" + reference + "\"}}}");
            Assert.That(manifest.IsValid, Is.False);
            var game = CreateGame();
            JsonConvert.PopulateObject("{\"name\":\"Base\",\"localization\":{\"defaultLocale\":\"es\",\"locales\":{\"es\":{\"inline\":{\"assets\":{\"rulesUrl\":\"" + reference + "\"}}}}}}", game);
            foreach (var scheme in new[] { "http", "https" })
            {
                var origin = LocalizationOrigin.Remote(new Uri(scheme + "://cdn.example/g/cgs.json"));
                Assert.That(LocalizationOrigin.Resolve(reference, origin), Is.Null);
                var display = new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "es",
                    definitionOrigin: origin);
                Assert.That(display.AssetCandidates("rulesUrl"), Is.Empty);
            }
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

        [TestCase("42")]
        [TestCase(@"{""defaultLocale"":""en"",""future"":{""version"":2}}")]
        [TestCase(@"{""defaultLocale"":""en"",""locales"":{""es"":{""inline"":{""game"":{""name"":42}}}}}")]
        public void SavingUnityGameRetainsUneditedAuthoredLocalization(string manifest)
        {
            var game = new UnityCardGame(null, "Base");
            JsonConvert.PopulateObject("{\"name\":\"Base\",\"localization\":" + manifest + "}", game);
            game.Copyright = "Edited owner";
            var written = JObject.Parse(JsonConvert.SerializeObject(game, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));
            Assert.That(JToken.DeepEquals(written["localization"], JToken.Parse(manifest)), Is.True);
            Assert.That(written["copyright"].Value<string>(), Is.EqualTo("Edited owner"));
            game.Localization.DefaultLocale = "ja";
            var edited = JObject.Parse(JsonConvert.SerializeObject(game));
            Assert.That(edited["localization"]["defaultLocale"].Value<string>(), Is.EqualTo("ja"));
        }
    }
}
