using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SelectionPanel : MonoBehaviour
{
    public RectTransform selectionContent;
    public RectTransform selectionTemplate;
    public Scrollbar scrollBar;
    
    public void Rebuild(List<string> options, UnityAction<bool> valueChange, string currentValue = "")
    {
        if (options == null)
            options = new List<string>();
        if (valueChange == null)
            valueChange = new UnityAction<bool>();
        if (currentValue == null)
            currentValue = string.Empty;

        selectionContent.GetComponent<ToggleGroup>().SetAllTogglesOff();
        selectionContent.DestroyAllChildren();
        selectionContent.sizeDelta = new Vector2(selectionContent.sizeDelta.x, selectionTemplate.rect.height * options.Count);

        Vector2 pos = Vector2.zero;
        foreach (string option in options) {
            GameObject selection = Instantiate(selectionTemplate.gameObject, selectionContent);
            selection.SetActive(true);
            selection.transform.localScale = Vector3.one;
            selection.transform.localPosition = pos;
            selection.GetComponentInChildren<Text>().text = option;
            selection.GetComponent<Toggle>().isOn = option.Equals(currentValue);
            selection.GetComponent<Toggle>().onValueChanged.AddListener(valueChange);
            pos.y -= selectionTemplate.rect.height;
        }
        
        selectionTemplate.gameObject.SetActive(options.Count < 1);
        float index = options.IndexOf(currentValue);
        if (index < 0)
            return;
        
        float newSpot = selectionTemplate.GetComponent<RectTransform>().rect.height 
            * (index + ((index < options.Count / 2f) ? 0f : 1f)) / selectionContent.sizeDelta.y;
        StartCoroutine(WaitToMoveScrollbar(1 - Mathf.Clamp01(newSpot)));
    }

    public IEnumerator WaitToMoveScrollbar(float scrollBarValue)
    {
        yield return null;
        scrollBar.value = Mathf.Clamp01(scrollBarValue);
    }
}
