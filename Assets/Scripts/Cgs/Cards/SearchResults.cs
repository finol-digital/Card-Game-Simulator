/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class SearchResults : MonoBehaviour
    {
        public static string InputPrompt => $"Search {CardGameManager.Current.Name} cards";
        private const string CountSeparator = " / ";

        public GameObject cardSearchMenuPrefab;
        public GameObject cardModelPrefab;
        public RectTransform layoutArea;
        public LayoutGroup layoutGroup;
        public InputField inputField;
        public Text countText;
        public ScrollRect scrollRect;
        public bool isInteractable;

        public int CardsPerRow
        {
            get
            {
                var paddingRectOffset = layoutGroup.padding;
                float padding = 0;
                float spacing = 0;
                var cardWidth = CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.X;
                switch (layoutGroup)
                {
                    case HorizontalLayoutGroup horizontalLayoutGroup:
                    {
                        padding = paddingRectOffset.left + paddingRectOffset.right;
                        spacing = horizontalLayoutGroup.spacing;
                        break;
                    }
                    case GridLayoutGroup gridLayoutGroup:
                    {
                        padding = paddingRectOffset.left + paddingRectOffset.right;
                        spacing = gridLayoutGroup.spacing.x;
                        cardWidth = gridLayoutGroup.cellSize.x;
                        break;
                    }
                }

                return Mathf.FloorToInt((layoutArea.rect.width - padding + spacing) / (cardWidth + spacing));
            }
        }

        private int CardsPerPage
        {
            get
            {
                if (layoutArea.rect.height < CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y * 2)
                    return CardsPerRow;

                if (layoutGroup is not GridLayoutGroup gridLayoutGroup)
                    return CardsPerRow;

                var gridPadding = gridLayoutGroup.padding;
                float padding = gridPadding.top + gridPadding.bottom;
                var rowsPerPage = Mathf.FloorToInt((layoutArea.rect.height - padding + gridLayoutGroup.spacing.y)
                                                   / (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y));

                return CardsPerRow * rowsPerPage;
            }
        }

        private int TotalPageCount => CardsPerPage == 0
            ? 0
            : AllResults.Count / CardsPerPage + TotalPageCountOffset;

        private int TotalPageCountOffset => AllResults.Count % CardsPerPage == 0 ? -1 : 0;

        private CardSearchMenu CardSearcher =>
            _cardSearcher ??= Instantiate(cardSearchMenuPrefab).GetOrAddComponent<CardSearchMenu>();

        private CardSearchMenu _cardSearcher;

        private List<UnityCard> AllResults
        {
            get => _allResults ??= new List<UnityCard>();
            set
            {
                _allResults = value;
                CurrentPageIndex = 0;
                UpdateSearchResultsPanel();
            }
        }

        private List<UnityCard> _allResults;

        public int CurrentPageIndex { get; set; }

        public CardAction HorizontalDoubleClickAction { get; set; }

        private void OnEnable()
        {
            CardSearcher.SearchCallback = ShowResults;
            CardGameManager.Instance.OnSceneActions.Add(CardSearcher.ClearSearch);
            CardGameManager.Instance.OnSceneActions.Add(ResetPlaceholderText);
        }

        private void Start()
        {
            UpdateSearchResultsPanel();
        }

        private void ResetPlaceholderText()
        {
            if (inputField != null && inputField.placeholder is Text text)
                text.text = InputPrompt;
        }

        [UsedImplicitly]
        public string UpdateInputField(string input)
        {
            inputField.text = input;
            return inputField.text;
        }

        [UsedImplicitly]
        public void SetInput(string input)
        {
            CardSearcher.SetFilters(input);
        }

        [UsedImplicitly]
        public void Search()
        {
            CardSearcher.Search();
        }

        [UsedImplicitly]
        public void DecrementPage()
        {
            if (!CardViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardViewer.Instance.SelectedCardModel = null;
            CurrentPageIndex--;
            if (CurrentPageIndex < 0)
                CurrentPageIndex = TotalPageCount;
            UpdateSearchResultsPanel();
        }

        [UsedImplicitly]
        public void IncrementPage()
        {
            if (!CardViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardViewer.Instance.SelectedCardModel = null;
            CurrentPageIndex++;
            if (CurrentPageIndex > TotalPageCount)
                CurrentPageIndex = 0;
            UpdateSearchResultsPanel();
        }

        // Public to allow the layout classes to refresh on layout change
        public void UpdateSearchResultsPanel()
        {
            layoutArea.DestroyAllChildren();

            for (var i = 0;
                 i < CardsPerPage && CurrentPageIndex >= 0 && CurrentPageIndex * CardsPerPage + i < AllResults.Count;
                 i++)
            {
                var cardId = AllResults[CurrentPageIndex * CardsPerPage + i].Id;
                if (!CardGameManager.Current.Cards.ContainsKey(cardId))
                    continue;
                var cardToShow = CardGameManager.Current.Cards[cardId];
                var cardModel = Instantiate(cardModelPrefab, layoutArea).GetComponent<CardModel>();
                cardModel.Value = cardToShow;
                cardModel.IsStatic = !isInteractable;
                cardModel.DoesCloneOnDrag = isInteractable;
                if (HorizontalDoubleClickAction != null
                    && ((RectTransform) transform).rect.width > ((RectTransform) transform).rect.height)
                    cardModel.DefaultAction = HorizontalDoubleClickAction;
                else
                    cardModel.DefaultAction = CardViewer.Instance.MaximizeOn;
            }

            countText.text = (CurrentPageIndex + 1) + CountSeparator + (TotalPageCount + 1);

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1;
        }

        [UsedImplicitly]
        public void ShowSearchMenu()
        {
            CardSearcher.Show(ShowResults);
        }

        private void ShowResults(string filters, List<UnityCard> results)
        {
            inputField.text = filters;
            AllResults = results;
        }
    }
}
