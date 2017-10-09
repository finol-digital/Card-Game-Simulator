using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInfoSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public bool checksDeselect = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        CardInfoViewer.Instance.IsVisible = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!checksDeselect) {
            return;
        }

        CardInfoViewer.Instance.IsVisible = false;
    }
    
}
