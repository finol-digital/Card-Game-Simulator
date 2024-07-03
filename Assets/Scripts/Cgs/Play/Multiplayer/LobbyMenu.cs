/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cgs.Menu;
using Cgs.UI;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(Modal))]
    public class LobbyMenu : SelectionPanel
    {
        public const string ShareWarningMessage =
            "WARNING: \n" +
            "You may need to ensure that all connecting players have manually loaded the latest .zip for this game.\n" +
            "For assistance, contact david@finoldigital.com";

        public const string InvalidServerErrorMessage =
            "Error: Attempted to join a game without having selected a valid server!";

        public string RoomIdIpLabel => "Room " + (_isLanConnectionSource ? "IP" : "Id") + ":";
        public string RoomIdIpPlaceholder => "Enter Room " + (_isLanConnectionSource ? "IP" : "Id") + "...";

        private const float SecondsPerRefresh = 5;

        public ToggleGroup lanToggleGroup;
        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;
        public Text roomIdIpLabel;
        public InputField roomIdIpInputField;

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

        private Dictionary<string, DiscoveryResponseData> DiscoveredServers { get; } = new();

        private Dictionary<string, LobbyData> Lobbies { get; } = new();

        private string _selectedServer = string.Empty;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private float _secondsSinceRefresh;

        private bool _shouldRedisplay;

        private void Start()
        {
            roomIdIpInputField.onValidateInput += (_, _, addedChar) => Inputs.FilterFocusInput(addedChar);
            StartCoroutine(SignInAnonymouslyCoroutine());
        }

        private IEnumerator SignInAnonymouslyCoroutine()
        {
            yield return null;

            if (AuthenticationService.Instance.IsSignedIn)
                yield break;

            var signInTask = CgsNetManager.SignInAnonymouslyAsync();
            while (!signInTask.IsCompleted)
                yield return null;

            if (signInTask.IsFaulted)
            {
                Debug.LogError(CgsNetManager.GenericConnectionErrorMessage + signInTask.Exception?.Message);
                CardGameManager.Instance.Messenger.Show(CgsNetManager.GenericConnectionErrorMessage +
                                                        signInTask.Exception?.Message);
            }

            _secondsSinceRefresh = SecondsPerRefresh;
        }

        private void Update()
        {
            _secondsSinceRefresh += Time.deltaTime;
            if (IsInternetConnectionSource && _secondsSinceRefresh > SecondsPerRefresh)
            {
                _secondsSinceRefresh = 0;
                if (AuthenticationService.Instance.IsSignedIn)
#pragma warning disable CS4014
                    RefreshLobbies();
#pragma warning restore CS4014
                else if (IsLanConnectionSource)
                    CgsNetManager.Instance.Discovery.ClientBroadcast(new DiscoveryBroadcastData());
            }

            if (_shouldRedisplay)
                Redisplay();

            if (!Menu.IsFocused || roomIdIpInputField.isFocused)
                return;

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
            else if (Inputs.IsFocusNext)
                roomIdIpInputField.ActivateInputField();
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

            _selectedServer = string.Empty;
            if (CgsNetManager.Instance.Discovery.IsRunning)
                CgsNetManager.Instance.Discovery.StopDiscovery();
            DiscoveredServers.Clear();
            CgsNetManager.Instance.Discovery.StartClient();
            CgsNetManager.Instance.Discovery.OnServerFound = OnServerFound;

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

            _shouldRedisplay = false;
        }

        private async Task RefreshLobbies()
        {
            var queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 15,
                // Filter for open lobbies only
                Filters = new List<QueryFilter>
                {
                    new(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0")
                },
                // Order by newest lobbies first
                Order = new List<QueryOrder>
                {
                    new(
                        asc: false,
                        field: QueryOrder.FieldOptions.Created)
                }
            };

            var response = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Lobbies.Clear();
            foreach (var lobby in response.Results)
            {
                var lobbyData = new LobbyData
                {
                    Id = lobby.Id,
                    Name = lobby.Name,
                    PlayerCount = lobby.Players.Count,
                    MaxPlayers = lobby.MaxPlayers
                };
                if (lobby.Data != null && lobby.Data.TryGetValue(LobbyData.KeyRelayJoinCode, out var code))
                    lobbyData.RelayJoinCode = code.Value;
                Lobbies[lobbyData.Id] = lobbyData;
            }

            Debug.Log($"[CgsNet LobbyMenu] RefreshLobbies: {Lobbies.Count}");

            _shouldRedisplay = true;
        }

        private void ToggleConnectionSource()
        {
            var isInternetConnectionSource = !IsInternetConnectionSource;
            lanToggle.isOn = !isInternetConnectionSource;
            internetToggle.isOn = isInternetConnectionSource;
        }

        private void OnServerFound(IPEndPoint sender, DiscoveryResponseData response)
        {
            Debug.Log($"OnServerFound {sender} {response}");
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
            if (IsInternetConnectionSource)
            {
                if (CardGameManager.Current.AutoUpdateUrl == null ||
                    !CardGameManager.Current.AutoUpdateUrl.IsWellFormedOriginalString())
                    CardGameManager.Instance.Messenger.Show(ShareWarningMessage);
                CgsNetManager.Instance.StartBroadcastHost();
            }
            else
            {
                CgsNetManager.Instance.Transport = CgsNetManager.Instance.Transports.unityTransport;
                CgsNetManager.Instance.Transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
                NetworkManager.Singleton.StartHost();
                if (CgsNetManager.Instance.Discovery.IsRunning)
                    CgsNetManager.Instance.Discovery.StopDiscovery();
                CgsNetManager.Instance.Discovery.StartServer();
            }
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
                if (Lobbies.TryGetValue(_selectedServer, out var lobby))
                    CgsNetManager.Instance.StartJoinLobby(lobby.Id);
                else
                {
                    if (Uri.IsWellFormedUriString(_selectedServer, UriKind.Absolute))
                        CgsNetManager.Instance.StartJoin(_selectedServer);
                    else
                        CgsNetManager.Instance.StartJoinLobbyCode(_selectedServer);
                }
            }
            else
            {
                if (DiscoveredServers.TryGetValue(_selectedServer, out var discoveryResponse))
                {
                    CgsNetManager.Instance.Transport = CgsNetManager.Instance.Transports.unityTransport;
                    CgsNetManager.Instance.Transport.SetConnectionData(_selectedServer, discoveryResponse.Port,
                        "0.0.0.0");
                    NetworkManager.Singleton.StartClient();
                }
                else if (Uri.IsWellFormedUriString(_selectedServer, UriKind.RelativeOrAbsolute))
                {
                    CgsNetManager.Instance.Transport = CgsNetManager.Instance.Transports.unityTransport;
                    CgsNetManager.Instance.Transport.SetConnectionData(_selectedServer, 7777, "0.0.0.0");
                    NetworkManager.Singleton.StartClient();
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

        public void Hide()
        {
            if (CgsNetManager.Instance.Discovery.IsRunning && !CgsNetManager.Instance.IsServer)
                CgsNetManager.Instance.Discovery.StopDiscovery();

            Menu.Hide();
        }

        [UsedImplicitly]
        public void Close()
        {
            if (CgsNetManager.Instance.Discovery.IsRunning && !CgsNetManager.Instance.IsServer)
                CgsNetManager.Instance.Discovery.StopDiscovery();

            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
