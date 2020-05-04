/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.Cards
{
    public class CardsExplorerLayout : MonoBehaviour
    {
        public const float MinWidth = 1200;
        public const float CardsPortraitHeight = 5000;
        public const float CardsLandscapeHeight = 2000;

        public RectTransform cardsViewContent;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            Vector2 sizeDelta = cardsViewContent.sizeDelta;
            sizeDelta = ((RectTransform) transform).rect.width < MinWidth
                ? new Vector2(sizeDelta.x, CardsPortraitHeight)
                : new Vector2(sizeDelta.x, CardsLandscapeHeight);
            cardsViewContent.sizeDelta = sizeDelta;
        }
    }
}
