using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HandZone : MonoBehaviour, IDropHandler
{
    public RectTransform handExtended;
    public Text handCountText;

    public bool Extended { get; private set; }

    void Start()
    {
        handExtended.gameObject.GetOrAddComponent<CardStack>().OnCardDropActions.Add(CardModel.ShowCard);
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

            CardModel newCardModel = cardModel.Clone(handExtended);
            newCardModel.DoubleClickEvent = CardModel.ToggleFacedown;
            newCardModel.CanvasGroup.blocksRaycasts = true;
        }
    }

    void Update()
    {
        handCountText.text = handExtended.childCount.ToString();
    }

    public void ToggleExtended()
    {
        Extended = !Extended;
        handExtended.gameObject.GetOrAddComponent<CanvasGroup>().alpha = Extended ? 1 : 0;
        handExtended.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = Extended;
    }
}
