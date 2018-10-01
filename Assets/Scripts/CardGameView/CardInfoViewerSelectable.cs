/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGameView
{
    public class CardInfoViewerSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
    {
        public bool ignoreDeselect = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CardInfoViewer.Instance.WasVisible)
                CardInfoViewer.Instance.IsVisible = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (ignoreDeselect || CardInfoViewer.Instance == null)
                return;

            if (!CardInfoViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardInfoViewer.Instance.IsVisible = false;
        }
    }
}
