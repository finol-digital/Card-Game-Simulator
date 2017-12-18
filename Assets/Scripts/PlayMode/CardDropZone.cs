using UnityEngine;
using UnityEngine.EventSystems;

public interface ICardDropHandler
{
    void OnDrop(CardModel cardModel);
}

public class CardDropZone : MonoBehaviour, IDropHandler
{
    // HACK: UNITY CAN'T PASS INTERFACES THROUGH UI, SO THIS WILL NEED TO BE SET THROUGH CODE
    public ICardDropHandler dropHandler;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || dropHandler == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            CardModel draggedCardModel;
            if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
                cardModel = draggedCardModel;
            if (cardModel.PlaceHolder == null && cardModel.ParentCardStack == null)
                dropHandler.OnDrop(cardModel);
        }
    }

}
