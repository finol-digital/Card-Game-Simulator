using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInfoViewer : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public const string GameObjectTag = "CardInfo";
    public const float VisibleYMin = 0.625f;
    public const float VisibleYMax = 1;
    public const float HiddenYmin = 1.025f;
    public const float HiddenYMax = 1.4f;

    public GameObject cardZoomPrefab;
    public Image cardImage;
    public Text nameText;
    public Text idText;
    public Dropdown propertySelection;
    public Text labelText;
    public Text contentText;
    public float animationSpeed = 5.0f;

    private static CardInfoViewer _instance;

    private RectTransform _cardZoomPanel;
    private List<Dropdown.OptionData> _propertyOptions;
    private int _selectedPropertyIndex;
    private CardModel _selectedCardModel;
    private bool _isVisible;

    public void ResetPropertyOptions()
    {
        SelectedPropertyIndex = 0;
        PropertyOptions.Clear();
        foreach (PropertyDef propDef in CardGameManager.Current.CardProperties) {
            PropertyOptions.Add(new Dropdown.OptionData() { text = propDef.Name });
            if (propDef.Name.Equals(CardGameManager.Current.CardPrimaryProperty))
                SelectedPropertyIndex = PropertyOptions.Count - 1;
        }
        propertySelection.options = PropertyOptions;
        propertySelection.value = SelectedPropertyIndex;
        propertySelection.onValueChanged.Invoke(SelectedPropertyIndex);
    }

    public void SetContentText()
    {
        string propertyText = string.Empty;
        PropertyDefValuePair property;
        if (SelectedCardModel != null && SelectedCardModel.Card.Properties.TryGetValue(SelectedPropertyName, out property)) {
            propertyText = property.Value.Value;
            int enumValue;
            if (property.Def.Type == PropertyType.Enum && EnumDef.TryParse(propertyText, out enumValue)) {
                EnumDef enumDef = CardGameManager.Current.Enums.Where((def) => def.Property.Equals(SelectedPropertyName)).First();
                if (enumDef != null)
                    propertyText = enumDef.GetStringFromFlags(enumValue);
            }
        }
        contentText.text = propertyText;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        IsVisible = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        IsVisible = false;
    }

    public void ShowCardZoomed()
    {
        CardZoomPanel.gameObject.SetActive(true);
        CardZoomPanel.GetChild(0).GetComponent<Image>().sprite = cardImage.sprite;
    }

    void Update()
    {
        if (SelectedCardModel == null)
            IsVisible = false;

        // TODO: CONFIRM THAT THIS METHOD OF HIDING AND SHOWING DOESN'T CAUSE TOO MUCH OF A PERFORMANCE IMPACT
        Vector2 targetAnchorMin = new Vector2(rectTransform.anchorMin.x, Mathf.Lerp(rectTransform.anchorMin.y, HiddenYmin, animationSpeed * Time.deltaTime));
        Vector2 targetAnchorMax = new Vector2(rectTransform.anchorMax.x, Mathf.Lerp(rectTransform.anchorMax.y, HiddenYMax, animationSpeed * Time.deltaTime));
        if (IsVisible) {
            targetAnchorMin = new Vector2(rectTransform.anchorMin.x, Mathf.Lerp(rectTransform.anchorMin.y, VisibleYMin, animationSpeed * Time.deltaTime));
            targetAnchorMax = new Vector2(rectTransform.anchorMax.x, Mathf.Lerp(rectTransform.anchorMax.y, VisibleYMax, animationSpeed * Time.deltaTime));
        }
        rectTransform.anchorMin = targetAnchorMin;
        rectTransform.anchorMax = targetAnchorMax;
    }

    public static CardInfoViewer Instance {
        get {
            if (_instance == null)
                _instance = GameObject.FindWithTag(GameObjectTag).GetOrAddComponent<CardInfoViewer>();
            return _instance;
        }
    }

    public RectTransform rectTransform {
        get {
            return this.transform as RectTransform;
        }
    }

    public RectTransform CardZoomPanel {
        get {
            if (_cardZoomPanel == null) {
                _cardZoomPanel = Instantiate(cardZoomPrefab, this.gameObject.FindInParents<Canvas>().transform).transform as RectTransform;
                _cardZoomPanel.gameObject.SetActive(false);
            }
            return _cardZoomPanel;
        }
    }

    public List<Dropdown.OptionData> PropertyOptions {
        get {
            if (_propertyOptions == null)
                _propertyOptions = new List<Dropdown.OptionData>();
            return _propertyOptions;
        }
    }

    public int SelectedPropertyIndex {
        get {
            return _selectedPropertyIndex;
        }
        set {
            if (value < 0 || value >= PropertyOptions.Count)
                return;

            _selectedPropertyIndex = value;
            labelText.text = SelectedPropertyName;
            SetContentText();
        }
    }

    public string SelectedPropertyName {
        get {
            string selectedName = string.Empty;
            if (SelectedPropertyIndex >= 0 && SelectedPropertyIndex < PropertyOptions.Count)
                selectedName = PropertyOptions [SelectedPropertyIndex].text;
            return selectedName;
        } 
    }

    public CardModel SelectedCardModel {
        get {
            return _selectedCardModel;
        }
        set {
            if (_selectedCardModel != null)
                _selectedCardModel.UnHighlight();
            
            _selectedCardModel = value;

            if (_selectedCardModel == null) {
                IsVisible = false;
                return;
            }
            cardImage.sprite = _selectedCardModel.Image.sprite;
            nameText.text = value.Card.Name;
            idText.text = value.Card.Id;
            SetContentText();

            IsVisible = true;
        }
    }

    public bool IsVisible {
        get {
            return _isVisible;
        }
        set {
            _isVisible = value;
            if (SelectedCardModel != null) {
                if (_isVisible)
                    SelectedCardModel.Highlight();
                else
                    SelectedCardModel.UnHighlight();
            }
        }
    }
}
