using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public delegate void OnFilterChangeDelegate(string filters);
public delegate void OnSearchDelegate(List<Card> searchResults);

public class SearchMenu : MonoBehaviour
{
    public const float PropertyPanelHeight = 150f;

    public string Filters {
        get {
            string filters = string.Empty;
            if (!string.IsNullOrEmpty(IdFilter))
                filters += "id:" + IdFilter + "; ";
            if (!string.IsNullOrEmpty(SetCodeFilter))
                filters += "set:" + SetCodeFilter + "; ";
            // TODO: BETTER ORDERING
            foreach (var filter in StringPropertyFilters)
                filters += filter.Key + ":" + filter.Value + "; ";
            foreach (var filter in IntMinPropertyFilters)
                filters += filter.Key + ">=" + filter.Value + "; ";
            foreach (var filter in IntMaxPropertyFilters)
                filters += filter.Key + "<=" + filter.Value + "; ";
            return filters;
        }
    }

    public InputField nameInputField;
    public RectTransform propertyFiltersContent;
    public SearchPropertyPanel stringPropertyPanel;
    public SearchPropertyPanel integerPropertyPanel;
    public SearchPropertyPanel enumPropertyPanel;

    public NameChangeDelegate NameChangeCallback { get; set; }

    public OnFilterChangeDelegate FilterChangeCallback { get; set; }

    public OnSearchDelegate SearchCallback { get; set; }

    private List<GameObject> _filterPanels;
    private string _nameFilter;
    private string _idFilter;
    private string _setCodeFilter;
    private Dictionary<string, string> _stringPropertyFilters;
    private Dictionary<string, int> _intMinPropertyFilters;
    private Dictionary<string, int> _intMaxPropertyFilters;
    private List<Card> _results;

    public void Show(NameChangeDelegate nameChangeCallback, OnFilterChangeDelegate filterChangeCallback, OnSearchDelegate searchCallback)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        NameChangeCallback = nameChangeCallback;
        FilterChangeCallback = filterChangeCallback;
        SearchCallback = searchCallback;

        stringPropertyPanel.gameObject.SetActive(false);
        integerPropertyPanel.gameObject.SetActive(false);
        enumPropertyPanel.gameObject.SetActive(false);

        for (int i = FilterPanels.Count - 1; i >= 0; i--) {
            Destroy(FilterPanels [i].gameObject);
            FilterPanels.RemoveAt(i);
        }

        Vector2 panelPosition = stringPropertyPanel.transform.localPosition;
        foreach (PropertyDef property in CardGameManager.Current.CardProperties) {
            GameObject newPanel = null;
            if (property.Type == PropertyType.String)
                newPanel = CreateStringPropertyFilterPanel(panelPosition, property.Name);
            else if (property.Type == PropertyType.Integer)
                newPanel = CreateIntegerPropertyFilterPanel(panelPosition, property.Name);
            else if (property.Type == PropertyType.Enum)
                newPanel = CreateIntegerPropertyFilterPanel(panelPosition, property.Name);
            
            if (newPanel != null) {
                panelPosition.y -= PropertyPanelHeight;
                FilterPanels.Add(newPanel);
            }
        }

