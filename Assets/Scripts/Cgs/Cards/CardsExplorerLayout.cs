/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.Cards
{
    public class CardsExplorerLayout : MonoBehaviour
    {
        private const float MinWidth = 1200f;
        private const float CardsPortraitHeight = 5000f;
        private const float CardsLandscapeHeight = 2000f;

        public RectTransform cardsViewContent;

        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            var sizeDelta = cardsViewContent.sizeDelta;
            sizeDelta = ((RectTransform) transform).rect.width < MinWidth
                ? new Vector2(sizeDelta.x, CardsPortraitHeight)
                : new Vector2(sizeDelta.x, CardsLandscapeHeight);
            cardsViewContent.sizeDelta = sizeDelta;
        }
    }
}
