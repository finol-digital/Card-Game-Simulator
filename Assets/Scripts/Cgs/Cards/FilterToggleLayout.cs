/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Cards
{
    // The filter's manually positioned buttons must also resize after an
    // asynchronous translation or font change, without measuring every frame.
    public sealed class FilterToggleLayout : MonoBehaviour
    {
        private Text[] _labels;
        private Vector3 _origin;
        private bool _dirty;

        private void OnEnable()
        {
            _labels = GetComponentsInChildren<Toggle>().Select(toggle => toggle.GetComponentInChildren<Text>())
                .Where(label => label != null).ToArray();
            if (_labels.Length > 0)
                _origin = _labels[0].GetComponentInParent<Toggle>().transform.localPosition;
            foreach (var label in _labels)
                label.RegisterDirtyVerticesCallback(MarkDirty);
            _dirty = true;
        }

        private void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty)
                return;
            _dirty = false;
            var position = _origin;
            var width = 0f;
            foreach (var label in _labels)
            {
                var toggle = label.GetComponentInParent<Toggle>();
                if (!toggle.gameObject.activeSelf)
                    continue;
                var buttonWidth = label.preferredWidth + 25f;
                toggle.transform.localPosition = position;
                var image = toggle.GetComponentInChildren<Image>().rectTransform;
                image.sizeDelta = new Vector2(buttonWidth, image.sizeDelta.y);
                position.x += buttonWidth;
                width += buttonWidth;
            }
            var container = (RectTransform)transform;
            container.sizeDelta = new Vector2(width, container.sizeDelta.y);
        }

        private void OnDisable()
        {
            if (_labels == null)
                return;
            foreach (var label in _labels)
                if (label != null)
                    label.UnregisterDirtyVerticesCallback(MarkDirty);
        }
    }
}
