using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInfoViewer : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public GameObject cardZoomPrefab;
    public Image cardImage;
    public Text nameContent;
    public Text idContent;
    public Dropdown propertySelection;
    public Text textLabel;
    public Text textContent;
    public float animationSpeed = 5.0f;

    private static CardInfoViewer instance;

    private RectTransform cardZoomPanel;
    private RectTransform rectTransform;
    private float targetYPos;
    private List<Dropdown.OptionData> propertyOptions;
    private int selectedPropertyIndex;
    private CardModel selectedCard;

    void Start()
    {
        Debug.Log("Card Info Viewer is initializing");
        cardZoomPanel = Instantiate(cardZoomPrefab, UnityExtensionMethods.FindInParents<Canvas>(this.gameObject).transform).transform as RectTransform;
        cardZoomPanel.gameObject.SetActive(false);
        rectTransform = this.transform as RectTransform;
        targetYPos = rectTransform.sizeDelta.y;
    }

    public void UpdatePropertyOptions()
    {
        Debug.Log("Card Info viewer is setting the property options");
        propertyOptions = new List<Dropdown.OptionData>();
        foreach (PropertyDef propDef in CardGameManager.Current.CardProperties) {
            if (propDef.Name.Equals(CardGameManager.Current.CardPrimaryProperty))
                selectedPropertyIndex = propertyOptions.Count;
            propertyOptions.Add(new Dropdown.OptionData() { text = propDef.Name });
        }
        propertySelection.options = propertyOptions;
        propertySelection.value = selectedPropertyIndex;
    }

    public void SelectProperty(int propertyIndex)
    {
        if (propertyIndex < 0 || propertyIndex >= propertyOptions.Count) {
            Debug.LogWarning("Attempted to select an invalid property for the card info viewer! Ignoring");
            return;
        }

        selectedPropertyIndex = propertyIndex;
        Debug.Log("Selected property: " + SelectedPropertyName);
        textLabel.text = SelectedPropertyName;

        if (selectedCard == null)
            return;
        if (selectedCard.RepresentedCard.Properties.ContainsKey(SelectedPropertyName))
            textContent.text = selectedCard.RepresentedCard.Properties [SelectedPropertyName].Value.Value;
        else
            Debug.LogWarning("Not updating card info text with property, since the card does not have that property!: " + SelectedPropertyName);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Clicked on and therefore selecting Card Info");
        EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("Selected Card Info");
        ShowCardInfo();
    }

    public void SelectCard(CardModel cardModelToSelect)
    {
        if (cardModelToSelect == null) {
            Debug.LogWarning("Attempted to select a null cardModel! Ignoring the request");
            return;
        }

        Debug.Log("Updating the card info view with info from the card: " + cardModelToSelect.gameObject.name);
        selectedCard = cardModelToSelect;

        Sprite sprite;
        CardImageRepository.TryGetCachedCardImage(cardModelToSelect.RepresentedCard, out sprite);
        cardImage.sprite = sprite;
        nameContent.text = cardModelToSelect.RepresentedCard.Name;
        idContent.text = cardModelToSelect.RepresentedCard.Id;

        string mainLabel = "";
        string mainText = "";
        PropertySet prop;
        if (cardModelToSelect.RepresentedCard.Properties.TryGetValue(SelectedPropertyName, out prop)) {
            mainLabel = SelectedPropertyName;
            mainText = prop.Value.Value;
        } else
            Debug.LogWarning("Selected a card that does not have the correct properties to display! Defaulting to blank");
        textLabel.text = mainLabel;
        textContent.text = mainText;

        ShowCardInfo();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Debug.Log("Deselected Card Info");
        DeselectCard();
    }

    public void DeselectCard()
    {
        if (selectedCard != null) { 
            Debug.Log("Card info viewer deselecting the selected card");
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(this.gameObject);
            selectedCard.UnHighlight();
        }

        HideCardInfo();
    }

    public void ShowCardInfo()
    {
        Debug.Log("Showing the card info view");
        if (selectedCard != null)
            selectedCard.Highlight();
        targetYPos = 0.0f;
    }

    public void HideCardInfo()
    {
        Debug.Log("Hiding the card info view");
        if (selectedCard != null)
            selectedCard.UnHighlight();
        targetYPos = rectTransform.sizeDelta.y;
    }

    public void ShowCardZoomed()
    {
        Debug.Log("Showing zoomed image of card");
        cardZoomPanel.gameObject.SetActive(true);
        cardZoomPanel.GetChild(0).GetComponent<Image>().sprite = cardImage.sprite;
    }

    void Update()
    {
        float newYPos = Mathf.Lerp(rectTransform.anchoredPosition.y, targetYPos, animationSpeed * Time.deltaTime);
        rectTransform.anchoredPosition = new Vector2(0, newYPos);
    }

    public static CardInfoViewer Instance {
        get {
            if (instance == null)
                instance = GameObject.FindWithTag("CardInfo").transform.GetOrAddComponent<CardInfoViewer>();
            return instance;
        }
    }

    public string SelectedPropertyName {
        get {
            string selectedName = "";
            if (selectedPropertyIndex >= 0 && selectedPropertyIndex < propertyOptions.Count)
                selectedName = propertyOptions [selectedPropertyIndex].text;
            return selectedName;
        } 
    }

    public bool IsVisible { 
        get {
            return rectTransform.anchoredPosition.y < rectTransform.sizeDelta.y / 2.0f;
        } 
    }
}
