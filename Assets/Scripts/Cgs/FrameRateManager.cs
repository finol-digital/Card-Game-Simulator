/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_ANDROID || UNITY_IOS
using System;
using System.Linq;
#endif
using UnityEngine;

namespace Cgs
{
    public class FrameRateManager : MonoBehaviour
    {
        // These options are reiterated in the Settings dropdown
        private static readonly string[] SupportedFrameRates =
        {
            "30",
            "60",
            "120",
            "240",
            "Uncapped"
        };

#if UNITY_ANDROID || UNITY_IOS
        private static int DefaultFrameRateIndex => SupportedFrameRates.Contains(Application.targetFrameRate.ToString())
            ? Array.IndexOf(SupportedFrameRates, Application.targetFrameRate.ToString())
            : 0;
#else
        private const int DefaultFrameRateIndex = 1;
#endif

        private const string PlayerPrefsFrameRateIndex = "FrameRateIndex";

        public static int FrameRateIndex
        {
            get => PlayerPrefs.GetInt(PlayerPrefsFrameRateIndex, DefaultFrameRateIndex);
            set
            {
                if (value < 0 || value >= SupportedFrameRates.Length)
                {
                    Debug.LogError($"Error: Attempted to set frame rate index to unsupported value {value}!");
                    return;
                }

                PlayerPrefs.SetInt(PlayerPrefsFrameRateIndex, value);
                Application.targetFrameRate = FrameRate;
            }
        }

        private static int FrameRate
        {
            get
            {
                var frameRateIndex = FrameRateIndex;
                if (frameRateIndex < 0 || frameRateIndex >= SupportedFrameRates.Length)
                {
                    Debug.LogError($"Error: Attempted to get unsupported frame rate index {frameRateIndex}!");
                    return Application.targetFrameRate;
                }

                var frameRate = SupportedFrameRates[frameRateIndex];

                if (int.TryParse(frameRate, out var targetFrameRate))
                    return targetFrameRate;

                return -1;
            }
        }

        private void Start()
        {
            Application.targetFrameRate = FrameRate;
        }
    }
}
