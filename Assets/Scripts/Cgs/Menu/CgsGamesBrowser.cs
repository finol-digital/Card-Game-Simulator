/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cgs.UI;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Menu
{
    [RequireComponent(typeof(Modal))]
    public class CgsGamesBrowser : SelectionPanel
    {
        public string Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                BuildGameSelectionOptions();
            }
        }

        private string _filter = string.Empty;

        protected override bool AllowSwitchOff => false;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private readonly Dictionary<int, CgsGame> _gameOptions = new();

        private int _selectedGameId;

        private InputAction _moveAction;
        private InputAction _pageAction;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.SubMenuMenu).performed += InputMenu;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed += InputFocusNext;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed += InputLoad;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        private void Start()
        {
            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
            StartCoroutine(RunRefreshCoroutine());
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            if (Menu.IsBlocked)
                return;

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            if (Mathf.Abs(pageVertical) > 0)
            {
                var delta = pageVertical * Time.deltaTime;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta);
            }

            if (!(_moveAction?.WasPressedThisFrame() ?? false))
                return;
            var moveVertical = _moveAction.ReadValue<Vector2>().y;
            switch (moveVertical)
            {
                case > 0 when _gameOptions.Count > 0:
                    SelectPrevious();
                    break;
                case < 0 when _gameOptions.Count > 0:
                    SelectNext();
                    break;
            }
        }

        private IEnumerator RunRefreshCoroutine()
        {
            ClearPanel();

            using var request = UnityWebRequest.Get(Tags.CgsGamesBrowseApiUrl);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError(request.error);
                CardGameManager.Instance.Messenger.Show(request.error);
                Application.OpenURL(Tags.CgsGamesBrowseUrl);
                Hide();
                yield break;
            }

            var cgsGames = new List<CgsGame>();
            JsonConvert.PopulateObject(request.downloadHandler.text, cgsGames);

            for (var i = 0; i < cgsGames.Count; i++)
                _gameOptions[i] = cgsGames[i];

            BuildGameSelectionOptions();
        }

        public void Show()
        {
            Menu.Show();
            BuildGameSelectionOptions();
        }

        private void InputFocusNext(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            Menu.FocusInputField();
        }

        [UsedImplicitly]
        public void BuildGameSelectionOptions()
        {
            var filteredGameOptions = BuildFilteredGameOptions();
            if (filteredGameOptions.Count == 0)
                _selectedGameId = -1;
            else if (!filteredGameOptions.ContainsKey(_selectedGameId))
                _selectedGameId = filteredGameOptions.Keys.OrderBy(key => key).First();

            Rebuild(filteredGameOptions, SelectGame, _selectedGameId);
        }

        [UsedImplicitly]
        public void SelectGame(Toggle toggle, int gameId)
        {
            if (toggle != null && toggle.isOn)
                _selectedGameId = gameId;
        }

        private void InputMenu(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            GoToCgsGamesBrowser();
        }

        [UsedImplicitly]
        public void GoToCgsGamesBrowser()
        {
            Application.OpenURL(Tags.CgsGamesBrowseUrl);
        }

        private void InputLoad(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            if (_gameOptions.Count > 0)
                Refresh();
        }

        [UsedImplicitly]
        public void Refresh()
        {
            StopAllCoroutines();
            StartCoroutine(RunRefreshCoroutine());
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            if (_gameOptions.Count > 0)
                Import();
        }

        [UsedImplicitly]
        public void Import()
        {
            var filteredGameOptions = BuildFilteredGameOptions();
            if (filteredGameOptions.Count == 0 ||
                !filteredGameOptions.TryGetValue(_selectedGameId, out var selectedGame))
                return;

            CardGameManager.Instance.StartGetCardGame(selectedGame.AutoUpdateUrl);
            Hide();
        }

        private Dictionary<int, CgsGame> BuildFilteredGameOptions()
        {
            var filteredGameOptions = new Dictionary<int, CgsGame>();
            foreach (var gameOption in _gameOptions.Where(gameOption =>
                         string.IsNullOrEmpty(Filter) || gameOption.Value.Name.ToLower().Contains(Filter.ToLower())))
                filteredGameOptions[gameOption.Key] = gameOption.Value;

            return filteredGameOptions;
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            Hide();
        }

        [UsedImplicitly]
        public void Hide()
        {
            Menu.Hide();
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.SubMenuMenu).performed -= InputMenu;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed -= InputFocusNext;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed -= InputLoad;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
