/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Mirror;

using CGS.Menu;

namespace CGS.Play.Multiplayer
{
    [RequireComponent(typeof(Modal))]
    public class LobbyMenu : SelectionPanel
    {
        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;

        public bool IsInternetConnectionSource { get; private set; } = false;

        public long? SelectedServerId { get; private set; } = null;
        public IReadOnlyDictionary<long, DiscoveryResponse> DiscoveredServers => _discoveredServers;
        private readonly Dictionary<long, DiscoveryResponse> _discoveredServers = new Dictionary<long, DiscoveryResponse>();

        public string SelectedServerIp { get; private set; } = null;
        public IReadOnlyDictionary<string, ServerStatus> ListedServers => _listedServers;
        private readonly Dictionary<string, ServerStatus> _listedServers = new Dictionary<string, ServerStatus>();

        private bool _wasDown;
        private bool _wasUp;
        private bool _wasPageVertical;
        private bool _wasPageHorizontal;

        private Modal _modal;

        void Start()
        {
            _modal = GetComponent<Modal>();
        }

        void Update()
        {
            if (!_modal.IsFocused)
                return;

            if (Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
            {
                if (Input.GetAxis(Inputs.Vertical) > 0 && !_wasUp)
                    SelectPrevious();
                else if (Input.GetAxis(Inputs.Vertical) < 0 && !_wasDown)
                    SelectNext();
            }

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && joinButton.interactable)
                Join();
            else if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Select(toggle => toggle.gameObject).Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
            else if (Input.GetButtonDown(Inputs.New))
                Host();
            else if ((Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0) && !_wasPageVertical)
                ScrollPage(Input.GetAxis(Inputs.PageVertical));
            else if ((Input.GetButtonDown(Inputs.PageHorizontal) || Input.GetAxis(Inputs.PageHorizontal) != 0) && !_wasPageHorizontal)
                ToggleConnectionSource();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Hide();

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasPageVertical = Input.GetAxis(Inputs.PageVertical) != 0;
            _wasPageHorizontal = Input.GetAxis(Inputs.PageHorizontal) != 0;
        }

        public void Show(UnityAction cancelAction)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            _discoveredServers.Clear();
            SelectedServerId = null;

            _listedServers.Clear();
            SelectedServerIp = null;

            CGSNetManager.Instance.Discovery.OnServerFound = OnDiscoveredServer;
            CGSNetManager.Instance.Discovery.StartDiscovery();

            CGSNetManager.Instance.ListServer.OnServerFound = OnListServer;
            CGSNetManager.Instance.ListServer.StartClient();

            Redisplay();
        }

        private void Redisplay()
        {
            if (!IsInternetConnectionSource)
            {
                if (SelectedServerId == null || !_discoveredServers.ContainsKey(SelectedServerId.GetValueOrDefault()))
                    joinButton.interactable = false;
                Rebuild<long, DiscoveryResponse>(_discoveredServers, SelectServer, SelectedServerId.GetValueOrDefault());
            }
            else
            {
                if (SelectedServerIp == null || !_listedServers.ContainsKey(SelectedServerIp))
                    joinButton.interactable = false;
                Rebuild<string, ServerStatus>(_listedServers, SelectServer, SelectedServerIp);
            }
        }

        private void ToggleConnectionSource()
        {
            bool isInternetConnectionSource = !IsInternetConnectionSource;
            lanToggle.isOn = !isInternetConnectionSource;
            internetToggle.isOn = isInternetConnectionSource;
        }

        public void SetLanConnectionSource(bool isLanConnectionSource)
        {
            IsInternetConnectionSource = !isLanConnectionSource;
            Redisplay();
        }

        public void SetInternetConnectionSource(bool isInternetConnectionSource)
        {
            IsInternetConnectionSource = isInternetConnectionSource;
            Redisplay();
        }

        public void OnDiscoveredServer(DiscoveryResponse info)
        {
            _discoveredServers[info.serverId] = info;
            Redisplay();
        }

        public void OnListServer(ServerStatus info)
        {
            _listedServers[info.ip] = info;
            Redisplay();
        }

        public void Host()
        {
            NetworkManager.singleton.StartHost();
            if (!IsInternetConnectionSource)
                CGSNetManager.Instance.Discovery.AdvertiseServer();
            else
                CGSNetManager.Instance.ListServer.StartGameServer();
            Hide();
        }

        public void SelectServer(Toggle toggle, long serverId)
        {
            if (toggle.isOn)
            {
                SelectedServerId = serverId;
                joinButton.interactable = true;
            }
            else if (!toggle.group.AnyTogglesOn() && serverId == SelectedServerId)
                Join();
        }

        public void SelectServer(Toggle toggle, string serverIp)
        {
            if (toggle.isOn)
            {
                SelectedServerIp = serverIp;
                joinButton.interactable = true;
            }
            else if (!toggle.group.AnyTogglesOn() && serverIp.Equals(SelectedServerId))
                Join();
        }

        public void Join()
        {
            if (!IsInternetConnectionSource)
            {
                if (SelectedServerId == null
                    || !DiscoveredServers.TryGetValue(SelectedServerId.GetValueOrDefault(), out DiscoveryResponse serverResponse)
                    || serverResponse.uri == null)
                {
                    Debug.LogError("Warning: Attempted to join a game without having selected a valid server!");
                    return;
                }
                NetworkManager.singleton.StartClient(serverResponse.uri);
            }
            else
            {
                if (SelectedServerIp == null
                    || !ListedServers.TryGetValue(SelectedServerIp, out ServerStatus serverResponse)
                    || string.IsNullOrEmpty(serverResponse.ip))
                {
                    Debug.LogError("Warning: Attempted to join a game without having selected a valid server!");
                    return;
                }
                NetworkManager.singleton.networkAddress = serverResponse.ip;
                NetworkManager.singleton.StartClient();
            }

            CGSNetManager.Instance.statusText.text = "Connecting...";

            Hide();
        }

        public void Hide()
        {
            if (!NetworkServer.active)
            {
                CGSNetManager.Instance.Discovery.StopDiscovery();
                CGSNetManager.Instance.ListServer.Stop();
            }
            gameObject.SetActive(false);
        }
    }
}
