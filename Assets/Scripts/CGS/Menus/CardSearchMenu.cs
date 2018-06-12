using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public delegate void OnSearchDelegate(string filters, List<Card> searchResults);

public class CardSearchMenu : MonoBehaviour
{
    public const string ClearFiltersPrompt = "Clear Filters?";
    public float PropertyPanelHeight => ((RectTransform)stringPropertyPanel.transform).rect.height;

    public Scrollbar scrollbar;
    public InputField nameInputField;
    public InputField idInputField;
    public InputField setCodeInputField;
    public RectTransform propertyFiltersContent;
    public SearchPropertyPanel stringPropertyPanel;
    public SearchPropertyPanel integerPropertyPanel;
    public SearchPropertyPanel enumPropertyPanel;

    public OnDeckNameChangeDelegate NameChangeCallback { get; set; }
    public OnSearchDelegate SearchCallback { get; set; }

    public List<GameObject> FilterPanels { get; } = new List<GameObject>();
    public List<InputField> InputFields { get; } = new List<InputField>();
    public List<Toggle> Toggles { get; } = new List<Toggle>();

    public Dictionary<string, string> StringPropertyFilters { get; } = new Dictionary<string, string>();
    public Dictionary<string, int> IntMinPropertyFilters { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> IntMaxPropertyFilters { get; } = new Dictionary<string, int>();
    public Dictionary<string, int> EnumPropertyFilters { get; } = new Dictionary<string, int>();
    public List<Card> Results { get; } = new List<Card>();

    private string _nameFilter;
    private string _idFilter;
    private string _setCodeFilter;

    void LateUpdate()
    {
        if (!Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
            return;

        if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
        {
            Search();
            Hide();
        }
        else if (Input.GetButtonDown(Inputs.Page))
            scrollbar.value = Mathf.Clamp01(scrollbar.value + (Input.GetAxis(Inputs.Page) < 0 ? 0.1f : -0.1f));
        else if (Input.GetButtonDown(Inputs.FocusName) || Input.GetButtonDown(Inputs.FocusText))
            FocusInputField();
        else if (Input.GetButtonDown(Inputs.Vertical) || Input.GetButtonDown(Inputs.Horizontal))
            FocusToggle();
        else if (Input.GetButtonDown(Inputs.New) && ActiveToggle != null)
            ToggleEnum();
        else if (Input.GetButtonDown(Inputs.Delete) && ActiveInputField == null)
            ClearFilters();
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
            Hide();
    }

    public void FocusInputField()
    {
        if (ActiveInputField == null || InputFields.Count < 1)
        {
            InputFields.FirstOrDefault()?.ActivateInputField();
            ActiveInputField = InputFields.FirstOrDefault();
            return;
        }

        if (Input.GetButtonDown(Inputs.FocusName))
        { // up
            InputField previous = InputFields.Last();
            for (int i = 0; i < InputFields.Count; i++)
            {
                if (ActiveInputField == InputFields[i])
                {
                    previous.ActivateInputField();
                    ActiveInputField = previous;
                    break;
                }
                previous = InputFields[i];
            }
        }
        else
        { // down
            InputField next = InputFields.First();
            for (int i = InputFields.Count - 1; i >= 0; i--)
            {
                if (ActiveInputField == InputFields[i])
                {
                    next.ActivateInputField();
                    ActiveInputField = next;
                    break;
                }
                next = InputFields[i];
            }
        }
    }

    public void FocusToggle()
    {
        if (ActiveToggle == null || Toggles.Count < 1)
        {
            ActiveToggle = Toggles.FirstOrDefault();
            return;
        }

        if (Input.GetButtonDown(Inputs.Vertical))
        {
            Transform currentPanel = ActiveToggle.transform.parent;
            if (Input.GetAxis(Inputs.Vertical) > 0)
            { // up
                Toggle previous = Toggles.Last();
                for (int i = 0; i < Toggles.Count; i++)
                {
                    if (ActiveToggle == Toggles[i])
                    {
                        ActiveToggle = previous;
                        break;
                    }
                    if (Toggles[i].transform.parent != ActiveToggle.transform.parent)
                        previous = Toggles[i];
                }
            }
            else
            { // down
                Toggle next = Toggles.First();
                for (int i = Toggles.Count - 1; i >= 0; i--)
                {
                    if (ActiveToggle == Toggles[i])
                    {
                        ActiveToggle = next;
                        break;
                    }
                    if (Toggles[i].transform.parent != ActiveToggle.transform.parent)
                        next = Toggles[i];
                }
            }
        }
        else if (Input.GetButton(Inputs.Horizontal))
        {
            if (Input.GetAxis(Inputs.Horizontal) > 0)
            { // right
                Toggle next = Toggles.First();
                for (int i = Toggles.Count - 1; i >= 0; i--)
                {
                    if (ActiveToggle == Toggles[i])
                    {
                        ActiveToggle = next;
                        break;
                    }
                    next = Toggles[i];
                }
            }
            else
            { // left
                Toggle previous = Toggles.Last();
                for (int i = 0; i < Toggles.Count; i++)
                {
                    if (ActiveToggle == Toggles[i])
                    {
                        ActiveToggle = previous;
                        break;
                    }
                    previous = Toggles[i];
                }
            }
        }
    }

    public void ToggleEnum()
    {
        if (ActiveToggle == null)
            return;
        ActiveToggle.isOn = !ActiveToggle.isOn;
    }

    public void Show(OnDeckNameChangeDelegate nameChangeCallback, OnSearchDelegate searchCallback)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        NameChangeCallback = nameChangeCallback;
        SearchCallback = searchCallback;

        stringPropertyPanel.gameObject.SetActive(false);
        integerPropertyPanel.gameObject.SetActive(false);
        enumPropertyPanel.gameObject.SetActive(false);
        for (int i = FilterPanels.Count - 1; i >= 0; i--)
        {
            Destroy(FilterPanels[i].gameObject);
            FilterPanels.RemoveAt(i);
        }
        InputFields.Clear();
        Toggles.Clear();

        nameInputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
        InputFields.Add(nameInputField);
        idInputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
        InputFields.Add(idInputField);
        setCodeInputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
        InputFields.Add(setCodeInputField);

        propertyFiltersContent.sizeDelta = new Vector2(propertyFiltersContent.sizeDelta.x, PropertyPanelHeight * CardGameManager.Current.CardProperties.Count + (PropertyPanelHeight * 3));
        foreach (PropertyDef property in CardGameManager.Current.CardProperties)
        {
            GameObject newPanel;
            if (EnumDef.IsEnumProperty(property.Name))
                newPanel = CreateEnumPropertyFilterPanel(property);
            else if (property.Type == PropertyType.Integer)
                newPanel = CreateIntegerPropertyFilterPanel(property.Name, property.Display);
            else //if (property.Type == PropertyType.String)
                newPanel = CreateStringPropertyFilterPanel(property.Name, property.Display);
            FilterPanels.Add(newPanel);
            foreach (InputField inputField in newPanel.GetComponentsInChildren<InputField>())
            {
                inputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
                InputFields.Add(inputField);
            }
            foreach (Toggle toggle in newPanel.GetComponentsInChildren<Toggle>())
                Toggles.Add(toggle);
        }
    }

    public GameObject CreateStringPropertyFilterPanel(string propertyName, string displayName)
    {
        GameObject newPanel = Instantiate(stringPropertyPanel.gameObject, propertyFiltersContent);
        newPanel.gameObject.SetActive(true);

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = !string.IsNullOrEmpty(displayName) ? displayName : propertyName;
        string storedFilter;
        if (StringPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.stringInputField.text = storedFilter;
        config.stringPlaceHolderText.text = "Enter " + propertyName + "...";
        config.stringInputField.onValueChanged.AddListener(text => SetStringPropertyFilter(propertyName, text));

        return newPanel;
    }

    public GameObject CreateIntegerPropertyFilterPanel(string propertyName, string displayName)
    {
        GameObject newPanel = Instantiate(integerPropertyPanel.gameObject, propertyFiltersContent);
        newPanel.gameObject.SetActive(true);

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = !string.IsNullOrEmpty(displayName) ? displayName : propertyName;
        int storedFilter;

        if (IntMinPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.integerMinInputField.text = storedFilter.ToString();
        config.integerMinInputField.onValueChanged.AddListener(text => SetIntMinPropertyFilter(propertyName, text));

        if (IntMaxPropertyFilters.TryGetValue(propertyName, out storedFilter))
            config.integerMaxInputField.text = storedFilter.ToString();
        config.integerMaxInputField.onValueChanged.AddListener(text => SetIntMaxPropertyFilter(propertyName, text));

        return newPanel;
    }

    public GameObject CreateEnumPropertyFilterPanel(PropertyDef property)
    {
        GameObject newPanel = Instantiate(enumPropertyPanel.gameObject, propertyFiltersContent);
        newPanel.gameObject.SetActive(true);

        SearchPropertyPanel config = newPanel.GetComponent<SearchPropertyPanel>();
        config.nameLabelText.text = !string.IsNullOrEmpty(property.Display) ? property.Display : property.Name;
        int storedFilter;
        EnumPropertyFilters.TryGetValue(property.Name, out storedFilter);

        EnumDef enumDef = CardGameManager.Current.Enums.First(def => def.Property.Equals(property.Name));
        Vector3 localPosition = config.enumToggle.transform.localPosition;
        float panelWidth = 0;
        foreach (KeyValuePair<string, string> enumValue in enumDef.Values)
        {
            int lookupKey;
            if (!enumDef.Lookup.TryGetValue(enumValue.Key, out lookupKey))
                lookupKey = enumDef.CreateLookup(enumValue.Key);
            Toggle newToggle = Instantiate(config.enumToggle.gameObject, config.enumContent).GetOrAddComponent<Toggle>();
            newToggle.isOn = (storedFilter & lookupKey) != 0;
            newToggle.onValueChanged.AddListener(isOn => SetEnumPropertyFilter(property.Name, lookupKey, isOn));
            newToggle.GetComponentInChildren<Text>().text = enumValue.Value;
            newToggle.transform.localPosition = localPosition;
            float width = newToggle.GetComponentInChildren<Text>().preferredWidth + 25;
            RectTransform imageTransform = (RectTransform)newToggle.GetComponentInChildren<Image>().transform;
            imageTransform.sizeDelta = new Vector2(width, imageTransform.sizeDelta.y);
            localPosition.x += width;
            panelWidth += width;
        }

        if (!string.IsNullOrEmpty(property.Empty))
        {
            int lookupKey;
            if (!enumDef.Lookup.TryGetValue(property.Empty, out lookupKey))
                lookupKey = enumDef.CreateLookup(property.Empty);
            Toggle newToggle = Instantiate(config.enumToggle.gameObject, config.enumContent).GetOrAddComponent<Toggle>();
            newToggle.isOn = (storedFilter & lookupKey) != 0;
            newToggle.onValueChanged.AddListener(isOn => SetEnumPropertyFilter(property.Name, lookupKey, isOn));
            newToggle.GetComponentInChildren<Text>().text = property.Empty;
            newToggle.transform.localPosition = localPosition;
            float width = newToggle.GetComponentInChildren<Text>().preferredWidth + 25;
            RectTransform imageTransform = (RectTransform)newToggle.GetComponentInChildren<Image>().transform;
            imageTransform.sizeDelta = new Vector2(width, imageTransform.sizeDelta.y);
            localPosition.x += width;
            panelWidth += width;
        }
        config.enumToggle.gameObject.SetActive(false);
        config.enumContent.sizeDelta = new Vector2(panelWidth, config.enumContent.sizeDelta.y);

        return newPanel;
    }

    public void SetStringPropertyFilter(string propertyName, string filterValue)
    {
        if (string.IsNullOrEmpty(filterValue))
        {
            if (StringPropertyFilters.ContainsKey(propertyName))
                StringPropertyFilters.Remove(propertyName);
            return;
        }

        StringPropertyFilters[propertyName] = filterValue;
    }

    public void SetIntMinPropertyFilter(string propertyName, string filterValue)
    {
        int intValue;
        if (!int.TryParse(filterValue, out intValue))
        {
            if (IntMinPropertyFilters.ContainsKey(propertyName))
                IntMinPropertyFilters.Remove(propertyName);
            return;
        }

        IntMinPropertyFilters[propertyName] = intValue;
    }

    public void SetIntMaxPropertyFilter(string propertyName, string filterValue)
    {
        int intValue;
        if (!int.TryParse(filterValue, out intValue))
        {
            if (IntMaxPropertyFilters.ContainsKey(propertyName))
                IntMaxPropertyFilters.Remove(propertyName);
            return;
        }

        IntMaxPropertyFilters[propertyName] = intValue;
    }

    public void SetEnumPropertyFilter(string propertyName, int filterValue, bool isOn)
    {
        if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
            return;

        bool isStored = EnumPropertyFilters.ContainsKey(propertyName);
        int storedFilter = 0;
        if (isStored)
            storedFilter = EnumPropertyFilters[propertyName];

        int newFilter = isOn ? storedFilter | filterValue : storedFilter & ~filterValue;
        if (newFilter == 0)
        {
            if (isStored)
                EnumPropertyFilters.Remove(propertyName);
        }
        else
            EnumPropertyFilters[propertyName] = newFilter;
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
        SearchCallback?.Invoke(Filters, Results);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public string Filters
    {
        get
        {
            string filters = string.Empty;
            if (!string.IsNullOrEmpty(IdFilter))
                filters += "id:" + IdFilter + "; ";
            if (!string.IsNullOrEmpty(SetCodeFilter))
                filters += "set:" + SetCodeFilter + "; ";
            foreach (PropertyDef property in CardGameManager.Current.CardProperties)
            {
                switch (property.Type)
                {
                    case PropertyType.ObjectEnum:
                    case PropertyType.ObjectEnumList:
                    case PropertyType.StringEnum:
                    case PropertyType.StringEnumList:
                        if (!EnumPropertyFilters.ContainsKey(property.Name))
                            break;
                        EnumDef enumDef = CardGameManager.Current.Enums.FirstOrDefault(def => def.Property.Equals(property.Name));
                        if (enumDef != null)
                            filters += property.Name + ":=" + EnumPropertyFilters[property.Name] + "; ";
                        break;
                    case PropertyType.Integer:
                        if (IntMinPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + ">=" + IntMinPropertyFilters[property.Name] + "; ";
                        if (IntMaxPropertyFilters.ContainsKey(property.Name))
                            filters += property.Name + "<=" + IntMaxPropertyFilters[property.Name] + "; ";
                        break;
                    case PropertyType.Object:
                    case PropertyType.ObjectList:
                    case PropertyType.Number:
                    case PropertyType.Boolean:
                    case PropertyType.StringList:
                    case PropertyType.EscapedString:
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

    public string NameFilter
    {
        get { return _nameFilter ?? (_nameFilter = string.Empty); }
        set
        {
            _nameFilter = value;
            NameChangeCallback?.Invoke(_nameFilter);
            if (nameInputField != null)
                nameInputField.text = _nameFilter;
        }
    }

    public string IdFilter
    {
        get { return _idFilter ?? (_idFilter = string.Empty); }
        set { _idFilter = value; }
    }

    public string SetCodeFilter
    {
        get { return _setCodeFilter ?? (_setCodeFilter = string.Empty); }
        set { _setCodeFilter = value; }
    }
    public InputField ActiveInputField
    {
        get
        {
            return EventSystem.current.currentSelectedGameObject != null
                ? EventSystem.current.currentSelectedGameObject?.GetComponent<InputField>() : null;
        }
        set
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(value.gameObject);
        }
    }

    public Toggle ActiveToggle
    {
        get
        {
            return EventSystem.current.currentSelectedGameObject != null
                ? EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>() : null;
        }
        set
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(value.gameObject);
        }
    }
}
