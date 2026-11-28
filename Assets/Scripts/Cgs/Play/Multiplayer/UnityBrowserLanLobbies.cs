/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace Cgs.Play.Multiplayer
{
    /// <summary>Carries browser LAN signaling in Unity Lobby; see <see cref="BrowserNetworkLan"/>.</summary>
    public sealed class UnityBrowserLanLobbies : IBrowserLanLobbies
    {
        /// <summary>Hash of the network's public address; indexed so that browsers can query their network.</summary>
        public const string KeyNetwork = "WebLanNetwork";

        /// <summary>The BrowserLanTransport room; it also marks the lobby as browser LAN rather than Internet.</summary>
        public const string KeyRoom = "WebLanRoom";

        public const string KeyOffer = "WebLanOffer";
        private const string AnswerPrefix = "Answer";
        private const int AnswerPlayerIdLength = 18;
        private const int MaxPlayers = 10;
        private const int MaxLobbyNameLength = 100;

        public static bool IsBrowserLanLobby(Lobby lobby) => lobby?.Data != null && lobby.Data.ContainsKey(KeyRoom);

        // Lobby data keys are short, so answers use a prefix of the player's ID.
        public static string AnswerKey(string playerId) =>
            AnswerPrefix + (playerId.Length > AnswerPlayerIdLength ? playerId[..AnswerPlayerIdLength] : playerId);

        private static async Task EnsureSignedInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await CgsNetManager.SignInAnonymouslyAsync();
        }

        public async Task<string> CreateAsync(string name, string networkKey, string roomId)
        {
            await EnsureSignedInAsync();
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    [KeyNetwork] = new(DataObject.VisibilityOptions.Public, networkKey, DataObject.IndexOptions.S1),
                    [KeyRoom] = new(DataObject.VisibilityOptions.Public, roomId)
                }
            };
            if (name.Length > MaxLobbyNameLength)
                name = name[..(MaxLobbyNameLength - BrowserNetworkLan.LobbyNameSuffix.Length)] +
                       BrowserNetworkLan.LobbyNameSuffix;
            var lobby = await LobbyService.Instance.CreateLobbyAsync(name, MaxPlayers, options);
            return lobby.Id;
        }

        public async Task<IReadOnlyList<BrowserLanRoom>> QueryAsync(string networkKey)
        {
            await EnsureSignedInAsync();
            var response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new(field: QueryFilter.FieldOptions.S1, op: QueryFilter.OpOptions.EQ, value: networkKey),
                    new(field: QueryFilter.FieldOptions.AvailableSlots, op: QueryFilter.OpOptions.GT, value: "0")
                }
            });
            return response.Results.Where(IsBrowserLanLobby).Select(lobby => new BrowserLanRoom
            {
                LobbyId = lobby.Id, RoomId = lobby.Data[KeyRoom].Value,
                Name = BrowserNetworkLan.DisplayName(lobby.Name)
            }).ToList();
        }

        public async Task<BrowserLanLobbySnapshot> GetAsync(string lobbyId)
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
            var snapshot = new BrowserLanLobbySnapshot { HostId = lobby.HostId };
            var presentAnswerKeys = new HashSet<string>();
            foreach (var player in lobby.Players)
            {
                snapshot.PlayerIds.Add(player.Id);
                var answerKey = AnswerKey(player.Id);
                presentAnswerKeys.Add(answerKey);
                if (player.Data != null && player.Data.TryGetValue(KeyOffer, out var offer))
                    snapshot.Offers[player.Id] = offer.Value;
                if (lobby.Data != null && lobby.Data.TryGetValue(answerKey, out var answer) &&
                    !string.IsNullOrEmpty(answer?.Value))
                    snapshot.Answers[player.Id] = answer.Value;
            }

            if (lobby.Data != null)
                snapshot.StaleAnswerKeys.AddRange(lobby.Data.Keys.Where(key =>
                    key.StartsWith(AnswerPrefix, StringComparison.Ordinal) && !presentAnswerKeys.Contains(key)));
            return snapshot;
        }

        public Task UpdateAnswersAsync(string lobbyId, IReadOnlyDictionary<string, string> answers,
            IReadOnlyCollection<string> staleAnswerKeys)
        {
            var data = new Dictionary<string, DataObject>();
            foreach (var (playerId, answer) in answers)
                data[AnswerKey(playerId)] = new DataObject(DataObject.VisibilityOptions.Member, answer);
            // A null value removes the key.
            foreach (var key in staleAnswerKeys)
                data[key] = null;
            return LobbyService.Instance.UpdateLobbyAsync(lobbyId, new UpdateLobbyOptions { Data = data });
        }

        public Task HeartbeatAsync(string lobbyId) => LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);

        public Task UpdateNetworkAsync(string lobbyId, string networkKey) =>
            LobbyService.Instance.UpdateLobbyAsync(lobbyId, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    [KeyNetwork] = new(DataObject.VisibilityOptions.Public, networkKey, DataObject.IndexOptions.S1)
                }
            });

        public Task DeleteAsync(string lobbyId) => LobbyService.Instance.DeleteLobbyAsync(lobbyId);

        public async Task<string> JoinAsync(string lobbyId, string offer)
        {
            await EnsureSignedInAsync();
            var playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, new JoinLobbyByIdOptions
            {
                Player = new Player(id: playerId, data: new Dictionary<string, PlayerDataObject>
                {
                    [KeyOffer] = new(PlayerDataObject.VisibilityOptions.Member, offer)
                })
            });
            return playerId;
        }

        public Task LeaveAsync(string lobbyId, string playerId) =>
            LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
    }
}
