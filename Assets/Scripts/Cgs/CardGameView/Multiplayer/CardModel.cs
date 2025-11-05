/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using Cgs.UI.ScrollRects;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
{
    public class CardModel : CgsNetPlayable, ICardDisplay, ICardDropHandler, IStackDropHandler, IMaterialModifier
    {
        public const string DropErrorMessage = "Error: Card dropped on Card outside of play area!";
        public override string DeletePrompt => $"Delete cannot be undone. Delete {gameObject.name}?";

        private const float ZoomHoldTime = 0.5f;
        private const float MovementSpeed = 600f;

        public int Index { get; set; }

        public bool IsStatic { get; set; }
        public bool DoesCloneOnDrag { get; set; }
        public CardAction DefaultAction { get; set; }
        public UnityAction SecondaryDragAction { get; set; }
        public CardDropArea DropTarget { get; set; }

        public string Id
        {
            get => IsSpawned ? _idNetworkVariable.Value : _id;
            private set
            {
                var oldValue = _id;
                _id = value;
                if (IsSpawned)
                    _idNetworkVariable.Value = value;
                else if (oldValue != _id)
                    OnChangeId(oldValue, _id);
            }
        }

        private string _id = UnityCard.Blank.Id;
        private NetworkVariable<CgsNetString> _idNetworkVariable;

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
                Id = value?.Id ?? UnityCard.Blank.Id;
                gameObject.name = $"[{Id}] {value?.Name}";
                if (!IsFacedown)
                    value?.RegisterDisplay(this);
            }
        }

        public bool IsFacedown
        {
            get => _isFacedown;
            set
            {
                var oldValue = _isFacedown;
                _isFacedown = value;
                if (IsSpawned)
                    SetIsFacedownServerRpc(_isFacedown);
                else if (oldValue != _isFacedown)
                    OnChangeIsFacedown(oldValue, _isFacedown);
            }
        }

        private bool _isFacedown;
        private NetworkVariable<bool> _isFacedownNetworkVariable;

        private Sprite CardBackImageSprite
        {
            get
            {
                var backFaceId = Value.BackFaceId;
                if (!string.IsNullOrEmpty(backFaceId) &&
                    CardGameManager.Current.CardBackFaceImageSprites.TryGetValue(backFaceId,
                        out var backFaceImageSprite))
                    return backFaceImageSprite;
                return CardGameManager.Current.CardBackImageSprite;
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
                PlaceHolder = (RectTransform)placeholder.transform;
                PlaceHolder.SetParent(_placeHolderCardZone.transform);
                PlaceHolder.sizeDelta = ((RectTransform)transform).sizeDelta;
                PlaceHolder.localPosition = Vector2.zero;
            }
        }

        private CardZone _placeHolderCardZone;

        private bool IsMovingToPlaceHolder { get; set; }

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
        private Material _material;
        private int IsSelectedPropertyId => _isSelectedPropertyId ??= Shader.PropertyToID("_IsSelected");
        private int? _isSelectedPropertyId;

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            var material = new Material(baseMaterial);
            material.SetFloat(IsSelectedPropertyId,
                EventSystem.current.currentSelectedGameObject == gameObject ? 1 : 0);
            return material;
        }

        protected override void OnAwakePlayable()
        {
            _idNetworkVariable = new NetworkVariable<CgsNetString>();
            _idNetworkVariable.OnValueChanged += OnChangeId;
            _isFacedownNetworkVariable = new NetworkVariable<bool>();
            _isFacedownNetworkVariable.OnValueChanged += OnChangeIsFacedown;
        }

        protected override void OnNetworkSpawnPlayable()
        {
            _isFacedown = _isFacedownNetworkVariable.Value;

            if (string.IsNullOrEmpty(_idNetworkVariable.Value))
                return;

            var oldId = _id;
            _id = _idNetworkVariable.Value;
            OnChangeId(oldId, _id);
        }

        protected override void OnStartPlayable()
        {
            if (PlayController.Instance != null &&
                PlayController.Instance.playAreaCardZone.transform == transform.parent)
            {
                PlayController.SetPlayAreaActions(this);
                var cardDropArea = gameObject.GetOrAddComponent<CardDropArea>();
                cardDropArea.isBlocker = true;
                cardDropArea.DropHandler = this;

                var stackDropArea = gameObject.GetOrAddComponent<StackDropArea>();
                stackDropArea.DropHandler = this;
            }

            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((RectTransform)transform).sizeDelta = CardGameManager.PixelsPerInch * cardSize;
            gameObject.GetOrAddComponent<BoxCollider2D>().size = CardGameManager.PixelsPerInch * cardSize;

            if (CgsNetManager.Instance == null || !CgsNetManager.Instance.IsOnline ||
                (CgsNetManager.Instance.IsServer && !IsSpawned))
            {
                var targetRotation = Value.GetPropertyValueInt(CardGameManager.Current.CardRotationIdentifier);
                if (targetRotation == 0)
                    targetRotation = CardGameManager.Current.CardRotationDefault;
                if (targetRotation != 0)
                    Rotation = Quaternion.Euler(0, 0, targetRotation);
            }

            SetIsNameVisible(!IsFacedown && !IsSpawned);
            if (!IsFacedown && !IsSpawned)
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
            View.sprite = CardBackImageSprite;
            if (!IsFacedown && !IsSpawned)
                SetIsNameVisible(true);
        }

        protected override void OnUpdatePlayable()
        {
            if (InputManager.IsOption && CardViewer.Instance.PreviewCardModel == this || HoldTime > ZoomHoldTime)
                RequestZoomOnThis();

            UpdateCheckForPlaceHolder();

            UpdateForMovingToPlaceHolder();
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
                eventData.dragging || eventData.button is PointerEventData.InputButton.Middle)
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
            if (CardViewer.Instance == null)
                return;

            if (Settings.PreviewOnMouseOver && !CardViewer.Instance.IsVisible
                                            && !(PlayableViewer.Instance != null && PlayableViewer.Instance.IsVisible)
                                            && CurrentDragPhase != DragPhase.Drag && !IsFacedown)
                CardViewer.Instance.PreviewCardModel = this;
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            if (CardViewer.Instance == null)
                return;

            CardViewer.Instance.HidePreview();
            if (CardViewer.Instance.PreviewCardModel == this)
                CardViewer.Instance.PreviewCardModel = null;
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            if (CardViewer.Instance != null)
                CardViewer.Instance.SelectedCardModel = this;
            View.material.SetFloat(IsSelectedPropertyId, 1);
            View.SetMaterialDirty();
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            if (CardViewer.Instance != null && !CardViewer.Instance.Zoom)
                CardViewer.Instance.IsVisible = false;
            View.material.SetFloat(IsSelectedPropertyId, 0);
            View.SetMaterialDirty();
        }

        public void OnDrop(CardModel cardModel)
        {
            if (cardModel == this || (_placeHolder != null && cardModel == _placeHolder.GetComponent<CardModel>()))
                return;

            Debug.Log($"Dropped {cardModel.gameObject.name} on {gameObject.name}");

            if (!PlaySettings.AutoStackCards)
            {
                Debug.Log(" Ignoring drop request because PlaySettings.AutoStackCards is false.");
                return;
            }

            if (CgsNetManager.Instance == null || PlayController.Instance == null)
            {
                Debug.LogError(DropErrorMessage);
                CardGameManager.Instance.Messenger.Show(DropErrorMessage);
                return;
            }

            var cards = new List<UnityCard> { Value, cardModel.Value };
            if (IsSpawned)
                CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(PlayController.DefaultStackName, cards,
                    Position, Rotation, !IsFacedown);
            else
                PlayController.Instance.CreateCardStack(PlayController.DefaultStackName, cards, Position, Rotation,
                    !IsFacedown);

            Debug.Log($"OnDrop {cardModel.gameObject.name} on {gameObject.name} delete");
            cardModel.RequestDelete();
            RequestDelete();
        }

        public void OnDrop(CardStack cardStack)
        {
            cardStack.RequestInsert(0, Id);
            RequestDelete();
        }

        public static CardModel CreateDrag(PointerEventData eventData, GameObject gameObject, Transform transform,
            UnityCard value, bool isFacedown, CardZone placeHolderCardZone = null)
        {
            var position = transform.position;
            var newGameObject = Instantiate(gameObject, position, transform.rotation,
                CardGameManager.Instance.CardCanvas.transform);
            eventData.pointerPress = newGameObject;
            eventData.pointerDrag = newGameObject;
            var cardModel = newGameObject.GetOrAddComponent<CardModel>();
            cardModel.Visibility.blocksRaycasts = false;
            cardModel.HighlightMode = HighlightMode.Off;
            cardModel.Value = value;
            cardModel.IsFacedown = isFacedown;
            cardModel.PlaceHolderCardZone = placeHolderCardZone;
            cardModel.DoesCloneOnDrag = false;
            cardModel.PointerDragOffsets[eventData.pointerId] = (Vector2)position - eventData.position;
            cardModel.OnBeginDrag(eventData);
            return cardModel;
        }

        protected override bool PreBeginDrag(PointerEventData eventData)
        {
            DidDrag = true;
            if (DoesCloneOnDrag && !IsProcessingSecondaryDragAction)
            {
                if (!(Container != null && Container.transform == transform.parent) && IsSpawned)
                    MyNetworkObject.Despawn(false);
                CreateDrag(eventData, gameObject, transform, Value, IsFacedown);
                return true;
            }

            EventSystem.current.SetSelectedGameObject(null, eventData);
            return false;
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (CardViewer.Instance != null)
                CardViewer.Instance.HidePreview();

            if (LacksOwnership)
                RequestChangeOwnership();
            else
                ActOnDrag();
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (!LacksOwnership)
                ActOnDrag();
        }

        protected override void PostDragPlayable(PointerEventData eventData)
        {
            if (IsProcessingSecondaryDragAction)
            {
                Debug.Log("PostDragPlayable::IsProcessingSecondaryDragAction");
                return;
            }

            if (DropTarget != null && DropTarget.gameObject != gameObject)
            {
                var dropTargetCardModel = DropTarget.GetComponent<CardModel>();

                if (Visibility.blocksRaycasts && ParentCardZone != null && ParentCardZone.Type == CardZoneType.Area
                    || PlaceHolderCardZone != null && PlaceHolderCardZone.Type == CardZoneType.Area
                    || dropTargetCardModel != null && dropTargetCardModel.ParentCardZone != null &&
                    dropTargetCardModel.ParentCardZone.Type == CardZoneType.Area)
                {
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, hits);
                    var isPointerOverDropTarget = hits.Any(hit => hit.gameObject == DropTarget.gameObject);
                    if (isPointerOverDropTarget)
                    {
                        DropTarget.OnDrop(eventData);

                        var cardScrollArea = DropTarget.GetComponent<CardScrollArea>();
                        var shouldDeleteOnDropTarget = PlaySettings.AutoStackCards || dropTargetCardModel == null;

                        if (cardScrollArea != null)
                        {
                            Debug.Log("PostDragPlayable::cardScrollArea");
                        }
                        else if (shouldDeleteOnDropTarget)
                        {
                            Debug.Log("PostDragPlayable::shouldDeleteOnDropTarget");
                            RequestDelete();
                            return;
                        }
                    }
                }

                if (dropTargetCardModel != null && dropTargetCardModel.ParentCardZone != null &&
                    dropTargetCardModel.ParentCardZone.Type == CardZoneType.Area)
                {
                    PlaceHolderCardZone = dropTargetCardModel.ParentCardZone;
                    PlaceHolderCardZone.UpdateLayout(PlaceHolder, transform.position);
                }
            }

            if (DropTarget == null && ParentCardZone == null && PlaceHolderCardZone == null &&
                CgsNetManager.Instance != null && PlayController.Instance != null)
            {
                PlaceHolderCardZone = PlayController.Instance.playAreaCardZone;
                PlaceHolderCardZone.UpdateLayout(PlaceHolder, transform.position);
            }

            DropTarget = null;

            if (PlaceHolder != null)
                IsMovingToPlaceHolder = true;
            else if (ParentCardZone == null)
            {
                Debug.LogWarning($"PostDragPlayable Lost PlaceHolder and ParentCardZone for {gameObject.name}!");
                RecoverLostPlaceholder();
            }
            else if (ParentCardZone.Type == CardZoneType.Area)
                SnapToGrid();
        }

        public static CardModel GetPointerDrag(PointerEventData eventData)
        {
            return eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CardModel>();
        }

        protected override void ActOnDrag()
        {
            UpdatePosition();
            if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
                SecondaryDragAction();
        }

        public override void SnapToGrid()
        {
            if (ToDelete)
                return;

            var gridPosition = CalculateGridPosition();

            if (PlaySettings.AutoStackCards && PlayController.Instance != null)
            {
                var playAreaCardZoneTransform = PlayController.Instance.playAreaCardZone.transform;
                for (var i = 0; i < playAreaCardZoneTransform.childCount; i++)
                {
                    var siblingTransform = playAreaCardZoneTransform.GetChild(i);
                    if (siblingTransform == transform)
                        continue;

                    var distance = Vector2.Distance(siblingTransform.position, gridPosition);
                    if (distance > 0.1f)
                        continue;

                    var siblingCardModel = siblingTransform.GetComponent<CardModel>();
                    if (siblingCardModel != null)
                    {
                        var cards = new List<UnityCard> { siblingCardModel.Value, Value };
                        if (IsSpawned)
                            CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(PlayController.DefaultStackName,
                                cards, siblingCardModel.Position, siblingCardModel.Rotation,
                                !siblingCardModel.IsFacedown);
                        else
                            PlayController.Instance.CreateCardStack(PlayController.DefaultStackName,
                                cards, siblingCardModel.Position, siblingCardModel.Rotation,
                                !siblingCardModel.IsFacedown);
                        siblingCardModel.RequestDelete();
                        RequestDelete();
                        return;
                    }

                    var siblingCardStack = siblingTransform.GetComponent<CardStack>();
                    if (siblingCardStack == null)
                        continue;

                    siblingCardStack.OnDrop(this);
                    RequestDelete();
                    return;
                }
            }

            base.SnapToGrid();
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

            var rectTransform = (RectTransform)transform;
            if (ParentCardZone != null)
                UpdateCardZonePosition(targetPosition);
            else if (!IsStatic)
                rectTransform.position = targetPosition;

            if (IsStatic)
                return;

            if (DropTarget != null && DropTarget.isBlocker && DropTarget.gameObject != gameObject
                && ParentCardZone != null)
                ParentToCanvas(targetPosition);

            // Check for card zones
            if (PlayController.Instance != null && ParentCardZone == PlayController.Instance.playAreaCardZone &&
                PlaceHolderCardZone == null)
            {
                foreach (var cardZone in PlayController.Instance.AllCardZones)
                {
                    if (cardZone == PlayController.Instance.playAreaCardZone)
                        continue;
                    if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)cardZone.transform,
                            CurrentPointerEventData.position))
                        PlaceHolderCardZone = cardZone;
                }
            }
            else if (PlaceHolderCardZone != null
                     && PlaceHolderCardZone.transform.parent == PlayController.Instance?.playAreaCardZone.transform
                     && !RectTransformUtility.RectangleContainsScreenPoint(
                         (RectTransform)PlaceHolderCardZone.transform,
                         CurrentPointerEventData.position))
                PlaceHolderCardZone = null;
            // end check

            if (PlaceHolderCardZone != null)
                PlaceHolderCardZone.UpdateLayout(PlaceHolder, targetPosition);

            if (IsSpawned && IsOwner)
                RequestUpdatePosition(rectTransform.localPosition);
        }

        private void UpdateCardZonePosition(Vector2 targetPosition)
        {
            var cardZone = ParentCardZone;
            if (cardZone == null || LacksOwnership)
                return;

            if (!cardZone.DoesImmediatelyRelease && cardZone.Type is CardZoneType.Vertical or CardZoneType.Horizontal)
                cardZone.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
            else if (!IsStatic)
                cardZone.UpdateLayout(transform as RectTransform, targetPosition);

            if (IsStatic)
                return;

            if (cardZone.Type == CardZoneType.Area)
                transform.SetAsLastSibling();

            var zoneCorners = new Vector3[4];
            ((RectTransform)cardZone.transform).GetWorldCorners(zoneCorners);
            var isOutYBounds = targetPosition.y < zoneCorners[0].y || targetPosition.y > zoneCorners[1].y;
            var isOutXBounds = targetPosition.x < zoneCorners[0].x || targetPosition.y > zoneCorners[2].x;
            if ((cardZone.DoesImmediatelyRelease && !IsProcessingSecondaryDragAction)
                || (cardZone.Type == CardZoneType.Vertical && isOutXBounds)
                || (cardZone.Type == CardZoneType.Horizontal && isOutYBounds)
                || (cardZone.Type == CardZoneType.Area && PlaceHolder != null &&
                    PlaceHolder.parent.parent !=
                    PlayController.Instance.playAreaCardZone.transform)
               ) // Assumes we are in play area and card zones are siblings
                ParentToCanvas(targetPosition);
        }

        public void UpdateParentCardZoneScrollRect()
        {
            var cardZone = ParentCardZone;
            if (cardZone != null)
                cardZone.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
        }

        private void UpdateCheckForPlaceHolder()
        {
            if (ParentCardZone != null || CurrentDragPhase != DragPhase.End)
                return;

            if (PlaceHolder == null)
            {
                Debug.LogWarning("UpdateCheckForPlaceHolder: PlaceHolder == null");
                RecoverLostPlaceholder();
            }

            IsMovingToPlaceHolder = true;
        }

        private void UpdateForMovingToPlaceHolder()
        {
            if (!IsMovingToPlaceHolder)
                return;

            if (PlaceHolder != null && Vector3.Distance(transform.position, PlaceHolder.position) > 1)
            {
                var distance = MovementSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, PlaceHolder.position, distance);
            }
            else
                FinishMovingToPlaceHolder();
        }

        private void RecoverLostPlaceholder()
        {
            if (PlayController.Instance == null)
            {
                Debug.LogWarning("RecoverLostPlaceholder: PlayController.Instance == null");
                RequestDelete();
                return;
            }

            PlaceHolderCardZone = PlayController.Instance.playAreaCardZone;
            if (!Vector2.zero.Equals(Position))
                PlaceHolder.localPosition = Position;
        }

        private void FinishMovingToPlaceHolder()
        {
            if (PlaceHolder == null)
            {
                Debug.LogWarning($"FinishMovingToPlaceHolder Lost PlaceHolder for {gameObject.name}!");
                RecoverLostPlaceholder();
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

            PlaceHolderCardZone = null;
            Visibility.blocksRaycasts = true;
            IsMovingToPlaceHolder = false;
        }

        private void ParentToCanvas(Vector3 targetPosition)
        {
            Debug.Log($"ParentToCanvas {gameObject.name}");

            var cardDropArea = GetComponent<CardDropArea>();
            if (cardDropArea != null)
                Destroy(cardDropArea);

            var previousParentCardZone = ParentCardZone;
            if (CurrentDragPhase == DragPhase.Drag)
                previousParentCardZone.UpdateScrollRect(DragPhase.End, CurrentPointerEventData);
            transform.SetParent(CardGameManager.Instance.CardCanvas.transform);
            transform.SetAsLastSibling();
            if (previousParentCardZone != null)
                previousParentCardZone.OnRemove(this);
            Visibility.blocksRaycasts = false;
            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMax = 0.5f * Vector2.one;
            rectTransform.anchorMin = 0.5f * Vector2.one;
            rectTransform.pivot = 0.5f * Vector2.one;
            rectTransform.position = targetPosition;
            rectTransform.localScale = Vector3.one;

            CreateDrag(CurrentPointerEventData, gameObject, transform, Value, IsFacedown, PlaceHolderCardZone);

            if (IsSpawned)
                DeleteServerRpc();
            else
                Destroy(gameObject);
        }

        [PublicAPI]
        public void OnChangeId(CgsNetString oldValue, CgsNetString newValue)
        {
            Value.UnregisterDisplay(this);
            _id = newValue;
            if (string.IsNullOrEmpty(_id) || !CardGameManager.Current.Cards.TryGetValue(_id, out var unityCard) ||
                unityCard == null)
            {
                Debug.LogError($"ERROR: Id for {gameObject.name} changed to unknown card!");
                return;
            }

            gameObject.name = $"[{_id}] {unityCard.Name}";
            if (!IsFacedown)
                unityCard.RegisterDisplay(this);
            if (CardViewer.Instance != null && CardViewer.Instance.IsVisible &&
                CardViewer.Instance.SelectedCardModel == this)
                CardViewer.Instance.SelectedCardModel = this;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetIsFacedownServerRpc(bool isFacedown)
        {
            _isFacedownNetworkVariable.Value = isFacedown;
        }

        [PublicAPI]
        public void OnChangeIsFacedown(bool oldValue, bool newValue)
        {
            _isFacedown = newValue;
            if (!IsFacedown)
                Value.RegisterDisplay(this);
            else
                Value.UnregisterDisplay(this);
        }

        public override void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, RequestDelete);
        }

        public override void OnDestroy()
        {
            if (CardGameManager.IsQuitting)
                return;

            Value.UnregisterDisplay(this);
            if (PlaceHolder != null)
                Destroy(PlaceHolder.gameObject);

            base.OnDestroy();
        }
    }
}
