/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.PlayMode
{
    public class GameManifestLocalizationTests
    {
        private readonly List<string> _directories = new();

        private UnityCardGame CreateGame()
        {
            var game = new UnityCardGame(null, "Localization-test-" + Guid.NewGuid().ToString("N"));
            _directories.Add(game.GameDirectoryPath);
            Directory.CreateDirectory(game.GameDirectoryPath);
            return game;
        }

        private static JObject Manifest(UnityCardGame game, string translatedName)
        {
            return new JObject
            {
                ["name"] = game.Name,
                ["autoUpdate"] = -1,
                ["localization"] = new JObject
                {
                    ["defaultLocale"] = "en",
                    ["locales"] = new JObject
                    {
                        ["es"] = new JObject { ["inline"] = new JObject { ["game"] = new JObject { ["name"] = translatedName } } }
                    }
                }
            };
        }

        private static string DisplayName(UnityCardGame game)
        {
            return new LocalizationContext(new LocalizationTargets(game, game.Cards.Values), "es").GameName;
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var path in _directories)
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            _directories.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SameSourceReloadReplacesInlineAndAbsentOrNullRemovesLocalization(bool explicitNull)
        {
            var game = CreateGame();
            var notifications = 0;
            game.DefinitionChanged += changed =>
            {
                Assert.That(changed.HasReadProperties, Is.True);
                notifications++;
            };
            File.WriteAllText(game.GameFilePath, Manifest(game, "Uno").ToString());
            game.ReadProperties();
            Assert.That(DisplayName(game), Is.EqualTo("Uno"));
            Assert.That(game.DefinitionGeneration, Is.EqualTo(1));
            File.WriteAllText(game.GameFilePath, Manifest(game, "Dos").ToString());
            game.ReadProperties();
            Assert.That(DisplayName(game), Is.EqualTo("Dos"));
            Assert.That(game.DefinitionGeneration, Is.EqualTo(2));
            var removed = new JObject { ["name"] = game.Name };
            if (explicitNull)
                removed["localization"] = null;
            File.WriteAllText(game.GameFilePath, removed.ToString());
            game.ReadProperties();
            Assert.That(game.Localization, Is.Null);
            Assert.That(DisplayName(game), Is.EqualTo(game.Name));
            Assert.That(game.DefinitionGeneration, Is.EqualTo(3));
            Assert.That(notifications, Is.EqualTo(3));
        }

        [TestCase("missing")]
        [TestCase("syntax")]
        [TestCase("required")]
        [TestCase("type")]
        public void FailedReadKeepsLastGoodDefinitionAndGeneration(string failure)
        {
            var game = CreateGame();
            var manifest = Manifest(game, "Bueno");
            manifest["copyright"] = "Original";
            manifest["cardProperties"] = new JArray(new JObject { ["name"] = "rules", ["type"] = "String" });
            File.WriteAllText(game.GameFilePath, manifest.ToString());
            game.ReadProperties();
            Assert.That(game.HasReadProperties, Is.True, game.Error);
            var localization = game.Localization;
            var properties = game.CardProperties;
            if (failure == "missing")
                File.Delete(game.GameFilePath);
            else if (failure == "syntax")
                File.WriteAllText(game.GameFilePath, "{\"copyright\":\"Changed\",\"localization\":null,");
            else if (failure == "required")
                File.WriteAllText(game.GameFilePath, "{\"copyright\":\"Changed\",\"localization\":null}");
            else
            {
                manifest["copyright"] = "Changed";
                manifest["autoUpdate"] = "invalid integer";
                File.WriteAllText(game.GameFilePath, manifest.ToString());
            }
            var notifications = 0;
            game.DefinitionChanged += _ => notifications++;
            game.ReadProperties();
            Assert.That(game.HasReadProperties, Is.False);
            Assert.That(game.DefinitionGeneration, Is.EqualTo(1));
            Assert.That(notifications, Is.Zero);
            Assert.That(game.Localization, Is.SameAs(localization));
            Assert.That(game.CardProperties, Is.SameAs(properties));
            Assert.That(game.CardProperties.Count, Is.EqualTo(1));
            Assert.That(game.Copyright, Is.EqualTo("Original"));
            Assert.That(DisplayName(game), Is.EqualTo("Bueno"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefinitionMigrationKeepsPackageAndCanonicalLoadedObjects(bool backupName)
        {
            var game = CreateGame();
            var oldDirectory = game.GameDirectoryPath;
            var newName = "Localization-test-" + Guid.NewGuid().ToString("N");
            var target = new UnityCardGame(null, newName, "https://example.invalid/nested/cgs.json");
            _directories.Add(target.GameDirectoryPath);
            var manifest = Manifest(game, "Migrado");
            manifest["name"] = newName;
            manifest["autoUpdateUrl"] = target.AutoUpdateUrl.AbsoluteUri;
            File.WriteAllText(backupName ? game.GameBackupFilePath : game.GameFilePath, manifest.ToString());
            File.WriteAllText(Path.Combine(oldDirectory, "AllCards.json"), "[]");
            var card = new UnityCard(game, "canonical", "Original", "BASE", new Dictionary<string, PropertyDefValuePair>(), false);
            game.Add(card, false);
            game.ReadProperties();
            Assert.That(game.HasReadProperties, Is.True, game.Error);
            Assert.That(game.Id, Is.EqualTo(target.Id));
            Assert.That(Directory.Exists(oldDirectory), Is.False);
            Assert.That(File.Exists(game.GameFilePath), Is.True);
            Assert.That(File.Exists(game.CardsFilePath), Is.True);
            Assert.That(game.Cards[card.Id], Is.SameAs(card));
            Assert.That(DisplayName(game), Is.EqualTo("Migrado"));
            game.ReadProperties();
            Assert.That(game.HasReadProperties, Is.True, game.Error);
            Assert.That(game.DefinitionGeneration, Is.EqualTo(2));
        }
    }
}
