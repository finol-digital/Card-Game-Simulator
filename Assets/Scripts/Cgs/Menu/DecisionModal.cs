/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class DecisionModal : Modal
    {
        public Text label;

        public Button button1;
        public Text text1;

        public Button button2;
        public Text text2;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed += InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed += InputSelectNext;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            if (!(MoveAction?.WasPressedThisFrame() ?? false))
                return;

            if (IsBlocked)
                return;

            if (EventSystem.current.currentSelectedGameObject != button1.gameObject
                && EventSystem.current.currentSelectedGameObject != button2.gameObject
                && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(button1.gameObject);
        }

        public void Show(string prompt, Tuple<string, UnityAction> option1, Tuple<string, UnityAction> option2)
        {
            base.Show();

            label.text = prompt;

            var (button1Text, button1Action) = option1;
            button1.onClick.RemoveAllListeners();
            button1.onClick.AddListener(button1Action);
            button1.onClick.AddListener(Hide);
            text1.text = button1Text;

            var (button2Text, button2Action) = option2;
            button2.onClick.RemoveAllListeners();
            button2.onClick.AddListener(button2Action);
            button2.onClick.AddListener(Hide);
            text2.text = button2Text;
        }

        private void InputSelectPrevious(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            button1.onClick.Invoke();
        }

        private void InputSelectNext(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            button2.onClick.Invoke();
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Hide();
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed -= InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed -= InputSelectNext;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
