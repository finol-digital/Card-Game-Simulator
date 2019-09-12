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
                float horizontalSpacing = 0;
                if (layoutGroup is HorizontalLayoutGroup)
                    horizontalSpacing = ((HorizontalLayoutGroup)layoutGroup).spacing;
                else if (layoutGroup is GridLayoutGroup)
                    horizontalSpacing = ((GridLayoutGroup)layoutGroup).spacing.x;
                return Mathf.FloorToInt((layoutArea.rect.width -
                    (layoutGroup is GridLayoutGroup ? ((GridLayoutGroup)layoutGroup).padding.left + ((GridLayoutGroup)layoutGroup).padding.right : 0))
                    / (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.x + horizontalSpacing));
            }
        }
        public int CardsPerPage
        {
            get
            {
                int rowsPerPage = 1;
                if (layoutGroup is GridLayoutGroup)
                {
                    rowsPerPage = Mathf.FloorToInt((layoutArea.rect.height -
                        (((GridLayoutGroup)layoutGroup).padding.top - ((GridLayoutGroup)layoutGroup).padding.bottom))
                        / (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.y + ((GridLayoutGroup)layoutGroup).spacing.y));
                }
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
            CardGameManager.Instance.OnSceneActions.Add(ResetPlaceholderText);
        }

        void Start()
        {
            UpdateSearchResultsPanel();
        }

        public void ResetPlaceholderText()
        {
            if (inputField != null && inputField.placeholder is Text)
                (inputField.placeholder as Text).text = InputPrompt;
        }

        public string UpdateInputField(string input)
        {
            inputField.text = input;
            return inputField.text;
        }

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
                    cardModelToShow.DoubleClickAction = CardViewer.Instance.MaximizeOn;
            }

            countText.text = (CurrentPageIndex + 1) + CountSeparator + (TotalPageCount + 1);

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1;
        }

        public void ShowSearchMenu()
        {
            CardSearcher.Show(ShowResults);
        }

        public void ShowResults(string filters, List<Card> results)
        {
            inputField.text = filters;
            AllResults = results;
        }
    }
}
