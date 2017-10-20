using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// HACK: THIS CLASS EXISTS TO DEAL WITH WONKINESS WITH SELECTING THINGS ON THE CARD INFO VIEWER
// TODO: FIND A SOLUTION THAT DOES NOT REQUIRE HAVING THIS HACK
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
