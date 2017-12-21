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
        selectionContent.GetComponent<ToggleGroup>().SetAllTogglesOff();
        selectionContent.DestroyAllChildren();
        // selectionTemplate.SetParent(selectionContent);

        Vector3 pos = selectionTemplate.localPosition;
        pos.y = 0;
        float i = 0;
        float index = 0;
        foreach (string option in options) {
            GameObject selection = Instantiate(selectionTemplate.gameObject, selectionContent);
            selection.SetActive(true);
            gameSelection.transform.localScale = Vector3.one;
            gameSelection.transform.localPosition = pos;
            gameSelection.GetComponentInChildren<Text>().text = gameName;
            Toggle toggle = gameSelection.GetComponent<Toggle>();
            toggle.isOn = gameName.Equals(CardGameManager.CurrentGameName);
            if (toggle.isOn)
                index = i;
            UnityAction<bool> valueChange = isOn => SelectGame(isOn, gameName);
            toggle.onValueChanged.AddListener(valueChange);
            pos.y -= gameSelectionTemplate.rect.height;
            i++;
        }

        gameSelectionTemplate.SetParent(gameSelectionArea.parent);
        gameSelectionTemplate.gameObject.SetActive(CardGameManager.Instance.AllCardGames.Count < 1);
        gameSelectionArea.sizeDelta = new Vector2(gameSelectionArea.sizeDelta.x, gameSelectionTemplate.rect.height * CardGameManager.Instance.AllCardGames.Count);

        float newSpot = gameSelectionTemplate.GetComponent<RectTransform>().rect.height 
            * (index + ((index < CardGameManager.Instance.AllCardGames.Keys.Count / 2f) ? 0f : 1f)) / gameSelectionArea.sizeDelta.y;
        StartCoroutine(WaitToMoveScrollbar(1 - Mathf.Clamp01(newSpot)));
    }

    public IEnumerator WaitToMoveScrollbar(float scrollBarValue)
    {
        yield return null;
        scrollBar.value = Mathf.Clamp01(scrollBarValue);
    }
}
