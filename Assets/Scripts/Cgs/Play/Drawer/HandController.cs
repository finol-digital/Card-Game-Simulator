/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using CardGameDef.Unity;
using Cgs.CardGameView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class HandController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Vector2 ShownPosition = Vector2.zero;

        private static Vector2 HiddenPosition =>
            new Vector2(0, -(CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y) - 10);

        public StackViewer handViewer;
        public Button downButton;
        public Button upButton;

        private RectTransform _rectTransform;
        private float _dragOffsetHeight;

        private void Start()
        {
            _rectTransform = (RectTransform) transform.parent;
            handViewer.Resize();
        }

        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;

            if (!Inputs.IsSort)
                return;

            if (upButton.interactable)
                Show();
            else
                Hide();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var rectTransform = (RectTransform) transform;
            _dragOffsetHeight = eventData.position.y - rectTransform.position.y + _rectTransform.rect.height *
                CardGameManager.Instance.CardCanvas.transform.localScale.y;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float targetY = eventData.position.y - _dragOffsetHeight;
            float minY = HiddenPosition.y * CardGameManager.Instance.CardCanvas.transform.localScale.y;
            float y = Mathf.Clamp(targetY, minY, ShownPosition.y);
            _rectTransform.position = new Vector3(_rectTransform.position.x, y);
            downButton.interactable = Math.Abs(y - minY) > 0.1f;
            upButton.interactable = Math.Abs(y - ShownPosition.y) > 0.1f;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragOffsetHeight = 0;
        }

        public void Show()
        {
            _rectTransform.anchoredPosition = ShownPosition;
            downButton.interactable = true;
            upButton.interactable = false;
        }

        public void AddCard(UnityCard card)
        {
            handViewer.AddCard(card);
        }

        public void Clear()
        {
            handViewer.Clear();
        }

        public void Hide()
        {
            _rectTransform.anchoredPosition = HiddenPosition;
            downButton.interactable = false;
            upButton.interactable = true;
        }
    }
}
