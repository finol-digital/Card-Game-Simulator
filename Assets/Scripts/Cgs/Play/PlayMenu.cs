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
        [SerializeField] GameObject panels;
        [SerializeField] Button fullscreenButton;
        [SerializeField] Button backButton;

        [SerializeField] PlayController controller;

        public GameObject Panels => panels;

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSettings).performed += InputPlaySettings;
            InputSystem.actions.FindAction(Tags.CardsFilter).performed += InputCardsFilter;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed += InputDecksLoad;
            InputSystem.actions.FindAction(Tags.PlayGameDie).performed += InputDie;
            InputSystem.actions.FindAction(Tags.PlayGameCounter).performed += InputCounter;
        }

#if CGS_SINGLEPLAYER
        protected void Start()
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
                || CardGameManager.Instance.ModalCanvas != null || controller.Scoreboard.NameInputField.isFocused)
                return;

            ShowPlaySettingsMenu();
        }

        [UsedImplicitly]
        public void ShowPlaySettingsMenu()
        {
            controller.ShowPlaySettingsMenu();
            Hide();
        }

        private void InputCardsFilter(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.Scoreboard.NameInputField.isFocused)
                return;

            ShowCardsMenu();
        }

        [UsedImplicitly]
        public void ShowCardsMenu()
        {
            controller.ShowCardsMenu();
            Hide();
        }

        private void InputDecksLoad(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.Scoreboard.NameInputField.isFocused)
                return;

            ShowDeckMenu();
        }

        [UsedImplicitly]
        public void ShowDeckMenu()
        {
            controller.ShowDeckMenu();
            Hide();
        }

        private void InputDie(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.Scoreboard.NameInputField.isFocused)
                return;

            CreateDie();
        }

        [UsedImplicitly]
        public void CreateDie()
        {
            controller.CreateDefaultDie();
            Hide();
        }

        private void InputCounter(InputAction.CallbackContext callbackContext)
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom
                || CardGameManager.Instance.ModalCanvas != null || controller.Scoreboard.NameInputField.isFocused)
                return;

            CreateCounter();
        }

        [UsedImplicitly]
        public void CreateCounter()
        {
            controller.CreateDefaultCounter();
            Hide();
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

        protected void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSettings).performed -= InputPlaySettings;
            InputSystem.actions.FindAction(Tags.CardsFilter).performed -= InputCardsFilter;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed -= InputDecksLoad;
            InputSystem.actions.FindAction(Tags.PlayGameDie).performed -= InputDie;
            InputSystem.actions.FindAction(Tags.PlayGameCounter).performed -= InputCounter;
        }
    }
}
