/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cgs;
using Cgs.CardGameView.Multiplayer;
using Cgs.Localization;
using Cgs.Menu;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>Players can choose to always load a game's start decks, and turn that choice off in play settings.</summary>
    public class StartDeckChoiceTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const string StartDecksMessageKey = "play.load.decks";
        private const string GameId = Tags.StandardPlayingCardsDirectoryName;
        private const int TimeoutFrames = 1200;

        private Func<string> _launchDeepLink;
        private int _hasSeenWelcome;
        private string _defaultGame;
        private bool _alwaysLoadStartDecks;
        private bool _isMenuLoaded;

        [SetUp]
        public void SetUp()
        {
            _launchDeepLink = CardGameManager.LaunchDeepLink;
            _hasSeenWelcome = PlayerPrefs.GetInt(HasSeenWelcome, 0);
            _defaultGame = PlayerPrefs.GetString(CardGameManager.PlayerPrefsDefaultGame, string.Empty);
            _alwaysLoadStartDecks = PlaySettings.GetAlwaysLoadStartDecks(GameId);
            PlayerPrefs.SetInt(HasSeenWelcome, 1);
            PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, GameId);
            PlaySettings.SetAlwaysLoadStartDecks(GameId, false);
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
            PlaySettings.SetAlwaysLoadStartDecks(GameId, _alwaysLoadStartDecks);
            if (!_isMenuLoaded)
                yield break;

            if (CgsNetManager.Instance != null)
                CgsNetManager.Instance.Stop();
            var messenger = CardGameManager.Instance != null ? CardGameManager.Instance.Messenger : null;
            for (var i = 0; i < 10 && messenger != null && messenger.IsShowing; i++)
                messenger.OkClose();

            // Leave an empty scene so the next test does not inherit a table that listens to input
            var empty = SceneManager.CreateScene("StartDeckChoiceTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static Dialog Messenger => CardGameManager.Instance.Messenger;

        private static bool IsAskingForStartDecks => Messenger.CurrentMessageKey == StartDecksMessageKey;

        private static bool IsAlwaysOffered =>
            ((GameObject)typeof(Dialog).GetField("alwaysButton", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(Messenger))?.activeInHierarchy == true;

        private static bool HasStartDecks => PlayController.Instance != null &&
                                             PlayController.Instance.PlayAreaCardZone
                                                 .GetComponentsInChildren<CardStack>().Any();

        private IEnumerator OpenSoloTable()
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
            Assert.AreEqual(GameId, CardGameManager.Current.Id);

            CardGameManager.Instance.IsSearchingForServer = false;
            yield return SceneManager.LoadSceneAsync(Tags.PlayModeSceneIndex);
        }

        private static IEnumerator WaitFor(Func<bool> condition, string message)
        {
            for (var frame = 0; frame < TimeoutFrames && !condition(); frame++)
                yield return null;
            Assert.IsTrue(condition(), message);
        }

        [UnityTest]
        public IEnumerator ChoosingAlwaysLoadsTheStartDecksAndRemembersTheChoice()
        {
            yield return OpenSoloTable();
            yield return WaitFor(() => IsAskingForStartDecks, "The start decks are offered");
            Assert.IsTrue(IsAlwaysOffered, "The start-deck prompt offers to always load them");

            Messenger.ChooseAlways();
            Assert.IsTrue(PlaySettings.GetAlwaysLoadStartDecks(GameId), "The choice is remembered for this game");
            Assert.IsFalse(PlaySettings.GetAlwaysLoadStartDecks("Mahjong@www.cardgamesimulator.com"),
                "The choice is remembered for this game only");
            yield return WaitFor(() => HasStartDecks, "Always also loads the start decks now");

            // Other questions do not offer to always answer yes
            for (var i = 0; i < 10 && Messenger.IsShowing; i++)
                Messenger.OkClose();
            Messenger.Ask(new UiMessage("prompt.quit", "Quit?"), null, () => { });
            Assert.IsFalse(IsAlwaysOffered, "Only questions that can be remembered offer Always");
            Messenger.OkClose();
        }

        [UnityTest]
        public IEnumerator ARememberedChoiceLoadsTheStartDecksWithoutAsking()
        {
            PlaySettings.SetAlwaysLoadStartDecks(GameId, true);
            yield return OpenSoloTable();

            var asked = false;
            for (var frame = 0; frame < TimeoutFrames && !HasStartDecks; frame++)
            {
                asked |= IsAskingForStartDecks;
                yield return null;
            }

            Assert.IsTrue(HasStartDecks, "The start decks load by themselves");
            Assert.IsFalse(asked, "A remembered choice is not asked again");
        }

        [UnityTest]
        public IEnumerator TurningTheChoiceOffInPlaySettingsAsksAgain()
        {
            PlaySettings.SetAlwaysLoadStartDecks(GameId, true);
            yield return OpenSoloTable();
            yield return WaitFor(() => HasStartDecks, "The start decks load by themselves");

            PlayController.Instance.ShowPlaySettingsMenu();
            yield return null;
            var menu = Object.FindObjectsByType<PlaySettingsMenu>().Single();
            var toggle = (Toggle)typeof(PlaySettingsMenu).GetField("alwaysLoadStartDecksToggle",
                BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(menu);
            Assert.IsNotNull(toggle);
            Assert.IsTrue(toggle.gameObject.activeInHierarchy, "A game with start decks shows the choice");
            Assert.IsTrue(toggle.isOn, "Play settings show the remembered choice");

            toggle.isOn = false;
            Assert.IsFalse(PlaySettings.GetAlwaysLoadStartDecks(GameId), "Turning it off forgets the choice");
            menu.Hide();

            // The next game start asks again
            yield return SceneManager.LoadSceneAsync(Tags.PlayModeSceneIndex);
            yield return WaitFor(() => IsAskingForStartDecks, "The next game start asks again");
            Assert.IsTrue(IsAlwaysOffered);
        }
    }
}
