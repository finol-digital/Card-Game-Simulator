/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.CardGameView.Viewer;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Play
{
    [RequireComponent(typeof(PlayController))]
    public class PlayMatRotation : MonoBehaviour
    {
        public Slider slider;
        public CanvasGroup sliderCanvasGroup;

        private const float PageHorizontalSensitivity = 0.2f;
        private const float Tolerance = 0.01f;
        private const float TimeToDisappear = 3f;

        private float _timeSinceChange = TimeToDisappear;

        private bool _rotationEnabled;

        private PlayController _playController;

        private InputAction _pageAction;

        private bool IsBlocked => CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                                  CardGameManager.Instance.ModalCanvas != null ||
                                  _playController.menu.panels.activeSelf ||
                                  _playController.scoreboard.nameInputField.isFocused;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed += InputToggleRotation;
        }

        private void Start()
        {
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
            _playController = GetComponent<PlayController>();
        }

        private void Update()
        {
            if (Math.Abs(slider.value - _playController.playArea.CurrentRotation) > Tolerance)
            {
                slider.value = _playController.playArea.CurrentRotation;
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
            if (_pageAction == null || !_pageAction.WasPressedThisFrame() || IsBlocked)
                return;

            var pageHorizontal = _pageAction.ReadValue<Vector2>().x;
            if (_rotationEnabled)
            {
                if (pageHorizontal < 0)
                    _playController.playArea.CurrentRotation -= 90;
                else
                    _playController.playArea.CurrentRotation += 90;
            }
            else
                _playController.playArea.horizontalNormalizedPosition -=
                    pageHorizontal * PageHorizontalSensitivity * 10;
        }

        private void InputToggleRotation(InputAction.CallbackContext obj)
        {
            if (IsBlocked)
                return;

            _rotationEnabled = !_rotationEnabled;
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

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayGameToggleZoomRotation).performed -= InputToggleRotation;
        }
    }
}
