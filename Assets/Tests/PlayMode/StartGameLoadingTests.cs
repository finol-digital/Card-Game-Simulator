/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Linq;
using Cgs;
using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>Start Game keeps the Main Menu drawing while the play screen loads, and the play screen opens light.</summary>
    public class StartGameLoadingTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const int TimeoutFrames = 1200;

        private Func<string> _launchDeepLink;
        private int _hasSeenWelcome;
        private string _defaultGame;
        private bool _isMenuLoaded;

        [SetUp]
        public void SetUp()
        {
            _launchDeepLink = CardGameManager.LaunchDeepLink;
            _hasSeenWelcome = PlayerPrefs.GetInt(HasSeenWelcome, 0);
            _defaultGame = PlayerPrefs.GetString(CardGameManager.PlayerPrefsDefaultGame, string.Empty);
            PlayerPrefs.SetInt(HasSeenWelcome, 1);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, Tags.StandardPlayingCardsDirectoryName);
            _isMenuLoaded = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CardGameManager.LaunchDeepLink = _launchDeepLink;
            PlayerPrefs.SetInt(HasSeenWelcome, _hasSeenWelcome);
            if (string.IsNullOrEmpty(_defaultGame))
                PlayerPrefs.DeleteKey(CardGameManager.PlayerPrefsDefaultGame);
            else
                PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, _defaultGame);
            if (!_isMenuLoaded)
                yield break;

            // A play screen still loading would open after cleanup, so let it finish first
            for (var frame = 0; frame < TimeoutFrames &&
                                Object.FindObjectsByType<MainMenu>().Any(menu => menu.IsOpeningPlayScreen); frame++)
                yield return null;
            if (CgsNetManager.Instance != null)
                CgsNetManager.Instance.Stop();
            var messenger = CardGameManager.Instance != null ? CardGameManager.Instance.Messenger : null;
            for (var i = 0; i < 10 && messenger != null && messenger.IsShowing; i++)
                messenger.OkClose();

            // Leave an empty scene so the next test does not inherit a screen that listens to input
            var empty = SceneManager.CreateScene("StartGameLoadingTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        private IEnumerator OpenMainMenu()
        {
            foreach (var manager in Object.FindObjectsByType<CardGameManager>())
                Object.Destroy(manager.gameObject);
            yield return null;
            CardGameManager.LaunchDeepLink = () => null;
            _isMenuLoaded = true;
            yield return SceneManager.LoadSceneAsync(Tags.MainMenuSceneIndex);
            var mainMenu = Object.FindObjectsByType<MainMenu>().Single();
            for (var frame = 0; frame < TimeoutFrames && mainMenu.IsWaitingForLanguage; frame++)
                yield return null;
            // Let the held-input gate see a frame without presses
            yield return null;
        }

        private static int PlayControllerCount => Object.FindObjectsByType<PlayController>().Length;

        [UnityTest]
        public IEnumerator TheMainMenuKeepsDrawingWhileThePlayScreenLoads()
        {
            yield return OpenMainMenu();
            var mainMenu = Object.FindObjectsByType<MainMenu>().Single();

            mainMenu.StartGame();
            Assert.IsTrue(mainMenu.IsOpeningPlayScreen, "Start Game starts opening the play screen");
            // A second press, or another menu button, must not load another screen
            mainMenu.StartGame();
            mainMenu.ShowSettings();
            mainMenu.EditDeck();

            yield return null;
            Assert.IsTrue(mainMenu != null && mainMenu.isActiveAndEnabled,
                "The menu is still drawn the frame after Start Game, while the play screen loads");
            Assert.AreEqual(0, PlayControllerCount, "The play screen has not replaced the menu yet");

            for (var frame = 0; frame < TimeoutFrames && PlayController.Instance == null; frame++)
                yield return null;
            Assert.IsNotNull(PlayController.Instance, "The play screen opens");
            yield return null;
            Assert.AreEqual(1, PlayControllerCount, "The play screen is loaded only once");
            Assert.AreEqual(Tags.PlayModeSceneIndex, SceneManager.GetActiveScene().buildIndex,
                "No other screen replaces the play screen");
            Assert.IsTrue(mainMenu == null, "The play screen replaces the menu");
        }

        [UnityTest]
        public IEnumerator ThePlayScreenCreatesItsViewersAfterItOpens()
        {
            yield return OpenMainMenu();
            Object.FindObjectsByType<MainMenu>().Single().StartGame();
            for (var frame = 0; frame < TimeoutFrames && PlayController.Instance == null; frame++)
                yield return null;
            yield return null;

            Assert.IsNull(CardViewer.Existing, "The card viewer is not created while the play screen opens");
            Assert.IsNull(PlayableViewer.Existing, "The playable viewer is not created while the play screen opens");
            Assert.IsFalse(PlayController.Instance.IsBlocked, "Checking for a shown viewer does not create one");
            Assert.IsNull(CardViewer.Existing, "Checking whether the table is blocked does not create a viewer");

            var until = Time.realtimeSinceStartup + PlayController.ViewerPrepareDelaySeconds + 10;
            while ((CardViewer.Existing == null || PlayableViewer.Existing == null) &&
                   Time.realtimeSinceStartup < until)
                yield return null;
            Assert.IsNotNull(CardViewer.Existing, "The card viewer is ready once the table is up");
            Assert.IsNotNull(PlayableViewer.Existing, "The playable viewer is ready once the table is up");
            Assert.IsTrue(CardViewer.Existing.IsActionable, "The play screen's card viewer offers card actions");
        }

        [UnityTest]
        public IEnumerator UsingAViewerBeforeItIsReadyCreatesIt()
        {
            yield return OpenMainMenu();
            Object.FindObjectsByType<MainMenu>().Single().StartGame();
            for (var frame = 0; frame < TimeoutFrames && PlayController.Instance == null; frame++)
                yield return null;

            Assert.IsNull(CardViewer.Existing, "The card viewer waits until it is needed");
            var cardViewer = CardViewer.Instance;
            Assert.IsNotNull(cardViewer, "Using the card viewer creates it");
            Assert.AreSame(cardViewer, CardViewer.Existing, "The created card viewer is the one in use");
            Assert.IsTrue(cardViewer.IsActionable, "The play screen's card viewer offers card actions");
            Assert.IsNotNull(PlayableViewer.Instance, "Using the playable viewer creates it");
        }
    }
}
