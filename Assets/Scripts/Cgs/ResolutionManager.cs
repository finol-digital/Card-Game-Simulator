/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs
{
    public class ResolutionManager : MonoBehaviour
    {
        public static Vector2 Resolution
        {
            get
            {
                var resolutionIndex = ResolutionIndex;
                if (resolutionIndex < 0 || resolutionIndex >= SupportedResolutions.Length)
                {
                    Debug.LogError($"Error: Attempted to get unsupported resolution index {resolutionIndex}!");
                    return DefaultResolution;
                }

                var resolution = SupportedResolutions[resolutionIndex]
                    .Split(new[] {ResolutionSplitter}, StringSplitOptions.None);

                if (resolution.Length == 2 && int.TryParse(resolution[0], out var width) &&
                    int.TryParse(resolution[1], out var height))
                    return new Vector2(width, height);

                Debug.LogError($"Error: Failed to parse resolution {resolution}!");
                return DefaultResolution;
            }
        }

        private const string PlayerPrefsResolutionIndex = "ResolutionIndex";
        private const string ResolutionSplitter = " x ";

#if UNITY_ANDROID || UNITY_IOS
        private const int DefaultResolutionIndex = 0;
        private static readonly Vector2 DefaultResolution = new Vector2(1920, 1080);
#else
        private const int DefaultResolutionIndex = 2;
        private static readonly Vector2 DefaultResolution = new Vector2(3200, 1800);
#endif

        // These options are reiterated in the Settings dropdown
        private static readonly string[] SupportedResolutions =
        {
            "1080 x 1920",
            "2560 x 1440",
            "3200 x 1800",
            "3840 x 2160"
        };

        public static int ResolutionIndex
        {
            get => PlayerPrefs.GetInt(PlayerPrefsResolutionIndex, DefaultResolutionIndex);
            set
            {
                if (value < 0 || value >= SupportedResolutions.Length)
                {
                    Debug.LogError($"Error: Attempted to set resolution index to unsupported value {value}!");
                    return;
                }

                PlayerPrefs.SetInt(PlayerPrefsResolutionIndex, value);
                ScaleResolution();
            }
        }

        public static void ScaleResolution()
        {
            foreach (var canvasScaler in FindObjectsOfType<CanvasScaler>())
                canvasScaler.referenceResolution = Resolution;
        }
    }
}
