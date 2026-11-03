/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

namespace Cgs.CardGameView.Viewer
{
    // Draw a local copy above the table without changing layout, network state, or hit targets.
    public sealed class CardHoverPreview
    {
        private const float Magnification = 1.5f;
        private readonly RectTransform _bounds;
        private readonly Image _image;
        private readonly Vector3[] _corners = new Vector3[4];
        private Image _source;

        public CardHoverPreview(RectTransform bounds)
        {
            _bounds = bounds;
            var overlay = new GameObject("Card Hover Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(bounds, false);
            overlay.layer = bounds.gameObject.layer;
            _image = overlay.GetComponent<Image>();
            _image.raycastTarget = false;
            _image.maskable = false;
            _image.enabled = false;
        }

        public void Show(Image source)
        {
            _source = source;
            Refresh();
        }

        public void Hide()
        {
            _source = null;
            _image.enabled = false;
            _image.sprite = null;
        }

        public void Refresh()
        {
            if (_source == null || !_source.isActiveAndEnabled)
            {
                Hide();
                return;
            }

            // Keep the source so an asynchronous image load can resume the preview.
            if (_source.sprite == null)
            {
                _image.enabled = false;
                _image.sprite = null;
                return;
            }

            var sourceCanvas = _source.canvas;
            var targetCanvas = _image.canvas;
            if (sourceCanvas == null || targetCanvas == null)
            {
                Hide();
                return;
            }

            var sourceCamera = sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCanvas.worldCamera;
            var targetCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            _source.rectTransform.GetWorldCorners(_corners);
            for (var i = 0; i < _corners.Length; i++)
            {
                var screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, _corners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_bounds, screenPoint, targetCamera, out var local);
                _corners[i] = local;
            }

            var horizontal = _corners[3] - _corners[0];
            var vertical = _corners[1] - _corners[0];
            var size = new Vector2(horizontal.magnitude, vertical.magnitude);
            var extents = new Vector2(Mathf.Abs(horizontal.x) + Mathf.Abs(vertical.x),
                Mathf.Abs(horizontal.y) + Mathf.Abs(vertical.y));
            if (extents.x <= 0 || extents.y <= 0)
            {
                _image.enabled = false;
                return;
            }

            var bounds = _bounds.rect;
            var scale = Mathf.Min(Magnification, bounds.width / extents.x, bounds.height / extents.y);
            var half = extents * (scale * 0.5f);
            var center = (_corners[0] + _corners[2]) * 0.5f;
            center.x = Mathf.Clamp(center.x, bounds.xMin + half.x, bounds.xMax - half.x);
            center.y = Mathf.Clamp(center.y, bounds.yMin + half.y, bounds.yMax - half.y);

            var rect = _image.rectTransform;
            rect.anchorMin = rect.anchorMax = _bounds.pivot;
            rect.sizeDelta = size * scale;
            rect.localPosition = center;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(horizontal.y, horizontal.x) * Mathf.Rad2Deg);
            _image.sprite = _source.sprite;
            _image.color = _source.color;
            _image.preserveAspect = _source.preserveAspect;
            _image.enabled = true;
        }
    }
}
