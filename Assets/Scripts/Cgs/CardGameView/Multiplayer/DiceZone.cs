/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Unity.Netcode;
using UnityEngine;

namespace Cgs.CardGameView.Multiplayer
{
    public class DiceZone : CgsNetPlayable
    {
        public Vector2 Size
        {
            get => IsSpawned ? _sizeNetworkVariable.Value : _size;
            set
            {
                _size = value;
                ((RectTransform)transform).sizeDelta = _size;
                if (IsSpawned)
                    _sizeNetworkVariable.Value = _size;
            }
        }

        private Vector2 _size = Vector2.zero;
        private NetworkVariable<Vector2> _sizeNetworkVariable;

        protected override void OnAwakePlayable()
        {
            _sizeNetworkVariable = new NetworkVariable<Vector2>();
        }

        protected override void OnNetworkSpawnPlayable()
        {
            if (Vector2.zero.Equals(_size))
                _size = _sizeNetworkVariable.Value;
        }

        protected override void OnStartPlayable()
        {
            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            if (!Vector2.zero.Equals(Position))
                rectTransform.localPosition = Position;
            if (!Vector2.zero.Equals(Size))
                rectTransform.sizeDelta = Size;
        }
    }
}
