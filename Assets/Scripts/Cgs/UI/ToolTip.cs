/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using Cgs.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.UI
{
    public class ToolTip : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject tooltipPrefab;
        public string tooltip = "";
        public bool avoidOverlap;
        public bool isBelow;

        private GameObject ToolTipGameObject => _toolTipGameObject ??= Instantiate(tooltipPrefab, transform);

        private GameObject _toolTipGameObject;

        private CanvasGroup ToolTipCanvasGroup =>
            _toolTipCanvasGroup ??= ToolTipGameObject.GetOrAddComponent<CanvasGroup>();

        private CanvasGroup _toolTipCanvasGroup;

        private Text ToolTipText => _toolTipText ??= ToolTipGameObject.GetComponentInChildren<Text>();

        private Text _toolTipText;

        private void Awake()
        {
            ToolTipCanvasGroup.interactable = false;
            ToolTipCanvasGroup.blocksRaycasts = false;
            ToolTipCanvasGroup.alpha = 0;
            ToolTipText.text = tooltip;
        }

        private void OnEnable()
        {
            Hide();
        }

        private IEnumerator Start()
        {
            yield return null;

            var rectTransform = (RectTransform) ToolTipGameObject.transform;

            if (isBelow)
            {
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            yield return null;

            var offsetDirection = isBelow ? Vector2.down : Vector2.up;
            var offsetAmount = avoidOverlap ? 1.0f : 0.5f;
            rectTransform.anchoredPosition += offsetDirection * offsetAmount * rectTransform.sizeDelta.y;
        }

        public void OnSelect(BaseEventData eventData)
        {
            Show();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            Hide();
        }

        public void Show()
        {
            if (Settings.ButtonTooltipsEnabled)
                ToolTipCanvasGroup.alpha = 1;
        }

        public void Hide()
        {
            ToolTipCanvasGroup.alpha = 0;
        }
    }
}
