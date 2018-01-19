using UnityEngine;
using UnityEngine.EventSystems;

// HACK: THIS CLASS EXISTS TO DEAL WITH WONKINESS WITH SELECTING THINGS ON THE CARD INFO VIEWER
public class CardInfoViewerSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public bool ignoreDeselect = false;

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
        if (ignoreDeselect)
            return;

        CardInfoViewer.Instance.IsVisible = false;
    }
}
