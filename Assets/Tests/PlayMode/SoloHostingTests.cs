/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cgs;
using Cgs.CardGameView.Multiplayer;
using Cgs.Menu;
using Cgs.Play;
using Cgs.Play.Drawer;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    /// <summary>A solo table is playable before CGS hosts it, and hosting keeps what is already on the table.</summary>
    public class SoloHostingTests
    {
        private const string HasSeenWelcome = "HasSeenWelcome";
        private const string StartDecksMessageKey = "play.load.decks";
        private const int TimeoutFrames = 1200;

        private Func<string> _launchDeepLink;
        private int _hasSeenWelcome;
        private string _defaultGame;
        private bool _isMenuLoaded;
        private NetworkManager _client;

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
            if (_client != null)
            {
                if (_client.IsListening)
                    _client.Shutdown();
                yield return null;
                Object.Destroy(_client.gameObject);
                _client = null;
            }

            CardGameManager.LaunchDeepLink = _launchDeepLink;
            PlayerPrefs.SetInt(HasSeenWelcome, _hasSeenWelcome);
            if (string.IsNullOrEmpty(_defaultGame))
                PlayerPrefs.DeleteKey(CardGameManager.PlayerPrefsDefaultGame);
            else
                PlayerPrefs.SetString(CardGameManager.PlayerPrefsDefaultGame, _defaultGame);
            if (!_isMenuLoaded)
                yield break;

            if (CgsNetManager.Instance != null)
                CgsNetManager.Instance.Stop();
            var messenger = CardGameManager.Instance != null ? CardGameManager.Instance.Messenger : null;
            for (var i = 0; i < 10 && messenger != null && messenger.IsShowing; i++)
                messenger.OkClose();

            // Leave an empty scene so the next test does not inherit a table that listens to input
            var empty = SceneManager.CreateScene("SoloHostingTests " + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                         .Where(scene => scene != empty).ToList())
                yield return SceneManager.UnloadSceneAsync(scene);
        }

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
            Assert.AreEqual(Tags.StandardPlayingCardsDirectoryName, CardGameManager.Current.Id);

            CardGameManager.Instance.IsSearchingForServer = false;
            yield return SceneManager.LoadSceneAsync(Tags.PlayModeSceneIndex);
        }

        // As a player would: accept the start decks, then deal the starting hand
        private static void AnswerStartPrompts()
        {
            var dealer = Object.FindObjectsByType<HandDealer>().FirstOrDefault(d => d.gameObject.activeInHierarchy);
            var messenger = CardGameManager.Instance.Messenger;
            if (dealer != null)
                dealer.Confirm();
            else if (messenger.IsShowing)
            {
                if (messenger.CurrentMessageKey == StartDecksMessageKey)
                    messenger.ChooseYes();
                else
                    messenger.ChooseNo();
            }
        }

        private static bool IsHosting => CgsNetManager.Instance != null && CgsNetManager.Instance.IsHost;

        private static bool HasDealtHand => PlayController.Instance != null && PlayController.Instance.Drawer.HasCards;

        // Dealing happens as a prompt is answered, so whenDealt runs in that same frame, before hosting could start
        private static IEnumerator DealStartingHand(Action whenDealt)
        {
            for (var frame = 0; frame < TimeoutFrames; frame++)
            {
                AnswerStartPrompts();
                if (HasDealtHand)
                {
                    whenDealt();
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("The starting hand was not dealt");
        }

        [UnityTest]
        public IEnumerator TheFirstCardIsDealtBeforeHostingStarts()
        {
            yield return OpenSoloTable();

            var wasOnlineAtFirstCard = true;
            yield return DealStartingHand(() => wasOnlineAtFirstCard = CgsNetManager.Instance.IsOnline);
            for (var frame = 0; frame < TimeoutFrames && !IsHosting; frame++)
            {
                AnswerStartPrompts();
                yield return null;
            }

            Assert.IsFalse(wasOnlineAtFirstCard, "The starting hand is dealt before hosting starts");
            Assert.IsTrue(IsHosting, "The solo table is hosted once it is set up, so others can still join");
        }

        [UnityTest]
        public IEnumerator HostingKeepsWhatIsAlreadyOnTheTableForPlayersWhoJoin()
        {
            yield return OpenSoloTable();
            // An open dialog keeps hosting waiting while the solo player uses the table
            yield return DealStartingHand(() => CardGameManager.Instance.Messenger.Show("Keeping the table busy"));
            for (var frame = 0; frame < 3; frame++)
                yield return null;

            var controller = PlayController.Instance;
            Assert.IsFalse(CgsNetManager.Instance.IsOnline, "Hosting waits while a dialog is open");
            var deck = controller.CurrentDeckStack;
            Assert.IsNotNull(deck, "The start deck is on the table");
            deck.IsTopFaceup = true;
            deck.Position += Vector2.right * 120;
            controller.CreateDefaultDie();
            var die = controller.PlayAreaCardZone.GetComponentInChildren<Die>();
            // A new die rolls for a moment first
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(CgsNetManager.Instance.IsOnline, "Hosting still waits while the dialog is open");
            die.Value = 4;
            die.Position = new Vector2(-200, 50);
            controller.CreateDefaultCounter();
            var counter = controller.PlayAreaCardZone.GetComponentInChildren<Counter>();
            counter.Value = 7;
            counter.Position = new Vector2(200, -50);
            controller.AddCard(deck.Cards[0], true);
            var card = controller.PlayAreaCardZone.GetComponentsInChildren<CardModel>()
                .Single(cardModel => cardModel.transform.parent == controller.PlayAreaCardZone.transform);
            card.Position = new Vector2(-50, -150);
            card.Rotation = Quaternion.Euler(0, 0, 90);
            controller.Scoreboard.ChangePoints("12");
            var hand = controller.Drawer.LocalHandCardIds[0].ToList();
            Assert.IsNotEmpty(hand, "The starting hand was dealt");

            var table = Snapshot(controller);
            CardGameManager.Instance.Messenger.OkClose();
            for (var frame = 0; frame < TimeoutFrames && !IsHosting; frame++)
                yield return null;
            Assert.IsTrue(IsHosting, "Hosting starts once the table is idle");

            var player = CgsNetManager.Instance.LocalPlayer;
            Assert.IsTrue(table.Keys.All(playable => playable.IsSpawned), "Every table object is spawned");
            AssertSameTable(table, playable => playable, "on the host");
            Assert.AreEqual(deck.MyNetworkObject, player.CurrentDeck, "The start deck is still the current deck");
            CollectionAssert.AreEqual(hand, player.HandCards[0].Select(unityCard => unityCard.Id),
                "The host keeps the dealt hand");
            Assert.AreEqual(12, player.Points, "The host keeps the points");

            yield return JoinAsAnotherPlayer();
            var replicas = _client.SpawnManager.SpawnedObjects;
            for (var frame = 0; frame < TimeoutFrames &&
                                !table.Keys.All(playable => replicas.ContainsKey(playable.NetworkObjectId)); frame++)
                yield return null;
            AssertSameTable(table, playable => replicas.TryGetValue(playable.NetworkObjectId, out var replica)
                ? replica.GetComponent<CgsNetPlayable>()
                : null, "for a player who joins");
        }

        private static Dictionary<CgsNetPlayable, string> Snapshot(PlayController controller) =>
            controller.PlayAreaCardZone.GetComponentsInChildren<CgsNetPlayable>()
                .Where(playable => playable != controller.PlayAreaCardZone)
                .ToDictionary(playable => playable, Describe);

        private static string Describe(CgsNetPlayable playable)
        {
            var pose = $"{playable.GetType().Name} at {playable.Position} turned {playable.Rotation.eulerAngles.z:F0}";
            return playable switch
            {
                CardStack stack => $"{pose} named {stack.Name}, faceup {stack.IsTopFaceup}: " +
                                   string.Join(",", stack.Cards.Select(unityCard => unityCard.Id)),
                CardModel cardModel => $"{pose} card {cardModel.Id}, facedown {cardModel.IsFacedown}, " +
                                       $"shared {cardModel.IsCardShared}",
                Die die => $"{pose} showing {die.Value} of {die.Max}",
                Counter counter => $"{pose} counting {counter.Value}",
                CardZone zone => $"{pose} zone {zone.Name} of type {zone.Type}",
                _ => pose
            };
        }

        private static void AssertSameTable(Dictionary<CgsNetPlayable, string> table,
            Func<CgsNetPlayable, CgsNetPlayable> find, string where)
        {
            foreach (var (playable, description) in table)
            {
                var found = find(playable);
                Assert.IsNotNull(found, $"{description} is missing {where}");
                Assert.AreEqual(description, Describe(found), $"The table changed {where}");
            }
        }

        // A second NetworkManager in this process stands in for another device on the local network
        private IEnumerator JoinAsAnotherPlayer()
        {
            var host = CgsNetManager.Instance;
            // The other player's own table code would act on this process's shared play screen, so it gets no player
            host.NetworkConfig.PlayerPrefab = null;
            var node = new GameObject("Joining player");
            var transport = node.AddComponent<UnityTransport>();
            _client = node.AddComponent<NetworkManager>();
            _client.enabled = false;
            _client.NetworkConfig = new NetworkConfig();
            _client.NetworkConfig.FromBase64(host.NetworkConfig.ToBase64());
            _client.NetworkConfig.NetworkTransport = transport;
            foreach (var networkPrefab in host.NetworkConfig.Prefabs.Prefabs)
                _client.AddNetworkPrefab(networkPrefab.Prefab);
            var hostTransport = (UnityTransport)host.NetworkConfig.NetworkTransport;
            transport.SetConnectionData("127.0.0.1", hostTransport.ConnectionData.Port, "127.0.0.1");
            Assert.IsTrue(_client.StartClient(), "The other player starts joining");
            for (var frame = 0; frame < TimeoutFrames && !_client.IsConnectedClient; frame++)
                yield return null;
            Assert.IsTrue(_client.IsConnectedClient, "The other player joins the solo table");
        }
    }
}
