using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Enables or disable game objects on Android or iOS in the background.</summary>
   //[HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_common_1_1_util_1_1_background_controller.html")]
   public class BackgroundController : MonoBehaviour
   {
      #region Variables

      ///<summary>Selected objects to disable in the background for the controller.</summary>
      [Tooltip("Selected objects to disable in the background for the controller.")] public GameObject[] Objects;

      private bool isFocused;

      #endregion


      #region MonoBehaviour methods

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR //|| CT_DEVELOP
        public void Start()
        {
            isFocused = Application.isFocused;
        }

        public void FixedUpdate()
        {
            if (Application.isFocused != isFocused)
            {
                isFocused = Application.isFocused;

                if ((BaseHelper.isAndroidPlatform || BaseHelper.isIOSPlatform) && !TouchScreenKeyboard.visible)
                {
                    foreach (var go in Objects.Where(go => go != null))
                    {
                        go.SetActive(isFocused);
                    }

                    if (BaseConstants.DEV_DEBUG)
                        Debug.Log("Application.isFocused: " + isFocused);
                }
            }
        }
#endif

      #endregion
   }
}
// © 2018-2020 crosstales LLC (https://www.crosstales.com)