/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace CGS
{
    public class ScreenRotationManager : MonoBehaviour
    {
        public static bool IsAutoRotationOn
        {
            get
            {
                bool isAutoRotationOn = true;
#if UNITY_ANDROID && !UNITY_EDITOR
                using (AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
                    int rotationOn = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "accelerometer_rotation");
                    isAutoRotationOn = rotationOn==1;
                }
#endif
                return isAutoRotationOn;
            }
        }

        void OnApplicationFocus(bool haveFocus)
        {
            if (haveFocus)
                ToggleAutoRotation();
        }

        public static void ToggleAutoRotation()
        {
            bool autoRotationOn = IsAutoRotationOn;
            Screen.autorotateToPortrait = autoRotationOn;
            Screen.autorotateToPortraitUpsideDown = autoRotationOn;
            Screen.autorotateToLandscapeLeft = autoRotationOn;
            Screen.autorotateToLandscapeRight = autoRotationOn;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
