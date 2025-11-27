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

        private void Update()
        {
            if (!Menu.IsFocused || _gameOptions.Count < 1)
                return;

            if (InputManager.IsVertical)
            {
                if (InputManager.IsUp && !InputManager.WasUp)
                    SelectPrevious();
                else if (InputManager.IsDown && !InputManager.WasDown)
                    SelectNext();
            }

            if (InputManager.IsOption)
                GoToCgsGamesBrowser();
            else if (InputManager.IsLoad)
                Refresh();
            else if (InputManager.IsSubmit)
                Import();
            else if (InputManager.IsPageVertical && !InputManager.WasPageVertical)
                ScrollPage(InputManager.IsPageDown);
            else if (InputManager.IsCancel)
                Hide();
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

        [UsedImplicitly]
        public void SelectGame(Toggle toggle, int gameId)
        {
            if (toggle != null && toggle.isOn)
                _selectedGameId = gameId;
        }

        [UsedImplicitly]
        public void GoToCgsGamesBrowser()
        {
            Application.OpenURL(Tags.CgsGamesBrowseUrl);
        }

        [UsedImplicitly]
        public void Refresh()
        {
            StopAllCoroutines();
            StartCoroutine(RunRefreshCoroutine());
        }

        [UsedImplicitly]
        public void Import()
        {
            CardGameManager.Instance.StartGetCardGame(_gameOptions[_selectedGameId].AutoUpdateUrl);
            Hide();
        }

        [UsedImplicitly]
        public void Hide()
        {
            Menu.Hide();
        }
    }
}
