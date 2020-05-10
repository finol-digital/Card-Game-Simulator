/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGameDef.Unity;
using Cgs;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = System.Diagnostics.Debug;

namespace CardGameView
{
    public enum DragPhase
    {
        Begin,
        Drag,
        End
    }

    [RequireComponent(typeof(Image), typeof(CanvasGroup), typeof(Outline))]
    public class CardModel : NetworkBehaviour, ICardDisplay, IPointerDownHandler, IPointerUpHandler, ISelectHandler,
        IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float ZoomHoldTime = 1.5f;
        private const float MovementSpeed = 600f;
        private static readonly Color SelectedHighlightColor = new Color(0.02f, 0.5f, 0.4f);
        private static readonly Vector2 OutlineHighlightDistance = new Vector2(15, 15);

        public bool IsOnline => CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive
                                                               && transform.parent == CgsNetManager.Instance
                                                                   .playController.playAreaCardStack.transform;

        private bool IsProcessingSecondaryDragAction => PointerPositions.Count > 1 || (CurrentPointerEventData != null &&
                                                                                       (CurrentPointerEventData.button ==
                                                                                        PointerEventData.InputButton
                                                                                            .Middle ||
                                                                                        CurrentPointerEventData.button ==
                                                                                        PointerEventData.InputButton
                                                                                            .Right));

        public CardStack ParentCardStack => transform.parent.GetComponent<CardStack>();

        public bool IsStatic { get; set; }
        public bool DoesCloneOnDrag { get; set; }
        public CardAction DoubleClickAction { get; set; }
        public UnityAction SecondaryDragAction { get; set; }
        public CardDropArea DropTarget { get; set; }

        public float HoldTime { get; private set; }
        public bool DidSelectOnDown { get; private set; }
        public bool DidDrag { get; private set; }
        public PointerEventData CurrentPointerEventData { get; private set; }
        public DragPhase CurrentDragPhase { get; private set; }

        public Dictionary<int, Vector2> PointerPositions { get; } = new Dictionary<int, Vector2>();
        private Dictionary<int, Vector2> PointerDragOffsets { get; } = new Dictionary<int, Vector2>();

        [SyncVar(hook = "OnChangePosition")] public Vector2 position;

        [SyncVar(hook = "OnChangeRotation")] public Quaternion rotation;

        [SyncVar] private string _id;
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public string Id => _id;

        public UnityCard Value
        {
            get
            {
                if (string.IsNullOrEmpty(_id) ||
                    !CardGameManager.Current.Cards.TryGetValue(_id, out UnityCard cardValue))
                    return UnityCard.Blank;
                return cardValue;
            }
            set
            {
                Value.UnregisterDisplay(this);
                _id = value != null ? value.Id : string.Empty;
                gameObject.name = value != null ? "[" + value.Id + "] " + value.Name : string.Empty;
                value?.RegisterDisplay(this);
            }
        }

        [SyncVar] private bool _isFacedown;

        public bool IsFacedown
        {
            get => _isFacedown;
            set
            {
                if (value == _isFacedown)
                    return;
                _isFacedown = value;
                if (!_isFacedown)
                    Value.RegisterDisplay(this);
                else
                    Value.UnregisterDisplay(this);

                if (IsOnline && hasAuthority)
                    CmdUpdateIsFacedown(_isFacedown);
            }
        }

        public RectTransform PlaceHolder
        {
            get => _placeHolder;
            private set
            {
                if (_placeHolder != null)
                    Destroy(_placeHolder.gameObject);
                _placeHolder = value;
                if (_placeHolder == null)
                {
                    if (ParentCardStack == null && DropTarget == null)
                        WarnHighlight();
                    _placeHolderCardStack = null;
                }
                else
                    IsHighlighted = false;
            }
        }

        private RectTransform _placeHolder;

        public CardStack PlaceHolderCardStack
        {
            get => _placeHolderCardStack;
            set
            {
                _placeHolderCardStack = value;

                if (_placeHolderCardStack == null)
                {
                    PlaceHolder = null;
                    return;
                }

                DropTarget = null;

                var placeholder = new GameObject(gameObject.name + "(PlaceHolder)", typeof(RectTransform));
                PlaceHolder = (RectTransform) placeholder.transform;
                PlaceHolder.SetParent(_placeHolderCardStack.transform);
                PlaceHolder.sizeDelta = ((RectTransform) transform).sizeDelta;
                PlaceHolder.anchoredPosition = Vector2.zero;
            }
        }

