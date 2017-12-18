using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckEditorScrollArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    public DeckEditor editor;
    public bool scrollsRight = false;
    public float scrollAmount = 0.01f;
    public float holdFrequency = 0.01f;

    private CanvasGroup _canvasGroup;

    void Start()
    {
        _canvasGroup = this.gameObject.GetOrAddComponent<CanvasGroup>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel != null && cardModel.ParentCardStack == null)
            StartCoroutine(MoveScrollbar());
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
        float scrollValue = Mathf.Clamp01(editor.scrollBar.value + scrollAmount * (scrollsRight ? 1 : -1));
		editor.CurrentCardStackIndex = Mathf.RoundToInt (scrollValue * editor.CardStackCount);
        editor.scrollBar.value = scrollValue;
        if (editor.scrollBar.value > 0 && editor.scrollBar.value < 1) {
            yield return new WaitForSeconds(holdFrequency);
            StartCoroutine(MoveScrollbar());
        }
    }

    void Update()
    {
        _canvasGroup.blocksRaycasts = (!scrollsRight && editor.scrollBar.value != 0) || (scrollsRight && editor.scrollBar.value != 1);
    }

}
