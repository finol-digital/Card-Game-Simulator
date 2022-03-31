/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Cgs.Menu;
using Cgs.UI;
using JetBrains.Annotations;
using Mirror;
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

        public string RoomIdIpLabel => "Room " + (_isLanConnectionSource ? "IP" : "Id") + ":";
        public string RoomIdIpPlaceholder => "Enter Room " + (_isLanConnectionSource ? "IP" : "Id") + "...";

        public const string ConnectionErrorMessage =
            "Error: Attempted to join a game without having selected a valid server!";

        private const float ServerListUpdateTime = 5;

        public GameObject hostAuthenticationPrefab;

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

        private IReadOnlyDictionary<long, DiscoveryResponse> DiscoveredServers => _discoveredServers;

        private readonly Dictionary<long, DiscoveryResponse> _discoveredServers =
            new Dictionary<long, DiscoveryResponse>();

        private long? _selectedServerId;
        private string _selectedServerIp;

        private string TargetIpAddress =>
            IsLanConnectionSource && _discoveredServers.TryGetValue(_selectedServerId.GetValueOrDefault(),
                out var discoveryResponse)
                ? discoveryResponse.Uri.ToString()
                : _selectedServerIp;

        private HostAuthentication Authenticator =>
            _authenticator ??= Instantiate(hostAuthenticationPrefab).GetComponent<HostAuthentication>();

        private HostAuthentication _authenticator;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private float _lrmUpdateSecond = ServerListUpdateTime;

        private bool _shouldRedisplay;

        private void OnEnable()
        {
            EnableLrm();
        }

        private void EnableLrm()
        {
            CgsNetManager.Instance.lrm.serverListUpdated.RemoveAllListeners();
            CgsNetManager.Instance.lrm.serverListUpdated.AddListener(Redisplay);
        }

        private void Start()
        {
            roomIdIpInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            passwordInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            EnableLrm();
        }

        private void Update()
        {
            if (_shouldRedisplay)
                Redisplay();
            _shouldRedisplay = false;

            if (!Menu.IsFocused)
                return;

            _lrmUpdateSecond += Time.deltaTime;
            if (IsInternetConnectionSource && CgsNetManager.Instance.lrm.IsAuthenticated() &&
                _lrmUpdateSecond > ServerListUpdateTime)
            {
                CgsNetManager.Instance.lrm.RequestServerList();
                _lrmUpdateSecond = 0;
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
            else if (Inputs.IsPageVertical && !Inputs.IsPageVertical)
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

            _discoveredServers.Clear();
            _selectedServerId = null;
            _selectedServerIp = null;

            CgsNetManager.Instance.Discovery.StartDiscovery();
            CgsNetManager.Instance.Discovery.OnServerFound = OnDiscoveredServer;

            Redisplay();
        }

        private void Redisplay()
        {
            if (IsLanConnectionSource)
                Rebuild(_discoveredServers, SelectServer, _selectedServerId.GetValueOrDefault());
            else
                Rebuild(
                    CgsNetManager.Instance.lrm.relayServerList.ToDictionary(server => server.serverId,
                        server => server),
                    SelectServer,
                    _selectedServerIp);

            var ip = TargetIpAddress;
            joinButton.interactable =
                !string.IsNullOrEmpty(ip) && Uri.IsWellFormedUriString(ip, UriKind.RelativeOrAbsolute);
        }

        private void ToggleConnectionSource()
        {
            var isInternetConnectionSource = !IsInternetConnectionSource;
            lanToggle.isOn = !isInternetConnectionSource;
            internetToggle.isOn = isInternetConnectionSource;
        }

        private void OnDiscoveredServer(DiscoveryResponse info)
        {
            _discoveredServers[info.ServerId] = info;
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
            if (IsInternetConnectionSource)
            {
                CgsNetManager.Instance.lrm.serverName = CgsNetManager.Instance.RoomName;
                CgsNetManager.Instance.lrm.extraServerData = Application.platform.ToString();
                CgsNetManager.Instance.lrm.isPublicServer = true;
            }
            else
                Transport.activeTransport = CgsNetManager.Instance.lanConnector.directConnectTransport;

            NetworkManager.singleton.StartHost();
            CgsNetManager.Instance.Discovery.AdvertiseServer();
        }

        [UsedImplicitly]
        public void SelectServer(Toggle toggle, long serverId)
        {
            _selectedServerIp = null;
            if (toggle.isOn)
            {
                _selectedServerId = serverId;
                if (!string.IsNullOrEmpty(roomIdIpInputField.text))
                    roomIdIpInputField.text = string.Empty;
                joinButton.interactable = true;
            }
            else if (!roomIdIpInputField.isFocused && !toggle.group.AnyTogglesOn() && serverId == _selectedServerId)
                Join();
        }

        [UsedImplicitly]
        public void SelectServer(Toggle toggle, string serverId)
        {
            _selectedServerId = null;
            if (toggle.isOn)
            {
                _selectedServerIp = serverId;
                if (!string.IsNullOrEmpty(roomIdIpInputField.text))
                    roomIdIpInputField.text = string.Empty;
                joinButton.interactable = true;
            }
            else if (!roomIdIpInputField.isFocused && !toggle.group.AnyTogglesOn() &&
                     serverId.Equals(_selectedServerIp))
                Join();
        }

        [UsedImplicitly]
        public void SetTargetIpAddress(string targetIpAddress)
        {
            if (string.IsNullOrEmpty(targetIpAddress))
                return;

            _selectedServerId = null;
            _selectedServerIp = targetIpAddress;
            lanToggleGroup.SetAllTogglesOff();
            joinButton.interactable = !string.IsNullOrWhiteSpace(_selectedServerIp)
                                      && Uri.IsWellFormedUriString(_selectedServerIp, UriKind.RelativeOrAbsolute);
        }

        [UsedImplicitly]
        public void SetPassword(string password)
        {
            Authenticator.passwordInputField.text = password;
            Authenticator.SetPassword(password);
        }

        [UsedImplicitly]
        public void Join()
        {
            if (IsLanConnectionSource)
            {
                if (_selectedServerId != null && DiscoveredServers.TryGetValue(_selectedServerId.GetValueOrDefault(),
                        out var discoveryResponse) && discoveryResponse.Uri != null)
                {
                    CgsNetManager.Instance.RoomName = discoveryResponse.RoomName;
                    Transport.activeTransport = CgsNetManager.Instance.lanConnector.directConnectTransport;
                    if (RuntimePlatform.Android.ToString().Equals(discoveryResponse.HostPlatform))
                        CardGameManager.Instance.Messenger.Show(AndroidWarningMessage, true);
                    NetworkManager.singleton.StartClient(discoveryResponse.Uri);
                }
                else if (Uri.IsWellFormedUriString(_selectedServerIp, UriKind.RelativeOrAbsolute))
                {
                    Transport.activeTransport = CgsNetManager.Instance.lanConnector.directConnectTransport;
                    var host = _selectedServerIp.StartsWith("lrm://") ? "" : "lrm://" + _selectedServerIp;
                    NetworkManager.singleton.StartClient(new Uri(host));
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
                if (CgsNetManager.Instance.lrm.relayServerList.ToDictionary(server => server.serverId,
                        server => server).TryGetValue(_selectedServerIp, out var serverRoom))
                {
                    CgsNetManager.Instance.RoomName = serverRoom.serverName;
                    if (RuntimePlatform.Android.ToString().Equals(serverRoom.serverData))
                        CardGameManager.Instance.Messenger.Show(AndroidWarningMessage, true);
                }

                NetworkManager.singleton.networkAddress = _selectedServerIp;
                NetworkManager.singleton.StartClient();
            }

            Hide();
        }

        public void Hide()
        {
            if (!NetworkServer.active)
                CgsNetManager.Instance.Discovery.StopDiscovery();

            Menu.Hide();
        }

        [UsedImplicitly]
        public void Close()
        {
            if (!NetworkServer.active)
                CgsNetManager.Instance.Discovery.StopDiscovery();

            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }

        private void OnDisable()
        {
            CgsNetManager.Instance.lrm.serverListUpdated.RemoveListener(Redisplay);
        }
    }
}
