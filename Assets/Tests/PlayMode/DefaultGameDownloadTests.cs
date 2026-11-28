/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>
    /// When CGS has no games, as on a first web visit, the player's game downloads first and can be started while the
    /// other default games download in the background.
    /// </summary>
    public class DefaultGameDownloadTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const int TimeoutFrames = 1200;

        private readonly List<string> _directories = new();
        private Func<string> _launchDeepLink;
        private int _hasSeenWelcome;
        private string _defaultGame;
        private string _sourceDirectory;

        [SetUp]
        public void SetUp()
        {
            _launchDeepLink = CardGameManager.LaunchDeepLink;
            _hasSeenWelcome = PlayerPrefs.GetInt(HasSeenWelcome, 0);
            _defaultGame = PlayerPrefs.GetString(CardGameManager.PlayerPrefsDefaultGame, string.Empty);
            PlayerPrefs.SetInt(HasSeenWelcome, 1);
            _sourceDirectory = Path.Combine(Application.temporaryCachePath, "DefaultGameDownloadTests");
            if (Directory.Exists(_sourceDirectory))
                Directory.Delete(_sourceDirectory, true);
            _directories.Add(_sourceDirectory);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CardGameManager.LaunchDeepLink = _launchDeepLink;
            PlayerPrefs.SetInt(HasSeenWelcome, _hasSeenWelcome);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, _defaultGame);
            if (CgsNetManager.Instance != null)
                CgsNetManager.Instance.Stop();
            foreach (var manager in Object.FindObjectsByType<CardGameManager>())
            {
                manager.StopAllCoroutines();
                for (var i = 0; i < 10 && manager.Messenger != null && manager.Messenger.IsShowing; i++)
                    manager.Messenger.OkClose();
            }

            // Close the screens before the manager they use
            var empty = SceneManager.CreateScene("DefaultGameDownloadTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);
            foreach (var manager in Object.FindObjectsByType<CardGameManager>())
                Object.Destroy(manager.gameObject);
            yield return null;
            foreach (var directory in _directories.Where(Directory.Exists))
                Directory.Delete(directory, true);
        }

        private const int CardBackWidth = 6;
        private const int CardBackHeight = 9;

        /// <summary>Writes a downloadable game whose definition, cards, and card back are local file URLs.</summary>
        private string CreateSource(string name, int cards)
        {
            var directory = Path.Combine(_sourceDirectory, name);
            Directory.CreateDirectory(directory);
            var definitionPath = Path.Combine(directory, "cgs.json");
            var cardsPath = Path.Combine(directory, "AllCards.json");
            File.WriteAllText(cardsPath, JsonConvert.SerializeObject(Enumerable.Range(0, cards).Select(index =>
                new Dictionary<string, string> { ["id"] = $"{name}-{index}", ["name"] = $"{name} {index}" })));
            var cardBackPath = Path.Combine(directory, "CardBack.png");
            var cardBack = new Texture2D(CardBackWidth, CardBackHeight);
            File.WriteAllBytes(cardBackPath, cardBack.EncodeToPNG());
            Object.Destroy(cardBack);
            var definitionUrl = new Uri(definitionPath).AbsoluteUri;
            File.WriteAllText(definitionPath, JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                ["name"] = name,
                ["autoUpdate"] = -1,
                ["autoUpdateUrl"] = definitionUrl,
                ["allCardsUrl"] = new Uri(cardsPath).AbsoluteUri,
                ["cardBackImageUrl"] = new Uri(cardBackPath).AbsoluteUri
            }));
            return definitionUrl;
        }

        private static UnityCardGame GameFrom(string url) => CardGameManager.Instance.AllCardGames.Values.Single(game =>
            game.AutoUpdateUrl != null && game.AutoUpdateUrl.AbsoluteUri == url);

        // The game's own card back, not the default one shown until it is decoded
        private static bool ShowsItsCardBack(UnityCardGame game) => game.CardBackImageSprite != null &&
                                                                    game.CardBackImageSprite.texture.width == CardBackWidth &&
                                                                    game.CardBackImageSprite.texture.height == CardBackHeight;

        private static bool HasGameFrom(string url) => CardGameManager.Instance.AllCardGames.Values.Any(game =>
            game.AutoUpdateUrl != null && game.AutoUpdateUrl.AbsoluteUri == url);

        private void TrackDownloadedGames()
        {
            foreach (var game in CardGameManager.Instance.AllCardGames.Values.Where(game =>
                         game.AutoUpdateUrl != null && game.AutoUpdateUrl.IsFile &&
                         game.AutoUpdateUrl.LocalPath.StartsWith(_sourceDirectory)))
                if (!_directories.Contains(game.GameDirectoryPath))
                    _directories.Add(game.GameDirectoryPath);
        }

        [UnityTest]
        public IEnumerator ThePlayersGameDownloadsFirstAndStartsWhileTheOthersDownload()
        {
            var first = CreateSource("DefaultDownloadOther", 10);
            var preferred = CreateSource("DefaultDownloadPreferred", 10);
            var last = CreateSource("DefaultDownloadLast", 10);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, "preferred");

            foreach (var existing in Object.FindObjectsByType<CardGameManager>())
                Object.Destroy(existing.gameObject);
            yield return null;
            CardGameManager.LaunchDeepLink = () => null;
            yield return SceneManager.LoadSceneAsync(Tags.MainMenuSceneIndex);
            var mainMenu = Object.FindObjectsByType<MainMenu>().Single();
            for (var frame = 0; frame < TimeoutFrames && mainMenu.IsWaitingForLanguage; frame++)
                yield return null;

            var manager = CardGameManager.Instance;
            var download = manager.GetDefaultCardGames(new[] { ("other", first), ("preferred", preferred), ("last", last) });
            try
            {
                // The download the player waits for is only the preferred game's
                yield return download;
                TrackDownloadedGames();
                Assert.AreEqual(preferred, CardGameManager.Current.AutoUpdateUrl?.AbsoluteUri,
                    "The player's saved game downloads first and is selected");
                Assert.IsTrue(manager.IsDownloadingDefaultGames, "The other games are still to download");
                Assert.IsFalse(HasGameFrom(first), "The other games have not downloaded yet");
                Assert.IsFalse(HasGameFrom(last), "The other games have not downloaded yet");

                // The selected game can be started while the others download
                for (var frame = 0; frame < TimeoutFrames && !CardGameManager.IsCurrentContentReady; frame++)
                    yield return null;
                Assert.IsFalse(manager.Progress.gameObject.activeSelf, "Background downloads do not show progress");
                Assert.IsNull(manager.ModalCanvas, "Background downloads do not block the menu");
                yield return null;
                mainMenu.StartGame();
                Assert.IsTrue(mainMenu.IsOpeningPlayScreen, "Start Game opens the play screen");

                // The others finish in the background, without changing the selection
                for (var frame = 0; frame < TimeoutFrames && manager.IsDownloadingDefaultGames; frame++)
                    yield return null;
                TrackDownloadedGames();
                Assert.IsFalse(manager.IsDownloadingDefaultGames);
                Assert.IsTrue(HasGameFrom(first), "The other default games download in the background");
                Assert.IsTrue(HasGameFrom(last), "The other default games download in the background");
                Assert.AreEqual(preferred, CardGameManager.Current.AutoUpdateUrl?.AbsoluteUri,
                    "Background downloads keep the player's selection");
                Assert.IsTrue(ShowsItsCardBack(CardGameManager.Current), "The selected game shows its card back");
                Assert.IsTrue(ShowsItsCardBack(GameFrom(first)),
                    "Game selection shows a background-downloaded game's card back before it is selected");
                Assert.IsTrue(ShowsItsCardBack(GameFrom(last)),
                    "Game selection shows a background-downloaded game's card back before it is selected");
                for (var frame = 0; frame < TimeoutFrames && mainMenu != null && mainMenu.IsOpeningPlayScreen; frame++)
                    yield return null;
            }
            finally
            {
                TrackDownloadedGames();
            }
        }
    }
}
