/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.CardGameView.Multiplayer
{
    public class Token : CgsNetPlayable
    {
        public override string ViewValue => "Token";
        public override string DeletePrompt => "Delete token?";
        
        public Image logoImage;

        public Color LogoColor
        {
            get => !IsSpawned
                ? logoImage.color
                : new Color(_colorNetworkVariable.Value.x, _colorNetworkVariable.Value.y,
                    _colorNetworkVariable.Value.z);
            set
            {
                var oldValue = new Vector3(logoImage.color.r, logoImage.color.g, logoImage.color.b);
                var newValue = new Vector3(value.r, value.g, value.b);
                if (IsSpawned)
                    UpdateColorServerRpc(newValue);
                else
                    OnChangeColor(oldValue, newValue);
            }
        }

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

        [ServerRpc(RequireOwnership = false)]
        private void UpdateColorServerRpc(Vector3 value)
        {
            _colorNetworkVariable.Value = value;
        }

        [PublicAPI]
        public void OnChangeColor(Vector3 oldValue, Vector3 newValue)
        {
            logoImage.color = new Color(newValue.x, newValue.y, newValue.z);
        }
    }
}
