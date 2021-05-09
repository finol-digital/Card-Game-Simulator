/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView
{
    public class CardViewerSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
    {
        public bool ignoreDeselect;

        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CardViewer.Instance.WasVisible)
                CardViewer.Instance.IsVisible = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (ignoreDeselect || CardViewer.Instance == null)
                return;

            if (!CardViewer.Instance.Zoom)
                CardViewer.Instance.IsVisible = false;
        }
    }
}
