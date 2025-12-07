/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class PlayMenu : MonoBehaviour
    {
        public GameObject panels;
        public Button fullscreenButton;
        public Button backButton;

        public PlayController controller;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSettings).performed += InputPlaySettings;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed += InputDecksLoad;
            InputSystem.actions.FindAction(Tags.CardsFilter).performed += InputCardsFilter;
            InputSystem.actions.FindAction(Tags.PlayGameDie).performed += InputDie;
            InputSystem.actions.FindAction(Tags.PlayGameToken).performed += InputToken;
        }

#if CGS_SINGLEPLAYER
        private void Start()
        {
            fullscreenButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(false);
        }
#endif

        public void Show()
        {
            panels.SetActive(true);
        }

        private void InputPlaySettings(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.scoreboard.nameInputField.isFocused)
                return;

            ShowPlaySettingsMenu();
        }

        [UsedImplicitly]
        public void ShowPlaySettingsMenu()
        {
            controller.ShowPlaySettingsMenu();
        }

        private void InputDecksLoad(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.scoreboard.nameInputField.isFocused)
                return;

            ShowDeckMenu();
        }

        [UsedImplicitly]
        public void ShowDeckMenu()
        {
            controller.ShowDeckMenu();
        }

        private void InputCardsFilter(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.scoreboard.nameInputField.isFocused)
                return;

            ShowCardsMenu();
        }

        [UsedImplicitly]
        public void ShowCardsMenu()
        {
            controller.ShowCardsMenu();
        }

        private void InputDie(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.scoreboard.nameInputField.isFocused)
                return;

            CreateDie();
        }

        [UsedImplicitly]
        public void CreateDie()
        {
            controller.CreateDefaultDie();
        }

        private void InputToken(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.scoreboard.nameInputField.isFocused)
                return;

            CreateToken();
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

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSettings).performed -= InputPlaySettings;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed -= InputDecksLoad;
            InputSystem.actions.FindAction(Tags.CardsFilter).performed -= InputCardsFilter;
            InputSystem.actions.FindAction(Tags.PlayGameDie).performed -= InputDie;
            InputSystem.actions.FindAction(Tags.PlayGameToken).performed -= InputToken;
        }
    }
}
