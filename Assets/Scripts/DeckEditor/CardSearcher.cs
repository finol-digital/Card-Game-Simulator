using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CardSearcher : MonoBehaviour
{
    public int ResultsPanelSize {
        get {
            return Mathf.FloorToInt(resultsPanel.rect.width / (cardPrefab.GetComponent<RectTransform>().rect.width + (resultsPanel.gameObject.GetOrAddComponent<HorizontalLayoutGroup>().spacing)));
        }
    }

    public int ResultRowCount {
        get {
            return ResultsPanelSize == 0 ? 0 : (SearchResults.Count / ResultsPanelSize) + ((SearchResults.Count % ResultsPanelSize) == 0 ? -1 : 0);
        }
    }

    public string nameFilter { get; set; }

    public string idFilter { get; set; }

    public string setCodeFilter { get; set; }

    public int ResultsIndex { get; set; }

    public GameObject cardPrefab;
    public DeckEditor deckEditor;
    public RectTransform searchAdvancedMenu;
    public RectTransform filterContentView;
    public RectTransform nameProperty;
    public RectTransform idProperty;
    public RectTransform setProperty;
    public RectTransform propertyTemplate;
    public RectTransform resultsPanel;
    public Text resultsCountText;

    private List<Card> _searchResults;
    private Dictionary<string, string> _propertyFilters;

    void OnEnable()
    {
        CardGameManager.Instance.OnSelectActions.Add(ResetCardSearcher);
    }

    void Start()
    {
        nameFilter = string.Empty;
        idFilter = string.Empty;
        setCodeFilter = string.Empty;
    }

    public void ResetCardSearcher()
    {
        propertyTemplate.gameObject.SetActive(true);
        nameProperty.SetParent(searchAdvancedMenu);
        idProperty.SetParent(searchAdvancedMenu);
        setProperty.SetParent(searchAdvancedMenu);
        propertyTemplate.SetParent(searchAdvancedMenu);
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

        ClearFilters();
        Search();
    }

    public void SetPropertyFilter(string key, string val)
    {
        PropertyFilters [key] = val;
    }

    public void ClearFilters()
    {
        foreach (InputField input in searchAdvancedMenu.GetComponentsInChildren<InputField>())
            input.text = string.Empty;
        PropertyFilters.Clear();
    }

    public void Search()
    {
        SearchResults.Clear();
        ResultsIndex = 0;
        IEnumerable<Card> cardSearcher = CardGameManager.Current.FilterCards(idFilter, nameFilter, setCodeFilter, PropertyFilters);
        foreach (Card card in cardSearcher)
            SearchResults.Add(card);
        UpdateSearchResultsPanel();
    }

    public void MoveSearchResultsLeft()
    {
        ResultsIndex--;
        if (ResultsIndex < 0)
            ResultsIndex = ResultRowCount;
        UpdateSearchResultsPanel();
    }

    public void MoveSearchResultsRight()
    {
        ResultsIndex++;
        if (ResultsIndex > ResultRowCount)
            ResultsIndex = 0;
        UpdateSearchResultsPanel();
    }

    public void UpdateSearchResultsPanel()
    {
        resultsPanel.DestroyAllChildren();

        for (int i = 0; i < ResultsPanelSize && ResultsIndex >= 0 && ResultsIndex * ResultsPanelSize + i < SearchResults.Count; i++) {
            string cardId = SearchResults [ResultsIndex * ResultsPanelSize + i].Id;
            Card cardToShow = CardGameManager.Current.Cards.Where(card => card.Id == cardId).FirstOrDefault();
            CardModel cardModelToShow = Instantiate(cardPrefab, resultsPanel).GetOrAddComponent<CardModel>();
            cardModelToShow.RepresentedCard = cardToShow;
            cardModelToShow.ClonesOnDrag = true;
            cardModelToShow.DoubleClickEvent = new OnDoubleClickDelegate(deckEditor.AddCard);
        }

        resultsCountText.text = (ResultsIndex + 1) + " / " + (ResultRowCount + 1);
    }

    public void ShowAdvancedFilterPanel()
    {
        searchAdvancedMenu.gameObject.SetActive(true);
        searchAdvancedMenu.SetAsLastSibling();
    }

    public void HideAdvancedFilterPanel()
    {
        searchAdvancedMenu.gameObject.SetActive(false);
    }

    void Update()
    {
        if (searchAdvancedMenu.gameObject.activeSelf && Input.GetButtonDown("Submit")) {
            Search();
            HideAdvancedFilterPanel();
        }
    }

    void OnDisable()
    {
        CardGameManager.Instance.OnSelectActions.Remove(ResetCardSearcher);
    }

    public List<Card> SearchResults {
        get {
            if (_searchResults == null)
                _searchResults = new List<Card>();
            return _searchResults;
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
