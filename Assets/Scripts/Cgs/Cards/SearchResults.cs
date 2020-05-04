/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using CardGameDef.Unity;
using CardGameView;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class SearchResults : MonoBehaviour
    {
        public static string InputPrompt => $"Search {CardGameManager.Current.Name} cards";
        public const string CountSeparator = " / ";

        public GameObject cardSearchMenuPrefab;
        public GameObject cardModelPrefab;
        public RectTransform layoutArea;
        public LayoutGroup layoutGroup;
        public InputField inputField;
        public Text countText;
        public ScrollRect scrollRect;

        public int CardsPerRow
        {
            get
            {
                float padding = 0;
                float spacing = 0;
                float cardWidth = CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.X;
                if (layoutGroup is HorizontalLayoutGroup horizontalLayoutGroup)
                    spacing = horizontalLayoutGroup.spacing;
                else if (layoutGroup is GridLayoutGroup gridLayoutGroup)
                {
                    RectOffset gridPadding = gridLayoutGroup.padding;
                    padding = gridPadding.left + gridPadding.right;
                    spacing = gridLayoutGroup.spacing.x;
                    cardWidth = gridLayoutGroup.cellSize.x;
                }

                return Mathf.FloorToInt((layoutArea.rect.width - padding + spacing) / (cardWidth + spacing));
            }
        }

        public int CardsPerPage
        {
            get
            {
                var rowsPerPage = 1;
                if (!(layoutGroup is GridLayoutGroup gridLayoutGroup))
                    return CardsPerRow * rowsPerPage;

                RectOffset gridPadding = gridLayoutGroup.padding;
                float padding = gridPadding.top + gridPadding.bottom;
                rowsPerPage = Mathf.FloorToInt((layoutArea.rect.height - padding + gridLayoutGroup.spacing.y)
                                               / (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y));

                return CardsPerRow * rowsPerPage;
            }
        }

        public int TotalPageCount => CardsPerPage == 0
            ? 0
            : (AllResults.Count / CardsPerPage) + ((AllResults.Count % CardsPerPage) == 0 ? -1 : 0);

        public CardSearchMenu CardSearcher => _cardSearcher ??
                                              (_cardSearcher = Instantiate(cardSearchMenuPrefab)
                                                  .GetOrAddComponent<CardSearchMenu>());

        private CardSearchMenu _cardSearcher;

        public List<UnityCard> AllResults
        {
            get => _allResults ?? (_allResults = new List<UnityCard>());
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

        void OnEnable()
        {
            CardSearcher.SearchCallback = ShowResults;
            CardGameManager.Instance.OnSceneActions.Add(CardSearcher.ClearSearch);
            CardGameManager.Instance.OnSceneActions.Add(ResetPlaceholderText);
        }

        void Start()
        {
            UpdateSearchResultsPanel();
        }

        public void ResetPlaceholderText()
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

        public void Search()
        {
            CardSearcher.Search();
        }

        public void DecrementPage()
        {
            if (!CardViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardViewer.Instance.SelectedCardModel = null;
            CurrentPageIndex--;
            if (CurrentPageIndex < 0)
                CurrentPageIndex = TotalPageCount;
            UpdateSearchResultsPanel();
        }

        public void IncrementPage()
        {
            if (!CardViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardViewer.Instance.SelectedCardModel = null;
            CurrentPageIndex++;
            if (CurrentPageIndex > TotalPageCount)
                CurrentPageIndex = 0;
            UpdateSearchResultsPanel();
        }

        public void UpdateSearchResultsPanel()
        {
            layoutArea.DestroyAllChildren();

            for (var i = 0;
                i < CardsPerPage && CurrentPageIndex >= 0 && CurrentPageIndex * CardsPerPage + i < AllResults.Count;
                i++)
            {
                string cardId = AllResults[CurrentPageIndex * CardsPerPage + i].Id;
                if (!CardGameManager.Current.Cards.ContainsKey(cardId))
                    continue;
                UnityCard cardToShow = CardGameManager.Current.Cards[cardId];
                var cardModel = Instantiate(cardModelPrefab, layoutArea).GetComponent<CardModel>();
                cardModel.Value = cardToShow;
                cardModel.IsStatic = layoutGroup is GridLayoutGroup;
                cardModel.DoesCloneOnDrag = layoutGroup is HorizontalLayoutGroup;
                if (HorizontalDoubleClickAction != null
                    && ((RectTransform) transform).rect.width > ((RectTransform) transform).rect.height)
                    cardModel.DoubleClickAction = HorizontalDoubleClickAction;
                else
                    cardModel.DoubleClickAction = CardViewer.Instance.MaximizeOn;
            }

            countText.text = (CurrentPageIndex + 1) + CountSeparator + (TotalPageCount + 1);

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1;
        }

        public void ShowSearchMenu()
        {
            CardSearcher.Show(ShowResults);
        }

        public void ShowResults(string filters, List<UnityCard> results)
        {
            inputField.text = filters;
            AllResults = results;
        }
    }
}
