/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

using CardGameView;
using Cgs.Cards;

namespace Cgs.Decks
{
    public class SearchResultsLayout : MonoBehaviour
    {
        public static readonly Vector2 PageButtonsPortraitPosition = new Vector2(0, 447.5f);
        public static readonly Vector2 PageButtonsLandscapePosition = new Vector2(1050, 375);

        public RectTransform pageButtons;
        public SearchResults searchResults;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (((RectTransform)transform).rect.width < CardsExplorerLayout.MinWidth) // Portrait
            {
                pageButtons.anchorMin = Vector2.right;
                pageButtons.anchorMax = Vector2.right;
                pageButtons.pivot = Vector2.right;
                pageButtons.anchoredPosition = PageButtonsPortraitPosition;
            }
            else // Landscape
            {
                pageButtons.anchorMin = Vector2.zero;
                pageButtons.anchorMax = Vector2.zero;
                pageButtons.pivot = Vector2.zero;
                pageButtons.anchoredPosition = PageButtonsLandscapePosition;
            }

            // TODO: CORRECTLY RE-MAP TO CURRENT PAGE
            searchResults.CurrentPageIndex = 0;
            searchResults.UpdateSearchResultsPanel();
            if (CardViewer.Instance != null)
                CardViewer.Instance.IsVisible = false;
        }

    }
}

