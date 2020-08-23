/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using CardGameDef.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CardGameView
{
    public class StackViewer : MonoBehaviour, ICardDropHandler
    {
        public GameObject cardModelPrefab;

        public List<CardDropArea> drops;
        public CardZone contentCardZone;
        public Text countLabel;

        private void Start()
        {
            foreach (CardDropArea drop in drops)
                drop.DropHandler = this;
            contentCardZone.OnAddCardActions.Add(OnAddCardModel);
            contentCardZone.OnRemoveCardActions.Add(OnRemoveCardModel);
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
            cardModel.DoubleClickAction = CardActions.FlipFace;
            countLabel.text = contentCardZone.GetComponentsInChildren<CardModel>().Length.ToString();
        }

        private void OnRemoveCardModel(CardZone cardZone, CardModel cardModel)
        {
            countLabel.text = contentCardZone.GetComponentsInChildren<CardModel>().Length.ToString();
        }

        public void Clear()
        {
            contentCardZone.transform.DestroyAllChildren();
        }
    }
}
