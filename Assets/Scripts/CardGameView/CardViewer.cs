/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using CardGameDef;
using CGS;

namespace CardGameView
{
    public enum CardViewerMode
    {
        Minimal = 0,
        Expanded = 1,
        Maximal = 2
    }

    public class CardViewer : MonoBehaviour, ICardDisplay, IPointerDownHandler, ISelectHandler, IDeselectHandler
    {
        public const string SetLabel = "Set";
        public const string IdLabel = "Id";
        public const string Delimiter = ": ";

        public static CardViewer Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                GameObject cardViewer = GameObject.FindWithTag(Tags.CardViewer);
                _instance = cardViewer?.GetOrAddComponent<CardViewer>();
                return _instance;
            }
        }
        private static CardViewer _instance;

        public CardViewerMode Mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                Redisplay();
            }
        }
        private CardViewerMode _mode;

        public CanvasGroup minimal;
        public CanvasGroup expanded;
        public CanvasGroup maximal;

        public RectTransform maximalContent;

        public RectTransform zoomPanel;

        public List<AspectRatioFitter> cardAspectRatioFitters;
        public List<Image> cardImages;
        public List<Text> nameTexts;
        public Text idText;
        public Text setText;
        public Text propertyTextTemplate;
        public List<Dropdown> propertySelectors;
        public List<Text> propertyValueTexts;
        public List<Text> propertyTexts { get; } = new List<Text>();
        public List<Dropdown.OptionData> PropertyOptions { get; } = new List<Dropdown.OptionData>();
        public Dictionary<string, string> DisplayNameLookup { get; } = new Dictionary<string, string>();

        public int PrimaryPropertyIndex
        {
            get
            {
                int primaryPropertyIndex = 0;
                for (int i = 0; i < PropertyOptions.Count; i++)
                    if (DisplayNameLookup.TryGetValue(PropertyOptions[i].text, out string propertyName)
                            && propertyName.Equals(CardGameManager.Current.CardPrimaryProperty))
                        primaryPropertyIndex = i;
                return primaryPropertyIndex;
            }
        }

        public string SelectedPropertyName
        {
            get
            {
                string selectedName = SetLabel;
                if (SelectedPropertyIndex == 1)
                    selectedName = IdLabel;
                if (SelectedPropertyIndex > 1 && SelectedPropertyIndex < PropertyOptions.Count)
                    DisplayNameLookup.TryGetValue(SelectedPropertyDisplay, out selectedName);
                return selectedName;
            }
        }
        public string SelectedPropertyDisplay
        {
            get
            {
                string selectedDisplay = SetLabel;
                if (SelectedPropertyIndex == 1)
                    selectedDisplay = IdLabel;
                if (SelectedPropertyIndex > 1 && SelectedPropertyIndex < PropertyOptions.Count)
                    selectedDisplay = PropertyOptions[SelectedPropertyIndex].text;
                return selectedDisplay;
            }
        }
        public int SelectedPropertyIndex
        {
            get { return _selectedPropertyIndex; }
            set
            {
                _selectedPropertyIndex = value;
                if (_selectedPropertyIndex < 0)
                    _selectedPropertyIndex = PropertyOptions.Count - 1;
                if (_selectedPropertyIndex >= PropertyOptions.Count)
                    _selectedPropertyIndex = 0;
                foreach (Dropdown propertySelector in propertySelectors)
                    propertySelector.value = _selectedPropertyIndex;
                ResetPropertyValueText();
            }
        }
        private int _selectedPropertyIndex;

        public CardModel SelectedCardModel
        {
            get { return _selectedCardModel; }
            set
            {
                if (_selectedCardModel != null)
                {
                    _selectedCardModel.IsHighlighted = false;
                    _selectedCardModel.Value.UnregisterDisplay(this);
                }

                _selectedCardModel = value;

                if (_selectedCardModel != null)
                {
                    Card selectedCard = _selectedCardModel.Value;
                    ResetTexts();
                    selectedCard.RegisterDisplay(this);
                }
                else if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(null);
                IsVisible = _selectedCardModel != null;
                ZoomTime = 0;
            }
        }
        private CardModel _selectedCardModel;

        public bool Zoom
        {
            get { return zoomPanel.gameObject.activeSelf; }
            set { if (ZoomTime > 0.5f || value) zoomPanel.gameObject.SetActive(value); }
        }
        public float ZoomTime { get; private set; }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                if (!_isVisible && zoomPanel != null)
                    zoomPanel.gameObject.SetActive(false);
                if (SelectedCardModel != null)
                    SelectedCardModel.IsHighlighted = _isVisible;
                Redisplay();
            }
        }
        private bool _isVisible;
        public bool WasVisible { get; private set; }

        void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(ResetInfo);
        }

        void Start()
        {
            ResetInfo();
        }

        void Update()
        {
            WasVisible = IsVisible;
            if (!IsVisible || SelectedCardModel == null || CardGameManager.Instance.ModalCanvas != null)
                return;

            if (EventSystem.current.currentSelectedGameObject == null && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject);

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && SelectedCardModel.DoubleClickAction != null)
            {

                if (Mode == CardViewerMode.Maximal)
                    Mode = CardViewerMode.Expanded;
                else
                    SelectedCardModel.DoubleClickAction(SelectedCardModel);
            }
            else if (Input.GetButtonDown(Inputs.Sort))
                DecrementProperty();
            else if (Input.GetButtonDown(Inputs.Filter))
                IncrementProperty();
            else if (Input.GetButtonDown(Inputs.Option))
                Zoom = !Zoom;
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                SelectedCardModel = null;

            if (Zoom)
                ZoomTime += Time.deltaTime;
            else
                ZoomTime = 0;
        }

        public void ResetInfo()
        {
            foreach (AspectRatioFitter cardAspectRatioFitter in cardAspectRatioFitters)
                cardAspectRatioFitter.aspectRatio = CardGameManager.Current.CardAspectRatio;

            PropertyOptions.Clear();
            PropertyOptions.Add(new Dropdown.OptionData() { text = SetLabel });
            PropertyOptions.Add(new Dropdown.OptionData() { text = IdLabel });
            DisplayNameLookup.Clear();
            foreach (PropertyDef propertyDef in CardGameManager.Current.CardProperties)
                AddProperty(propertyDef);

            foreach (Dropdown propertySelector in propertySelectors)
            {
                propertySelector.options = PropertyOptions;
                propertySelector.value = PrimaryPropertyIndex;
                propertySelector.onValueChanged.Invoke(propertySelector.value);
            }
        }

        public void AddProperty(PropertyDef propertyDef, string parentPrefix = "")
        {
            if (propertyDef == null || parentPrefix == null)
            {
                Debug.LogWarning("AddProperty::NullProperty");
                return;
            }

            if (propertyDef.Type == PropertyType.Object || propertyDef.Type == PropertyType.ObjectList)
            {
                foreach (PropertyDef childProperty in propertyDef.Properties)
                    AddProperty(childProperty, parentPrefix + propertyDef.Name + PropertyDef.ObjectDelimiter);
            }
            else
            {
                string displayName = !string.IsNullOrEmpty(propertyDef.Display) ? propertyDef.Display : propertyDef.Name;
                PropertyOptions.Add(new Dropdown.OptionData() { text = displayName });
                DisplayNameLookup[displayName] = parentPrefix + propertyDef.Name;
            }
        }

        public void DecrementProperty()
        {
            SelectedPropertyIndex--;
        }

        public void IncrementProperty()
        {
            SelectedPropertyIndex++;
        }

        public void SetImageSprite(Sprite imageSprite)
        {
            foreach (Image cardImage in cardImages)
                cardImage.sprite = imageSprite ?? CardGameManager.Current.CardBackImageSprite;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnSelect(BaseEventData eventData)
        {
            IsVisible = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!Zoom)
                IsVisible = false;
        }

        public void MaximizeOn(CardModel cardModel)
        {
            SelectedCardModel = cardModel;
            Mode = CardViewerMode.Maximal;
        }

        public void ZoomOn(CardModel cardModel)
        {
            SelectedCardModel = cardModel;
            Zoom = true;
        }

        public void SetMode(int mode)
        {
            Mode = (CardViewerMode)mode;
        }

        private void Redisplay()
        {
            minimal.alpha = IsVisible && Mode == CardViewerMode.Minimal ? 1 : 0;
            minimal.interactable = IsVisible && Mode == CardViewerMode.Minimal;
            minimal.blocksRaycasts = IsVisible && Mode == CardViewerMode.Minimal;
            expanded.alpha = IsVisible && Mode == CardViewerMode.Expanded ? 1 : 0;
            expanded.interactable = IsVisible && Mode == CardViewerMode.Expanded;
            expanded.blocksRaycasts = IsVisible && Mode == CardViewerMode.Expanded;
            maximal.alpha = IsVisible && Mode == CardViewerMode.Maximal ? 1 : 0;
            maximal.interactable = IsVisible && Mode == CardViewerMode.Maximal;
            maximal.blocksRaycasts = IsVisible && Mode == CardViewerMode.Maximal;
        }

        private void ResetTexts()
        {
            foreach (Text nameText in nameTexts)
                nameText.text = SelectedCardModel.Value.Name;
            idText.text = IdLabel + Delimiter + SelectedCardModel.Id;
            setText.text = SetLabel + Delimiter
                + (CardGameManager.Current.Sets.TryGetValue(SelectedCardModel.Value.SetCode, out Set currentSet)
                    ? currentSet.ToString() : SelectedCardModel.Value.SetCode);
            foreach (Text propertyText in propertyTexts)
                Destroy(propertyText.gameObject);
            propertyTexts.Clear();
            for (int i = 2; i < PropertyOptions.Count; i++)
            {
                Text newPropertyText = Instantiate(propertyTextTemplate.gameObject, maximalContent).GetComponent<Text>();
                newPropertyText.gameObject.SetActive(true);
                newPropertyText.text = PropertyOptions[i].text + Delimiter
                    + (DisplayNameLookup.TryGetValue(PropertyOptions[i].text, out string propertyName)
                        ? SelectedCardModel.Value.GetPropertyValueString(propertyName) : string.Empty);
                propertyTexts.Add(newPropertyText);
            }
            ResetPropertyValueText();
        }

        private void ResetPropertyValueText()
        {
            if (SelectedCardModel == null)
            {
                foreach (Text propertyValueText in propertyValueTexts)
                    propertyValueText.text = string.Empty;
                return;
            }

            string newContentTextValue = string.Empty;
            if (SelectedPropertyIndex > 1)
                newContentTextValue = SelectedCardModel.Value.GetPropertyValueString(SelectedPropertyName);
            else if (SelectedPropertyIndex == 1)
                newContentTextValue = SelectedCardModel.Id;
            else if (CardGameManager.Current.Sets.TryGetValue(SelectedCardModel.Value.SetCode, out Set currentSet))
                newContentTextValue = currentSet.ToString();

            foreach (Text propertyValueText in propertyValueTexts)
                propertyValueText.text = newContentTextValue;
        }
    }
}
