using UnityEngine;
using UnityEngine.EventSystems;

public class CardInfoViewerSelectable : MonoBehaviour, IPointerDownHandler, ISelectHandler, IDeselectHandler
{
    public bool ignoreDeselect = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (CardInfoViewer.Instance.WasVisible)
            CardInfoViewer.Instance.IsVisible = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (ignoreDeselect)
            return;

        if (!CardInfoViewer.Instance.zoomPanel.gameObject.activeSelf)
            CardInfoViewer.Instance.IsVisible = false;
    }
}
