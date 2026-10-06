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
        private static bool ShouldDeal
        {
            get
            {
                switch (CardGameManager.Current.DeckSharePreference)
                {
                    case SharePreference.Ask:
                        var localPlayer = CgsNetManager.Instance.LocalPlayer;
                        return localPlayer != null && localPlayer.IsDeckShared;
                    case SharePreference.Share:
                        return true;
                    case SharePreference.Individual:
                    default:
                        return false;
                }

            }
        }

        private static string DealDraw => ShouldDeal ? "Deal" : "Draw";

        private string PromptMessage => $"{DealDraw} hand of {Count} cards?";

        [SerializeField] Text promptText;
        [SerializeField] Text countText;

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

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSub).performed += InputSub;
            InputSystem.actions.FindAction(Tags.PlayGameAdd).performed += InputAdd;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.SubMenuNo).performed += InputCancel;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        protected override void Start()
        {
            base.Start();
            Count = CardGameManager.Current.GameStartHandCount;
        }

        // Poll for Vector2 inputs
        protected void Update()
        {
            if (!(MoveAction?.WasPressedThisFrame() ?? false) && !(PageAction?.WasPressedThisFrame() ?? false))
                return;

            if (IsBlocked)
                return;

            var vector2 = MoveAction?.ReadValue<Vector2>() ?? PageAction.ReadValue<Vector2>();
            if (vector2.x < 0 || vector2.y < 0)
                Decrement();
            else if (vector2.x > 0 || vector2.y > 0)
                Increment();
        }

        private void RefreshText()
        {
            Cgs.Localization.LocalizedUiText.Set(promptText, Cgs.Localization.UiMessage.With(
                ShouldDeal ? "play.hand.deal" : "play.hand.draw", PromptMessage, ("count", Count)));
            Cgs.Localization.LocalizedUiText.Set(countText, Cgs.Localization.UiMessage.With(
                "common.count", Count.ToString(), ("count", Count)));
        }

        public void Show(UnityAction callback)
        {
            Show();
            Count = CardGameManager.Current.GameStartHandCount;
            _callback = callback;
        }

        private void InputSub(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Decrement();
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Count--;
        }

        private void InputAdd(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Increment();
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

        protected void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameSub).performed -= InputSub;
            InputSystem.actions.FindAction(Tags.PlayGameAdd).performed -= InputAdd;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.SubMenuNo).performed -= InputCancel;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
