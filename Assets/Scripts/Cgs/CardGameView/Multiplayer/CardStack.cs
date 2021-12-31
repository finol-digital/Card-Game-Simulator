/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CardGameDef.Unity;
using Cgs.Decks;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
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
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThisThreadsRandom.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }

    [RequireComponent(typeof(CardDropArea))]
    public class CardStack : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, ICardDropHandler
    {
        public const string ShuffleText = "Shuffled!";
        public const string SaveText = "Saved";
        public const string TopOrBottomPrompt = "Keep on Top or Move to Bottom?";
        public const string Top = "Keep on Top";
        public const string Bottom = "Move to Bottom";

        private const string SaveDelimiter = "_";

        public string ShufflePrompt => $"Shuffle {deckLabel.text}?";
        public string SavePrompt => $"Save {deckLabel.text}?";
        public string DeletePrompt => $"Delete {deckLabel.text}?";

        private bool IsDraggingCard => _pointerPositions.Count == 1 && _currentPointerEventData != null &&
                                       _currentPointerEventData.button != PointerEventData.InputButton.Middle &&
                                       _currentPointerEventData.button != PointerEventData.InputButton.Right
                                       || _pointerPositions.Count > 1;

        private bool LacksAuthority => NetworkManager.singleton.isNetworkActive && !hasAuthority;

        public GameObject stackViewerPrefab;
        public GameObject cardModelPrefab;

        public Text deckLabel;
        public Text countLabel;
        public Text actionLabel;
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

        [SyncVar] private string _actionText = "";
        [SyncVar] private float _actionTime;

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
                rectTransform.localPosition = position;
            }

            _cardIds.Callback += OnCardsUpdated;

            topCard.sprite = CardGameManager.Current.CardBackImageSprite;
        }

        private void Update()
        {
            bool isAction = _actionTime > 0;
            if (actionLabel.gameObject.activeSelf != isAction)
            {
                actionLabel.gameObject.SetActive(isAction);
                actionLabel.text = _actionText;
            }

            if (isAction && (!CgsNetManager.Instance.isNetworkActive || isServer))
                _actionTime -= Time.deltaTime;
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
            else if (LacksAuthority)
                CmdTransferAuthority();
            else
                UpdatePosition();
        }

        public void OnDrag(PointerEventData eventData)
        {
            _currentPointerEventData = eventData;
            _currentDragPhase = DragPhase.Drag;
            _pointerPositions[eventData.pointerId] = eventData.position;
            if (IsDraggingCard)
                return;

            if (LacksAuthority)
                CmdTransferAuthority();
            else
                UpdatePosition();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _currentPointerEventData = eventData;
            _currentDragPhase = DragPhase.End;

            if (!IsDraggingCard && !LacksAuthority)
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

        private void UpdatePosition()
        {
            Vector2 targetPosition =
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(_pointerPositions.Values.ToList());
            targetPosition +=
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(_pointerDragOffsets.Values.ToList());

            var rectTransform = (RectTransform) transform;
            rectTransform.position = targetPosition;
            rectTransform.SetAsLastSibling();

            if (hasAuthority)
                CmdUpdatePosition(rectTransform.localPosition);
        }

        [Command(requiresAuthority = false)]
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
            transform.localPosition = newValue;
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
            cardModel.PlaceHolderCardZone = null;
            if (CgsNetManager.Instance.isNetworkActive && !hasAuthority)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(gameObject, Cards.Count, cardModel.Id, true);
            else
                Insert(Cards.Count, cardModel.Id, true);
        }

        public void Insert(int index, string cardId, bool prompt = false)
        {
            _cardIds.Insert(index, cardId);
            Debug.Log(index + " " + _cardIds.Count + " " + prompt);
            if (index == _cardIds.Count - 1 && prompt)
                PromptMoveToBottom();
        }

        public void PromptMoveToBottom()
        {
            CgsNetManager.Instance.playController.Decider.Show(TopOrBottomPrompt,
                new Tuple<string, UnityAction>(Top, () => { }),
                new Tuple<string, UnityAction>(Bottom, RequestMoveToBottom));
        }

        private void RequestMoveToBottom()
        {
            if (CgsNetManager.Instance.isNetworkActive)
                CmdMoveToBottom();
            else
            {
                var cardId = _cardIds[_cardIds.Count - 1];
                _cardIds.RemoveAt(_cardIds.Count - 1);
                _cardIds.Insert(0, cardId);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdMoveToBottom()
        {
            var cardId = _cardIds[_cardIds.Count - 1];
            _cardIds.RemoveAt(_cardIds.Count - 1);
            _cardIds.Insert(0, cardId);
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
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, Delete);
        }

        private void Delete()
        {
            if (NetworkManager.singleton.isNetworkActive)
                CmdDelete();
            else
                Destroy(gameObject);
        }

        [Command(requiresAuthority = false)]
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
