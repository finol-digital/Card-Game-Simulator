using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public delegate void OnDropDelegate(CardStack cardStack,CardModel cardModel);

public enum CardStackType
{
    Full,
    Vertical,
    Horizontal,
    Bounds
}

public class CardStack : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    public CardStackType type;

    private List<OnDropDelegate> _cardAddedActions;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            CardModel draggedCardModel;
            if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
                draggedCardModel.PlaceHolderStack = this;
            else
                cardModel.PlaceHolderStack = this;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            CardModel draggedCardModel;
            if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
                draggedCardModel.PlaceHolderStack = null;
            else
                cardModel.PlaceHolderStack = null;
        }
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
            foreach (OnDropDelegate cardAddAction in CardAddedActions)
                cardAddAction(this, cardModel);
        }
    }

    public List<OnDropDelegate> CardAddedActions {
        get {
            if (_cardAddedActions == null)
                _cardAddedActions = new List<OnDropDelegate>();
            return _cardAddedActions;
        }
    }
}
