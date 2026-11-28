/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class BrowserNetworkLanTests
    {
        private const string Room = "0123456789abcdef0123456789abcdef";
        private const string Network = "feedfacefeedfacefeedfacefeedface";

        private sealed class FakeSignals : IBrowserLanSignals
        {
            public string NetworkKey = Network;
            public bool AnswerFails;
            public int AnswerCount;
            public readonly List<string> Offers = new();
            public readonly List<string> Accepted = new();
            public readonly List<string> Discarded = new();
            private readonly string _name;

            public FakeSignals(string name) => _name = name;

            public readonly List<bool> KeyRequests = new();
            public bool DeferKeys;
            public readonly List<Action<string>> PendingKeys = new();

            public void RequestNetworkKey(bool fresh, Action<string> completed)
            {
                KeyRequests.Add(fresh);
                if (DeferKeys)
                    PendingKeys.Add(completed);
                else
                    completed(NetworkKey);
            }

            public void CreateOffer(string room, Action<string> completed)
            {
                var offer = $"offer:{_name}:{room}";
                Offers.Add(offer);
                completed(offer);
            }

            public void AcceptAnswer(string room, string answer, Action<bool> completed)
            {
                Accepted.Add(answer);
                completed(answer.StartsWith("answer:", StringComparison.Ordinal));
            }

            public void AnswerOffer(string offer, Action<string> completed)
            {
                AnswerCount++;
                completed(AnswerFails ? null : "answer:" + offer);
            }

            public void DiscardOffer(string room) => Discarded.Add(room);
        }

        private sealed class FakeLobby
        {
            public string Id, Name, NetworkKey, RoomId, HostId;
            public int Heartbeats;
            public readonly Dictionary<string, string> Offers = new();
            public readonly Dictionary<string, string> Answers = new();
        }

        // One in-memory Lobby service, seen by each player through its own view.
        private sealed class FakeLobbyService
        {
            public readonly Dictionary<string, FakeLobby> Lobbies = new();
            public int Updates;
            public int FailingUpdates;
            public TaskCompletionSource<bool> CreateGate;
            public bool CreateStarted;
            public int Deletes;
        }

        private sealed class FakeLobbies : IBrowserLanLobbies
        {
            private readonly FakeLobbyService _service;
            private readonly string _playerId;

            public FakeLobbies(FakeLobbyService service, string playerId)
            {
                _service = service;
                _playerId = playerId;
            }

            public async Task<string> CreateAsync(string name, string networkKey, string roomId)
            {
                _service.CreateStarted = true;
                if (_service.CreateGate != null)
                    await _service.CreateGate.Task;
                var lobby = new FakeLobby
                {
                    Id = "lobby" + _service.Lobbies.Count, Name = name, NetworkKey = networkKey, RoomId = roomId,
                    HostId = _playerId
                };
                lobby.Offers[_playerId] = null;
                _service.Lobbies[lobby.Id] = lobby;
                return lobby.Id;
            }

            public Task<IReadOnlyList<BrowserLanRoom>> QueryAsync(string networkKey) =>
                Task.FromResult<IReadOnlyList<BrowserLanRoom>>(_service.Lobbies.Values
                    .Where(lobby => lobby.NetworkKey == networkKey)
                    .Select(lobby => new BrowserLanRoom
                    {
                        LobbyId = lobby.Id, RoomId = lobby.RoomId, Name = BrowserNetworkLan.DisplayName(lobby.Name)
                    }).ToList());

            public Task<BrowserLanLobbySnapshot> GetAsync(string lobbyId)
            {
                if (!_service.Lobbies.TryGetValue(lobbyId, out var lobby))
                    return Task.FromException<BrowserLanLobbySnapshot>(new InvalidOperationException("No lobby"));
                var snapshot = new BrowserLanLobbySnapshot { HostId = lobby.HostId };
                foreach (var (playerId, offer) in lobby.Offers)
                {
                    snapshot.PlayerIds.Add(playerId);
                    if (offer != null)
                        snapshot.Offers[playerId] = offer;
                }

                foreach (var (playerId, answer) in lobby.Answers)
                    if (lobby.Offers.ContainsKey(playerId))
                        snapshot.Answers[playerId] = answer;
                    else
                        snapshot.StaleAnswerKeys.Add(playerId);
                return Task.FromResult(snapshot);
            }

            public Task UpdateAnswersAsync(string lobbyId, IReadOnlyDictionary<string, string> answers,
                IReadOnlyCollection<string> staleAnswerKeys)
            {
                _service.Updates++;
                if (_service.FailingUpdates > 0)
                {
                    _service.FailingUpdates--;
                    return Task.FromException(new InvalidOperationException("Rate limited"));
                }

                var lobby = _service.Lobbies[lobbyId];
                foreach (var (playerId, answer) in answers)
                    lobby.Answers[playerId] = answer;
                foreach (var key in staleAnswerKeys)
                    lobby.Answers.Remove(key);
                return Task.CompletedTask;
            }

            public Task HeartbeatAsync(string lobbyId)
            {
                _service.Lobbies[lobbyId].Heartbeats++;
                return Task.CompletedTask;
            }

            public Task UpdateNetworkAsync(string lobbyId, string networkKey)
            {
                _service.Lobbies[lobbyId].NetworkKey = networkKey;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string lobbyId)
            {
                _service.Deletes++;
                _service.Lobbies.Remove(lobbyId);
                return Task.CompletedTask;
            }

            public Task<string> JoinAsync(string lobbyId, string offer)
            {
                if (!_service.Lobbies.TryGetValue(lobbyId, out var lobby))
                    return Task.FromException<string>(new InvalidOperationException("No lobby"));
                lobby.Offers[_playerId] = offer;
                return Task.FromResult(_playerId);
            }

            public Task LeaveAsync(string lobbyId, string playerId)
            {
                if (_service.Lobbies.TryGetValue(lobbyId, out var lobby))
                    lobby.Offers.Remove(playerId);
                return Task.CompletedTask;
            }
        }

        private static BrowserNetworkLan Lan(IBrowserLanSignals signals, IBrowserLanLobbies lobbies) =>
            new(signals, lobbies) { PollSeconds = 0.05f, HeartbeatSeconds = 0.2f, JoinTimeoutSeconds = 0.5f };

        private static IEnumerator Run(IEnumerator routine, Func<bool> done, float seconds = 5)
        {
            var runner = new GameObject("BrowserNetworkLanTests").AddComponent<Runner>();
            runner.StartCoroutine(routine);
            var started = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - started < seconds)
                yield return null;
            UnityEngine.Object.Destroy(runner.gameObject);
            Assert.IsTrue(done(), "Timed out");
        }

        private sealed class Runner : MonoBehaviour
        {
        }

        [UnityTest]
        public IEnumerator HostedRoomIsFoundAndJoinedThroughLobbySignaling()
        {
            var service = new FakeLobbyService();
            var hostSignals = new FakeSignals("host");
            var clientSignals = new FakeSignals("client");
            var host = Lan(hostSignals, new FakeLobbies(service, "host-player"));
            var client = Lan(clientSignals, new FakeLobbies(service, "client-player"));
            var hosting = true;
            var runner = new GameObject("Host").AddComponent<Runner>();
            runner.StartCoroutine(host.Advertise(Room, "Cards", () => hosting));
            yield return new WaitUntil(() => service.Lobbies.Count == 1);

            var lobby = service.Lobbies.Values.Single();
            Assert.AreEqual("Cards" + BrowserNetworkLan.LobbyNameSuffix, lobby.Name);
            Assert.AreEqual(Network, lobby.NetworkKey);
            Assert.AreEqual(Room, lobby.RoomId);

            IReadOnlyList<BrowserLanRoom> rooms = null;
            yield return Run(client.FindRooms(found => rooms = found), () => rooms != null);
            Assert.AreEqual(1, rooms.Count);
            Assert.AreEqual("Cards", rooms[0].Name);
            Assert.AreEqual(Room, rooms[0].RoomId);

            bool? joined = null;
            yield return Run(client.Join(rooms[0], result => joined = result), () => joined.HasValue);
            Assert.IsTrue(joined.Value);
            Assert.AreEqual(new[] { "answer:offer:client:" + Room }, clientSignals.Accepted);
            Assert.IsEmpty(clientSignals.Discarded);
            Assert.IsFalse(lobby.Offers.ContainsKey("client-player"), "Joined players leave the lobby.");

            // The host removes the departed player's answer, keeps the lobby alive, and deletes it when done.
            yield return new WaitUntil(() => lobby.Answers.Count == 0);
            yield return new WaitUntil(() => lobby.Heartbeats > 0);
            hosting = false;
            yield return new WaitUntil(() => service.Lobbies.Count == 0);
            UnityEngine.Object.Destroy(runner.gameObject);
        }

        [UnityTest]
        public IEnumerator WithoutANetworkAddressOnlySameBrowserLanRemains()
        {
            var service = new FakeLobbyService();
            var signals = new FakeSignals("host") { NetworkKey = null };
            var lan = Lan(signals, new FakeLobbies(service, "host-player"));
            var finished = false;
            yield return Run(Wrap(lan.Advertise(Room, "Cards", () => true), () => finished = true), () => finished);
            Assert.IsEmpty(service.Lobbies);

            IReadOnlyList<BrowserLanRoom> rooms = null;
            yield return Run(lan.FindRooms(found => rooms = found), () => rooms != null);
            Assert.IsEmpty(rooms);
        }

        [UnityTest]
        public IEnumerator JoinGivesUpWhenTheHostDoesNotAnswer()
        {
            var service = new FakeLobbyService();
            var hostSignals = new FakeSignals("host") { AnswerFails = true };
            var clientSignals = new FakeSignals("client");
            var host = Lan(hostSignals, new FakeLobbies(service, "host-player"));
            var client = Lan(clientSignals, new FakeLobbies(service, "client-player"));
            var hosting = true;
            var runner = new GameObject("Host").AddComponent<Runner>();
            runner.StartCoroutine(host.Advertise(Room, "Cards", () => hosting));
            yield return new WaitUntil(() => service.Lobbies.Count == 1);
            var lobby = service.Lobbies.Values.Single();

            bool? joined = null;
            yield return Run(client.Join(new BrowserLanRoom { LobbyId = lobby.Id, RoomId = Room },
                result => joined = result), () => joined.HasValue);
            Assert.IsFalse(joined.Value);
            Assert.IsEmpty(clientSignals.Accepted);
            Assert.AreEqual(new[] { Room }, clientSignals.Discarded);
            Assert.IsFalse(lobby.Offers.ContainsKey("client-player"));
            hosting = false;
            UnityEngine.Object.Destroy(runner.gameObject);
        }

        [UnityTest]
        public IEnumerator JoiningAMissingLobbyFailsAndDiscardsTheOffer()
        {
            var signals = new FakeSignals("client");
            var client = Lan(signals, new FakeLobbies(new FakeLobbyService(), "client-player"));
            bool? joined = null;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Browser LAN"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Browser LAN join failed"));
            yield return Run(client.Join(new BrowserLanRoom { LobbyId = "gone", RoomId = Room },
                result => joined = result), () => joined.HasValue);
            Assert.IsFalse(joined.Value);
            Assert.AreEqual(new[] { Room }, signals.Discarded);
        }

        [UnityTest]
        public IEnumerator AnswersThatFailToPublishAreRetried()
        {
            var service = new FakeLobbyService { FailingUpdates = 2 };
            var hostSignals = new FakeSignals("host");
            var host = Lan(hostSignals, new FakeLobbies(service, "host-player"));
            var client = Lan(new FakeSignals("client"), new FakeLobbies(service, "client-player"));
            var hosting = true;
            var runner = new GameObject("Host").AddComponent<Runner>();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Rate limited"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Rate limited"));
            runner.StartCoroutine(host.Advertise(Room, "Cards", () => hosting));
            yield return new WaitUntil(() => service.Lobbies.Count == 1);
            var lobby = service.Lobbies.Values.Single();

            bool? joined = null;
            yield return Run(client.Join(new BrowserLanRoom { LobbyId = lobby.Id, RoomId = Room },
                result => joined = result), () => joined.HasValue);
            Assert.IsTrue(joined.Value);
            Assert.AreEqual(1, hostSignals.AnswerCount, "The same answer, and its connection, is published again.");
            hosting = false;
            UnityEngine.Object.Destroy(runner.gameObject);
        }

        [UnityTest]
        public IEnumerator StoppingAdvertisingDeletesTheLobbyEvenWhenItsCoroutineStops()
        {
            var service = new FakeLobbyService();
            var lan = Lan(new FakeSignals("host"), new FakeLobbies(service, "host-player"));
            var runner = new GameObject("Host").AddComponent<Runner>();
            runner.StartCoroutine(lan.Advertise(Room, "Cards", () => true));
            yield return new WaitUntil(() => service.Lobbies.Count == 1);
            runner.StopAllCoroutines();
            lan.StopAdvertising();
            yield return new WaitUntil(() => service.Lobbies.Count == 0);

            // A lobby whose creation finishes after stopping is deleted once it exists.
            service.CreateGate = new TaskCompletionSource<bool>();
            service.CreateStarted = false;
            var deletes = service.Deletes;
            runner.StartCoroutine(lan.Advertise(Room, "Cards", () => true));
            yield return new WaitUntil(() => service.CreateStarted);
            runner.StopAllCoroutines();
            lan.StopAdvertising();
            Assert.IsEmpty(service.Lobbies);
            service.CreateGate.SetResult(true);
            var started = Time.realtimeSinceStartup;
            while (service.Deletes == deletes && Time.realtimeSinceStartup - started < 5)
                yield return null;
            Assert.AreEqual(deletes + 1, service.Deletes, "The late lobby is deleted.");
            Assert.IsEmpty(service.Lobbies);
            UnityEngine.Object.Destroy(runner.gameObject);
        }

        [UnityTest]
        public IEnumerator EachSearchUsesTheCurrentNetwork()
        {
            var service = new FakeLobbyService();
            service.Lobbies["lobby0"] = new FakeLobby
                { Id = "lobby0", Name = "Cards", NetworkKey = Network, RoomId = Room, HostId = "host-player" };
            var signals = new FakeSignals("client");
            var lan = Lan(signals, new FakeLobbies(service, "client-player"));
            IReadOnlyList<BrowserLanRoom> rooms = null;
            yield return Run(lan.FindRooms(found => rooms = found), () => rooms != null);
            Assert.AreEqual(1, rooms.Count);

            signals.NetworkKey = "another network";
            rooms = null;
            yield return Run(lan.FindRooms(found => rooms = found), () => rooms != null);
            Assert.IsEmpty(rooms, "A browser that moved networks no longer sees the old network's rooms.");
        }

        [UnityTest]
        public IEnumerator HostingUsesAFreshKeyAndFollowsANetworkChange()
        {
            var service = new FakeLobbyService();
            var signals = new FakeSignals("host");
            var lan = Lan(signals, new FakeLobbies(service, "host-player"));
            var hosting = true;
            var runner = new GameObject("Host").AddComponent<Runner>();
            runner.StartCoroutine(lan.Advertise(Room, "Cards", () => hosting));
            yield return new WaitUntil(() => service.Lobbies.Count == 1);
            Assert.IsTrue(signals.KeyRequests[0], "Hosting does not reuse an earlier key.");

            var lobby = service.Lobbies.Values.Single();
            signals.NetworkKey = "another network";
            var started = Time.realtimeSinceStartup;
            while (lobby.NetworkKey != "another network" && Time.realtimeSinceStartup - started < 5)
                yield return null;
            Assert.AreEqual("another network", lobby.NetworkKey, "The room moves to the host's new network.");
            hosting = false;
            yield return new WaitUntil(() => service.Lobbies.Count == 0);
            UnityEngine.Object.Destroy(runner.gameObject);
        }

        [UnityTest]
        public IEnumerator AnOlderKeyLookupFinishingLastDoesNotReplaceTheHostsFreshKey()
        {
            var service = new FakeLobbyService();
            var signals = new FakeSignals("host") { DeferKeys = true, NetworkKey = "network B" };
            var lan = Lan(signals, new FakeLobbies(service, "host-player"));
            var hosting = true;
            var runner = new GameObject("Host").AddComponent<Runner>();
            runner.StartCoroutine(lan.FindRooms(_ => { }));
            runner.StartCoroutine(lan.Advertise(Room, "Cards", () => hosting));
            yield return new WaitUntil(() => signals.PendingKeys.Count == 2);

            // The fresh hosting lookup finishes first, then the search's lookup from the old network.
            signals.DeferKeys = false;
            signals.PendingKeys[1]("network B");
            signals.PendingKeys[0]("network A");
            yield return new WaitUntil(() => service.Lobbies.Count == 1);
            Assert.AreEqual("network B", service.Lobbies.Values.Single().NetworkKey);
            hosting = false;
            UnityEngine.Object.Destroy(runner.gameObject);
        }

        [UnityTest]
        public IEnumerator APlayerWhoJoinsAgainGetsAnAnswerForTheNewOffer()
        {
            var service = new FakeLobbyService { FailingUpdates = 1 };
            var hostSignals = new FakeSignals("host");
            var lan = Lan(hostSignals, new FakeLobbies(service, "host-player"));
            var hosting = true;
            var runner = new GameObject("Host").AddComponent<Runner>();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Rate limited"));
            runner.StartCoroutine(lan.Advertise(Room, "Cards", () => hosting));
            yield return new WaitUntil(() => service.Lobbies.Count == 1);
            var lobby = service.Lobbies.Values.Single();

            // The first answer fails to publish; the player then joins again before the host sees them leave.
            lobby.Offers["player"] = "offer:first";
            yield return new WaitUntil(() => service.Updates >= 1);
            lobby.Offers["player"] = "offer:second";
            var started = Time.realtimeSinceStartup;
            while (!lobby.Answers.ContainsKey("player") && Time.realtimeSinceStartup - started < 5)
                yield return null;
            Assert.AreEqual("answer:offer:second", lobby.Answers["player"]);

            // An answered offer is not answered again, but a newer one is.
            var answerCount = hostSignals.AnswerCount;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(answerCount, hostSignals.AnswerCount);
            lobby.Offers["player"] = "offer:third";
            started = Time.realtimeSinceStartup;
            while (lobby.Answers["player"] != "answer:offer:third" && Time.realtimeSinceStartup - started < 5)
                yield return null;
            Assert.AreEqual("answer:offer:third", lobby.Answers["player"]);
            hosting = false;
            UnityEngine.Object.Destroy(runner.gameObject);
        }

        [Test]
        public void DisplayNameRemovesTheWebLanMarker()
        {
            Assert.AreEqual("Cards", BrowserNetworkLan.DisplayName("Cards" + BrowserNetworkLan.LobbyNameSuffix));
            Assert.AreEqual("Cards", BrowserNetworkLan.DisplayName("Cards"));
        }

        [Test]
        public void AnswerKeysUseAShortPlayerIdPrefix()
        {
            Assert.AreEqual("Answer" + new string('x', 18), UnityBrowserLanLobbies.AnswerKey(new string('x', 28)));
            Assert.AreEqual("Answerabc", UnityBrowserLanLobbies.AnswerKey("abc"));
        }

        [Test]
        public void SignalsCompleteWithNullOutsideTheBrowser()
        {
            var transport = new GameObject("Transport").AddComponent<BrowserLanTransport>();
            try
            {
                string key = "unset", offer = "unset", answer = "unset";
                bool? accepted = null;
                transport.RequestNetworkKey(true, value => key = value);
                transport.CreateOffer(Room, value => offer = value);
                transport.AnswerOffer("offer", value => answer = value);
                transport.AcceptAnswer(Room, "answer", value => accepted = value);
                Assert.IsNull(key);
                Assert.IsNull(offer);
                Assert.IsNull(answer);
                Assert.AreEqual(false, accepted);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(transport.gameObject);
            }
        }

        private static IEnumerator Wrap(IEnumerator routine, Action finished)
        {
            yield return routine;
            finished();
        }
    }
}
