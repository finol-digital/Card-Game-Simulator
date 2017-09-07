using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInfoViewer : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public const float VisibleYMin = 0.625f;
    public const float VisibleYMax = 1;
    public const float HiddenYmin = 1;
    public const float HiddenYMax = 1.375f;

    public GameObject cardZoomPrefab;
    public Image cardImage;
    public Text nameContent;
    public Text idContent;
    public Dropdown propertySelection;
    public Text textLabel;
    public Text textContent;
    public float animationSpeed = 5.0f;

    private static CardInfoViewer _instance;

    private RectTransform _cardZoomPanel;
    private List<Dropdown.OptionData> _propertyOptions;
    private int _selectedPropertyIndex;
    private CardModel _selectedCardModel;
    public bool _isVisible;

    public void UpdatePropertyOptions()
    {
        Debug.Log("Card Info viewer is setting the property options");
        PropertyOptions.Clear();
        foreach (PropertyDef propDef in CardGameManager.Current.CardProperties) {
            if (propDef.Name.Equals(CardGameManager.Current.CardPrimaryProperty))
                SelectedPropertyIndex = PropertyOptions.Count;
            PropertyOptions.Add(new Dropdown.OptionData() { text = propDef.Name });
        }
        propertySelection.options = PropertyOptions;
        propertySelection.value = SelectedPropertyIndex;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Clicked on and therefore selecting Card Info");
        EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("Selected Card Info");
        IsVisible = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Debug.Log("Deselected Card Info");
        DeselectCard();
    }

    public void DeselectCard()
    {
        Debug.Log("Deselected Card in Info Viewer");
        if (SelectedCardModel != null && !EventSystem.current.alreadySelecting)
            EventSystem.current.SetSelectedGameObject(this.gameObject);
        IsVisible = false;
    }

    public void ShowCardZoomed()
    {
        Debug.Log("Showing zoomed image of card");
        CardZoomPanel.gameObject.SetActive(true);
        CardZoomPanel.GetChild(0).GetComponent<Image>().sprite = cardImage.sprite;
    }

    void Update()
    {
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
                _instance = GameObject.FindWithTag("CardInfo").transform.GetOrAddComponent<CardInfoViewer>();
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
                _cardZoomPanel = Instantiate(cardZoomPrefab, UnityExtensionMethods.FindInParents<Canvas>(this.gameObject).transform).transform as RectTransform;
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
            textLabel.text = SelectedPropertyName;
            if (SelectedCardModel != null && SelectedCardModel.RepresentedCard.Properties.ContainsKey(SelectedPropertyName))
                textContent.text = SelectedCardModel.RepresentedCard.Properties [SelectedPropertyName].Value.Value;
        }
    }

    public string SelectedPropertyName {
        get {
            string selectedName = "";
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
            if (value == null)
                return;

            Sprite sprite;
            CardImageRepository.TryGetCachedCardImage(value.RepresentedCard, out sprite);
            cardImage.sprite = sprite;
            nameContent.text = value.RepresentedCard.Name;
            idContent.text = value.RepresentedCard.Id;

            PropertySet prop;
            if (value.RepresentedCard.Properties.TryGetValue(SelectedPropertyName, out prop))
                textContent.text = prop.Value.Value;
            else
                textContent.text = "";

            _selectedCardModel = value;
            IsVisible = true;
        }
    }

    public bool IsVisible {
        get {
            return _isVisible;
        }
        set {
            if (SelectedCardModel != null) {
                if (value)
                    SelectedCardModel.Highlight();
                else
                    SelectedCardModel.UnHighlight();
            }
            _isVisible = value;
        }
    }
}
