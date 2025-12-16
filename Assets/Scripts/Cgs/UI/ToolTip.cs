/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.UI
{
    public class ToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject tooltipPrefab;
        public string tooltip = "";
        public bool avoidOverlap;
        public bool isBelow;
        public string inputActionId;
        public Transform parentTransform;

        private GameObject ToolTipGameObject => _toolTipGameObject ??= Instantiate(tooltipPrefab,
            parentTransform == null ? transform : parentTransform);

        private GameObject _toolTipGameObject;

        private CanvasGroup ToolTipCanvasGroup =>
            _toolTipCanvasGroup ??= ToolTipGameObject.GetOrAddComponent<CanvasGroup>();

        private CanvasGroup _toolTipCanvasGroup;

        private Text ToolTipText => _toolTipText ??= ToolTipGameObject.GetComponentInChildren<Text>();

        private Text _toolTipText;

        private bool _isOver;

        private string TooltipTextContent
        {
            get
            {
                var inputActionBinding = InputActionBinding;
                if (EventSystem.current == null
                    || (EventSystem.current.currentSelectedGameObject != gameObject && !_isOver))
                    return inputActionBinding;
                var hasBinding = !string.IsNullOrEmpty(inputActionBinding);
                if (hasBinding)
                    return isBelow ? $"{inputActionBinding}\n{tooltip}" : $"{tooltip}\n{inputActionBinding}";
                return tooltip;
            }
        }

        private string InputActionBinding
        {
            get
            {
                if (string.IsNullOrEmpty(inputActionId))
                    return string.Empty;

                var inputAction = InputSystem.actions.FindAction(inputActionId);
                if (inputAction == null)
                {
                    Debug.LogError($"ToolTip: {gameObject.name} has Input Action '{inputActionId}' not found.");
                    return string.Empty;
                }

                var inputBinding = Gamepad.current != null
                    ? InputBinding.MaskByGroup("Gamepad")
                    : InputBinding.MaskByGroup("Keyboard&Mouse");
                return inputAction.GetBindingDisplayString(inputBinding).Replace("| Backspace", "");
            }
        }

        private void Awake()
        {
            ToolTipCanvasGroup.interactable = false;
            ToolTipCanvasGroup.blocksRaycasts = false;
            ToolTipCanvasGroup.alpha = 0; // Initially invisible
        }

        private void Start()
        {
            var rectTransform = (RectTransform)ToolTipGameObject.transform;
            if (!isBelow)
                return;
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (!Settings.ButtonTooltipsEnabled)
            {
                ToolTipCanvasGroup.alpha = 0;
                return;
            }

            var tooltipText = TooltipTextContent;
            if (string.IsNullOrEmpty(tooltipText))
            {
                ToolTipCanvasGroup.alpha = 0;
                return;
            }

            ToolTipCanvasGroup.alpha = 1;
            ToolTipText.text = tooltipText;

            var offsetDirection = isBelow ? Vector2.down : Vector2.up;
            var offsetAmount = avoidOverlap ? 1.0f : 0.5f;
            var rectTransform = (RectTransform)ToolTipGameObject.transform;
            rectTransform.anchoredPosition = offsetDirection * (offsetAmount * rectTransform.sizeDelta.y)
                                             + ((RectTransform)ToolTipGameObject.transform.parent).anchoredPosition;
            rectTransform.anchoredPosition = new Vector2(0, rectTransform.anchoredPosition.y);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isOver = false;
        }
    }
}
