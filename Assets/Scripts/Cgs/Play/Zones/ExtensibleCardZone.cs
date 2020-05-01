/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using CardGameDef;
using CardGameView;

namespace Cgs.Play.Zones
{
    public class ExtensibleCardZone : MonoBehaviour, ICardDropHandler
    {
        public virtual IReadOnlyList<Card> Cards => extensionContent.GetComponentsInChildren<CardModel>().Select(cardModel => cardModel.Value).ToList();

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

        void Start()
        {
            foreach (CardDropArea dropZone in cardDropZones)
                dropZone.dropHandler = this;
            OnStart();
        }

        public virtual void OnStart()
        {
            extensionContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(OnAddCardModel);
            extensionContent.gameObject.GetOrAddComponent<CardStack>().OnRemoveCardActions.Add(OnRemoveCardModel);
        }

        public virtual void OnDrop(CardModel cardModel)
        {
            AddCard(cardModel.Value);
        }

        public virtual void AddCard(Card card)
        {
            CardModel newCardModel = Instantiate(cardModelPrefab, extensionContent).GetOrAddComponent<CardModel>();
            newCardModel.Value = card;
            OnAddCardModel(null, newCardModel);
        }

        public virtual void OnAddCardModel(CardStack cardStack, CardModel cardModel)
        {
            if (cardModel == null)
                return;

            CardActions.ShowFaceup(cardModel);
            CardActions.ResetRotation(cardModel);
            cardModel.DoubleClickAction = CardActions.FlipFace;
            cardModel.SecondaryDragAction = null;

            UpdateCountText();
        }

        public virtual void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
        {
            UpdateCountText();
        }

        public virtual void Clear()
        {
            extensionContent.DestroyAllChildren();
            UpdateCountText();
        }

        public virtual void Shuffle()
        {
            StopAllCoroutines();
            List<Card> cards = new List<Card>(Cards);
            cards.Shuffle();
            Sync(cards);
            StartCoroutine(DisplayShuffle());
        }

        public IEnumerator DisplayShuffle()
        {
            if (statusText != null)
            {
                if (shuffleButton != null)
                    shuffleButton.SetActive(false);
                statusText.gameObject.SetActive(true);
                yield return new WaitForSeconds(1);
                statusText.gameObject.SetActive(false);
                if (shuffleButton != null)
                    shuffleButton.SetActive(true);
            }
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
            RectTransform.Edge edge = RectTransform.Edge.Right;
            float inset = ZonesViewer.Width - ZonesViewer.ScrollbarWidth;
            float width = ((RectTransform)Viewer.transform).rect.width - ZonesViewer.Width + (Viewer.IsExtended ? 0 : inset);
            extension.SetInsetAndSizeFromParentEdge(edge, inset, width);

            extension.anchorMin = new Vector2(extension.anchorMin.x, 0);
            extension.anchorMax = new Vector2(extension.anchorMin.x, 1);
            extension.offsetMin = new Vector2(extension.offsetMin.x, 0);
            extension.offsetMax = new Vector2(extension.offsetMax.x, 0);
        }

        public void UpdateCountText()
        {
            countText.text = Cards.Count.ToString();
        }

        public void Sync(List<Card> cards)
        {
            Clear();
            foreach (Card card in cards)
                AddCard(card);
        }

        public IEnumerator WaitForLoad(UnityAction action)
        {
            IReadOnlyList<Card> deckCards = Cards;
            bool loaded = false;
            while (!loaded)
            {
                yield return null;
                loaded = deckCards.Where(card => card.IsLoadingImage).ToList().Count == 0;
            }
            action();
        }
    }
}
