/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public delegate void OnSearchDelegate(string filters, List<UnityCard> searchResults);

    public class CardSearchMenu : Modal
    {
        private float PropertyPanelHeight => ((RectTransform) stringFilterPanel.transform).rect.height;

        public Scrollbar scrollbar;
        public InputField nameInputField;
        public InputField idInputField;
        public InputField setCodeInputField;
        public RectTransform propertyFiltersContent;
        public SearchFilterPanel stringFilterPanel;
        public SearchFilterPanel integerFilterPanel;
        public SearchFilterPanel toggleFilterPanel;

        public OnSearchDelegate SearchCallback { get; set; }

        protected override List<InputField> InputFields
        {
            get => _inputFields;
            set => _inputFields = value;
        }

        private List<InputField> _inputFields = new List<InputField>();

        protected override List<Toggle> Toggles
        {
            get => _toggles;
            set => _toggles = value;
        }

        private List<Toggle> _toggles = new List<Toggle>();

        private readonly List<GameObject> _filterPanels = new List<GameObject>();
        private readonly CardSearchFilters _filters = new CardSearchFilters();
        private readonly List<UnityCard> _results = new List<UnityCard>();

        private void Update()
        {
            if (!IsFocused)
                return;

            if (Inputs.IsFocus)
            {
                FocusInputField();
                return;
            }

            if (ActiveInputField != null && ActiveInputField.isFocused)
                return;

            if (Inputs.IsVertical || Inputs.IsHorizontal)
                FocusToggle();
            else if (Inputs.IsPageVertical && !Inputs.WasPageVertical)
                Scroll();

            if (Inputs.IsSubmit)
            {
                Search();
                Hide();
            }
            else if (Inputs.IsNew && ActiveToggle != null)
                ToggleEnum();
            else if (Inputs.IsOption && ActiveInputField == null)
                ClearFilters();
            else if (Inputs.IsCancel)
                Hide();
        }

        private void Scroll()
        {
            scrollbar.value = Mathf.Clamp01(scrollbar.value + (Inputs.IsPageDown ? 0.1f : -0.1f));
        }

        public void Show(OnSearchDelegate searchCallback)
        {
            Show();

            SearchCallback = searchCallback;

            stringFilterPanel.gameObject.SetActive(false);
            integerFilterPanel.gameObject.SetActive(false);
            toggleFilterPanel.gameObject.SetActive(false);
            for (int i = _filterPanels.Count - 1; i >= 0; i--)
            {
                Destroy(_filterPanels[i].gameObject);
                _filterPanels.RemoveAt(i);
            }

            _inputFields.Clear();
            _toggles.Clear();

            nameInputField.text = _filters.Name;
            nameInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            _inputFields.Add(nameInputField);

            idInputField.text = _filters.Id;
            idInputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            _inputFields.Add(idInputField);

            setCodeInputField.text = _filters.SetCode;
            setCodeInputField.onValidateInput +=
                (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
            _inputFields.Add(setCodeInputField);

            foreach (PropertyDef property in CardGameManager.Current.CardProperties)
                AddPropertyPanel(property, property.Name);
            propertyFiltersContent.sizeDelta = new Vector2(propertyFiltersContent.sizeDelta.x,
                PropertyPanelHeight * (_filterPanels.Count + 3));
        }

        private void AddPropertyPanel(PropertyDef forProperty, string propertyName)
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
            else
                switch (forProperty.Type)
                {
                    case PropertyType.Boolean:
                        newPanel = CreateBooleanPropertyFilterPanel(propertyName, forProperty.Display);
                        break;
                    case PropertyType.Integer:
                        newPanel = CreateIntegerPropertyFilterPanel(propertyName, forProperty.Display);
                        break;
                    case PropertyType.String:
                    case PropertyType.EscapedString:
                    case PropertyType.Object:
                    case PropertyType.StringEnum:
                    case PropertyType.StringList:
                    case PropertyType.StringEnumList:
                    case PropertyType.ObjectEnum:
                    case PropertyType.ObjectList:
                    case PropertyType.ObjectEnumList:
                    default:
                        newPanel = CreateStringPropertyFilterPanel(propertyName, forProperty.Display);
                        break;
                }

            _filterPanels.Add(newPanel);

            foreach (InputField inputField in newPanel.GetComponentsInChildren<InputField>())
            {
                inputField.onValidateInput += (input, charIndex, addedChar) => Inputs.FilterFocusInput(addedChar);
                _inputFields.Add(inputField);
            }

            foreach (Toggle toggle in newPanel.GetComponentsInChildren<Toggle>())
                _toggles.Add(toggle);
        }

        private GameObject CreateStringPropertyFilterPanel(string propertyName, string displayName)
        {
            GameObject newPanel = Instantiate(stringFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            var config = newPanel.GetComponent<SearchFilterPanel>();
            config.nameLabelText.text = !string.IsNullOrEmpty(displayName) ? displayName : propertyName;
            if (_filters.StringProperties.TryGetValue(propertyName, out string storedFilter))
                config.stringInputField.text = storedFilter;
            config.stringPlaceHolderText.text = "Enter " + propertyName + "...";
            config.stringInputField.onValueChanged.AddListener(text => SetStringPropertyFilter(propertyName, text));

            return newPanel;
        }

        private GameObject CreateIntegerPropertyFilterPanel(string propertyName, string displayName)
        {
            GameObject newPanel = Instantiate(integerFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            var config = newPanel.GetComponent<SearchFilterPanel>();
            config.nameLabelText.text = !string.IsNullOrEmpty(displayName) ? displayName : propertyName;

            if (_filters.IntMinProperties.TryGetValue(propertyName, out int storedFilter))
                config.integerMinInputField.text = storedFilter.ToString();
            config.integerMinInputField.onValueChanged.AddListener(text => SetIntMinPropertyFilter(propertyName, text));

            if (_filters.IntMaxProperties.TryGetValue(propertyName, out storedFilter))
                config.integerMaxInputField.text = storedFilter.ToString();
            config.integerMaxInputField.onValueChanged.AddListener(text => SetIntMaxPropertyFilter(propertyName, text));

            return newPanel;
        }

        private GameObject CreateBooleanPropertyFilterPanel(string propertyName, string displayName)
        {
            GameObject newPanel = Instantiate(toggleFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            var config = newPanel.GetComponent<SearchFilterPanel>();
            config.nameLabelText.text = !string.IsNullOrEmpty(displayName) ? displayName : propertyName + "?";
            bool hasFilter = _filters.BoolProperties.TryGetValue(propertyName, out bool storedFilter);

            Vector3 toggleLocalPosition = config.toggle.transform.localPosition;
            float panelWidth = 0;

            var toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer).GetOrAddComponent<Toggle>();
            Transform toggleTransform = toggle.transform;
            toggle.GetComponentInChildren<Text>().text = "true";
            float toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25f;
            toggle.isOn = hasFilter && storedFilter;
            toggle.onValueChanged.AddListener(isOn => SetBoolPropertyFilter(propertyName, true, isOn));
            toggleTransform.localPosition = toggleLocalPosition;
            var toggleImageTransform = (RectTransform) toggle.GetComponentInChildren<Image>().transform;
            toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
            toggleLocalPosition.x += toggleWidth;
            panelWidth += toggleWidth;

            toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer).GetOrAddComponent<Toggle>();
            toggle.GetComponentInChildren<Text>().text = "false";
            toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25;
            toggle.isOn = hasFilter && !storedFilter;
            toggle.onValueChanged.AddListener(isOn => SetBoolPropertyFilter(propertyName, false, isOn));
            toggle.transform.localPosition = toggleLocalPosition;
            toggleImageTransform = (RectTransform) toggle.GetComponentInChildren<Image>().transform;
            toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
            toggleLocalPosition.x += toggleWidth;
            panelWidth += toggleWidth;

            config.toggle.gameObject.SetActive(false);
            config.toggleGroupContainer.sizeDelta = new Vector2(panelWidth, config.toggleGroupContainer.sizeDelta.y);

            return newPanel;
        }

        private GameObject CreateEnumPropertyFilterPanel(string propertyName, PropertyDef property)
        {
            GameObject newPanel = Instantiate(toggleFilterPanel.gameObject, propertyFiltersContent);
            newPanel.gameObject.SetActive(true);

            var config = newPanel.GetComponent<SearchFilterPanel>();
            config.nameLabelText.text = !string.IsNullOrEmpty(property.Display) ? property.Display : propertyName;
            _filters.EnumProperties.TryGetValue(propertyName, out int storedFilter);
            EnumDef enumDef = CardGameManager.Current.Enums.First(def => def.Property.Equals(propertyName));
            float toggleWidth;
            Vector3 toggleLocalPosition = config.toggle.transform.localPosition;
            float panelWidth = 0;

            if (property.DisplayEmptyFirst)
            {
                toggleWidth = CreateEmptyEnumToggle(propertyName, property, enumDef, config, storedFilter,
                    toggleLocalPosition);
                toggleLocalPosition.x += toggleWidth;
                panelWidth += toggleWidth;
            }

            foreach (KeyValuePair<string, string> enumValue in enumDef.Values)
            {
                if (!enumDef.Lookups.TryGetValue(enumValue.Key, out int lookupKey))
                    lookupKey = enumDef.CreateLookup(enumValue.Key);
                var toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer)
                    .GetOrAddComponent<Toggle>();
                toggle.isOn = (storedFilter & lookupKey) != 0;
                toggle.onValueChanged.AddListener(isOn => SetEnumPropertyFilter(propertyName, lookupKey, isOn));
                toggle.GetComponentInChildren<Text>().text = enumValue.Value;
                toggle.transform.localPosition = toggleLocalPosition;
                toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25;
                var toggleImageTransform = (RectTransform) toggle.GetComponentInChildren<Image>().transform;
                toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
                toggleLocalPosition.x += toggleWidth;
                panelWidth += toggleWidth;
            }

            if (!property.DisplayEmptyFirst && !string.IsNullOrEmpty(property.DisplayEmpty))
            {
                toggleWidth = CreateEmptyEnumToggle(propertyName, property, enumDef, config, storedFilter,
                    toggleLocalPosition);
                toggleLocalPosition.x += toggleWidth;
                panelWidth += toggleWidth;
            }

            config.toggle.gameObject.SetActive(false);
            config.toggleGroupContainer.sizeDelta = new Vector2(panelWidth, config.toggleGroupContainer.sizeDelta.y);

            return newPanel;
        }

        private float CreateEmptyEnumToggle(string propertyName, PropertyDef property, EnumDef enumDef,
            SearchFilterPanel config, int storedFilter, Vector3 toggleLocalPosition)
        {
            if (!enumDef.Lookups.TryGetValue(property.DisplayEmpty, out int lookupKey))
                lookupKey = enumDef.CreateLookup(property.DisplayEmpty);
            var toggle = Instantiate(config.toggle.gameObject, config.toggleGroupContainer)
                .GetOrAddComponent<Toggle>();
            toggle.isOn = (storedFilter & lookupKey) != 0;
            toggle.onValueChanged.AddListener(isOn => SetEnumPropertyFilter(propertyName, lookupKey, isOn));
            toggle.GetComponentInChildren<Text>().text = property.DisplayEmpty;
            toggle.transform.localPosition = toggleLocalPosition;
            float toggleWidth = toggle.GetComponentInChildren<Text>().preferredWidth + 25;
            var toggleImageTransform = (RectTransform) toggle.GetComponentInChildren<Image>().transform;
            toggleImageTransform.sizeDelta = new Vector2(toggleWidth, toggleImageTransform.sizeDelta.y);
            return toggleWidth;
        }

        [UsedImplicitly]
        public void SetFilters(string input)
        {
            _filters.Parse(input);
        }

        [UsedImplicitly]
        public void SetNameFilter(string nameFilter)
        {
            _filters.Name = nameFilter;
        }

        [UsedImplicitly]
        public void SetIdFilter(string idFilter)
        {
            _filters.Id = idFilter;
        }

        [UsedImplicitly]
        public void SetCodeFilter(string codeFilter)
        {
            _filters.SetCode = codeFilter;
        }

        [UsedImplicitly]
        public void SetStringPropertyFilter(string propertyName, string filterValue)
        {
            if (string.IsNullOrEmpty(filterValue))
            {
                if (_filters.StringProperties.ContainsKey(propertyName))
                    _filters.StringProperties.Remove(propertyName);
                return;
            }

            _filters.StringProperties[propertyName] = filterValue;
        }

        [UsedImplicitly]
        public void SetIntMinPropertyFilter(string propertyName, string filterValue)
        {
            if (!int.TryParse(filterValue, out int intValue))
            {
                if (_filters.IntMinProperties.ContainsKey(propertyName))
                    _filters.IntMinProperties.Remove(propertyName);
                return;
            }

            _filters.IntMinProperties[propertyName] = intValue;
        }

        [UsedImplicitly]
        public void SetIntMaxPropertyFilter(string propertyName, string filterValue)
        {
            if (!int.TryParse(filterValue, out int intValue))
            {
                if (_filters.IntMaxProperties.ContainsKey(propertyName))
                    _filters.IntMaxProperties.Remove(propertyName);
                return;
            }

            _filters.IntMaxProperties[propertyName] = intValue;
        }

        [UsedImplicitly]
        public void SetBoolPropertyFilter(string propertyName, bool filterValue, bool isOn)
        {
            if (Inputs.IsSubmit)
                return;

            if (isOn)
                _filters.BoolProperties[propertyName] = filterValue;
            else if (_filters.BoolProperties.ContainsKey(propertyName))
                _filters.BoolProperties.Remove(propertyName);
        }

        [UsedImplicitly]
        public void SetEnumPropertyFilter(string propertyName, int filterValue, bool isOn)
        {
            if (Inputs.IsSubmit)
                return;

            bool isStored = _filters.EnumProperties.ContainsKey(propertyName);
            var storedFilter = 0;
            if (isStored)
                storedFilter = _filters.EnumProperties[propertyName];

            int newFilter = isOn ? storedFilter | filterValue : storedFilter & ~filterValue;
            if (newFilter == 0)
            {
                if (isStored)
                    _filters.EnumProperties.Remove(propertyName);
            }
            else
                _filters.EnumProperties[propertyName] = newFilter;
        }

        [UsedImplicitly]
        public void ClearFilters()
        {
            foreach (InputField input in GetComponentsInChildren<InputField>())
                input.text = string.Empty;
            foreach (Toggle toggle in GetComponentsInChildren<Toggle>())
                toggle.isOn = false;

            _filters.Clear();
        }

        [UsedImplicitly]
        public void ClearSearch()
        {
            ClearFilters();
            Search();
        }

        [UsedImplicitly]
        public void Search()
        {
            _results.Clear();
            bool hideReprints = Settings.HideReprints;
            IEnumerable<UnityCard> cardSearcher = CardGameManager.Current.FilterCards(_filters);
            foreach (UnityCard card in cardSearcher)
                if (!hideReprints || !card.IsReprint)
                    _results.Add(card);
            SearchCallback?.Invoke(_filters.ToString(), _results);
        }
    }
}
