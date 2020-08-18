/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using CardGameDef.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CardGameView
{
    public class ZoneViewer : MonoBehaviour, ICardDropHandler
    {
        public GameObject cardModelPrefab;

        public List<CardDropArea> drops;
        public CardStack contentCardStack;
        public Text countLabel;

        private void Start()
        {
            foreach (CardDropArea drop in drops)
                drop.DropHandler = this;
            contentCardStack.OnAddCardActions.Add(OnAddCardModel);
            contentCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
        }

        public void OnDrop(CardModel cardModel)
        {
            AddCard(cardModel.Value);
        }

        public void AddCard(UnityCard card)
        {
            var cardModel = Instantiate(cardModelPrefab, contentCardStack.transform).GetOrAddComponent<CardModel>();
            cardModel.Value = card;

            OnAddCardModel(contentCardStack, cardModel);
        }

        private void OnAddCardModel(CardStack cardStack, CardModel cardModel)
        {
            cardModel.transform.rotation = Quaternion.identity;
            cardModel.IsFacedown = false;
            cardModel.DoubleClickAction = CardActions.FlipFace;
            countLabel.text = contentCardStack.GetComponentsInChildren<CardModel>().Length.ToString();
        }

        private void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
        {
            countLabel.text = contentCardStack.GetComponentsInChildren<CardModel>().Length.ToString();
        }

        public void Clear()
        {
            contentCardStack.transform.DestroyAllChildren();
        }
    }
}
