/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView.Viewer
{
    public class ViewerSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
    {
        public bool ignoreDeselect;

        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && CardViewer.Instance.WasVisible)
                CardViewer.Instance.IsVisible = true;

            if (PlayableViewer.Instance != null && PlayableViewer.Instance.WasVisible)
                PlayableViewer.Instance.IsVisible = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (ignoreDeselect)
                return;

            if (CardViewer.Instance != null && !CardViewer.Instance.Zoom)
                CardViewer.Instance.IsVisible = false;

            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.IsVisible = false;
        }
    }
}
