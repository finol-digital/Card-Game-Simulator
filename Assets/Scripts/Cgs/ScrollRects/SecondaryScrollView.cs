/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.ScrollRects
{
    public class SecondaryScrollView : ScrollRect
    {
        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle ||
                eventData.button == PointerEventData.InputButton.Right)
            {
                content.localPosition += new Vector3(eventData.delta.x * 2, eventData.delta.y);
                normalizedPosition = new Vector2(Mathf.Clamp(normalizedPosition.x, 0.0f, 1.0f),
                    Mathf.Clamp(normalizedPosition.y, 0.0f, 1.0f));
            }
            else
                base.OnDrag(eventData);
        }
    }
}
