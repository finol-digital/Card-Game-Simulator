/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.CardGameView.Multiplayer
{
    public class Die : CgsNetPlayable
    {
        public const string DeletePrompt = "Delete die?";

        private const float RollTime = 1.0f;
        private const float RollDelay = 0.05f;

        public Text valueText;
        public CanvasGroup buttonsCanvasGroup;

        [field: SyncVar] public int Min { get; set; } = 1;

        [field: SyncVar] public int Max { get; set; } = 6;

        private int Value
        {
            get => _value;
            set
            {
                var newValue = value;
                if (newValue > Max)
                    newValue = Min;
                if (newValue < Min)
                    newValue = Max;

                if (NetworkManager.singleton.isNetworkActive)
                    CmdUpdateValue(newValue);
                else
                {
                    _value = newValue;
                    OnChangeValue(value, newValue);
                }
            }
        }

        [SyncVar(hook = nameof(OnChangeValue))]
        private int _value;

        private float _rollTime;
        private float _rollDelay;

        protected override void OnStartPlayable()
        {
            _value = Min;
            valueText.text = _value.ToString();
            if (!NetworkManager.singleton.isNetworkActive || isServer)
                _rollTime = RollTime;

            HideButtons();
        }

        private void Update()
        {
            if (_rollTime <= 0 || (NetworkManager.singleton.isNetworkActive && !isServer))
                return;

            _rollTime -= Time.deltaTime;
            _rollDelay += Time.deltaTime;
            if (_rollDelay < RollDelay)
                return;

            Value = Random.Range(Min, Max);
            _rollDelay = 0;
        }

        protected override void OnPointerDownPlayable(PointerEventData eventData)
        {
            // OnPointerDown is required for OnPointerUp to trigger
        }

        protected override void OnPointerUpPlayable(PointerEventData eventData)
        {
            ShowButtons();
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            ShowButtons();
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            HideButtons();
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            HideButtons();
        }

        [Command(requiresAuthority = false)]
        private void CmdUpdateValue(int value)
        {
            _value = value;
        }

        [PublicAPI]
        public void OnChangeValue(int oldValue, int newValue)
        {
            valueText.text = newValue.ToString();
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Value--;
        }

        [UsedImplicitly]
        public void Increment()
        {
            Value++;
        }

        [UsedImplicitly]
        public void Roll()
        {
            if (NetworkManager.singleton.isNetworkActive)
                CmdRoll();
            else
                _rollTime = RollTime;
        }

        [Command(requiresAuthority = false)]
        private void CmdRoll()
        {
            _rollTime = RollTime;
        }

        // TODO: VIEWER
        private void ShowButtons()
        {
            buttonsCanvasGroup.alpha = 1;
            buttonsCanvasGroup.interactable = true;
            buttonsCanvasGroup.blocksRaycasts = true;
        }

        // TODO: VIEWER
        private void HideButtons()
        {
            buttonsCanvasGroup.alpha = 0;
            buttonsCanvasGroup.interactable = false;
            buttonsCanvasGroup.blocksRaycasts = false;
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, Delete);
        }
    }
}
