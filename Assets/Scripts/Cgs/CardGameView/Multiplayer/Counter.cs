/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.CardGameView.Multiplayer
{
    public class Counter : CgsNetPlayable
    {
        public override string ViewValue => "Counter";
        public override string DeletePrompt => "Delete counter?";

        public Image logoImage;

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
            _colorNetworkVariable = new NetworkVariable<Vector3>();
            _colorNetworkVariable.OnValueChanged += OnChangeColor;
        }

        protected override void OnNetworkSpawnPlayable()
        {
            if (!Vector3.zero.Equals(_colorNetworkVariable.Value))
            {
                logoImage.color = new Color(_colorNetworkVariable.Value.x, _colorNetworkVariable.Value.y,
                    _colorNetworkVariable.Value.z);
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
            logoImage.color = _counterColor;
        }
    }
}
