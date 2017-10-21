using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public delegate void OnFilterChangeDelegate(string filters);
public delegate void OnSearchDelegate(List<Card> searchResults);

public class CardSearchMenu : MonoBehaviour
{
    public const float PropertyPanelHeight = 150f;
    public const float ToggleButtonWidth = 200f;

    public string Filters {
        get {
            string filters = string.Empty;
            if (!string.IsNullOrEmpty(IdFilter))
                filters += "id:" + IdFilter + "; ";
            if (!string.IsNullOrEmpty(SetCodeFilter))
                filters += "set:" + SetCodeFilter + "; ";
            
            foreach (PropertyDef property in CardGameManager.Current.CardProperties) {
                switch (property.Type) {
                    case PropertyType.String:
                        if (StringPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + ":" + StringPropertyFilters [property.Name] + "; ";
                        break;
                    case PropertyType.Integer:
                        if (IntMinPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + ">=" + IntMinPropertyFilters [property.Name].ToString() + "; ";
                        if (IntMaxPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + "<=" + IntMaxPropertyFilters [property.Name].ToString() + "; ";
                        break;
                    case PropertyType.Enum:
                        if (!EnumPropertyFilters.ContainsKey(property.Name))
                            break;
                        EnumDef enumDef = CardGameManager.Current.Enums.Where((def) => def.Property.Equals(property.Name)).First();
                        if (enumDef != null)
                            filters += property.Name + ":" + enumDef.GetStringFromIntFlags(EnumPropertyFilters [property.Name]) + "; ";
                        break;
                }
            }

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
    private Dictionary<string, int> _enumPropertyFilters;
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
                newPanel = CreateEnumPropertyFilterPanel(panelPosition, property.Name);
            
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
        EnumDef enumDef = CardGameManager.Current.Enums.Where((def) => def.Property.Equals(propertyName)).First();
        if (enumDef == null || enumDef.Values.Count < 1)
            return null;
        
        GameObject newPanel = Instantiate(enumPropertyPanel.gameObject, propertyFiltersContent) as GameObject;
        newPanel.gameObject.SetActive(true);
        newPanel.transform.localPosition = panelPosition;

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = propertyName;
        int storedFilter = 0;
        EnumPropertyFilters.TryGetValue(propertyName, out storedFilter);

        Vector3 localPosition = config.enumToggle.transform.localPosition;
        foreach (KeyValuePair<string, string> enumValue in enumDef.Values) {
            int intValue;
            if (!EnumDef.TryParseInt(enumValue.Key, out intValue))
                continue;
            Toggle newToggle = Instantiate(config.enumToggle.gameObject, config.enumContent).GetOrAddComponent<Toggle>();
            newToggle.isOn = (storedFilter & intValue) != 0;
            UnityAction<bool> enumChange = new UnityAction<bool>(isOn => SetEnumPropertyFilter(propertyName, intValue, isOn));
            newToggle.onValueChanged.AddListener(enumChange);
            newToggle.GetComponentInChildren<Text>().text = enumValue.Value;
            newToggle.transform.localPosition = localPosition;
            localPosition.x += ToggleButtonWidth;
        }

        config.enumToggle.gameObject.SetActive(false);
        config.enumContent.sizeDelta = new Vector2(ToggleButtonWidth * enumDef.Values.Count, config.enumContent.sizeDelta.y);

        return newPanel;
    }

    public void SetStringPropertyFilter(string propertyName, string filterValue)
    {
        if (string.IsNullOrEmpty(filterValue)) {
            if (StringPropertyFilters.ContainsKey(propertyName))
                StringPropertyFilters.Remove(propertyName);
            return;
        }

        StringPropertyFilters [propertyName] = filterValue;

        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void SetIntMinPropertyFilter(string propertyName, string filterValue)
    {
        int intValue;
        if (!int.TryParse(filterValue, out intValue)) {
            if (IntMinPropertyFilters.ContainsKey(propertyName))
                IntMinPropertyFilters.Remove(propertyName);
            return;
        }

        IntMinPropertyFilters [propertyName] = intValue;

        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void SetIntMaxPropertyFilter(string propertyName, string filterValue)
    {
        int intValue;
        if (!int.TryParse(filterValue, out intValue)) {
            if (IntMaxPropertyFilters.ContainsKey(propertyName))
                IntMaxPropertyFilters.Remove(propertyName);
            return;
        }

        IntMaxPropertyFilters [propertyName] = intValue;

        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void SetEnumPropertyFilter(string propertyName, int filterValue, bool isOn)
    {
        bool isStored = EnumPropertyFilters.ContainsKey(propertyName);
        int storedFilter = 0;
        if (isStored)
            storedFilter = EnumPropertyFilters [propertyName];
        
        int newFilter = isOn ? storedFilter | filterValue : storedFilter & ~filterValue;
        if (newFilter == 0) {
            if (isStored)
                EnumPropertyFilters.Remove(propertyName);
        } else
            EnumPropertyFilters [propertyName] = newFilter;
        
        if (FilterChangeCallback != null)
            FilterChangeCallback(Filters);
    }

    public void ClearFilters()
    {
        foreach (InputField input in GetComponentsInChildren<InputField>())
            input.text = string.Empty;
        foreach (Toggle toggle in GetComponentsInChildren<Toggle>())
            toggle.isOn = false;
        
        StringPropertyFilters.Clear();
        IntMinPropertyFilters.Clear();
        IntMaxPropertyFilters.Clear();
        EnumPropertyFilters.Clear();

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
        IEnumerable<Card> cardSearcher = CardGameManager.Current.FilterCards(IdFilter, NameFilter, SetCodeFilter, StringPropertyFilters, IntMinPropertyFilters, IntMaxPropertyFilters, EnumPropertyFilters);
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

    public Dictionary<string, int> EnumPropertyFilters {
        get {
            if (_enumPropertyFilters == null)
                _enumPropertyFilters = new Dictionary<string, int>();
            return _enumPropertyFilters;
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
