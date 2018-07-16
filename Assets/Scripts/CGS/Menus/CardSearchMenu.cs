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

    public CardSearchFilters Filters { get; } = new CardSearchFilters();
    public List<Card> Results { get; } = new List<Card>();

    void LateUpdate()
    {
        if (!Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
            return;

        if (Input.GetButtonDown(Inputs.FocusName) || Input.GetButtonDown(Inputs.FocusText))
        {
            FocusInputField();
            return;
        }

        if (ActiveInputField != null && ActiveInputField.isFocused)
            return;

        if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
        {
            Search();
            Hide();
        }
        else if (Input.GetButtonDown(Inputs.Page))
            scrollbar.value = Mathf.Clamp01(scrollbar.value + (Input.GetAxis(Inputs.Page) < 0 ? 0.1f : -0.1f));
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
        if (Filters.StringProperties.TryGetValue(propertyName, out storedFilter))
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

        if (Filters.IntMinProperties.TryGetValue(propertyName, out storedFilter))
            config.integerMinInputField.text = storedFilter.ToString();
        config.integerMinInputField.onValueChanged.AddListener(text => SetIntMinPropertyFilter(propertyName, text));

        if (Filters.IntMaxProperties.TryGetValue(propertyName, out storedFilter))
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
        Filters.EnumProperties.TryGetValue(property.Name, out storedFilter);

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

    public void SetNameFilter(string name)
    {
        Filters.Name = name;
    }

    public void SetIdFilter(string id)
    {
        Filters.Id = id;
    }

    public void SetCodeFilter(string code)
    {
        Filters.SetCode = code;
    }

    public void SetStringPropertyFilter(string propertyName, string filterValue)
    {
        if (string.IsNullOrEmpty(filterValue))
        {
            if (Filters.StringProperties.ContainsKey(propertyName))
                Filters.StringProperties.Remove(propertyName);
            return;
        }

        Filters.StringProperties[propertyName] = filterValue;
    }

    public void SetIntMinPropertyFilter(string propertyName, string filterValue)
    {
        int intValue;
        if (!int.TryParse(filterValue, out intValue))
        {
            if (Filters.IntMinProperties.ContainsKey(propertyName))
                Filters.IntMinProperties.Remove(propertyName);
            return;
        }

        Filters.IntMinProperties[propertyName] = intValue;
    }

    public void SetIntMaxPropertyFilter(string propertyName, string filterValue)
    {
        int intValue;
        if (!int.TryParse(filterValue, out intValue))
        {
            if (Filters.IntMaxProperties.ContainsKey(propertyName))
                Filters.IntMaxProperties.Remove(propertyName);
            return;
        }

        Filters.IntMaxProperties[propertyName] = intValue;
    }

    public void SetEnumPropertyFilter(string propertyName, int filterValue, bool isOn)
    {
        if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
            return;

        bool isStored = Filters.EnumProperties.ContainsKey(propertyName);
        int storedFilter = 0;
        if (isStored)
            storedFilter = Filters.EnumProperties[propertyName];

        int newFilter = isOn ? storedFilter | filterValue : storedFilter & ~filterValue;
        if (newFilter == 0)
        {
            if (isStored)
                Filters.EnumProperties.Remove(propertyName);
        }
        else
            Filters.EnumProperties[propertyName] = newFilter;
    }

    public void ClearFilters()
    {
        foreach (InputField input in GetComponentsInChildren<InputField>())
            input.text = string.Empty;
        foreach (Toggle toggle in GetComponentsInChildren<Toggle>())
            toggle.isOn = false;

        Filters.StringProperties.Clear();
        Filters.IntMinProperties.Clear();
        Filters.IntMaxProperties.Clear();
        Filters.EnumProperties.Clear();
    }

    public void ClearSearch()
    {
        ClearFilters();
        Search();
    }

    public void Search()
    {
        Results.Clear();
        IEnumerable<Card> cardSearcher = CardGameManager.Current.FilterCards(Filters);
        foreach (Card card in cardSearcher)
            Results.Add(card);
        SearchCallback?.Invoke(Filters.ToString(), Results);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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
