/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Cgs.Menu;
using JetBrains.Annotations;
using Mirror;
using ScrollRects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(Modal))]
    public class LobbyMenu : SelectionPanel
    {
        public Toggle lanToggle;
        public Toggle internetToggle;
        public Button joinButton;

        [UsedImplicitly]
        public bool IsLanConnectionSource
        {
            get => !IsInternetConnectionSource;
            set => IsInternetConnectionSource = !value;
        }

        public bool IsInternetConnectionSource { get; set; }

        public long? SelectedServerId { get; private set; }
        public IReadOnlyDictionary<long, DiscoveryResponse> DiscoveredServers => _discoveredServers;

        private readonly Dictionary<long, DiscoveryResponse> _discoveredServers =
            new Dictionary<long, DiscoveryResponse>();

        public string SelectedServerIp { get; private set; }
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

            if (Input.GetButtonDown(Inputs.Vertical) || Math.Abs(Input.GetAxis(Inputs.Vertical)) > Inputs.Tolerance)
            {
                if (Input.GetAxis(Inputs.Vertical) > 0 && !_wasUp)
                    SelectPrevious();
                else if (Input.GetAxis(Inputs.Vertical) < 0 && !_wasDown)
                    SelectNext();
            }

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) &&
                joinButton.interactable)
                Join();
            else if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Any(toggle =>
                toggle.gameObject == EventSystem.current.currentSelectedGameObject))
                Toggles.First(toggle => toggle.gameObject == EventSystem.current.currentSelectedGameObject).isOn = true;
            else if (Input.GetButtonDown(Inputs.New))
                Host();
            else if ((Input.GetButtonDown(Inputs.PageVertical) ||
                      Math.Abs(Input.GetAxis(Inputs.PageVertical)) > Inputs.Tolerance) && !_wasPageVertical)
                ScrollPage(Input.GetAxis(Inputs.PageVertical));
            else if ((Input.GetButtonDown(Inputs.PageHorizontal) ||
                      Math.Abs(Input.GetAxis(Inputs.PageHorizontal)) > Inputs.Tolerance) && !_wasPageHorizontal)
                ToggleConnectionSource();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Hide();

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasPageVertical = Math.Abs(Input.GetAxis(Inputs.PageVertical)) > Inputs.Tolerance;
            _wasPageHorizontal = Math.Abs(Input.GetAxis(Inputs.PageHorizontal)) > Inputs.Tolerance;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            _discoveredServers.Clear();
            SelectedServerId = null;

            _listedServers.Clear();
            SelectedServerIp = null;

            CgsNetManager.Instance.Discovery.OnServerFound = OnDiscoveredServer;
            CgsNetManager.Instance.Discovery.StartDiscovery();

            CgsNetManager.Instance.ListServer.OnServerFound = OnListServer;
            CgsNetManager.Instance.ListServer.StartClient();

            Redisplay();
        }

        public void Redisplay()
        {
            if (!IsInternetConnectionSource)
            {
                if (SelectedServerId == null || !_discoveredServers.ContainsKey(SelectedServerId.GetValueOrDefault()))
                    joinButton.interactable = false;
                Rebuild(_discoveredServers, SelectServer, SelectedServerId.GetValueOrDefault());
            }
            else
            {
                if (SelectedServerIp == null || !_listedServers.ContainsKey(SelectedServerIp))
                    joinButton.interactable = false;
                Rebuild(_listedServers, SelectServer, SelectedServerIp);
            }
        }

        public void ToggleConnectionSource()
        {
            bool isInternetConnectionSource = !IsInternetConnectionSource;
            lanToggle.isOn = !isInternetConnectionSource;
            internetToggle.isOn = isInternetConnectionSource;
        }

        public void OnDiscoveredServer(DiscoveryResponse info)
        {
            _discoveredServers[info.ServerId] = info;
            Redisplay();
        }

        public void OnListServer(ServerStatus info)
        {
            _listedServers[info.Ip] = info;
            Redisplay();
        }

        public void Host()
        {
            NetworkManager.singleton.StartHost();
            CgsNetManager.Instance.Discovery.AdvertiseServer();
            if (IsInternetConnectionSource)
            {
                CgsNetManager.Instance.ListServer.StartGameServer();
                CgsNetManager.Instance.CheckForPortForwarding();
            }

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
            else if (!toggle.group.AnyTogglesOn() && serverIp.Equals(SelectedServerIp))
                Join();
        }

        public void Join()
        {
            if (!IsInternetConnectionSource)
            {
                if (SelectedServerId == null
                    || !DiscoveredServers.TryGetValue(SelectedServerId.GetValueOrDefault(),
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
                if (SelectedServerIp == null
                    || !ListedServers.TryGetValue(SelectedServerIp, out ServerStatus serverResponse)
                    || string.IsNullOrEmpty(serverResponse.Ip))
                {
                    Debug.LogError("Warning: Attempted to join a game without having selected a valid server!");
                    return;
                }

                NetworkManager.singleton.networkAddress = serverResponse.Ip;
                NetworkManager.singleton.StartClient();
            }

            CgsNetManager.Instance.statusText.text = "Connecting...";

            Hide();
        }

        public void Hide()
        {
            if (!NetworkServer.active)
            {
                CgsNetManager.Instance.Discovery.StopDiscovery();
                CgsNetManager.Instance.ListServer.Stop();
            }

            gameObject.SetActive(false);
        }
    }
}
