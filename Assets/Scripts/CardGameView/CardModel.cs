/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Mirror;

using CardGameDef;
using CGS;
using CGS.Play.Multiplayer;

namespace CardGameView
{
    public enum DragPhase
    {
        Begin,
        Drag,
        End
    }

    [RequireComponent(typeof(Image), typeof(CanvasGroup), typeof(Outline))]
    public class CardModel : NetworkBehaviour, ICardDisplay, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float MovementSpeed = 600f;
        public static readonly Color SelectedHighlightColor = new Color(1f, 0.45f, 0f);
        public static readonly Vector2 OutlineHighlightDistance = new Vector2(15, 15);

        public bool IsOnline => CGSNetManager.Instance != null && CGSNetManager.Instance.isNetworkActive
            && transform.parent == CGSNetManager.Instance.playController.playAreaContent;
        public bool IsProcessingSecondaryDragAction => PointerPositions.Count > 1 || (CurrentPointerEventData != null &&
            (CurrentPointerEventData.button == PointerEventData.InputButton.Middle || CurrentPointerEventData.button == PointerEventData.InputButton.Right));
        public CardStack ParentCardStack => transform.parent.GetComponent<CardStack>();

        public bool IsStatic { get; set; }
        public bool DoesCloneOnDrag { get; set; }
        public CardAction DoubleClickAction { get; set; }
        public UnityAction SecondaryDragAction { get; set; }
        public CardDropArea DropTarget { get; set; }

        public bool DidSelectOnDown { get; private set; }
        public PointerEventData CurrentPointerEventData { get; private set; }
        public DragPhase CurrentDragPhase { get; private set; }

        public Dictionary<int, Vector2> PointerPositions { get; } = new Dictionary<int, Vector2>();
        protected Dictionary<int, Vector2> PointerDragOffsets { get; } = new Dictionary<int, Vector2>();

        [SyncVar(hook = "OnChangePosition")]
        public Vector2 position;

        [SyncVar(hook = "OnChangeRotation")]
        public Quaternion rotation;

        [SyncVar]
        private string _id;
        public string Id => _id;
        public Card Value
        {
            get
            {
                if (string.IsNullOrEmpty(_id) || !CardGameManager.Current.Cards.TryGetValue(_id, out Card cardValue))
                    return Card.Blank;
                return cardValue;
            }
            set
            {
                Value.UnregisterDisplay(this);
                _id = value != null ? value.Id : string.Empty;
                gameObject.name = value != null ? "[" + value.Id + "] " + value.Name : string.Empty;
                value.RegisterDisplay(this);
            }
        }

        [SyncVar]
        private int _quantity;
        public int Quantity
        {
            get { return _quantity; }
            set
            {
                _quantity = value;
                IsQuantityVisible = _quantity > 1;
            }
        }

        [SyncVar]
        private bool _isFacedown;
        public bool IsFacedown
        {
            get { return _isFacedown; }
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
            get { return _placeHolder; }
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
            get { return _placeHolderCardStack; }
            set
            {
                _placeHolderCardStack = value;

                if (_placeHolderCardStack == null)
                {
                    PlaceHolder = null;
                    return;
                }
                DropTarget = null;

                GameObject placeholder = new GameObject(gameObject.name + "(PlaceHolder)", typeof(RectTransform));
                PlaceHolder = (RectTransform)placeholder.transform;
                PlaceHolder.SetParent(_placeHolderCardStack.transform);
                PlaceHolder.sizeDelta = ((RectTransform)transform).sizeDelta;
                PlaceHolder.anchoredPosition = Vector2.zero;
            }
        }
        private CardStack _placeHolderCardStack;

        public bool IsNameVisible
        {
            get { return nameLabel.activeSelf; }
            set
            {
                nameLabel.SetActive(value);
                if (value)
                    nameText.text = Value.Name;
            }
        }
        public GameObject nameLabel;
        public Text nameText;

        public bool IsQuantityVisible
        {
            get { return quantityLabel.activeSelf; }
            set
            {
                quantityLabel.SetActive(value);
                quantityText.text = Quantity.ToString();
            }
        }
        public GameObject quantityLabel;
        public Text quantityText;

