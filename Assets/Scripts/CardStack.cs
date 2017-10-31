using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public delegate void OnAddCardDelegate(CardStack cardStack,CardModel cardModel);
public delegate void OnRemoveCardDelegate(CardStack cardStack,CardModel cardModel);

public enum CardStackType
{
    Full,
    Vertical,
    Horizontal,
    Area
}

public class CardStack : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardStackType type;
    public ScrollRect scrollRectContainer;

    private List<OnAddCardDelegate> _cardAddedActions;
    private List<OnRemoveCardDelegate> _cardRemovedActions;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            CardModel draggedCardModel;
            if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
                cardModel = draggedCardModel;
            if (cardModel.ParentCardStack == null || cardModel.ParentCardStack.type == CardStackType.Horizontal)
                cardModel.PlaceHolderCardStack = this;
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
            if (cardModel.PlaceHolderCardStack == this)
                cardModel.PlaceHolderCardStack = null;
        }
    }

    public void OnAdd(CardModel cardModel)
    {
        if (cardModel == null)
            return;
        
        foreach (OnAddCardDelegate cardAddAction in OnAddCardActions)
            cardAddAction(this, cardModel);
    }

    public void OnRemove(CardModel cardModel)
    {
        if (cardModel == null)
            return;
        
        foreach (OnRemoveCardDelegate cardRemoveAction in OnRemoveCardActions)
            cardRemoveAction(this, cardModel);
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

    public void UpdateScrollRect(DragPhase dragPhase, PointerEventData eventData)
    {
        if (scrollRectContainer == null)
            return;
        
        switch (dragPhase) {
            case DragPhase.Begin:
                scrollRectContainer.OnBeginDrag(eventData);
                break;
            case DragPhase.Drag:
                scrollRectContainer.OnDrag(eventData);
                break;
            case DragPhase.End:
            default:
                scrollRectContainer.OnEndDrag(eventData);
                break;
        }

    }

    public List<OnAddCardDelegate> OnAddCardActions {
        get {
            if (_cardAddedActions == null)
                _cardAddedActions = new List<OnAddCardDelegate>();
            return _cardAddedActions;
        }
    }

    public List<OnRemoveCardDelegate> OnRemoveCardActions {
        get {
            if (_cardRemovedActions == null)
                _cardRemovedActions = new List<OnRemoveCardDelegate>();
            return _cardRemovedActions;
        }
    }
}
