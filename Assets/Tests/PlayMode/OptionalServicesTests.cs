/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cgs;
using Cgs.Menu;
using Cgs.Performance;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>Online services, LAN discovery, and Steam start on first use, not at launch.</summary>
    public class OptionalServicesTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const int MenuTimeoutFrames = 600;
        private const string ExpectedFailure = "Expected optional services test failure.";

        private Func<Task> _initializeServices;
        private Func<string> _launchDeepLink;
        private int _hasSeenWelcome;
        private int _serviceStarts;
        private bool _isMenuLoaded;

        [SetUp]
        public void SetUp()
        {
            _initializeServices = CgsNetManager.InitializeServices;
            _launchDeepLink = CardGameManager.LaunchDeepLink;
            _hasSeenWelcome = PlayerPrefs.GetInt(HasSeenWelcome, 0);
            PlayerPrefs.SetInt(HasSeenWelcome, 1);
            _serviceStarts = 0;
            _isMenuLoaded = false;
            CgsNetManager.ResetServices();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            CgsNetManager.InitializeServices = _initializeServices;
            CgsNetManager.ResetServices();
            CardGameManager.LaunchDeepLink = _launchDeepLink;
            PlayerPrefs.SetInt(HasSeenWelcome, _hasSeenWelcome);
            if (!_isMenuLoaded)
                yield break;

            // Leave an empty scene so the next test does not inherit a menu that listens to input
            var empty = SceneManager.CreateScene("OptionalServicesTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        private Task CountServiceStart(Task result)
        {
            _serviceStarts++;
            return result;
        }

        [UnityTest]
        public IEnumerator LaunchLeavesOnlineServicesAndLanDiscoveryStopped()
        {
            CgsNetManager.InitializeServices = () => CountServiceStart(Task.CompletedTask);

            foreach (var manager in Object.FindObjectsByType<CardGameManager>())
                Object.Destroy(manager.gameObject);
            yield return null;
            CardGameManager.LaunchDeepLink = () => null;
            _isMenuLoaded = true;
            yield return SceneManager.LoadSceneAsync(Tags.MainMenuSceneIndex);
            var mainMenu = Object.FindObjectsByType<MainMenu>().Single();
            for (var frame = 0; frame < MenuTimeoutFrames && mainMenu.IsWaitingForLanguage; frame++)
                yield return null;
            // Give deferred start-up work a few frames to run
            for (var frame = 0; frame < 10; frame++)
                yield return null;

            Assert.AreEqual(0, _serviceStarts, "Online services must wait for the first online action");
            var netManager = CgsNetManager.Instance;
            Assert.IsFalse(netManager.IsListening, "Nothing may host or join at launch");
            var discovery = netManager.GetComponent<CgsNetDiscovery>();
            Assert.IsTrue(discovery == null || !discovery.IsRunning, "LAN discovery must wait for the lobby");
            Assert.IsNull(netManager.GetComponent<BrowserLanTransport>(), "Browser LAN must wait for the lobby");
        }

        [Test]
        public void ServicesStartOnceWhenFirstNeeded()
        {
            var initialization = new TaskCompletionSource<bool>();
            CgsNetManager.InitializeServices = () => CountServiceStart(initialization.Task);

            var first = CgsNetManager.EnsureServicesAsync();
            var second = CgsNetManager.EnsureServicesAsync();
            initialization.SetResult(true);

            Assert.AreEqual(1, _serviceStarts);
            Assert.AreSame(first, second);
            Assert.IsTrue(CgsNetManager.EnsureServicesAsync().IsCompletedSuccessfully);
            Assert.AreEqual(1, _serviceStarts);
        }

        [Test]
        public void FailedServiceStartIsRetriedByTheNextCaller()
        {
            CgsNetManager.InitializeServices =
                () => CountServiceStart(Task.FromException(new InvalidOperationException(ExpectedFailure)));

            Assert.IsTrue(CgsNetManager.EnsureServicesAsync().IsFaulted);
            CgsNetManager.InitializeServices = () => CountServiceStart(Task.CompletedTask);

            Assert.IsTrue(CgsNetManager.EnsureServicesAsync().IsCompletedSuccessfully);
            Assert.AreEqual(2, _serviceStarts);
        }

        [UnityTest]
        public IEnumerator SigningInStartsServicesFirst()
        {
            CgsNetManager.InitializeServices =
                () => CountServiceStart(Task.FromException(new InvalidOperationException(ExpectedFailure)));
            LogAssert.Expect(LogType.Exception, new Regex(Regex.Escape(ExpectedFailure)));
            LogAssert.Expect(LogType.Error, "[CgsNet] Sign in anonymously failed!");

            var signIn = CgsNetManager.SignInAnonymouslyAsync();
            for (var frame = 0; frame < 10 && !signIn.IsCompleted; frame++)
                yield return null;

            Assert.IsTrue(signIn.IsFaulted, "Sign-in must report that services could not start");
            Assert.AreEqual(1, _serviceStarts);
            Assert.IsFalse(CgsNetManager.IsSignedIn);
        }

        [Test]
        public void SteamStartsOnlyWhenSteamLaunchedCgs()
        {
            Assert.IsFalse(SteamLaunch.IsDetected(Environment(), _ => false), "A non-Steam build skips Steam");
            Assert.IsTrue(SteamLaunch.IsDetected(Environment(("SteamAppId", "1742850")), _ => false));
            Assert.IsTrue(SteamLaunch.IsDetected(Environment(("SteamGameId", "1742850")), _ => false));
            Assert.IsTrue(SteamLaunch.IsDetected(Environment(), path => path == SteamLaunch.AppIdFile),
                "The development App ID file starts Steam");
            Assert.IsFalse(SteamLaunch.IsDetected(Environment(("SteamAppId", "")), _ => false));

            Assert.IsTrue(SteamLaunch.IsSteamDeck(Environment(("SteamDeck", "1"))));
            Assert.IsFalse(SteamLaunch.IsSteamDeck(Environment(("SteamDeck", "0"))));
            Assert.IsFalse(SteamLaunch.IsSteamDeck(Environment()));
        }

        [Test]
        public void SteamWaitsForTheFirstUsableScreen()
        {
            double now = 0;
            var session = new StartupTimelineSession(() => now, _ => { }, 30_000, 60_000);
            session.Mark(StartupMilestone.FirstSceneLoaded);
            Assert.IsFalse(SteamLaunch.IsReadyToStart(session, Tags.MainMenuSceneIndex));

            now = 200;
            session.Mark(StartupMilestone.FirstFrameDrawn);
            Assert.IsFalse(SteamLaunch.IsReadyToStart(session, Tags.MainMenuSceneIndex),
                "The Main Menu's first frame is not yet usable");
            Assert.IsTrue(SteamLaunch.IsReadyToStart(session, Tags.PlayModeSceneIndex),
                "A launch link that opened another screen does not wait for the Main Menu");

            now = 300;
            session.Mark(StartupMilestone.MainMenuInteractive);
            Assert.IsTrue(SteamLaunch.IsReadyToStart(session, Tags.MainMenuSceneIndex));

            var stuck = new StartupTimelineSession(() => now, _ => { }, 30_000, 60_000);
            now = 30_000;
            stuck.Tick();
            Assert.IsTrue(SteamLaunch.IsReadyToStart(stuck, Tags.MainMenuSceneIndex),
                "A launch that timed out still starts Steam");
        }

        private static Func<string, string> Environment(params (string name, string value)[] variables)
        {
            var values = variables.ToDictionary(variable => variable.name, variable => variable.value);
            return name => values.GetValueOrDefault(name);
        }
    }
}
