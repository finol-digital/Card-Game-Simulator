using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckEditorScrollArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    public Scrollbar scrollBar;
    public bool scrollsRight = false;
    public float scrollAmount = 0.01f;
    public float holdFrequency = 0.01f;

    private CanvasGroup _canvasGroup;

    void Start()
    {
        _canvasGroup = transform.GetOrAddComponent<CanvasGroup>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null) {
            StartCoroutine(MoveScrollbar());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopAllCoroutines();
    }

    IEnumerator MoveScrollbar()
    {
        float scrollValue = Mathf.Clamp01(scrollBar.value + scrollAmount * (scrollsRight ? 1 : -1));
        scrollBar.value = scrollValue;
        if (scrollBar.value > 0 && scrollBar.value < 1) {
            yield return new WaitForSeconds(holdFrequency);
            StartCoroutine(MoveScrollbar());
        }
    }

    void Update()
    {
        _canvasGroup.blocksRaycasts = (!scrollsRight && scrollBar.value != 0) || (scrollsRight && scrollBar.value != 1);
    }

}
