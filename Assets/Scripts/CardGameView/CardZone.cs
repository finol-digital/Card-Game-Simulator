/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGameView
{
    [RequireComponent(typeof(CardDropArea))]
    public class CardZone : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler,
        IBeginDragHandler, IDragHandler, ICardDropHandler
    {
        public string ShufflePrompt => $"Shuffle {deckLabel.text}?";
        public string DeletePrompt => $"Delete {deckLabel.text}?";

        public Text deckLabel;
        public Text countLabel;
        public Image topCard;
        public CanvasGroup buttons;

        [field: SyncVar(hook = nameof(OnChangeName))]
        public string Name { get; set; }

        public List<UnityCard> Cards
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

        private readonly SyncListString _cardIds = new SyncListString();

        [SyncVar(hook = nameof(OnChangePosition))]
        public Vector2 position;

        private Vector2 _dragOffset;

        private void Start()
        {
            GetComponent<CardDropArea>().DropHandler = this;

            var rectTransform = (RectTransform) transform;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            rectTransform.sizeDelta = CardGameManager.PixelsPerInch * cardSize;

            if (!hasAuthority)
            {
                deckLabel.text = Name;
                countLabel.text = _cardIds.Count.ToString();
                rectTransform.anchoredPosition = position;
            }

            _cardIds.Callback += OnCardsUpdated;

            topCard.sprite = CardGameManager.Current.CardBackImageSprite;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // OnPointerDown is required for OnPointerUp to trigger
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            ShowButtons();
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
            if (!hasAuthority)
                return;
            _dragOffset = eventData.position - (Vector2) transform.position;
            transform.SetAsLastSibling();
            HideButtons();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!hasAuthority)
                return;
            var rectTransform = (RectTransform) transform;
            rectTransform.position = eventData.position - _dragOffset;
            CmdUpdatePosition(rectTransform.anchoredPosition);
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

        private void OnCardsUpdated(SyncList<string>.Operation op, int index, string oldId, string newId)
        {
            countLabel.text = _cardIds.Count.ToString();
        }

        public void OnDrop(CardModel cardModel)
        {
            AddCard(cardModel.Value);
        }

        private void AddCard(Card card)
        {
            _cardIds.Add(card.Id);
        }

        public UnityCard PopCard()
        {
            if (_cardIds.Count < 1)
                return UnityCard.Blank;
            UnityCard card = CardGameManager.Current.Cards[_cardIds[_cardIds.Count - 1]];
            _cardIds.RemoveAt(_cardIds.Count - 1);
            return card;
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
            // TODO: ZONE VIEWER
        }

        [UsedImplicitly]
        public void PromptShuffle()
        {
            CardGameManager.Instance.Messenger.Prompt(ShufflePrompt, Shuffle);
        }

        private void Shuffle()
        {
            Cards.Shuffle();
            // TODO: SHOW SHUFFLE LABEL
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, Delete);
        }

        private void Delete()
        {
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestDelete(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
