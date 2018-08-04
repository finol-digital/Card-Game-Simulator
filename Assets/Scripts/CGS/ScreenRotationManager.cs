using UnityEngine;

namespace CGS
{
    public class ScreenRotationManager : MonoBehaviour
    {
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

        public static bool IsAutoRotationOn
        {
            get
            {
                bool isAutoRotationOn = true;
#if UNITY_ANDROID && !UNITY_EDITOR
			using (AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
				AndroidJavaObject context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
				int rotationOn = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "accelerometer_rotation");
				isAutoRotationOn = rotationOn==1;
			}
#endif
                return isAutoRotationOn;
            }
        }
    }
}
