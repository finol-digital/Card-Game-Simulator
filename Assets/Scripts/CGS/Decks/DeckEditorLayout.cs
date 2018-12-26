/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace CGS.Decks
{
    public class DeckEditorLayout : MonoBehaviour
    {
        public const float WidthCheck = 1199f;

        public Vector2 SortButtonPortraitPosition => new Vector2(15f, -((RectTransform)transform).rect.height + 87.5f);
        public static readonly Vector2 SortButtonLandscapePosition = new Vector2(187.5f, 0f);

        public Vector2 VerticalButtonsPortraitPosition => new Vector2(250f, -((RectTransform)transform).rect.height + 75f);
        public static readonly Vector2 VerticalButtonsLandscapePosition = new Vector2(365f, 0f);

        public Vector2 DeckButtonsPortraitPosition => new Vector2(0f, -((RectTransform)transform).rect.height + 87.5f);
        public static readonly Vector2 DeckButtonsLandscapePosition = new Vector2(-650f, 0f);


        public RectTransform sortButton;
        public RectTransform selectVerticalButtons;
        public RectTransform deckButtons;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            RectTransform rt = GetComponent<RectTransform>();
            float aspectRatio = rt.rect.width / rt.rect.height;
            selectVerticalButtons.gameObject.SetActive(aspectRatio < 1 || aspectRatio >= 1.5f);

            sortButton.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? SortButtonPortraitPosition : SortButtonLandscapePosition;
            selectVerticalButtons.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? VerticalButtonsPortraitPosition : VerticalButtonsLandscapePosition;
            deckButtons.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? DeckButtonsPortraitPosition : DeckButtonsLandscapePosition;
        }
    }
}