        public bool IsHighlighted
        {
            get { return outline.effectColor == SelectedHighlightColor; }
            set
            {
                if (value)
                {
                    outline.effectColor = SelectedHighlightColor;
                    outline.effectDistance = OutlineHighlightDistance;
                }
                else
                {
                    bool isOthers = IsOnline && !hasAuthority;
                    outline.effectColor = isOthers ? Color.yellow : Color.black;
                    outline.effectDistance = isOthers ? OutlineHighlightDistance : Vector2.zero;
                }
            }
        }
        protected Outline outline => _outline ?? (_outline = GetComponent<Outline>());
        private Outline _outline;

        public Image image => _image ?? (_image = GetComponent<Image>());
        private Image _image;

        public CanvasGroup canvasGroup => _canvasGroup ?? (_canvasGroup = GetComponent<CanvasGroup>());
        private CanvasGroup _canvasGroup;

        void Start()
        {
            ((RectTransform)transform).sizeDelta = CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize;

            if (IsOnline)
            {
                if (Vector2.zero != position)
                    ((RectTransform)transform).anchoredPosition = position;
                if (Quaternion.identity != rotation)
                    transform.rotation = rotation;
            }

            IsNameVisible = !IsFacedown;
            if (Quantity == 0)
                Quantity = 1;
            if (!IsFacedown)
                Value.RegisterDisplay(this);
        }

        public void SetImageSprite(Sprite imageSprite)
        {
            if (image == null || imageSprite == null)
            {
                RemoveImageSprite();
                return;
            }

            image.sprite = imageSprite;
            IsNameVisible = false;
        }

        private void RemoveImageSprite()
        {
            if (image == null)
                return;
            image.sprite = CardGameManager.Current.CardBackImageSprite;
            if (!IsFacedown)
                IsNameVisible = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            DidSelectOnDown = eventData.button != PointerEventData.InputButton.Middle && eventData.button != PointerEventData.InputButton.Right
                && CardViewer.Instance.SelectedCardModel != this && CardViewer.Instance.WasVisible;
            if (DidSelectOnDown)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            CurrentPointerEventData = eventData;

            PointerPositions[eventData.pointerId] = eventData.position;
            PointerDragOffsets[eventData.pointerId] = ((Vector2)transform.position) - eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (CurrentPointerEventData != null && CurrentPointerEventData.pointerId == eventData.pointerId && !eventData.dragging
                && eventData.button != PointerEventData.InputButton.Middle && eventData.button != PointerEventData.InputButton.Right)
            {
                if (!DidSelectOnDown && EventSystem.current.currentSelectedGameObject == gameObject && DoubleClickAction != null)
                    DoubleClickAction(this);
                else if (PlaceHolder == null)
                    EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            }

            CurrentPointerEventData = eventData;
            if (CurrentDragPhase == DragPhase.Drag)
                return;

            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !IsFacedown)
                CardViewer.Instance.SelectedCardModel = this;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !CardViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardViewer.Instance.IsVisible = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsOnline && !hasAuthority)
                return;

            if (DoesCloneOnDrag)
            {
                GameObject newGameObject = Instantiate(gameObject, transform.position, transform.rotation, gameObject.FindInParents<Canvas>().transform);
                eventData.pointerPress = newGameObject;
                eventData.pointerDrag = newGameObject;
                CardModel cardModel = newGameObject.GetOrAddComponent<CardModel>();
                cardModel.canvasGroup.blocksRaycasts = false;
                cardModel.IsHighlighted = false;
                cardModel.Value = Value;
                cardModel.DoesCloneOnDrag = false;
                cardModel.PointerDragOffsets[eventData.pointerId] = (Vector2)transform.position - eventData.position;
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
                removedOffset = (Vector2)transform.position - eventData.position - pointerDragOffset;
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
            if (eventData.pointerDrag == null)
                return null;
            return eventData.pointerDrag.GetComponent<CardModel>();
        }

        public void UpdatePosition()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (SecondaryDragAction != Rotate && IsProcessingSecondaryDragAction)
                return;
#else
            if (Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) || Input.GetMouseButton(2) || Input.GetMouseButtonUp(2))
                return;
