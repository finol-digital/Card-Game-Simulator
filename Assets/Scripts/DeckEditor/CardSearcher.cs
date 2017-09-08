using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CardSearcher : MonoBehaviour
{
    public string nameFilter { get; set; }

    public string idFilter { get; set; }

    public string setCodeFilter { get; set; }

    public GameObject cardPrefab;
    public DeckEditor deckEditor;
    public RectTransform advancedSearchFilterMenu;
    public RectTransform filterContentView;
    public RectTransform nameProperty;
    public RectTransform idProperty;
    public RectTransform setProperty;
    public RectTransform propertyTemplate;
    public RectTransform resultsPanel;
    public Text resultsCountText;

    private List<Card> _searchResults;
    private int _resultsIndex;
    private Dictionary<string, string> _propertyFilters;

    void Start()
    {
        nameFilter = "";
        idFilter = "";
        setCodeFilter = "";
        CardGameManager.Instance.AddOnSelectAction(UpdateCardSearcher);
    }

    public void UpdateCardSearcher()
    {
        Debug.Log("Building the Advanced Search Filter Menu");
        propertyTemplate.gameObject.SetActive(true);
        nameProperty.SetParent(advancedSearchFilterMenu);
        idProperty.SetParent(advancedSearchFilterMenu);
        setProperty.SetParent(advancedSearchFilterMenu);
        propertyTemplate.SetParent(advancedSearchFilterMenu);
        filterContentView.DestroyAllChildren();
        nameProperty.SetParent(filterContentView);
        idProperty.SetParent(filterContentView);
        setProperty.SetParent(filterContentView);
        propertyTemplate.SetParent(filterContentView);
        Vector2 pos = propertyTemplate.localPosition;
        foreach (PropertyDef prop in CardGameManager.Current.CardProperties) {
            GameObject newProp = Instantiate(propertyTemplate.gameObject, propertyTemplate.position, propertyTemplate.rotation, filterContentView) as GameObject;
            newProp.transform.localPosition = pos;
            PropertyEditor editor = newProp.GetComponent<PropertyEditor>();
            editor.nameLabel.text = prop.Name;
            UnityAction<string> textChange = new UnityAction<string>(text => SetPropertyFilter(prop.Name, text));
            editor.inputField.onValueChanged.AddListener(textChange);
            editor.placeHolderText.text = "Enter " + prop.Name + "...";
            pos.y -= propertyTemplate.rect.height;
        }
        propertyTemplate.gameObject.SetActive(false);
        filterContentView.sizeDelta = new Vector2(filterContentView.sizeDelta.x, propertyTemplate.rect.height * CardGameManager.Current.CardProperties.Count + propertyTemplate.rect.height * 3);

        Debug.Log("Showing all cards in the search results");
        ClearFilters();
        Search();
    }

    public void SetPropertyFilter(string key, string val)
    {
        PropertyFilters [key] = val;
    }

    public void ClearFilters()
    {
        foreach (InputField input in advancedSearchFilterMenu.GetComponentsInChildren<InputField>())
            input.text = "";
        PropertyFilters.Clear();
    }

    public void Search()
    {
        SearchResults.Clear();
        _resultsIndex = 0;
        IEnumerable<Card> cardSearcher = CardGameManager.Current.FilterCards(idFilter, nameFilter, setCodeFilter, PropertyFilters);
        foreach (Card card in cardSearcher)
            SearchResults.Add(card);
        UpdateSearchResultsPanel();
    }

    public void MoveSearchResultsLeft()
    {
        _resultsIndex--;
        if (_resultsIndex < 0)
            _resultsIndex = ResultRowCount;
        UpdateSearchResultsPanel();
    }

    public void MoveSearchResultsRight()
    {
        _resultsIndex++;
        if (_resultsIndex > ResultRowCount)
            _resultsIndex = 0;
        UpdateSearchResultsPanel();
    }

    public void UpdateSearchResultsPanel()
    {
        resultsPanel.DestroyAllChildren();

        for (int i = 0; i < ResultsPanelSize && _resultsIndex >= 0 && _resultsIndex * ResultsPanelSize + i < SearchResults.Count; i++) {
            string cardId = SearchResults [_resultsIndex * ResultsPanelSize + i].Id;
            Card cardToShow = CardGameManager.Current.Cards.Where(card => card.Id == cardId).FirstOrDefault();
            CardModel cardModelToShow = Instantiate(cardPrefab, resultsPanel).transform.GetOrAddComponent<CardModel>();
            cardModelToShow.SetAsCard(cardToShow, true, new OnDoubleClickDelegate(deckEditor.AddCard));
        }

        resultsCountText.text = (_resultsIndex + 1) + " / " + (ResultRowCount + 1);
    }

    public void ShowAdvancedFilterPanel()
    {
        advancedSearchFilterMenu.gameObject.SetActive(true);
        advancedSearchFilterMenu.SetAsLastSibling();
    }

    public void HideAdvancedFilterPanel()
    {
        advancedSearchFilterMenu.gameObject.SetActive(false);
    }

    void Update()
    {
        if (advancedSearchFilterMenu.gameObject.activeSelf && Input.GetButtonDown("Submit")) {
            Search();
            HideAdvancedFilterPanel();
        }
    }

    public List<Card> SearchResults {
        get {
            if (_searchResults == null)
                _searchResults = new List<Card>();
            return _searchResults;
        }
    }

    public int ResultsPanelSize {
        get {
            return Mathf.FloorToInt(resultsPanel.rect.width / (cardPrefab.GetComponent<RectTransform>().rect.width + (resultsPanel.GetOrAddComponent<HorizontalLayoutGroup>().spacing / 2)));
        }
    }

    public int ResultRowCount {
        get {
            return (SearchResults.Count / ResultsPanelSize) + (SearchResults.Count % ResultsPanelSize == 0 ? -1 : 0);
        }
    }

    public Dictionary<string, string> PropertyFilters {
        get {
            if (_propertyFilters == null)
                _propertyFilters = new Dictionary<string, string>();
            return _propertyFilters;
        }
    }
}
