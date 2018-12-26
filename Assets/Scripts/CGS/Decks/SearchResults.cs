/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using CardGameDef;
using CardGameView;
using CGS.Cards;

namespace CGS.Decks
{
    public class SearchResults : MonoBehaviour
    {
        public const string EmptyFilterText = "*";

        public GameObject cardSearchMenuPrefab;
        public DeckEditor deckEditor;
        public RectTransform layoutArea;
        public InputField nameInputField;
        public Text filtersText;
        public Text countText;

        public int CardsPerPage => Mathf.FloorToInt(layoutArea.rect.width /
        (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.x + layoutArea.gameObject.GetOrAddComponent<HorizontalLayoutGroup>().spacing));
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
                CardModel cardModelToShow = Instantiate(deckEditor.cardModelPrefab, layoutArea).GetOrAddComponent<CardModel>();
                cardModelToShow.Value = cardToShow;
                cardModelToShow.DoesCloneOnDrag = true;
                if (((RectTransform)deckEditor.transform).rect.width > ((RectTransform)deckEditor.transform).rect.height)
                    cardModelToShow.DoubleClickAction = deckEditor.AddCardModel;
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
