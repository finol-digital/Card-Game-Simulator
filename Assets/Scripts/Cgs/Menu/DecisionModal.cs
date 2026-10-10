/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class DecisionModal : Modal
    {
        [SerializeField] Text label;

        [SerializeField] Button button1;
        [SerializeField] Text text1;

        [SerializeField] Button button2;
        [SerializeField] Text text2;

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed += InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed += InputSelectNext;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        // Poll for Vector2 inputs
        protected void Update()
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

            LocalizedUiText.SetLiteral(label, prompt);

            var (button1Text, button1Action) = option1;
            button1.onClick.RemoveAllListeners();
            button1.onClick.AddListener(button1Action);
            button1.onClick.AddListener(Hide);
            LocalizedUiText.SetLiteral(text1, button1Text);

            var (button2Text, button2Action) = option2;
            button2.onClick.RemoveAllListeners();
            button2.onClick.AddListener(button2Action);
            button2.onClick.AddListener(Hide);
            LocalizedUiText.SetLiteral(text2, button2Text);
        }

        public void Show(UiMessage prompt, Tuple<UiMessage, UnityAction> option1, Tuple<UiMessage, UnityAction> option2)
        {
            Show(prompt.English, Tuple.Create(option1.Item1.English, option1.Item2), Tuple.Create(option2.Item1.English, option2.Item2));
            LocalizedUiText.Set(label, prompt);
            LocalizedUiText.Set(text1, option1.Item1);
            LocalizedUiText.Set(text2, option2.Item1);
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

        protected void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed -= InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed -= InputSelectNext;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
