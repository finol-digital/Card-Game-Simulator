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

    public class CardZone : CgsNetPlayable, ICardContainer
    {
        public GameObject countLabelPrefab;

        public CardZoneType type;
        public bool allowsFlip;
        public bool allowsRotation;
        public ScrollRect scrollRectContainer;

        private Text _countLabel;
        private int _countLabelCardCount = -1;

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

        public string Name
        {
            get => IsSpawned ? _nameNetworkVariable.Value : _name;
            set
            {
                _name = value;
                if (IsSpawned)
                    _nameNetworkVariable.Value = value;
            }
        }

        private string _name = string.Empty;
        private NetworkVariable<CgsNetString> _nameNetworkVariable;

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

        // 0 means cards keep their rotation when added, matching CardRotationDefault
        public float DefaultCardRotation
        {
            get => IsSpawned ? _defaultCardRotationNetworkVariable.Value : _defaultCardRotation;
            set
            {
                _defaultCardRotation = value;
                if (IsSpawned)
                    _defaultCardRotationNetworkVariable.Value = value;
            }
        }

        private float _defaultCardRotation;
        private NetworkVariable<float> _defaultCardRotationNetworkVariable;

        public bool DoesImmediatelyRelease { get; set; }

        public UnityAction OnLayout { get; set; }

        public List<OnAddCardDelegate> OnAddCardActions { get; } = new();
        public List<OnRemoveCardDelegate> OnRemoveCardActions { get; } = new();

        protected override void OnAwakePlayable()
        {
            _typeNetworkVariable = new NetworkVariable<int>();
            _nameNetworkVariable = new NetworkVariable<CgsNetString>();
            _facePreferenceNetworkVariable = new NetworkVariable<int>();
            _cardActionNetworkVariable = new NetworkVariable<int>();
            _defaultCardRotationNetworkVariable = new NetworkVariable<float>();
        }

        protected override void OnNetworkSpawnPlayable()
        {
            if (IsServer)
            {
                _typeNetworkVariable.Value = (int)type;
                _nameNetworkVariable.Value = _name;
                _facePreferenceNetworkVariable.Value = (int)_facePreference;
                _cardActionNetworkVariable.Value = (int)_cardAction;
                _defaultCardRotationNetworkVariable.Value = _defaultCardRotation;
            }

            type = (CardZoneType)_typeNetworkVariable.Value;
            _name = _nameNetworkVariable.Value;
            _facePreference = (FacePreference)_facePreferenceNetworkVariable.Value;
            _cardAction = (CardAction)_cardActionNetworkVariable.Value;
            _defaultCardRotation = _defaultCardRotationNetworkVariable.Value;
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

            CreateCountLabel();

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
                if (cardZone.DefaultCardRotation != 0)
                {
                    // Set the transform directly so this rotation does not animate,
                    // since MoveCardToServer may immediately read the final rotation from the transform
                    cardModel.transform.localRotation = Quaternion.Euler(0, 0, cardZone.DefaultCardRotation);
                    cardModel.Rotation = cardModel.transform.localRotation;
                }
            });

            StartCoroutine(WaitToAddMoveCardToServer());
        }

        // Show the count of cards in this zone, like the count labels of card stacks and dice zones
        private void CreateCountLabel()
        {
            if (countLabelPrefab == null)
                return;

            var countLabelGameObject = Instantiate(countLabelPrefab, transform);
            // The label is not a card, so the layout group and the card raycasts must both ignore it
            countLabelGameObject.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
            var countLabelRectTransform = (RectTransform)countLabelGameObject.transform;
            countLabelRectTransform.anchorMin = new Vector2(0.5f, 0);
            countLabelRectTransform.anchorMax = new Vector2(0.5f, 0);
            countLabelRectTransform.pivot = new Vector2(0.5f, 0);
            countLabelRectTransform.anchoredPosition = new Vector2(0, -60);
            countLabelRectTransform.sizeDelta = new Vector2(100, 60);
            _countLabel = countLabelGameObject.GetComponent<Text>();
            _countLabel.raycastTarget = false;
            _countLabel.text = "0";
        }

        protected override void OnUpdatePlayable()
        {
            if (_countLabel == null)
                return;

            var cardCount = 0;
            for (var i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).TryGetComponent<CardModel>(out _))
                    cardCount++;
            if (cardCount != _countLabelCardCount)
            {
                _countLabelCardCount = cardCount;
                _countLabel.text = cardCount.ToString();
            }

            // Cards in this zone are ordered by sibling index, which is synced across the network,
            // so the label must stay last to keep card sibling indices consistent on all clients
            var countLabelTransform = _countLabel.transform;
            if (countLabelTransform.GetSiblingIndex() != transform.childCount - 1)
                countLabelTransform.SetAsLastSibling();
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

        public void AddCard(Card card)
        {
            AddCard(card, false);
        }

        public void AddCard(Card card, bool isFacedown)
        {
            // Apply this zone's face preference, same as when a card is dragged and dropped into the zone
            isFacedown = DefaultFace switch
            {
                FacePreference.Down => true,
                FacePreference.Up => false,
                _ => isFacedown
            } && !card.IsBackFaceCard;

            var isCardShared = SharePreference.Share == CardGameManager.Current.DeckSharePreference;
            var rotation = Quaternion.Euler(0, 0, DefaultCardRotation);
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewCardInZone(this, card.Id, Vector2.zero,
                    rotation, isFacedown, isCardShared);
            else
            {
                var cardModel = PlayController.Instance.CreateCardModel(gameObject, card.Id, Vector2.zero,
                    rotation, isFacedown, isCardShared);
                OnAdd(cardModel);
            }
        }

        public void OnAdd(CardModel cardModel)
        {
            if (cardModel == null)
                return;

            // A card added over a nested card zone belongs in that zone, not loose in this area
            if (Type == CardZoneType.Area && cardModel.MoveToOverlappingCardZone())
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
