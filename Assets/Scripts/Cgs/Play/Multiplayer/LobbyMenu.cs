/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using Cgs.Menu;
using JetBrains.Annotations;
using Mirror;
using ScrollRects;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(Modal))]
    public class LobbyMenu : SelectionPanel
    {
        public GameObject hostAuthenticationPrefab;

        public ToggleGroup lanToggleGroup;
        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;
        public InputField ipInputField;
        public InputField passwordInputField;

        [UsedImplicitly]
        public bool IsLanConnectionSource
        {
            get => !IsInternetConnectionSource;
            set => IsInternetConnectionSource = !value;
        }

        [UsedImplicitly] public bool IsInternetConnectionSource { get; set; }

        private IReadOnlyDictionary<long, DiscoveryResponse> DiscoveredServers => _discoveredServers;

        private readonly Dictionary<long, DiscoveryResponse> _discoveredServers =
            new Dictionary<long, DiscoveryResponse>();

        private long? _selectedServerId;

        private IReadOnlyDictionary<string, ServerStatus> ListedServers => _listedServers;
        private readonly Dictionary<string, ServerStatus> _listedServers = new Dictionary<string, ServerStatus>();
        private string _selectedServerIp;

        private string TargetIpAddress =>
            IsInternetConnectionSource && _selectedServerIp != null &&
            _listedServers.TryGetValue(_selectedServerIp, out ServerStatus internetServer)
                ? internetServer.Ip
                : IsLanConnectionSource && _discoveredServers.TryGetValue(_selectedServerId.GetValueOrDefault(),
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

        private void Start()
        {
            ipInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            passwordInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
        }

        private void Update()
        {
            if (!Menu.IsFocused)
                return;

            if (ipInputField.isFocused)
            {
                if (Inputs.IsFocusNext)
                    passwordInputField.ActivateInputField();
                return;
            }

            if (passwordInputField.isFocused)
            {
                if (Inputs.IsFocusBack)
                    ipInputField.ActivateInputField();
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
                ipInputField.ActivateInputField();
            else if (Inputs.IsFocusNext)
                passwordInputField.ActivateInputField();
            else if (Inputs.IsPageVertical && !Inputs.IsPageVertical)
                ScrollPage(Inputs.IsPageDown);
            else if (Inputs.IsPageHorizontal && !Inputs.WasPageHorizontal)
                ToggleConnectionSource();
            else if (Inputs.IsCancel)
                Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            _discoveredServers.Clear();
            _selectedServerId = null;

            _listedServers.Clear();
            _selectedServerIp = null;

            CgsNetManager.Instance.Discovery.OnServerFound = OnDiscoveredServer;
            CgsNetManager.Instance.Discovery.StartDiscovery();

            CgsNetManager.Instance.ListServer.OnServerFound = OnListServer;
            CgsNetManager.Instance.ListServer.StartClient();

            Redisplay();
        }

        private void Redisplay()
        {
            if (IsLanConnectionSource)
                Rebuild(_discoveredServers, SelectServer, _selectedServerId.GetValueOrDefault());
            else
                Rebuild(_listedServers, SelectServer, _selectedServerIp);

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

        private void OnListServer(ServerStatus info)
        {
            _listedServers[info.Ip] = info;
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
            NetworkManager.singleton.StartHost();
            CgsNetManager.Instance.Discovery.AdvertiseServer();
            if (!IsInternetConnectionSource)
                return;

            CgsNetManager.Instance.ListServer.StartGameServer();
            CgsNetManager.Instance.CheckForPortForwarding();
        }

        [UsedImplicitly]
        public void SelectServer(Toggle toggle, long serverId)
        {
            _selectedServerIp = null;
            if (toggle.isOn)
            {
                _selectedServerId = serverId;
                if (!string.IsNullOrEmpty(ipInputField.text))
                    ipInputField.text = string.Empty;
                joinButton.interactable = true;
            }
            else if (!ipInputField.isFocused && !toggle.group.AnyTogglesOn() && serverId == _selectedServerId)
                Join();
        }

        [UsedImplicitly]
        public void SelectServer(Toggle toggle, string serverIp)
        {
            _selectedServerId = null;
            if (toggle.isOn)
            {
                _selectedServerIp = serverIp;
                if (!string.IsNullOrEmpty(ipInputField.text))
                    ipInputField.text = string.Empty;
                joinButton.interactable = true;
            }
            else if (!ipInputField.isFocused && !toggle.group.AnyTogglesOn() && serverIp.Equals(_selectedServerIp))
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
            if (!IsInternetConnectionSource)
            {
                if (_selectedServerId == null
                    || !DiscoveredServers.TryGetValue(_selectedServerId.GetValueOrDefault(),
                        out DiscoveryResponse serverResponse)
                    || serverResponse.Uri == null)
                {
                    Debug.LogError("Error: Attempted to join a game without having selected a valid server!");
                    return;
                }

                NetworkManager.singleton.StartClient(serverResponse.Uri);
            }
            else
            {
                if (_selectedServerIp == null
                    || !ListedServers.TryGetValue(_selectedServerIp, out ServerStatus serverResponse)
                    || string.IsNullOrEmpty(serverResponse.Ip))
                {
                    if (!Uri.IsWellFormedUriString(_selectedServerIp, UriKind.RelativeOrAbsolute))
                    {
                        Debug.LogError("Error: Attempted to join a game without having selected a valid server!");
                        return;
                    }

                    NetworkManager.singleton.networkAddress = _selectedServerIp;
                    NetworkManager.singleton.StartClient();
                    return;
                }

                NetworkManager.singleton.networkAddress = serverResponse.Ip;
                NetworkManager.singleton.StartClient();
            }

            Hide();
        }

        [UsedImplicitly]
        public void Hide()
        {
            if (!NetworkServer.active)
            {
                CgsNetManager.Instance.Discovery.StopDiscovery();
                CgsNetManager.Instance.ListServer.Stop();
            }

            Menu.Hide();
        }
    }
}
