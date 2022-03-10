/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.Play.Drawer
{
    public class DrawerHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CardDrawer cardDrawer;

        public float offsetHeight;

        private float _dragOffsetHeight;

        public void OnBeginDrag(PointerEventData eventData)
        {
            var rectTransform = (RectTransform) transform;
            _dragOffsetHeight = eventData.position.y - rectTransform.position.y + (offsetHeight +
                    cardDrawer.panelRectTransform.rect.height) *
                CardGameManager.Instance.CardCanvas.transform.localScale.y;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var targetY = eventData.position.y - _dragOffsetHeight;
            var minY = CardDrawer.HiddenPosition.y * CardGameManager.Instance.CardCanvas.transform.localScale.y;
            var y = Mathf.Clamp(targetY, minY, CardDrawer.ShownPosition.y);
            cardDrawer.panelRectTransform.position = new Vector3(cardDrawer.panelRectTransform.position.x, y);
            cardDrawer.downButton.interactable = Math.Abs(y - minY) > 0.1f;
            cardDrawer.upButton.interactable = Math.Abs(y - CardDrawer.ShownPosition.y) > 0.1f;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragOffsetHeight = 0;
        }
    }
}
