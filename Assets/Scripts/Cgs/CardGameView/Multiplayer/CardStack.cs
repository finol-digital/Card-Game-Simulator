/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cgs.CardGameView.Viewer;
using Cgs.Decks;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;
using Random = System.Random;

namespace Cgs.CardGameView.Multiplayer
{
    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random _local;

        private static Random ThisThreadsRandom => _local ??= new Random(
            unchecked(Environment.TickCount * 31 + Thread
                .CurrentThread.ManagedThreadId));

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = ThisThreadsRandom.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }

    public class CardStack : CgsNetPlayable, ICardDisplay, ICardDropHandler, IStackDropHandler
    {
        private const float DragHoldTime = 0.5f;
        public const string ShuffleText = "Shuffled!";
        public const string SaveText = "Saved";

        private const string SaveDelimiter = "_";

        public string ShufflePrompt => $"Shuffle {deckLabel.text}?";
        public string SavePrompt => $"Save {deckLabel.text}?";
        public override string DeletePrompt => $"Delete {deckLabel.text}?";

        private bool IsDraggingCard => HoldTime < DragHoldTime && PointerPositions.Count == 1 &&
                                       CurrentPointerEventData != null &&
                                       CurrentPointerEventData.button != PointerEventData.InputButton.Middle &&
                                       CurrentPointerEventData.button != PointerEventData.InputButton.Right
                                       || PointerPositions.Count > 1;

        private bool IsDraggingStack => HoldTime >= DragHoldTime || CurrentPointerEventData is
            {button: PointerEventData.InputButton.Middle};

        protected override bool IsProcessingSecondaryDragAction =>
            !IsDraggingStack && base.IsProcessingSecondaryDragAction;

        public GameObject stackViewerPrefab;
        public GameObject cardModelPrefab;

        public Text deckLabel;
        public Text countLabel;
        public Text actionLabel;
        public Image topCard;

        public string Name
        {
            get => IsSpawned ? _nameNetworkVariable.Value : _name;
            set
            {
                var oldValue = _name;
                _name = value;
                if (IsSpawned)
                    _nameNetworkVariable.Value = value;
                else if (oldValue != _name)
                    OnChangeName(oldValue, _name);
            }
        }

        private string _name = string.Empty;
        private NetworkVariable<CgsNetString> _nameNetworkVariable;

        public override string ViewValue => Name;

        [NotNull]
        public IReadOnlyList<UnityCard> Cards
        {
            get
            {
                if (CgsNetManager.Instance == null || !CgsNetManager.Instance.IsOnline)
                    return _cards;

                _cards = new List<UnityCard>();
                foreach (var cardId in _cardIds)
                    _cards.Add(CardGameManager.Current.Cards[cardId]);
                return _cards;
            }
            set
            {
                _cards = new List<UnityCard>();
                foreach (var unityCard in value)
                    _cards.Add(unityCard);

                if (!CgsNetManager.Instance.IsOnline)
                    return;

                if (!CgsNetManager.Instance.IsConnectedClient || IsOwner)
                {
                    _cardIds.Clear();
                    foreach (var card in value.Select(card => card.Id).ToArray())
                        _cardIds.Add(card);
                }
                else
                    UpdateCardsServerRpc(value.Select(card => (CgsNetString) card.Id).ToArray());
            }
        }

        private List<UnityCard> _cards = new();
        private NetworkList<CgsNetString> _cardIds;

        private NetworkVariable<CgsNetString> _actionText;
        private NetworkVariable<float> _actionTime;

        private UnityCard TopCard
        {
            get
            {
                var cards = Cards;
                return cards.Count > 0 ? cards[^1] : null;
            }
        }

        public bool IsTopFaceup
        {
            get => _isTopFaceup;
            set
            {
                var oldValue = _isTopFaceup;
                _isTopFaceup = value;
                if (IsSpawned)
                    SetIsTopFaceupServerRpc(_isTopFaceup);
                else if (oldValue != _isTopFaceup)
                    OnChangeIsTopFaceup(oldValue, _isTopFaceup);
            }
        }

        private bool _isTopFaceup;
        private NetworkVariable<bool> _isTopFaceupNetworkVariable;

        private Sprite CardBackImageSprite
        {
            get
            {
                var backFaceId = Cards.LastOrDefault()?.BackFaceId;
                if (!string.IsNullOrEmpty(backFaceId) &&
                    CardGameManager.Current.CardBackFaceImageSprites.TryGetValue(backFaceId,
                        out var backFaceImageSprite))
                    return backFaceImageSprite;
                return CardGameManager.Current.CardBackImageSprite;
            }
        }

        public StackViewer Viewer { get; private set; }

        protected override void OnAwakePlayable()
        {
            _nameNetworkVariable = new NetworkVariable<CgsNetString>();
            _nameNetworkVariable.OnValueChanged += OnChangeName;

            _cardIds = new NetworkList<CgsNetString>();
            _cardIds.OnListChanged += OnCardsUpdated;

            _actionText = new NetworkVariable<CgsNetString>();
            _actionTime = new NetworkVariable<float>();

            _isTopFaceupNetworkVariable = new NetworkVariable<bool>();
            _isTopFaceupNetworkVariable.OnValueChanged += OnChangeIsTopFaceup;
        }

        protected override void OnNetworkSpawnPlayable()
        {
            _name = _nameNetworkVariable.Value;
            _cards = new List<UnityCard>();
            foreach (var cardId in _cardIds)
                _cards.Add(CardGameManager.Current.Cards[cardId]);
            _isTopFaceup = _isTopFaceupNetworkVariable.Value;
        }

        protected override void OnStartPlayable()
        {
            gameObject.GetOrAddComponent<CardDropArea>().DropHandler = this;
            gameObject.GetOrAddComponent<StackDropArea>().DropHandler = this;

            var rectTransform = (RectTransform) transform;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            rectTransform.sizeDelta = CardGameManager.PixelsPerInch * cardSize;
            rectTransform.localScale = Vector3.one;
            gameObject.GetOrAddComponent<BoxCollider2D>().size = CardGameManager.PixelsPerInch * cardSize;

            if (!IsOwner)
                deckLabel.text = Name;
            countLabel.text = Cards.Count.ToString();

            topCard.sprite = CardBackImageSprite;
            if (IsTopFaceup)
                TopCard?.RegisterDisplay(this);
        }

        public void SetImageSprite(Sprite imageSprite)
        {
            if (imageSprite == null)
            {
                RemoveImageSprite();
                return;
            }

            topCard.sprite = imageSprite;
        }

        private void RemoveImageSprite()
        {
            topCard.sprite = CardBackImageSprite;
        }

        protected override void OnUpdatePlayable()
        {
            var isAction = _actionTime.Value > 0;
            if (actionLabel.gameObject.activeSelf != isAction)
            {
                actionLabel.gameObject.SetActive(isAction);
                actionLabel.text = _actionText.Value;
            }

            if (isAction && (!CgsNetManager.Instance.IsConnectedClient || IsServer))
                _actionTime.Value -= Time.deltaTime;

            if (HoldTime > DragHoldTime)
                HighlightMode = HighlightMode.Authorized;
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            if (CurrentPointerEventData == null || CurrentPointerEventData.pointerId != eventData.pointerId ||
                eventData.dragging ||
                eventData.button is PointerEventData.InputButton.Middle or PointerEventData.InputButton.Right)
                return;

            if (PlaySettings.DoubleClickToViewStacks && EventSystem.current.currentSelectedGameObject == gameObject)
                View();
            else if (!EventSystem.current.alreadySelecting &&
                     EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (IsDraggingCard)
                DragCard(eventData);
            else if (LacksOwnership)
                RequestChangeOwnership();
            else
                ActOnDrag();
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            if (IsDraggingCard)
                return;

            if (LacksOwnership)
                RequestChangeOwnership();
            else
                ActOnDrag();
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (!IsDraggingCard && !LacksOwnership)
                ActOnDrag();
            Visibility.blocksRaycasts = true;
        }

        public static CardStack GetPointerDrag(PointerEventData eventData)
        {
            return eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CardStack>();
        }

        private void DragCard(PointerEventData eventData)
        {
            var cards = Cards;
            if (cards.Count < 1)
            {
                Debug.LogWarning("Attempted to remove from an empty card stack");
                return;
            }

            var unityCard = cards[^1];
            CardModel.CreateDrag(eventData, cardModelPrefab, transform, unityCard, !IsTopFaceup,
                PlayController.Instance.playAreaCardZone);
            RemovePointer(eventData);

            if (CgsNetManager.Instance.IsOnline && cards.Count > 1)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveAt(gameObject, cards.Count - 1);
            else if (!CgsNetManager.Instance.IsOnline)
                OwnerPopCard();

            if (PlaySettings.AutoStackCards && cards.Count == 1)
                RequestDelete();
        }

        [PublicAPI]
        public void OnChangeName(CgsNetString oldName, CgsNetString newName)
        {
            deckLabel.text = newName;
        }

        [ServerRpc]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private void UpdateCardsServerRpc(CgsNetString[] cardIds)
        {
            _cards = new List<UnityCard>();
            _cardIds.Clear();
            foreach (var cardId in cardIds)
            {
                _cards.Add(CardGameManager.Current.Cards[cardId]);
                _cardIds.Add(cardId);
            }
        }

        private void OnCardsUpdated(NetworkListEvent<CgsNetString> changeEvent)
        {
            _cards = new List<UnityCard>();
            foreach (var cardId in _cardIds)
                _cards.Add(CardGameManager.Current.Cards[cardId]);

            if (IsTopFaceup)
            {
                // Should this be part of SyncView()?
                if (changeEvent.Type.Equals(NetworkListEvent<CgsNetString>.EventType.RemoveAt) &&
                    changeEvent.Index == _cardIds.Count)
                {
                    if (CardGameManager.Current.Cards.TryGetValue(changeEvent.PreviousValue, out var previous))
                        previous.UnregisterDisplay(this);
                    if (CardGameManager.Current.Cards.TryGetValue(_cardIds[^1], out var current))
                        current.RegisterDisplay(this);
                }

                if (changeEvent.Type.Equals(NetworkListEvent<CgsNetString>.EventType.Insert) &&
                    changeEvent.Index == _cardIds.Count - 1)
                {
                    if (CardGameManager.Current.Cards.TryGetValue(_cardIds[^2], out var previous))
                        previous.UnregisterDisplay(this);
                    if (CardGameManager.Current.Cards.TryGetValue(changeEvent.Value, out var current))
                        current.RegisterDisplay(this);
                }
            }

            SyncView();
        }

        private void SyncView()
        {
            countLabel.text = Cards.Count.ToString();
            if (Viewer != null)
                Viewer.Sync(this);
        }

        public void OnDrop(CardModel cardModel)
        {
            cardModel.PlaceHolderCardZone = null;
            RequestInsert(Cards.Count, cardModel.Id);
            cardModel.RequestDelete();
        }

        public void OnDrop(CardStack cardStack)
        {
            var cards = Cards;
            for (var i = cards.Count - 1; i >= 0; i--)
                cardStack.RequestInsert(0, cards[i].Id);
            RequestDelete();
        }

        public void RequestInsert(int index, string cardId)
        {
            if (LacksOwnership)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(gameObject, index, cardId);
            else
                OwnerInsert(index, cardId);
        }

        public void OwnerInsert(int index, string cardId)
        {
            Debug.Log($"CardStack: {name} insert {cardId} at {index} of {Cards.Count}");
            _cards.Insert(index, CardGameManager.Current.Cards[cardId]);
            if (CgsNetManager.Instance.IsOnline)
                _cardIds.Insert(index, cardId);
            else
                SyncView();
        }

        public void RequestRemoveAt(int index)
        {
            if (LacksOwnership)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveAt(gameObject, index);
            else
                OwnerRemoveAt(index);
        }

        public string OwnerRemoveAt(int index)
        {
            if (index < 0 || index >= Cards.Count)
                return UnityCard.Blank.Id;
            var cardId = _cards[index].Id;
            _cards.RemoveAt(index);
            if (CgsNetManager.Instance.IsOnline)
                _cardIds.RemoveAt(index);
            else
                SyncView();
            return cardId;
        }

        public string OwnerPopCard()
        {
            return OwnerRemoveAt(Cards.Count - 1);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetIsTopFaceupServerRpc(bool isFacedown)
        {
            _isTopFaceupNetworkVariable.Value = isFacedown;
        }

        [PublicAPI]
        public void OnChangeIsTopFaceup(bool oldValue, bool newValue)
        {
            _isTopFaceup = newValue;
            if (IsTopFaceup)
                TopCard?.RegisterDisplay(this);
            else
                TopCard?.UnregisterDisplay(this);
        }

        [UsedImplicitly]
        public void View()
        {
            if (Viewer == null)
                Viewer = Instantiate(stackViewerPrefab, PlayController.Instance.stackViewers)
                    .GetComponent<StackViewer>();
            Viewer.Show(this);
        }

        [UsedImplicitly]
        public void PromptShuffle()
        {
            CardGameManager.Instance.Messenger.Prompt(ShufflePrompt, Shuffle);
        }

        private void Shuffle()
        {
            if (CgsNetManager.Instance.IsOnline)
                CgsNetManager.Instance.LocalPlayer.RequestShuffle(gameObject);
            else
                DoShuffle();
        }

        public void DoShuffle()
        {
            var cards = Cards.Select(card => card.Id).ToList();
            cards.Shuffle();

            _cards = cards.Select(card => CardGameManager.Current.Cards[card]).ToList();

            if (!CgsNetManager.Instance.IsOnline)
                return;

            if (CgsNetManager.Instance.IsConnectedClient && !IsServer)
            {
                Debug.LogError("Attempted to shuffle on client!");
                return;
            }

            _cardIds.Clear();
            foreach (var card in cards)
                _cardIds.Add(card);

            _actionText.Value = ShuffleText;
            _actionTime.Value = 1;
        }

        [UsedImplicitly]
        public void PromptSave()
        {
            CardGameManager.Instance.Messenger.Prompt(SavePrompt, Save);
        }

        private void Save()
        {
            var unityDeck = new UnityDeck(CardGameManager.Current, Name + SaveDelimiter + DateTime.Now,
                CardGameManager.Current.DeckFileType, Cards);
            try
            {
                if (!Directory.Exists(CardGameManager.Current.DecksDirectoryPath))
                    Directory.CreateDirectory(CardGameManager.Current.DecksDirectoryPath);
                File.WriteAllText(unityDeck.FilePath, unityDeck.ToString());

                _actionText.Value = SaveText;
                _actionTime.Value = 1;
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadMenu.DeckSaveErrorMessage + e.Message);
            }
        }

        [UsedImplicitly]
        public void FlipTopFace()
        {
            IsTopFaceup = !IsTopFaceup;
        }

        public override void OnDestroy()
        {
            TopCard?.UnregisterDisplay(this);

            base.OnDestroy();
        }
    }
}
