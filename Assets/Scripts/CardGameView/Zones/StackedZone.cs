/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;

namespace CardGameView.Zones
{
    [RequireComponent(typeof(CardStack))]
    public class StackedZone : ExtensibleCardZone
    {
        public bool IsFaceup { get; set; }

        public override IReadOnlyList<UnityCard> Cards => CardModels.Select(cardModel => cardModel.Value).ToList();
        protected List<CardModel> CardModels { get; } = new List<CardModel>();

        protected CardStack ExtensionCardStack => _extensionCardStack ??
                                                  (_extensionCardStack = extensionContent.gameObject
                                                      .GetOrAddComponent<CardStack>());

        private CardStack _extensionCardStack;

        protected CardStack ZoneCardStack => _zoneCardStack ?? (_zoneCardStack = GetComponent<CardStack>());
        private CardStack _zoneCardStack;

        protected CardDropArea DropZone => _dropZone ?? (_dropZone = gameObject.GetOrAddComponent<CardDropArea>());
        private CardDropArea _dropZone;

        protected override void OnStart()
        {
            ExtensionCardStack.OnAddCardActions.Add(OnAddCardModel);
            ExtensionCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
            ZoneCardStack.OnAddCardActions.Add(OnAddCardModel);
            ZoneCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
        }

        public override void AddCard(UnityCard card)
        {
            var cardModel =
                Instantiate(cardModelPrefab, IsExtended ? ExtensionCardStack.transform : ZoneCardStack.transform)
                    .GetOrAddComponent<CardModel>();
            cardModel.Value = card;
            cardModel.IsFacedown = !IsFaceup;
            OnAddCardModelLocal(IsExtended ? ExtensionCardStack : ZoneCardStack, cardModel);
        }

        private void OnAddCardModelLocal(Object cardStack, CardModel cardModel)
        {
            if (cardStack == null || cardModel == null)
                return;

            cardModel.transform.rotation = Quaternion.identity;
            cardModel.DoubleClickAction = ToggleExtension;
            cardModel.SecondaryDragAction = Shuffle;
            if (IsExtended)
                cardModel.IsFacedown = false;

            int cardIndex = CardModels.Count;
            if (cardStack == ExtensionCardStack)
            {
                int transformIndex = cardModel.transform.GetSiblingIndex();
                cardIndex = transformIndex >= 0 && transformIndex < CardModels.Count
                    ? transformIndex
                    : CardModels.Count;
            }

            CardModels.Insert(cardIndex, cardModel);
            UpdateCountText();
        }

        protected override void OnAddCardModel(CardStack cardStack, CardModel cardModel)
        {
            OnAddCardModelLocal(cardStack, cardModel);
            UpdateNetwork();
        }

        protected override void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
        {
            CardModels.Remove(cardModel);
            UpdateCountText();
            UpdateNetwork();
        }

        private void UpdateNetwork()
        {
            if (Viewer == null || Viewer.CurrentDeck != this ||
                CgsNetManager.Instance == null || !CgsNetManager.Instance.isNetworkActive ||
                CgsNetManager.Instance.LocalPlayer == null)
                return;

            string[] currentDeckCardIds = CgsNetManager.Instance.LocalPlayer.CurrentDeckCardIds;
            if (currentDeckCardIds == null)
                return;

#if !UNITY_WEBGL
            IReadOnlyList<Card> localDeck = Cards;
            bool deckMatches = localDeck.Count == currentDeckCardIds.Length;
            for (var i = 0; deckMatches && i < localDeck.Count; i++)
                if (!localDeck[i].Id.Equals(currentDeckCardIds[i]))
                    deckMatches = false;

            if (!deckMatches)
                CgsNetManager.Instance.LocalPlayer.RequestDeckUpdate(localDeck);
#endif
        }

        public UnityCard PopCard()
        {
            if (CardModels.Count < 1)
                return UnityCard.Blank;

            CardModel cardModel = CardModels[CardModels.Count - 1];
            UnityCard card = cardModel.Value;
            CardModels.Remove(cardModel);
            Destroy(cardModel.gameObject);
            UpdateCountText();
            // Calling method should update network
            return card;
        }

        protected override void Clear()
        {
            foreach (CardModel cardModel in CardModels)
                Destroy(cardModel.gameObject);
            CardModels.Clear();
            UpdateCountText();
        }

        public void Shuffle()
        {
            StopAllCoroutines();
            CardModels.Shuffle();
            Display();
            StartCoroutine(DisplayShuffle());
        }

        [UsedImplicitly]
        public void ToggleExtension(CardModel cardModel)
        {
            ToggleExtension();
        }

        [UsedImplicitly]
        public override void ToggleExtension()
        {
            base.ToggleExtension();
            DropZone.DropHandler = IsExtended ? this : null;
            ZoneCardStack.enabled = !IsExtended;
            Display();
        }

        private void Display()
        {
            Transform parent = ZoneCardStack.transform;
            if (IsExtended)
                parent = ExtensionCardStack.transform;

            int siblingIndex = IsExtended ? 0 : 3;
            foreach (CardModel cardModel in CardModels)
            {
                cardModel.transform.SetParent(parent);
                cardModel.IsFacedown = !IsExtended && !IsFaceup;
                if (IsExtended)
                    continue;
                var cardModelTransform = (RectTransform) cardModel.transform;
                cardModelTransform.anchorMin = new Vector2(0.5f, 0.5f);
                cardModelTransform.anchorMax = new Vector2(0.5f, 0.5f);
                cardModelTransform.anchoredPosition = Vector2.zero;
                cardModel.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }
        }
    }
}
