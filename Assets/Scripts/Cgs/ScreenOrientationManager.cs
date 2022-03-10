/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace Cgs
{
    public enum ScreenOrientationPref
    {
        OsControl,
        AutoRotate,
        Portrait,
        Landscape
    }

    public class ScreenOrientationManager : MonoBehaviour
    {
        private const string PlayerPrefScreenOrientation = "ScreenOrientation";
        private const string PlayerPrefControllerLockToLandscape = "ControllerLockToLandscape";

        public static ScreenOrientationPref PreferredScreenOrientation
        {
            get => (ScreenOrientationPref) PlayerPrefs.GetInt(PlayerPrefScreenOrientation);
            set
            {
                if (value == PreferredScreenOrientation)
                    return;
                PlayerPrefs.SetInt(PlayerPrefScreenOrientation, (int) value);
                ResetOrientation();
            }
        }

        public static bool DoesControllerLockToLandscape
        {
            get => PlayerPrefs.GetInt(PlayerPrefControllerLockToLandscape, 0) == 1;
            set
            {
                if (value == DoesControllerLockToLandscape)
                    return;
                PlayerPrefs.SetInt(PlayerPrefControllerLockToLandscape, value ? 1 : 0);
                ResetOrientation();
            }
        }

        private static bool DoesOsWantAutoRotation
        {
            get
            {
                // ReSharper disable once ConvertToConstant.Local
                var doesOsWantAutoRotation = true;
#if UNITY_ANDROID && !UNITY_EDITOR
                using (AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
                    int rotationOn =
 systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "accelerometer_rotation");
                    doesOsWantAutoRotation = rotationOn==1;
                }
#endif
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return doesOsWantAutoRotation;
            }
        }

        private static bool IsControllerConnected =>
            Input.GetJoystickNames().Length > 0 && Input.GetJoystickNames()[0].Length > 0;

        private static bool WasControllerConnected { get; set; }

        private static void ResetOrientation()
        {
            if (DoesControllerLockToLandscape && IsControllerConnected)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = false;
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }
            else
            {
                var isAutoRotationOn = PreferredScreenOrientation == ScreenOrientationPref.AutoRotate
                                       || (PreferredScreenOrientation == ScreenOrientationPref.OsControl &&
                                           DoesOsWantAutoRotation);
                Screen.autorotateToPortrait =
                    isAutoRotationOn || PreferredScreenOrientation == ScreenOrientationPref.Portrait;
                Screen.autorotateToPortraitUpsideDown =
                    isAutoRotationOn || PreferredScreenOrientation == ScreenOrientationPref.Portrait;
                Screen.autorotateToLandscapeLeft =
                    isAutoRotationOn || PreferredScreenOrientation == ScreenOrientationPref.Landscape;
                Screen.autorotateToLandscapeRight = isAutoRotationOn;
                switch (PreferredScreenOrientation)
                {
                    case ScreenOrientationPref.Landscape:
                        Screen.orientation = ScreenOrientation.LandscapeLeft;
                        break;
                    case ScreenOrientationPref.Portrait:
                        Screen.orientation = ScreenOrientation.Portrait;
                        break;
                    case ScreenOrientationPref.OsControl:
                    case ScreenOrientationPref.AutoRotate:
                    default:
                        Screen.orientation = ScreenOrientation.AutoRotation;
                        break;
                }
            }
        }

        private void Awake()
        {
            WasControllerConnected = IsControllerConnected;
        }

        private void OnApplicationFocus(bool haveFocus)
        {
            if (haveFocus)
                ResetOrientation();
        }

        private void Update()
        {
            if (DoesControllerLockToLandscape && (IsControllerConnected != WasControllerConnected))
                ResetOrientation();
            WasControllerConnected = IsControllerConnected;
        }
    }
}
