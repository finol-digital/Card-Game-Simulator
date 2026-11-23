/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cgs.Play
{
    public class DiceShakeRoller : MonoBehaviour
    {
        private readonly ShakeDetector _shakeDetector = new();

        private Accelerometer _enabledAccelerometer;

        protected void OnDisable()
        {
            DisableAccelerometer();
        }

        protected void Update()
        {
            if (!PlaySettings.ShakeToRollDice)
            {
                DisableAccelerometer();
                return;
            }

            var accelerometer = Accelerometer.current;
            if (accelerometer == null)
                return;

            if (_enabledAccelerometer != accelerometer)
            {
                DisableAccelerometer();
                InputSystem.EnableDevice(accelerometer);
                _enabledAccelerometer = accelerometer;
            }

            var acceleration = accelerometer.acceleration.ReadValue();
            if (!_shakeDetector.Sample(acceleration, Time.unscaledDeltaTime, Time.unscaledTime))
                return;

            var playController = PlayController.Instance;
            if (playController == null || playController.IsBlocked)
                return;

            RollAllDice(playController.PlayAreaCardZone);
        }

        public static int RollAllDice(Component playArea)
        {
            if (playArea == null)
                return 0;

            var dice = playArea.GetComponentsInChildren<Die>();
            foreach (var die in dice)
                die.Roll();
            return dice.Length;
        }

        private void DisableAccelerometer()
        {
            if (_enabledAccelerometer != null && _enabledAccelerometer.added)
                InputSystem.DisableDevice(_enabledAccelerometer);
            _enabledAccelerometer = null;
            _shakeDetector.Reset();
        }
    }
}
