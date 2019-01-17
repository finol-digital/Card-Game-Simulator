/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

using CardGameView;

namespace CGS.Cards
{
    public class CardsExplorerLayout : MonoBehaviour
    {
        public const float MinWidth = 1200;
        public const float CardsPortaitHeight = 5000;
        public const float CardsLandscapeHeight = 2000;

        public static readonly Vector2 SearchNameLandscapePosition = new Vector2(200, 0);
        public static readonly Vector2 CardsViewPortraitOffset = new Vector2(0, -160);
        public static readonly Vector2 CardsViewLandscapeOffset = new Vector2(0, -72.5f);

        public RectTransform searchName;
        public RectTransform searchFilters;
        public RectTransform cardsView;
        public RectTransform cardsViewContent;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (((RectTransform)transform).rect.width < MinWidth) // Portrait
            {
                searchName.anchorMin = Vector2.one;
                searchName.anchorMax = Vector2.one;
                searchName.pivot = Vector2.one;
                searchName.anchoredPosition = Vector2.zero;
                searchFilters.anchoredPosition = new Vector2(0, -searchName.rect.height);
                cardsView.offsetMax = CardsViewPortraitOffset;
                cardsViewContent.sizeDelta = new Vector2(cardsViewContent.sizeDelta.x, CardsPortaitHeight);
            }
            else // Landscape
            {
                searchName.anchorMin = Vector2.up;
                searchName.anchorMax = Vector2.up;
                searchName.pivot = Vector2.up;
                searchName.anchoredPosition = SearchNameLandscapePosition;
                searchFilters.anchoredPosition = Vector2.zero;
                cardsView.offsetMax = CardsViewLandscapeOffset;
                cardsViewContent.sizeDelta = new Vector2(cardsViewContent.sizeDelta.x, CardsLandscapeHeight);
            }
        }

    }
}

