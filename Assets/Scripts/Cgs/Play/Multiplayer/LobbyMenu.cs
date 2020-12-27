/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using CardGameDef.Unity;
using Cgs.Menu;
using JetBrains.Annotations;
using Mirror;
using ScrollRects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(Modal))]
    public class LobbyMenu : SelectionPanel
    {
        public GameObject hostAuthenticationPrefab;

        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;
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

        private HostAuthentication Authenticator =>
            _authenticator
                ? _authenticator
                : (_authenticator = Instantiate(hostAuthenticationPrefab).GetComponent<HostAuthentication>());

        private HostAuthentication _authenticator;

        private Modal Menu =>
            _menu ? _menu : (_menu = gameObject.GetOrAddComponent<Modal>());

        private Modal _menu;

        private void Update()
        {
            if (!Menu.IsFocused || passwordInputField.isFocused)
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
            else if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Select(toggle => toggle.gameObject)
                .Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
            else if (Inputs.IsNew)
                Host();
            else if (Inputs.IsFocus)
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
            if (!IsInternetConnectionSource)
            {
                if (_selectedServerId == null || !_discoveredServers.ContainsKey(_selectedServerId.GetValueOrDefault()))
                    joinButton.interactable = false;
                Rebuild(_discoveredServers, SelectServer, _selectedServerId.GetValueOrDefault());
            }
            else
            {
                if (_selectedServerIp == null || !_listedServers.ContainsKey(_selectedServerIp))
                    joinButton.interactable = false;
                Rebuild(_listedServers, SelectServer, _selectedServerIp);
            }
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
            if (toggle.isOn)
            {
                _selectedServerId = serverId;
                joinButton.interactable = true;
            }
            else if (!toggle.group.AnyTogglesOn() && serverId == _selectedServerId)
                Join();
        }

        [UsedImplicitly]
        public void SelectServer(Toggle toggle, string serverIp)
        {
            if (toggle.isOn)
            {
                _selectedServerIp = serverIp;
                joinButton.interactable = true;
            }
            else if (!toggle.group.AnyTogglesOn() && serverIp.Equals(_selectedServerIp))
                Join();
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
                    Debug.LogError("Warning: Attempted to join a game without having selected a valid server!");
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
                    Debug.LogError("Warning: Attempted to join a game without having selected a valid server!");
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
