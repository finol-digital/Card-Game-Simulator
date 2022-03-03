/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace Cgs.CardGameView.Multiplayer
{
    public class CgsNetPlayable : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnChangePosition))]
        public Vector2 position;

        private void Start()
        {
            if (Vector2.zero != position)
                ((RectTransform) transform).localPosition = position;
            OnStart();
        }

        protected virtual void OnStart()
        {
            // Child classes may override
        }

        [PublicAPI]
        public void OnChangePosition(Vector2 oldValue, Vector2 newValue)
        {
            transform.localPosition = newValue;
        }
    }
}
