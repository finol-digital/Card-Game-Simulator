/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using CardGameDef;
using CGS.Menu;

namespace CGS.Cards
{
    public delegate void OnSearchDelegate(string filters, List<Card> searchResults);

    public class CardSearchMenu : MonoBehaviour
    {
        public float PropertyPanelHeight => ((RectTransform)stringFilterPanel.transform).rect.height;

        public Scrollbar scrollbar;
        public InputField nameInputField;
        public InputField idInputField;
        public InputField setCodeInputField;
        public RectTransform propertyFiltersContent;
        public SearchFilterPanel stringFilterPanel;
        public SearchFilterPanel integerFilterPanel;
        public SearchFilterPanel toggleFilterPanel;

        public OnSearchDelegate SearchCallback { get; set; }

        public List<GameObject> FilterPanels { get; } = new List<GameObject>();
        public List<InputField> InputFields { get; } = new List<InputField>();
        public List<Toggle> Toggles { get; } = new List<Toggle>();

        public CardSearchFilters Filters { get; } = new CardSearchFilters();
        public List<Card> Results { get; } = new List<Card>();

        private bool _wasDown;
        private bool _wasUp;
        private bool _wasLeft;
        private bool _wasRight;
        private bool _wasPage;

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

        void Update()
        {
            if (gameObject != CardGameManager.Instance.TopMenuCanvas?.gameObject)
                return;

            if (Input.GetButtonDown(Inputs.FocusBack) || Input.GetAxis(Inputs.FocusBack) != 0
                    || Input.GetButtonDown(Inputs.FocusNext) || Input.GetAxis(Inputs.FocusNext) != 0)
            {
                FocusInputField();
                return;
            }

            if (ActiveInputField != null && ActiveInputField.isFocused)
                return;

            if (Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0
                    || Input.GetButtonDown(Inputs.Horizontal) || Input.GetAxis(Inputs.Horizontal) != 0)
                FocusToggle();
            else if ((Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0) && !_wasPage)
                scrollbar.value = Mathf.Clamp01(scrollbar.value + (Input.GetAxis(Inputs.PageVertical) < 0 ? 0.1f : -0.1f));

            if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
            {
                Search();
                Hide();
            }
            else if (Input.GetButtonDown(Inputs.New) && ActiveToggle != null)
                ToggleEnum();
            else if (Input.GetButtonDown(Inputs.Option) && ActiveInputField == null)
                ClearFilters();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Hide();

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasLeft = Input.GetAxis(Inputs.Horizontal) < 0;
            _wasRight = Input.GetAxis(Inputs.Horizontal) > 0;
            _wasPage = Input.GetAxis(Inputs.PageVertical) != 0;
        }

