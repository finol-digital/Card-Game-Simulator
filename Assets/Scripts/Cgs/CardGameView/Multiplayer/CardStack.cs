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
using FinolDigital.Cgs.CardGameDef.Unity;
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

    [RequireComponent(typeof(CardDropArea))]
    public class CardStack : CgsNetPlayable, ICardDropHandler
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

        public GameObject stackViewerPrefab;
        public GameObject cardModelPrefab;

        public Text deckLabel;
        public Text countLabel;
        public Text actionLabel;
        public Image topCard;

        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        private readonly NetworkVariable<CgsNetString> _name = new();

        public override string ViewValue => Name;

        [NotNull]
        public IReadOnlyList<UnityCard> Cards
        {
            // This getter is slow, so it should be cached when appropriate
            get
            {
                List<UnityCard> cards = new();
                foreach (var cardId in _cardIds)
                    cards.Add(CardGameManager.Current.Cards[cardId]);
                return cards;
            }
            set
            {
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

        private NetworkList<CgsNetString> _cardIds;

        private readonly NetworkVariable<CgsNetString> _actionText = new();
        private readonly NetworkVariable<float> _actionTime = new();

        public StackViewer Viewer { get; private set; }

        protected override void OnAwakePlayable()
        {
            _cardIds = new NetworkList<CgsNetString>();
            _name.OnValueChanged += OnChangeName;
        }

        protected override void OnStartPlayable()
        {
            ParentToPlayAreaContent();
            GetComponent<CardDropArea>().DropHandler = this;

            var rectTransform = (RectTransform) transform;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            rectTransform.sizeDelta = CardGameManager.PixelsPerInch * cardSize;
            rectTransform.localScale = Vector3.one;
            gameObject.GetOrAddComponent<BoxCollider2D>().size = CardGameManager.PixelsPerInch * cardSize;

            if (!IsOwner)
                deckLabel.text = Name;
            countLabel.text = _cardIds.Count.ToString();
            _cardIds.OnListChanged += OnCardsUpdated;

            topCard.sprite = CardGameManager.Current.CardBackImageSprite;
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
        }

        private void DragCard(PointerEventData eventData)
        {
            if (_cardIds.Count < 1)
            {
                Debug.LogWarning("Attempted to remove from an empty card stack");
                return;
            }

            var unityCard = CardGameManager.Current.Cards[_cardIds[^1]];

            if (CgsNetManager.Instance.IsOnline)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveAt(gameObject, _cardIds.Count - 1);
            else
                PopCard();

            CardModel.CreateDrag(eventData, cardModelPrefab, transform, unityCard, true,
                PlayController.Instance.playAreaCardZone);

            RemovePointer(eventData);

            if (PlaySettings.AutoStackCards && _cardIds.Count < 1)
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
            _cardIds.Clear();
            foreach (var cardId in cardIds)
                _cardIds.Add(cardId);
        }

        private void OnCardsUpdated(NetworkListEvent<CgsNetString> changeEvent)
        {
            countLabel.text = _cardIds.Count.ToString();
            if (Viewer != null)
                Viewer.Sync(this);
        }

        public void OnDrop(CardModel cardModel)
        {
            cardModel.PlaceHolderCardZone = null;
            if (LacksOwnership)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(gameObject, Cards.Count, cardModel.Id);
            else
                Insert(Cards.Count, cardModel.Id);
        }

        public void Insert(int index, string cardId)
        {
            Debug.Log($"CardStack: {name} insert {cardId} at {index} of {_cardIds.Count}");
            _cardIds.Insert(index, cardId);
        }

        public string RemoveAt(int index)
        {
            if (index < 0 || index >= _cardIds.Count)
                return UnityCard.Blank.Id;
            var cardId = _cardIds[index];
            _cardIds.RemoveAt(index);
            return cardId;
        }

        public string PopCard()
        {
            return RemoveAt(_cardIds.Count - 1);
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
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.IsOnline)
                CgsNetManager.Instance.LocalPlayer.RequestShuffle(gameObject);
            else
                DoShuffle();
        }

        public void DoShuffle()
        {
            if (CgsNetManager.Instance.IsConnectedClient && !IsServer)
            {
                Debug.LogError("Attempted to shuffle on client!");
                return;
            }

            var cards = new List<string>();
            foreach (var card in _cardIds)
                cards.Add(card);
            cards.Shuffle();
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
    }
}
