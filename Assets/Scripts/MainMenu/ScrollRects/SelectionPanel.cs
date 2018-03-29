using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void OnSelectDelegate (bool isOn, string selection);

public class SelectionPanel : MonoBehaviour
{
    public RectTransform selectionContent;
    public RectTransform selectionTemplate;
    public Scrollbar scrollBar;

    public void Rebuild(List<string> options, OnSelectDelegate valueChange, string currentValue = "")
    {
        if (options == null)
            options = new List<string>();
        if (valueChange == null)
            valueChange = delegate {};
        if (currentValue == null)
            currentValue = string.Empty;

        selectionContent.DestroyAllChildren();
        selectionContent.sizeDelta = new Vector2(selectionContent.sizeDelta.x, selectionTemplate.rect.height * options.Count);

        foreach (string option in options) {
            GameObject selection = Instantiate(selectionTemplate.gameObject, selectionContent);
            selection.SetActive(true);
            selection.transform.localScale = Vector3.one;
            selection.GetComponentInChildren<Text>().text = option;
            selection.GetComponent<Toggle>().interactable = true;
            selection.GetComponent<Toggle>().isOn = option.Equals(currentValue);
            selection.GetComponent<Toggle>().onValueChanged.AddListener(isOn => valueChange(isOn, option));
        }

        selectionTemplate.gameObject.SetActive(options.Count < 1);
        selectionTemplate.GetComponent<Toggle>().isOn = options.Count > 0;

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

    public void ScrollPage(bool scrollUp)
    {
        scrollBar.value = Mathf.Clamp01(scrollBar.value + (scrollUp ? 0.1f : -0.1f));
    }
}
