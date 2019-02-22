/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace CGS
{
    public enum ScreenOrientationPref : int
    {
        OSControl,
        AutoRotate,
        Portrait,
        Landscape
    }

    public class ScreenOrientationManager : MonoBehaviour
    {
        public const string PlayerPrefScreenOrientation = "ScreenOrientation";
        public const string PlayerPrefControllerLockToLandscape = "ControllerLockToLandscape";

        public static ScreenOrientationPref PreferredScreenOrientation
        {
            get { return (ScreenOrientationPref)PlayerPrefs.GetInt(PlayerPrefScreenOrientation); }
            set
            {
                if (value == PreferredScreenOrientation)
                    return;
                PlayerPrefs.SetInt(PlayerPrefScreenOrientation, (int)value);
                ResetOrientation();
            }
        }

        public static bool DoesControllerLockToLandscape
        {
            get { return PlayerPrefs.GetInt(PlayerPrefControllerLockToLandscape, 1) == 1; }
            set
            {
                if (value == DoesControllerLockToLandscape)
                    return;
                PlayerPrefs.SetInt(PlayerPrefControllerLockToLandscape, value ? 1 : 0);
                ResetOrientation();
            }
        }

        public static bool DoesOSWantAutoRotation
        {
            get
            {
                bool doesOSWantAutoRotation = true;
#if UNITY_ANDROID && !UNITY_EDITOR
                using (AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
                    int rotationOn = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "accelerometer_rotation");
                    doesOSWantAutoRotation = rotationOn==1;
                }
#endif
                return doesOSWantAutoRotation;
            }
        }

        public static bool IsControllerConnected => Input.GetJoystickNames().Length > 0;
        public static bool WasControllerConnected { get; private set; }

        public static void ResetOrientation()
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
                bool autoRotationOn = PreferredScreenOrientation == ScreenOrientationPref.AutoRotate
                    || (PreferredScreenOrientation == ScreenOrientationPref.OSControl && DoesOSWantAutoRotation);
                Screen.autorotateToPortrait = autoRotationOn || PreferredScreenOrientation == ScreenOrientationPref.Portrait;
                Screen.autorotateToPortraitUpsideDown = autoRotationOn || PreferredScreenOrientation == ScreenOrientationPref.Portrait;
                Screen.autorotateToLandscapeLeft = autoRotationOn || PreferredScreenOrientation == ScreenOrientationPref.Landscape;
                Screen.autorotateToLandscapeRight = autoRotationOn || PreferredScreenOrientation == ScreenOrientationPref.Landscape;
                switch (PreferredScreenOrientation)
                {
                    case ScreenOrientationPref.Landscape:
                        Screen.orientation = ScreenOrientation.Landscape;
                        break;
                    case ScreenOrientationPref.Portrait:
                        Screen.orientation = ScreenOrientation.Portrait;
                        break;
                    case ScreenOrientationPref.OSControl:
                    case ScreenOrientationPref.AutoRotate:
                    default:
                        Screen.orientation = ScreenOrientation.AutoRotation;
                        break;
                }

            }
        }

        void Awake()
        {
            WasControllerConnected = IsControllerConnected;
        }

        void OnApplicationFocus(bool haveFocus)
        {
            if (haveFocus)
                ResetOrientation();
        }

        void Update()
        {
            if (DoesControllerLockToLandscape && (IsControllerConnected != WasControllerConnected))
                ResetOrientation();
            WasControllerConnected = IsControllerConnected;
        }
    }
}
