/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.CardGameView.Viewer;
using Cgs.UI.ScrollRects;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Play
{
    [RequireComponent(typeof(PlayController))]
    public class PlayMatZoom : MonoBehaviour
    {
        public Slider slider;
        public CanvasGroup sliderCanvasGroup;

        private const float PageVerticalSensitivity = 0.2f;
        private const float Tolerance = 0.01f;
        private const float TimeToDisappear = 3f;

        private float _timeSinceChange = TimeToDisappear;

        private PlayController _playController;

        private InputAction _pageAction;

        private bool IsBlocked => CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                                  CardGameManager.Instance.ModalCanvas != null ||
                                  _playController.menu.panels.activeSelf ||
                                  _playController.scoreboard.nameInputField.isFocused;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed += InputToggleZoomPan;
        }

        private void Start()
        {
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
            _playController = GetComponent<PlayController>();
        }

        private void Update()
        {
            if (Math.Abs(slider.value - _playController.playArea.CurrentZoom) > Tolerance)
            {
                slider.value = _playController.playArea.CurrentZoom;
                _timeSinceChange = 0;
            }
            else
                _timeSinceChange += Time.deltaTime;

            if (_timeSinceChange < TimeToDisappear)
            {
                sliderCanvasGroup.alpha = 1 - _timeSinceChange / TimeToDisappear;
                sliderCanvasGroup.interactable = true;
                sliderCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                sliderCanvasGroup.alpha = 0;
                sliderCanvasGroup.interactable = false;
                sliderCanvasGroup.blocksRaycasts = false;
            }

            // Poll for Vector2 inputs
            if (IsBlocked)
                return;

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            if (Mathf.Abs(pageVertical) < PageVerticalSensitivity)
                return;

            if (_playController.playArea.ZoomEnabled)
            {
                var zoomFactor = Mathf.Clamp(1 - pageVertical * PageVerticalSensitivity, 0.5f, 1.5f);
                _playController.playArea.CurrentZoom = Mathf.Clamp(
                    _playController.playArea.CurrentZoom * zoomFactor,
                    RotateZoomableScrollRect.MinZoom,
                    RotateZoomableScrollRect.MaxZoom);
            }
            else
                _playController.playArea.verticalNormalizedPosition -= pageVertical * PageVerticalSensitivity * 10;
        }

        private void InputToggleZoomPan(InputAction.CallbackContext obj)
        {
            if (IsBlocked)
                return;

            _playController.playArea.ZoomEnabled = !_playController.playArea.ZoomEnabled;
        }

        [UsedImplicitly]
        public void UpdateZoom(float zoom)
        {
            _timeSinceChange = 0;
            _playController.playArea.CurrentZoom = zoom;
        }

        [UsedImplicitly]
        public void ResetZoom()
        {
            _timeSinceChange = 0;
            _playController.playArea.CurrentZoom = RotateZoomableScrollRect.DefaultZoom;
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed -= InputToggleZoomPan;
        }
    }
}
