/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.UI.ScrollRects
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectAutoScroll : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float scrollSpeed = 10f;

        private bool _active;
        private ScrollRect _scrollRect;
        private Vector2 _nextScrollPosition = Vector2.up;
        private readonly List<Selectable> _selectables = new List<Selectable>();

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void OnEnable()
        {
            if (_scrollRect)
                _scrollRect.content.GetComponentsInChildren(_selectables);
        }

        private void Start()
        {
            if (_scrollRect)
                _scrollRect.content.GetComponentsInChildren(_selectables);

            ScrollToSelected(true);
        }

        private void Update()
        {
            // Scroll via input
            InputScroll();

            // Scroll via hovering
            if (!_active)
            {
                _scrollRect.normalizedPosition = Vector2.Lerp(_scrollRect.normalizedPosition, _nextScrollPosition,
                    scrollSpeed * Time.deltaTime);
            }
            else
            {
                _nextScrollPosition = _scrollRect.normalizedPosition;
            }
        }

        private void InputScroll()
        {
            if (_selectables.Count <= 0)
                return;

            if (Input.GetButtonDown("Horizontal") || Input.GetButtonDown("Vertical") ||
                Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
            {
                ScrollToSelected(false);
            }
        }

        private void ScrollToSelected(bool quickScroll)
        {
            var selectedIndex = -1;
            var selectedElement = EventSystem.current.currentSelectedGameObject
                ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()
                : null;

            if (selectedElement)
                selectedIndex = _selectables.IndexOf(selectedElement);

            if (selectedIndex <= -1)
                return;

            if (quickScroll)
            {
                _scrollRect.normalizedPosition =
                    new Vector2(0, 1 - (selectedIndex / ((float) _selectables.Count - 1)));
                _nextScrollPosition = _scrollRect.normalizedPosition;
            }
            else
            {
                _nextScrollPosition = new Vector2(0, 1 - (selectedIndex / ((float) _selectables.Count - 1)));
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _active = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _active = false;
            ScrollToSelected(false);
        }
    }
}
