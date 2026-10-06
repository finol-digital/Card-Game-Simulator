/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Globalization;
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
        [SerializeField] GameObject tooltipPrefab;
        [SerializeField] string tooltip = "";
        [SerializeField] bool avoidOverlap;
        [SerializeField] bool isBelow;
        [SerializeField] string inputActionId;
        [SerializeField] Transform parentTransform;
        [SerializeField] RectTransform singleLineBounds;

        private GameObject ToolTipGameObject => _toolTipGameObject ??= Instantiate(tooltipPrefab,
            parentTransform == null ? transform : parentTransform);

        private GameObject _toolTipGameObject;

        private CanvasGroup ToolTipCanvasGroup =>
            _toolTipCanvasGroup ??= ToolTipGameObject.GetOrAddComponent<CanvasGroup>();

        private CanvasGroup _toolTipCanvasGroup;

        private Text ToolTipText => _toolTipText ??= ToolTipGameObject.GetComponentInChildren<Text>();

        private Text _toolTipText;

        private bool _isOver;
        private readonly Vector3[] _boundsCorners = new Vector3[4];
        private string _previousContent;
        private float _previousWidth = -1;

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
                {
                    if (singleLineBounds != null)
                        return $"{inputActionBinding} — {tooltip}";
                    return isBelow ? $"{inputActionBinding}\n{tooltip}" : $"{tooltip}\n{inputActionBinding}";
                }
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
                return inputAction.GetBindingDisplayString(inputBinding)
                    .Replace("| `", "").Replace("| Backspace", "")
                    .Replace("Keypad ", "").Replace("Numpad ", "").Replace("Num ", "");
            }
        }

        protected void Awake()
        {
            ToolTipCanvasGroup.interactable = false;
            ToolTipCanvasGroup.blocksRaycasts = false;
            ToolTipCanvasGroup.alpha = 0; // Initially invisible
            if (singleLineBounds != null)
            {
                ToolTipGameObject.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().enabled = false;
                ToolTipGameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>().enabled = false;
                ToolTipText.GetComponent<UnityEngine.UI.ContentSizeFitter>().enabled = false;
                ToolTipText.horizontalOverflow = HorizontalWrapMode.Overflow;
                ToolTipText.verticalOverflow = VerticalWrapMode.Truncate;
                ToolTipText.resizeTextForBestFit = false;
                var label = ToolTipText.rectTransform;
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.offsetMin = new Vector2(8, 5);
                label.offsetMax = new Vector2(-8, -5);
            }
        }

        protected void Start()
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

        protected void Update()
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
            if (singleLineBounds == null)
                ToolTipText.text = tooltipText;
            else
                UpdateSingleLine(tooltipText);

            var offsetDirection = isBelow ? Vector2.down : Vector2.up;
            var offsetAmount = avoidOverlap ? 1.0f : 0.5f;
            var rectTransform = (RectTransform)ToolTipGameObject.transform;
            rectTransform.anchoredPosition = offsetDirection * (offsetAmount * rectTransform.sizeDelta.y)
                                             + ((RectTransform)ToolTipGameObject.transform.parent).anchoredPosition;
            var positionX = 0f;
            if (singleLineBounds != null)
            {
                var parent = (RectTransform)rectTransform.parent;
                var center = parent.InverseTransformPoint(singleLineBounds.TransformPoint(singleLineBounds.rect.center));
                positionX = center.x - parent.rect.center.x;
            }
            rectTransform.anchoredPosition = new Vector2(positionX, rectTransform.anchoredPosition.y);
        }

        private void UpdateSingleLine(string content)
        {
            var rectTransform = (RectTransform)ToolTipGameObject.transform;
            singleLineBounds.GetWorldCorners(_boundsCorners);
            var parent = rectTransform.parent;
            var availableWidth = Mathf.Max(0, parent.InverseTransformPoint(_boundsCorners[2]).x
                                             - parent.InverseTransformPoint(_boundsCorners[0]).x - 24);
            if (content == _previousContent && Mathf.Approximately(availableWidth, _previousWidth))
                return;

            _previousContent = content;
            _previousWidth = availableWidth;
            content = content.Replace('\r', ' ').Replace('\n', ' ');
            var settings = ToolTipText.GetGenerationSettings(Vector2.zero);
            var generator = ToolTipText.cachedTextGeneratorForLayout;
            var pixelsPerUnit = ToolTipText.pixelsPerUnit;
            var width = Mathf.Min(availableWidth, Mathf.Ceil(generator.GetPreferredWidth(content, settings) / pixelsPerUnit) + 16);
            var height = Mathf.Ceil(generator.GetPreferredHeight("Ag", settings) / pixelsPerUnit) + 10;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            var textWidth = Mathf.Max(0, width - 16);
            if (generator.GetPreferredWidth(content, settings) / pixelsPerUnit > textWidth)
            {
                // Search text elements so truncation preserves surrogate pairs and combining characters.
                var elements = StringInfo.ParseCombiningCharacters(content);
                var low = 0;
                var high = elements.Length;
                while (low < high)
                {
                    var middle = (low + high + 1) / 2;
                    var end = middle < elements.Length ? elements[middle] : content.Length;
                    var candidate = content.Substring(0, end) + "…";
                    if (generator.GetPreferredWidth(candidate, settings) / pixelsPerUnit <= textWidth)
                        low = middle;
                    else
                        high = middle - 1;
                }
                var length = low < elements.Length ? elements[low] : content.Length;
                content = generator.GetPreferredWidth("…", settings) / pixelsPerUnit <= textWidth
                    ? content.Substring(0, length) + "…"
                    : string.Empty;
            }
            ToolTipText.text = content;
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
