using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CardStack))]
public class DeckZone : ExtensibleCardZone
{
    public CardStack DeckCardStack { get; private set; }

    public CardStack ExtensionCardStack { get; private set; }

    private List<Card> _cards;

    public override void OnStart()
    {
        DeckCardStack = GetComponent<CardStack>();
        ExtensionCardStack = extensionContent.GetComponent<CardStack>();

        DeckCardStack.OnAddCardActions.Add(CardModel.ResetRotation);
        DeckCardStack.OnAddCardActions.Add(OnAddCardModel);
        DeckCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);

        ExtensionCardStack.OnAddCardActions.Remove(CardModel.ShowCard);
        ExtensionCardStack.OnAddCardActions.Add(OnAddCardModel);
        ExtensionCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
    }

    public void OnAddCardModel(CardStack cardStack, CardModel cardModel)
    {
        if (cardStack == null || cardModel == null)
            return;

        int cardIndex = Cards.Count;
        if (cardStack == ExtensionCardStack)
            cardIndex = cardModel.transform.GetSiblingIndex();
        
        cardModel.DoubleClickEvent = ToggleDeckExtension;
        cardModel.SecondaryDragAction = Shuffle;

        Cards.Insert(cardIndex, cardModel.Value);
    }

    public void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
    {
        if (cardStack == null || cardModel == null)
            return;

        int cardIndex = Cards.Count - 1;
        if (cardStack == ExtensionCardStack)
            cardIndex = cardModel.transform.GetSiblingIndex();
        
        if (Cards.Contains(cardModel.Value))
            Cards.RemoveAt(cardIndex);
    }

    public void ToggleDeckExtension(CardModel cardModel)
    {
        ToggleExtension();
        Display();
    }

    public void Shuffle(Vector2 unused, Vector2 unused2)
    {
        Shuffle();
    }

    public void Shuffle()
    {
        Cards.Shuffle();
        Display();
    }

    public void Display()
    {
        DeckCardStack.transform.DestroyAllChildren();
        ExtensionCardStack.transform.DestroyAllChildren();

        Transform parent = DeckCardStack.transform;
        if (IsExtended)
            parent = ExtensionCardStack.transform;
        
        foreach (Card card in Cards) {
            CardModel newCard = Instantiate(cardPrefab, parent).GetOrAddComponent<CardModel>();
            newCard.Value = card;
            newCard.IsFacedown = !IsExtended;
            newCard.DoubleClickEvent = ToggleDeckExtension;
            newCard.SecondaryDragAction = Shuffle;
        }
    }

    public List<Card> Cards {
        get {
            if (_cards == null)
                _cards = new List<Card>();
            return _cards;
        }
        set {
            _cards = new List<Card>(value);
            Display();
        }
    }
}
