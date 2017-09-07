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
    public RectTransform propertyTemplate;
    public RectTransform resultsPanel;
    public Text resultsCountText;

    private Transform _cardModelStaging;
    private Dictionary<string, CardModel> _allCardModels;
    private List<Card> _searchResults;
    private int _resultsPanelSize;
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
        Vector2 pos = propertyTemplate.localPosition;
        foreach (PropertyDef prop in CardGameManager.Current.CardProperties) {
            GameObject newProp = Instantiate(propertyTemplate.gameObject, propertyTemplate.position, propertyTemplate.rotation, propertyTemplate.parent) as GameObject;
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
        Debug.Log("Searching with id " + idFilter + ", name " + nameFilter + ", setCode " + setCodeFilter);
        string debugFilters = "Search property filters: ";
        foreach (KeyValuePair<string, string> entry in PropertyFilters)
            debugFilters += entry.Key + ": " + entry.Value + "; ";
        Debug.Log(debugFilters);

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
        for (int i = resultsPanel.childCount - 1; i >= 0; i--) {
            resultsPanel.GetChild(i).SetParent(CardModelStaging);
        }

        for (int i = 0; i < ResultsPanelSize && _resultsIndex >= 0 && _resultsIndex * ResultsPanelSize + i < SearchResults.Count; i++) {
            string cardId = SearchResults [_resultsIndex * ResultsPanelSize + i].Id;

            CardModel cardModelToShow;
            if (!AllCardModels.TryGetValue(cardId, out cardModelToShow)) {
                Debug.Log("Creating Card Model for " + cardId);
                Card cardToShow = CardGameManager.Current.Cards.Where(card => card.Id == cardId).FirstOrDefault();
                cardModelToShow = Instantiate(cardPrefab, resultsPanel).transform.GetOrAddComponent<CardModel>();
                cardModelToShow.SetAsCard(cardToShow, true, new OnDoubleClickDelegate(deckEditor.AddCard));
            }
            cardModelToShow.transform.SetParent(resultsPanel);
            AllCardModels [cardId] = cardModelToShow;
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
        if (advancedSearchFilterMenu.gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Return)) {
            Search();
            HideAdvancedFilterPanel();
        }
    }

    public Transform CardModelStaging {
        get {
            if (_cardModelStaging == null) {
                GameObject go = new GameObject("Card Model Staging");
                _cardModelStaging = go.transform;
            }
            return _cardModelStaging;
        }
    }

    public Dictionary<string, CardModel> AllCardModels {
        get {
            if (_allCardModels == null)
                _allCardModels = new Dictionary<string, CardModel>();
            return _allCardModels;
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
            if (_resultsPanelSize == 0)
                _resultsPanelSize = Mathf.FloorToInt(resultsPanel.rect.width / (cardPrefab.GetComponent<RectTransform>().rect.width + (resultsPanel.GetOrAddComponent<HorizontalLayoutGroup>().spacing / 2)));
            return _resultsPanelSize;
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
