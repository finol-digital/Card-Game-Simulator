/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cgs.CardGameView.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetManager : NetworkManager
    {
        public const int DefaultPort = 7777;
        public const int MaxPlayers = 10;

        public static CgsNetManager Instance => (CgsNetManager) Singleton;

        public bool IsOnline => IsHost || IsConnectedClient;

        public CgsNetPlayer LocalPlayer { get; set; }

        public UnityTransport Transport => (UnityTransport) NetworkConfig.NetworkTransport;

        public string RoomIdIp => "127.0.0.1".Equals(Transport.ConnectionData.Address,
            StringComparison.Ordinal)
            ? RoomId
            : Transport.ConnectionData.Address;

        private static string RoomId => PlayController.Instance != null && PlayController.Instance.Lobby != null &&
                                        !string.IsNullOrEmpty(PlayController.Instance.Lobby.RelayCode)
            ? PlayController.Instance.Lobby.RelayCode
            : Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(f => f.AddressFamily == AddressFamily.InterNetwork)
                .ToString();

        private Lobby CurrentLobby
        {
            get => _currentLobby;
            set
            {
                if (_currentLobby != null)
                {
                    var playerId = AuthenticationService.Instance.PlayerId;
                    if (_currentLobby.HostId == playerId)
                        LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                    else
                        LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, playerId);
                }
                _currentLobby = value;
            }
        }

        private Lobby _currentLobby;

        private void Start()
        {
#pragma warning disable CS4014
            SignInAnonymouslyAsync();
#pragma warning restore CS4014
        }

        private static async Task SignInAnonymouslyAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[CgsNet] Sign in anonymously succeeded!");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static async Task<RelayServerData> AllocateRelayServerAndGetCode(int maxConnections, string region = null)
        {
            Allocation allocation;
            try
            {
                allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
            }
            catch (Exception e)
            {
                Debug.LogError($"Relay allocation request failed {e.Message}");
                throw;
            }

            Debug.Log($"[CgsNet] server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"[CgsNet] server: {allocation.AllocationId}");

            try
            {
                PlayController.Instance.Lobby.RelayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            }
            catch
            {
                Debug.LogError("Relay create join code request failed");
                throw;
            }

            return new RelayServerData(allocation, "dtls");
        }

        public static async Task<RelayServerData> JoinRelayServerFromJoinCode(string joinCode)
        {
            JoinAllocation allocation;
            try
            {
                allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            }
            catch
            {
                Debug.LogError("Relay create join code request failed");
                throw;
            }

            Debug.Log($"[CgsNet] client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"[CgsNet] host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
            Debug.Log($"[CgsNet] client: {allocation.AllocationId}");

            return new RelayServerData(allocation, "dtls");
        }

        public static void Restart()
        {
            foreach (var cardStack in PlayController.Instance.playMat.GetComponentsInChildren<CardStack>())
                cardStack.MyNetworkObject.Despawn();
            foreach (var cardModel in PlayController.Instance.playMat.GetComponentsInChildren<CardModel>())
                cardModel.MyNetworkObject.Despawn();
            foreach (var die in PlayController.Instance.playMat.GetComponentsInChildren<Die>())
                die.MyNetworkObject.Despawn();
            foreach (var player in FindObjectsOfType<CgsNetPlayer>())
                player.RestartClientRpc(player.OwnerClientRpcParams);
        }

        public async void CreateLobbyWithHeartbeatAsync(string relayCode)
        {
            var createLobbyOptions = new CreateLobbyOptions
            {
                Data =
                {
                    [LobbyData.KeyRelayCode] = new DataObject(DataObject.VisibilityOptions.Public, relayCode)
                }
            };
            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(CardGameManager.Current.Name, MaxPlayers,
                createLobbyOptions);

            StartCoroutine(HeartbeatLobbyCoroutine());
        }

        private IEnumerator HeartbeatLobbyCoroutine()
        {
            while (IsOnline)
            {
                LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
                yield return new WaitForSecondsRealtime(15);
            }

            CurrentLobby = null;
        }

        private void OnApplicationQuit()
        {
            if (CurrentLobby != null)
                CurrentLobby = null;
        }
    }
}
