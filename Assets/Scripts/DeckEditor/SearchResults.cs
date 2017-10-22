using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SearchResults : MonoBehaviour
{
    public const string EmptyFilterText = "*";

    public GameObject cardSearchMenuPrefab;
    public DeckEditor deckEditor;
    public RectTransform layoutArea;
    public InputField nameInputField;
    public Text filtersText;
    public Text countText;

    public int CardsPerPage {
        get { return Mathf.FloorToInt(layoutArea.rect.width / (deckEditor.cardModelPrefab.GetComponent<RectTransform>().rect.width + layoutArea.gameObject.GetOrAddComponent<HorizontalLayoutGroup>().spacing)); }
    }

    public int TotalPageCount {
        get { return CardsPerPage == 0 ? 0 : (AllResults.Count / CardsPerPage) + ((AllResults.Count % CardsPerPage) == 0 ? -1 : 0); }
    }

    public int CurrentPageIndex { get; set; }

    private CardSearchMenu _cardSearcher;
    private List<Card> _allResults;

    void OnEnable()
    {
        CardSearcher.SearchCallback = ShowResults;
        CardGameManager.Instance.OnSelectActions.Add(CardSearcher.ClearSearch);
    }

    public string SetNameInputField(string name)
    {
        nameInputField.text = name;
        return nameInputField.text;
    }

    public void SetNameFilter(string name)
    {
        CardSearcher.NameFilter = name;
    }

    public void SetFiltersText(string filters)
    {
        if (string.IsNullOrEmpty(filters))
            filters = EmptyFilterText;
        filtersText.text = filters;
    }

    public void Search()
    {
        CardSearcher.Search();
    }

    public void MoveLeft()
    {
        CurrentPageIndex--;
        if (CurrentPageIndex < 0)
            CurrentPageIndex = TotalPageCount;
        UpdateSearchResultsPanel();
    }

    public void MoveRight()
    {
        CurrentPageIndex++;
        if (CurrentPageIndex > TotalPageCount)
            CurrentPageIndex = 0;
        UpdateSearchResultsPanel();
    }

    public void UpdateSearchResultsPanel()
    {
        layoutArea.DestroyAllChildren();

        for (int i = 0; i < CardsPerPage && CurrentPageIndex >= 0 && CurrentPageIndex * CardsPerPage + i < AllResults.Count; i++) {
            string cardId = AllResults [CurrentPageIndex * CardsPerPage + i].Id;
            if (!CardGameManager.Current.Cards.ContainsKey(cardId))
                continue;
            Card cardToShow = CardGameManager.Current.Cards [cardId];
            CardModel cardModelToShow = Instantiate(deckEditor.cardModelPrefab, layoutArea).GetOrAddComponent<CardModel>();
            cardModelToShow.Card = cardToShow;
            cardModelToShow.DoesCloneOnDrag = true;
            cardModelToShow.DoubleClickEvent = new OnDoubleClickDelegate(deckEditor.AddCardModel);
        }

        countText.text = (CurrentPageIndex + 1) + "/" + (TotalPageCount + 1);
    }

    public void ShowSearchMenu()
    {
        CardSearcher.Show(SetNameInputField, SetFiltersText, ShowResults);
    }

    public void ShowResults(List<Card> results)
    {
        AllResults = results;
    }

    void OnDisable()
    {
        if (CardGameManager.HasInstance)
            CardGameManager.Instance.OnSelectActions.Remove(CardSearcher.ClearSearch);
    }

    public CardSearchMenu CardSearcher {
        get {
            if (_cardSearcher == null)
                _cardSearcher = Instantiate(cardSearchMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<CardSearchMenu>();
            return _cardSearcher;
        }
    }

    public List<Card> AllResults {
        get {
            if (_allResults == null)
                _allResults = new List<Card>();
            return _allResults;
        }
        set {
            _allResults = value;
            CurrentPageIndex = 0;
            UpdateSearchResultsPanel();
        }
    }
}
