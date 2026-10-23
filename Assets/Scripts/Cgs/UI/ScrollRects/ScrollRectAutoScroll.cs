/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.UI.ScrollRects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectAutoScroll : MonoBehaviour
    {
        private ScrollRect _scrollRect;
        private GameObject _lastSelected;
        private readonly Vector3[] _corners = new Vector3[4];

        protected void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        protected void OnEnable()
        {
            _lastSelected = null;
        }

        protected void LateUpdate()
        {
            // Observe selection after UI navigation, including held-input repeats and
            // controls created after this view was enabled. Leave manual scrolling alone.
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _lastSelected)
                return;
            _lastSelected = selected;

            if (selected == null || !_scrollRect.isActiveAndEnabled || _scrollRect.content == null
                || !selected.activeInHierarchy || selected.transform is not RectTransform target
                || !target.IsChildOf(_scrollRect.content))
                return;

            // A dropdown popup owns its selection; opening it must not move the menu behind it.
            if (target.GetComponentInParent<ScrollRect>() != _scrollRect)
                return;

            Canvas.ForceUpdateCanvases();
            var viewport = _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)transform;
            var view = viewport.rect;
            var bounds = GetBounds(target, viewport);
            var contentBounds = GetBounds(_scrollRect.content, viewport);
            var position = _scrollRect.normalizedPosition;
            var next = position;
            var hiddenWidth = contentBounds.size.x - view.width;
            var hiddenHeight = contentBounds.size.y - view.height;

            if (_scrollRect.horizontal && hiddenWidth > 0)
            {
                var offset = VisibilityOffset(bounds.min.x, bounds.max.x, view.xMin, view.xMax, false);
                next.x = Mathf.Clamp01(position.x - offset / hiddenWidth);
            }
            if (_scrollRect.vertical && hiddenHeight > 0)
            {
                var offset = VisibilityOffset(bounds.min.y, bounds.max.y, view.yMin, view.yMax, true);
                next.y = Mathf.Clamp01(position.y - offset / hiddenHeight);
            }

            if (next == position)
                return;
            _scrollRect.StopMovement();
            _scrollRect.normalizedPosition = next;
        }

        private Bounds GetBounds(RectTransform target, RectTransform viewport)
        {
            // Measure the control itself, excluding children such as dropdown templates.
            target.GetWorldCorners(_corners);
            var bounds = new Bounds(viewport.InverseTransformPoint(_corners[0]), Vector3.zero);
            for (var i = 1; i < _corners.Length; i++)
                bounds.Encapsulate(viewport.InverseTransformPoint(_corners[i]));
            return bounds;
        }

        private static float VisibilityOffset(float min, float max, float viewMin, float viewMax, bool alignEnd)
        {
            if (max - min > viewMax - viewMin)
                return alignEnd ? viewMax - max : viewMin - min;
            if (min < viewMin)
                return viewMin - min;
            return max > viewMax ? viewMax - max : 0;
        }
    }
}
