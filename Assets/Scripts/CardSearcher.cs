using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CardSearcher : MonoBehaviour
{
    public GameObject cardPrefab;
    public DeckEditor deckEditor;
    public RectTransform advancedFilterPanel;
    public RectTransform filterContentView;
    public RectTransform propertyTemplate;
    public RectTransform resultsPanel;
    public Text resultsCountText;

    private Transform cardModelStaging;
    private Dictionary<string, CardModel> allCardModels;
    private List<Card> searchResults;
    private int resultsPanelSize;
    private int resultsIndex;
    private string idFilter;
    private string nameFilter;
    private string setCodeFilter;
    private Dictionary<string, string> propFilters;

    void Awake()
    {
        Debug.Log("Card Searcher initializing");
        GameObject go = new GameObject("Card Model Staging");
        cardModelStaging = go.transform;
        allCardModels = new Dictionary<string, CardModel>();
        searchResults = new List<Card>();
        resultsPanelSize = Mathf.FloorToInt(resultsPanel.rect.width / (cardPrefab.GetComponent<RectTransform>().rect.width + 25));
        resultsIndex = 0;
        idFilter = "";
        nameFilter = "";
        setCodeFilter = "";
        propFilters = new Dictionary<string, string>();
    }

    IEnumerator Start()
    {

        Debug.Log("Card Searcher waiting for the card game to finish loading");
        while (!CardGameManager.IsLoaded)
            yield return null;

        Debug.Log("Building the Advanced filter panel");
        Vector2 pos = propertyTemplate.localPosition;
        foreach (PropertyDef prop in CardGameManager.CurrentCardGame.CardProperties) {
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
        filterContentView.sizeDelta = new Vector2(filterContentView.sizeDelta.x, propertyTemplate.rect.height * CardGameManager.CurrentCardGame.CardProperties.Count + propertyTemplate.rect.height * 3);

        Debug.Log("Showing all cards in the search results");
        ClearFilters();
        Search();

        Debug.Log("Card Searcher ready");

    }

    public void SetIdFilter(string val)
    {
        this.idFilter = val;
    }

    public void SetNameFilter(string val)
    {
        this.nameFilter = val;
    }

    public void SetSetCodeFilter(string val)
    {
        this.setCodeFilter = val;
    }

    public void SetPropertyFilter(string key, string val)
    {
        this.propFilters [key] = val;
    }

    public void ClearFilters()
    {
        foreach (InputField input in advancedFilterPanel.GetComponentsInChildren<InputField>())
            input.text = "";
        this.idFilter = "";
        this.nameFilter = "";
        this.setCodeFilter = "";
        propFilters.Clear();
    }

    public void Search()
    {
        Debug.Log("Searching with id " + idFilter + ", name " + nameFilter + ", setCode " + setCodeFilter);
        string debugFilters = " ";
        foreach (KeyValuePair<string, string> entry in propFilters)
            debugFilters += entry.Key + ": " + entry.Value + "; ";
        Debug.Log(debugFilters);

        searchResults.Clear();
        IEnumerable<Card> cardSearcher = CardGameManager.CurrentCardGame.FilterCards(idFilter, nameFilter, setCodeFilter, propFilters);
        foreach (Card card in cardSearcher)
            searchResults.Add(card);
        ApplySearchResults(searchResults);

    }

    public void ApplySearchResults(List<Card> cards)
    {
        searchResults = cards;
        resultsIndex = 0;
        UpdateSearchResultsPanel();

    }

    public void MoveSearchResultsLeft()
    {
        resultsIndex--;
        if (resultsIndex < 0)
            resultsIndex = ResultRowCount;
        UpdateSearchResultsPanel();

    }

    public void MoveSearchResultsRight()
    {
        resultsIndex++;
        if (resultsIndex > ResultRowCount)
            resultsIndex = 0;
        UpdateSearchResultsPanel();

    }

    public void UpdateSearchResultsPanel()
    {
        for (int i = resultsPanel.childCount - 1; i >= 0; i--) {
            resultsPanel.GetChild(i).SetParent(cardModelStaging);
        }

        for (int i = 0; i < resultsPanelSize && resultsIndex >= 0 && resultsIndex * resultsPanelSize + i < searchResults.Count; i++) {
            string cardId = searchResults [resultsIndex * resultsPanelSize + i].Id;

            CardModel cardModelToShow;
            if (!allCardModels.TryGetValue(cardId, out cardModelToShow)) {
                Debug.Log("Creating Card Model for " + cardId);
                Card cardToShow = CardGameManager.Cards.Where(card => card.Id == cardId).LastOrDefault();
                cardModelToShow = CreateCardModel(cardToShow);
            }
            cardModelToShow.transform.SetParent(resultsPanel);
            allCardModels [cardId] = cardModelToShow;
        }

        resultsCountText.text = (resultsIndex + 1) + " / " + (ResultRowCount + 1);
    }

    private CardModel CreateCardModel(Card cardToShow)
    {
        GameObject newCard = Instantiate(cardPrefab, resultsPanel);
        CardModel cardModel = newCard.transform.GetOrAddComponent<CardModel>();
        cardModel.SetAsCard(cardToShow, true, new OnDoubleClickDelegate(deckEditor.AddCard));
        return cardModel;
    }

    public void ShowAdvancedFilterPanel()
    {
        advancedFilterPanel.gameObject.SetActive(true);
    }

    public void HideAdvancedFilterPanel()
    {
        advancedFilterPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (advancedFilterPanel.gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Return)) {
            Search();
            HideAdvancedFilterPanel();
        }
    }

    public int ResultRowCount {
        get {
            return (searchResults.Count / resultsPanelSize) + (searchResults.Count % resultsPanelSize == 0 ? -1 : 0);
        }
    }
}
