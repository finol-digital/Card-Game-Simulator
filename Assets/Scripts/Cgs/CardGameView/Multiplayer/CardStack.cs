/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CardGameDef.Unity;
using Cgs.CardGameView.Viewer;
using Cgs.Decks;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
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
        public string DeletePrompt => $"Delete {deckLabel.text}?";

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

        [field: SyncVar(hook = nameof(OnChangeName))]
        public string Name { get; set; }

        public override string ViewValue => Name;

        public IReadOnlyList<UnityCard> Cards
        {
            // This getter is slow, so it should be cached when appropriate
            get => _cardIds.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList();
            set
            {
                if (!CgsNetManager.Instance.isNetworkActive || !hasAuthority)
                {
                    _cardIds.Clear();
                    _cardIds.AddRange(value?.Select(card => card.Id).ToArray());
                }
                else
                    CmdUpdateCards(value?.Select(card => card.Id).ToArray());
            }
        }

        private readonly SyncList<string> _cardIds = new SyncList<string>();

        [SyncVar] private string _actionText = "";
        [SyncVar] private float _actionTime;

        private StackViewer _viewer;

        protected override void OnStartPlayable()
        {
            GetComponent<CardDropArea>().DropHandler = this;

            var rectTransform = (RectTransform) transform;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            rectTransform.sizeDelta = CardGameManager.PixelsPerInch * cardSize;
            rectTransform.localScale = Vector3.one;
            gameObject.GetOrAddComponent<BoxCollider2D>().size = CardGameManager.PixelsPerInch * cardSize;

            if (!hasAuthority)
            {
                deckLabel.text = Name;
                countLabel.text = _cardIds.Count.ToString();
            }

            _cardIds.Callback += OnCardsUpdated;

            topCard.sprite = CardGameManager.Current.CardBackImageSprite;
        }

        protected override void OnUpdatePlayable()
        {
            var isAction = _actionTime > 0;
            if (actionLabel.gameObject.activeSelf != isAction)
            {
                actionLabel.gameObject.SetActive(isAction);
                actionLabel.text = _actionText;
            }

            if (isAction && (!CgsNetManager.Instance.isNetworkActive || isServer))
                _actionTime -= Time.deltaTime;

            if (HoldTime > DragHoldTime)
                HighlightMode = HighlightMode.Authorized;
        }

        protected override void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            if (CurrentPointerEventData == null || CurrentPointerEventData.pointerId != eventData.pointerId ||
                eventData.dragging || eventData.button == PointerEventData.InputButton.Middle ||
                eventData.button == PointerEventData.InputButton.Right)
                return;

            if (EventSystem.current.currentSelectedGameObject == gameObject)
                View();
            else if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        protected override void OnPointerEnterPlayable(PointerEventData eventData)
        {
            if (Settings.PreviewOnMouseOver && CardViewer.Instance != null && !CardViewer.Instance.IsVisible
                && PlayableViewer.Instance != null && !PlayableViewer.Instance.IsVisible)
                PlayableViewer.Instance.Preview(this);
        }

        protected override void OnPointerExitPlayable(PointerEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.HidePreview();
        }

        protected override void OnSelectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.SelectedPlayable = this;
        }

        protected override void OnDeselectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.IsVisible = false;
        }

        protected override void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (IsDraggingCard)
                DragCard(eventData);
            else if (LacksAuthority)
                RequestTransferAuthority();
            else
                UpdatePosition();
        }

        protected override void OnDragPlayable(PointerEventData eventData)
        {
            if (IsDraggingCard)
                return;

            if (LacksAuthority)
                RequestTransferAuthority();
            else
                UpdatePosition();
        }

        protected override void OnEndDragPlayable(PointerEventData eventData)
        {
            if (!IsDraggingCard && !LacksAuthority)
                UpdatePosition();
        }

        private void DragCard(PointerEventData eventData)
        {
            if (_cardIds.Count < 1)
            {
                Debug.LogWarning("Attempted to remove from an empty card stack");
                return;
            }

            var unityCard = CardGameManager.Current.Cards[_cardIds[_cardIds.Count - 1]];

            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveAt(gameObject, _cardIds.Count - 1);
            else
                PopCard();

            var cardModel = CardModel.CreateDrag(eventData, cardModelPrefab, transform, unityCard, true,
                CgsNetManager.Instance.playController.playMat);
            CgsNetManager.Instance.LocalPlayer.RemovedCard = cardModel;

            RemovePointer(eventData);
        }

        [PublicAPI]
        public void OnChangeName(string oldName, string newName)
        {
            deckLabel.text = newName;
        }

        [Command]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private void CmdUpdateCards(string[] cardIds)
        {
            _cardIds.Clear();
            _cardIds.AddRange(cardIds);
        }

        private void OnCardsUpdated(SyncList<string>.Operation op, int index, string oldId, string newId)
        {
            countLabel.text = _cardIds.Count.ToString();
            if (_viewer != null)
                _viewer.Sync(this);
        }

        public void OnDrop(CardModel cardModel)
        {
            cardModel.PlaceHolderCardZone = null;
            if (CgsNetManager.Instance.isNetworkActive && !hasAuthority)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(gameObject, Cards.Count, cardModel.Id);
            else
                Insert(Cards.Count, cardModel.Id, true);
        }

        public void Insert(int index, string cardId, bool prompt = false)
        {
            _cardIds.Insert(index, cardId);
            Debug.Log(index + " " + _cardIds.Count + " " + prompt);
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
            if (_viewer == null)
                _viewer = Instantiate(stackViewerPrefab, CgsNetManager.Instance.playController.stackViewers)
                    .GetComponent<StackViewer>();
            _viewer.Show(this);
        }

        [UsedImplicitly]
        public void PromptShuffle()
        {
            CardGameManager.Instance.Messenger.Prompt(ShufflePrompt, Shuffle);
        }

        private void Shuffle()
        {
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestShuffle(gameObject);
            else
                DoShuffle();
        }

        public void DoShuffle()
        {
            if (NetworkManager.singleton.isNetworkActive && !isServer)
            {
                Debug.LogError("Attempted to shuffle on client!");
                return;
            }

            var cards = new List<string>(_cardIds);
            cards.Shuffle();
            _cardIds.Clear();
            _cardIds.AddRange(cards);

            _actionText = ShuffleText;
            _actionTime = 1;
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

                _actionText = SaveText;
                _actionTime = 1;
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadMenu.DeckSaveErrorMessage + e.Message);
            }
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, RequestDelete);
        }
    }
}
