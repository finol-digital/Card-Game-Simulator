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
        public Button joinButton;

        public long? SelectedServerId { get; private set; } = null;
        public IReadOnlyDictionary<long, DiscoveryResponse> DiscoveredServers => _discoveredServers;
        private readonly Dictionary<long, DiscoveryResponse> _discoveredServers = new Dictionary<long, DiscoveryResponse>();

        private bool _wasDown;
        private bool _wasUp;
        private bool _wasPage;

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
            else if ((Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0) && !_wasPage)
                ScrollPage(Input.GetAxis(Inputs.PageVertical));
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Hide();

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasPage = Input.GetAxis(Inputs.PageVertical) != 0;
        }

        public void Show(UnityAction cancelAction)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            CGSNetManager.Instance.Discovery.OnServerFound = OnDiscoveredServer;
            CGSNetManager.Instance.Discovery.StartDiscovery();

            _discoveredServers.Clear();
            SelectedServerId = null;
            Redisplay();
        }

        public void OnDiscoveredServer(DiscoveryResponse info)
        {
            _discoveredServers[info.serverId] = info;
            Redisplay();
        }

        private void Redisplay()
        {
            if (SelectedServerId == null || !_discoveredServers.ContainsKey(SelectedServerId.GetValueOrDefault()))
                joinButton.interactable = false;
            Rebuild<long, DiscoveryResponse>(_discoveredServers, SelectServer, SelectedServerId.GetValueOrDefault());
        }

        public void Host()
        {
            NetworkManager.singleton.StartHost();
            CGSNetManager.Instance.Discovery.AdvertiseServer();
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

        public void Join()
        {
            if (SelectedServerId == null
                || !DiscoveredServers.TryGetValue(SelectedServerId.GetValueOrDefault(), out DiscoveryResponse serverResponse) || serverResponse.uri == null)
            {
                Debug.LogWarning("Warning: Attempted to join a game without having selected a valid server! Ignoring...");
                return;
            }
            NetworkManager.singleton.StartClient(serverResponse.uri);
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
