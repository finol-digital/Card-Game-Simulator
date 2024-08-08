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
        public override string DeletePrompt => "Delete die?";

        public const int DefaultMin = 1;
        public const int DefaultMax = 6;

        private const float RollTotalTime = 1.0f;
        private const float RollPeriodTime = 0.05f;

        public Text valueText;
        public Image dieImage;

        public int Min
        {
            get => IsSpawned ? _minNetworkVariable.Value : _min;
            set
            {
                var oldMin = _min;
                _min = value;
                if (_min > Max)
                    _min = Max;
                if (Value < _min)
                    Value = _min;
                if (IsSpawned)
                    UpdateMinServerRpc(_min);
                else
                    OnChangeMin(oldMin, _min);
            }
        }

        private int _min = DefaultMin;
        private NetworkVariable<int> _minNetworkVariable;

        public int Max
        {
            get => IsSpawned ? _maxNetworkVariable.Value : _max;
            set
            {
                var oldMax = _max;
                _max = value;
                if (_max < Min)
                    _max = _min;
                if (Value > _max)
                    Value = _max;
                if (IsSpawned)
                    UpdateMaxServerRpc(_max);
                else
                    OnChangeMax(oldMax, _max);
            }
        }

        private int _max = DefaultMax;
        private NetworkVariable<int> _maxNetworkVariable;

        public override string ViewValue => $"Value: {Value} / Max: {Max}";

        public int Value
        {
            get => IsSpawned ? _valueNetworkVariable.Value : _value;
            set
            {
                var oldValue = _value;
                var newValue = value;
                if (newValue > Max)
                    newValue = Min;
                if (newValue < Min)
                    newValue = Max;

                _value = newValue;
                if (IsSpawned)
                    UpdateValueServerRpc(newValue);
                else
                    OnChangeValue(oldValue, newValue);
            }
        }

        private int _value = DefaultMin;
        private NetworkVariable<int> _valueNetworkVariable;

        public Color DieColor
        {
            get => !IsSpawned
                ? dieImage.color
                : new Color(_colorNetworkVariable.Value.x, _colorNetworkVariable.Value.y,
                    _colorNetworkVariable.Value.z);
            set
            {
                var oldValue = new Vector3(dieImage.color.r, dieImage.color.g, dieImage.color.b);
                var newValue = new Vector3(value.r, value.g, value.b);
                if (IsSpawned)
                    UpdateColorServerRpc(newValue);
                else
                    OnChangeColor(oldValue, newValue);
            }
        }

        private NetworkVariable<Vector3> _colorNetworkVariable;

        private float _rollRemainingTime;
        private float _rollPeriodTime;

        protected override void OnAwakePlayable()
        {
            _minNetworkVariable = new NetworkVariable<int>();
            _minNetworkVariable.OnValueChanged += OnChangeMin;
            _maxNetworkVariable = new NetworkVariable<int>();
            _maxNetworkVariable.OnValueChanged += OnChangeMax;
            _valueNetworkVariable = new NetworkVariable<int>();
            _valueNetworkVariable.OnValueChanged += OnChangeValue;
            _colorNetworkVariable = new NetworkVariable<Vector3>();
            _colorNetworkVariable.OnValueChanged += OnChangeColor;
        }

        protected override void OnNetworkSpawnPlayable()
        {
            if (!Vector3.zero.Equals(_colorNetworkVariable.Value))
                dieImage.color = new Color(_colorNetworkVariable.Value.x, _colorNetworkVariable.Value.y,
                    _colorNetworkVariable.Value.z);
        }

        protected override void OnStartPlayable()
        {
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
        private void UpdateMinServerRpc(int value)
        {
            _minNetworkVariable.Value = value;
        }

        [PublicAPI]
        public void OnChangeMin(int oldValue, int newValue)
        {
            _min = newValue;
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateMaxServerRpc(int value)
        {
            _maxNetworkVariable.Value = value;
        }

        [PublicAPI]
        public void OnChangeMax(int oldValue, int newValue)
        {
            _max = newValue;
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

        [ServerRpc(RequireOwnership = false)]
        private void UpdateColorServerRpc(Vector3 value)
        {
            _colorNetworkVariable.Value = value;
        }

        [PublicAPI]
        public void OnChangeColor(Vector3 oldValue, Vector3 newValue)
        {
            dieImage.color = new Color(newValue.x, newValue.y, newValue.z);
        }

        [UsedImplicitly]
        public void Roll()
        {
            if (IsSpawned)
                RollServerRpc();
            else
                _rollRemainingTime = RollTotalTime;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RollServerRpc()
        {
            _rollRemainingTime = RollTotalTime;
        }
    }
}
