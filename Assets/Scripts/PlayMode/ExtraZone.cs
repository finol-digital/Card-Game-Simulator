using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExtraZone : MonoBehaviour
{
    public GameObject cardPrefab;
    public RectTransform extension;
    public Text countText;

    public bool IsExtended { get; private set; }

    void Start()
    {
        extension.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ShowCard);
        extension.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ResetRotation);
    }

    public void AddCard(Card card)
    {
        CardModel newCardModel = Instantiate(cardPrefab, extension).GetOrAddComponent<CardModel>();
        newCardModel.Card = card;
        newCardModel.DoubleClickEvent = CardModel.ToggleFacedown;
        newCardModel.SecondaryDragAction = null;
        newCardModel.CanvasGroup.blocksRaycasts = true;
    }

    void Update()
    {
        countText.text = extension.childCount.ToString();
    }

    public void ToggleExtension()
    {
        IsExtended = !IsExtended;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().alpha = IsExtended ? 1 : 0;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = IsExtended;
    }
    
}
