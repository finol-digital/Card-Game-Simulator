/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace CGS.Decks
{
    public class DeckEditorLayout : MonoBehaviour
    {
        public const float MinWidth = 1200;

        public static readonly Vector2 DeckButtonsPortraitAnchor = new Vector2(0, 0.43f);
        public static readonly Vector2 DeckButtonsLandscapePosition = new Vector2(-650, 0);

        public static readonly Vector2 SelectButtonsPortraitPosition = new Vector2(0, 8);
        public static readonly Vector2 SelectButtonsLandscapePosition = new Vector2(-850, 8);

        public RectTransform deckButtons;
        public RectTransform selectButtons;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (((RectTransform)transform).rect.width < MinWidth) // Portrait
            {
                deckButtons.anchorMin = DeckButtonsPortraitAnchor;
                deckButtons.anchorMax = DeckButtonsPortraitAnchor;
                deckButtons.pivot = Vector2.up;
                deckButtons.anchoredPosition = Vector2.zero;
                selectButtons.anchoredPosition = SelectButtonsPortraitPosition;
            }
            else // Landscape
            {
                deckButtons.anchorMin = Vector2.one;
                deckButtons.anchorMax = Vector2.one;
                deckButtons.pivot = Vector2.one;
                deckButtons.anchoredPosition = DeckButtonsLandscapePosition;
                selectButtons.anchoredPosition = SelectButtonsLandscapePosition;
            }
        }
    }
}