        private CardStack _placeHolderCardStack;

        private bool IsNameVisible
        {
            set
            {
                nameLabel.SetActive(value);
                if (value)
                    nameText.text = Value.Name;
            }
        }

        public GameObject nameLabel;
        public Text nameText;

        public bool IsHighlighted
        {
            set
            {
                Debug.Assert(_outline != null, nameof(_outline) + " != null");
                if (value)
                {
                    _outline.effectColor = SelectedHighlightColor;
                    _outline.effectDistance = OutlineHighlightDistance;
                }
                else
                {
                    bool isOthers = IsOnline && !hasAuthority;
                    _outline.effectColor = isOthers ? Color.yellow : Color.black;
                    _outline.effectDistance = isOthers ? OutlineHighlightDistance : Vector2.zero;
                }
            }
        }

        private Outline _outline;
        private Image _image;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            _outline = GetComponent<Outline>();
            _image = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();

            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((RectTransform) transform).sizeDelta = CardGameManager.PixelsPerInch * cardSize;

            if (IsOnline)
            {
                if (Vector2.zero != position)
                    ((RectTransform) transform).anchoredPosition = position;
                if (Quaternion.identity != rotation)
                    transform.rotation = rotation;
            }

            IsNameVisible = !IsFacedown;
            if (!IsFacedown)
                Value.RegisterDisplay(this);
        }

        public void SetImageSprite(Sprite imageSprite)
        {
            if (_image == null || imageSprite == null)
            {
                RemoveImageSprite();
                return;
            }

            _image.sprite = imageSprite;
            IsNameVisible = false;
        }

        private void RemoveImageSprite()
        {
            if (_image == null)
                return;
            _image.sprite = CardGameManager.Current.CardBackImageSprite;
            if (!IsFacedown)
                IsNameVisible = true;
        }

        private void Update()
        {
            if (PointerPositions.Count > 0 && !DidDrag)
                HoldTime += Time.deltaTime;
            else
                HoldTime = 0;

            if (!(HoldTime > ZoomHoldTime))
                return;

            CurrentPointerEventData = null;
            PointerPositions.Clear();
            PointerDragOffsets.Clear();
            if (CardViewer.Instance != null)
                CardViewer.Instance.ZoomOn(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            DidSelectOnDown =
                eventData.button != PointerEventData.InputButton.Middle && eventData.button !=
                                                                        PointerEventData.InputButton.Right
                                                                        && CardViewer.Instance.SelectedCardModel !=
                                                                        this && CardViewer.Instance.WasVisible;
            if (DidSelectOnDown && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            CurrentPointerEventData = eventData;

            PointerPositions[eventData.pointerId] = eventData.position;
            PointerDragOffsets[eventData.pointerId] = (Vector2) transform.position - eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (CurrentPointerEventData != null && CurrentPointerEventData.pointerId == eventData.pointerId &&
                !eventData.dragging
                && eventData.button != PointerEventData.InputButton.Middle &&
                eventData.button != PointerEventData.InputButton.Right)
            {
                if (!DidSelectOnDown && EventSystem.current.currentSelectedGameObject == gameObject &&
                    DoubleClickAction != null)
                    DoubleClickAction(this);
                else if (PlaceHolder == null)
                {
                    if (CardViewer.Instance != null && CardViewer.Instance.Mode == CardViewerMode.Maximal)
                        CardViewer.Instance.Mode = CardViewerMode.Expanded;
                    if (!EventSystem.current.alreadySelecting)
                        EventSystem.current.SetSelectedGameObject(gameObject, eventData);
                }
            }

            CurrentPointerEventData = eventData;
            if (CurrentDragPhase == DragPhase.Drag)
                return;

            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
            if (DidDrag && PointerDragOffsets.Count == 0)
                DidDrag = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !IsFacedown)
                CardViewer.Instance.SelectedCardModel = this;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !CardViewer.Instance.Zoom)
                CardViewer.Instance.IsVisible = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsOnline && !hasAuthority)
                return;

            DidDrag = true;
            if (DoesCloneOnDrag)
            {
                Transform transform1 = transform;
                GameObject newGameObject = Instantiate(gameObject, transform1.position, transform1.rotation,
                    gameObject.FindInParents<Canvas>().transform);
                eventData.pointerPress = newGameObject;
                eventData.pointerDrag = newGameObject;
                var cardModel = newGameObject.GetOrAddComponent<CardModel>();
                cardModel.GetComponent<CanvasGroup>().blocksRaycasts = false;
                cardModel.IsHighlighted = false;
                cardModel.Value = Value;
                cardModel.DoesCloneOnDrag = false;
                cardModel.PointerDragOffsets[eventData.pointerId] = (Vector2) transform.position - eventData.position;
                cardModel.OnBeginDrag(eventData);
                return;
            }

            EventSystem.current.SetSelectedGameObject(null, eventData);
            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.Begin;
            PointerPositions[eventData.pointerId] = eventData.position;

            UpdatePosition();
            if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
                SecondaryDragAction();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsOnline && !hasAuthority)
                return;

            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.Drag;
            PointerPositions[eventData.pointerId] = eventData.position;

            UpdatePosition();
            if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
                SecondaryDragAction();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsOnline && !hasAuthority)
                return;

            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.End;

            UpdatePosition();
            if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
                SecondaryDragAction();

            Vector2 removedOffset = Vector2.zero;
            if (PointerDragOffsets.TryGetValue(eventData.pointerId, out Vector2 pointerDragOffset))
                removedOffset = (Vector2) transform.position - eventData.position - pointerDragOffset;
            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
            foreach (int offsetKey in PointerDragOffsets.Keys.ToList())
                if (PointerDragOffsets.TryGetValue(offsetKey, out Vector2 otherOffset))
                    PointerDragOffsets[offsetKey] = otherOffset - removedOffset;

            if (IsProcessingSecondaryDragAction)
                return;

            if (PlaceHolder != null)
                StartCoroutine(MoveToPlaceHolder());
            else if (ParentCardStack == null)
                Discard();
        }

        public static CardModel GetPointerDrag(PointerEventData eventData)
        {
            return eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CardModel>();
        }

        private void UpdatePosition()
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

            Vector2 targetPosition = UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList());
            targetPosition += UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());
            if (ParentCardStack != null)
                UpdateCardStackPosition(targetPosition);
            else if (!IsStatic)
                transform.position = targetPosition;

            if (IsStatic)
                return;

            if (PlaceHolderCardStack != null)
                PlaceHolderCardStack.UpdateLayout(PlaceHolder, targetPosition);

            if (IsOnline)
                CmdUpdatePosition(((RectTransform) transform).anchoredPosition);
        }

        private void UpdateCardStackPosition(Vector2 targetPosition)
        {
            CardStack cardStack = ParentCardStack;
            if (cardStack == null || (IsOnline && !hasAuthority))
                return;

            if (!cardStack.DoesImmediatelyRelease &&
                (cardStack.type == CardStackType.Vertical || cardStack.type == CardStackType.Horizontal))
                cardStack.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
            else if (!IsStatic)
                cardStack.UpdateLayout(transform as RectTransform, targetPosition);

            if (IsStatic)
                return;

            if (cardStack.type == CardStackType.Area)
                transform.SetAsLastSibling();

            Vector3[] stackCorners = new Vector3[4];
            ((RectTransform) cardStack.transform).GetWorldCorners(stackCorners);
            bool isOutYBounds = targetPosition.y < stackCorners[0].y || targetPosition.y > stackCorners[1].y;
            bool isOutXBounds = targetPosition.x < stackCorners[0].x || targetPosition.y > stackCorners[2].x;
            if ((cardStack.DoesImmediatelyRelease && !IsProcessingSecondaryDragAction)
                || (cardStack.type == CardStackType.Full && CurrentDragPhase == DragPhase.Begin)
                || (cardStack.type == CardStackType.Vertical && isOutXBounds)
                || (cardStack.type == CardStackType.Horizontal && isOutYBounds)
                || (cardStack.type == CardStackType.Area
                    && (isOutYBounds || (PlaceHolder != null && PlaceHolder.parent != transform.parent))))
                ParentToCanvas(targetPosition);
        }

        [Command]
        private void CmdUpdatePosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        [UsedImplicitly]
        public void OnChangePosition(Vector2 oldPosition, Vector2 newPosition)
        {
            if (!hasAuthority)
                ((RectTransform) transform).anchoredPosition = newPosition;
        }

        private void ParentToCanvas(Vector3 targetPosition)
        {
            if (IsOnline && hasAuthority)
                CmdUnspawnCard();
            CardStack prevParentStack = ParentCardStack;
            if (CurrentDragPhase == DragPhase.Drag)
                prevParentStack.UpdateScrollRect(DragPhase.End, CurrentPointerEventData);
            transform.SetParent(CardGameManager.Instance.CardCanvas.transform);
            transform.SetAsLastSibling();
            if (prevParentStack != null)
                prevParentStack.OnRemove(this);
            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = false;
            var rectTransform = (RectTransform) transform;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.pivot = 0.5f * Vector2.one;
            rectTransform.position = targetPosition;
            rectTransform.localScale = Vector3.one;
        }

        [Command]
        private void CmdUnspawnCard()
        {
            RpcUnspawnCard();
        }

        [ClientRpc]
        private void RpcUnspawnCard()
        {
            if (!isServer && !hasAuthority)
                Discard();
            else if (isServer)
                NetworkServer.UnSpawn(gameObject);
        }

        private IEnumerator MoveToPlaceHolder()
        {
            while (PlaceHolder != null && Vector3.Distance(transform.position, PlaceHolder.position) > 1)
            {
                float step = MovementSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, PlaceHolder.position, step);
                yield return null;
            }

            if (PlaceHolder == null)
            {
                Discard();
                yield break;
            }

            CardStack prevParentStack = ParentCardStack;
            Transform transform1 = transform;
            transform1.SetParent(PlaceHolder.parent);
            transform1.SetSiblingIndex(PlaceHolder.GetSiblingIndex());
            transform1.localScale = Vector3.one;
            if (prevParentStack != null)
                prevParentStack.OnRemove(this);
            if (ParentCardStack != null)
                ParentCardStack.OnAdd(this);
            PlaceHolder = null;
            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = true;
        }

        public void UpdateParentCardStackScrollRect()
        {
            CardStack cardStack = ParentCardStack;
            if (cardStack != null)
                cardStack.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
        }

        public void Rotate()
        {
            Vector2 referencePoint = transform.position;
            foreach (KeyValuePair<int, Vector2> pointerDragPosition in PointerPositions)
                if (pointerDragPosition.Key != CurrentPointerEventData.pointerId)
                    referencePoint = pointerDragPosition.Value;
            Vector2 prevDir = (CurrentPointerEventData.position - CurrentPointerEventData.delta) - referencePoint;
            Vector2 currDir = CurrentPointerEventData.position - referencePoint;
            transform.Rotate(0, 0, Vector2.SignedAngle(prevDir, currDir));

            if (IsOnline)
                CmdUpdateRotation(transform.rotation);
        }

        [Command]
        public void CmdUpdateRotation(Quaternion newRotation)
        {
            rotation = newRotation;
        }

        [UsedImplicitly]
        public void OnChangeRotation(Quaternion oldRotation, Quaternion newRotation)
        {
            if (!hasAuthority)
                transform.rotation = newRotation;
        }

        [Command]
        private void CmdUpdateIsFacedown(bool isFacedown)
        {
            RpcUpdateIsFacedown(isFacedown);
        }

        [ClientRpc]
        private void RpcUpdateIsFacedown(bool isFacedown)
        {
            if (!hasAuthority)
                IsFacedown = isFacedown;
        }

        private void WarnHighlight()
        {
            if (_outline == null)
                return;
            _outline.effectColor = Color.red;
            _outline.effectDistance = OutlineHighlightDistance;
        }

        [ClientRpc]
        public void RpcHideHighlight()
        {
            IsHighlighted = false;
        }

        private void Discard()
        {
            if (DropTarget == null && CgsNetManager.Instance != null)
                CgsNetManager.Instance.playController.CatchDiscard(Value);
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
