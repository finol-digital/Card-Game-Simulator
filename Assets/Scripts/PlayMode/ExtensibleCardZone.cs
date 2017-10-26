using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CardDropZone))]
public class ExtensibleCardZone : MonoBehaviour, ICardDropHandler
{
    public GameObject cardPrefab;
    public RectTransform extension;
    public RectTransform content;
    public Text labelText;
    public Text countText;

    public bool IsExtended { get; private set; }

    void Start()
    {
        content.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ShowCard);
        content.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ResetRotation);
        GetComponent<CardDropZone>().dropHandler = this;
        extension.gameObject.GetOrAddComponent<CardDropZone>().dropHandler = this;
    }

    public void AddCard(Card card)
    {
        CardModel newCardModel = Instantiate(cardPrefab, content).GetOrAddComponent<CardModel>();
        newCardModel.Card = card;
        newCardModel.DoubleClickEvent = CardModel.ToggleFacedown;
        newCardModel.SecondaryDragAction = null;
        newCardModel.CanvasGroup.blocksRaycasts = true;
    }

    void Update()
    {
        countText.text = content.childCount.ToString();
    }

    public void ToggleExtension()
    {
        IsExtended = !IsExtended;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().alpha = IsExtended ? 1 : 0;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = IsExtended;
    }
}
