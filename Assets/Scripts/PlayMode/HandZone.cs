using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HandZone : MonoBehaviour, IDropHandler
{
    public RectTransform handExtension;
    public Text handCountText;

    public bool IsExtended { get; private set; }

    void Start()
    {
        handExtension.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ShowCard);
        handExtension.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(CardModel.ResetRotation);
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

            CardModel newCardModel = cardModel.Clone(handExtension);
            newCardModel.DoubleClickEvent = CardModel.ToggleFacedown;
            newCardModel.SecondaryDragAction = null;
            newCardModel.CanvasGroup.blocksRaycasts = true;
        }
    }

    void Update()
    {
        handCountText.text = handExtension.childCount.ToString();
    }

    public void ToggleExtension()
    {
        IsExtended = !IsExtended;
        handExtension.gameObject.GetOrAddComponent<CanvasGroup>().alpha = IsExtended ? 1 : 0;
        handExtension.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = IsExtended;
    }
}
