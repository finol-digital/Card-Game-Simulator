using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CardStack))]
public class StackedZone : ExtensibleCardZone
{
    public int Count => CardModels.Count;

    public CardDropZone DropZone { get; private set; }
    public CardStack ZoneCardStack { get; private set; }
    public CardStack ExtensionCardStack { get; private set; }

    protected List<CardModel> CardModels { get; } = new List<CardModel>();

    public bool isFaceup;

    public override void OnStart()
    {
        DropZone = gameObject.GetOrAddComponent<CardDropZone>();

        ZoneCardStack = GetComponent<CardStack>();
        ZoneCardStack.OnAddCardActions.Add(OnAddCardModel);
        ZoneCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);

        ExtensionCardStack = extensionContent.GetComponent<CardStack>();
        ExtensionCardStack.OnAddCardActions.Add(OnAddCardModel);
        ExtensionCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
    }

    public override void AddCard(Card card)
    {
        CardModel newCardModel = Instantiate(cardModelPrefab, IsExtended ? ExtensionCardStack.transform : ZoneCardStack.transform).GetOrAddComponent<CardModel>();
        newCardModel.Value = card;
        newCardModel.IsFacedown = !isFaceup;
        OnAddCardModel(IsExtended ? ExtensionCardStack : ZoneCardStack, newCardModel);
    }

    public override void OnAddCardModel(CardStack cardStack, CardModel cardModel)
    {
        if (cardStack == null || cardModel == null)
            return;

        cardModel.transform.rotation = Quaternion.identity;
        cardModel.DoubleClickAction = ToggleExtension;
        cardModel.SecondaryDragAction = Shuffle;
        if (IsExtended)
            cardModel.IsFacedown = false;

        int cardIndex = CardModels.Count;
        if (cardStack == ExtensionCardStack)
            cardIndex = cardModel.transform.GetSiblingIndex();
        CardModels.Insert(cardIndex, cardModel);
        UpdateCountText();
    }

    public override void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
    {
        CardModels.Remove(cardModel);
        UpdateCountText();
    }

    public Card PopCard()
    {
        if (CardModels.Count < 1)
            return Card.Blank;

        CardModel cardModel = CardModels [CardModels.Count - 1];
        Card card = cardModel.Value;
        CardModels.Remove(cardModel);
        Destroy(cardModel.gameObject);
        return card;
    }

    public void ToggleExtension(CardModel cardModel)
    {
        ToggleExtension();
    }

    public override void ToggleExtension()
    {
        base.ToggleExtension();
        DropZone.dropHandler = IsExtended ? this : null;
        ZoneCardStack.enabled = !IsExtended;
        Display();
    }

    public void Shuffle()
    {
        CardModels.Shuffle();
        Display();
    }

    public void Display()
    {
        Transform parent = ZoneCardStack.transform;
        if (IsExtended)
            parent = ExtensionCardStack.transform;

        int siblingIndex = IsExtended ? 0 : 3;
        foreach (CardModel cardModel in CardModels) {
            cardModel.transform.SetParent(parent);
            cardModel.IsFacedown = !IsExtended && !isFaceup;
            if (IsExtended)
                continue;
            ((RectTransform)cardModel.transform).anchorMin = new Vector2(0.5f, 0.5f);
            ((RectTransform)cardModel.transform).anchorMax = new Vector2(0.5f, 0.5f);
            ((RectTransform)cardModel.transform).anchoredPosition = Vector2.zero;
            cardModel.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
    }

    public override void UpdateCountText()
    {
        countText.text = CardModels.Count.ToString();
    }
}
