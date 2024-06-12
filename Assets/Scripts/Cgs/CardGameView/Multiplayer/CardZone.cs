/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Viewer;
using Cgs.Play;
using Cgs.UI.ScrollRects;
using FinolDigital.Cgs.CardGameDef;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;
using CardAction = FinolDigital.Cgs.CardGameDef.CardAction;

namespace Cgs.CardGameView.Multiplayer
{
    public delegate void OnAddCardDelegate(CardZone cardZone, CardModel cardModel);

    public delegate void OnRemoveCardDelegate(CardZone cardZone, CardModel cardModel);

    public enum CardZoneType
    {
        Area = 0,
        Horizontal = 1,
        Vertical = 2
    }

    public class CardZone : CgsNetPlayable
    {
        public CardZoneType type;
        public bool allowsFlip;
        public bool allowsRotation;
        public ScrollRect scrollRectContainer;

        public CardZoneType Type
        {
            get => IsOnline ? (CardZoneType) _typeNetworkVariable.Value : type;
            set
            {
                type = value;
                if (IsOnline)
                    _typeNetworkVariable.Value = (int) value;
            }
        }

        private readonly NetworkVariable<int> _typeNetworkVariable = new();

        public Vector2 Size
        {
            get => IsOnline ? _sizeNetworkVariable.Value : ((RectTransform) transform).sizeDelta;
            set
            {
                ((RectTransform) transform).sizeDelta = value;
                if (IsOnline)
                    _sizeNetworkVariable.Value = value;
            }
        }

        private readonly NetworkVariable<Vector2> _sizeNetworkVariable = new();

        public FacePreference DefaultFace
        {
            get => IsOnline ? (FacePreference) _faceNetworkVariable.Value : _facePreference;
            set
            {
                _facePreference = value;
                if (IsOnline)
                    _faceNetworkVariable.Value = (int) value;
            }
        }

        private FacePreference _facePreference;
        private readonly NetworkVariable<int> _faceNetworkVariable = new();

        public CardAction DefaultAction
        {
            get => IsOnline ? (CardAction) _actionNetworkVariable.Value : _cardAction;
            set
            {
                _cardAction = value;
                if (IsOnline)
                    _actionNetworkVariable.Value = (int) value;
            }
        }

        private CardAction _cardAction;
        private readonly NetworkVariable<int> _actionNetworkVariable = new();

        public bool DoesImmediatelyRelease { get; set; }

        public UnityAction OnLayout { get; set; }

        public List<OnAddCardDelegate> OnAddCardActions { get; } = new();
        public List<OnRemoveCardDelegate> OnRemoveCardActions { get; } = new();

        protected override void OnStartPlayable()
        {
            if (!IsOnline)
                return;

            var rectTransform = (RectTransform) transform;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localPosition = Position;
            if (Vector2.zero.Equals(Size))
                rectTransform.sizeDelta = Size;

            var spacing = PlaySettings.StackViewerOverlap switch
            {
                2 => StackViewer.HighOverlapSpacing,
                1 => StackViewer.LowOverlapSpacing,
                _ => StackViewer.NoOverlapSpacing
            };

            HorizontalOrVerticalLayoutGroup layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.spacing = spacing;

            type = Type;
            allowsFlip = true;
            allowsRotation = true;
            scrollRectContainer = PlayController.Instance.playArea;
            DoesImmediatelyRelease = true;

            switch (DefaultFace)
            {
                case FacePreference.Any:
                    OnAddCardActions.Add(PlayController.OnAddCardModel);
                    break;
                case FacePreference.Down:
                    OnAddCardActions.Add(PlayController.OnAddCardModelFaceDown);
                    break;
                case FacePreference.Up:
                    OnAddCardActions.Add(PlayController.OnAddCardModelFaceUp);
                    break;
                default:
                    OnAddCardActions.Add(PlayController.OnAddCardModel);
                    break;
            }

            OnAddCardActions.Add((_, cardModel) =>
                cardModel.DefaultAction = CardActions.ActionsDictionary[DefaultAction]);
        }

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
