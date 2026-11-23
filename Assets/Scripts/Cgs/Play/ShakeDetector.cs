/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs.Play
{
    public class ShakeDetector
    {
        public const float DefaultThreshold = 2.0f;
        public const float DefaultCooldown = 1.5f;

        private const float LowPassKernelWidth = 1.0f;

        private readonly float _sqrThreshold;
        private readonly float _cooldown;

        private Vector3 _lowPassValue;
        private bool _hasSample;
        private float _lastShakeTime = float.NegativeInfinity;

        public ShakeDetector(float threshold = DefaultThreshold, float cooldown = DefaultCooldown)
        {
            _sqrThreshold = threshold * threshold;
            _cooldown = cooldown;
        }

        public bool Sample(Vector3 acceleration, float deltaTime, float time)
        {
            if (!_hasSample)
            {
                _lowPassValue = acceleration;
                _hasSample = true;
                return false;
            }

            _lowPassValue = Vector3.Lerp(_lowPassValue, acceleration, Mathf.Clamp01(deltaTime / LowPassKernelWidth));
            var deltaAcceleration = acceleration - _lowPassValue;
            if (deltaAcceleration.sqrMagnitude < _sqrThreshold || time - _lastShakeTime < _cooldown)
                return false;

            _lastShakeTime = time;
            return true;
        }

        public void Reset()
        {
            _hasSample = false;
            _lastShakeTime = float.NegativeInfinity;
        }
    }
}
