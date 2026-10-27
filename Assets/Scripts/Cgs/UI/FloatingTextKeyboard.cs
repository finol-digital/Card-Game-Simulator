/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.UI
{
    public interface IFloatingTextKeyboard
    {
        event Action Dismissed;
        bool Show(bool multiline, RectInt fieldBounds);
        void Hide();
    }

    // Watches the selected field, including fields instantiated after scene load.
    // Steam sends ordinary key events, so uGUI/TMP retain validation and caret handling.
    public sealed class FloatingTextKeyboard : MonoBehaviour
    {
        private IFloatingTextKeyboard _backend;
        private GameObject _selection;
        private InputField _legacy;
        private TMP_InputField _tmp;
        private Canvas _canvas;
        private bool _requested;
        private bool _visible;
        private bool _closing;
        private readonly Vector3[] _corners = new Vector3[4];

        public void Configure(IFloatingTextKeyboard backend)
        {
            Release();
            _backend = backend;
            if (_backend != null)
                _backend.Dismissed += OnDismissed;
        }

        private void LateUpdate()
        {
            if (_backend == null)
                return;
            var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (_selection != selection)
            {
                Close();
                _selection = selection;
                _legacy = selection != null ? selection.GetComponent<InputField>() : null;
                _tmp = selection != null ? selection.GetComponent<TMP_InputField>() : null;
                _canvas = selection != null ? selection.GetComponentInParent<Canvas>() : null;
                _requested = false;
            }

            if (!IsEditableFieldFocused())
            {
                Close();
                _requested = false;
                return;
            }
            if (_visible || _closing)
                return;

            var rect = (RectTransform)_selection.transform;
            var camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            var clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                          && RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current.position.ReadValue(), camera);
            var submitted = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            if (_requested && !clicked && !submitted)
                return;

            _requested = true; // Failed requests must not retry every frame either.
            var multiline = _legacy != null ? _legacy.multiLine : _tmp.multiLine;
            rect.GetWorldCorners(_corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in _corners)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            _visible = _backend.Show(multiline, ToKeyboardBounds(min, max, Screen.width, Screen.height));
            ActiveInputDevice.IsOnScreenKeyboardOpen = _visible;
        }

        private bool IsEditableFieldFocused()
        {
            if (_legacy != null)
                return _legacy.isActiveAndEnabled && _legacy.IsInteractable() && !_legacy.readOnly && _legacy.isFocused;
            if (_tmp != null)
                return _tmp.isActiveAndEnabled && _tmp.IsInteractable() && !_tmp.readOnly && _tmp.isFocused;
            return false;
        }

        public static RectInt ToKeyboardBounds(Vector2 min, Vector2 max, int width, int height)
        {
            // Steam uses client pixels with the origin at the top left; Unity uses bottom left.
            var x = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, Mathf.Max(0, width - 1));
            var y = Mathf.Clamp(Mathf.FloorToInt(height - max.y), 0, Mathf.Max(0, height - 1));
            var right = Mathf.Clamp(Mathf.CeilToInt(max.x), x + 1, Mathf.Max(x + 1, width));
            var bottom = Mathf.Clamp(Mathf.CeilToInt(height - min.y), y + 1, Mathf.Max(y + 1, height));
            return new RectInt(x, y, right - x, bottom - y);
        }

        private void Close()
        {
            if (!_visible || _closing)
                return;
            _closing = true;
            _backend.Hide();
        }

        private void OnDismissed()
        {
            _visible = false;
            _closing = false;
            ActiveInputDevice.IsOnScreenKeyboardOpen = false;
        }

        private void Release()
        {
            if (_backend != null)
            {
                _backend.Dismissed -= OnDismissed;
                if (_visible && !_closing)
                    _backend.Hide();
            }
            _backend = null;
            _requested = false;
            OnDismissed();
        }

        private void OnDestroy() => Release();
    }
}
