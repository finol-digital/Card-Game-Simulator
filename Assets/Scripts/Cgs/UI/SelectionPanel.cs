/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.UI
{
    public delegate void OnSelectDelegate<in T>(Toggle toggle, T selection);

    public class SelectionPanel : MonoBehaviour
    {
        public ToggleGroup toggleGroup;
        public RectTransform selectionContent;
        public RectTransform selectionTemplate;
        public Text emptyText;
        public ScrollRect scrollRect;

        protected virtual bool AllowSwitchOff => true;

        protected List<Toggle> Toggles { get; } = new();

        protected void ClearPanel()
        {
            if (toggleGroup != null)
                toggleGroup.allowSwitchOff = true;

            Toggles.Clear();
            selectionContent.DestroyAllChildren();
            selectionContent.sizeDelta = new Vector2(selectionContent.sizeDelta.x, 0);
        }

        protected void Rebuild<TKey, TValue>(IDictionary<TKey, TValue> options, OnSelectDelegate<TKey> select,
            TKey current)
        {
            if (toggleGroup != null)
                toggleGroup.allowSwitchOff = true;

            Toggles.Clear();
            selectionContent.DestroyAllChildren();
            selectionContent.sizeDelta =
                new Vector2(selectionContent.sizeDelta.x, selectionTemplate.rect.height * options.Count);

            var currentSelectionIndex = -1;
            var i = 0;
            foreach (var option in options)
            {
                var toggle = Instantiate(selectionTemplate.gameObject, selectionContent).GetOrAddComponent<Toggle>();
                toggle.gameObject.SetActive(true);
                toggle.transform.localScale = Vector3.one;
                toggle.GetComponentInChildren<Text>().text = option.Value.ToString();
                toggle.interactable = true;
                toggle.isOn = option.Key.Equals(current);
                toggle.onValueChanged.AddListener(_ => select(toggle, option.Key));
                Toggles.Add(toggle);
                if (option.Key.Equals(current))
                    currentSelectionIndex = i;
                i++;
            }

            if (toggleGroup != null)
                toggleGroup.allowSwitchOff = AllowSwitchOff;

            if (!AllowSwitchOff && currentSelectionIndex >= 0)
                Toggles[currentSelectionIndex].isOn = true;

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

            for (var i = Toggles.Count - 1; i >= 0; i--)
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
