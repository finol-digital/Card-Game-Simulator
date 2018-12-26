/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

using CardGameView;

namespace CGS.Decks
{
    public class SearchResultsLayout : MonoBehaviour
    {
        public const float WidthCheck = 1199f;

        public static readonly Vector2 SearchNamePortraitPosition = new Vector2(15f, 450f);
        public static readonly Vector2 SearchNameLandscapePosition = new Vector2(15f, 367.5f);

        public Vector2 PageButtonsPortraitPosition => new Vector2(GetComponent<RectTransform>().rect.width - pageButtons.rect.width, 450f);
        public static readonly Vector2 PageButtonsLandscapePosition = new Vector2(675, 367.5f);

        public RectTransform searchName;
        public RectTransform pageButtons;

        public SearchResults searchResults;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            RectTransform rt = GetComponent<RectTransform>();
            float aspectRatio = rt.rect.width / rt.rect.height;
            pageButtons.gameObject.SetActive(aspectRatio < 1 || aspectRatio >= 1.5f);

            searchName.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? SearchNamePortraitPosition : SearchNameLandscapePosition;
            pageButtons.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? PageButtonsPortraitPosition : PageButtonsLandscapePosition;

            searchResults.CurrentPageIndex = 0;
            searchResults.UpdateSearchResultsPanel();

            if (CardInfoViewer.Instance != null)
                CardInfoViewer.Instance.IsVisible = false;
        }

    }
}

