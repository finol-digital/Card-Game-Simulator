/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityExtensionMethods;

namespace Tests.PlayMode
{
    public class GameContentProvenanceTests
    {
        private UnityCardGame _game;
        private string _path;
        private string _relocated;
        private const string Source = "https://example.invalid/Starter.txt";
        private const string DeckText = "# Identical empty deck\n";

        [SetUp]
        public void SetUp()
        {
            _game = new UnityCardGame(null, "provenance-" + Guid.NewGuid().ToString("N"));
            _game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""en"",""locales"":{}}").Value;
            Directory.CreateDirectory(_game.DecksDirectoryPath);
            _path = Path.Combine(_game.DecksDirectoryPath, "Starter.txt");
            _relocated = _game.GameDirectoryPath + "-relocated";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var path in new[] { _game.GameDirectoryPath, _relocated })
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
        }

        private static IEnumerator Download(string source, string path, Action<FileDownloadResult> complete)
        {
            File.WriteAllText(path, DeckText);
            complete(new FileDownloadResult(true, new Uri(source), true));
            yield break;
        }

        private static void Run(IEnumerator routine)
        {
            while (routine.MoveNext())
                if (routine.Current is IEnumerator nested)
                    Run(nested);
        }

        private void Install() => Run(AuthoredDeckFiles.Download(_game, "Starter", Source, _path, Download));

        [Test]
        public void FirstAuthoredInstallCreatesDeckDirectoryAndInventoryFromSidecar()
        {
            Directory.Delete(_game.DecksDirectoryPath);
            Install();
            Assert.That(File.Exists(_path), Is.True);
            Assert.That(AuthoredDeckFiles.Names(_game), Is.EqualTo(new[] { "Starter" }));
        }

        [Test]
        public void IdenticalUserReplacementRevokesFileAndAlreadyLoadedInstance()
        {
            Install();
            var deck = UnityDeck.LoadFile(_game, _path, "Starter");
            Assert.That(deck.AuthoredName, Is.EqualTo("Starter"));
            AuthoredDeckFiles.WriteUserFile(_game, _path, DeckText);
            Assert.That(AuthoredDeckFiles.AuthoredName(_game, _path), Is.Null);
            Assert.That(deck.AuthoredName, Is.Null);
            Assert.That(File.ReadAllText(_path), Is.EqualTo(DeckText));
            Install();
            Assert.That(deck.AuthoredName, Is.Null, "A new artifact generation cannot revive an old binding.");
        }

        [Test]
        public void NameAndCardsNeverInferProvenanceAndStandaloneCopiesStayUnproven()
        {
            File.WriteAllText(_path, DeckText);
            Assert.That(UnityDeck.LoadFile(_game, _path, "Starter").AuthoredName, Is.Null);
            Install();
            Assert.That(UnityDeck.Parse(_game, "Starter", DeckFileType.Txt, DeckText).AuthoredName, Is.Null);
            var copy = Path.Combine(_game.DecksDirectoryPath, "Copy.txt");
            File.Copy(_path, copy);
            Assert.That(UnityDeck.LoadFile(_game, copy, "Copy").AuthoredName, Is.Null);
            var bound = UnityDeck.LoadFile(_game, _path, "Starter");
            bound.Name = "Renamed";
            Assert.That(bound.AuthoredName, Is.Null);
            bound.ClearAuthoredName();
            bound.Name = "Starter";
            Assert.That(bound.AuthoredName, Is.Null);
            AuthoredDeckFiles.WriteUserFile(_game, copy, DeckText);
            Assert.That(AuthoredDeckFiles.AuthoredName(_game, copy), Is.Null);
        }

        [Test]
        public void ExternalMutationAndUnsupportedSidecarsFallBack()
        {
            Install();
            File.AppendAllText(_path, "# changed");
            Assert.That(AuthoredDeckFiles.AuthoredName(_game, _path), Is.Null);
            Install();
            var statePath = Path.Combine(_game.GameDirectoryPath, "localization", "provenance.json");
            var state = JObject.Parse(File.ReadAllText(statePath));
            state["version"] = 999;
            File.WriteAllText(statePath, state.ToString());
            Assert.That(AuthoredDeckFiles.AuthoredName(_game, _path), Is.Null);
        }

        [Test]
        public void PortableSidecarSurvivesReopenAndNoLocalizationStaysInert()
        {
            Install();
            var statePath = Path.Combine(_game.GameDirectoryPath, "localization", "provenance.json");
            var state = File.ReadAllText(statePath);
            Assert.That(state, Does.Not.Contain(_game.GameDirectoryPath));
            LocalizationFiles.CopyPackageSnapshot(_game.GameDirectoryPath, _relocated);
            Assert.That(AuthoredDeckFiles.ReadBinding(_relocated, _game.Id,
                Path.Combine(_relocated, "decks", "Starter.txt"))?.Name, Is.EqualTo("Starter"));
            var reopened = new UnityCardGame(null, _game.Name) { Localization = _game.Localization };
            Assert.That(AuthoredDeckFiles.AuthoredName(reopened, _path), Is.EqualTo("Starter"));
            reopened.Localization = null;
            Assert.That(AuthoredDeckFiles.AuthoredName(reopened, _path), Is.Null);
            Assert.That(File.ReadAllText(statePath), Is.EqualTo(state));
        }

        [Test]
        public void PackageReplacementRejectsOldPendingInstall()
        {
            Install();
            var pending = AuthoredDeckFiles.Download(_game, "Starter", Source, _path, Download);
            Assert.That(pending.MoveNext(), Is.True);
            var transport = (IEnumerator)pending.Current;
            LocalizationCache.Invalidate(_game.GameDirectoryPath);
            var statePath = Path.Combine(_game.GameDirectoryPath, "localization", "provenance.json");
            var before = File.ReadAllText(statePath);
            Run(transport);
            Run(pending);
            Assert.That(File.ReadAllText(statePath), Is.EqualTo(before));
        }

        [Test]
        public void UserSaveDuringDownloadPreventsLateAuthoredReclassification()
        {
            var pending = AuthoredDeckFiles.Download(_game, "Starter", Source, _path, Download);
            Assert.That(pending.MoveNext(), Is.True);
            var transport = (IEnumerator)pending.Current;
            AuthoredDeckFiles.WriteUserFile(_game, _path, "# user");
            Run(transport);
            Run(pending);
            Assert.That(File.ReadAllText(_path), Is.EqualTo("# user"));
            Assert.That(AuthoredDeckFiles.AuthoredName(_game, _path), Is.Null);
        }
    }
}
