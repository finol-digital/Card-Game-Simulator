/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using CardGameDef.Unity;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView
{
    public class StackViewer : MonoBehaviour, ICardDropHandler
    {
        private const float HandleHeight = 100.0f;
        private const float ScrollbarHeight = 50.0f;

        public GameObject cardModelPrefab;

        public RectTransform cardZoneTransform;

        public List<CardDropArea> drops;
        public CardZone contentCardZone;
        public Text nameLabel;
        public Text countLabel;

        private CardStack _cardStack;
        private int? _handIndex;

        private void Start()
        {
            foreach (CardDropArea drop in drops)
                drop.DropHandler = this;
            contentCardZone.OnAddCardActions.Add(OnAddCardModel);
            contentCardZone.OnRemoveCardActions.Add(OnRemoveCardModel);
        }

        public void Show(CardStack stack)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            Resize();

            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);

            Sync(stack);
            contentCardZone.scrollRectContainer.horizontalNormalizedPosition = 0;
        }

        private void Resize()
        {
            var rectTransform = (RectTransform)transform;
            float cardHeight = CardGameManager.Current.CardSize.Y * CardGameManager.PixelsPerInch;
            rectTransform.sizeDelta =
                new Vector2(rectTransform.sizeDelta.x, HandleHeight + cardHeight + ScrollbarHeight);
            cardZoneTransform.sizeDelta = new Vector2(cardZoneTransform.sizeDelta.x, cardHeight);
        }

        public void Sync(CardStack stack)
        {
            _cardStack = stack;
            nameLabel.text = _cardStack.Name;

            contentCardZone.transform.DestroyAllChildren();
            List<UnityCard> cards = _cardStack.Cards.Reverse().ToList();
            int index = cards.Count - 1;
            foreach (UnityCard card in cards)
            {
                var cardModel = Instantiate(cardModelPrefab, contentCardZone.transform).GetOrAddComponent<CardModel>();
                cardModel.Value = card;
                cardModel.Index = index;
                index--;
            }

            countLabel.text = _cardStack.Cards.Count.ToString();
        }

        public void Sync(int handIndex, CardZone cardZone, Text nameText, Text countText)
        {
            _handIndex = handIndex;
            contentCardZone = cardZone;
            nameLabel = nameText;
            countLabel = countText;
        }

        public void OnDrop(CardModel cardModel)
        {
            AddCard(cardModel.Value);
        }

        public void AddCard(UnityCard card)
        {
            var cardModel = Instantiate(cardModelPrefab, contentCardZone.transform).GetOrAddComponent<CardModel>();
            cardModel.Value = card;
            cardModel.transform.SetAsFirstSibling();

            OnAddCardModel(contentCardZone, cardModel);
        }

        public void OnAddCardModel(CardZone cardZone, CardModel cardModel)
        {
            cardModel.transform.rotation = Quaternion.identity;
            cardModel.IsFacedown = false;
            cardModel.DefaultAction = CardActions.Flip;

            CardModel[] cardModels = contentCardZone.GetComponentsInChildren<CardModel>();
            countLabel.text = cardModels.Length.ToString();

            if (_handIndex != null)
                CgsNetManager.Instance.LocalPlayer.RequestSyncHand((int)_handIndex,
                    cardModels.Select(card => card.Id).ToArray());

            if (_cardStack == null)
                return;

            int cardCount = cardZone.GetComponentsInChildren<CardModel>().Length;
            int index = cardCount - 1 - cardModel.transform.GetSiblingIndex();
            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(_cardStack.gameObject, index, cardModel.Id);
            else
                _cardStack.Insert(index, cardModel.Id);
        }

        public void OnRemoveCardModel(CardZone cardZone, CardModel cardModel)
        {
            CardModel[] cardModels = contentCardZone.GetComponentsInChildren<CardModel>();
            countLabel.text = cardModels.Length.ToString();

            if (_handIndex != null)
                CgsNetManager.Instance.LocalPlayer.RequestSyncHand((int)_handIndex,
                    cardModels.Select(card => card.Id).ToArray());

            if (_cardStack == null)
                return;

            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveAt(_cardStack.gameObject, cardModel.Index);
            else
                _cardStack.RemoveAt(cardModel.Index);
        }

        [UsedImplicitly]
        public void Close()
        {
            Destroy(gameObject);
        }
    }
}
