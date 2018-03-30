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

    protected List<GameObject> Toggles { get; } = new List<GameObject>();

    public void Rebuild(List<string> options, OnSelectDelegate valueChange, string currentValue = "")
    {
        if (options == null)
            options = new List<string>();
        if (valueChange == null)
            valueChange = delegate {};
        if (currentValue == null)
            currentValue = string.Empty;

        Toggles.Clear();
        selectionContent.DestroyAllChildren();
        selectionContent.sizeDelta = new Vector2(selectionContent.sizeDelta.x, selectionTemplate.rect.height * options.Count);

        foreach (string option in options) {
            Toggle toggle = Instantiate(selectionTemplate.gameObject, selectionContent).GetOrAddComponent<Toggle>();
            toggle.gameObject.SetActive(true);
            toggle.transform.localScale = Vector3.one;
            toggle.GetComponentInChildren<Text>().text = option;
            toggle.interactable = true;
            toggle.isOn = option.Equals(currentValue);
            toggle.onValueChanged.AddListener(isOn => valueChange(isOn, option));
            Toggles.Add(toggle.gameObject);
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

    public void ScrollToggles(bool scrollUp)
    {

    }
}
