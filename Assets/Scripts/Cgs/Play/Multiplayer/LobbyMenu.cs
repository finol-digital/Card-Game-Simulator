/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Cgs.Menu;
using JetBrains.Annotations;
using LightReflectiveMirror;
using Mirror;
using ScrollRects;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(Modal))]
    public class LobbyMenu : SelectionPanel
    {
        private const float ServerListUpdateTime = 5;

        public GameObject hostAuthenticationPrefab;

        public ToggleGroup lanToggleGroup;
        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;
        public Text roomIdIpLabel;
        public InputField roomIdIpInputField;
        public InputField passwordInputField;

        public string RoomName { get; set; } = CardGameManager.Current.Name; // TODO: SET THIS WHEN CLIENT CONNECTS TO SERVER

        [UsedImplicitly]
        public bool IsLanConnectionSource
        {
            get => _isLanConnectionSource;
            set
            {
                _isLanConnectionSource = value;
                roomIdIpLabel.text = "Room " + (_isLanConnectionSource ? "IP" : "Id") + ":";
                ((Text)roomIdIpInputField.placeholder).text =
                    "Enter Room " + (_isLanConnectionSource ? "IP" : "Id") + "...";
            }
        }

        private bool _isLanConnectionSource;

        [UsedImplicitly]
        public bool IsInternetConnectionSource
        {
            get => !IsLanConnectionSource;
            set => IsLanConnectionSource = !value;
        }

        public string IdIp => IsInternetConnectionSource
            ? _lrm.serverId
            : _lrm != null && _lrm.IsAuthenticated()
                ? _lrm.ServerUri().ToString() // TODO: CONFIRM THIS LINE WORKS
                : CgsNetManager.Instance.lanConnector.directConnectTransport.ServerUri().ToString();

        private IReadOnlyDictionary<long, DiscoveryResponse> DiscoveredServers => _discoveredServers;

        private readonly Dictionary<long, DiscoveryResponse> _discoveredServers =
            new Dictionary<long, DiscoveryResponse>();

        private long? _selectedServerId;
        private string _selectedServerIp;

        private string TargetIpAddress =>
            IsLanConnectionSource && _discoveredServers.TryGetValue(_selectedServerId.GetValueOrDefault(),
                out DiscoveryResponse lanServer)
                ? lanServer.Uri.ToString()
                : _selectedServerIp;

        private HostAuthentication Authenticator =>
            _authenticator
                ? _authenticator
                : (_authenticator = Instantiate(hostAuthenticationPrefab).GetComponent<HostAuthentication>());

        private HostAuthentication _authenticator;

        private Modal Menu =>
            _menu ? _menu : (_menu = gameObject.GetOrAddComponent<Modal>());

        private Modal _menu;

        private LightReflectiveMirrorTransport _lrm;
        private float _lrmUpdateSecond = ServerListUpdateTime;

        private void OnEnable()
        {
            EnableLrm();
        }

        private void EnableLrm()
        {
            if (_lrm == null)
                _lrm = Transport.activeTransport as LightReflectiveMirrorTransport;
            if (_lrm == null)
                return;

            _lrm.serverListUpdated.RemoveAllListeners();
            _lrm.serverListUpdated.AddListener(Redisplay);
        }

        private void Start()
        {
            roomIdIpInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            passwordInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            EnableLrm();
        }

        private void Update()
        {
            if (!Menu.IsFocused)
                return;

            _lrmUpdateSecond += Time.deltaTime;
            if (IsInternetConnectionSource && _lrm != null && _lrm.IsAuthenticated() &&
                _lrmUpdateSecond > ServerListUpdateTime)
            {
                _lrm.RequestServerList();
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

            CgsNetManager.Instance.Discovery.OnServerFound = OnDiscoveredServer;
            CgsNetManager.Instance.Discovery.StartDiscovery();

            Redisplay();
        }

        private void Redisplay()
        {
            if (IsLanConnectionSource)
                Rebuild(_discoveredServers, SelectServer, _selectedServerId.GetValueOrDefault());
            else
                Rebuild(_lrm.relayServerList.ToDictionary(server => server.serverId, server => server),
                    SelectServer,
                    _selectedServerIp);

            string ip = TargetIpAddress;
            joinButton.interactable =
                !string.IsNullOrEmpty(ip) && Uri.IsWellFormedUriString(ip, UriKind.RelativeOrAbsolute);
        }

        private void ToggleConnectionSource()
        {
            bool isInternetConnectionSource = !IsInternetConnectionSource;
            lanToggle.isOn = !isInternetConnectionSource;
            internetToggle.isOn = isInternetConnectionSource;
        }

        private void OnDiscoveredServer(DiscoveryResponse info)
        {
            _discoveredServers[info.ServerId] = info;
            Redisplay();
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
                _lrm.serverName = RoomName;
                _lrm.isPublicServer = true;
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
                if (_selectedServerId == null
                    || !DiscoveredServers.TryGetValue(_selectedServerId.GetValueOrDefault(),
                        out DiscoveryResponse serverResponse)
                    || serverResponse.Uri == null)
                {
                    Debug.LogError("Error: Attempted to join a game without having selected a valid server!");
                    CardGameManager.Instance.Messenger.Show(
                        "Error: Attempted to join a game without having selected a valid server!");
                    return;
                }

                Transport.activeTransport = CgsNetManager.Instance.lanConnector.directConnectTransport;
                NetworkManager.singleton.StartClient(serverResponse.Uri);
            }
            else
            {
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
            if (_lrm != null)
                _lrm.serverListUpdated.RemoveListener(Redisplay);
        }
    }
}
