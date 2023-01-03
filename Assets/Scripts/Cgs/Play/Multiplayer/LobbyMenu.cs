/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cgs.Menu;
using Cgs.UI;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(Modal), typeof(CgsNetDiscovery))]
    public class LobbyMenu : SelectionPanel
    {
        public const string ShareWarningMessage =
            "WARNING: \n" +
            "You may need to ensure that all connecting players have manually loaded the latest .zip for this game.\n" +
            "For assistance, contact david@finoldigital.com";

        public string RoomIdIpLabel => "Room " + (_isLanConnectionSource ? "IP" : "Id") + ":";
        public string RoomIdIpPlaceholder => "Enter Room " + (_isLanConnectionSource ? "IP" : "Id") + "...";

        public const string InvalidServerErrorMessage =
            "Error: Attempted to join a game without having selected a valid server!";

        public const string GenericConnectionErrorMessage =
            "Exception thrown when attempting to connect to Server. Exception: ";

        private const float SecondsPerRefresh = 15;

        public CgsNetDiscovery discovery;
        public ToggleGroup lanToggleGroup;
        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;
        public Text roomIdIpLabel;
        public InputField roomIdIpInputField;
        public InputField passwordInputField;

        [UsedImplicitly]
        public bool IsLanConnectionSource
        {
            get => _isLanConnectionSource;
            set
            {
                _isLanConnectionSource = value;
                roomIdIpLabel.text = RoomIdIpLabel;
                ((Text) roomIdIpInputField.placeholder).text = RoomIdIpPlaceholder;
            }
        }

        private bool _isLanConnectionSource;

        [UsedImplicitly]
        public bool IsInternetConnectionSource
        {
            get => !IsLanConnectionSource;
            set => IsLanConnectionSource = !value;
        }

        public string RelayCode { get; set; }

        private Dictionary<string, DiscoveryResponseData> DiscoveredServers { get; } = new();

        private Dictionary<string, LobbyData> Lobbies { get; } = new();

        private string _selectedServer = string.Empty;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private float _secondsSinceRefresh = SecondsPerRefresh;

        private bool _shouldRedisplay;

        private void Start()
        {
            roomIdIpInputField.onValidateInput += (_, _, addedChar) => Inputs.FilterFocusInput(addedChar);
            passwordInputField.onValidateInput += (_, _, addedChar) => Inputs.FilterFocusInput(addedChar);
        }

        private void Update()
        {
            if (_shouldRedisplay)
                Redisplay();
            _shouldRedisplay = false;

            if (!Menu.IsFocused)
                return;

            _secondsSinceRefresh += Time.deltaTime;
            if (IsInternetConnectionSource && _secondsSinceRefresh > SecondsPerRefresh)
            {
                _secondsSinceRefresh = 0;
                RefreshLobbies();
            }

            if (roomIdIpInputField.isFocused)
            {
                if (Inputs.IsFocusNext)
                    passwordInputField.ActivateInputField();
                return;
            }

            if (passwordInputField.isFocused)
            {
                if (Inputs.IsFocusBack)
                    roomIdIpInputField.ActivateInputField();
                return;
            }

            if (Inputs.IsVertical)
            {
                if (Inputs.IsUp && !Inputs.WasUp)
                    SelectPrevious();
                else if (Inputs.IsDown && !Inputs.WasDown)
                    SelectNext();
            }

            if (Inputs.IsSubmit && joinButton.interactable)
                Join();
            else if (Inputs.IsNew)
                Host();
            else if (Inputs.IsFocusBack)
                roomIdIpInputField.ActivateInputField();
            else if (Inputs.IsFocusNext)
                passwordInputField.ActivateInputField();
            else if (Inputs.IsPageVertical && !Inputs.WasPageVertical)
                ScrollPage(Inputs.IsPageDown);
            else if (Inputs.IsPageHorizontal && !Inputs.WasPageHorizontal)
                ToggleConnectionSource();
            else if (Inputs.IsCancel)
                Close();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            DiscoveredServers.Clear();
            _selectedServer = string.Empty;

            discovery.StartClient();
            discovery.OnServerFound = OnServerFound;

            Redisplay();
        }

        private void Redisplay()
        {
            if (IsLanConnectionSource)
                Rebuild(DiscoveredServers, SelectServer, _selectedServer);
            else
                Rebuild(Lobbies, SelectServer, _selectedServer);

            joinButton.interactable =
                !string.IsNullOrEmpty(_selectedServer) &&
                Uri.IsWellFormedUriString(_selectedServer, UriKind.RelativeOrAbsolute);
        }

        private async void RefreshLobbies()
        {
            var queryLobbiesOptions = new QueryLobbiesOptions
            {
                // Filter for open lobbies only
                Filters = new List<QueryFilter>
                {
                    new(field: QueryFilter.FieldOptions.AvailableSlots, op: QueryFilter.OpOptions.GT, value: "0")
                },
                // Order by newest lobbies first
                Order = new List<QueryOrder>
                {
                    new(asc: false, field: QueryOrder.FieldOptions.Created)
                }
            };

            var response = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            Lobbies.Clear();
            foreach (var lobbyData in response.Results.Select(lobby => new LobbyData
                     {
                         Id = lobby.Id,
                         Name = lobby.Name,
                         PlayerCount = lobby.Players.Count,
                         MaxPlayers = lobby.MaxPlayers,
                         LobbyCode = lobby.LobbyCode,
                         RelayCode = lobby.Data[LobbyData.KeyRelayCode].Value
                     }))
            {
                Lobbies[lobbyData.Id] = lobbyData;
            }
        }

        private void ToggleConnectionSource()
        {
            var isInternetConnectionSource = !IsInternetConnectionSource;
            lanToggle.isOn = !isInternetConnectionSource;
            internetToggle.isOn = isInternetConnectionSource;
        }

        private void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
        {
            DiscoveredServers[sender.Address.ToString()] = response;
            _shouldRedisplay = true;
        }

        [UsedImplicitly]
        public void Host()
        {
            StartHost();
            Hide();
        }

        private void StartHost()
        {
            if (CardGameManager.Current.AutoUpdateUrl == null ||
                !CardGameManager.Current.AutoUpdateUrl.IsWellFormedOriginalString())
            {
                Debug.LogWarning(ShareWarningMessage);
                CardGameManager.Instance.Messenger.Show(ShareWarningMessage, true);
            }

            if (IsInternetConnectionSource)
                StartCoroutine(StartBroadcastingHost());
            else
            {
                NetworkManager.Singleton.StartHost();
                discovery.StartServer();
            }
        }

        private IEnumerator StartBroadcastingHost()
        {
            var serverRelayUtilityTask = CgsNetManager.AllocateRelayServerAndGetCode(CgsNetManager.MaxPlayers);
            while (!serverRelayUtilityTask.IsCompleted)
                yield return null;

            if (serverRelayUtilityTask.IsFaulted)
            {
                Debug.LogError(GenericConnectionErrorMessage + serverRelayUtilityTask.Exception?.Message);
                CardGameManager.Instance.Messenger.Show(GenericConnectionErrorMessage + serverRelayUtilityTask.Exception?.Message);
                yield break;
            }

            var relayServerData = serverRelayUtilityTask.Result;
            CgsNetManager.Instance.Transport.SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();

            yield return null;

            CgsNetManager.Instance.CreateLobbyWithHeartbeatAsync(RelayCode);
        }

        [UsedImplicitly]
        public void SelectServer(Toggle toggle, string server)
        {
            if (toggle.isOn)
            {
                _selectedServer = server;
                if (!string.IsNullOrEmpty(roomIdIpInputField.text))
                    roomIdIpInputField.text = string.Empty;
                joinButton.interactable = true;
            }
            else if (!roomIdIpInputField.isFocused && !toggle.group.AnyTogglesOn() &&
                     server.Equals(_selectedServer))
                Join();
        }

        [UsedImplicitly]
        public void SetTargetIpAddress(string targetIpAddress)
        {
            if (string.IsNullOrEmpty(targetIpAddress))
                return;

            _selectedServer = targetIpAddress;
            lanToggleGroup.SetAllTogglesOff();
            joinButton.interactable = !string.IsNullOrWhiteSpace(_selectedServer)
                                      && Uri.IsWellFormedUriString(_selectedServer, UriKind.RelativeOrAbsolute);
        }

        [UsedImplicitly]
        public void Join()
        {
            if (IsInternetConnectionSource)
            {
                if (Lobbies.ContainsKey(_selectedServer))
                {
                    RelayCode = Lobbies[_selectedServer].RelayCode;
                    StartCoroutine(JoinRelayCoroutine());
                }
                else
                {
                    if (Uri.IsWellFormedUriString(_selectedServer, UriKind.Absolute))
                    {
                        CgsNetManager.Instance.Transport.SetConnectionData(_selectedServer, CgsNetManager.DefaultPort);
                        NetworkManager.Singleton.StartClient();
                    }
                    else
                        StartCoroutine(JoinLobbyCoroutine());
                }
            }
            else
            {
                if (DiscoveredServers.TryGetValue(_selectedServer, out var discoveryResponse))
                {
                    CgsNetManager.Instance.Transport.SetConnectionData(_selectedServer, discoveryResponse.Port);
                    CgsNetManager.Instance.StartClient();
                }
                else if (Uri.IsWellFormedUriString(_selectedServer, UriKind.RelativeOrAbsolute))
                {
                    CgsNetManager.Instance.Transport.SetConnectionData(_selectedServer, CgsNetManager.DefaultPort);
                    CgsNetManager.Instance.StartClient();
                }
                else
                {
                    Debug.LogError(InvalidServerErrorMessage);
                    CardGameManager.Instance.Messenger.Show(InvalidServerErrorMessage);
                    return;
                }
            }

            Hide();
        }

        private IEnumerator JoinLobbyCoroutine()
        {
            var lobbyTask = GetLobby(_selectedServer);
            while (!lobbyTask.IsCompleted)
                yield return null;

            if (lobbyTask.IsFaulted)
            {
                Debug.LogError(GenericConnectionErrorMessage + lobbyTask.Exception?.Message);
                CardGameManager.Instance.Messenger.Show(GenericConnectionErrorMessage + lobbyTask.Exception?.Message);
                yield break;
            }

            var lobby = lobbyTask.Result;
            if (lobby is {Data: { }} && lobby.Data.TryGetValue(LobbyData.KeyRelayCode, out var dataObject))
            {
                RelayCode = dataObject.Value;
                yield return JoinRelayCoroutine();
            }
            else
            {
                Debug.LogError(InvalidServerErrorMessage);
                CardGameManager.Instance.Messenger.Show(InvalidServerErrorMessage);
            }
        }

        private static async Task<Lobby> GetLobby(string lobbyId)
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
            return lobby;
        }

        private IEnumerator JoinRelayCoroutine()
        {
            var clientRelayUtilityTask = CgsNetManager.JoinRelayServerFromJoinCode(RelayCode);
            while (!clientRelayUtilityTask.IsCompleted)
                yield return null;

            if (clientRelayUtilityTask.IsFaulted)
            {
                Debug.LogError(GenericConnectionErrorMessage + clientRelayUtilityTask.Exception?.Message);
                CardGameManager.Instance.Messenger.Show(GenericConnectionErrorMessage + clientRelayUtilityTask.Exception?.Message);
                yield break;
            }

            var relayServerData = clientRelayUtilityTask.Result;

            CgsNetManager.Instance.Transport.SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }

        public void Hide()
        {
            if (discovery.IsRunning && !CgsNetManager.Instance.IsServer)
                discovery.StopDiscovery();

            Menu.Hide();
        }

        [UsedImplicitly]
        public void Close()
        {
            if (discovery.IsRunning && !CgsNetManager.Instance.IsServer)
                discovery.StopDiscovery();

            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
