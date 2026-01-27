/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.CardGameView.Viewer;
using Cgs.Play.Multiplayer;
using Cgs.UI.ScrollRects;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Play
{
    [RequireComponent(typeof(PlayController))]
    public class PlayMatRotationZoomController : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public Button rotationButton;
        public Slider rotationSlider;
        public Button zoomButton;
        public Slider zoomSlider;

        private const float PageSensitivity = 0.2f;
        private const float Tolerance = 0.01f;
        private const float TimeToDisappear = 3f;

        private float _timeSinceChange = TimeToDisappear;

        private PlayController _playController;

        private InputAction _pageAction;

        private bool IsBlocked => CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                                  PlayableViewer.Instance.IsVisible ||
                                  CardGameManager.Instance.ModalCanvas != null ||
                                  _playController.menu.panels.activeSelf ||
                                  _playController.scoreboard.nameInputField.isFocused;

        private void Awake()
        {
            _playController = GetComponent<PlayController>();
        }

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed += InputToggleRotation;
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed += InputToggleZoom;
        }

        private void Start()
        {
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        private void Update()
        {
            // Sync buttons
            if (rotationButton.interactable != _playController.playArea.RotationEnabled)
                rotationButton.interactable = _playController.playArea.RotationEnabled;
            if (zoomButton.interactable != _playController.playArea.ZoomEnabled)
                zoomButton.interactable = _playController.playArea.ZoomEnabled;

            // Sync sliders
            if (rotationSlider.interactable != _playController.playArea.RotationEnabled)
                rotationSlider.interactable = _playController.playArea.RotationEnabled;
            if (zoomSlider.interactable != _playController.playArea.ZoomEnabled)
                zoomSlider.interactable = _playController.playArea.ZoomEnabled;

            var changed = false;
            if (Math.Abs(rotationSlider.value - _playController.playArea.CurrentRotation) > Tolerance)
            {
                rotationSlider.value = _playController.playArea.CurrentRotation;
                _timeSinceChange = 0;
                changed = true;
            }

            if (Math.Abs(zoomSlider.value - _playController.playArea.CurrentZoom) > Tolerance)
            {
                zoomSlider.value = _playController.playArea.CurrentZoom;
                _timeSinceChange = 0;
                changed = true;
            }

            if (!changed)
                _timeSinceChange += Time.deltaTime;

            // Handle canvas group visibility
            if (_timeSinceChange < TimeToDisappear)
            {
                canvasGroup.alpha = 1 - _timeSinceChange / TimeToDisappear;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // Poll for Vector2 inputs
            if (IsBlocked)
                return;

            var pageHorizontal = _pageAction?.ReadValue<Vector2>().x ?? 0;
            if (Mathf.Abs(pageHorizontal) > PageSensitivity)
            {
                var horizontal = pageHorizontal * Time.deltaTime;
                if (_playController.playArea.RotationEnabled)
                    _playController.playArea.CurrentRotation += horizontal * RotateZoomableScrollRect.RotationSpeed;
                else
                    _playController.playArea.horizontalNormalizedPosition =
                        Mathf.Clamp01(_playController.playArea.horizontalNormalizedPosition + horizontal);
            }

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            if (Mathf.Abs(pageVertical) < PageSensitivity)
                return;

            var delta = pageVertical * Time.deltaTime;
            if (_playController.playArea.ZoomEnabled)
            {
                var zoomFactor = Mathf.Clamp(1 + delta, RotateZoomableScrollRect.MinZoom,
                    RotateZoomableScrollRect.MaxZoom);
                _playController.playArea.CurrentZoom = Mathf.Clamp(
                    _playController.playArea.CurrentZoom * zoomFactor,
                    RotateZoomableScrollRect.MinZoom,
                    RotateZoomableScrollRect.MaxZoom);
            }
            else
                _playController.playArea.verticalNormalizedPosition =
                    Mathf.Clamp01(_playController.playArea.verticalNormalizedPosition + delta);
        }

        private void InputToggleRotation(InputAction.CallbackContext obj)
        {
            if (IsBlocked)
                return;

            ToggleRotation();
        }

        [UsedImplicitly]
        public void ToggleRotation()
        {
            _playController.playArea.RotationEnabled = !_playController.playArea.RotationEnabled;
        }

        [UsedImplicitly]
        public void UpdateRotation(float rotation)
        {
            _timeSinceChange = 0;
            _playController.playArea.CurrentRotation = rotation;
        }

        [UsedImplicitly]
        public void ResetRotation()
        {
            _timeSinceChange = 0;
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.LocalPlayer != null)
                _playController.playArea.CurrentRotation = CgsNetManager.Instance.LocalPlayer.DefaultZRotation;
            else
                _playController.playArea.CurrentRotation = 0;
        }

        private void InputToggleZoom(InputAction.CallbackContext obj)
        {
            if (IsBlocked)
                return;

            ToggleZoom();
        }

        [UsedImplicitly]
        public void ToggleZoom()
        {
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
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed -= InputToggleRotation;
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed -= InputToggleZoom;
        }
    }
}
