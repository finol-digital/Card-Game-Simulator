using System.Collections.Generic;
using CardGameDef.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace ScrollRects
{
    public delegate void OnSelectDelegate<in T>(Toggle toggle, T selection);

    public class SelectionPanel : MonoBehaviour
    {
        public RectTransform selectionContent;
        public RectTransform selectionTemplate;
        public Text emptyText;
        public ScrollRect scrollRect;

        protected List<Toggle> Toggles { get; } = new List<Toggle>();

        protected void Rebuild<TKey, TValue>(IDictionary<TKey, TValue> options, OnSelectDelegate<TKey> select,
            TKey current)
        {
            Toggles.Clear();
            selectionContent.DestroyAllChildren();
            selectionContent.sizeDelta =
                new Vector2(selectionContent.sizeDelta.x, selectionTemplate.rect.height * options.Count);

            int currentSelectionIndex = -1;
            var i = 0;
            foreach (KeyValuePair<TKey, TValue> option in options)
            {
                var toggle = Instantiate(selectionTemplate.gameObject, selectionContent).GetOrAddComponent<Toggle>();
                toggle.gameObject.SetActive(true);
                toggle.transform.localScale = Vector3.one;
                toggle.GetComponentInChildren<Text>().text = option.Value.ToString();
                toggle.interactable = true;
                toggle.isOn = option.Key.Equals(current);
                toggle.onValueChanged.AddListener(isOn => select(toggle, option.Key));
                Toggles.Add(toggle);
                if (toggle.isOn)
                    currentSelectionIndex = i;
                i++;
            }

            if (emptyText == null)
            {
                selectionTemplate.gameObject.SetActive(options.Count < 1);
                selectionTemplate.GetComponent<Toggle>().isOn = options.Count > 0;
            }
            else
            {
                selectionTemplate.gameObject.SetActive(false);
                emptyText.gameObject.SetActive(options.Count < 1);
            }

            if (currentSelectionIndex > 0 && currentSelectionIndex < options.Count && options.Count > 1)
                scrollRect.verticalNormalizedPosition = 1f - (currentSelectionIndex / (options.Count - 1f));
        }

        protected void ScrollPage(bool scrollDown)
        {
            scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(scrollRect.verticalNormalizedPosition + (scrollDown ? -0.1f : 0.1f));
        }

        protected void SelectPrevious()
        {
            if (EventSystem.current.alreadySelecting || Toggles.Count < 1)
                return;

            if (!Toggles[0].group.AnyTogglesOn())
            {
                Toggles[0].isOn = true;
                if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(Toggles[0].gameObject);
                scrollRect.verticalNormalizedPosition = 1f;
                return;
            }

            for (int i = Toggles.Count - 1; i >= 0; i--)
            {
                if (!Toggles[i].isOn)
                    continue;
                i--;
                if (i < 0)
                    i = Toggles.Count - 1;
                Toggles[i].isOn = true;
                if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(Toggles[i].gameObject);
                scrollRect.verticalNormalizedPosition = 1f - (i / (Toggles.Count - 1f));
                return;
            }
        }

        protected void SelectNext()
        {
            if (EventSystem.current.alreadySelecting || Toggles.Count < 1)
                return;

            if (!Toggles[0].group.AnyTogglesOn())
            {
                Toggles[0].isOn = true;
                if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(Toggles[0].gameObject);
                scrollRect.verticalNormalizedPosition = 1f;
                return;
            }

            for (var i = 0; i < Toggles.Count; i++)
            {
                if (!Toggles[i].isOn)
                    continue;
                i++;
                if (i == Toggles.Count)
                    i = 0;
                Toggles[i].isOn = true;
                if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(Toggles[i].gameObject);
                scrollRect.verticalNormalizedPosition = 1f - (i / (Toggles.Count - 1f));
                return;
            }
        }
    }
}
