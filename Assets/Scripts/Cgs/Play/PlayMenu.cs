/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class PlayMenu : MonoBehaviour
    {
        public GameObject panels;
        public Button fullscreenButton;
        public Button backButton;

        public PlayController controller;

#if CGS_SINGLEPLAYER
        private void Start()
        {
            fullscreenButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(false);
        }
#endif

        private void Update()
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom || !Input.anyKeyDown ||
                CardGameManager.Instance.ModalCanvas != null || controller.scoreboard.nameInputField.isFocused)
                return;

            if (InputManager.IsSort)
                ShowPlaySettingsMenu();
            if (InputManager.IsNew)
                ShowDeckMenu();
            else if (InputManager.IsLoad)
                CreateDie();
            else if (InputManager.IsFilter)
                ShowCardsMenu();
            else if (InputManager.IsSave)
                CreateToken();
        }

        public void Show()
        {
            panels.SetActive(true);
        }

        [UsedImplicitly]
        public void ShowPlaySettingsMenu()
        {
            controller.ShowPlaySettingsMenu();
        }

        [UsedImplicitly]
        public void ShowDeckMenu()
        {
            controller.ShowDeckMenu();
        }

        [UsedImplicitly]
        public void ShowCardsMenu()
        {
            controller.ShowCardsMenu();
        }

        [UsedImplicitly]
        public void CreateDie()
        {
            controller.CreateDefaultDie();
        }

        [UsedImplicitly]
        public void CreateToken()
        {
            controller.CreateDefaultToken();
        }

        [UsedImplicitly]
        public void ToggleMenu()
        {
            if (panels.gameObject.activeSelf)
                Hide();
            else
                Show();
        }

        [UsedImplicitly]
        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        public void Hide()
        {
            panels.SetActive(false);
        }
    }
}
