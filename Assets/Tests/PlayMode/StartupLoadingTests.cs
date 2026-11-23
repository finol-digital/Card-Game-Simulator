/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cgs;
using Cgs.Cards;
using Cgs.Decks;
using Cgs.Menu;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>The Main Menu appears from game properties alone; card data loads after it is interactive.</summary>
    public class StartupLoadingTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const int TimeoutFrames = 1200;
        private const string UnreachableGameUrl = "http://127.0.0.1:9/cgs.json";

        private static readonly string[] SceneNames =
            { "MainMenu", "PlayGame", "DeckEditor", "CardsExplorer", "Settings" };

        private readonly List<string> _fixtureDirectories = new();
        private readonly List<InputDevice> _devices = new();
        private Func<string> _launchDeepLink;
        private string _defaultGame;
        private int _hasSeenWelcome;
        private bool _actionsEnabled;
        private InputSettings.BackgroundBehavior _backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInputBehavior;

        [SetUp]
        public void SetUp()
        {
            _launchDeepLink = CardGameManager.LaunchDeepLink;
            _defaultGame = PlayerPrefs.GetString(CardGameManager.PlayerPrefsDefaultGame, string.Empty);
            _hasSeenWelcome = PlayerPrefs.GetInt(HasSeenWelcome, 0);
            PlayerPrefs.SetInt(HasSeenWelcome, 1);
            _actionsEnabled = InputSystem.actions.enabled;
            InputSystem.actions.Enable();
            _backgroundBehavior = InputSystem.settings.backgroundBehavior;
            _editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CardGameManager.LaunchDeepLink = _launchDeepLink;
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, _defaultGame);
            PlayerPrefs.SetInt(HasSeenWelcome, _hasSeenWelcome);
            foreach (var device in _devices.Where(device => device.added))
                InputSystem.RemoveDevice(device);
            _devices.Clear();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            var manager = Object.FindObjectsByType<CardGameManager>().FirstOrDefault();
            if (manager != null)
            {
                manager.StopAllCoroutines();
                for (var i = 0; i < 10 && manager.Messenger != null && manager.Messenger.IsShowing; i++)
                    manager.Messenger.OkClose();
                Object.Destroy(manager.gameObject);
            }

            var empty = SceneManager.CreateScene("StartupLoadingTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);

            foreach (var directory in _fixtureDirectories.Where(Directory.Exists))
                Directory.Delete(directory, true);
            _fixtureDirectories.Clear();
            if (!_actionsEnabled)
                InputSystem.actions.Disable();
            InputSystem.settings.backgroundBehavior = _backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInputBehavior;
        }

        [UnityTest]
        public IEnumerator MainMenuIsInteractiveWhileLargeGameContentLoads()
        {
            const int pages = 40;
            const int cardsPerPage = 250;
            // Install the bundled games first, so that the fixture is not the only game
            yield return ColdStart();
            var gameId = CreateFixture("StartupLoadingLarge", pages, cardsPerPage);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            _devices.Add(keyboard);
            bool? loadedWhenSettingsOpened = null;

            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (scene.buildIndex == Tags.SettingsSceneIndex)
                    loadedWhenSettingsOpened ??= CardGameManager.Current.HasLoaded;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            try
            {
                yield return ColdStart();

                // The first Main Menu frame shows the game from its properties, before its cards load
                Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
                Assert.AreEqual(gameId, CardGameManager.Current.Id);
                Assert.IsTrue(CardGameManager.Current.HasReadProperties);
                Assert.IsTrue(CardGameManager.Current.HasPreviewImages);
                Assert.IsFalse(CardGameManager.Current.HasLoaded);
                Assert.IsFalse(CardGameManager.IsCurrentContentReady);

                // The menu acts on input while the cards are still loading
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Delete));
                yield return WaitUntil(() => SceneManager.GetActiveScene().buildIndex == Tags.SettingsSceneIndex);
                Assert.AreEqual(false, loadedWhenSettingsOpened, "The menu must accept input before cards load");
                // Settings does not need the cards, so it stays usable while they load
                yield return null;
                Assert.IsFalse(CardGameManager.Current.HasLoaded);
                Assert.IsFalse(CardGameManager.Instance.Progress.gameObject.activeSelf);
                Assert.IsNull(CardGameManager.Instance.ModalCanvas);

                // The cards finish loading in the background, once
                yield return WaitUntil(() => CardGameManager.IsCurrentContentReady);
                Assert.AreEqual(gameId, CardGameManager.Current.Id);
                Assert.AreEqual(pages * cardsPerPage, CardGameManager.Current.Cards.Count);
                Assert.IsTrue(string.IsNullOrEmpty(CardGameManager.Current.Error));
                // Games that game selection does not show yet decode their images in the background
                yield return WaitUntil(() =>
                    CardGameManager.Instance.AllCardGames.Values.All(game => game.HasPreviewImages));
            }
            finally
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        [UnityTest]
        public IEnumerator CorruptGameShowsLoadErrorWithoutBlockingTheMenu()
        {
            yield return ColdStart();
            var gameId = CreateFixture("StartupLoadingCorrupt", 1, 10);
            var gameDirectory = _fixtureDirectories.Last();
            File.WriteAllText(Path.Combine(gameDirectory, "AllCards.json"), "{ not valid json");
            LogAssert.ignoreFailingMessages = true;

            yield return ColdStart();
            Assert.AreEqual(gameId, CardGameManager.Current.Id);
            Assert.IsFalse(CardGameManager.Instance.Messenger.IsShowing, "The menu appears before the load fails");

            yield return WaitUntil(() => CardGameManager.Instance.Messenger.IsShowing);
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.IsFalse(string.IsNullOrEmpty(CardGameManager.Current.Error));
            Assert.IsFalse(CardGameManager.IsCurrentContentReady);
            Assert.IsTrue(CardGameManager.IsCurrentContentSettled, "Screens waiting for content must not wait forever");
            Assert.IsNotNull(Object.FindObjectsByType<MainMenu>().FirstOrDefault());
        }

        [UnityTest]
        public IEnumerator GameDueForUpdateUpdatesOnlyAfterTheMenuIsInteractive()
        {
            yield return ColdStart();
            var gameId = CreateFixture("StartupLoadingUpdate", 1, 10, autoUpdate: 0,
                autoUpdateUrl: UnreachableGameUrl);
            LogAssert.ignoreFailingMessages = true;
            var downloadFrame = -1;

            void OnLog(string message, string stackTrace, LogType type)
            {
                if (message == "Download Start" && downloadFrame < 0)
                    downloadFrame = Time.frameCount;
            }

            Application.logMessageReceived += OnLog;
            try
            {
                yield return ColdStart();
                Assert.AreEqual(gameId, CardGameManager.Current.Id);
                Assert.IsFalse(CardGameManager.Current.IsDownloading);
                Assert.Less(downloadFrame, 0, "The update must not start before the menu is shown");
                Assert.IsTrue(Object.FindObjectsByType<MainMenu>().Single().isActiveAndEnabled);

                // The update starts after the menu's first frame
                yield return WaitUntil(() => downloadFrame >= 0);

                // The update fails against a closed port; the game then loads from what is installed
                yield return WaitUntil(() => CardGameManager.IsCurrentContentReady);
                Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }

        [UnityTest]
        public IEnumerator CardsExplorerOpenedByALinkWhileCardsLoadShowsProgressThenFillsIn()
        {
            const int pages = 40;
            const int cardsPerPage = 50;
            yield return ColdStart();
            var gameId = CreateFixture("StartupLoadingExplorer", pages, cardsPerPage);
            var loads = new LoadCounter();

            yield return ColdStart();
            Assert.IsFalse(CardGameManager.Current.HasLoaded);

            // A link arriving while CGS runs opens the screen before the cards have loaded
            CardGameManager.Instance.OnDeepLinkActivated("cardgamesim://link/cards");
            yield return WaitUntil(() => SceneManager.GetActiveScene().buildIndex == Tags.CardsExplorerSceneIndex,
                loads);
            yield return null;
            loads.Sample();
            Assert.IsFalse(CardGameManager.Current.HasLoaded, "The screen must open before the cards load");
            Assert.IsTrue(CardGameManager.Instance.Progress.gameObject.activeSelf, "The screen shows loading progress");
            Assert.IsNotNull(CardGameManager.Instance.ModalCanvas);
            var searchResults = Object.FindObjectsByType<SearchResults>().Single();
            Assert.AreEqual(0, SearchResultCount(searchResults));

            // Once the cards load, the progress closes and the search results fill in, from a single load
            yield return WaitUntil(() => CardGameManager.IsCurrentContentReady, loads);
            yield return CardGameManager.WaitForCurrentContent();
            yield return null;
            Assert.AreEqual(gameId, CardGameManager.Current.Id);
            Assert.AreEqual(pages * cardsPerPage, CardGameManager.Current.Cards.Count);
            Assert.AreEqual(pages * cardsPerPage, SearchResultCount(searchResults));
            Assert.IsFalse(CardGameManager.Instance.Progress.gameObject.activeSelf);
            Assert.AreEqual(1, loads.Count, "The cards must load once");
        }

        [UnityTest]
        public IEnumerator DeckEditorOffersSavedDecksOnlyOnceCardsHaveLoaded()
        {
            const int pages = 40;
            const int cardsPerPage = 50;
            yield return ColdStart();
            var gameId = CreateFixture("StartupLoadingDecks", pages, cardsPerPage);
            var decksDirectory = Path.Combine(_fixtureDirectories.Last(), "decks");
            Directory.CreateDirectory(decksDirectory);
            File.WriteAllText(Path.Combine(decksDirectory, "Saved.txt"), "1 Card 1-0");
            var loads = new LoadCounter();

            yield return ColdStart();
            Assert.IsFalse(CardGameManager.Current.HasLoaded);

            CardGameManager.Instance.OnDeepLinkActivated("cardgamesim://link/decks");
            yield return WaitUntil(() => SceneManager.GetActiveScene().buildIndex == Tags.DeckEditorSceneIndex,
                loads);
            // Record each refresh of the screen, and whether the cards had loaded by then
            var refreshes = new List<bool>();
            CardGameManager.Instance.OnSceneActions.Add(() => refreshes.Add(CardGameManager.Current.HasLoaded));
            yield return null;
            loads.Sample();
            Assert.IsFalse(CardGameManager.Current.HasLoaded, "The screen must open before the cards load");
            Assert.IsTrue(CardGameManager.Instance.Progress.gameObject.activeSelf, "The screen shows loading progress");
            Assert.IsFalse(IsDeckLoadMenuShowing(), "A deck cannot load before the game's cards");

            // The saved decks are offered once the cards load and the screen has refreshed with them
            yield return WaitUntil(IsDeckLoadMenuShowing, loads);
            Assert.IsTrue(CardGameManager.IsCurrentContentReady);
            Assert.IsFalse(CardGameManager.Instance.Progress.gameObject.activeSelf);
            Assert.Contains(true, refreshes, "The screen fills in with the cards before offering decks");
            Assert.AreEqual(gameId, CardGameManager.Current.Id);
            Assert.AreEqual(pages * cardsPerPage, CardGameManager.Current.Cards.Count);
            Assert.AreEqual(1, loads.Count, "The cards must load once");

            // The player picks the saved deck, and no later refresh clears it
            var deckLoadMenu = Object.FindObjectsByType<DeckLoadMenu>().Single(menu => menu.gameObject.activeSelf);
            var deckFiles = (SortedList<string, string>)typeof(DeckLoadMenu)
                .GetField("_deckFiles", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(deckLoadMenu);
            Assert.IsNotNull(deckFiles);
            typeof(DeckLoadMenu).GetField("_selectedFilePath", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(deckLoadMenu, deckFiles.Keys.Single());
            deckLoadMenu.LoadFromFileAndHide();
            var refreshesAtDeckLoad = refreshes.Count;
            for (var frame = 0; frame < 10; frame++)
                yield return null;
            Assert.IsFalse(CardGameManager.Instance.IsContentLoadScheduled);
            Assert.IsFalse(CardGameManager.Current.IsLoading);
            Assert.AreEqual(refreshesAtDeckLoad, refreshes.Count, "The screen must not refresh after a deck loads");
            var deckEditor = Object.FindObjectsByType<DeckEditor>().Single();
            Assert.AreEqual(1, deckEditor.CardModels.Count, "The loaded deck stays in the editor");
            Assert.AreEqual("Card 1-0", deckEditor.CardModels.Single().Value.Name);
            var savedDeck = (FinolDigital.Cgs.Json.Deck)typeof(DeckEditor)
                .GetProperty("SavedDeck", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(deckEditor);
            Assert.AreEqual("Saved", savedDeck?.Name);
            Assert.AreEqual(1, loads.Count, "Loading a deck must not reload the cards");
        }

        [UnityTest]
        public IEnumerator RapidGameChangesKeepOnlyTheLatestSelection()
        {
            const int pages = 40;
            const int cardsPerPage = 50;
            yield return ColdStart();
            var first = CreateFixture("StartupLoadingRapidA", pages, cardsPerPage);
            var second = CreateFixture("StartupLoadingRapidB", pages, cardsPerPage);
            var third = CreateFixture("StartupLoadingRapidC", pages, cardsPerPage);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, first);

            yield return ColdStart();
            Assert.AreEqual(first, CardGameManager.Current.Id);
            Assert.IsFalse(CardGameManager.Current.HasLoaded);

            // The player switches games several times, faster than the games load
            foreach (var gameId in new[] { second, third, first, second, third })
            {
                CardGameManager.Instance.Select(gameId);
                yield return null;
                yield return null;
            }

            Assert.AreEqual(third, CardGameManager.Current.Id);
            var games = new[] { first, second, third }.Select(id => CardGameManager.Instance.AllCardGames[id]).ToList();

            // Every load finishes, and none of them changes the selection
            yield return WaitUntil(() => games.All(game => !game.IsLoading) && CardGameManager.IsCurrentContentReady);
            yield return null;
            Assert.AreEqual(third, CardGameManager.Current.Id);
            Assert.AreEqual(pages * cardsPerPage, CardGameManager.Current.Cards.Count);
            Assert.AreEqual(third, PlayerPrefs.GetString(CardGameManager.PlayerPrefsDefaultGame));
            var mainMenu = Object.FindObjectsByType<MainMenu>().Single();
            var nameText = (Text)typeof(MainMenu).GetField("currentGameNameText",
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(mainMenu);
            Assert.IsNotNull(nameText);
            Assert.AreEqual("StartupLoadingRapidC", nameText.text);
        }

        /// <summary>
        /// Writes a game with <paramref name="pages"/> card files and selects it as the saved default. The game loads
        /// one card file per frame, so a game with many pages keeps loading for many frames.
        /// </summary>
        private string CreateFixture(string name, int pages, int cardsPerPage, int autoUpdate = -1,
            string autoUpdateUrl = null)
        {
            var directory = Path.Combine(UnityCardGame.GamesDirectoryPath, name);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            _fixtureDirectories.Add(directory);

            var definition = new Dictionary<string, object>
            {
                ["name"] = name,
                ["autoUpdate"] = autoUpdate,
                ["allCardsUrlPageCount"] = pages,
                ["allCardsUrlPageCountStartIndex"] = 1
            };
            if (autoUpdateUrl != null)
                definition["autoUpdateUrl"] = autoUpdateUrl;
            File.WriteAllText(Path.Combine(directory, "cgs.json"), JsonConvert.SerializeObject(definition));

            for (var page = 1; page <= pages; page++)
            {
                var cards = Enumerable.Range(0, cardsPerPage).Select(index => new Dictionary<string, string>
                {
                    ["id"] = $"{name}-{page}-{index}",
                    ["name"] = $"Card {page}-{index}",
                    ["set"] = "FIX"
                });
                var fileName = page == 1 ? "AllCards.json" : $"AllCards.json{page}";
                File.WriteAllText(Path.Combine(directory, fileName), JsonConvert.SerializeObject(cards));
            }

            // Reading the definition gives the game its ID, and may rename its directory to match
            var game = new UnityCardGame(null, name);
            game.ReadProperties(false);
            Assert.IsTrue(string.IsNullOrEmpty(game.Error), game.Error);
            if (!_fixtureDirectories.Contains(game.GameDirectoryPath))
                _fixtureDirectories.Add(game.GameDirectoryPath);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, game.Id);
            return game.Id;
        }

        /// <summary>Starts CGS as a launch does: a fresh CardGameManager in the first scene.</summary>
        private static IEnumerator ColdStart()
        {
            // Close the previous launch's screens first; they expect a CardGameManager while they run
            var empty = SceneManager.CreateScene("StartupLoadingTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => SceneNames.Contains(scene.name)).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);

            foreach (var manager in Object.FindObjectsByType<CardGameManager>())
            {
                manager.StopAllCoroutines();
                Object.Destroy(manager.gameObject);
            }

            yield return null;
            CardGameManager.LaunchDeepLink = () => null;
            yield return SceneManager.LoadSceneAsync(0);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, LoadCounter loads = null)
        {
            loads?.Sample();
            for (var frame = 0; frame < TimeoutFrames && !condition(); frame++)
            {
                yield return null;
                loads?.Sample();
            }
            Assert.IsTrue(condition(), "Timed out waiting for the condition");
        }

        private static int SearchResultCount(SearchResults searchResults) =>
            ((ICollection)typeof(SearchResults).GetProperty("AllResults", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(searchResults))?.Count ?? -1;

        private static bool IsDeckLoadMenuShowing() =>
            Object.FindObjectsByType<DeckLoadMenu>().Any(menu => menu.gameObject.activeSelf);

        /// <summary>Counts the loads of the current game that start while it is sampled every frame.</summary>
        private sealed class LoadCounter
        {
            private UnityCardGame _game;
            private bool _wasLoading;

            public int Count { get; private set; }

            public void Sample()
            {
                var game = CardGameManager.Current;
                if (game != _game)
                {
                    _game = game;
                    _wasLoading = false;
                }

                if (game.IsLoading && !_wasLoading)
                    Count++;
                _wasLoading = game.IsLoading;
            }
        }
    }
}
