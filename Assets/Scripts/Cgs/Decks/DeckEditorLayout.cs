/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.Decks
{
    public class DeckEditorLayout : MonoBehaviour
    {
        public bool IsPortrait => searchResultsLayout.IsPortrait;

        public static readonly Vector2 DeckButtonsPortraitAnchor = new Vector2(0, 0.43f);
        private static readonly Vector2 DeckButtonsLandscapePosition = new Vector2(-650, 0);

        private static readonly Vector2 SelectButtonsPortraitPosition = new Vector2(0, 10);
        private static readonly Vector2 SelectButtonsLandscapePosition = new Vector2(-350, 10);

        public RectTransform deckButtons;
        public RectTransform selectButtons;
        public DeckEditor deckEditor;
        public SearchResultsLayout searchResultsLayout;

        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (IsPortrait) // Portrait
            {
                deckButtons.anchorMin = deckEditor.IsZoomed ? Vector2.zero : DeckButtonsPortraitAnchor;
                deckButtons.anchorMax = deckEditor.IsZoomed ? Vector2.zero : DeckButtonsPortraitAnchor;
                deckButtons.pivot = Vector2.up;
                deckButtons.anchoredPosition =
                    deckEditor.IsZoomed ? Vector2.up * deckButtons.rect.height : Vector2.zero;
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
