/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGameDef.Unity;
using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
{
    public class CardModel : CgsNetPlayable, ICardDisplay, ICardDropHandler
    {
        private const float ZoomHoldTime = 1.5f;
        private const float MovementSpeed = 600f;

        public int Index { get; set; }

        public bool IsStatic { get; set; }
        public bool DoesCloneOnDrag { get; set; }
        public CardAction DefaultAction { get; set; }
        public UnityAction SecondaryDragAction { get; set; }
        public CardDropArea DropTarget { get; set; }

        [field: SyncVar] public string Id { get; private set; }

        public override string ViewValue => Value.Name;

        public UnityCard Value
        {
            get
            {
                if (string.IsNullOrEmpty(Id) ||
                    !CardGameManager.Current.Cards.TryGetValue(Id, out var unityCard))
                    return UnityCard.Blank;
                return unityCard;
            }
            set
            {
                Value.UnregisterDisplay(this);
                Id = value != null ? value.Id : string.Empty;
                gameObject.name = value != null ? "[" + value.Id + "] " + value.Name : string.Empty;
                if (!isFacedown)
                    value?.RegisterDisplay(this);
            }
        }

        [SyncVar(hook = nameof(OnChangeFacedown))]
        public bool isFacedown;

        public void SetIsFacedown(bool value)
        {
            if (IsOnline)
                CmdUpdateFacedown(value);
            else
            {
                var oldValue = isFacedown;
                isFacedown = value;
                OnChangeFacedown(oldValue, isFacedown);
            }
        }

        public RectTransform PlaceHolder
        {
            get => _placeHolder;
            private set
            {
                if (_placeHolder != null)
                {
                    _placeHolder.SetParent(null);
                    Destroy(_placeHolder.gameObject);
                }

                _placeHolder = value;
                if (_placeHolder == null)
                {
                    if (ParentCardZone == null && DropTarget == null)
                        HighlightMode = HighlightMode.Warn;
                    _placeHolderCardZone = null;
                }
                else
                    HighlightMode = HighlightMode.Off;
            }
        }

        private RectTransform _placeHolder;

        public CardZone PlaceHolderCardZone
        {
            get => _placeHolderCardZone;
            set
            {
                _placeHolderCardZone = value;

                if (_placeHolderCardZone == null)
                {
                    PlaceHolder = null;
                    return;
                }

                var placeholder = new GameObject(gameObject.name + "(PlaceHolder)", typeof(RectTransform));
                PlaceHolder = (RectTransform) placeholder.transform;
                PlaceHolder.SetParent(_placeHolderCardZone.transform);
                PlaceHolder.sizeDelta = ((RectTransform) transform).sizeDelta;
                PlaceHolder.localPosition = Vector2.zero;
            }
        }

        private CardZone _placeHolderCardZone;

        private void SetIsNameVisible(bool value)
        {
            nameLabel.SetActive(value);
            if (value)
                nameText.text = Value.Name;
        }

        public GameObject nameLabel;
        public Text nameText;

        private Image View => _view ??= GetComponent<Image>();
        private Image _view;

        private CanvasGroup Visibility => _visibility ??= GetComponent<CanvasGroup>();
        private CanvasGroup _visibility;

        protected override void OnStartPlayable()
        {
            GetComponent<CardDropArea>().DropHandler = this;

            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((RectTransform) transform).sizeDelta = CardGameManager.PixelsPerInch * cardSize;
            gameObject.GetOrAddComponent<BoxCollider2D>().size = CardGameManager.PixelsPerInch * cardSize;

            var targetRotation = Value.GetPropertyValueInt(CardGameManager.Current.CardRotationIdentifier);
            if (targetRotation == 0)
                targetRotation = CardGameManager.Current.CardRotationDefault;
            if (targetRotation != 0)
            {
                transform.Rotate(0, 0, targetRotation);
                if (IsOnline)
                    RequestUpdateRotation(transform.rotation);
            }

            SetIsNameVisible(!isFacedown);
            if (!isFacedown)
                Value.RegisterDisplay(this);
        }

        public void SetImageSprite(Sprite imageSprite)
        {
            if (imageSprite == null)
            {
                RemoveImageSprite();
                return;
            }

            View.sprite = imageSprite;
            SetIsNameVisible(false);
        }

        private void RemoveImageSprite()
        {
            View.sprite = CardGameManager.Current.CardBackImageSprite;
            if (!isFacedown)
                SetIsNameVisible(true);
        }

        protected override void OnUpdatePlayable()
        {
            if (HoldTime > ZoomHoldTime)
                RequestZoomOnThis();
        }

        private void RequestZoomOnThis()
        {
            CurrentPointerEventData = null;
            PointerPositions.Clear();
            PointerDragOffsets.Clear();
            if (CardViewer.Instance != null)
                CardViewer.Instance.ZoomOn(this);
        }

        protected override void OnPointerDownPlayable(PointerEventData eventData)
        {
            DidSelectOnDown =
                eventData.button != PointerEventData.InputButton.Middle && eventData.button !=
                                                                        PointerEventData.InputButton.Right
                                                                        && CardViewer.Instance.SelectedCardModel !=
                                                                        this && CardViewer.Instance.WasVisible;
            if (DidSelectOnDown && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            if (CurrentPointerEventData == null || CurrentPointerEventData.pointerId != eventData.pointerId ||
                eventData.dragging || eventData.button == PointerEventData.InputButton.Middle ||
                eventData.button == PointerEventData.InputButton.Right)
                return;

            if (!DidSelectOnDown && EventSystem.current.currentSelectedGameObject == gameObject &&
                DefaultAction != null)
                DefaultAction(this);
            else if (PlaceHolder == null)
            {
                if (CardViewer.Instance != null && CardViewer.Instance.Mode == CardViewerMode.Maximal)
                    CardViewer.Instance.Mode = CardViewerMode.Expanded;
                if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            }
        }

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            if (Settings.PreviewOnMouseOver && CardViewer.Instance != null && !CardViewer.Instance.IsVisible
                && (PlayableViewer.Instance == null || !PlayableViewer.Instance.IsVisible)
                && CurrentDragPhase != DragPhase.Drag && !isFacedown)
                CardViewer.Instance.Preview(this);
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            if (CardViewer.Instance != null)
                CardViewer.Instance.HidePreview();
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            if (CardViewer.Instance != null)
                CardViewer.Instance.SelectedCardModel = this;
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !CardViewer.Instance.Zoom)
                CardViewer.Instance.IsVisible = false;
        }

        public void OnDrop(CardModel cardModel)
        {
            // TODO: CREATE STACK WITH 2 CARDS
        }

        public static CardModel CreateDrag(PointerEventData eventData, GameObject gameObject, Transform transform,
            UnityCard value, bool isFacedown, CardZone placeHolderCardZone = null)
        {
            var position = transform.position;
            var newGameObject = Instantiate(gameObject, position, transform.rotation,
                transform.gameObject.FindInParents<Canvas>().transform);
            eventData.pointerPress = newGameObject;
            eventData.pointerDrag = newGameObject;
            var cardModel = newGameObject.GetOrAddComponent<CardModel>();
            cardModel.Visibility.blocksRaycasts = false;
            cardModel.HighlightMode = HighlightMode.Off;
            cardModel.Value = value;
            cardModel.SetIsFacedown(isFacedown);
            cardModel.PlaceHolderCardZone = placeHolderCardZone;
            cardModel.DoesCloneOnDrag = false;
            cardModel.PointerDragOffsets[eventData.pointerId] = (Vector2) position - eventData.position;
            cardModel.OnBeginDrag(eventData);
            return cardModel;
        }

        protected override bool PreBeginDrag(PointerEventData eventData)
        {
            DidDrag = true;
            if (DoesCloneOnDrag)
            {
                if (!IsOnline)
                    NetworkServer.UnSpawn(gameObject); // Avoid Mirror error
                CreateDrag(eventData, gameObject, transform, Value, isFacedown);
                return true;
            }

            EventSystem.current.SetSelectedGameObject(null, eventData);
            return false;
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (CardViewer.Instance != null)
                CardViewer.Instance.HidePreview();

            if (!IsOnline)
                ActOnDrag();
            else
                RequestTransferAuthority();
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            if (!IsOnline || hasAuthority)
                ActOnDrag();
            else
                RequestTransferAuthority();
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (!IsOnline || hasAuthority)
                ActOnDrag();
        }

        protected override void PostDragPlayable(PointerEventData eventData)
        {
            if (IsProcessingSecondaryDragAction)
                return;

            if (DropTarget != null &&
                (ParentCardZone != null && ParentCardZone.type == CardZoneType.Area
                 || PlaceHolderCardZone != null && PlaceHolderCardZone.type == CardZoneType.Area))
            {
                var shouldDiscard = true;
                if (Visibility.blocksRaycasts)
                {
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, hits);
                    var isPointerOverDropTarget = hits.Any(hit => hit.gameObject == DropTarget.gameObject);
                    if (isPointerOverDropTarget)
                        DropTarget.OnDrop(eventData);
                    else
                        shouldDiscard = false;
                }

                if (!shouldDiscard)
                    return;

                Discard();
                return;
            }

            DropTarget = null;

            if (PlaceHolder != null)
                StartCoroutine(MoveToPlaceHolder());
            else if (ParentCardZone == null)
                Discard();
            else if (ParentCardZone.type == CardZoneType.Area)
                SnapToGrid();
        }

        public static CardModel GetPointerDrag(PointerEventData eventData)
        {
            return eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CardModel>();
        }

        private void ActOnDrag()
        {
            UpdatePosition();
            if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
                SecondaryDragAction();
        }

        public override void SnapToGrid()
        {
            var rectTransform = (RectTransform) transform;
            var gridPosition = CalculateGridPosition();
            rectTransform.position = gridPosition;

            if (IsOnline && hasAuthority)
                RequestUpdatePosition(rectTransform.localPosition);
        }

        protected override void UpdatePosition()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (SecondaryDragAction != Rotate && IsProcessingSecondaryDragAction)
                return;
