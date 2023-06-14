/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.Decks
{
    public class DeckEditorLayout : MonoBehaviour
    {
        private static bool IsPortrait => Screen.orientation == ScreenOrientation.Portrait;

        private static readonly Vector2 DeckButtonsPortraitAnchor = new(0, 0.43f);
        private static readonly Vector2 DeckButtonsLandscapePosition = new(-650, 0);

        public RectTransform deckButtons;
        public RectTransform searchLayoutArea;

        public bool IsZoomed
        {
            get => _isZoomed;
            set
            {
                _isZoomed = value;
                searchLayoutArea.gameObject.SetActive(!_isZoomed);
                var deckEditorLayoutRectTransform = (RectTransform) transform;
                deckEditorLayoutRectTransform.anchorMin =
                    IsZoomed ? Vector2.zero : DeckButtonsPortraitAnchor;
                deckEditorLayoutRectTransform.offsetMin =
                    Vector2.up * (_isZoomed && IsPortrait ? 90 : 10);
            }
        }

        private bool _isZoomed;

        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (IsPortrait) // Portrait
            {
                deckButtons.anchorMin = IsZoomed ? Vector2.zero : DeckButtonsPortraitAnchor;
                deckButtons.anchorMax = IsZoomed ? Vector2.zero : DeckButtonsPortraitAnchor;
                deckButtons.pivot = Vector2.up;
                deckButtons.anchoredPosition = IsZoomed ? Vector2.up * deckButtons.rect.height : Vector2.zero;
            }
            else // Landscape
            {
                deckButtons.anchorMin = Vector2.one;
                deckButtons.anchorMax = Vector2.one;
                deckButtons.pivot = Vector2.one;
                deckButtons.anchoredPosition = DeckButtonsLandscapePosition;
            }
        }
    }
}
