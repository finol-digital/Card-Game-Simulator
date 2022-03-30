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

namespace Cgs.CardGameView.Viewer
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
            foreach (var cardDropArea in drops)
                cardDropArea.DropHandler = this;
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
            var rectTransform = (RectTransform) transform;
            var cardHeight = CardGameManager.Current.CardSize.Y * CardGameManager.PixelsPerInch;
            rectTransform.sizeDelta =
                new Vector2(rectTransform.sizeDelta.x, HandleHeight + cardHeight + ScrollbarHeight);
            cardZoneTransform.sizeDelta = new Vector2(cardZoneTransform.sizeDelta.x, cardHeight);
        }

        public void Sync(CardStack stack)
        {
            _cardStack = stack;
            nameLabel.text = _cardStack.Name;

            contentCardZone.transform.DestroyAllChildren();
            var cards = _cardStack.Cards.Reverse().ToList();
            var cardModelIndex = cards.Count - 1;
            foreach (var unityCard in cards)
            {
                var cardModel = Instantiate(cardModelPrefab, contentCardZone.transform).GetOrAddComponent<CardModel>();
                cardModel.Value = unityCard;
                cardModel.Index = cardModelIndex;
                cardModelIndex--;
            }

            countLabel.text = _cardStack.Cards.Count.ToString();
        }

        public void Sync(int handIndex, CardZone cardZone, Text nameText, Text countText)
        {
            _handIndex = handIndex;
            if (!cardZone.OnAddCardActions.Contains(OnAddCardModel))
                cardZone.OnAddCardActions.Add(OnAddCardModel);
            if (!cardZone.OnRemoveCardActions.Contains(OnRemoveCardModel))
                cardZone.OnRemoveCardActions.Add(OnRemoveCardModel);
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

        private void OnAddCardModel(CardZone cardZone, CardModel cardModel)
        {
            cardModel.transform.rotation = Quaternion.identity;
            cardModel.SetIsFacedown(false);
            cardModel.DefaultAction = CardActions.Flip;

            var cardModels = contentCardZone.GetComponentsInChildren<CardModel>();
            countLabel.text = cardModels.Length.ToString();

            if (_handIndex != null)
                CgsNetManager.Instance.LocalPlayer.RequestSyncHand((int) _handIndex,
                    cardModels.Select(card => card.Id).ToArray());

            if (_cardStack == null)
                return;

            var cardCount = cardZone.GetComponentsInChildren<CardModel>().Length;
            var cardIndex = cardCount - 1 - cardModel.transform.GetSiblingIndex();
            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(_cardStack.gameObject, cardIndex, cardModel.Id);
            else
                _cardStack.Insert(cardIndex, cardModel.Id);
        }

        private void OnRemoveCardModel(CardZone cardZone, CardModel cardModel)
        {
            var cardModels = contentCardZone.GetComponentsInChildren<CardModel>();
            countLabel.text = cardModels.Length.ToString();

            if (_handIndex != null)
                CgsNetManager.Instance.LocalPlayer.RequestSyncHand((int) _handIndex,
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