        public void FocusInputField()
        {
            if (ActiveInputField == null || InputFields.Count < 1)
            {
                InputFields.FirstOrDefault()?.ActivateInputField();
                ActiveInputField = InputFields.FirstOrDefault();
                return;
            }

            if (Input.GetButtonDown(Inputs.FocusBack) || Input.GetAxis(Inputs.FocusBack) != 0)
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

            if (Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
            {
                Transform currentPanel = ActiveToggle.transform.parent;
                if (Input.GetAxis(Inputs.Vertical) > 0 && !_wasUp)
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
                else if (Input.GetAxis(Inputs.Vertical) < 0 && !_wasDown)
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
            else if (Input.GetButton(Inputs.Horizontal) || Input.GetAxis(Inputs.Horizontal) != 0)
            {
                if (Input.GetAxis(Inputs.Horizontal) > 0 && !_wasRight)
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
                else if (Input.GetAxis(Inputs.Horizontal) < 0 && !_wasLeft)
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

        public void Show(OnSearchDelegate searchCallback)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SearchCallback = searchCallback;

            stringFilterPanel.gameObject.SetActive(false);
            integerFilterPanel.gameObject.SetActive(false);
            toggleFilterPanel.gameObject.SetActive(false);
            for (int i = FilterPanels.Count - 1; i >= 0; i--)
            {
                Destroy(FilterPanels[i].gameObject);
                FilterPanels.RemoveAt(i);
            }
            InputFields.Clear();
            Toggles.Clear();

            nameInputField.text = Filters.Name;
            nameInputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
            InputFields.Add(nameInputField);

            idInputField.text = Filters.Id;
            idInputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
            InputFields.Add(idInputField);

            setCodeInputField.text = Filters.SetCode;
            setCodeInputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
            InputFields.Add(setCodeInputField);

            foreach (PropertyDef property in CardGameManager.Current.CardProperties)
                AddPropertyPanel(property, property.Name);
            propertyFiltersContent.sizeDelta = new Vector2(propertyFiltersContent.sizeDelta.x, PropertyPanelHeight * (FilterPanels.Count + 3));
        }

        public void AddPropertyPanel(PropertyDef forProperty, string propertyName)
        {
            if (forProperty == null || string.IsNullOrEmpty(propertyName))
            {
                Debug.LogWarning("AddPropertyPanel::NullPropertyOrName");
                return;
            }

            if (forProperty.Type == PropertyType.Object || forProperty.Type == PropertyType.ObjectList)
            {
                foreach (PropertyDef childProperty in forProperty.Properties)
                    AddPropertyPanel(childProperty, propertyName + PropertyDef.ObjectDelimiter + childProperty.Name);
                return;
            }

            GameObject newPanel;
            if (CardGameManager.Current.IsEnumProperty(propertyName))
                newPanel = CreateEnumPropertyFilterPanel(propertyName, forProperty);
            else if (forProperty.Type == PropertyType.Boolean)
                newPanel = CreateBooleanPropertyFilterPanel(propertyName, forProperty.Display);
            else if (forProperty.Type == PropertyType.Integer)
                newPanel = CreateIntegerPropertyFilterPanel(propertyName, forProperty.Display);
            else //if (property.Type == PropertyType.String)
                newPanel = CreateStringPropertyFilterPanel(propertyName, forProperty.Display);
            FilterPanels.Add(newPanel);
            foreach (InputField inputField in newPanel.GetComponentsInChildren<InputField>())
            {
                inputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return Inputs.FilterFocusNameInput(addedChar); };
                InputFields.Add(inputField);
            }
            foreach (Toggle toggle in newPanel.GetComponentsInChildren<Toggle>())
                Toggles.Add(toggle);
        }

        public GameObject CreateStringPropertyFilterPanel(string propertyName, string displayName)
        {
            GameObject newPanel = Instantiate(stringFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            SearchFilterPanel config = newPanel.GetComponent<SearchFilterPanel>();
            config.nameLabelText.text = !string.IsNullOrEmpty(displayName) ? displayName : propertyName;
            if (Filters.StringProperties.TryGetValue(propertyName, out string storedFilter))
                config.stringInputField.text = storedFilter;
            config.stringPlaceHolderText.text = "Enter " + propertyName + "...";
            config.stringInputField.onValueChanged.AddListener(text => SetStringPropertyFilter(propertyName, text));

            return newPanel;
        }

        public GameObject CreateIntegerPropertyFilterPanel(string propertyName, string displayName)
        {
            GameObject newPanel = Instantiate(integerFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            SearchFilterPanel config = newPanel.GetComponent<SearchFilterPanel>();
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

        public GameObject CreateBooleanPropertyFilterPanel(string propertyName, string displayName)
        {
            GameObject newPanel = Instantiate(toggleFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            SearchFilterPanel config = newPanel.GetComponent<SearchFilterPanel>();
            config.nameLabelText.text = !string.IsNullOrEmpty(displayName) ? displayName : propertyName + "?";
            bool hasFilter = false;
            bool storedFilter;
            if (Filters.BoolProperties.TryGetValue(propertyName, out storedFilter))
                hasFilter = true;

            Toggle toggle;
            float toggleWidth;
            RectTransform toggleImageTransform;
            Vector3 toggleLocalPosition = config.toggle.transform.localPosition;
            float panelWidth = 0;

            toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer).GetOrAddComponent<Toggle>();
            toggle.isOn = hasFilter && storedFilter;
            toggle.onValueChanged.AddListener(isOn => SetBoolPropertyFilter(propertyName, true, isOn));
            toggle.GetComponentInChildren<Text>().text = "true";
            toggle.transform.localPosition = toggleLocalPosition;
            toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25;
            toggleImageTransform = (RectTransform)toggle.GetComponentInChildren<Image>().transform;
            toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
            toggleLocalPosition.x += toggleWidth;
            panelWidth += toggleWidth;

            toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer).GetOrAddComponent<Toggle>();
            toggle.isOn = hasFilter && !storedFilter;
            toggle.onValueChanged.AddListener(isOn => SetBoolPropertyFilter(propertyName, false, isOn));
            toggle.GetComponentInChildren<Text>().text = "false";
            toggle.transform.localPosition = toggleLocalPosition;
            toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25;
            toggleImageTransform = (RectTransform)toggle.GetComponentInChildren<Image>().transform;
            toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
            toggleLocalPosition.x += toggleWidth;
            panelWidth += toggleWidth;

            config.toggle.gameObject.SetActive(false);
            config.toggleGroupContainer.sizeDelta = new Vector2(panelWidth, config.toggleGroupContainer.sizeDelta.y);

            return newPanel;
        }

        public GameObject CreateEnumPropertyFilterPanel(string propertyName, PropertyDef property)
        {
            GameObject newPanel = Instantiate(toggleFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            SearchFilterPanel config = newPanel.GetComponent<SearchFilterPanel>();
            config.nameLabelText.text = !string.IsNullOrEmpty(property.Display) ? property.Display : propertyName;
            int storedFilter = 0;
            Filters.EnumProperties.TryGetValue(propertyName, out storedFilter);
            EnumDef enumDef = CardGameManager.Current.Enums.First(def => def.Property.Equals(propertyName));
            float toggleWidth = 0;
            Vector3 toggleLocalPosition = config.toggle.transform.localPosition;
            float panelWidth = 0;

            if (property.DisplayEmptyFirst)
            {
                toggleWidth = CreateEmptyEnumToggle(propertyName, property, enumDef, config, storedFilter, toggleLocalPosition);
                toggleLocalPosition.x += toggleWidth;
                panelWidth += toggleWidth;
            }

            foreach (KeyValuePair<string, string> enumValue in enumDef.Values)
            {
                int lookupKey = 0;
                if (!enumDef.Lookups.TryGetValue(enumValue.Key, out lookupKey))
                    lookupKey = enumDef.CreateLookup(enumValue.Key);
                Toggle toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer).GetOrAddComponent<Toggle>();
                toggle.isOn = (storedFilter & lookupKey) != 0;
                toggle.onValueChanged.AddListener(isOn => SetEnumPropertyFilter(propertyName, lookupKey, isOn));
                toggle.GetComponentInChildren<Text>().text = enumValue.Value;
                toggle.transform.localPosition = toggleLocalPosition;
                toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25;
                RectTransform toggleImageTransform = (RectTransform)toggle.GetComponentInChildren<Image>().transform;
                toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
                toggleLocalPosition.x += toggleWidth;
                panelWidth += toggleWidth;
            }

            if (!property.DisplayEmptyFirst && !string.IsNullOrEmpty(property.DisplayEmpty))
            {
                toggleWidth = CreateEmptyEnumToggle(propertyName, property, enumDef, config, storedFilter, toggleLocalPosition);
                toggleLocalPosition.x += toggleWidth;
                panelWidth += toggleWidth;
            }
            config.toggle.gameObject.SetActive(false);
            config.toggleGroupContainer.sizeDelta = new Vector2(panelWidth, config.toggleGroupContainer.sizeDelta.y);

            return newPanel;
        }

        public float CreateEmptyEnumToggle(string propertyName, PropertyDef property, EnumDef enumDef, SearchFilterPanel config, int storedFilter, Vector3 toggleLocalPosition)
        {
            int lookupKey = 0;
            if (!enumDef.Lookups.TryGetValue(property.DisplayEmpty, out lookupKey))
                lookupKey = enumDef.CreateLookup(property.DisplayEmpty);
            Toggle toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer).GetOrAddComponent<Toggle>();
            toggle.isOn = (storedFilter & lookupKey) != 0;
            toggle.onValueChanged.AddListener(isOn => SetEnumPropertyFilter(propertyName, lookupKey, isOn));
            toggle.GetComponentInChildren<Text>().text = property.DisplayEmpty;
            toggle.transform.localPosition = toggleLocalPosition;
            float toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25;
            RectTransform toggleImageTransform = (RectTransform)toggle.GetComponentInChildren<Image>().transform;
            toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
            return toggleWidth;
        }

        public void SetFilters(string input)
        {
            Filters.Parse(input);
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

        public void SetBoolPropertyFilter(string propertyName, bool filterValue, bool isOn)
        {
            if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
                return;

            if (isOn)
                Filters.BoolProperties[propertyName] = filterValue;
            else if (Filters.BoolProperties.ContainsKey(propertyName))
                Filters.BoolProperties.Remove(propertyName);
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

            Filters.Clear();
        }

        public void ClearSearch()
        {
            ClearFilters();
            Search();
        }

        public void Search()
        {
            Results.Clear();
            bool hideReprints = Settings.HideReprints;
            IEnumerable<Card> cardSearcher = CardGameManager.Current.FilterCards(Filters);
            foreach (Card card in cardSearcher)
                if (!hideReprints || !card.IsReprint)
                    Results.Add(card);
            SearchCallback?.Invoke(Filters.ToString(), Results);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
