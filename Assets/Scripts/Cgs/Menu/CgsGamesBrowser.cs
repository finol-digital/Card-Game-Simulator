/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
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
        protected override bool AllowSwitchOff => false;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private readonly Dictionary<int, CgsGame> _gameOptions = new();

        private int _selectedGameId;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayerMove).performed += InputMove;
            InputSystem.actions.FindAction(Tags.PlayerPage).performed += InputPage;
            InputSystem.actions.FindAction(Tags.SubMenuMenu).performed += InputMenu;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed += InputLoad;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        private void Start()
        {
            StartCoroutine(RunRefreshCoroutine());
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

        private void BuildGameSelectionOptions()
        {
            Rebuild(_gameOptions, SelectGame, _selectedGameId);
        }

        private void InputMove(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            var moveVertical = InputSystem.actions.FindAction(Tags.PlayerMove).ReadValue<Vector2>().y;
            if (moveVertical > 0 && _gameOptions.Count > 0)
                SelectPrevious();
            else if (moveVertical < 0 && _gameOptions.Count > 0)
                SelectNext();
        }

        private void InputPage(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            var pageVertical = InputSystem.actions.FindAction(Tags.PlayerPage).ReadValue<Vector2>().y;
            if (Mathf.Abs(pageVertical) > 0)
                ScrollPage(pageVertical < 0);
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
            CardGameManager.Instance.StartGetCardGame(_gameOptions[_selectedGameId].AutoUpdateUrl);
            Hide();
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
            InputSystem.actions.FindAction(Tags.PlayerMove).performed -= InputMove;
            InputSystem.actions.FindAction(Tags.PlayerPage).performed -= InputPage;
            InputSystem.actions.FindAction(Tags.SubMenuMenu).performed -= InputMenu;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed -= InputLoad;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
