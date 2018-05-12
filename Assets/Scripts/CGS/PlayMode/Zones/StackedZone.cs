using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using UnityEngine;

[RequireComponent(typeof(CardStack))]
public class StackedZone : ExtensibleCardZone
{
    public bool IsFaceup { get; set; }

    public override IReadOnlyList<Card> Cards => CardModels.Select(cardModel => cardModel.Value).ToList();
    protected List<CardModel> CardModels { get; } = new List<CardModel>();

    protected CardStack ExtensionCardStack => _extensionCardStack ?? (_extensionCardStack = extensionContent.gameObject.GetOrAddComponent<CardStack>());
    private CardStack _extensionCardStack;

    protected CardStack ZoneCardStack => _zoneCardStack ?? (_zoneCardStack = GetComponent<CardStack>());
    private CardStack _zoneCardStack;

    protected CardDropZone DropZone => _dropZone ?? (_dropZone = gameObject.GetOrAddComponent<CardDropZone>());
    private CardDropZone _dropZone;

    public override void OnStart()
    {
        ExtensionCardStack.OnAddCardActions.Add(OnAddCardModel);
        ExtensionCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
        ZoneCardStack.OnAddCardActions.Add(OnAddCardModel);
        ZoneCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
    }

    public override void AddCard(Card card)
    {
        CardModel newCardModel = Instantiate(cardModelPrefab, IsExtended ? ExtensionCardStack.transform : ZoneCardStack.transform).GetOrAddComponent<CardModel>();
        newCardModel.Value = card;
        newCardModel.IsFacedown = !IsFaceup;
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
        if (cardStack == ExtensionCardStack) {
            int transformIndex = cardModel.transform.GetSiblingIndex();
            cardIndex = transformIndex >= 0 && transformIndex < CardModels.Count ? transformIndex : CardModels.Count;
        }
        CardModels.Insert(cardIndex, cardModel);
        UpdateCountText();
    }

    public override void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
    {
        CardModels.Remove(cardModel);
        UpdateCountText();
    }

    public override void Clear()
    {
        foreach (CardModel cardModel in CardModels)
            Destroy(cardModel.gameObject);
        CardModels.Clear();
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
        UpdateCountText();
        return card;
    }

    public override void Shuffle()
    {
        StopAllCoroutines();
        CardModels.Shuffle();
        Display();
        StartCoroutine(DisplayShuffle());
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

    public void Display()
    {
        Transform parent = ZoneCardStack.transform;
        if (IsExtended)
            parent = ExtensionCardStack.transform;

        int siblingIndex = IsExtended ? 0 : 3;
        foreach (CardModel cardModel in CardModels) {
            cardModel.transform.SetParent(parent);
            cardModel.IsFacedown = !IsExtended && !IsFaceup;
            if (IsExtended)
                continue;
            ((RectTransform)cardModel.transform).anchorMin = new Vector2(0.5f, 0.5f);
            ((RectTransform)cardModel.transform).anchorMax = new Vector2(0.5f, 0.5f);
            ((RectTransform)cardModel.transform).anchoredPosition = Vector2.zero;
            cardModel.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
    }
}
