using UnityEngine;

public class ScreenRotationManager : MonoBehaviour 
{
    void OnApplicationFocus(bool haveFocus)
    {
        if (haveFocus)
          ToggleAutoRotation();
    }

    static void ToggleAutoRotation()
    {
        AutoRotationOn = DeviceAutoRotationIsOn();
        Screen.autorotateToPortrait = AutoRotationOn;
        Screen.autorotateToPortraitUpsideDown = AutoRotationOn;
        Screen.autorotateToLandscapeLeft = AutoRotationOn;
        Screen.autorotateToLandscapeRight = AutoRotationOn;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    static bool DeviceAutoRotationIsOn()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
            AndroidJavaObject context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass systemGlobal = new AndroidJavaClass("android.provider.Settings$System");
            int rotationOn = systemGlobal.CallStatic<int>("getInt", context.Call<AndroidJavaObject>("getContentResolver"), "accelerometer_rotation");
            return rotationOn==1;
        }
#endif
        return true;
    }
}
