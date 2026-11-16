/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cgs;
using Cgs.Menu;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>CGS opens directly on the Main Menu: deep links, held input, and first-frame input.</summary>
    public class MainMenuLaunchTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const int SceneTimeoutFrames = 600;
        private const string UnreachableGameUrl = "http://127.0.0.1:9/cgs.json";

        private static readonly string[] SceneNames =
            { "MainMenu", "PlayGame", "DeckEditor", "CardsExplorer", "Settings" };

        private static IEnumerable<TestCaseData> SceneLinks()
        {
            yield return new TestCaseData("cardgamesim://link/main", Tags.MainMenuSceneIndex).Returns(null).SetName("main");
            yield return new TestCaseData("cardgamesim://link/play", Tags.PlayModeSceneIndex).Returns(null).SetName("play");
            yield return new TestCaseData("cardgamesim://link/decks", Tags.DeckEditorSceneIndex).Returns(null).SetName("decks");
            yield return new TestCaseData("cardgamesim://link/cards", Tags.CardsExplorerSceneIndex).Returns(null).SetName("cards");
            yield return new TestCaseData("cardgamesim://link/settings", Tags.SettingsSceneIndex).Returns(null).SetName("settings");
        }

        private readonly List<InputDevice> _devices = new();
        private Func<string> _launchDeepLink;
        private int _hasSeenWelcome;
        private bool _actionsEnabled;
        private InputSettings.BackgroundBehavior _backgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInputBehavior;

        [SetUp]
        public void SetUp()
        {
            _launchDeepLink = CardGameManager.LaunchDeepLink;
            _hasSeenWelcome = PlayerPrefs.GetInt(HasSeenWelcome, 0);
            PlayerPrefs.SetInt(HasSeenWelcome, 1);
            // The test runner disables project-wide actions; the Main Menu listens to them
            _actionsEnabled = InputSystem.actions.enabled;
            InputSystem.actions.Enable();
            // Batch Editors have no focused Game view; still process the simulated devices
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
            PlayerPrefs.SetInt(HasSeenWelcome, _hasSeenWelcome);
            foreach (var device in _devices.Where(device => device.added))
                InputSystem.RemoveDevice(device);
            _devices.Clear();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
            var messenger = Object.FindObjectsByType<CardGameManager>().FirstOrDefault()?.Messenger;
            for (var i = 0; i < 10 && messenger != null && messenger.IsShowing; i++)
                messenger.OkClose();

            // Leave an empty scene so the next test does not inherit a menu that listens to input
            var empty = SceneManager.CreateScene("MainMenuLaunchTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);
            if (!_actionsEnabled)
                InputSystem.actions.Disable();
            InputSystem.settings.backgroundBehavior = _backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInputBehavior;
        }

        [UnityTest, TestCaseSource(nameof(SceneLinks))]
        public IEnumerator LaunchLinkOpensItsScreen(string link, int sceneIndex)
        {
            yield return ColdStart(link);

            yield return WaitForScene(sceneIndex);
            Assert.AreEqual(SceneNames[sceneIndex], SceneManager.GetActiveScene().name);
        }

        [UnityTest, TestCaseSource(nameof(SceneLinks))]
        public IEnumerator LinkWhileRunningOpensItsScreen(string link, int sceneIndex)
        {
            var startIndex = sceneIndex == Tags.SettingsSceneIndex ? Tags.MainMenuSceneIndex : Tags.SettingsSceneIndex;
            yield return ColdStart(null);
            yield return SceneManager.LoadSceneAsync(startIndex);
            Assert.AreEqual(SceneNames[startIndex], SceneManager.GetActiveScene().name);

            CardGameManager.Instance.OnDeepLinkActivated(link);

            yield return WaitForScene(sceneIndex);
            Assert.AreEqual(SceneNames[sceneIndex], SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator LaunchLinkWithGameUrlStaysOnMainMenuAndDownloads()
        {
            // The download fails quickly against a closed local port; this checks that it starts from the link
            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Log, "GetAutoUpdateUrl::autoUpdateUrl: " + UnreachableGameUrl);
            LogAssert.Expect(LogType.Log, "DownloadCardGame: start");

            yield return ColdStart("cardgamesim://link/main?url=" + Uri.EscapeDataString(UnreachableGameUrl));
            for (var frame = 0; frame < 10; frame++)
                yield return null;

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Object.FindObjectsByType<MainMenu>().FirstOrDefault());
        }

        [UnityTest]
        public IEnumerator LinkWithGameUrlWhileRunningDownloads()
        {
            yield return ColdStart(null);
            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Log, "GetAutoUpdateUrl::autoUpdateUrl: " + UnreachableGameUrl);
            LogAssert.Expect(LogType.Log, "DownloadCardGame: start");

            CardGameManager.Instance.OnDeepLinkActivated(
                "cardgamesim://link/main?url=" + Uri.EscapeDataString(UnreachableGameUrl));
            for (var frame = 0; frame < 10; frame++)
                yield return null;

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator FirstFramePressActs()
        {
            var keyboard = Add<Keyboard>();
            yield return ColdStart(null);

            // MainMenu/Settings is bound to Delete; no fixed delay may drop this press
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Delete));
            yield return WaitForScene(Tags.SettingsSceneIndex);

            Assert.AreEqual("Settings", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator KeyHeldFromBeforeIsIgnoredUntilReleasedAndPressedAgain()
        {
            var keyboard = Add<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Delete));
            yield return null;

            yield return ColdStart(null);
            for (var frame = 0; frame < 5; frame++)
                yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "A held key must not act");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            for (var frame = 0; frame < 3; frame++)
                yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Releasing the key must not act");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Delete));
            yield return WaitForScene(Tags.SettingsSceneIndex);
            Assert.AreEqual("Settings", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator PointerHeldFromBeforeCannotClickUntilReleased()
        {
            var mouse = Add<Mouse>();
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;

            yield return ColdStart(null);
            var mainMenu = Object.FindObjectsByType<MainMenu>().Single();
            mainMenu.ShowSettings();
            yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "A held button must not click");

            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
            mainMenu.ShowSettings();
            yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Its release must not click");

            yield return null;
            mainMenu.ShowSettings();
            yield return WaitForScene(Tags.SettingsSceneIndex);
            Assert.AreEqual("Settings", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator GamepadPlayerHasAControlSelectedAndTheFirstPressActs()
        {
            var gamepad = Add<Gamepad>();
            yield return null;
            Assert.IsTrue(ActiveInputDevice.UseGamepad);

            yield return ColdStart(null);
            var frames = 0;
            while (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            {
                Assert.Less(frames++, 2, "A Main Menu control must be selected on the first interactive frame");
                yield return null;
            }

            Assert.AreEqual("Current Card", EventSystem.current.currentSelectedGameObject.name);

            // South submits the selected game, which opens game management
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return null;
            yield return null;
            Assert.IsTrue(Object.FindObjectsByType<GamesManagementMenu>()
                .Any(menu => menu.gameObject.activeInHierarchy), "The first gamepad press must act");
        }

        private T Add<T>() where T : InputDevice
        {
            var device = InputSystem.AddDevice<T>();
            _devices.Add(device);
            return device;
        }

        /// <summary>Starts CGS as a launch does: a fresh CardGameManager in the first scene sees the launch link.</summary>
        private static IEnumerator ColdStart(string launchLink)
        {
            foreach (var manager in Object.FindObjectsByType<CardGameManager>())
                Object.Destroy(manager.gameObject);
            yield return null;
            CardGameManager.LaunchDeepLink = () => launchLink;
            yield return SceneManager.LoadSceneAsync(0);
        }

        private static IEnumerator WaitForScene(int sceneIndex)
        {
            for (var frame = 0; frame < SceneTimeoutFrames &&
                                SceneManager.GetActiveScene().buildIndex != sceneIndex; frame++)
                yield return null;
            // Let the scene run its first frame
            yield return null;
        }
    }
}
