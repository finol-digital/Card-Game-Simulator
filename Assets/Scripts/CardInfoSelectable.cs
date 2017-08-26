using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInfoSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public bool checkDeselect = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Clicked on and therefore selecting Card info component " + gameObject.name);
        EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("Selected Card info component " + gameObject.name);
        CardInfoViewer.Instance.ShowCardInfo();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!checkDeselect) {
            Debug.Log("Deselected " + gameObject.name + ", but it does not check deselect, so ignoring");
            return;
        }

        Debug.Log("Deselected Card info component " + gameObject.name);
        CardInfoViewer.Instance.HideCardInfo();
    }
    
}
