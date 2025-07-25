/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Decks
{
    public class DeckEditorLayout : MonoBehaviour
    {
        private const float Buffer = 20f;
        private const float SearchAreaPortraitHeight = 550f;
        private const float SearchAreaLandscapeWidth = 500f;

        public bool IsPortrait => ((RectTransform)transform).rect.width < 1200f;

        public RectTransform deckLabelContainer;
        public RectTransform deckLabel;
        public RectTransform deckButtonsContainer;
        public RectTransform deckEditorButtonsGroup;
        public RectTransform deckEditorLayoutArea;

        public RectTransform searchArea;
        public GridLayoutGroup searchAreaGridLayoutGroup;

        public RectTransform cardCountLabel;

        public RectTransform dockedCardViewer;

        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (IsPortrait) // Portrait
            {
                deckLabel.offsetMax = Vector2.zero;
                deckEditorButtonsGroup.SetParent(deckButtonsContainer);
                deckEditorButtonsGroup.anchoredPosition = Vector2.zero;
                deckEditorButtonsGroup.localScale = Vector3.one;
                deckEditorLayoutArea.offsetMin =
                    new Vector2(deckEditorLayoutArea.offsetMin.x, SearchAreaPortraitHeight + 100);
                deckEditorLayoutArea.offsetMax = new Vector2(-Buffer, deckEditorLayoutArea.offsetMax.y);
                searchArea.anchorMin = Vector2.zero;
                searchArea.anchorMax = Vector2.right;
                searchArea.pivot = Vector2.one;
                searchArea.offsetMin = Vector2.down * SearchAreaPortraitHeight;
                searchArea.offsetMax = Vector2.zero;
                searchArea.anchoredPosition = Vector2.up * SearchAreaPortraitHeight;
                searchAreaGridLayoutGroup.childAlignment = TextAnchor.LowerCenter;
                cardCountLabel.anchoredPosition = Vector2.zero;
                dockedCardViewer.offsetMin = new Vector2(0, SearchAreaPortraitHeight + 100);
            }
            else // Landscape
            {
                deckLabel.offsetMax = new Vector2(525, 0);
                deckEditorButtonsGroup.SetParent(deckLabelContainer);
                deckEditorButtonsGroup.anchoredPosition = Vector2.zero;
                deckEditorButtonsGroup.localScale = Vector3.one;
                deckEditorLayoutArea.offsetMin = new Vector2(deckEditorLayoutArea.offsetMin.x, Buffer);
                deckEditorLayoutArea.offsetMax =
                    new Vector2(-SearchAreaLandscapeWidth, deckEditorLayoutArea.offsetMax.y);
                searchArea.anchorMin = Vector2.right;
                searchArea.anchorMax = Vector2.one;
                searchArea.pivot = Vector2.one;
                searchArea.offsetMin = Vector2.left * SearchAreaLandscapeWidth;
                searchArea.offsetMax = Vector2.zero;
                searchArea.anchoredPosition = Vector2.zero;
                searchAreaGridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                cardCountLabel.anchoredPosition = Vector2.zero;
                dockedCardViewer.offsetMin = Vector2.zero;
            }
        }
    }
}
