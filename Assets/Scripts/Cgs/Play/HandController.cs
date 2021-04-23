/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
        private Vector2 _dragOffset;

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
            _dragOffset = eventData.position - ((Vector2) transform.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 targetPosition = eventData.position - _dragOffset;
            float y = Mathf.Clamp(targetPosition.y, HiddenPosition.y, ShownPosition.y);
            _rectTransform.position = new Vector3(_rectTransform.position.x, y);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragOffset = Vector2.zero;
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
