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
        contentText.text = string.Empty;
        if (SelectedCardModel != null)
            contentText.text = SelectedCardModel.Card.GetPropertyValueString(SelectedPropertyName);
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

        RectTransform rectTransform = this.transform as RectTransform;
        rectTransform.anchorMin = IsVisible ? 
            new Vector2(rectTransform.anchorMin.x, Mathf.Lerp(rectTransform.anchorMin.y, VisibleYMin, animationSpeed * Time.deltaTime)) :
            new Vector2(rectTransform.anchorMin.x, Mathf.Lerp(rectTransform.anchorMin.y, HiddenYmin, animationSpeed * Time.deltaTime));
        rectTransform.anchorMax = IsVisible ? 
            new Vector2(rectTransform.anchorMax.x, Mathf.Lerp(rectTransform.anchorMax.y, VisibleYMax, animationSpeed * Time.deltaTime)) :
            new Vector2(rectTransform.anchorMax.x, Mathf.Lerp(rectTransform.anchorMax.y, HiddenYMax, animationSpeed * Time.deltaTime));
    }

    public static CardInfoViewer Instance {
        get {
            if (_instance == null) {
                GameObject cardInfoViwer = GameObject.FindWithTag(GameObjectTag);
                if (cardInfoViwer != null)
                    _instance = cardInfoViwer.GetOrAddComponent<CardInfoViewer>();
            }
            return _instance;
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
                _selectedCardModel.HideHighlight();
            
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
            if (!_isVisible)
                CardZoomPanel.gameObject.SetActive(false);
            if (SelectedCardModel != null) {
                if (_isVisible)
                    SelectedCardModel.ShowHighlight();
                else
                    SelectedCardModel.HideHighlight();
            }
        }
    }
}
