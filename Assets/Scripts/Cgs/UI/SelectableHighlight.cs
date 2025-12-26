/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.UI
{
    [RequireComponent(typeof(Image))]
    public class SelectableHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public static int IsSelectedPropertyId => _isSelectedPropertyId ??= Shader.PropertyToID("_IsSelected");
        private static int? _isSelectedPropertyId;

        private Image _image;
        private Outline _outline;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _outline = GetComponent<Outline>();
        }

        private void Start()
        {
            if (_image.material != null)
                return;

            var shader = Shader.Find("Shader Graphs/SelectableShader");
            if (shader != null)
                _image.material = new Material(shader);
        }

        public void OnSelect(BaseEventData eventData)
        {
            _image.material.SetFloat(IsSelectedPropertyId, 1);
            _image.SetMaterialDirty();
            if (_outline != null)
                _outline.enabled = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _image.material.SetFloat(IsSelectedPropertyId, 0);
            _image.SetMaterialDirty();
            if (_outline != null)
                _outline.enabled = false;
        }
    }
}
