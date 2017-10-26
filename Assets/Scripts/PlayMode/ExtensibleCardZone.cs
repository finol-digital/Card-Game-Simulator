using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ExtensibleCardZone : MonoBehaviour, ICardDropHandler
{
    public GameObject cardPrefab;
    public List<CardDropZone> cardDropZones;
    public RectTransform extension;
    public RectTransform extensionContent;
    public Text labelText;
    public Text countText;

    public bool IsExtended { get; private set; }

    void Start()
    {
        foreach (CardDropZone dropZone in cardDropZones)
            dropZone.dropHandler = this;
        extensionContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ShowCard);
        extensionContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ResetRotation);
        OnStart();
    }

    public virtual void OnStart()
    {
    }

    public virtual void AddCard(Card card)
    {
        CardModel newCardModel = Instantiate(cardPrefab, extensionContent).GetOrAddComponent<CardModel>();
        newCardModel.Value = card;
        newCardModel.DoubleClickEvent = CardModel.ToggleFacedown;
        newCardModel.SecondaryDragAction = null;
    }

    public void ToggleExtension()
    {
        IsExtended = !IsExtended;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().alpha = IsExtended ? 1 : 0;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = IsExtended;
    }

    void Update()
    {
        if (countText != null)
            countText.text = extensionContent.childCount.ToString();
    }
}
