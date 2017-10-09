using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckZone : MonoBehaviour, IDropHandler
{
    public GameObject cardPrefab;

    private Deck _deck;

    void Start()
    {
        GetComponent<CardStack>().OnAddCardActions.Add(CardModel.HideCard);
        GetComponent<CardStack>().OnAddCardActions.Add(CardModel.ResetRotation);
    }

    public void Shuffle(Vector2 unused1, Vector2 unused2)
    {
        Deck = new Deck(Deck.Name, Deck.ToTxt());
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            CardModel draggedCardModel;
            if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
                cardModel = draggedCardModel;
            cardModel.DoubleClickEvent = CardModel.ToggleFacedown;
            cardModel.SecondaryDragAction = Shuffle;
        }
    }

    public Deck Deck {
        get {
            return _deck;
        }
        set {
            _deck = value;

            if (_deck == null || _deck.Cards.Count < 1)
                return;

            _deck.Cards.Shuffle();
            this.transform.DestroyAllChildren();
            foreach (Card card in _deck.Cards) {
                CardModel newCard = Instantiate(cardPrefab, this.transform).GetOrAddComponent<CardModel>();
                newCard.Card = card;
                newCard.IsFacedown = true;
                newCard.DoubleClickEvent = CardModel.ToggleFacedown;
                newCard.SecondaryDragAction = Shuffle;
            }
        }
    }
}
