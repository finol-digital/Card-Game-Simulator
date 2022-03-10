/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView.Viewer
{
    public class StackViewerHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private float MaxY => Screen.height -
                              ((RectTransform) transform).rect.height *
                              CardGameManager.Instance.CardCanvas.transform.localScale.y;

        private float MinY => _rectTransform.rect.height * CardGameManager.Instance.CardCanvas.transform.localScale.y;

        private RectTransform _rectTransform;
        private Vector2 _dragOffset;

        private void Start()
        {
            _rectTransform = (RectTransform) transform.parent;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragOffset = eventData.position - ((Vector2) transform.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var targetPosition = eventData.position - _dragOffset;
            var targetY = Mathf.Clamp(targetPosition.y, MinY, MaxY);
            _rectTransform.position = new Vector3(_rectTransform.position.x, targetY);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragOffset = Vector2.zero;
        }
    }
}
