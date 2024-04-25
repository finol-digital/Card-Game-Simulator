/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.UI.ScrollRects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
{
    public delegate void OnAddCardDelegate(CardZone cardZone, CardModel cardModel);

    public delegate void OnRemoveCardDelegate(CardZone cardZone, CardModel cardModel);

    public enum CardZoneType
    {
        Vertical,
        Horizontal,
        Area
    }

    public class CardZone : CgsNetPlayable
    {
        public CardZoneType type;
        public bool allowsFlip;
        public bool allowsRotation;
        public ScrollRect scrollRectContainer;

        public bool DoesImmediatelyRelease { get; set; }

        public UnityAction OnLayout { get; set; }

        public List<OnAddCardDelegate> OnAddCardActions { get; } = new();
        public List<OnRemoveCardDelegate> OnRemoveCardActions { get; } = new();

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            var cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && (type != CardZoneType.Area || cardModel.transform.parent != transform) &&
                !cardModel.IsStatic)
                cardModel.PlaceHolderCardZone = this;

            if (type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnPointerEnter(eventData);
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            var cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && cardModel.PlaceHolderCardZone == this)
                cardModel.PlaceHolderCardZone = null;

            OnLayout?.Invoke();

            if (type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnPointerExit(eventData);
        }

        protected override void OnPointerDownPlayable(PointerEventData eventData)
        {
            // Nothing
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            // Nothing
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            // Nothing
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            // Nothing
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnBeginDrag(eventData);
            else if (scrollRectContainer != null)
                scrollRectContainer.OnBeginDrag(eventData);
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            if (type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnDrag(eventData);
            else if (scrollRectContainer != null)
                scrollRectContainer.OnDrag(eventData);
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnEndDrag(eventData);
            else if (scrollRectContainer != null)
                scrollRectContainer.OnEndDrag(eventData);
        }

        protected override void PostDragPlayable(PointerEventData eventData)
        {
            // Nothing
        }

        protected override void UpdatePosition()
        {
            // Nothing
        }

        public void OnAdd(CardModel cardModel)
        {
            if (cardModel == null)
                return;

            if (type == CardZoneType.Area)
                cardModel.SnapToGrid();

            if (cardModel.ToDiscard)
                return;

            foreach (var onAddCardDelegate in OnAddCardActions)
                onAddCardDelegate(this, cardModel);
        }

        public void OnRemove(CardModel cardModel)
        {
            if (cardModel == null)
                return;

            foreach (var onRemoveCardDelegate in OnRemoveCardActions)
                onRemoveCardDelegate(this, cardModel);
        }

        public void UpdateLayout(RectTransform child, Vector2 targetPosition)
        {
            if (child == null)
                return;

            switch (type)
            {
                case CardZoneType.Vertical:
                case CardZoneType.Horizontal:
                    var newSiblingIndex = transform.childCount;
                    for (var i = 0; i < transform.childCount; i++)
                    {
                        if (type == CardZoneType.Vertical
                                ? targetPosition.y < transform.GetChild(i).position.y
                                : targetPosition.x > transform.GetChild(i).position.x)
                            continue;
                        newSiblingIndex = i;
                        if (child.GetSiblingIndex() < newSiblingIndex)
                            newSiblingIndex--;
                        break;
                    }

                    child.SetSiblingIndex(newSiblingIndex);
                    break;
                case CardZoneType.Area:
                default:
                    child.position = targetPosition;
                    break;
            }

            OnLayout?.Invoke();
        }

        public void UpdateScrollRect(DragPhase dragPhase, PointerEventData eventData)
        {
            if (scrollRectContainer == null)
                return;

            switch (dragPhase)
            {
                case DragPhase.Begin:
                    scrollRectContainer.OnBeginDrag(eventData);
                    break;
                case DragPhase.Drag:
                    scrollRectContainer.OnDrag(eventData);
                    break;
                case DragPhase.End:
                default:
                    scrollRectContainer.OnEndDrag(eventData);
                    break;
            }
        }

        public void Clear()
        {
            transform.DestroyAllChildren();
        }
    }
}
