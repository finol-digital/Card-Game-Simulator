/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ActionPanelViewportFit : MonoBehaviour
    {
        [SerializeField, Min(0)] float topInset = 160;
        [SerializeField, Min(0)] float bottomInset = 20;

        private RectTransform _rectTransform;
        private RectTransform _viewport;
        private Vector2 _preferredPosition;
        private Rect _previousViewport;
        private bool _layoutDirty = true;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _viewport = transform.parent as RectTransform;
            _preferredPosition = _rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            _layoutDirty = true;
        }

        private void OnRectTransformDimensionsChange()
        {
            _layoutDirty = true;
        }

        private void LateUpdate()
        {
            if (_viewport == null || _rectTransform.rect.height <= 0)
                return;

            var viewport = _viewport.rect;
            // Fixed-anchor children need not change dimensions when their canvas resizes.
            if (!_layoutDirty && viewport == _previousViewport)
                return;

            _layoutDirty = false;
            _previousViewport = viewport;
            var availableHeight = Mathf.Max(0, viewport.height - topInset - bottomInset);
            var scale = Mathf.Min(1, availableHeight / _rectTransform.rect.height);

            // Scale the complete panel so the gaps and below-button tooltips stay together.
            _rectTransform.localScale = new Vector3(scale, scale, 1);
            var height = _rectTransform.rect.height * scale;
            var anchorY = viewport.yMin + viewport.height * Mathf.Lerp(
                _rectTransform.anchorMin.y, _rectTransform.anchorMax.y, _rectTransform.pivot.y);
            var minimumY = viewport.yMin + bottomInset + height * _rectTransform.pivot.y;
            var maximumY = viewport.yMax - topInset - height * (1 - _rectTransform.pivot.y);
            var positionY = Mathf.Clamp(anchorY + _preferredPosition.y, minimumY, maximumY);
            _rectTransform.anchoredPosition = new Vector2(_preferredPosition.x, positionY - anchorY);
        }
    }
}
