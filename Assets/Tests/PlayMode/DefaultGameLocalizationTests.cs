/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cgs.GameContent;
using Cgs.Localization;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class DefaultGameLocalizationTests
    {
        private readonly List<string> _directories = new();
        private static readonly string[] Locales = { "en", "es", "fr", "de", "it", "ja", "ko", "pt-BR", "ru", "zh-Hans" };

        public static IEnumerable<TestCaseData> CatalogCases()
        {
            foreach (var game in new[] { "Standard", "Mahjong", "Dominoes" })
                foreach (var locale in Locales)
                    yield return new TestCaseData(game, locale);
        }

        private static JObject Package(string key) => JObject.Parse(Resources.Load<TextAsset>("DefaultGameContent/" + key).text);

        private UnityCardGame Game(string key, bool metadata)
        {
            var package = Package(key);
            var root = Path.Combine(Application.streamingAssetsPath, package.Value<string>("name") + "@www.cardgamesimulator.com");
            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(root, "cgs.json")));
            if (!metadata)
                manifest.Remove("localization");
            // Retain a unique cached ID so tests never write to a player's default game.
            var game = new UnityCardGame(null, "Default-catalog-test-" + Guid.NewGuid().ToString("N"));
            JsonConvert.PopulateObject(manifest.ToString(), game);
            _directories.Add(game.GameDirectoryPath);
            Directory.CreateDirectory(game.GameDirectoryPath);
            File.Copy(Path.Combine(root, "AllSets.json"), game.SetsFilePath);
            foreach (var definition in game.Enums)
                definition.InitializeLookups();
            game.LoadSets();
            foreach (var back in game.CardBackFaceImageUrls)
                game.CardBackFaceImageSprites[back.Id] = null;
            game.LoadCards(Path.Combine(root, "AllCards.json"), game.SetCodeDefault);
            return game;
        }

        private static void Apply(UnityCardGame game, JObject definition) => typeof(BuiltInGameContent)
            .GetMethod("ApplyCatalog", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { game, definition });

        private static void Install(UnityCardGame game) => typeof(BuiltInGameContent)
            .GetMethod("EnsureDecks", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { game });

        [TearDown]
        public void CleanUp()
        {
            foreach (var directory in _directories)
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            _directories.Clear();
        }

        [TestCaseSource(nameof(CatalogCases))]
        public void EveryDefaultGameHasCompleteOfflinePresentation(string key, string locale)
        {
            var game = Game(key, true);
            var before = JsonConvert.SerializeObject(game.Cards.Values);
            var canonicalName = game.Name;
            Assert.That(game.Localization.Locales.Keys, Is.EquivalentTo(Locales));
            var catalog = game.Localization.Locales[locale].Inline;
            var targets = new LocalizationTargets(game, game.Cards.Values, game.Sets.Values,
                cardBackIds: game.CardBackFaceImageUrls.Select(back => back.Id));
            Assert.That(targets.Validate(catalog).Where(d => !d.IsWarning), Is.Empty);
            var display = GameContentPresentation.Context(game, locale);
            Assert.That(display.GameName, Is.EqualTo(catalog.Game.Name));
            foreach (var card in game.Cards.Values)
            {
                Assert.That(catalog.CardNames.ContainsKey(card.Name), Is.True, card.Name);
                Assert.That(display.CardName(card), Is.EqualTo(catalog.CardNames[card.Name]));
                foreach (var property in game.CardProperties)
                    Assert.That(display.PropertyLabel(property.Name, property.Display), Is.EqualTo(catalog.Properties[property.Name].Display));
            }
            foreach (var definition in game.Enums)
                foreach (var value in definition.Values)
                    Assert.That(display.EnumLabel(definition.Property, value.Key, value.Value),
                        Is.EqualTo(catalog.Enums[definition.Property][value.Key]));
            foreach (var set in game.Sets.Values.Where(value => value.Code != game.SetCodeDefault))
                Assert.That(display.SetName(set), Is.EqualTo(catalog.Sets[set.Code].Name));
            if (catalog.Cards != null)
                foreach (var translatedCard in catalog.Cards)
                    foreach (var property in translatedCard.Value.Properties)
                        Assert.That(display.PropertyValue(game.Cards[translatedCard.Key], property.Key), Is.EqualTo(property.Value));
            Install(game);
            foreach (var deck in game.DeckUrls)
            {
                var path = BuiltInGameContent.DeckPath(game, deck.Name, deck.Url.AbsoluteUri);
                Assert.That(path, Is.Not.Null, deck.Name);
                var loaded = UnityDeck.LoadFile(game, path, deck.Name);
                Assert.That(loaded.AuthoredName, Is.EqualTo(deck.Name));
                Assert.That(loaded.MissingCards, Is.Empty, deck.Name);
                Assert.That(display.DeckName(loaded.Name, loaded.AuthoredName != null), Is.EqualTo(catalog.Decks[deck.Name].Name));
            }
            Assert.That(game.Name, Is.EqualTo(canonicalName));
            Assert.That(JsonConvert.SerializeObject(game.Cards.Values), Is.EqualTo(before));
        }

        [TestCase("Standard")]
        [TestCase("Mahjong")]
        [TestCase("Dominoes")]
        public void OlderDefaultDefinitionsReceiveCatalogsWithoutReplacingUserDecks(string key)
        {
            var game = Game(key, false);
            var deck = game.DeckUrls[0];
            Directory.CreateDirectory(game.DecksDirectoryPath);
            var userPath = Path.Combine(game.DecksDirectoryPath, deck.Name + ".txt");
            // Even byte-identical legacy/user files remain unproven.
            var text = Package(key)["decks"][deck.Name].Value<string>("text");
            File.WriteAllText(userPath, text);
            Apply(game, new JObject { ["name"] = game.Name });
            Install(game);
            Assert.That(game.Localization.Locales.Keys, Is.EquivalentTo(Locales));
            var authoredPath = BuiltInGameContent.DeckPath(game, deck.Name);
            Assert.That(authoredPath, Is.Not.EqualTo(userPath));
            Assert.That(BuiltInGameContent.IsManagedDeck(game, authoredPath), Is.True);
            Assert.That(AuthoredDeckFiles.AuthoredName(game, userPath), Is.Null);
            Assert.That(File.ReadAllText(userPath), Is.EqualTo(text));
            var binding = AuthoredDeckFiles.ReadBinding(game, authoredPath).Generation;
            var spanish = GameContentPresentation.Context(game, "es");
            Assert.That(spanish.DeckName(deck.Name, true), Is.Not.EqualTo(deck.Name));
            Assert.That(spanish.DeckName(deck.Name, false), Is.EqualTo(deck.Name));
            GameContentPresentation.Context(game, "ja");
            Install(game);
            Assert.That(AuthoredDeckFiles.ReadBinding(game, authoredPath).Generation, Is.EqualTo(binding));
        }

        [Test]
        public void ExplicitNullAndCustomSourceKeepCanonicalBehavior()
        {
            var game = Game("Standard", false);
            Apply(game, new JObject { ["localization"] = null });
            Assert.That(game.Localization, Is.Null);
            game.AutoUpdateUrl = new Uri("https://example.invalid/custom/cgs.json");
            Apply(game, new JObject());
            Assert.That(game.Localization, Is.Null);
            Assert.That(BuiltInGameContent.DeckPaths(game), Is.Empty);
        }

        [TestCase("Standard", 4)]
        [TestCase("Mahjong", 1)]
        [TestCase("Dominoes", 2)]
        public void UpgradedDeckPickerShowsEachBuiltInOnceWithoutChangingLegacyFiles(string key, int count)
        {
            var game = Game(key, false);
            Directory.CreateDirectory(game.DecksDirectoryPath);
            foreach (var deck in ((JObject)Package(key)["decks"]).Properties())
                File.WriteAllText(Path.Combine(game.DecksDirectoryPath, deck.Name + ".txt"), deck.Value.Value<string>("text"));
            var saved = Directory.GetFiles(game.DecksDirectoryPath);
            var before = saved.ToDictionary(path => path, File.ReadAllBytes);
            Apply(game, new JObject());
            Install(game);
            foreach (var locale in Locales)
            {
                var choices = BuiltInGameContent.DeckChoices(game, saved).ToArray();
                Assert.That(choices, Has.Length.EqualTo(count));
                Assert.That(choices, Is.EquivalentTo(BuiltInGameContent.DeckPaths(game)));
                var display = GameContentPresentation.Context(game, locale);
                foreach (var path in choices)
                {
                    var binding = AuthoredDeckFiles.ReadBinding(game, path);
                    Assert.That(binding, Is.Not.Null);
                    Assert.That(display.DeckName(binding.Name, true),
                        Is.EqualTo(game.Localization.Locales[locale].Inline.Decks[binding.Name].Name));
                }
            }
            foreach (var path in saved)
            {
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(before[path]));
                Assert.That(AuthoredDeckFiles.ReadBinding(game, path), Is.Null);
            }
            // Opting out restores the ordinary entries, without needing to recover any deleted files.
            game.Localization = null;
            Assert.That(BuiltInGameContent.DeckChoices(game, saved), Is.EquivalentTo(saved));
        }

        [TestCase("Standard")]
        [TestCase("Mahjong")]
        [TestCase("Dominoes")]
        public void ExplicitUserSaveBeforeFirstCatalogInstallStaysVisible(string key)
        {
            var game = Game(key, false);
            var deck = game.DeckUrls[0];
            var path = Path.Combine(game.DecksDirectoryPath, deck.Name + ".txt");
            var text = Package(key)["decks"][deck.Name].Value<string>("text");
            AuthoredDeckFiles.WriteUserFile(game, path, text);
            Apply(game, new JObject());
            Install(game);
            Assert.That(BuiltInGameContent.DeckChoices(game, new[] { path }), Does.Contain(path));
            Assert.That(AuthoredDeckFiles.ReadBinding(game, path), Is.Null);
        }

        [TestCase("Standard")]
        [TestCase("Mahjong")]
        [TestCase("Dominoes")]
        public void DeckPickerKeepsModifiedRenamedAndExplicitlySavedCopies(string key)
        {
            var game = Game(key, false);
            var deck = game.DeckUrls[0];
            var text = Package(key)["decks"][deck.Name].Value<string>("text");
            var path = Path.Combine(game.DecksDirectoryPath, deck.Name + ".txt");
            Directory.CreateDirectory(game.DecksDirectoryPath);
            File.WriteAllText(path, text + "\n");
            var renamed = Path.Combine(game.DecksDirectoryPath, "My deck.txt");
            File.WriteAllText(renamed, text);
            Apply(game, new JObject());
            Install(game);
            var saved = new[] { path, renamed };
            Assert.That(BuiltInGameContent.DeckChoices(game, saved), Does.Contain(path).And.Contain(renamed));
            File.WriteAllText(path, text);
            Assert.That(BuiltInGameContent.DeckChoices(game, saved), Does.Not.Contain(path));
            // Saving the same bytes is an explicit user action, including while catalogs are disabled.
            game.Localization = null;
            AuthoredDeckFiles.WriteUserFile(game, path, text);
            Apply(game, new JObject());
            Install(game);
            Assert.That(BuiltInGameContent.DeckChoices(game, saved), Does.Contain(path).And.Contain(renamed));
            Assert.That(AuthoredDeckFiles.ReadBinding(game, path), Is.Null);
            Assert.That(File.ReadAllText(path), Is.EqualTo(text));
        }

        [Test]
        public void StagedOlderManifestReceivesFallbackAndExplicitNullDisablesIt()
        {
            var game = Game("Standard", false);
            var file = Path.Combine(game.GameDirectoryPath, "candidate.json");
            var manifest = Package("Standard");
            var definition = new JObject { ["name"] = manifest["name"], ["autoUpdateUrl"] = manifest["url"] };
            var stage = typeof(UnityCardGame).GetMethod("StageDefinition", BindingFlags.Instance | BindingFlags.NonPublic);
            File.WriteAllText(file, definition.ToString());
            var staged = (UnityCardGame)stage.Invoke(game, new object[] { file });
            Assert.That(staged.Localization.Locales.Keys, Is.EquivalentTo(Locales));
            definition["localization"] = null;
            File.WriteAllText(file, definition.ToString());
            staged = (UnityCardGame)stage.Invoke(game, new object[] { file });
            Assert.That(staged.Localization, Is.Null);
            Assert.That(staged.LocalizationSpecified, Is.True);
            // The editor copies both parts of the optional member to its edit model.
            var edited = new UnityCardGame(null, staged.Id)
            {
                AutoUpdateUrl = staged.AutoUpdateUrl,
                Localization = staged.Localization,
                LocalizationSpecified = staged.LocalizationSpecified
            };
            var saved = JsonConvert.SerializeObject(edited);
            Assert.That(JObject.Parse(saved)["localization"].Type, Is.EqualTo(JTokenType.Null));
            File.WriteAllText(file, saved);
            staged = (UnityCardGame)stage.Invoke(game, new object[] { file });
            Assert.That(staged.Localization, Is.Null);
            Assert.That(staged.LocalizationSpecified, Is.True);
            Assert.That(BuiltInGameContent.DeckPaths(staged), Is.Empty);
        }

        [Test]
        public void ShippedDeckSidecarsBindOnlyTheirPackagedFiles()
        {
            foreach (var key in new[] { "Standard", "Mahjong", "Dominoes" })
            {
                var package = Package(key);
                var id = package.Value<string>("name") + "@www.cardgamesimulator.com";
                var root = Path.Combine(Application.streamingAssetsPath, id);
                foreach (var deck in ((JObject)package["decks"]).Properties())
                    Assert.That(AuthoredDeckFiles.ReadBinding(root, id, Path.Combine(root, "decks", deck.Name + ".txt"))?.Name,
                        Is.EqualTo(deck.Name));
            }
        }

        [UnityTest]
        public IEnumerator LanguagePickerRefreshesDefaultCardDeckAndBackLabels()
        {
            var hadPreference = PlayerPrefs.HasKey(CgsLocaleSelector.PreferenceKey);
            var preference = PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey);
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            var original = CgsLocalization.CurrentLocale;
            var game = Game("Standard", true);
            Install(game);
            var deck = game.DeckUrls[0];
            var path = BuiltInGameContent.DeckPath(game, deck.Name);
            var card = game.Cards.Values.First(value => value.Name == "Ace of Spades");
            var cardObject = new GameObject("Card label", typeof(RectTransform), typeof(Text));
            var deckObject = new GameObject("Deck label", typeof(RectTransform), typeof(Text));
            var backObject = new GameObject("Back selector", typeof(RectTransform), typeof(Dropdown));
            try
            {
                var cardLabel = cardObject.GetComponent<Text>();
                var deckLabel = deckObject.GetComponent<Text>();
                var dropdown = backObject.GetComponent<Dropdown>();
                GameContentText.Set(cardLabel, game, display => display.CardName(card));
                GameContentText.Set(deckLabel, game, display => display.DeckName(AuthoredDeckFiles.AuthoredName(game, path), true));
                GameContentBackOptions.Bind(dropdown, game, "DARK_BACK");
                foreach (var locale in Locales)
                {
                    CgsLocalization.SelectLanguage(locale);
                    var deadline = Time.realtimeSinceStartup + 20;
                    while (CgsLocalization.CurrentLocale != locale && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    yield return null;
                    Assert.That(CgsLocalization.CurrentLocale, Is.EqualTo(locale));
                    var display = GameContentPresentation.Context(game, locale);
                    Assert.That(cardLabel.text, Is.EqualTo(display.CardName(card)), locale);
                    Assert.That(deckLabel.text, Is.EqualTo(display.DeckName(deck.Name, true)), locale);
                    Assert.That(dropdown.options[dropdown.value].text, Is.EqualTo(display.CardBackName("DARK_BACK")), locale);
                    Assert.That(GameContentBackOptions.SelectedKey(dropdown), Is.EqualTo("DARK_BACK"));
                }
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
                Object.DestroyImmediate(deckObject);
                Object.DestroyImmediate(backObject);
                CgsLocalization.SelectLanguage(original);
                if (hadPreference)
                    PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, preference);
                else
                    PlayerPrefs.DeleteKey(CgsLocaleSelector.PreferenceKey);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void CardBackSelectionKeepsCanonicalIdentity()
        {
            var game = Game("Standard", true);
            game.CardBackFaceImageSprites["DARK_BACK"] = null;
            var target = new GameObject("Back selector", typeof(RectTransform), typeof(Dropdown));
            try
            {
                var dropdown = target.GetComponent<Dropdown>();
                GameContentBackOptions.Bind(dropdown, game, "DARK_BACK");
                Assert.That(GameContentBackOptions.SelectedKey(dropdown), Is.EqualTo("DARK_BACK"));
                Assert.That(GameContentPresentation.Context(game, "ja").CardBackName("DARK_BACK"), Is.EqualTo("暗い裏面"));
                Assert.That(GameContentBackOptions.SelectedKey(dropdown), Is.EqualTo("DARK_BACK"));
            }
            finally { Object.DestroyImmediate(target); }
        }
    }
}
