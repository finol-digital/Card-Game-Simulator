/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Play;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.CardGameView.Multiplayer
{
    public class Die : CgsNetPlayable
    {
        public const string DeletePrompt = "Delete die?";

        public const int DefaultMin = 1;
        public const int DefaultMax = 6;

        private const float RollTotalTime = 1.0f;
        private const float RollPeriodTime = 0.05f;

        public Text valueText;

        public int Min
        {
            get => IsOnline ? _minNetworkVariable.Value : _min;
            set
            {
                _min = value;
                if (IsOnline)
                    _minNetworkVariable.Value = value;
            }
        }

        private int _min = DefaultMin;
        private readonly NetworkVariable<int> _minNetworkVariable = new();

        public int Max
        {
            get => IsOnline ? _maxNetworkVariable.Value : _max;
            set
            {
                _max = value;
                if (IsOnline)
                    _maxNetworkVariable.Value = value;
            }
        }

        private int _max = DefaultMax;
        private readonly NetworkVariable<int> _maxNetworkVariable = new();

        public override string ViewValue => $"Value: {Value}";

        private int Value
        {
            get => IsOnline ? _valueNetworkVariable.Value : _value;
            set
            {
                var oldValue = _value;
                var newValue = value;
                if (newValue > Max)
                    newValue = Min;
                if (newValue < Min)
                    newValue = Max;

                _value = newValue;
                if (IsOnline)
                    UpdateValueServerRpc(newValue);
                else
                    OnChangeValue(oldValue, newValue);
            }
        }

        private int _value;
        private readonly NetworkVariable<int> _valueNetworkVariable = new();

        private float _rollRemainingTime;
        private float _rollPeriodTime;

        protected override void OnAwakePlayable()
        {
            _valueNetworkVariable.OnValueChanged += OnChangeValue;
        }

        protected override void OnStartPlayable()
        {
            ParentToPlayMat();
            transform.localPosition = Position;

            if (!NetworkManager.Singleton.IsConnectedClient || IsServer)
                _rollRemainingTime = RollTotalTime;
        }

        protected override void OnUpdatePlayable()
        {
            if ((NetworkManager.Singleton.IsConnectedClient && !IsServer) || _rollRemainingTime <= 0)
                return;

            _rollRemainingTime -= Time.deltaTime;
            _rollPeriodTime += Time.deltaTime;
            if (_rollPeriodTime < RollPeriodTime)
                return;

            Value = Random.Range(Min, Max + 1);
            _rollPeriodTime = 0;
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            if (CurrentPointerEventData == null || CurrentPointerEventData.pointerId != eventData.pointerId ||
                eventData.dragging ||
                eventData.button is PointerEventData.InputButton.Middle or PointerEventData.InputButton.Right)
                return;

            if (PlaySettings.DoubleClickToRollDice && EventSystem.current.currentSelectedGameObject == gameObject)
                Roll();
            else if (!EventSystem.current.alreadySelecting &&
                     EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateValueServerRpc(int value)
        {
            _valueNetworkVariable.Value = value;
        }

        [PublicAPI]
        public void OnChangeValue(int oldValue, int newValue)
        {
            _value = newValue;
            valueText.text = newValue.ToString();
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Value -= 1;
        }

        [UsedImplicitly]
        public void Increment()
        {
            Value += 1;
        }

        [UsedImplicitly]
        public void Roll()
        {
            if (IsOnline)
                RollServerRpc();
            else
                _rollRemainingTime = RollTotalTime;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RollServerRpc()
        {
            _rollRemainingTime = RollTotalTime;
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, RequestDelete);
        }
    }
}
