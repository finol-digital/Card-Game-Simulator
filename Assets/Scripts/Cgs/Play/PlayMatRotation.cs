/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.CardGameView;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    [RequireComponent(typeof(PlayController))]
    public class PlayMatRotation : MonoBehaviour
    {
        public Slider slider;
        public CanvasGroup sliderCanvasGroup;

        private const float PageHorizontalSensitivity = 45f;
        private const float Tolerance = 0.01f;
        private const float TimeToDisappear = 3f;

        private PlayController _playController;
        private float _timeSinceChange = TimeToDisappear;

        private bool _rotationEnabled = true;

        private void Start()
        {
            _playController = GetComponent<PlayController>();
        }

        private void Update()
        {
            // Update Visuals
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

            // Handle Input
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                CardGameManager.Instance.ModalCanvas != null || _playController.scoreboard.nameInputField.isFocused)
                return;

            if (Inputs.IsLeft && !Inputs.WasLeft)
                _playController.playArea.CurrentRotation -= 90;
            else if (Inputs.IsRight && !Inputs.WasRight)
                _playController.playArea.CurrentRotation += 90;

            if (Inputs.IsSort)
                _rotationEnabled = !_rotationEnabled;

            if (!Inputs.IsPageHorizontal)
                return;
            if (_rotationEnabled)
                _playController.playArea.CurrentRotation +=
                    Time.deltaTime * Inputs.FPageHorizontal * PageHorizontalSensitivity;
            else
                _playController.playArea.horizontalNormalizedPosition -=
                    Inputs.FPageHorizontal * PageHorizontalSensitivity * Time.deltaTime;
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
            _playController.playArea.CurrentRotation = 0;
        }
    }
}
