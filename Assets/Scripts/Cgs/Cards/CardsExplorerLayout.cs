/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class CardsExplorerLayout : MonoBehaviour
    {
        private const float MinWidth = 1500f;
        private const float CardsPortraitHeight = 5000f;
        private const float CardsLandscapeHeight = 2000f;
        private const float FooterRowHeight = 100f;
        private const float FooterButtonWidth = 175f;
        private const float FooterButtonHeight = 80f;
        private const float FooterButtonBuffer = 20f;

        private static readonly Vector2 FooterButtonRowPivot = new(0.5f, 0);
        private static readonly Vector2 NewButtonPosition = new(65, 2.5f);
        private static readonly Vector2 EditButtonPosition = new(240, 2.5f);
        private static readonly Vector2 DeleteButtonPosition = new(-65, 2.5f);
        private static readonly Vector2 FooterButtonRowPosition = new(0, 2.5f);

        private bool IsPortrait => ((RectTransform) transform).rect.width < MinWidth;

        private bool AreEditButtonsVisible => newCardButton != null && newCardButton.gameObject.activeSelf;

        private bool HasTwoFooterRows => IsPortrait && AreEditButtonsVisible;

        public RectTransform cardsViewContent;
        public GridLayoutGroup cardsViewGrid;

        public RectTransform footer;
        public RectTransform pageCountText;
        public RectTransform newCardButton;
        public RectTransform editCardButton;
        public RectTransform deleteCardButton;

        private void OnRectTransformDimensionsChange()
        {
            ResetLayout();
        }

        public void ResetLayout()
        {
            if (!gameObject.activeInHierarchy)
                return;

            var hasTwoFooterRows = HasTwoFooterRows;
            var footerHeight = hasTwoFooterRows ? FooterRowHeight * 2 : FooterRowHeight;

            footer.sizeDelta = new Vector2(footer.sizeDelta.x, footerHeight);
            pageCountText.anchoredPosition = new Vector2(0, footerHeight - FooterRowHeight);
            if (hasTwoFooterRows)
                SetFooterButtonsToSecondRow();
            else
                SetFooterButtonsToFirstRow();

            SetCardsViewBottomPadding(Mathf.RoundToInt(footerHeight));

            var sizeDelta = cardsViewContent.sizeDelta;
            cardsViewContent.sizeDelta =
                new Vector2(sizeDelta.x, IsPortrait ? CardsPortraitHeight : CardsLandscapeHeight);
        }

        private void SetFooterButtonsToFirstRow()
        {
            var buttonSize = new Vector2(FooterButtonWidth, FooterButtonHeight);
            SetFooterButton(newCardButton, Vector2.zero, Vector2.zero, buttonSize, NewButtonPosition);
            SetFooterButton(editCardButton, Vector2.zero, Vector2.zero, buttonSize, EditButtonPosition);
            SetFooterButton(deleteCardButton, Vector2.right, Vector2.right, buttonSize, DeleteButtonPosition);
        }

        private void SetFooterButtonsToSecondRow()
        {
            var buttonWidth = Mathf.Clamp(footer.rect.width / 3f - FooterButtonBuffer, 0, FooterButtonWidth);
            var buttonSize = new Vector2(buttonWidth, FooterButtonHeight);
            SetFooterButton(newCardButton, new Vector2(1 / 6f, 0), FooterButtonRowPivot, buttonSize,
                FooterButtonRowPosition);
            SetFooterButton(editCardButton, new Vector2(3 / 6f, 0), FooterButtonRowPivot, buttonSize,
                FooterButtonRowPosition);
            SetFooterButton(deleteCardButton, new Vector2(5 / 6f, 0), FooterButtonRowPivot, buttonSize,
                FooterButtonRowPosition);
        }

        private static void SetFooterButton(RectTransform button, Vector2 anchor, Vector2 pivot, Vector2 size,
            Vector2 position)
        {
            button.anchorMin = anchor;
            button.anchorMax = anchor;
            button.pivot = pivot;
            button.sizeDelta = size;
            button.anchoredPosition = position;
        }

        private void SetCardsViewBottomPadding(int bottom)
        {
            var padding = cardsViewGrid.padding;
            if (padding.bottom == bottom)
                return;
            cardsViewGrid.padding = new RectOffset(padding.left, padding.right, padding.top, bottom);
        }
    }
}
