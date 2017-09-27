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
    Area
}

public class CardStack : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    public CardStackType type;
    public ScrollRect container;
    public bool free;

    private List<OnDropDelegate> _cardAddedActions;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            CardModel draggedCardModel;
            if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
                cardModel = draggedCardModel;
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
                cardModel = draggedCardModel;
            if (cardModel.PlaceHolderStack == this)
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
            foreach (OnDropDelegate cardDropAction in OnCardDropActions)
                cardDropAction(this, cardModel);
        }
    }

    public void UpdateLayout(RectTransform child, Vector2 targetPosition)
    {
        if (child == null || this.type == CardStackType.Full)
            return;

        if (this.type == CardStackType.Vertical || this.type == CardStackType.Horizontal) {
            child.gameObject.GetOrAddComponent<LayoutElement>().ignoreLayout = false;
            int newSiblingIndex = this.transform.childCount;
            for (int i = 0; i < this.transform.childCount; i++) {
                bool goesBelow = targetPosition.y > this.transform.GetChild(i).position.y;
                if (this.type == CardStackType.Horizontal)
                    goesBelow = targetPosition.x < this.transform.GetChild(i).position.x;
                if (goesBelow) {
                    newSiblingIndex = i;
                    if (child.GetSiblingIndex() < newSiblingIndex)
                        newSiblingIndex--;
                    break;
                }
            }
            child.SetSiblingIndex(newSiblingIndex);
        } else if (this.type == CardStackType.Area)
            child.position = targetPosition;
    }

    public List<OnDropDelegate> OnCardDropActions {
        get {
            if (_cardAddedActions == null)
                _cardAddedActions = new List<OnDropDelegate>();
            return _cardAddedActions;
        }
    }
}
