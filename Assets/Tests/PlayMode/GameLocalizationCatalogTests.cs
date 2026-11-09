/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using Cgs.GameContent;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class GameLocalizationCatalogTests
    {
        private UnityCardGame _game;

        [SetUp]
        public void SetUp()
        {
            _game = new UnityCardGame(null, "Catalog-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_game.GameDirectoryPath);
        }

        [TearDown]
        public void CleanUp()
        {
            LocalizationCache.Invalidate(_game.GameDirectoryPath);
            Directory.Delete(_game.GameDirectoryPath, true);
        }

        private void WriteManifest(string source)
        {
            File.WriteAllText(_game.GameFilePath, new JObject
            {
                ["name"] = _game.Name, ["autoUpdate"] = -1,
                ["localization"] = new JObject
                {
                    ["defaultLocale"] = "en",
                    ["locales"] = new JObject { ["es"] = new JObject { ["url"] = source } }
                }
            }.ToString());
            _game.ReadProperties();
            Assert.That(_game.HasReadProperties, Is.True, _game.Error);
        }

        [Test]
        public void CompleteVerificationFixtureLoadsExternalCatalogsAndRollsBackWithoutChangingCards()
        {
            var fixture = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../developer-docs/fixtures/localization-verification"));
            LocalizationFiles.CopyPackageSnapshot(fixture, _game.GameDirectoryPath);
            var manifest = JObject.Parse(File.ReadAllText(_game.GameFilePath));
            // Keep the test's isolated installation identity; never touch a player's fixture install.
            manifest["name"] = _game.Name;
            manifest.Remove("autoUpdateUrl");
            File.WriteAllText(_game.GameFilePath, manifest.ToString());
            var provenancePath = Path.Combine(_game.GameDirectoryPath, "localization/provenance.json");
            var provenance = JObject.Parse(File.ReadAllText(provenancePath));
            provenance["game"] = _game.Id;
            File.WriteAllText(provenancePath, provenance.ToString());
            _game.ReadProperties();
            Assert.That(_game.HasReadProperties, Is.True, _game.Error);
            foreach (var definition in _game.Enums)
                definition.InitializeLookups();
            _game.LoadSets();
            _game.LoadCards(_game.CardsFilePath, _game.SetCodeDefault);
            Assert.That(_game.Cards.Count, Is.EqualTo(2));
            var canonical = File.ReadAllBytes(_game.CardsFilePath);
            var club = _game.Cards["ace_of_clubs"];
            var spade = _game.Cards["ace_of_spades"];
            var spanish = GameContentPresentation.Context(_game, "es");
            Assert.That(spanish.GameName, Is.EqualTo("Verificación de idiomas"));
            Assert.That(spanish.CardName(club), Is.EqualTo("As de tréboles"));
            Assert.That(spanish.PropertyValue(club, "rules"), Does.Contain("\n"));
            var back = spanish.AssetCandidates("cardBackImageUrl").Single().Uri.LocalPath;
            Assert.That(File.Exists(back), Is.True);
            Assert.That(File.ReadAllBytes(back), Is.Not.EqualTo(File.ReadAllBytes(_game.CardBackImageFilePath)));
            var japanese = GameContentPresentation.Context(_game, "ja");
            Assert.That(japanese.CardName(club), Is.EqualTo("クラブのエース"));
            Assert.That(japanese.CardName(spade), Is.EqualTo("Ace of Spades"), "Partial catalogs must fall back per leaf.");
            var deckPath = Path.Combine(_game.DecksDirectoryPath, "Verification deck.txt");
            var deck = UnityDeck.LoadFile(_game, deckPath, "Verification deck");
            Assert.That(deck.MissingCards, Is.Empty);
            Assert.That(deck.Cards.Count(), Is.EqualTo(2));
            Assert.That(deck.AuthoredName, Is.EqualTo("Verification deck"));
            Assert.That(spanish.DeckName(deck.AuthoredName, true), Is.EqualTo("Baraja de verificación"));
            foreach (var explicitNull in new[] { false, true })
            {
                File.WriteAllText(_game.GameFilePath, manifest.ToString());
                _game.ReadProperties();
                Assert.That(GameContentPresentation.Context(_game, "es").CardName(club), Is.EqualTo("As de tréboles"));
                var removed = (JObject)manifest.DeepClone();
                if (explicitNull)
                    removed["localization"] = null;
                else
                    removed.Remove("localization");
                File.WriteAllText(_game.GameFilePath, removed.ToString());
                _game.ReadProperties();
                Assert.That(GameContentPresentation.Context(_game, "es").CardName(club), Is.EqualTo(club.Name));
                Assert.That(_game.LocalizationSpecified, Is.EqualTo(explicitNull));
                Assert.That(File.ReadAllBytes(_game.CardsFilePath), Is.EqualTo(canonical));
                Assert.That(UnityDeck.LoadFile(_game, deckPath, "Verification deck").AuthoredName, Is.Null);
            }
        }

        [Test]
        public void BundledCatalogUsesItsOwnOriginAndParentFallbackWithoutRequests()
        {
            var localeDirectory = Path.Combine(_game.GameDirectoryPath, "locales");
            Directory.CreateDirectory(localeDirectory);
            File.WriteAllText(Path.Combine(localeDirectory, "es.json"), "{\"game\":{\"name\":\"Juego\"},\"assets\":{\"bannerImageUrl\":\"banner.png\"}}");
            File.WriteAllBytes(Path.Combine(localeDirectory, "banner.png"), new byte[] { 1 });
            WriteManifest("locales/es.json");
            var loader = new GameLocalizationCatalogs(_game);
            var documents = loader.Available("es-MX");
            var context = new LocalizationContext(new LocalizationTargets(_game, _game.Cards.Values), "es-MX", documents, _game.DefinitionOrigin);
            Assert.That(context.GameName, Is.EqualTo("Juego"));
            Assert.That(context.AssetCandidates("bannerImageUrl")[0].Uri.LocalPath, Is.EqualTo(Path.Combine(localeDirectory, "banner.png")));
            Assert.That(loader.Refresh("es-MX", () => Assert.Fail("Disabled automatic updates published content")).MoveNext(), Is.False);
            Assert.That(Directory.Exists(Path.Combine(_game.GameDirectoryPath, "localization")), Is.False);
        }

        [Test]
        public void CachedCatalogUsesRecordedOriginAndChangingSourceNeverReusesIt()
        {
            var source = new Uri("https://example.invalid/es.json");
            WriteManifest(source.AbsoluteUri);
            var cache = new LocalizationCache(_game.GameDirectoryPath, _game.Id);
            var request = cache.Begin("es", source);
            var bytes = Encoding.UTF8.GetBytes("{\"game\":{\"name\":\"En caché\"}}");
            Assert.That(cache.Publish(request, bytes, new Uri("https://cdn.example.invalid/locales/es.json"),
                payload => LocalizationJson.ReadCatalog(Encoding.UTF8.GetString(payload)).Value != null), Is.True);
            var loader = new GameLocalizationCatalogs(_game);
            var documents = loader.Available("es");
            Assert.That(documents["es"].Catalog.Game.Name, Is.EqualTo("En caché"));
            Assert.That(documents["es"].Origin.RemoteUrl.Host, Is.EqualTo("cdn.example.invalid"));
            Assert.That(loader.Refresh("es", () => Assert.Fail("Offline refresh published content")).MoveNext(), Is.False);
            _game.AutoUpdate = 1;
            Assert.That(loader.Refresh("es", () => Assert.Fail("Fresh cache was refreshed")).MoveNext(), Is.False);
            WriteManifest("https://example.invalid/new/es.json");
            Assert.That(loader.Available("es"), Is.Empty);
        }

        [Test]
        public void SameUriReloadAndLocaleChangeCannotLetAnOlderResponseReplaceNewerCache()
        {
            WriteManifest("https://example.invalid/es.json");
            _game.AutoUpdate = 0;
            var completed = new[] { false, false };
            var started = 0;
            IEnumerator Download(string uri, string path, Action<FileDownloadResult> done)
            {
                var index = started++;
                while (!completed[index])
                    yield return null;
                File.WriteAllText(path, new JObject { ["game"] = new JObject { ["name"] = index == 0 ? "Old" : "New" } }.ToString());
                done(FileDownloadResult.FromResponse(uri, false));
            }
            var loader = new GameLocalizationCatalogs(_game, Download);
            var publications = 0;
            var older = loader.Refresh("es", () => publications++);
            Assert.That(older.MoveNext(), Is.True);
            var olderDownload = (IEnumerator)older.Current;
            Assert.That(olderDownload.MoveNext(), Is.True);
            loader.Available("ja");
            WriteManifest("https://example.invalid/es.json");
            _game.AutoUpdate = 0;
            var newer = loader.Refresh("es", () => publications++);
            Assert.That(newer.MoveNext(), Is.True);
            var newerDownload = (IEnumerator)newer.Current;
            Assert.That(newerDownload.MoveNext(), Is.True);
            completed[1] = true;
            Assert.That(newerDownload.MoveNext(), Is.False);
            Assert.That(newer.MoveNext(), Is.False);
            completed[0] = true;
            Assert.That(olderDownload.MoveNext(), Is.False);
            Assert.That(older.MoveNext(), Is.False);
            Assert.That(publications, Is.EqualTo(1));
            Assert.That(loader.Available("es")["es"].Catalog.Game.Name, Is.EqualTo("New"));
            var reopened = new GameLocalizationCatalogs(_game);
            Assert.That(reopened.Available("es")["es"].Catalog.Game.Name, Is.EqualTo("New"));
        }

        [Test]
        public void MalformedCatalogDoesNotPreventBasePresentation()
        {
            File.WriteAllText(Path.Combine(_game.GameDirectoryPath, "es.json"), "[]");
            WriteManifest("es.json");
            var loader = new GameLocalizationCatalogs(_game);
            var documents = loader.Available("es");
            Assert.That(documents, Is.Empty);
            Assert.That(new LocalizationContext(new LocalizationTargets(_game, _game.Cards.Values), "es", documents).GameName,
                Is.EqualTo(_game.Name));
        }
    }
}
