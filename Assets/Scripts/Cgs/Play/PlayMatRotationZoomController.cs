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
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Button rotationButton;
        [SerializeField] Slider rotationSlider;
        [SerializeField] Toggle rotationLockToggle;
        [SerializeField] Button zoomButton;
        [SerializeField] Slider zoomSlider;
        [SerializeField] Toggle zoomLockToggle;

        private const float PageSensitivity = 0.2f;
        private const float Tolerance = 0.01f;
        private const float TimeToDisappear = 3f;

        private float _timeSinceChange = TimeToDisappear;

        private PlayController _playController;

        private InputAction _pageAction;

        private bool IsBlocked => CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                                  PlayableViewer.Instance.IsVisible ||
                                  CardGameManager.Instance.ModalCanvas != null ||
                                  _playController.Menu.Panels.activeSelf ||
                                  _playController.Scoreboard.NameInputField.isFocused;

        protected void Awake()
        {
            _playController = GetComponent<PlayController>();
        }

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed += InputToggleRotation;
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed += InputToggleZoom;
        }

        protected void Start()
        {
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        protected void Update()
        {
            // Sync buttons
            if (rotationButton.interactable != _playController.PlayArea.RotationEnabled)
                rotationButton.interactable = _playController.PlayArea.RotationEnabled;
            if (zoomButton.interactable != _playController.PlayArea.ZoomEnabled)
                zoomButton.interactable = _playController.PlayArea.ZoomEnabled;

            // Sync lock toggles
            if (rotationLockToggle.isOn != _playController.PlayArea.RotationEnabled)
                rotationLockToggle.SetIsOnWithoutNotify(_playController.PlayArea.RotationEnabled);
            if (zoomLockToggle.isOn != _playController.PlayArea.ZoomEnabled)
                zoomLockToggle.SetIsOnWithoutNotify(_playController.PlayArea.ZoomEnabled);

            // Sync sliders
            if (rotationSlider.interactable != _playController.PlayArea.RotationEnabled)
                rotationSlider.interactable = _playController.PlayArea.RotationEnabled;
            if (zoomSlider.interactable != _playController.PlayArea.ZoomEnabled)
                zoomSlider.interactable = _playController.PlayArea.ZoomEnabled;

            var changed = false;
            if (Math.Abs(rotationSlider.value - _playController.PlayArea.CurrentRotation) > Tolerance)
            {
                rotationSlider.value = _playController.PlayArea.CurrentRotation;
                _timeSinceChange = 0;
                changed = true;
            }

            if (Math.Abs(zoomSlider.value - _playController.PlayArea.CurrentZoom) > Tolerance)
            {
                zoomSlider.value = _playController.PlayArea.CurrentZoom;
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
                if (_playController.PlayArea.RotationEnabled)
                    _playController.PlayArea.CurrentRotation += horizontal * RotateZoomableScrollRect.RotationSpeed;
                else
                    _playController.PlayArea.horizontalNormalizedPosition =
                        Mathf.Clamp01(_playController.PlayArea.horizontalNormalizedPosition + horizontal);
            }

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            if (Mathf.Abs(pageVertical) < PageSensitivity)
                return;

            var delta = pageVertical * Time.deltaTime;
            if (_playController.PlayArea.ZoomEnabled)
            {
                var zoomFactor = Mathf.Clamp(1 + delta, RotateZoomableScrollRect.MinZoom,
                    RotateZoomableScrollRect.MaxZoom);
                _playController.PlayArea.CurrentZoom = Mathf.Clamp(
                    _playController.PlayArea.CurrentZoom * zoomFactor,
                    RotateZoomableScrollRect.MinZoom,
                    RotateZoomableScrollRect.MaxZoom);
            }
            else
                _playController.PlayArea.verticalNormalizedPosition =
                    Mathf.Clamp01(_playController.PlayArea.verticalNormalizedPosition + delta);
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
            SetRotationEnabled(!_playController.PlayArea.RotationEnabled);
        }

        [UsedImplicitly]
        public void SetRotationEnabled(bool isRotationEnabled)
        {
            _timeSinceChange = 0;
            _playController.PlayArea.RotationEnabled = isRotationEnabled;
        }

        [UsedImplicitly]
        public void UpdateRotation(float rotation)
        {
            _timeSinceChange = 0;
            _playController.PlayArea.CurrentRotation = rotation;
        }

        [UsedImplicitly]
        public void ResetRotation()
        {
            _timeSinceChange = 0;
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.LocalPlayer != null)
                _playController.PlayArea.CurrentRotation = CgsNetManager.Instance.LocalPlayer.DefaultZRotation;
            else
                _playController.PlayArea.CurrentRotation = 0;
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
            SetZoomEnabled(!_playController.PlayArea.ZoomEnabled);
        }

        [UsedImplicitly]
        public void SetZoomEnabled(bool isZoomEnabled)
        {
            _timeSinceChange = 0;
            _playController.PlayArea.ZoomEnabled = isZoomEnabled;
        }

        [UsedImplicitly]
        public void UpdateZoom(float zoom)
        {
            _timeSinceChange = 0;
            _playController.PlayArea.CurrentZoom = zoom;
        }

        [UsedImplicitly]
        public void ResetZoom()
        {
            _timeSinceChange = 0;
            _playController.PlayArea.CurrentZoom = RotateZoomableScrollRect.DefaultZoom;
        }

        protected void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed -= InputToggleRotation;
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed -= InputToggleZoom;
        }
    }
}