#else
            if (Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) || Input.GetMouseButton(2) ||
                Input.GetMouseButtonUp(2))
                return;
#endif
            if (PointerPositions.Count < 1 || PointerDragOffsets.Count < 1 || (IsOnline && !hasAuthority))
                return;

            if (DropTarget == null && PlaceHolder == null && ParentCardZone == null)
                HighlightMode = HighlightMode.Warn;
            else if (CurrentDragPhase != DragPhase.End)
                HighlightMode = HighlightMode.Authorized;
            else
                HighlightMode = HighlightMode.Off;

            var targetPosition =
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList());
            targetPosition +=
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());
            if (ParentCardZone != null)
                UpdateCardZonePosition(targetPosition);
            else if (!IsStatic)
                transform.position = targetPosition;

            if (IsStatic)
                return;

            if (DropTarget != null && DropTarget.isBlocker && ParentCardZone != null)
                ParentToCanvas(targetPosition);

            if (PlaceHolderCardZone != null)
                PlaceHolderCardZone.UpdateLayout(PlaceHolder, targetPosition);

            if (IsOnline)
                RequestUpdatePosition(((RectTransform) transform).localPosition);
        }

        private void UpdateCardZonePosition(Vector2 targetPosition)
        {
            var cardZone = ParentCardZone;
            if (cardZone == null || (IsOnline && !hasAuthority))
                return;

            if (!cardZone.DoesImmediatelyRelease &&
                (cardZone.type == CardZoneType.Vertical || cardZone.type == CardZoneType.Horizontal))
                cardZone.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
            else if (!IsStatic)
                cardZone.UpdateLayout(transform as RectTransform, targetPosition);

            if (IsStatic)
                return;

            if (cardZone.type == CardZoneType.Area)
                transform.SetAsLastSibling();

            var zoneCorners = new Vector3[4];
            ((RectTransform) cardZone.transform).GetWorldCorners(zoneCorners);
            var isOutYBounds = targetPosition.y < zoneCorners[0].y || targetPosition.y > zoneCorners[1].y;
            var isOutXBounds = targetPosition.x < zoneCorners[0].x || targetPosition.y > zoneCorners[2].x;
            if ((cardZone.DoesImmediatelyRelease && !IsProcessingSecondaryDragAction)
                || (cardZone.type == CardZoneType.Vertical && isOutXBounds)
                || (cardZone.type == CardZoneType.Horizontal && isOutYBounds)
                || (cardZone.type == CardZoneType.Area
                    && (isOutYBounds || (PlaceHolder != null && PlaceHolder.parent != transform.parent))))
                ParentToCanvas(targetPosition);
        }

        private void ParentToCanvas(Vector3 targetPosition)
        {
            if (IsOnline && hasAuthority)
                CmdUnspawnCard(true);
            var previousParentCardZone = ParentCardZone;
            if (CurrentDragPhase == DragPhase.Drag)
                previousParentCardZone.UpdateScrollRect(DragPhase.End, CurrentPointerEventData);
            transform.SetParent(CardGameManager.Instance.CardCanvas.transform);
            transform.SetAsLastSibling();
            if (previousParentCardZone != null)
                previousParentCardZone.OnRemove(this);
            Visibility.blocksRaycasts = false;
            var rectTransform = (RectTransform) transform;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.pivot = 0.5f * Vector2.one;
            rectTransform.position = targetPosition;
            rectTransform.localScale = Vector3.one;
        }

        private IEnumerator MoveToPlaceHolder()
        {
            while (PlaceHolder != null && Vector3.Distance(transform.position, PlaceHolder.position) > 1)
            {
                var distance = MovementSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, PlaceHolder.position, distance);
                yield return null;
            }

            if (PlaceHolder == null)
            {
                Discard();
                yield break;
            }

            var previousParentCardZone = ParentCardZone;
            var cachedTransform = transform;
            cachedTransform.SetParent(PlaceHolder.parent);
            cachedTransform.SetSiblingIndex(PlaceHolder.GetSiblingIndex());
            cachedTransform.localScale = Vector3.one;
            if (previousParentCardZone != null)
                previousParentCardZone.OnRemove(this);
            if (ParentCardZone != null)
                ParentCardZone.OnAdd(this);

            PlaceHolder = null;
            Visibility.blocksRaycasts = true;
        }

        public void UpdateParentCardZoneScrollRect()
        {
            var cardZone = ParentCardZone;
            if (cardZone != null)
                cardZone.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
        }

        [Command(requiresAuthority = false)]
        private void CmdUpdateFacedown(bool facedown)
        {
            isFacedown = facedown;
        }

        [PublicAPI]
        public void OnChangeFacedown(bool oldValue, bool newValue)
        {
            if (!isFacedown)
                Value.RegisterDisplay(this);
            else
                Value.UnregisterDisplay(this);
        }

        private void Discard()
        {
            ToDiscard = true;
            if (IsOnline)
                CmdUnspawnCard(false);
            Destroy(gameObject);
        }

        [Command(requiresAuthority = false)]
        private void CmdUnspawnCard(bool shouldClientKeep)
        {
            RpcUnspawnCard(shouldClientKeep);
        }

        [ClientRpc]
        private void RpcUnspawnCard(bool shouldClientKeep)
        {
            var shouldKeep = shouldClientKeep && hasAuthority;
            if (isServer)
                NetworkServer.UnSpawn(gameObject);
            if (!shouldKeep)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (CardGameManager.IsQuitting)
                return;

            Value.UnregisterDisplay(this);
            if (PlaceHolder != null)
                Destroy(PlaceHolder.gameObject);
        }
    }
}
