/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
{
    public class Counter : CgsNetPlayable, ICounterDropHandler
    {
        public override string DeletePrompt => "Delete counter?";

        public const int DefaultValue = 1;

        public Image counterImage;
        public Transform logoTransform;
        public Text valueText;

        public override string ViewValue => $"Counter: {Value}";

        public int Value
        {
            get => IsSpawned ? _valueNetworkVariable.Value : _value;
            set
            {
                var oldValue = _value;
                _value = value;
                if (IsSpawned)
                    UpdateValueServerRpc(_value);
                else
                    OnChangeValue(oldValue, _value);
            }
        }

        private int _value = DefaultValue;
        private NetworkVariable<int> _valueNetworkVariable;

        public Color CounterColor
        {
            get => _counterColor;
            set
            {
                var oldValue = new Vector3(_counterColor.r, _counterColor.g, _counterColor.b);
                _counterColor = value;
                var newValue = new Vector3(_counterColor.r, _counterColor.g, _counterColor.b);
                if (IsSpawned)
                    UpdateColorServerRpc(newValue);
                else
                    OnChangeColor(oldValue, newValue);
            }
        }

        private Color _counterColor = Color.white;
        private NetworkVariable<Vector3> _colorNetworkVariable;

        protected override void OnAwakePlayable()
        {
            _valueNetworkVariable = new NetworkVariable<int>();
            _valueNetworkVariable.OnValueChanged += OnChangeValue;
            _colorNetworkVariable = new NetworkVariable<Vector3>();
            _colorNetworkVariable.OnValueChanged += OnChangeColor;
        }

        protected override void OnNetworkSpawnPlayable()
        {
            if (!Vector3.zero.Equals(_colorNetworkVariable.Value))
                counterImage.color = new Color(_colorNetworkVariable.Value.x, _colorNetworkVariable.Value.y,
                    _colorNetworkVariable.Value.z);
        }

        protected override void OnStartPlayable()
        {
            gameObject.GetOrAddComponent<CounterDropArea>().DropHandler = this;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void UpdateValueServerRpc(int value)
        {
            _valueNetworkVariable.Value = value;
        }

        [PublicAPI]
        public void OnChangeValue(int oldValue, int newValue)
        {
            _value = newValue;
            if (newValue == DefaultValue)
            {
                logoTransform.gameObject.SetActive(true);
                valueText.gameObject.SetActive(false);
            }
            else
            {
                logoTransform.gameObject.SetActive(false);
                valueText.gameObject.SetActive(true);
                valueText.text = newValue.ToString();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void UpdateColorServerRpc(Vector3 value)
        {
            _colorNetworkVariable.Value = value;
        }

        [PublicAPI]
        public void OnChangeColor(Vector3 oldValue, Vector3 newValue)
        {
            _counterColor = new Color(newValue.x, newValue.y, newValue.z);
            counterImage.color = _counterColor;
        }

        public static Counter GetPointerDrag(PointerEventData eventData)
        {
            return eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<Counter>();
        }

        public void OnDrop(Counter counter)
        {
            counter.Value += Value;
            RequestDelete();
        }
    }
}
