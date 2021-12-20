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
    public class PlayMatZoom : MonoBehaviour
    {
        public Slider slider;
        public CanvasGroup sliderCanvasGroup;

        private const float PageVerticalSensitivity = 0.2f;
        private const float Tolerance = 0.01f;
        private const float TimeToDisappear = 3f;

        private PlayController _playController;
        private float _timeSinceChange = TimeToDisappear;

        private void Start()
        {
            _playController = GetComponent<PlayController>();
        }

        private void Update()
        {
            // Update Visuals
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

            // Handle Input
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                CardGameManager.Instance.ModalCanvas != null || _playController.scoreboard.nameInputField.isFocused)
                return;

            if (Inputs.IsSort)
                _playController.playArea.ZoomEnabled = !_playController.playArea.ZoomEnabled;

            if (!Inputs.IsPageVertical)
                return;
            if (_playController.playArea.ZoomEnabled)
                _playController.playArea.CurrentZoom *=
                    1 - Inputs.FPageVertical * PageVerticalSensitivity * Time.deltaTime;
            else
                _playController.playArea.verticalNormalizedPosition -=
                    Inputs.FPageVertical * PageVerticalSensitivity * Time.deltaTime;
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
            _playController.playArea.CurrentZoom = 1;
        }
    }
}
