/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    /// <summary>Browser WebRTC operations used for LAN signaling; see BrowserLan.jslib.</summary>
    public interface IBrowserLanSignals
    {
        /// <summary>A hash that browsers on the same network share, or null if it can't be found.</summary>
        void RequestNetworkKey(Action<string> completed);

        void CreateOffer(string room, Action<string> completed);

        void AcceptAnswer(string room, string answer, Action<bool> completed);

        void AnswerOffer(string offer, Action<string> completed);

        void DiscardOffer(string room);
    }

    public sealed class BrowserLanRoom
    {
        public string LobbyId { get; set; }
        public string RoomId { get; set; }
        public string Name { get; set; }
    }

    public sealed class BrowserLanLobbySnapshot
    {
        public string HostId { get; set; }

        /// <summary>Connection offers by player ID, for players still in the lobby.</summary>
        public Dictionary<string, string> Offers { get; } = new();

        public HashSet<string> PlayerIds { get; } = new();

        /// <summary>Host answers by player ID, for players still in the lobby.</summary>
        public Dictionary<string, string> Answers { get; } = new();

        /// <summary>Lobby data keys of answers for players who have left.</summary>
        public List<string> StaleAnswerKeys { get; } = new();
    }

    /// <summary>The Lobby operations that carry browser LAN signaling; Unity Lobby in players.</summary>
    public interface IBrowserLanLobbies
    {
        Task<string> CreateAsync(string name, string networkKey, string roomId);

        Task<IReadOnlyList<BrowserLanRoom>> QueryAsync(string networkKey);

        Task<BrowserLanLobbySnapshot> GetAsync(string lobbyId);

        Task UpdateAnswersAsync(string lobbyId, IReadOnlyDictionary<string, string> answers,
            IReadOnlyCollection<string> staleAnswerKeys);

        Task HeartbeatAsync(string lobbyId);

        Task DeleteAsync(string lobbyId);

        /// <summary>Joins with a connection offer and returns the joining player's ID.</summary>
        Task<string> JoinAsync(string lobbyId, string offer);

        Task LeaveAsync(string lobbyId, string playerId);
    }

    /// <summary>
    /// Lets Web players on the same network find and join each other's LAN games. A hosting browser lists its
    /// room in a Lobby tagged with a hash of the network's public address. A joining browser adds a WebRTC offer
    /// to its Lobby player data; the host answers in the lobby data. The browsers then exchange game data directly
    /// over the local network, and the Lobby only remains for discovery.
    /// </summary>
    public sealed class BrowserNetworkLan
    {
        public const string LobbyNameSuffix = " (Web LAN)";

        // Lobby limits request rates, so poll gently; tests shorten these.
        public float PollSeconds { get; set; } = 1.5f;
        public float HeartbeatSeconds { get; set; } = 15;
        public float JoinTimeoutSeconds { get; set; } = 20;

        private readonly IBrowserLanSignals _signals;
        private readonly IBrowserLanLobbies _lobbies;
        private string _networkKey;

        public BrowserNetworkLan(IBrowserLanSignals signals, IBrowserLanLobbies lobbies)
        {
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
            _lobbies = lobbies ?? throw new ArgumentNullException(nameof(lobbies));
        }

        public static string DisplayName(string lobbyName) =>
            lobbyName != null && lobbyName.EndsWith(LobbyNameSuffix, StringComparison.Ordinal)
                ? lobbyName[..^LobbyNameSuffix.Length]
                : lobbyName;

        private IEnumerator NetworkKey(Action<string> found)
        {
            if (string.IsNullOrEmpty(_networkKey))
            {
                var done = false;
                _signals.RequestNetworkKey(key =>
                {
                    _networkKey = key;
                    done = true;
                });
                while (!done)
                    yield return null;
            }

            found(_networkKey);
        }

        /// <summary>Finds rooms hosted by browsers on this network; reports an empty list if there are none.</summary>
        public IEnumerator FindRooms(Action<IReadOnlyList<BrowserLanRoom>> found)
        {
            string networkKey = null;
            yield return NetworkKey(key => networkKey = key);
            if (string.IsNullOrEmpty(networkKey))
            {
                found(Array.Empty<BrowserLanRoom>());
                yield break;
            }

            var query = _lobbies.QueryAsync(networkKey);
            yield return Wait(query);
            found(query.IsCompletedSuccessfully ? query.Result : Array.Empty<BrowserLanRoom>());
        }

        /// <summary>
        /// Lists a hosted room for this network and answers joining players while <paramref name="isHosting"/>
        /// stays true. Without a network address, same-browser LAN still works, so this ends quietly.
        /// </summary>
        public IEnumerator Advertise(string roomId, string gameName, Func<bool> isHosting)
        {
            string networkKey = null;
            yield return NetworkKey(key => networkKey = key);
            if (string.IsNullOrEmpty(networkKey) || !isHosting())
                yield break;

            var create = _lobbies.CreateAsync(gameName + LobbyNameSuffix, networkKey, roomId);
            yield return Wait(create);
            if (!create.IsCompletedSuccessfully)
            {
                Debug.LogWarning("[CgsNet] Browser LAN rooms can't be listed on this network: " +
                                 create.Exception?.GetBaseException().Message);
                yield break;
            }

            var lobbyId = create.Result;
            var answered = new HashSet<string>();
            var sinceHeartbeat = 0f;
            while (isHosting())
            {
                var get = _lobbies.GetAsync(lobbyId);
                yield return Wait(get);
                if (get.IsCompletedSuccessfully)
                    yield return Answer(lobbyId, get.Result, answered);

                yield return new WaitForSecondsRealtime(PollSeconds);
                sinceHeartbeat += PollSeconds;
                if (sinceHeartbeat < HeartbeatSeconds || !isHosting())
                    continue;
                sinceHeartbeat = 0;
                yield return Wait(_lobbies.HeartbeatAsync(lobbyId));
            }

            yield return Wait(_lobbies.DeleteAsync(lobbyId));
        }

        private IEnumerator Answer(string lobbyId, BrowserLanLobbySnapshot snapshot, HashSet<string> answered)
        {
            var answers = new Dictionary<string, string>();
            foreach (var (playerId, offer) in snapshot.Offers)
            {
                if (playerId == snapshot.HostId || answered.Contains(playerId) ||
                    snapshot.Answers.ContainsKey(playerId) || string.IsNullOrEmpty(offer))
                    continue;

                string answer = null;
                var done = false;
                _signals.AnswerOffer(offer, result =>
                {
                    answer = result;
                    done = true;
                });
                while (!done)
                    yield return null;

                answered.Add(playerId);
                if (!string.IsNullOrEmpty(answer))
                    answers[playerId] = answer;
            }

            // Players leave the lobby once connected; drop their answers to stay within Lobby's data limits.
            answered.RemoveWhere(playerId => !snapshot.PlayerIds.Contains(playerId));
            if (answers.Count > 0 || snapshot.StaleAnswerKeys.Count > 0)
                yield return Wait(_lobbies.UpdateAnswersAsync(lobbyId, answers, snapshot.StaleAnswerKeys));
        }

        /// <summary>
        /// Exchanges WebRTC descriptions with a host on this network, then leaves its lobby. Reports true when the
        /// browser can start a client for <see cref="BrowserLanRoom.RoomId"/> over the new connection.
        /// </summary>
        public IEnumerator Join(BrowserLanRoom room, Action<bool> joined)
        {
            if (room == null || string.IsNullOrEmpty(room.LobbyId) || string.IsNullOrEmpty(room.RoomId))
            {
                joined(false);
                yield break;
            }

            string offer = null;
            var offered = false;
            _signals.CreateOffer(room.RoomId, result =>
            {
                offer = result;
                offered = true;
            });
            while (!offered)
                yield return null;
            if (string.IsNullOrEmpty(offer))
            {
                joined(false);
                yield break;
            }

            var join = _lobbies.JoinAsync(room.LobbyId, offer);
            yield return Wait(join);
            if (!join.IsCompletedSuccessfully)
            {
                Debug.LogWarning("[CgsNet] Browser LAN join failed: " + join.Exception?.GetBaseException().Message);
                _signals.DiscardOffer(room.RoomId);
                joined(false);
                yield break;
            }

            var playerId = join.Result;
            string answer = null;
            var waited = 0f;
            while (answer == null && waited < JoinTimeoutSeconds)
            {
                yield return new WaitForSecondsRealtime(PollSeconds);
                waited += PollSeconds;
                var get = _lobbies.GetAsync(room.LobbyId);
                yield return Wait(get);
                if (get.IsCompletedSuccessfully)
                    get.Result.Answers.TryGetValue(playerId, out answer);
            }

            var accepted = false;
            if (answer != null)
            {
                var done = false;
                _signals.AcceptAnswer(room.RoomId, answer, result =>
                {
                    accepted = result;
                    done = true;
                });
                while (!done)
                    yield return null;
            }

            // The browsers now connect directly; the lobby is no longer needed for this player.
            yield return Wait(_lobbies.LeaveAsync(room.LobbyId, playerId));
            if (!accepted)
                _signals.DiscardOffer(room.RoomId);
            joined(accepted);
        }

        private static IEnumerator Wait(Task task)
        {
            while (!task.IsCompleted)
                yield return null;
            if (task.IsFaulted)
                Debug.LogWarning("[CgsNet] Browser LAN lobby request failed: " +
                                 task.Exception?.GetBaseException().Message);
        }
    }
}
