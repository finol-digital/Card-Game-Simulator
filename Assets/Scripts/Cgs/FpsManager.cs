/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs
{
    public class FpsManager : MonoBehaviour
    {
        // These options are reiterated in the Settings dropdown
        private static readonly string[] SupportedFps = {
            "30",
            "60",
            "120",
            "240",
            "Uncapped"
        };

#if UNITY_ANDROID || UNITY_IOS
        private const int DefaultFpsIndex = 0;
#else
        private const int DefaultFpsIndex = 1;
#endif

        private const string PlayerPrefsFpsIndex = "FpsIndex";

        public static int FpsIndex
        {
            get => PlayerPrefs.GetInt(PlayerPrefsFpsIndex, DefaultFpsIndex);
            set
            {
                if (value < 0 || value >= SupportedFps.Length)
                {
                    Debug.LogError($"Error: Attempted to set resolution index to unsupported value {value}!");
                    return;
                }

                PlayerPrefs.SetInt(PlayerPrefsFpsIndex, value);
                Application.targetFrameRate = Fps;
            }
        }

        private static int Fps
        {
            get
            {
                var fpsIndex = FpsIndex;
                if (fpsIndex < 0 || fpsIndex >= SupportedFps.Length)
                {
                    Debug.LogError($"Error: Attempted to get unsupported fps index {fpsIndex}!");
                    return Application.targetFrameRate;
                }

                var fps = SupportedFps[fpsIndex];

                if (int.TryParse(fps, out var targetFps))
                    return targetFps;

                return -1;
            }
        }
    }
}
