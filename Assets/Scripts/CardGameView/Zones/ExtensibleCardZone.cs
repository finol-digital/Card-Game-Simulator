/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardGameView.Zones
{
    public class ExtensibleCardZone : MonoBehaviour, ICardDropHandler
    {
        public virtual IReadOnlyList<UnityCard> Cards => extensionContent.GetComponentsInChildren<CardModel>()
            .Select(cardModel => cardModel.Value).ToList();

        public GameObject cardModelPrefab;
        public List<CardDropArea> cardDropZones;
        public RectTransform extension;
        public RectTransform extensionContent;
        public Text labelText;
        public Text countText;
        public Text statusText;
        public GameObject shuffleButton;

        public ZonesViewer Viewer { get; set; }
        public bool IsExtended { get; private set; }

        private void Start()
        {
            foreach (CardDropArea dropZone in cardDropZones)
                dropZone.DropHandler = this;
            OnStart();
        }

        protected virtual void OnStart()
        {
            extensionContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(OnAddCardModel);
            extensionContent.gameObject.GetOrAddComponent<CardStack>().OnRemoveCardActions.Add(OnRemoveCardModel);
        }

        public virtual void OnDrop(CardModel cardModel)
        {
            AddCard(cardModel.Value);
        }

        public virtual void AddCard(UnityCard card)
        {
            var cardModel = Instantiate(cardModelPrefab, extensionContent).GetOrAddComponent<CardModel>();
            cardModel.Value = card;
            OnAddCardModel(null, cardModel);
        }

        protected virtual void OnAddCardModel(CardStack cardStack, CardModel cardModel)
        {
            if (cardModel == null)
                return;

            CardActions.ShowFaceup(cardModel);
            CardActions.ResetRotation(cardModel);
            cardModel.DoubleClickAction = CardActions.FlipFace;
            cardModel.SecondaryDragAction = null;

            UpdateCountText();
        }

        protected virtual void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
        {
            UpdateCountText();
        }

        protected virtual void Clear()
        {
            extensionContent.DestroyAllChildren();
            UpdateCountText();
        }

        protected IEnumerator DisplayShuffle()
        {
            if (statusText == null)
                yield break;

            if (shuffleButton != null)
                shuffleButton.SetActive(false);
            statusText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1);
            statusText.gameObject.SetActive(false);
            if (shuffleButton != null)
                shuffleButton.SetActive(true);
        }

        public virtual void ToggleExtension()
        {
            IsExtended = !IsExtended;
            ResizeExtension();
            extension.gameObject.GetOrAddComponent<CanvasGroup>().alpha = IsExtended ? 1 : 0;
            extension.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = IsExtended;
        }

        public void ResizeExtension()
        {
            const RectTransform.Edge edge = RectTransform.Edge.Right;
            const float inset = ZonesViewer.Width - ZonesViewer.ScrollbarWidth;
            float width = ((RectTransform) Viewer.transform).rect.width - ZonesViewer.Width +
                          (Viewer.IsExtended ? 0 : inset);
            extension.SetInsetAndSizeFromParentEdge(edge, inset, width);

            extension.anchorMin = new Vector2(extension.anchorMin.x, 0);
            // ReSharper disable once Unity.InefficientPropertyAccess
            extension.anchorMax = new Vector2(extension.anchorMin.x, 1);
            extension.offsetMin = new Vector2(extension.offsetMin.x, 0);
            extension.offsetMax = new Vector2(extension.offsetMax.x, 0);
        }

        protected void UpdateCountText()
        {
            countText.text = Cards.Count.ToString();
        }

        public void Sync(IEnumerable<Card> cards)
        {
            Clear();
            foreach (Card card in cards)
                AddCard((UnityCard) card);
        }

        public IEnumerator WaitForLoad(UnityAction action)
        {
            IReadOnlyList<UnityCard> deckCards = Cards;
            var loaded = false;
            while (!loaded)
            {
                yield return null;
                loaded = deckCards.Where(card => card.IsLoadingImage).ToList().Count == 0;
            }

            action();
        }
    }
}
