/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

using CardGameView;

namespace Cgs.Cards
{
    public class CardsExplorerLayout : MonoBehaviour
    {
        public const float MinWidth = 1200;
        public const float CardsPortaitHeight = 5000;
        public const float CardsLandscapeHeight = 2000;

        public RectTransform cardsViewContent;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (((RectTransform)transform).rect.width < MinWidth) // Portrait
            {
                cardsViewContent.sizeDelta = new Vector2(cardsViewContent.sizeDelta.x, CardsPortaitHeight);
            }
            else // Landscape
            {
                cardsViewContent.sizeDelta = new Vector2(cardsViewContent.sizeDelta.x, CardsLandscapeHeight);
            }
        }

    }
}

