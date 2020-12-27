/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardGameDef.Unity;
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

        public static Random ThisThreadsRandom => _local ??
                                                  (_local = new Random(
                                                      unchecked(Environment.TickCount * 31 + Thread
                                                          .CurrentThread.ManagedThreadId)));

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    [RequireComponent(typeof(CardDropArea))]
    public class CardStack : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, ICardDropHandler
    {
        public string ShufflePrompt => $"Shuffle {deckLabel.text}?";
        public string DeletePrompt => $"Delete {deckLabel.text}?";

        public bool IsDraggingCard => _pointerPositions.Count == 1 && _currentPointerEventData != null &&
                                      _currentPointerEventData.button != PointerEventData.InputButton.Middle &&
                                      _currentPointerEventData.button != PointerEventData.InputButton.Right
                                      || _pointerPositions.Count > 1;

        public GameObject stackViewerPrefab;
        public GameObject cardModelPrefab;

        public Text deckLabel;
        public Text countLabel;
        public GameObject shuffleLabel;
        public Image topCard;
        public CanvasGroup buttons;

        [field: SyncVar(hook = nameof(OnChangeName))]
        public string Name { get; set; }

        public IReadOnlyList<UnityCard> Cards
        {
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

        [SyncVar(hook = nameof(OnChangePosition))]
        public Vector2 position;

        [SyncVar] private float _shuffleTime;

        private StackViewer _viewer;

        private PointerEventData _currentPointerEventData;
        private DragPhase _currentDragPhase;

        private readonly Dictionary<int, Vector2> _pointerPositions = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, Vector2> _pointerDragOffsets = new Dictionary<int, Vector2>();

        private void Start()
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
                rectTransform.anchoredPosition = position;
            }

            _cardIds.Callback += OnCardsUpdated;

            topCard.sprite = CardGameManager.Current.CardBackImageSprite;
        }

        private void Update()
        {
            bool shuffled = _shuffleTime > 0;
            if (shuffleLabel.activeSelf != shuffled)
                shuffleLabel.SetActive(shuffled);
            if (shuffled && (!NetworkManager.singleton.isNetworkActive || isServer))
                _shuffleTime -= Time.deltaTime;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _currentPointerEventData = eventData;
            _pointerPositions[eventData.pointerId] = eventData.position;
            _pointerDragOffsets[eventData.pointerId] = (Vector2) transform.position - eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_currentPointerEventData != null && _currentPointerEventData.pointerId == eventData.pointerId &&
                !eventData.dragging && eventData.button != PointerEventData.InputButton.Middle &&
                eventData.button != PointerEventData.InputButton.Right)
            {
                if (EventSystem.current.currentSelectedGameObject == gameObject)
                    View();
                else if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            }

            _currentPointerEventData = eventData;

            if (_currentDragPhase == DragPhase.Drag)
                return;
            _pointerPositions.Remove(eventData.pointerId);
            _pointerDragOffsets.Remove(eventData.pointerId);
        }

        public void OnSelect(BaseEventData eventData)
        {
            ShowButtons();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            HideButtons();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _currentPointerEventData = eventData;
            _currentDragPhase = DragPhase.Begin;
            _pointerPositions[eventData.pointerId] = eventData.position;

            HideButtons();

            if (IsDraggingCard)
                DragCard(eventData);
            else if (hasAuthority)
                UpdatePosition();
            else
                CmdTransferAuthority();
        }

        public void OnDrag(PointerEventData eventData)
        {
            _currentPointerEventData = eventData;
            _currentDragPhase = DragPhase.Drag;
            _pointerPositions[eventData.pointerId] = eventData.position;
            if (IsDraggingCard)
                return;

            if (hasAuthority)
                UpdatePosition();
            else
                CmdTransferAuthority();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _currentPointerEventData = eventData;
            _currentDragPhase = DragPhase.End;

            if (!IsDraggingCard && hasAuthority)
                UpdatePosition();
            if (hasAuthority)
                CmdReleaseAuthority();

            RemovePointer(eventData);
        }

        private void RemovePointer(PointerEventData eventData)
        {
            Vector2 removedOffset = Vector2.zero;
            if (_pointerDragOffsets.TryGetValue(eventData.pointerId, out Vector2 pointerDragOffset))
                removedOffset = (Vector2) transform.position - eventData.position - pointerDragOffset;
            _pointerPositions.Remove(eventData.pointerId);
            _pointerDragOffsets.Remove(eventData.pointerId);
            foreach (int offsetKey in _pointerDragOffsets.Keys.ToList())
                if (_pointerDragOffsets.TryGetValue(offsetKey, out Vector2 otherOffset))
                    _pointerDragOffsets[offsetKey] = otherOffset - removedOffset;
        }

        private void DragCard(PointerEventData eventData)
        {
            if (_cardIds.Count < 1)
            {
                Debug.LogWarning("Attempted to remove from an empty card stack");
                return;
            }

            UnityCard card = CardGameManager.Current.Cards[_cardIds[_cardIds.Count - 1]];

            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveAt(gameObject, _cardIds.Count - 1);
            else
                PopCard();

            CardModel.CreateDrag(eventData, cardModelPrefab, transform, card, true,
                CgsNetManager.Instance.playController.playArea);

            RemovePointer(eventData);
        }

        private void UpdatePosition()
        {
            if (_pointerPositions.Count < 1 || _pointerDragOffsets.Count < 1 || !hasAuthority)
            {
                Debug.LogError("Attempted to process translation and authority without pointers or authority!");
                return;
            }

            Vector2 targetPosition = UnityExtensionMethods.UnityExtensionMethods.CalculateMean(_pointerPositions.Values.ToList());
            targetPosition += UnityExtensionMethods.UnityExtensionMethods.CalculateMean(_pointerDragOffsets.Values.ToList());

            var rectTransform = (RectTransform) transform;
            rectTransform.position = targetPosition;
            rectTransform.SetAsLastSibling();

            CmdUpdatePosition(rectTransform.anchoredPosition);
        }

        [Command(ignoreAuthority = true)]
        private void CmdTransferAuthority(NetworkConnectionToClient sender = null)
        {
            if (sender != null && netIdentity.connectionToClient == null)
                netIdentity.AssignClientAuthority(sender);
        }

        [Command]
        private void CmdUpdatePosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        [PublicAPI]
        public void OnChangePosition(Vector2 oldValue, Vector2 newValue)
        {
            ((RectTransform) transform).anchoredPosition = newValue;
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

        [Command]
        private void CmdReleaseAuthority()
        {
            netIdentity.RemoveClientAuthority();
        }

        private void OnCardsUpdated(SyncList<string>.Operation op, int index, string oldId, string newId)
        {
            countLabel.text = _cardIds.Count.ToString();
            if (_viewer != null)
                _viewer.Sync(this);
        }

        public void OnDrop(CardModel cardModel)
        {
            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(gameObject, Cards.Count, cardModel.Id);
            else
                Insert(Cards.Count, cardModel.Id);
        }

        public void Insert(int index, string cardId)
        {
            _cardIds.Insert(index, cardId);
        }

        public string RemoveAt(int index)
        {
            if (index < 0 || index >= _cardIds.Count)
                return UnityCard.Blank.Id;
            string cardId = _cardIds[index];
            _cardIds.RemoveAt(index);
            return cardId;
        }

        public string PopCard()
        {
            return RemoveAt(_cardIds.Count - 1);
        }

        private void ShowButtons()
        {
            buttons.alpha = 1;
            buttons.interactable = true;
            buttons.blocksRaycasts = true;
        }

        private void HideButtons()
        {
            buttons.alpha = 0;
            buttons.interactable = false;
            buttons.blocksRaycasts = false;
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

            List<string> cards = new List<string>(_cardIds);
            cards.Shuffle();
            _cardIds.Clear();
            _cardIds.AddRange(cards);
            _shuffleTime = 1;
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, Delete);
        }

        private void Delete()
        {
            if (NetworkManager.singleton.isNetworkActive)
                CmdDelete();
            else
                Destroy(gameObject);
        }

        [Command(ignoreAuthority = true)]
        private void CmdDelete()
        {
            if (netIdentity.connectionToClient != null)
            {
                Debug.LogWarning("Ignoring request to delete, since it is currently owned by a client!");
                return;
            }

            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }
    }
}
