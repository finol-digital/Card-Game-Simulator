using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
public class CardInfoSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public bool checkDeselect = false;

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
        if (!checkDeselect) {
            return;
        }

        CardInfoViewer.Instance.IsVisible = false;
    }
    
}
