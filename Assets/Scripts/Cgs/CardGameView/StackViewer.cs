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
        public GameObject cardModelPrefab;

        public List<CardDropArea> drops;
        public CardZone contentCardZone;
        public Text nameLabel;
        public Text countLabel;

        private CardStack _cardStack;

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

            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);

            Sync(stack);
            contentCardZone.scrollRectContainer.horizontalNormalizedPosition = 0;
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
            cardModel.IsFacedown = false;
            cardModel.DefaultAction = CardActions.Flip;
            countLabel.text = contentCardZone.GetComponentsInChildren<CardModel>().Length.ToString();

            if (_cardStack == null)
                return;

            int cardCount = cardZone.GetComponentsInChildren<CardModel>().Length;
            int index = cardCount - 1 - cardModel.transform.GetSiblingIndex();
            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestInsert(_cardStack.gameObject, index, cardModel.Id);
            else
                _cardStack.Insert(index, cardModel.Id);
        }

        private void OnRemoveCardModel(CardZone cardZone, CardModel cardModel)
        {
            countLabel.text = contentCardZone.GetComponentsInChildren<CardModel>().Length.ToString();

            if (_cardStack == null)
                return;

            if (CgsNetManager.Instance.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.RequestRemoveAt(_cardStack.gameObject, cardModel.Index);
            else
                _cardStack.RemoveAt(cardModel.Index);
        }

        public void Clear()
        {
            contentCardZone.transform.DestroyAllChildren();
        }

        [UsedImplicitly]
        public void Close()
        {
            Destroy(gameObject);
        }
    }
}
