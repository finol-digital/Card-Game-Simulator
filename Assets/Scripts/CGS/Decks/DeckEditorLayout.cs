/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

using CardGameView;

namespace CGS.Decks
{
    public class DeckEditorLayout : MonoBehaviour
    {
        public const float WidthCheck = 1199f;

        public Vector2 SortButtonPortraitPosition => new Vector2(15f, -(deckEditorLayout.rect.height + 87.5f));
        public static readonly Vector2 SortButtonLandscapePosition = new Vector2(187.5f, 0f);

        public Vector2 VerticalButtonsPortraitPosition => new Vector2(250f, -(deckEditorLayout.rect.height + 75f));
        public static readonly Vector2 VerticalButtonsLandscapePosition = new Vector2(365f, 0f);

        public Vector2 DeckButtonsPortraitPosition => new Vector2(0f, -(deckEditorLayout.rect.height + 87.5f));
        public static readonly Vector2 DeckButtonsLandscapePosition = new Vector2(-650f, 0f);

        public static readonly Vector2 SearchNamePortraitPosition = new Vector2(15f, 450f);
        public static readonly Vector2 SearchNameLandscapePosition = new Vector2(15f, 367.5f);

        public Vector2 HorizontalButtonsPortraitPosition => new Vector2(GetComponent<RectTransform>().rect.width - horizontalButtons.rect.width, 450f);
        public static readonly Vector2 HorizontalButtonsLandscapePosition = new Vector2(675, 367.5f);

        public RectTransform sortButton;
        public RectTransform verticalButtons;
        public RectTransform deckButtons;
        public RectTransform searchName;
        public RectTransform horizontalButtons;

        public RectTransform deckEditorLayout;
        public SearchResults searchResults;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            RectTransform rt = GetComponent<RectTransform>();
            float aspectRatio = rt.rect.width / rt.rect.height;
            verticalButtons.gameObject.SetActive(aspectRatio < 1 || aspectRatio >= 1.5f);
            horizontalButtons.gameObject.SetActive(aspectRatio < 1 || aspectRatio >= 1.5f);

            sortButton.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? SortButtonPortraitPosition : SortButtonLandscapePosition;
            verticalButtons.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? VerticalButtonsPortraitPosition : VerticalButtonsLandscapePosition;
            deckButtons.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? DeckButtonsPortraitPosition : DeckButtonsLandscapePosition;

            searchName.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? SearchNamePortraitPosition : SearchNameLandscapePosition;
            horizontalButtons.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? HorizontalButtonsPortraitPosition : HorizontalButtonsLandscapePosition;

            searchResults.CurrentPageIndex = 0;
            searchResults.UpdateSearchResultsPanel();
            if (CardInfoViewer.Instance != null)
                CardInfoViewer.Instance.IsVisible = false;
        }
    }
}
