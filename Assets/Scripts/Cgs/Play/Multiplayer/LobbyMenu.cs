/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cgs.Menu;
using Cgs.UI;
using JetBrains.Annotations;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public const string InvalidPasswordWarningMessage =
            "Password not applied: password length must be between 8 and 64 characters.";

        public const string InvalidServerErrorMessage =
            "Error: Attempted to join a game without having selected a valid server!";

        private const string ConnectionStartErrorMessage = "Unable to start the multiplayer connection.";
        private const string BrowserConnectionStartErrorMessage =
            "Unable to connect. For browser LAN, open this site in another tab of the same browser, host there, " +
            "then select its room or enter its room ID here.";

        private bool UsesIpAddress => _isLanConnectionSource && !BrowserLanTransport.IsBrowser;
        public string RoomIdIpLabel => "Room " + (UsesIpAddress ? "IP" : "Id") + ":";
        public string RoomIdIpPlaceholder => "Enter Room " + (UsesIpAddress ? "IP" : "Id") + "...";

        private const float SecondsPerRefresh = 5;

        [SerializeField] ToggleGroup lanToggleGroup;
        [SerializeField] Toggle lanToggle;
        [SerializeField] Toggle internetToggle;
        [SerializeField] Button joinButton;
        [SerializeField] Text roomIdIpLabel;
        [SerializeField] InputField roomIdIpInputField;
        [SerializeField] InputField passwordInputField;

        [UsedImplicitly]
        public bool IsLanConnectionSource
        {
            get => _isLanConnectionSource;
            set
            {
                _isLanConnectionSource = value;
                roomIdIpLabel.text = RoomIdIpLabel;
                ((Text)roomIdIpInputField.placeholder).text = RoomIdIpPlaceholder;
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

        private string _password = string.Empty;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private bool IsBlocked => Menu.IsBlocked || roomIdIpInputField.isFocused || passwordInputField.isFocused;

        private float _secondsSinceRefresh;

        private bool _shouldRedisplay;

        private InputAction _moveAction;
        private InputAction _pageAction;
        private InputAction _shiftAction;

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed += InputToggleConnection;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed += InputToggleConnection;
            InputSystem.actions.FindAction(Tags.SubMenuFocusPrevious).performed += InputFocusPrevious;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed += InputFocusNext;
            InputSystem.actions.FindAction(Tags.SubMenuHost).performed += InputHost;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        protected void Start()
        {
            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
            _shiftAction = InputSystem.actions.FindAction(Tags.SubMenuShift);

            roomIdIpInputField.onValidateInput += (_, _, addedChar) => Tags.FilterFocusInput(addedChar);
            passwordInputField.onValidateInput += (_, _, addedChar) => Tags.FilterFocusInput(addedChar);

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

            Refresh();
        }

        protected void Update()
        {
            if (BrowserLanTransport.IsBrowser)
            {
                if (CgsNetManager.Instance.BrowserTransport.TryReadDiscoveredRooms(out var rooms))
                {
                    DiscoveredServers.Clear();
                    foreach (var room in rooms)
                        DiscoveredServers[room.Key] = room.Value;
                    _shouldRedisplay = true;
                }
            }

            _secondsSinceRefresh += Time.deltaTime;
            if (_secondsSinceRefresh > SecondsPerRefresh)
                Refresh();

            if (_shouldRedisplay)
                Redisplay();

            // Poll for Vector2 inputs
            if (IsBlocked)
                return;

            var pageVector2 = _pageAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if ((_pageAction?.WasPressedThisFrame() ?? false) && Mathf.Abs(pageVector2.x) > 0.5f)
                ToggleConnectionSource();
            else if (Mathf.Abs(pageVector2.y) > 0)
            {
                var delta = pageVector2.y * Time.deltaTime;
                ScrollRect.verticalNormalizedPosition = Mathf.Clamp01(ScrollRect.verticalNormalizedPosition + delta);
            }

            if (!(_moveAction?.WasPressedThisFrame() ?? false))
                return;
            var moveVector2 = _moveAction.ReadValue<Vector2>();
            switch (moveVector2.y)
            {
                case > 0:
                    SelectPrevious();
                    break;
                case < 0:
                    SelectNext();
                    break;
                default:
                {
                    if (Mathf.Abs(moveVector2.x) > 0)
                        ToggleConnectionSource();
                    break;
                }
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            _selectedServer = string.Empty;
            DiscoveredServers.Clear();
            CgsNetManager.Instance.StartLanDiscovery(OnServerFound);

            Redisplay();
        }

        private void Refresh()
        {
            if (IsInternetConnectionSource && AuthenticationService.Instance.IsSignedIn)
            {
#pragma warning disable CS4014
                RefreshLobbies();
#pragma warning restore CS4014
                Debug.Log("LobbyMenu Refreshed Lobbies");
            }
            else if (IsLanConnectionSource)
            {
                CgsNetManager.Instance.RefreshLanDiscovery();
                Debug.Log("LobbyMenu Refreshed Discovery");
            }
            else
                Debug.Log("LobbyMenu Refreshed None");

            _secondsSinceRefresh = 0;
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

        private void InputToggleConnection(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            ToggleConnectionSource();
        }

        private void ToggleConnectionSource()
        {
            var isInternetConnectionSource = !IsInternetConnectionSource;
            lanToggle.isOn = !isInternetConnectionSource;
            internetToggle.isOn = isInternetConnectionSource;
        }

        private void OnServerFound(string sender, DiscoveryResponseData response)
        {
            Debug.Log($"OnServerFound {sender} {response}");
            DiscoveredServers[sender] = response;
            _shouldRedisplay = true;
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

        private void InputFocusPrevious(InputAction.CallbackContext callbackContext)
        {
            if (!Menu.IsFocused || !Menu.WasFocused)
                return;

            Menu.FocusInputField();
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

        private void InputFocusNext(InputAction.CallbackContext callbackContext)
        {
            if (!Menu.IsFocused || !Menu.WasFocused || _shiftAction?.ReadValue<float>() > 0.9f)
                return;

            Menu.FocusInputField();
        }

        [UsedImplicitly]
        public void SetPassword(string password)
        {
            _password = password ?? string.Empty;
        }

        private void InputHost(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Host();
        }

        [UsedImplicitly]
        public void Host()
        {
            if (StartHost())
                Hide();
            else
                ShowConnectionStartError();
        }

        private static void ShowConnectionStartError()
        {
            CardGameManager.Instance.Messenger.Show(BrowserLanTransport.IsBrowser
                ? BrowserConnectionStartErrorMessage
                : ConnectionStartErrorMessage);
        }

        private bool StartHost()
        {
            if (IsInternetConnectionSource)
            {
                if (CardGameManager.Current.AutoUpdateUrl == null ||
                    !CardGameManager.Current.AutoUpdateUrl.IsWellFormedOriginalString())
                    CardGameManager.Instance.Messenger.Show(ShareWarningMessage);
                if (!string.IsNullOrEmpty(_password) && _password.Length is < 8 or > 64)
                {
                    Debug.LogWarning(InvalidPasswordWarningMessage);
                    CardGameManager.Instance.Messenger.Show(InvalidPasswordWarningMessage, true);
                    _password = string.Empty;
                }

                CgsNetManager.Instance.StartBroadcastHost(_password);
            }
            else
            {
                return CgsNetManager.Instance.StartLanHost();
            }
            return true;
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (joinButton.interactable)
                Join();
        }

        [UsedImplicitly]
        public void Join()
        {
            if (IsInternetConnectionSource)
            {
                if (Lobbies.TryGetValue(_selectedServer, out var lobby))
                    CgsNetManager.Instance.StartJoinLobby(lobby.Id, _password);
                else
                {
                    if (Uri.IsWellFormedUriString(_selectedServer, UriKind.Absolute))
                        CgsNetManager.Instance.StartJoin(_selectedServer);
                    else
                        CgsNetManager.Instance.StartJoinLobbyCode(_selectedServer, _password);
                }
            }
            else
            {
                if (DiscoveredServers.TryGetValue(_selectedServer, out var discoveryResponse))
                {
                    if (!CgsNetManager.Instance.StartJoin(_selectedServer, discoveryResponse.Port))
                    {
                        ShowConnectionStartError();
                        return;
                    }
                }
                else if (Uri.IsWellFormedUriString(_selectedServer, UriKind.RelativeOrAbsolute))
                {
                    if (!CgsNetManager.Instance.StartJoin(_selectedServer))
                    {
                        ShowConnectionStartError();
                        return;
                    }
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
            if (!CgsNetManager.Instance.IsServer)
                CgsNetManager.Instance.StopLanDiscovery();

            Menu.Hide();
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Close();
        }

        [UsedImplicitly]
        public void Close()
        {
            if (!CgsNetManager.Instance.IsServer)
                CgsNetManager.Instance.StopLanDiscovery();

            SceneManager.LoadScene(Tags.MainMenuSceneIndex);
        }

        protected void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed -= InputToggleConnection;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed -= InputToggleConnection;
            InputSystem.actions.FindAction(Tags.SubMenuFocusPrevious).performed -= InputFocusPrevious;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed -= InputFocusNext;
            InputSystem.actions.FindAction(Tags.SubMenuHost).performed -= InputHost;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
