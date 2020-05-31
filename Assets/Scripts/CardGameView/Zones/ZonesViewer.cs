/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using CardGameDef;
using CardGameDef.Unity;
using Cgs;
using UnityEngine;
using UnityEngine.UI;

namespace CardGameView.Zones
{
    [RequireComponent(typeof(Canvas))]
    public class ZonesViewer : MonoBehaviour
    {
        public const float Width = 350f;
        public const float Height = 300f;
        public const float ScrollbarWidth = 80f;

        public bool IsPortrait => ((RectTransform) transform).sizeDelta.y > ((RectTransform) transform).sizeDelta.x;

        public bool IsExtended
        {
            get => ((RectTransform) scrollView.transform.parent).anchoredPosition.x < 1;
            set
            {
                const RectTransform.Edge edge = RectTransform.Edge.Right;
                const float size = Width;
                float inset = value ? 0 : -(size - ScrollbarWidth);
                ((RectTransform) scrollView.transform.parent).SetInsetAndSizeFromParentEdge(edge, inset, size);
                if (Results != null)
                    Results.ResizeExtension();
                if (Discard != null)
                    Discard.ResizeExtension();
                foreach (ExtensibleCardZone zone in ExtraZones)
                    zone.ResizeExtension();
                if (CurrentDeck != null)
                    CurrentDeck.ResizeExtension();
                if (Hand != null)
                    Hand.ResizeExtension();
                ResetButtons();
            }
        }

        public bool IsVisible
        {
            get => scrollView.gameObject.activeSelf;
            set
            {
                scrollView.gameObject.SetActive(value);
                const RectTransform.Edge edge = RectTransform.Edge.Top;
                float size = GetComponent<RectTransform>().rect.height;
                ((RectTransform) scrollView.transform.parent).SetInsetAndSizeFromParentEdge(edge, 0,
                    scrollView.gameObject.activeSelf ? size : ScrollbarWidth);
                ResetButtons();
            }
        }

        public GameObject resultsZonePrefab;
        public GameObject discardZonePrefab;
        public GameObject extraZonePrefab;
        public GameObject deckZonePrefab;
        public GameObject handZonePrefab;
        public ScrollRect scrollView;
        public GameObject extendButton;
        public GameObject showButton;
        public GameObject hideButton;

        public ExtensibleCardZone Results { get; private set; }
        public StackedZone Discard { get; private set; }
        private List<ExtensibleCardZone> ExtraZones { get; } = new List<ExtensibleCardZone>();
        public StackedZone CurrentDeck { get; private set; }
        public ExtensibleCardZone Hand { get; private set; }

        private void Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());
            CreateDiscard();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;
            IsExtended = IsExtended;
            IsVisible = IsVisible;
            ResizeContent();
        }

        private void ResizeContent()
        {
            float height = Height;
            var siblingIndex = 3;
            if (Results != null)
            {
                height += ((RectTransform) Results.transform).rect.height;
                Results.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }

            if (Discard != null)
            {
                height += ((RectTransform) Discard.transform).rect.height;
                Discard.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }

            foreach (ExtensibleCardZone zone in ExtraZones)
            {
                height += ((RectTransform) zone.transform).rect.height;
                zone.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }

            if (CurrentDeck != null)
            {
                height += ((RectTransform) CurrentDeck.transform).rect.height;
                CurrentDeck.transform.SetSiblingIndex(siblingIndex);
                siblingIndex++;
            }

            if (Hand != null)
            {
                height += ((RectTransform) Hand.transform).rect.height;
                Hand.transform.SetSiblingIndex(siblingIndex);
            }

            RectTransform content = scrollView.content;
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
        }

        public void CreateResults()
        {
            if (Results != null)
                return;

            Results = Instantiate(resultsZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
            Results.Viewer = this;
            ResizeContent();
        }

        public void CreateDiscard()
        {
            if (Discard != null)
                return;

            Discard = Instantiate(discardZonePrefab, scrollView.content).GetComponent<StackedZone>();
            Discard.Viewer = this;
            Discard.IsFaceup = true;
            ResizeContent();
        }

        public void CreateExtraZone(string zoneName, IEnumerable<Card> cards)
        {
            var extraZone = Instantiate(extraZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
            extraZone.labelText.text = zoneName;
            foreach (Card card in cards)
                extraZone.AddCard((UnityCard) card);
            extraZone.Viewer = this;
            ExtraZones.Add(extraZone);
            ResizeContent();
        }

        public void CreateDeck()
        {
            if (CurrentDeck != null)
                Destroy(CurrentDeck.gameObject);

            CurrentDeck = Instantiate(deckZonePrefab, scrollView.content).GetComponent<StackedZone>();
            CurrentDeck.Viewer = this;
            ResizeContent();
            IsVisible = true;
        }

        public void CreateHand()
        {
            if (Hand != null)
                return;

            Hand = Instantiate(handZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
            Hand.Viewer = this;
            ResizeContent();
            IsExtended = !IsPortrait;
        }

        private void ResetButtons()
        {
            extendButton.SetActive(!IsExtended);
            showButton.SetActive(IsExtended && !IsVisible);
            hideButton.SetActive(IsExtended && IsVisible);
        }

        public void Clear()
        {
            if (Results != null)
            {
                Results.transform.DestroyAllChildren();
                Destroy(Results.gameObject);
            }

            Results = null;

            if (Discard != null)
            {
                Discard.transform.DestroyAllChildren();
                Destroy(Discard.gameObject);
            }

            Discard = null;

            foreach (ExtensibleCardZone zone in ExtraZones)
            {
                zone.transform.DestroyAllChildren();
                Destroy(zone.gameObject);
            }

            ExtraZones.Clear();

            if (CurrentDeck != null)
            {
                CurrentDeck.transform.DestroyAllChildren();
                Destroy(CurrentDeck.gameObject);
            }

            CurrentDeck = null;

            if (Hand != null)
            {
                Hand.transform.DestroyAllChildren();
                Destroy(Hand.gameObject);
            }

            Hand = null;

            ResizeContent();
        }
    }
}
