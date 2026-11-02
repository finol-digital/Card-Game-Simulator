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
        [SerializeField] bool fitBelowAnchor;
        [SerializeField, Min(0)] float anchorGap = 20;

        private const float SelectionGap = 20;
        private const float MinimumSelectionScale = 0.25f;

        private RectTransform _rectTransform;
        private RectTransform _viewport;
        private Canvas _canvas;
        private RectTransform _avoidanceTarget;
        private Canvas _targetCanvas;
        private readonly Vector3[] _targetCorners = new Vector3[4];
        private Vector2 _preferredPosition;
        private Rect _previousViewport;
        private Rect? _previousTargetBounds;
        private float _previousTopEdge;
        private bool _layoutDirty = true;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _viewport = transform.parent as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            _preferredPosition = _rectTransform.anchoredPosition;
        }

        public void SetAvoidanceTarget(RectTransform target)
        {
            if (_avoidanceTarget == target)
                return;

            _avoidanceTarget = target;
            _targetCanvas = target != null ? target.GetComponentInParent<Canvas>(true) : null;
            _layoutDirty = true;
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
            if (_viewport == null || _rectTransform.rect.height <= 0 || _rectTransform.rect.width <= 0)
                return;

            var viewport = _viewport.rect;
            var targetBounds = GetTargetBounds();
            var anchorY = viewport.yMin + viewport.height * Mathf.Lerp(
                _rectTransform.anchorMin.y, _rectTransform.anchorMax.y, _rectTransform.pivot.y);
            // Card action panels share their vertical anchor with the viewer's lower edge.
            var topEdge = fitBelowAnchor
                ? Mathf.Min(viewport.yMax - topInset, anchorY - anchorGap)
                : viewport.yMax - topInset;
            // Fixed-anchor children need not change dimensions when their canvas resizes.
            if (!_layoutDirty && viewport == _previousViewport && targetBounds == _previousTargetBounds
                && Mathf.Approximately(topEdge, _previousTopEdge))
                return;

            _layoutDirty = false;
            _previousViewport = viewport;
            _previousTargetBounds = targetBounds;
            _previousTopEdge = topEdge;
            var availableHeight = Mathf.Max(0, topEdge - viewport.yMin - bottomInset);
            var scale = Mathf.Min(1, availableHeight / _rectTransform.rect.height);
            scale = Mathf.Min(scale, Mathf.Max(0, viewport.width) / _rectTransform.rect.width);

            // Scale the complete panel so the gaps and below-button tooltips stay together.
            var width = _rectTransform.rect.width * scale;
            var height = _rectTransform.rect.height * scale;
            var anchorX = viewport.xMin + viewport.width * Mathf.Lerp(
                _rectTransform.anchorMin.x, _rectTransform.anchorMax.x, _rectTransform.pivot.x);
            var minimumX = viewport.xMin + width * _rectTransform.pivot.x;
            var maximumX = viewport.xMax - width * (1 - _rectTransform.pivot.x);
            var minimumY = viewport.yMin + bottomInset + height * _rectTransform.pivot.y;
            var maximumY = topEdge - height * (1 - _rectTransform.pivot.y);
            var positionX = Mathf.Clamp(anchorX + _preferredPosition.x, minimumX, maximumX);
            var positionY = fitBelowAnchor
                ? maximumY
                : Mathf.Clamp(anchorY + _preferredPosition.y, minimumY, maximumY);
            var panelBounds = new Rect(positionX - width * _rectTransform.pivot.x,
                positionY - height * _rectTransform.pivot.y, width, height);
            var position = new Vector2(positionX, positionY);
            if (targetBounds.HasValue && panelBounds.Overlaps(targetBounds.Value))
            {
                var availableViewport = new Rect(viewport.xMin, viewport.yMin + bottomInset,
                    viewport.width, availableHeight);
                position = PositionBesideSelection(availableViewport, targetBounds.Value, panelBounds, ref scale);
            }

            _rectTransform.localScale = new Vector3(scale, scale, 1);
            _rectTransform.anchoredPosition = position - new Vector2(anchorX, anchorY);
        }

        private Vector2 PositionBesideSelection(Rect viewport, Rect target, Rect panelBounds, ref float scale)
        {
            var width = panelBounds.width;
            var height = panelBounds.height;
            var positionX = panelBounds.xMin + width * _rectTransform.pivot.x;
            var positionY = panelBounds.yMin + height * _rectTransform.pivot.y;
            var leftSpace = Mathf.Clamp(target.xMin - SelectionGap - viewport.xMin, 0, viewport.width);
            var rightSpace = Mathf.Clamp(viewport.xMax - target.xMax - SelectionGap, 0, viewport.width);
            var leftX = target.xMin - SelectionGap - width * (1 - _rectTransform.pivot.x);
            var rightX = target.xMax + SelectionGap + width * _rectTransform.pivot.x;
            // Prefer the nearest side that fits; use the larger side on a narrow screen.
            var moveLeft = leftSpace >= width && (rightSpace < width ||
                Mathf.Abs(positionX - leftX) <= Mathf.Abs(positionX - rightX));
            if (leftSpace < width && rightSpace < width)
            {
                moveLeft = leftSpace >= rightSpace;
                var sideScale = (moveLeft ? leftSpace : rightSpace) / _rectTransform.rect.width;
                if (sideScale < Mathf.Min(scale, MinimumSelectionScale))
                    return PositionAboveOrBelowSelection(viewport, target, panelBounds, ref scale);

                scale = Mathf.Min(scale, sideScale);
                width = _rectTransform.rect.width * scale;
                height = _rectTransform.rect.height * scale;
                // Keep the top of the actions fixed when shrinking beside a large selection.
                positionY = panelBounds.yMax - height * (1 - _rectTransform.pivot.y);
            }

            positionX = moveLeft
                ? target.xMin - SelectionGap - width * (1 - _rectTransform.pivot.x)
                : target.xMax + SelectionGap + width * _rectTransform.pivot.x;
            return new Vector2(positionX, positionY);
        }

        private Vector2 PositionAboveOrBelowSelection(Rect viewport, Rect target, Rect panelBounds, ref float scale)
        {
            var belowSpace = Mathf.Clamp(target.yMin - SelectionGap - viewport.yMin, 0, viewport.height);
            var aboveSpace = Mathf.Clamp(viewport.yMax - target.yMax - SelectionGap, 0, viewport.height);
            var position = panelBounds.min + Vector2.Scale(panelBounds.size, _rectTransform.pivot);
            var belowY = target.yMin - SelectionGap - panelBounds.height * (1 - _rectTransform.pivot.y);
            var aboveY = target.yMax + SelectionGap + panelBounds.height * _rectTransform.pivot.y;
            var moveBelow = belowSpace >= panelBounds.height && (aboveSpace < panelBounds.height ||
                Mathf.Abs(position.y - belowY) <= Mathf.Abs(position.y - aboveY));
            if (belowSpace < panelBounds.height && aboveSpace < panelBounds.height)
                moveBelow = belowSpace >= aboveSpace;

            var verticalScale = Mathf.Min(scale,
                (moveBelow ? belowSpace : aboveSpace) / _rectTransform.rect.height);
            // A selection covering the entire available view cannot be avoided. Keep the
            // normal on-screen controls reachable so the player can still act on it.
            if (verticalScale < Mathf.Min(scale, MinimumSelectionScale))
                return position;

            scale = verticalScale;
            var width = _rectTransform.rect.width * scale;
            var height = _rectTransform.rect.height * scale;
            position.x = Mathf.Clamp(position.x, viewport.xMin + width * _rectTransform.pivot.x,
                viewport.xMax - width * (1 - _rectTransform.pivot.x));
            position.y = moveBelow
                ? target.yMin - SelectionGap - height * (1 - _rectTransform.pivot.y)
                : target.yMax + SelectionGap + height * _rectTransform.pivot.y;
            return position;
        }

        private Rect? GetTargetBounds()
        {
            if (_avoidanceTarget == null || !_avoidanceTarget.gameObject.activeInHierarchy)
                return null;

            var sourceCamera = GetCanvasCamera(_targetCanvas);
            var destinationCamera = GetCanvasCamera(_canvas);
            _avoidanceTarget.GetWorldCorners(_targetCorners);
            var minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in _targetCorners)
            {
                var screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, corner);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _viewport, screenPoint, destinationCamera, out var localPoint))
                    return null;

                minimum = Vector2.Min(minimum, localPoint);
                maximum = Vector2.Max(maximum, localPoint);
            }

            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        private static Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.rootCanvas.worldCamera;
        }
    }
}
