using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchResults : MonoBehaviour
{
    public const string SearchNameInput = "SearchName";
    public const string FilterMenuInput = "FilterMenu";
    public const string EmptyFilterText = "*";

    public GameObject cardSearchMenuPrefab;
    public DeckEditor deckEditor;
    public RectTransform layoutArea;
    public InputField nameInputField;
    public Text filtersText;
    public Text countText;

    public int CardsPerPage => Mathf.FloorToInt(layoutArea.rect.width / (deckEditor.cardModelPrefab.GetComponent<RectTransform>().rect.width + layoutArea.gameObject.GetOrAddComponent<HorizontalLayoutGroup>().spacing));
    public int TotalPageCount => CardsPerPage == 0 ? 0 : (AllResults.Count / CardsPerPage) + ((AllResults.Count % CardsPerPage) == 0 ? -1 : 0);

    public CardSearchMenu CardSearcher => _cardSearcher ??
                                          (_cardSearcher = Instantiate(cardSearchMenuPrefab).GetOrAddComponent<CardSearchMenu>());

    public int CurrentPageIndex { get; set; }

    private CardSearchMenu _cardSearcher;
    private List<Card> _allResults;

    void OnEnable()
    {
        CardSearcher.SearchCallback = ShowResults;
        CardGameManager.Instance.OnSceneActions.Add(CardSearcher.ClearSearch);
    }
    
    void Update()
    {
        if (Input.GetButtonUp(SearchNameInput))
            nameInputField.ActivateInputField();
        else if (Input.GetButtonUp(FilterMenuInput))
            ShowSearchMenu();
    }

    public string SetNameInputField(string nameFilter)
    {
        nameInputField.text = nameFilter;
        return nameInputField.text;
    }

    public void SetNameFilter(string nameFilter)
    {
        CardSearcher.NameFilter = nameFilter;
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
            cardModelToShow.Value = cardToShow;
            cardModelToShow.DoesCloneOnDrag = true;
            cardModelToShow.DoubleClickAction = deckEditor.AddCardModel;
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

    public List<Card> AllResults
    {
        get { return _allResults ?? (_allResults = new List<Card>()); }
        set {
            _allResults = value;
            CurrentPageIndex = 0;
            UpdateSearchResultsPanel();
        }
    }
}
