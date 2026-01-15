/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using Cgs.CardGameView.Viewer;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using Cgs.UI.ScrollRects;
using FinolDigital.Cgs.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;
using CardAction = FinolDigital.Cgs.Json.CardAction;

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
            get => IsSpawned ? (CardZoneType)_typeNetworkVariable.Value : type;
            set
            {
                type = value;
                if (IsSpawned)
                    _typeNetworkVariable.Value = (int)value;
            }
        }

        private NetworkVariable<int> _typeNetworkVariable;

        public FacePreference DefaultFace
        {
            get => IsSpawned ? (FacePreference)_facePreferenceNetworkVariable.Value : _facePreference;
            set
            {
                _facePreference = value;
                if (IsSpawned)
                    _facePreferenceNetworkVariable.Value = (int)value;
            }
        }

        private FacePreference _facePreference = FacePreference.Any;
        private NetworkVariable<int> _facePreferenceNetworkVariable;

        public CardAction DefaultAction
        {
            get => IsSpawned ? (CardAction)_cardActionNetworkVariable.Value : _cardAction;
            set
            {
                _cardAction = value;
                if (IsSpawned)
                    _cardActionNetworkVariable.Value = (int)value;
            }
        }

        private CardAction _cardAction = CardAction.Move;
        private NetworkVariable<int> _cardActionNetworkVariable;

        public bool DoesImmediatelyRelease { get; set; }

        public UnityAction OnLayout { get; set; }

        public List<OnAddCardDelegate> OnAddCardActions { get; } = new();
        public List<OnRemoveCardDelegate> OnRemoveCardActions { get; } = new();

        protected override void OnAwakePlayable()
        {
            _typeNetworkVariable = new NetworkVariable<int>();
            _facePreferenceNetworkVariable = new NetworkVariable<int>();
            _cardActionNetworkVariable = new NetworkVariable<int>();
        }

        protected override void OnNetworkSpawnPlayable()
        {
            if (CardZoneType.Area.Equals(type) && (CardZoneType)_typeNetworkVariable.Value != CardZoneType.Area)
                type = (CardZoneType)_typeNetworkVariable.Value;
            if (FacePreference.Any.Equals(_facePreference) &&
                (FacePreference)_facePreferenceNetworkVariable.Value != FacePreference.Any)
                _facePreference = (FacePreference)_facePreferenceNetworkVariable.Value;
            if (CardAction.Move.Equals(_cardAction) && (CardAction)_cardActionNetworkVariable.Value != CardAction.Move)
                _cardAction = (CardAction)_cardActionNetworkVariable.Value;
        }

        protected override void OnStartPlayable()
        {
            if (PlayController.Instance == null ||
                PlayController.Instance.playAreaCardZone.transform != transform.parent)
                return;

            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            if (!Vector2.zero.Equals(Position))
                rectTransform.localPosition = Position;
            if (!Vector2.zero.Equals(Size))
                rectTransform.sizeDelta = Size;

            var spacing = PlaySettings.StackViewerOverlap switch
            {
                2 => StackViewer.HighOverlapSpacing,
                1 => StackViewer.LowOverlapSpacing,
                _ => StackViewer.NoOverlapSpacing
            };

            HorizontalOrVerticalLayoutGroup layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.spacing = spacing;
            else
                layoutGroup = GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.spacing = -225f;

            allowsFlip = true;
            allowsRotation = true;
            scrollRectContainer = PlayController.Instance.playArea;
            DoesImmediatelyRelease = true;

            OnAddCardActions.Add((cardZone, cardModel) =>
            {
                cardModel.IsFacedown = cardZone.DefaultFace switch
                {
                    FacePreference.Down => true,
                    FacePreference.Up => false,
                    _ => cardModel.IsFacedown
                };
                cardModel.DefaultAction = CardActionPanel.CardActionDictionary[cardZone.DefaultAction];
                cardModel.SecondaryDragAction = cardModel.UpdateParentCardZoneScrollRect;
            });

            StartCoroutine(WaitToAddMoveCardToServer());
        }

        private IEnumerator WaitToAddMoveCardToServer()
        {
            while (CgsNetManager.Instance.LocalPlayer == null)
                yield return null;
            OnAddCardActions.Add(CgsNetManager.Instance.LocalPlayer.MoveCardToServer);
        }

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            var cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && (Type != CardZoneType.Area || cardModel.transform.parent != transform) &&
                !cardModel.IsStatic)
                cardModel.PlaceHolderCardZone = this;

            if (Type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnPointerEnter(eventData);
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            var cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && cardModel.PlaceHolderCardZone == this)
                cardModel.PlaceHolderCardZone = null;

            OnLayout?.Invoke();

            if (Type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
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
            if (Type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnBeginDrag(eventData);
            else if (scrollRectContainer != null)
                scrollRectContainer.OnBeginDrag(eventData);
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            if (Type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
                scrollRect.OnDrag(eventData);
            else if (scrollRectContainer != null)
                scrollRectContainer.OnDrag(eventData);
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (Type == CardZoneType.Area && scrollRectContainer is RotateZoomableScrollRect scrollRect)
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

            if (Type == CardZoneType.Area)
                cardModel.SnapToGrid();

            if (cardModel.ToDelete)
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

            switch (Type)
            {
                case CardZoneType.Vertical:
                case CardZoneType.Horizontal:
                    var newSiblingIndex = transform.childCount;
                    for (var i = 0; i < transform.childCount; i++)
                    {
                        if (Type == CardZoneType.Vertical
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
