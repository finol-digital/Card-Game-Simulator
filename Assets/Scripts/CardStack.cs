using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardStack : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    public OnDoubleClickDelegate ActionForCardOnDoubleClick { get; set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            cardModel.CreatePlaceHolderInPanel(this.transform as RectTransform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            cardModel.PlaceHolder = null;
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
                draggedCardModel.DoubleClickEvent = ActionForCardOnDoubleClick;
            else
                cardModel.DoubleClickEvent = ActionForCardOnDoubleClick;
        }
    }

}
