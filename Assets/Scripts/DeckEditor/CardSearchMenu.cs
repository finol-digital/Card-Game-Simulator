using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public delegate void OnFilterChangeDelegate(string filters);
public delegate void OnSearchDelegate(List<Card> searchResults);

public class CardSearchMenu : MonoBehaviour
{
    public const string SubmitString = "Submit";
    public const float PropertyPanelHeight = 150f;

    public string Filters {
        get {
            string filters = string.Empty;
            if (!string.IsNullOrEmpty(IdFilter))
                filters += "id:" + IdFilter + "; ";
            if (!string.IsNullOrEmpty(SetCodeFilter))
                filters += "set:" + SetCodeFilter + "; ";

            foreach (PropertyDef property in CardGameManager.Current.CardProperties) {
                switch (property.Type) {
                    case PropertyType.Integer:
                        if (IntMinPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + ">=" + IntMinPropertyFilters[property.Name] + "; ";
                        if (IntMaxPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + "<=" + IntMaxPropertyFilters[property.Name] + "; ";
                        break;
                    case PropertyType.Enum:
                    case PropertyType.EnumList:
                        if (!EnumPropertyFilters.ContainsKey(property.Name))
                            break;
                        EnumDef enumDef = CardGameManager.Current.Enums.FirstOrDefault(def => def.Property.Equals(property.Name));
                        if (enumDef != null)
                            filters += property.Name + ":=" + EnumPropertyFilters[property.Name] + "; ";
                        break;
                    case PropertyType.String:
                    default:
                        if (StringPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + ":" + StringPropertyFilters[property.Name] + "; ";
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

    public OnDeckNameChangeDelegate NameChangeCallback { get; set; }
    public OnFilterChangeDelegate FilterChangeCallback { get; set; }
    public OnSearchDelegate SearchCallback { get; set; }

    public List<GameObject> FilterPanels { get; } = new List<GameObject>();
    public Dictionary<string, string> StringPropertyFilters { get; } = new Dictionary<string, string>();
    public Dictionary<string, int> IntMinPropertyFilters { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> IntMaxPropertyFilters { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> EnumPropertyFilters { get; } = new Dictionary<string, int>();
    public List<Card> Results { get; } = new List<Card>();

    private string _nameFilter;
    private string _idFilter;
    private string _setCodeFilter;

    public void Show(OnDeckNameChangeDelegate nameChangeCallback, OnFilterChangeDelegate filterChangeCallback, OnSearchDelegate searchCallback)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
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
            GameObject newPanel;
            if (EnumDef.IsEnumProperty(property.Name))
                newPanel = CreateEnumPropertyFilterPanel(panelPosition, property.Name);
            else if (property.Type == PropertyType.Integer)
                newPanel = CreateIntegerPropertyFilterPanel(panelPosition, property.Name);
            else //if (property.Type == PropertyType.String)
                newPanel = CreateStringPropertyFilterPanel(panelPosition, property.Name);

            if (newPanel == null)
                continue;

            panelPosition.y -= PropertyPanelHeight;
            FilterPanels.Add(newPanel);
        }

        propertyFiltersContent.sizeDelta = new Vector2(propertyFiltersContent.sizeDelta.x, PropertyPanelHeight * CardGameManager.Current.CardProperties.Count + (PropertyPanelHeight * 3));
    }

    public GameObject CreateStringPropertyFilterPanel(Vector3 panelPosition, string propertyName)
    {
        GameObject newPanel = Instantiate(stringPropertyPanel.gameObject, propertyFiltersContent);
        newPanel.gameObject.SetActive(true);
        newPanel.transform.localPosition = panelPosition;

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = propertyName;
        string storedFilter;
        if (StringPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.stringInputField.text = storedFilter;
        config.stringPlaceHolderText.text = "Enter " + propertyName + "...";
        UnityAction<string> textChange = text => SetStringPropertyFilter(propertyName, text);
        config.stringInputField.onValueChanged.AddListener(textChange);

        return newPanel;
    }

    public GameObject CreateIntegerPropertyFilterPanel(Vector3 panelPosition, string propertyName)
    {
        GameObject newPanel = Instantiate(integerPropertyPanel.gameObject, propertyFiltersContent);
        newPanel.gameObject.SetActive(true);
        newPanel.transform.localPosition = panelPosition;

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = propertyName;
        int storedFilter;

        if (IntMinPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.integerMinInputField.text = storedFilter.ToString();
        UnityAction<string> minChange = text => SetIntMinPropertyFilter(propertyName, text);
        config.integerMinInputField.onValueChanged.AddListener(minChange);

        if (IntMaxPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.integerMaxInputField.text = storedFilter.ToString();
        UnityAction<string> maxChange = text => SetIntMaxPropertyFilter(propertyName, text);
        config.integerMaxInputField.onValueChanged.AddListener(maxChange);

        return newPanel;
    }

    public GameObject CreateEnumPropertyFilterPanel(Vector3 panelPosition, string propertyName)
    {
        EnumDef enumDef = CardGameManager.Current.Enums.First(def => def.Property.Equals(propertyName));
        if (enumDef == null || enumDef.Values.Count < 1)
            return null;

        GameObject newPanel = Instantiate(enumPropertyPanel.gameObject, propertyFiltersContent);
        newPanel.gameObject.SetActive(true);
        newPanel.transform.localPosition = panelPosition;

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = propertyName;
        int storedFilter;
        EnumPropertyFilters.TryGetValue(propertyName, out storedFilter);

        Vector3 localPosition = config.enumToggle.transform.localPosition;
        float panelWidth = 0;
        foreach (KeyValuePair<string, string> enumValue in enumDef.Values) {
            int lookupKey;
            if (!enumDef.ReverseLookup.TryGetValue(enumValue.Key, out lookupKey))
                lookupKey = enumDef.CreateLookup(enumValue.Key);
            Toggle newToggle = Instantiate(config.enumToggle.gameObject, config.enumContent).GetOrAddComponent<Toggle>();
            newToggle.isOn = (storedFilter & lookupKey) != 0;
            UnityAction<bool> enumChange = isOn => SetEnumPropertyFilter(propertyName, lookupKey, isOn);
            newToggle.onValueChanged.AddListener(enumChange);
            newToggle.GetComponentInChildren<Text>().text = enumValue.Value;
            newToggle.transform.localPosition = localPosition;
            float width = newToggle.GetComponentInChildren<Text>().preferredWidth + 25;
            RectTransform imageTransform = (RectTransform)newToggle.GetComponentInChildren<Image>().transform;
            imageTransform.sizeDelta = new Vector2(width, imageTransform.sizeDelta.y);
            localPosition.x += width;
            panelWidth += width;
        }

        config.enumToggle.gameObject.SetActive(false);
        config.enumContent.sizeDelta = new Vector2(panelWidth, config.enumContent.sizeDelta.y);
        newPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Clamp(panelWidth + 250, newPanel.GetComponent<RectTransform>().sizeDelta.x, propertyFiltersContent.rect.width), newPanel.GetComponent<RectTransform>().sizeDelta.y);

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

        FilterChangeCallback?.Invoke(Filters);
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

        FilterChangeCallback?.Invoke(Filters);
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

        FilterChangeCallback?.Invoke(Filters);
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

        FilterChangeCallback?.Invoke(Filters);
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

        FilterChangeCallback?.Invoke(Filters);
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
        SearchCallback?.Invoke(Results);
    }

    void Update()
    {
        if (!Input.GetButtonDown(SubmitString))
            return;

        Search();
        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public string NameFilter {
        get { return _nameFilter ?? (_nameFilter = string.Empty); }
        set {
            _nameFilter = value;
            NameChangeCallback?.Invoke(_nameFilter);
            if (nameInputField != null)
                nameInputField.text = _nameFilter;
        }
    }

    public string IdFilter {
        get { return _idFilter ?? (_idFilter = string.Empty); }
        set {
            _idFilter = value;
            FilterChangeCallback?.Invoke(Filters);
        }
    }

    public string SetCodeFilter {
        get { return _setCodeFilter ?? (_setCodeFilter = string.Empty); }
        set {
            _setCodeFilter = value;
            FilterChangeCallback?.Invoke(Filters);
        }
    }
}