        propertyFiltersContent.sizeDelta = new Vector2(propertyFiltersContent.sizeDelta.x, PropertyPanelHeight * CardGameManager.Current.CardProperties.Count + (PropertyPanelHeight * 3));
    }

    public GameObject CreateStringPropertyFilterPanel(Vector3 panelPosition, string propertyName)
    {
        GameObject newPanel = Instantiate(stringPropertyPanel.gameObject, propertyFiltersContent) as GameObject;
        newPanel.gameObject.SetActive(true);
        newPanel.transform.localPosition = panelPosition;

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = propertyName;
        string storedFilter = string.Empty;
        if (StringPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.stringInputField.text = storedFilter;
        config.stringPlaceHolderText.text = "Enter " + propertyName + "...";
        UnityAction<string> textChange = new UnityAction<string>(text => SetStringPropertyFilter(propertyName, text));
        config.stringInputField.onValueChanged.AddListener(textChange);

        return newPanel;
    }

    public GameObject CreateIntegerPropertyFilterPanel(Vector3 panelPosition, string propertyName)
    {
        GameObject newPanel = Instantiate(integerPropertyPanel.gameObject, propertyFiltersContent) as GameObject;
        newPanel.gameObject.SetActive(true);
        newPanel.transform.localPosition = panelPosition;

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = propertyName;
        int storedFilter = 0;

        if (IntMinPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.integerMinInputField.text = storedFilter.ToString();
        UnityAction<string> minChange = new UnityAction<string>(text => SetIntMinPropertyFilter(propertyName, text));
        config.integerMinInputField.onValueChanged.AddListener(minChange);

        if (IntMaxPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.integerMaxInputField.text = storedFilter.ToString();
        UnityAction<string> maxChange = new UnityAction<string>(text => SetIntMaxPropertyFilter(propertyName, text));
        config.integerMaxInputField.onValueChanged.AddListener(maxChange);

        return newPanel;
    }

    public GameObject CreateEnumPropertyFilterPanel(Vector3 panelPosition, string propertyName)
    {
        return null;
    }

    public void SetStringPropertyFilter(string key, string value)
    {
        if (string.IsNullOrEmpty(value)) {
            if (StringPropertyFilters.ContainsKey(key))
                StringPropertyFilters.Remove(key);
            return;
        }

        StringPropertyFilters [key] = value;
        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void SetIntMinPropertyFilter(string key, string value)
    {
        int intValue;
        if (!int.TryParse(value, out intValue)) {
            if (IntMinPropertyFilters.ContainsKey(key))
                IntMinPropertyFilters.Remove(key);
            return;
        }

        IntMinPropertyFilters [key] = intValue;
        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void SetIntMaxPropertyFilter(string key, string value)
    {
        int intValue;
        if (!int.TryParse(value, out intValue)) {
            if (IntMaxPropertyFilters.ContainsKey(key))
                IntMaxPropertyFilters.Remove(key);
            return;
        }

        IntMaxPropertyFilters [key] = intValue;
        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void ClearFilters()
    {
        foreach (InputField input in GetComponentsInChildren<InputField>())
            input.text = string.Empty;
        StringPropertyFilters.Clear();

        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void ClearSearch()
    {
        ClearFilters();
        Search();
    }

    public void Search()
    {
        Results.Clear();
        IEnumerable<Card> cardSearcher = CardGameManager.Current.FilterCards(IdFilter, NameFilter, SetCodeFilter, StringPropertyFilters, IntMinPropertyFilters, IntMaxPropertyFilters);
        foreach (Card card in cardSearcher)
            Results.Add(card);
        if (SearchCallback != null)
            SearchCallback(Results);
    }

    void Update()
    {
        if (Input.GetButtonDown("Submit")) {
            Search();
            Hide();
        }
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    public List<GameObject> FilterPanels {
        get {
            if (_filterPanels == null)
                _filterPanels = new List<GameObject>();
            return _filterPanels;
        }
    }

    public string NameFilter {
        get {
            if (_nameFilter == null)
                _nameFilter = string.Empty;
            return _nameFilter;
        }
        set {
            _nameFilter = value;
            if (NameChangeCallback != null)
                NameChangeCallback(_nameFilter);
            if (nameInputField != null)
                nameInputField.text = _nameFilter;
        }
    }

    public string IdFilter {
        get {
            if (_idFilter == null)
                _idFilter = string.Empty;
            return _idFilter;
        }
        set {
            _idFilter = value;
            if (FilterChangeCallback != null)
                FilterChangeCallback(Filters);
        }
    }

    public string SetCodeFilter {
        get {
            if (_setCodeFilter == null)
                _setCodeFilter = string.Empty;
            return _setCodeFilter;
        }
        set {
            _setCodeFilter = value;
            if (FilterChangeCallback != null)
                FilterChangeCallback(Filters);
        }
    }

    public Dictionary<string, string> StringPropertyFilters {
        get {
            if (_stringPropertyFilters == null)
                _stringPropertyFilters = new Dictionary<string, string>();
            return _stringPropertyFilters;
        }
    }

    public Dictionary<string, int> IntMinPropertyFilters {
        get {
            if (_intMinPropertyFilters == null)
                _intMinPropertyFilters = new Dictionary<string, int>();
            return _intMinPropertyFilters;
        }
    }

    public Dictionary<string, int> IntMaxPropertyFilters {
        get {
            if (_intMaxPropertyFilters == null)
                _intMaxPropertyFilters = new Dictionary<string, int>();
            return _intMaxPropertyFilters;
        }
    }

    public List<Card> Results {
        get {
            if (_results == null)
                _results = new List<Card>();
            return _results;
        }
    }
}