#endif
            if (PointerPositions.Count < 1 || PointerDragOffsets.Count < 1 || (IsOnline && !hasAuthority))
                return;

            Vector2 targetPosition = UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList());
            targetPosition = targetPosition + UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());
            if (ParentCardStack != null)
                UpdateCardStackPosition(targetPosition);
            else if (!IsStatic)
                transform.position = targetPosition;

            if (!IsStatic)
            {
                if (PlaceHolderCardStack != null)
                    PlaceHolderCardStack.UpdateLayout(PlaceHolder, targetPosition);

                if (IsOnline)
                    CmdUpdatePosition(((RectTransform)transform).anchoredPosition);
            }
        }

        public void UpdateCardStackPosition(Vector2 targetPosition)
        {
            CardStack cardStack = ParentCardStack;
            if (cardStack == null || (IsOnline && !hasAuthority))
                return;

            if (!cardStack.DoesImmediatelyRelease && (cardStack.type == CardStackType.Vertical || cardStack.type == CardStackType.Horizontal))
                cardStack.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
            else if (!IsStatic)
                cardStack.UpdateLayout(transform as RectTransform, targetPosition);

            if (!IsStatic)
            {
                if (cardStack.type == CardStackType.Area)
                    transform.SetAsLastSibling();

                Vector3[] stackCorners = new Vector3[4];
                ((RectTransform)cardStack.transform).GetWorldCorners(stackCorners);
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
        }

        [Command]
        void CmdUpdatePosition(Vector2 position)
        {
            this.position = position;
        }

        public void OnChangePosition(Vector2 position)
        {
            if (!hasAuthority)
                ((RectTransform)transform).anchoredPosition = position;
        }

        public void ParentToCanvas(Vector3 targetPosition)
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
            canvasGroup.blocksRaycasts = false;
            ((RectTransform)transform).anchorMax = 0.5f * Vector2.one;
            ((RectTransform)transform).anchorMin = 0.5f * Vector2.one;
            ((RectTransform)transform).pivot = 0.5f * Vector2.one;
            transform.position = targetPosition;
            transform.localScale = Vector3.one;
        }

        [Command]
        void CmdUnspawnCard()
        {
            RpcUnspawnCard();
        }

        [ClientRpc]
        public void RpcUnspawnCard()
        {
            if (!isServer && !hasAuthority)
                Discard();
            else if (isServer)
                NetworkServer.UnSpawn(gameObject);
        }

        public IEnumerator MoveToPlaceHolder()
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
            transform.SetParent(PlaceHolder.parent);
            transform.SetSiblingIndex(PlaceHolder.GetSiblingIndex());
            transform.localScale = Vector3.one;
            if (prevParentStack != null)
                prevParentStack.OnRemove(this);
            if (ParentCardStack != null)
                ParentCardStack.OnAdd(this);
            PlaceHolder = null;
            canvasGroup.blocksRaycasts = true;
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
        public void CmdUpdateRotation(Quaternion rotation)
        {
            this.rotation = rotation;
        }

        public void OnChangeRotation(Quaternion rotation)
        {
            if (!hasAuthority)
                transform.rotation = rotation;
        }

        [Command]
        void CmdUpdateIsFacedown(bool isFacedown)
        {
            RpcUpdateIsFacedown(isFacedown);
        }

        [ClientRpc]
        void RpcUpdateIsFacedown(bool isFacedown)
        {
            if (!hasAuthority)
                IsFacedown = isFacedown;
        }

        public void WarnHighlight()
        {
            outline.effectColor = Color.red;
            outline.effectDistance = OutlineHighlightDistance;
        }

        [ClientRpc]
        public void RpcHideHighlight()
        {
            IsHighlighted = false;
        }

        public void Discard()
        {
            if (DropTarget == null && CardGameManager.Current.GameCatchesDiscard && CGSNetManager.Instance != null)
                CGSNetManager.Instance.playController.CatchDiscard(Value);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (CardGameManager.IsQuitting)
                return;

            Value.UnregisterDisplay(this);
            if (PlaceHolder != null)
                Destroy(PlaceHolder.gameObject);
        }
    }
}
