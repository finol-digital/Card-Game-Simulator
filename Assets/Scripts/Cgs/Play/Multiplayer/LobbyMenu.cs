/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Net;
using Cgs.Menu;
using Cgs.UI;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(Modal))]
    public class LobbyMenu : SelectionPanel
    {
        public const string AndroidWarningMessage =
            "WARNING!!!\nYou are connecting to an Android host, which is likely to lose connection or have errors.\nIt is recommended to use a PC host.";

        public const string ShareWarningMessage =
            "WARNING!!!\nYou are hosting a game that has not been properly uploaded.\nContact david@finoldigital.com for assistance in uploading.";

        public string RoomIdIpLabel => "Room " + (_isLanConnectionSource ? "IP" : "Id") + ":";
        public string RoomIdIpPlaceholder => "Enter Room " + (_isLanConnectionSource ? "IP" : "Id") + "...";

        public const string ConnectionErrorMessage =
            "Error: Attempted to join a game without having selected a valid server!";

        public const string PasswordErrorMessage = "Error: Wrong Password!";

        private CgsNetDiscovery Discovery => _discovery ??= CgsNetManager.Instance.GetComponent<CgsNetDiscovery>();

        private CgsNetDiscovery _discovery;

        private const float ServerListUpdateTime = 5;

        public GameObject hostAuthenticationPrefab;

        public ToggleGroup lanToggleGroup;
        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;
        public Text roomIdIpLabel;
        public InputField roomIdIpInputField;
        public InputField passwordInputField;

        private string _password = "";

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

        private string _selectedServer = string.Empty;

        private HostAuthentication Authenticator =>
            _authenticator ??= Instantiate(hostAuthenticationPrefab).GetComponent<HostAuthentication>();

        private HostAuthentication _authenticator;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        // TODO: private float _lrmUpdateSecond = ServerListUpdateTime;

        private bool _shouldRedisplay;

        private void OnEnable()
        {
            ListenToRelay();
        }

        private void ListenToRelay()
        {
            // TODO: CgsNetManager.Instance.lrm.serverListUpdated.RemoveAllListeners();
            // TODO: CgsNetManager.Instance.lrm.serverListUpdated.AddListener(Redisplay);
        }

        private void Start()
        {
            roomIdIpInputField.onValidateInput += (_, _, addedChar) => Inputs.FilterFocusInput(addedChar);
            passwordInputField.onValidateInput += (_, _, addedChar) => Inputs.FilterFocusInput(addedChar);
            ListenToRelay();
        }

        private void Update()
        {
            if (_shouldRedisplay)
                Redisplay();
            _shouldRedisplay = false;

            if (!Menu.IsFocused)
                return;

            /*
            // TODO:
            _lrmUpdateSecond += Time.deltaTime;
            if (IsInternetConnectionSource && CgsNetManager.Instance.lrm.IsAuthenticated() &&
                _lrmUpdateSecond > ServerListUpdateTime)
            {
                CgsNetManager.Instance.lrm.RequestServerList();
                _lrmUpdateSecond = 0;
            }*/

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

            Discovery.StartClient();
            Discovery.OnServerFound = OnServerFound;

            Redisplay();
        }

        private void Redisplay()
        {
            if (IsLanConnectionSource)
                Rebuild(DiscoveredServers, SelectServer, _selectedServer);
            /* todo: else
                Rebuild(
                    CgsNetManager.Instance.lrm.relayServerList.ToDictionary(server => server.serverId,
                        server => server), SelectServer, _selectedServer);*/

            joinButton.interactable =
                !string.IsNullOrEmpty(_selectedServer) &&
                Uri.IsWellFormedUriString(_selectedServer, UriKind.RelativeOrAbsolute);
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
            if (CardGameManager.Instance.IsSearchingForServer)
                Authenticator.Show(StartHost);
            else
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

            /* todo:
            if (IsInternetConnectionSource)
            {
                CgsNetManager.Instance.lrm.serverName = CardGameManager.Current.Name;
                CgsNetManager.Instance.lrm.extraServerData = JsonUtility.ToJson(CgsNetManager.Instance.RoomData);
                CgsNetManager.Instance.lrm.isPublicServer = true;
            }
            else
                Transport.activeTransport = CgsNetManager.Instance.lanConnector.directConnectTransport;*/

            NetworkManager.Singleton.StartHost();
            Discovery.StartServer();
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
        public void SetPassword(string password)
        {
            _password = password;
        }

        [UsedImplicitly]
        public void Join()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += PasswordCheck;

            if (IsLanConnectionSource)
            {
                if (DiscoveredServers.TryGetValue(_selectedServer, out var discoveryResponse))
                {
                    CgsNetManager.Instance.RoomName = discoveryResponse.ServerName;
                    CgsNetManager.Instance.Transport.SetConnectionData(_selectedServer, discoveryResponse.Port);
                    CgsNetManager.Instance.StartClient();
                }
                else if (Uri.IsWellFormedUriString(_selectedServer, UriKind.RelativeOrAbsolute))
                {
                    CgsNetManager.Instance.RoomName = discoveryResponse.ServerName;
                    CgsNetManager.Instance.Transport.SetConnectionData(_selectedServer, 7777);
                    CgsNetManager.Instance.StartClient();
                }
                else
                {
                    Debug.LogError(ConnectionErrorMessage);
                    CardGameManager.Instance.Messenger.Show(ConnectionErrorMessage);
                    return;
                }
            }
            else
            {
                /* todo
                if (CgsNetManager.Instance.lrm.relayServerList.ToDictionary(server => server.serverId,
                        server => server).TryGetValue(_selectedServer, out var serverRoom))
                {
                    CgsNetManager.Instance.RoomName = serverRoom.serverName;
                    if (RuntimePlatform.Android.ToString().Equals(serverRoom.serverData))
                        CardGameManager.Instance.Messenger.Show(AndroidWarningMessage, true);
                }

                NetworkManager.Singleton.networkAddress = _selectedServer;
                NetworkManager.Singleton.StartClient();*/
            }

            Hide();
        }

        private void PasswordCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = _password.Equals(CgsNetManager.Instance.RoomPassword);

            if (!response.Approved)
                return;

            Debug.LogError(PasswordErrorMessage);
            CardGameManager.Instance.Messenger.Show(PasswordErrorMessage);

            PlayController.BackToMainMenu();
        }

        public void Hide()
        {
            if (Discovery.IsRunning && !CgsNetManager.Instance.IsServer)
                Discovery.StopDiscovery();

            Menu.Hide();
        }

        [UsedImplicitly]
        public void Close()
        {
            if (Discovery.IsRunning && !CgsNetManager.Instance.IsServer)
                Discovery.StopDiscovery();

            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }

        private void OnDisable()
        {
            /* TODO:
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.lrm != null)
                CgsNetManager.Instance.lrm.serverListUpdated.RemoveListener(Redisplay);*/
        }
    }
}
