/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Linq;
using Cgs.GameContent;
using Cgs.Localization;
using Cgs.Play;
using Cgs.CardGameView.Multiplayer;
using UnityEngine.UI;
using System.Text;
using Cgs;
using Cgs.Play.Multiplayer;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Localization.Settings;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class GameContentNetworkTests
    {
        private readonly List<NetworkManager> _managers = new();
        private NetworkManager _previousManager;
        private UnityCardGame _previousGame;
        private UnityCardGame _game;
        private readonly List<GameObject> _objects = new();
        private readonly List<GameObject> _prefabs = new();
        private PlayController _previousController;
        private ushort _port;
        private int _frameRate;
        private int _vSync;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _frameRate = Application.targetFrameRate;
            _vSync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            _previousManager = NetworkManager.Singleton;
            _previousGame = CardGameManager.Current;
            _previousController = PlayController.Instance;
            _game = new UnityCardGame(null, "network-labels-" + Guid.NewGuid().ToString("N"));
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _game);
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            _port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
        }

        private void Localize() => _game.Localization = LocalizationJson.ReadManifest(
            @"{""defaultLocale"":""en"",""locales"":{""es"":{""inline"":{""game"":{""name"":""Juego""}}}}}").Value;

        private NetworkManager Manager(bool capable, bool host = false)
        {
            var node = new GameObject(host ? "Label host" : capable ? "Capable client" : "Legacy client");
            var transport = node.AddComponent<UnityTransport>();
            var manager = capable ? node.AddComponent<CgsNetManager>() : node.AddComponent<NetworkManager>();
            // Avoid the application's Unity Services startup; exercise local NGO transport.
            manager.enabled = false;
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, EnableSceneManagement = false, ForceSamePrefabs = false,
                RecycleNetworkIds = true, NetworkIdRecycleDelay = 0
            };
            foreach (var prefab in _prefabs)
                manager.AddNetworkPrefab(prefab);
            transport.SetConnectionData("127.0.0.1", _port, "127.0.0.1");
            _managers.Add(manager);
            if (capable)
                node.AddComponent<GameContentNetwork>();
            if (host)
            {
                manager.SetSingleton();
                Assert.That(manager.StartHost(), Is.True);
            }
            else
                Assert.That(manager.StartClient(), Is.True);
            return manager;
        }

        private static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static IDictionary Peers(NetworkManager host) => (IDictionary)Field(host.GetComponent<GameContentNetwork>(), "_peers");
        private static bool Ready(object peer) => (bool)peer.GetType().GetField("Ready").GetValue(peer);
        private static string Connection(object peer) => (string)peer.GetType().GetField("Connection").GetValue(peer);

        private IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 25;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(condition(), Is.True, "Local network did not settle within 25 seconds: " +
                string.Join("; ", _managers.ConvertAll(manager => manager.name + " connected=" + manager.IsConnectedClient +
                    " listening=" + manager.IsListening + " ready=" + (manager.GetComponent<GameContentNetwork>() == null
                        ? "legacy" : Field(manager.GetComponent<GameContentNetwork>(), "_ready").ToString()))));
        }

        private JObject Ack(NetworkManager host, NetworkManager client, int version = 1) => new()
        {
            ["Version"] = version, ["Type"] = "ack", ["Game"] = _game.Id,
            ["Session"] = (string)Field(host.GetComponent<GameContentNetwork>(), "_session"),
            ["Connection"] = Connection(Peers(host)[client.LocalClientId])
        };

        private static void Send(NetworkManager client, JObject packet, ulong recipient = NetworkManager.ServerClientId)
        {
            var bytes = Encoding.UTF8.GetBytes(packet.ToString());
            using var writer = new FastBufferWriter(bytes.Length + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            client.CustomMessagingManager.SendNamedMessage("game-content-labels-v1", recipient,
                writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        [UnityTest]
        public IEnumerator CapableClientNegotiatesAndLateAcknowledgementUpgradesCanonicalFallback()
        {
            Localize();
            var host = Manager(true, true);
            var capable = Manager(true);
            var legacy = Manager(false);
            yield return Until(() => capable.IsConnectedClient && legacy.IsConnectedClient && Peers(host).Count == 2);
            yield return Until(() => (bool)Field(capable.GetComponent<GameContentNetwork>(), "_ready"));
            Assert.That(Ready(Peers(host)[capable.LocalClientId]), Is.True);
            Assert.That(Ready(Peers(host)[legacy.LocalClientId]), Is.False);
            Send(legacy, Ack(host, legacy, 999));
            var stale = Ack(host, legacy);
            stale["Connection"] = Guid.NewGuid().ToString("N");
            Send(legacy, stale);
            yield return null;
            Assert.That(Ready(Peers(host)[legacy.LocalClientId]), Is.False);
            yield return new WaitForSecondsRealtime(6);
            Send(legacy, Ack(host, legacy));
            yield return Until(() => Ready(Peers(host)[legacy.LocalClientId]));
            Assert.That(legacy.IsConnectedClient, Is.True, "An unknown named message must not break legacy connectivity.");
        }

        [UnityTest]
        public IEnumerator OfferSurvivesClientGameSwitchAndSameGameReload()
        {
            Localize();
            var host = Manager(true, true);
            var client = Manager(true);
            var extension = client.GetComponent<GameContentNetwork>();
            yield return Until(() => (bool)Field(extension, "_ready"));
            var scope = Connection(Peers(host)[client.LocalClientId]);
            var offeredGame = _game;
            var otherGame = new UnityCardGame(null, "Other game");
            // Each real process has its own Current. Change only the client's state
            // synchronously here, before the host can observe the temporary selection.
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, otherGame);
            extension.GetType().GetMethod("ChangeGame", Private).Invoke(extension, null);
            Assert.That(Field(extension, "_offer"), Is.Not.Null);
            Assert.That(Field(extension, "_ready"), Is.False);
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, offeredGame);
            yield return Until(() => (bool)Field(extension, "_ready"));
            Assert.That(Connection(Peers(host)[client.LocalClientId]), Is.EqualTo(scope));
            // A local definition reload must re-probe in the same authenticated scope.
            extension.GetType().GetMethod("ChangeGame", Private).Invoke(extension, null);
            Assert.That(Field(extension, "_ready"), Is.False);
            yield return Until(() => (bool)Field(extension, "_ready"));
            Assert.That(Connection(Peers(host)[client.LocalClientId]), Is.EqualTo(scope));
        }

        [UnityTest]
        public IEnumerator UnlocalizedPeersEmitNoOfferAndReconnectUsesNewScope()
        {
            var host = Manager(true, true);
            var client = Manager(true);
            yield return Until(() => client.IsConnectedClient);
            yield return null;
            Assert.That(Peers(host).Count, Is.Zero);
            Assert.That(Field(client.GetComponent<GameContentNetwork>(), "_offer"), Is.Null);
            client.Shutdown();
            yield return Until(() => !client.IsListening && host.ConnectedClientsIds.Count == 1);
            Localize();
            // Explicit manifest change is represented by a new game instance, as on selection.
            var localized = new UnityCardGame(null, _game.Id) { Localization = _game.Localization };
            _game = localized;
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _game);
            yield return null;
            var first = Manager(true);
            yield return Until(() => (bool)Field(first.GetComponent<GameContentNetwork>(), "_ready"));
            var oldId = first.LocalClientId;
            var oldConnection = Connection(Peers(host)[oldId]);
            first.Shutdown();
            yield return Until(() => !first.IsListening && !Peers(host).Contains(oldId));
            var reconnected = Manager(true);
            yield return Until(() => (bool)Field(reconnected.GetComponent<GameContentNetwork>(), "_ready"));
            Assert.That(Connection(Peers(host)[reconnected.LocalClientId]), Is.Not.EqualTo(oldConnection));
        }

        [UnityTest]
        public IEnumerator ForgedCreationIsRejectedAndRetryRetainsOneOutcome()
        {
            Localize();
            _game.GamePlayDeckName = "Deck";
            var host = Manager(true, true);
            var client = Manager(true);
            yield return Until(() => (bool)Field(client.GetComponent<GameContentNetwork>(), "_ready"));
            var packet = Ack(host, client);
            packet["Type"] = "create";
            packet["Request"] = Guid.NewGuid().ToString("N");
            packet["Player"] = ulong.MaxValue;
            packet["Name"] = "Deck";
            packet["Cards"] = new JArray();
            packet["Label"] = new JObject { ["Kind"] = "gamePlayDeckName", ["Key"] = "Deck" };
            Send(client, packet);
            var peer = Peers(host)[client.LocalClientId];
            var results = (IDictionary)peer.GetType().GetField("Results").GetValue(peer);
            yield return Until(() => results.Count == 1);
            var outcome = results[(string)packet["Request"]];
            Send(client, packet);
            packet["Name"] = "Different payload with the same request ID";
            Send(client, packet);
            yield return null;
            yield return null;
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[(string)packet["Request"]], Is.SameAs(outcome));
            Assert.That(host.SpawnManager.SpawnedObjects.Count, Is.Zero);
        }

        private GameObject Prefab(string name, uint hash, Type component)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(NetworkObject));
            var network = node.GetComponent<NetworkObject>();
            network.AutoObjectParentSync = false;
            typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SetValue(network, hash);
            ((Behaviour)node.AddComponent(component)).enabled = false;
            _objects.Add(node);
            _prefabs.Add(node);
            return node;
        }

        private static void SetField(object value, string field, object content) =>
            value.GetType().GetField(field, Private).SetValue(value, content);

        private static Text Label(Transform parent, string name)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent);
            return label;
        }

        private IEnumerator VerifyLocaleChangesDoNotSendProvenance(NetworkManager host, NetworkManager client,
            Func<CardStack[]> stacks)
        {
            var hadPreference = PlayerPrefs.HasKey(CgsLocaleSelector.PreferenceKey);
            var preference = PlayerPrefs.GetString(CgsLocaleSelector.PreferenceKey);
            CgsLocalization.Initialize();
            yield return LocalizationSettings.InitializationOperation;
            var originalLocale = CgsLocalization.CurrentLocale;
            var receivers = new Dictionary<NetworkManager, CustomMessagingManager.HandleNamedMessageDelegate>();
            var received = 0;
            foreach (var manager in new[] { host, client })
            {
                var extension = manager.GetComponent<GameContentNetwork>();
                var receiver = (CustomMessagingManager.HandleNamedMessageDelegate)Delegate.CreateDelegate(
                    typeof(CustomMessagingManager.HandleNamedMessageDelegate), extension,
                    typeof(GameContentNetwork).GetMethod("Receive", Private));
                receivers.Add(manager, receiver);
                manager.CustomMessagingManager.RegisterNamedMessageHandler("game-content-labels-v1", (sender, reader) =>
                {
                    received++;
                    receiver(sender, reader);
                });
            }
            try
            {
                // Drain creation/snapshot traffic before observing locale-only changes.
                yield return new WaitForSecondsRealtime(1);
                var before = received;
                var state = stacks().Select(stack => new
                {
                    stack.NetworkObjectId, stack.Name, stack.OwnerClientId,
                    Cards = string.Join(",", stack.Cards.Select(card => card.Id)),
                    Position = stack.transform.position,
                    Label = Newtonsoft.Json.JsonConvert.SerializeObject(stack.ContentLabel)
                }).ToArray();
                foreach (var locale in new[] { "es", "ja", "en" })
                {
                    CgsLocalization.SelectLanguage(locale);
                    yield return Until(() => CgsLocalization.CurrentLocale == locale);
                    yield return new WaitForSecondsRealtime(1);
                    Assert.That(received, Is.EqualTo(before), "Locale-only changes must not send provenance packets.");
                    Assert.That(stacks().Select(stack => new
                    {
                        stack.NetworkObjectId, stack.Name, stack.OwnerClientId,
                        Cards = string.Join(",", stack.Cards.Select(card => card.Id)),
                        Position = stack.transform.position,
                        Label = Newtonsoft.Json.JsonConvert.SerializeObject(stack.ContentLabel)
                    }).ToArray(), Is.EqualTo(state), "Locale-only changes must preserve canonical stack state.");
                }
            }
            finally
            {
                foreach (var pair in receivers)
                    pair.Key.CustomMessagingManager.RegisterNamedMessageHandler("game-content-labels-v1", pair.Value);
                CgsLocalization.SelectLanguage(originalLocale);
                if (hadPreference)
                    PlayerPrefs.SetString(CgsLocaleSelector.PreferenceKey, preference);
                else
                    PlayerPrefs.DeleteKey(CgsLocaleSelector.PreferenceKey);
                PlayerPrefs.Save();
            }
            yield return Until(() => CgsLocalization.CurrentLocale == originalLocale);
        }

        [UnityTest]
        public IEnumerator SuccessfulCreationPreservesCanonicalStateAndSnapshotsFollowRenameAndLateJoin()
        {
            _game.GamePlayDeckName = "Deck";
            _game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""es"",""locales"":{""es"":{""inline"":{""game"":{""gamePlayDeckName"":""Mazo""}}}}}").Value;
            var card = new UnityCard(_game, "first", "First", Set.DefaultCode,
                new Dictionary<string, PropertyDefValuePair>(), false);
            _game.Add(card, false);
            var controllerNode = new GameObject("Protocol play area controller");
            controllerNode.SetActive(false);
            _objects.Add(controllerNode);
            var controller = controllerNode.AddComponent<PlayController>();
            typeof(PlayController).GetProperty(nameof(PlayController.Instance)).SetValue(null, controller);
            var areaNode = new GameObject("Protocol play area", typeof(RectTransform));
            _objects.Add(areaNode);
            var area = areaNode.AddComponent<CardZone>();
            area.enabled = false;
            SetField(controller, "playAreaCardZone", area);
            var stackPrefab = Prefab("Protocol stack", 721001, typeof(CardStack));
            var stackTemplate = stackPrefab.GetComponent<CardStack>();
            SetField(stackTemplate, "topCard", stackPrefab.AddComponent<Image>());
            SetField(stackTemplate, "deckLabel", Label(stackPrefab.transform, "Name"));
            SetField(stackTemplate, "countLabel", Label(stackPrefab.transform, "Count"));
            SetField(controller, "cardStackPrefab", stackPrefab);
            var playerPrefab = Prefab("Protocol player", 721002, typeof(GameContentNetworkTestPlayer));
            var host = Manager(true, true);
            var client = Manager(true);
            yield return Until(() => (bool)Field(client.GetComponent<GameContentNetwork>(), "_ready"));
            var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkObject>();
            playerObject.SpawnAsPlayerObject(client.LocalClientId);
            var playerId = playerObject.NetworkObjectId;
            yield return Until(() => client.SpawnManager.SpawnedObjects.ContainsKey(playerId));
            var player = client.SpawnManager.SpawnedObjects[playerId].GetComponent<CgsNetPlayer>();
            var service = client.GetComponent<GameContentNetwork>();
            var reference = new GameContentLabel { Kind = "gamePlayDeckName", Key = "Deck" };
            Assert.That(service.TryCreate(player, "Deck", new CgsNetString[] { "first" }, true,
                Vector2.zero, Quaternion.identity, new CardStackCreationOptions { AutoStackCards = false, Label = reference }), Is.True);
            var pending = (IDictionary)Field(service, "_pending");
            var retry = JObject.FromObject(pending.Values.Cast<object>().Single());
            Send(client, retry);
            Assert.That(service.TryCreate(player, "Deck", new CgsNetString[] { "first" }, false,
                Vector2.right * 800, Quaternion.identity, new CardStackCreationOptions { AutoStackCards = false, Label = reference }), Is.True);
            CardStack[] HostStacks() => host.SpawnManager.SpawnedObjects.Values.Select(obj => obj.GetComponent<CardStack>())
                .Where(stack => stack != null).ToArray();
            yield return Until(() => HostStacks().Length == 2 && pending.Count == 0);
            Assert.That(HostStacks().All(stack => stack.Name == "Deck" && stack.Cards.Single().Id == "first"), Is.True);
            Assert.That(playerObject.GetComponent<CgsNetPlayer>().CurrentDeck, Is.Not.Null);
            var stackId = playerObject.GetComponent<CgsNetPlayer>().CurrentDeck.NetworkObjectId;
            CardStack Replica(NetworkManager manager) => manager.SpawnManager.SpawnedObjects.TryGetValue(stackId, out var obj)
                ? obj.GetComponent<CardStack>() : null;
            yield return Until(() => Replica(client)?.ContentLabel != null);
            Assert.That(Replica(client).DisplayName, Is.EqualTo("Mazo"));
            yield return VerifyLocaleChangesDoNotSendProvenance(host, client, HostStacks);
            var observation = ((IDictionary)Field(service, "_observations"))[Replica(client)];
            var stale = Ack(host, client);
            stale["Type"] = "state";
            stale["Object"] = stackId;
            stale["Request"] = (string)observation.GetType().GetField("Probe").GetValue(observation);
            stale["Incarnation"] = (string)observation.GetType().GetField("Incarnation").GetValue(observation);
            stale["Revision"] = (long)observation.GetType().GetField("Revision").GetValue(observation);
            stale["Owner"] = client.LocalClientId;
            stale["Name"] = "Deck";
            stale["Label"] = JObject.FromObject(reference);
            var late = Manager(true);
            yield return Until(() => Replica(late)?.ContentLabel != null);
            Assert.That(Replica(late).Name, Is.EqualTo("Deck"));
            host.SpawnManager.SpawnedObjects[stackId].ChangeOwnership(late.LocalClientId);
            yield return Until(() => Replica(client)?.OwnerClientId == late.LocalClientId &&
                Replica(client).ContentLabel != null && Replica(late)?.ContentLabel != null);
            HostStacks().Single(stack => stack.NetworkObjectId == stackId).Name = "My custom stack";
            yield return Until(() => Replica(client)?.Name == "My custom stack" && Replica(late)?.Name == "My custom stack" &&
                Replica(client).ContentLabel == null && Replica(late).ContentLabel == null);
            Assert.That(Replica(client).DisplayName, Is.EqualTo("My custom stack"));
            Send(host, stale, client.LocalClientId);
            yield return null;
            yield return null;
            Assert.That(Replica(client).ContentLabel, Is.Null, "Pre-rename/ownership metadata cannot restore a binding.");
            Send(client, retry);
            yield return null;
            yield return null;
            Assert.That(HostStacks().Length, Is.EqualTo(2), "Retrying after rename must not create another stack.");
            host.SpawnManager.SpawnedObjects[stackId].Despawn();
            yield return Until(() => !client.SpawnManager.SpawnedObjects.ContainsKey(stackId) &&
                !late.SpawnManager.SpawnedObjects.ContainsKey(stackId));
            Send(client, retry);
            yield return null;
            yield return null;
            Assert.That(HostStacks().Length, Is.EqualTo(1), "Retrying after despawn must not recreate the stack.");
            host.SetSingleton();
            var reused = controller.CreateCardStack("Deck", new[] { card }, Vector2.zero,
                Quaternion.identity, false, ownerClientId: client.LocalClientId, autoStackCards: false);
            Assert.That(reused.NetworkObjectId, Is.EqualTo(stackId), "This test must exercise actual network ID reuse.");
            yield return Until(() => Replica(client) != null && Replica(client).Name == "Deck");
            Send(host, stale, client.LocalClientId);
            yield return null;
            yield return null;
            Assert.That(Replica(client).ContentLabel, Is.Null, "Old incarnation/probe cannot bind a reused ID.");
            var spoof = (JObject)retry.DeepClone();
            spoof["Request"] = Guid.NewGuid().ToString("N");
            spoof["Connection"] = Connection(Peers(host)[late.LocalClientId]);
            Send(late, spoof);
            var latePeer = Peers(host)[late.LocalClientId];
            var rejected = (IDictionary)latePeer.GetType().GetField("Results").GetValue(latePeer);
            yield return Until(() => rejected.Count == 1);
            Assert.That(HostStacks().Length, Is.EqualTo(2), "Another client's player object cannot authorize creation.");
            var unknown = (JObject)retry.DeepClone();
            unknown["Request"] = Guid.NewGuid().ToString("N");
            unknown["Label"]["Kind"] = "unknown future label kind";
            Send(client, unknown);
            yield return Until(() => HostStacks().Length == 3);
            Assert.That(HostStacks().Count(stack => stack.Name == "Deck" && stack.ContentLabel == null), Is.EqualTo(2),
                "An unknown optional reference must not block otherwise valid canonical gameplay.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var manager in _managers)
                if (manager != null && manager.IsListening)
                    manager.Shutdown();
            yield return null;
            yield return null;
            for (var index = _managers.Count - 1; index >= 0; index--)
                if (_managers[index] != null)
                    Object.DestroyImmediate(_managers[index].gameObject);
            _managers.Clear();
            for (var index = _objects.Count - 1; index >= 0; index--)
                if (_objects[index] != null)
                    Object.DestroyImmediate(_objects[index]);
            _objects.Clear();
            _prefabs.Clear();
            typeof(PlayController).GetProperty(nameof(PlayController.Instance)).SetValue(null, _previousController);
            Application.targetFrameRate = _frameRate;
            QualitySettings.vSyncCount = _vSync;
            if (_previousManager != null)
                _previousManager.SetSingleton();
            typeof(CardGameManager).GetProperty(nameof(CardGameManager.Current)).SetValue(null, _previousGame);
        }
    }
}
