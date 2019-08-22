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
    public class CardViewer : MonoBehaviour, ICardDisplay, IPointerDownHandler, ISelectHandler, IDeselectHandler
    {
        public const string SetLabel = "Set";

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

        public CanvasGroup infoPanel;
        public RectTransform zoomPanel;
        public Image cardImage;
        public Image zoomImage;
        public Text nameText;
        public Text idText;
        public Dropdown propertySelection;
        public Text labelText;
        public Text contentText;

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
                if (SelectedPropertyIndex >= 1 && SelectedPropertyIndex < PropertyOptions.Count)
                    DisplayNameLookup.TryGetValue(SelectedPropertyDisplay, out selectedName);
                return selectedName;
            }
        }
        public string SelectedPropertyDisplay
        {
            get
            {
                string selectedDisplay = SetLabel;
                if (SelectedPropertyIndex >= 1 && SelectedPropertyIndex < PropertyOptions.Count)
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
                propertySelection.value = _selectedPropertyIndex;
                labelText.text = SelectedPropertyDisplay;
                SetContentText();
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
                    nameText.text = selectedCard.Name;
                    idText.text = selectedCard.Id;
                    SetContentText();
                    selectedCard.RegisterDisplay(this);
                }
                else if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(null);
                IsVisible = _selectedCardModel != null;
                ZoomTime = 0;
            }
        }
        private CardModel _selectedCardModel;

        public bool IsVisible
        {
            get { return infoPanel.alpha > 0; }
            set
            {
                bool isVisible = value;
                infoPanel.alpha = isVisible ? 1 : 0;
                infoPanel.interactable = isVisible;
                infoPanel.blocksRaycasts = isVisible;
                if (!isVisible && zoomPanel != null)
                    zoomPanel.gameObject.SetActive(false);
                if (SelectedCardModel != null)
                    SelectedCardModel.IsHighlighted = isVisible;
            }
        }
        public bool WasVisible { get; private set; }

        public bool Zoom
        {
            get { return zoomPanel.gameObject.activeSelf; }
            set { if (ZoomTime > 0.5f || value) zoomPanel.gameObject.SetActive(value); }
        }
        public float ZoomTime { get; private set; }

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
                SelectedCardModel.DoubleClickAction(SelectedCardModel);
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
            cardImage.gameObject.GetOrAddComponent<AspectRatioFitter>().aspectRatio = CardGameManager.Current.CardAspectRatio;
            zoomPanel.GetChild(0).gameObject.GetOrAddComponent<AspectRatioFitter>().aspectRatio = CardGameManager.Current.CardAspectRatio;

            PropertyOptions.Clear();
            PropertyOptions.Add(new Dropdown.OptionData() { text = SetLabel });
            DisplayNameLookup.Clear();
            foreach (PropertyDef propertyDef in CardGameManager.Current.CardProperties)
                AddProperty(propertyDef);
            propertySelection.options = PropertyOptions;
            propertySelection.value = PrimaryPropertyIndex;
            propertySelection.onValueChanged.Invoke(propertySelection.value);
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

        public void SetContentText()
        {
            if (SelectedCardModel == null)
            {
                contentText.text = string.Empty;
                return;
            }

            string newContentTextValue = string.Empty;
            if (SelectedPropertyIndex != 0)
                newContentTextValue = SelectedCardModel.Value.GetPropertyValueString(SelectedPropertyName);
            else if (CardGameManager.Current.Sets.TryGetValue(SelectedCardModel.Value.SetCode, out Set currentSet))
                newContentTextValue = currentSet.ToString();
            contentText.text = newContentTextValue;
        }

        public void SetImageSprite(Sprite imageSprite)
        {
            cardImage.sprite = imageSprite ?? CardGameManager.Current.CardBackImageSprite;
            zoomImage.sprite = imageSprite ?? CardGameManager.Current.CardBackImageSprite;
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

        public void ZoomOn(CardModel cardModel)
        {
            SelectedCardModel = cardModel;
            Zoom = true;
        }
    }
}
