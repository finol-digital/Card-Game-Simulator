/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using CardGameDef;
using CardGameView;

namespace CGS.Cards
{
    public class SearchResults : MonoBehaviour
    {
        public const string EmptyFilterText = "*";

        public GameObject cardSearchMenuPrefab;
        public GameObject cardModelPrefab;
        public RectTransform layoutArea;
        public LayoutGroup layoutGroup;
        public InputField nameInputField;
        public Text filtersText;
        public Text countText;

        public int CardsPerRow
        {
            get
            {
                float horizontalSpacing = 0;
                if (layoutGroup is HorizontalLayoutGroup)
                    horizontalSpacing = ((HorizontalLayoutGroup)layoutGroup).spacing;
                else if (layoutGroup is GridLayoutGroup)
                    horizontalSpacing = ((GridLayoutGroup)layoutGroup).spacing.x;
                return Mathf.FloorToInt(layoutArea.rect.width /
                    (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.x + horizontalSpacing));
            }
        }
        public int CardsPerPage
        {
            get
            {
                int rowsPerPage = 1;
                if (layoutGroup is GridLayoutGroup)
                    rowsPerPage = Mathf.FloorToInt(layoutArea.rect.height /
                        (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.y + ((GridLayoutGroup)layoutGroup).spacing.y));
                return CardsPerRow * rowsPerPage;
            }
        }
        public int TotalPageCount => CardsPerPage == 0 ? 0 : (AllResults.Count / CardsPerPage) + ((AllResults.Count % CardsPerPage) == 0 ? -1 : 0);

        public CardSearchMenu CardSearcher => _cardSearcher ??
                                              (_cardSearcher = Instantiate(cardSearchMenuPrefab).GetOrAddComponent<CardSearchMenu>());
        private CardSearchMenu _cardSearcher;
        public List<Card> AllResults
        {
            get { return _allResults ?? (_allResults = new List<Card>()); }
            set
            {
                _allResults = value;
                CurrentPageIndex = 0;
                UpdateSearchResultsPanel();
            }
        }
        private List<Card> _allResults;

        public int CurrentPageIndex { get; set; }

        public CardAction HorizontalDoubleClickAction { get; set; }

        void OnEnable()
        {
            CardSearcher.SearchCallback = ShowResults;
            CardGameManager.Instance.OnSceneActions.Add(CardSearcher.ClearSearch);
        }

        void Start()
        {
            UpdateSearchResultsPanel();
        }

        public string SetNameInputField(string nameFilter)
        {
            nameInputField.text = nameFilter;
            return nameInputField.text;
        }

        public void SetNameFilter(string nameFilter)
        {
            CardSearcher.SetNameFilter(nameFilter);
        }

        public void ClearSearchName()
        {
            CardSearcher.ClearSearchName();
        }

        public void Search()
        {
            CardSearcher.Search();
        }

        public void PageLeft()
        {
            if (!CardInfoViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardInfoViewer.Instance.SelectedCardModel = null;
            CurrentPageIndex--;
            if (CurrentPageIndex < 0)
                CurrentPageIndex = TotalPageCount;
            UpdateSearchResultsPanel();
        }

        public void PageRight()
        {
            if (!CardInfoViewer.Instance.zoomPanel.gameObject.activeSelf)
                CardInfoViewer.Instance.SelectedCardModel = null;
            CurrentPageIndex++;
            if (CurrentPageIndex > TotalPageCount)
                CurrentPageIndex = 0;
            UpdateSearchResultsPanel();
        }

        public void UpdateSearchResultsPanel()
        {
            layoutArea.DestroyAllChildren();

            for (int i = 0; i < CardsPerPage && CurrentPageIndex >= 0 && CurrentPageIndex * CardsPerPage + i < AllResults.Count; i++)
            {
                string cardId = AllResults[CurrentPageIndex * CardsPerPage + i].Id;
                if (!CardGameManager.Current.Cards.ContainsKey(cardId))
                    continue;
                Card cardToShow = CardGameManager.Current.Cards[cardId];
                CardModel cardModelToShow = Instantiate(cardModelPrefab, layoutArea).GetComponent<CardModel>();
                cardModelToShow.Value = cardToShow;
                cardModelToShow.IsStatic = layoutGroup is GridLayoutGroup;
                cardModelToShow.DoesCloneOnDrag = layoutGroup is HorizontalLayoutGroup;
                if (HorizontalDoubleClickAction != null
                        && ((RectTransform)transform).rect.width > ((RectTransform)transform).rect.height)
                    cardModelToShow.DoubleClickAction = HorizontalDoubleClickAction;
                else
                    cardModelToShow.DoubleClickAction = CardInfoViewer.Instance.ShowCardZoomed;
            }

            countText.text = (CurrentPageIndex + 1) + "/" + (TotalPageCount + 1);
        }

        public void ShowSearchMenu()
        {
            CardSearcher.Show(SetNameInputField, ShowResults);
        }

        public void ShowResults(string filters, List<Card> results)
        {
            if (string.IsNullOrEmpty(filters))
                filters = EmptyFilterText;
            filtersText.text = filters;

            AllResults = results;
        }
    }
}
