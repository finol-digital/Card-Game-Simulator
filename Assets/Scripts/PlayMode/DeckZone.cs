using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CardStack))]
public class DeckZone : MonoBehaviour
{
    public GameObject cardPrefab;

    private List<Card> _cards;

    void Start()
    {
        GetComponent<CardStack>().OnAddCardActions.Add(CardModel.ResetRotation);
        GetComponent<CardStack>().OnAddCardActions.Add(OnAddCardModel);
        GetComponent<CardStack>().OnRemoveCardActions.Add(OnRemoveCardModel);
    }

    public void OnAddCardModel(CardStack unused, CardModel cardModel)
    {
        if (cardModel == null)
            return;

        cardModel.DoubleClickEvent = CardModel.ToggleFacedown;
        cardModel.SecondaryDragAction = Shuffle;
        Cards.Add(cardModel.Card);
    }

    public void OnRemoveCardModel(CardStack unused, CardModel cardModel)
    {
        if (cardModel == null)
            return;
        
        if (Cards.Contains(cardModel.Card))
            Cards.RemoveAt(Cards.LastIndexOf(cardModel.Card));
    }

    public void Display()
    {
        this.transform.DestroyAllChildren();

        foreach (Card card in Cards) {
            CardModel newCard = Instantiate(cardPrefab, this.transform).GetOrAddComponent<CardModel>();
            newCard.Card = card;
            newCard.IsFacedown = true;
            newCard.DoubleClickEvent = CardModel.ToggleFacedown;
            newCard.SecondaryDragAction = Shuffle;
        }
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
