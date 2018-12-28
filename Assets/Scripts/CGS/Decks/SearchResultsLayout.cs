/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

using CardGameView;

namespace CGS.Decks
{
    public class SearchResultsLayout : MonoBehaviour
    {
        public const float MinWidth = 1200;

        public static readonly Vector2 SearchNamePortraitPosition = new Vector2(15, 450);
        public static readonly Vector2 SearchNameLandscapePosition = new Vector2(15, 367.5f);

        public static readonly Vector2 PageButtonsPortraitPosition = new Vector2(0, 447.5f);
        public static readonly Vector2 PageButtonsLandscapePosition = new Vector2(675, 367.5f);

        public RectTransform searchName;
        public RectTransform pageButtons;

        public SearchResults searchResults;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (((RectTransform)transform).rect.width < MinWidth) // Portrait
            {
                searchName.anchoredPosition = SearchNamePortraitPosition;
                pageButtons.anchorMin = Vector2.right;
                pageButtons.anchorMax = Vector2.right;
                pageButtons.pivot = Vector2.right;
                pageButtons.anchoredPosition = PageButtonsPortraitPosition;
            }
            else // Landscape
            {
                searchName.anchoredPosition = SearchNameLandscapePosition;
                pageButtons.anchorMin = Vector2.zero;
                pageButtons.anchorMax = Vector2.zero;
                pageButtons.pivot = Vector2.zero;
                pageButtons.anchoredPosition = PageButtonsLandscapePosition;
            }

            // TODO: CORRECTLY RE-MAP TO CURRENT PAGE
            searchResults.CurrentPageIndex = 0;
            searchResults.UpdateSearchResultsPanel();
            if (CardInfoViewer.Instance != null)
                CardInfoViewer.Instance.IsVisible = false;
        }

    }
}

