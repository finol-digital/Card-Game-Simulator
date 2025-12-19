/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;
using Cgs.Play.Multiplayer;
using FinolDigital.Cgs.Json;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Play.Drawer
{
    public class HandDealer : Modal
    {
        private static string DealDraw
        {
            get
            {
                string result;
                switch (CardGameManager.Current.DeckSharePreference)
                {
                    case SharePreference.Ask:
                        var localPlayer = CgsNetManager.Instance.LocalPlayer;
                        result = localPlayer != null && localPlayer.IsDeckShared ? "Deal" : "Draw";
                        break;
                    case SharePreference.Share:
                        result = "Deal";
                        break;
                    case SharePreference.Individual:
                    default:
                        result = "Draw";
                        break;
                }

                return result;
            }
        }

        private string PromptMessage => $"{DealDraw} hand of {Count} cards?";

        public Text promptText;
        public Text countText;

        public int Count
        {
            get => _count;
            private set
            {
                _count = value;
                RefreshText();
            }
        }

        private int _count;

        private UnityAction _callback;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayerMove).performed += InputMove;
            InputSystem.actions.FindAction(Tags.PlayerPage).performed += InputMove;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
            InputSystem.actions.FindAction(Tags.SubMenuFocusPrevious).performed += InputCancel;
        }

        protected override void Start()
        {
            base.Start();
            Count = CardGameManager.Current.GameStartHandCount;
        }

        private void InputMove(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            var moveVector = InputSystem.actions.FindAction(Tags.PlayerMove).ReadValue<Vector2>();
            if (moveVector.x < 0 || moveVector.y < 0)
                Decrement();
            else if (moveVector.x > 0 || moveVector.y > 0)
                Increment();
        }

        private void RefreshText()
        {
            promptText.text = PromptMessage;
            countText.text = Count.ToString();
        }

        public void Show(UnityAction callback)
        {
            Show();
            Count = CardGameManager.Current.GameStartHandCount;
            _callback = callback;
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Count--;
        }

        [UsedImplicitly]
        public void Increment()
        {
            Count++;
        }

        private void InputSubmit(InputAction.CallbackContext obj)
        {
            if (IsBlocked)
                return;

            Confirm();
        }

        [UsedImplicitly]
        public void Confirm()
        {
            _callback?.Invoke();
            Hide();
        }

        private void InputCancel(InputAction.CallbackContext obj)
        {
            if (IsBlocked)
                return;

            Hide();
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayerMove).performed -= InputMove;
            InputSystem.actions.FindAction(Tags.PlayerPage).performed -= InputMove;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
            InputSystem.actions.FindAction(Tags.SubMenuFocusPrevious).performed -= InputCancel;
        }
    }
}
