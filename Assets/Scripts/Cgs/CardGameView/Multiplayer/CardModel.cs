/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGameDef.Unity;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
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
                                                                   .playController.playArea.transform;

        private bool IsProcessingSecondaryDragAction =>
            PointerPositions.Count > 1
            || CurrentPointerEventData != null &&
            (CurrentPointerEventData.button == PointerEventData.InputButton.Middle ||
             CurrentPointerEventData.button == PointerEventData.InputButton.Right);

        public CardZone ParentCardZone => transform.parent != null ? transform.parent.GetComponent<CardZone>() : null;

        public int Index { get; set; }

        public bool IsStatic { get; set; }
        public bool DoesCloneOnDrag { get; set; }
        public CardAction DefaultAction { get; set; }
        public UnityAction SecondaryDragAction { get; set; }
        public CardDropArea DropTarget { get; set; }

        public PointerEventData CurrentPointerEventData { get; private set; }

        public Dictionary<int, Vector2> PointerPositions { get; } = new Dictionary<int, Vector2>();
        private Dictionary<int, Vector2> PointerDragOffsets { get; } = new Dictionary<int, Vector2>();

        private float _holdTime;
        private bool _didSelectOnDown;
        private bool _didDrag;
        private DragPhase _currentDragPhase;

        [SyncVar(hook = nameof(OnChangePosition))]
        public Vector2 position;

        [SyncVar(hook = nameof(OnChangeRotation))]
        public Quaternion rotation;

        [field: SyncVar] public string Id { get; private set; }

        public UnityCard Value
        {
            get
            {
                if (string.IsNullOrEmpty(Id) ||
                    !CardGameManager.Current.Cards.TryGetValue(Id, out UnityCard cardValue))
                    return UnityCard.Blank;
                return cardValue;
            }
            set
            {
                Value.UnregisterDisplay(this);
                Id = value != null ? value.Id : string.Empty;
                gameObject.name = value != null ? "[" + value.Id + "] " + value.Name : string.Empty;
                value?.RegisterDisplay(this);
            }
        }

        [SyncVar(hook = nameof(OnChangeFacedown))]
        public bool isFacedown;

        public bool IsFacedown
        {
            set
            {
                if (IsOnline)
                    CmdUpdateFacedown(value);
                else
                {
                    bool oldValue = isFacedown;
                    isFacedown = value;
                    OnChangeFacedown(oldValue, isFacedown);
                }
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
                        WarnHighlight();
                    _placeHolderCardZone = null;
                }
                else
                    IsHighlighted = false;
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
                PlaceHolder.anchoredPosition = Vector2.zero;
            }
        }

        private CardZone _placeHolderCardZone;

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
                if (value)
                {
                    Highlight.effectColor = SelectedHighlightColor;
                    Highlight.effectDistance = OutlineHighlightDistance;
                }
                else
                {
                    bool isOthers = IsOnline && _isClientAuthorized && !hasAuthority;
                    Highlight.effectColor = isOthers ? Color.yellow : Color.black;
                    Highlight.effectDistance = isOthers ? OutlineHighlightDistance : Vector2.zero;
                }
            }
        }

        private Outline Highlight => _highlight ? _highlight : _highlight = GetComponent<Outline>();
        private Outline _highlight;

        private Image View => _view ? _view : _view = GetComponent<Image>();
        private Image _view;

        private CanvasGroup Visibility => _visibility ? _visibility : _visibility = GetComponent<CanvasGroup>();
        private CanvasGroup _visibility;

        [SyncVar] private bool _isClientAuthorized;

        private void Start()
        {
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((RectTransform) transform).sizeDelta = CardGameManager.PixelsPerInch * cardSize;
            gameObject.GetOrAddComponent<BoxCollider2D>().size = CardGameManager.PixelsPerInch * cardSize;

            if (IsOnline)
            {
                if (Vector2.zero != position)
                    ((RectTransform) transform).anchoredPosition = position;
                if (Quaternion.identity != rotation)
                    transform.rotation = rotation;
            }

            IsNameVisible = !isFacedown;
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
            IsNameVisible = false;
        }

        private void RemoveImageSprite()
        {
            View.sprite = CardGameManager.Current.CardBackImageSprite;
            if (!isFacedown)
                IsNameVisible = true;
        }

        private void Update()
        {
            if (PointerPositions.Count > 0 && !_didDrag)
                _holdTime += Time.deltaTime;
            else
                _holdTime = 0;

            if (_holdTime > ZoomHoldTime)
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

        public void OnPointerDown(PointerEventData eventData)
        {
            _didSelectOnDown =
                eventData.button != PointerEventData.InputButton.Middle && eventData.button !=
                                                                        PointerEventData.InputButton.Right
                                                                        && CardViewer.Instance.SelectedCardModel !=
                                                                        this && CardViewer.Instance.WasVisible;
            if (_didSelectOnDown && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            CurrentPointerEventData = eventData;

            PointerPositions[eventData.pointerId] = eventData.position;
            PointerDragOffsets[eventData.pointerId] = (Vector2) transform.position - eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (CurrentPointerEventData != null && CurrentPointerEventData.pointerId == eventData.pointerId &&
                !eventData.dragging && eventData.button != PointerEventData.InputButton.Middle &&
                eventData.button != PointerEventData.InputButton.Right)
            {
                if (!_didSelectOnDown && EventSystem.current.currentSelectedGameObject == gameObject &&
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

            CurrentPointerEventData = eventData;
            if (_currentDragPhase == DragPhase.Drag)
                return;

            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
            if (_didDrag && PointerDragOffsets.Count == 0)
                _didDrag = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !isFacedown)
                CardViewer.Instance.SelectedCardModel = this;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !CardViewer.Instance.Zoom)
                CardViewer.Instance.IsVisible = false;
        }

        public static void CreateDrag(PointerEventData eventData, GameObject gameObject, Transform transform,
            UnityCard value, bool isFacedown, CardZone placeHolderCardZone = null)
        {
            Vector3 position = transform.position;
            GameObject newGameObject = Instantiate(gameObject, position, transform.rotation,
                transform.gameObject.FindInParents<Canvas>().transform);
            eventData.pointerPress = newGameObject;
            eventData.pointerDrag = newGameObject;
            var cardModel = newGameObject.GetOrAddComponent<CardModel>();
            cardModel.Visibility.blocksRaycasts = false;
            cardModel.IsHighlighted = false;
            cardModel.Value = value;
            cardModel.IsFacedown = isFacedown;
            cardModel.PlaceHolderCardZone = placeHolderCardZone;
            cardModel.DoesCloneOnDrag = false;
            cardModel.PointerDragOffsets[eventData.pointerId] = (Vector2) position - eventData.position;
            cardModel.OnBeginDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _didDrag = true;
            if (DoesCloneOnDrag)
            {
                if (!IsOnline)
                    NetworkServer.UnSpawn(gameObject); // Avoid Mirror error
                CreateDrag(eventData, gameObject, transform, Value, isFacedown);
                return;
            }

            EventSystem.current.SetSelectedGameObject(null, eventData);
            CurrentPointerEventData = eventData;
            _currentDragPhase = DragPhase.Begin;
            PointerPositions[eventData.pointerId] = eventData.position;

            if (!IsOnline)
                ActOnDrag();
            else
                CmdTransferAuthority();
        }

        public void OnDrag(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            _currentDragPhase = DragPhase.Drag;
            PointerPositions[eventData.pointerId] = eventData.position;

            if (!IsOnline || hasAuthority)
                ActOnDrag();
            else
                CmdTransferAuthority();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            _currentDragPhase = DragPhase.End;

            if (!IsOnline || hasAuthority)
                ActOnDrag();

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

            if (DropTarget != null &&
                (ParentCardZone != null && ParentCardZone.type == CardZoneType.Area
                 || PlaceHolderCardZone != null && PlaceHolderCardZone.type == CardZoneType.Area))
            {
                var shouldDiscard = true;
                if (Visibility.blocksRaycasts)
                {
                    List<RaycastResult> hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, hits);
                    bool pointerOverDropTarget = hits.Any(hit => hit.gameObject == DropTarget.gameObject);
                    if (pointerOverDropTarget)
                        DropTarget.OnDrop(eventData);
                    else
                        shouldDiscard = false;
                }

                if (!shouldDiscard)
                    return;

                if (IsOnline)
                    CmdUnspawnCard();
                Discard();
                return;
            }

            if (IsOnline)
                CmdReleaseAuthority();

            DropTarget = null;

            if (PlaceHolder != null)
                StartCoroutine(MoveToPlaceHolder());
            else if (ParentCardZone == null)
                Discard();
        }

        public static CardModel GetPointerDrag(PointerEventData eventData)
        {
            return eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CardModel>();
        }

        [Command(ignoreAuthority = true)]
        private void CmdTransferAuthority(NetworkConnectionToClient sender = null)
        {
            if (sender == null || netIdentity.connectionToClient != null)
            {
                Debug.Log("Ignoring request to transfer authority, as it is already transferred");
                return;
            }

            _isClientAuthorized = true;
            netIdentity.AssignClientAuthority(sender);
        }

        private void ActOnDrag()
        {
            UpdatePosition();
            if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
                SecondaryDragAction();
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

            IsHighlighted = _currentDragPhase != DragPhase.End;
            Highlight.effectColor = DropTarget != null || PlaceHolder != null || ParentCardZone != null
                ? Color.green
                : Color.red;

            Vector2 targetPosition = UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList());
            targetPosition += UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());
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
                CmdUpdatePosition(((RectTransform) transform).anchoredPosition);
        }

        private void UpdateCardZonePosition(Vector2 targetPosition)
        {
            CardZone cardZone = ParentCardZone;
            if (cardZone == null || (IsOnline && !hasAuthority))
                return;

            if (!cardZone.DoesImmediatelyRelease &&
                (cardZone.type == CardZoneType.Vertical || cardZone.type == CardZoneType.Horizontal))
                cardZone.UpdateScrollRect(_currentDragPhase, CurrentPointerEventData);
            else if (!IsStatic)
                cardZone.UpdateLayout(transform as RectTransform, targetPosition);

            if (IsStatic)
                return;

            if (cardZone.type == CardZoneType.Area)
                transform.SetAsLastSibling();

            Vector3[] zoneCorners = new Vector3[4];
            ((RectTransform) cardZone.transform).GetWorldCorners(zoneCorners);
            bool isOutYBounds = targetPosition.y < zoneCorners[0].y || targetPosition.y > zoneCorners[1].y;
            bool isOutXBounds = targetPosition.x < zoneCorners[0].x || targetPosition.y > zoneCorners[2].x;
            if ((cardZone.DoesImmediatelyRelease && !IsProcessingSecondaryDragAction)
                || (cardZone.type == CardZoneType.Vertical && isOutXBounds)
                || (cardZone.type == CardZoneType.Horizontal && isOutYBounds)
                || (cardZone.type == CardZoneType.Area
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
            CardZone prevParentZone = ParentCardZone;
            if (_currentDragPhase == DragPhase.Drag)
                prevParentZone.UpdateScrollRect(DragPhase.End, CurrentPointerEventData);
            transform.SetParent(CardGameManager.Instance.CardCanvas.transform);
            transform.SetAsLastSibling();
            if (prevParentZone != null)
                prevParentZone.OnRemove(this);
            Visibility.blocksRaycasts = false;
            var rectTransform = (RectTransform) transform;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.pivot = 0.5f * Vector2.one;
            rectTransform.position = targetPosition;
            rectTransform.localScale = Vector3.one;
        }

        [Command]
        private void CmdReleaseAuthority()
        {
            netIdentity.RemoveClientAuthority();
            _isClientAuthorized = false;
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

            CardZone prevParentZone = ParentCardZone;
            Transform transform1 = transform;
            transform1.SetParent(PlaceHolder.parent);
            transform1.SetSiblingIndex(PlaceHolder.GetSiblingIndex());
            transform1.localScale = Vector3.one;
            if (prevParentZone != null)
                prevParentZone.OnRemove(this);
            if (ParentCardZone != null)
                ParentCardZone.OnAdd(this);
            PlaceHolder = null;
            Visibility.blocksRaycasts = true;
        }

        public void UpdateParentCardZoneScrollRect()
        {
            CardZone cardZone = ParentCardZone;
            if (cardZone != null)
                cardZone.UpdateScrollRect(_currentDragPhase, CurrentPointerEventData);
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

        [Command(ignoreAuthority = true)]
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

        [Command(ignoreAuthority = true)]
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

        private void WarnHighlight()
        {
            Highlight.effectColor = Color.red;
            Highlight.effectDistance = OutlineHighlightDistance;
        }

        [ClientRpc]
        public void RpcHideHighlight()
        {
            IsHighlighted = false;
        }

        private void Discard()
        {
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
