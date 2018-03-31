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

    public bool DoesImmediatelyRelease { get; set; }

    public List<OnAddCardDelegate> OnAddCardActions { get; } = new List<OnAddCardDelegate>();
    public List<OnRemoveCardDelegate> OnRemoveCardActions { get; } = new List<OnRemoveCardDelegate>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        CardModel cardModel = CardModel.GetPointerDrag(eventData);
        if (cardModel != null && (type != CardStackType.Area || cardModel.transform.parent != transform))
            cardModel.PlaceHolderCardStack = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CardModel cardModel = CardModel.GetPointerDrag(eventData);
        if (cardModel != null && cardModel.PlaceHolderCardStack == this)
            cardModel.PlaceHolderCardStack = null;
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
        if (child == null)
            return;

        switch (type) {
            case CardStackType.Full:
                break;
            case CardStackType.Vertical:
            case CardStackType.Horizontal:
                int newSiblingIndex = transform.childCount;
                for (int i = 0; i < transform.childCount; i++) {
                    if (type == CardStackType.Vertical ? targetPosition.y < transform.GetChild(i).position.y : targetPosition.x > transform.GetChild(i).position.x)
                        continue;
                    newSiblingIndex = i;
                    if (child.GetSiblingIndex() < newSiblingIndex)
                        newSiblingIndex--;
                    break;
                }
                child.SetSiblingIndex(newSiblingIndex);
                break;
            case CardStackType.Area:
            default:
                child.position = targetPosition;
                break;
        }
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
}
