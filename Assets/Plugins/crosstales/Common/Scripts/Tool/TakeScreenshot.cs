using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Take screen shots inside an application.</summary>
   [DisallowMultipleComponent]
   public class TakeScreenshot : MonoBehaviour
   {
      #region Variables

      ///<summary>Prefix for the generate file names.</summary>
      [Tooltip("Prefix for the generate file names.")] public string Prefix = "CT_Screenshot";

      ///<summary>Factor by which to increase resolution (default: 1).</summary>
      [Tooltip("Factor by which to increase resolution (default: 1).")] public int Scale = 1;

      ///<summary>Key-press to capture the screen (default: F8).</summary>
      [Tooltip("Key-press to capture the screen (default: F8).")] public KeyCode KeyCode = KeyCode.F8;

      ///<summary>Show file location (default: true).</summary>
      [Tooltip("Show file location (default: true).")] public bool ShowFileLocation = true;

      private Texture2D texture;
      private bool locationShown;

      #endregion

#if (!UNITY_WSA && !UNITY_WEBGL) || UNITY_EDITOR

      #region MonoBehaviour methods

      public void Start()
      {
         DontDestroyOnLoad(transform.root.gameObject);
      }

      public void Update()
      {
         if (Input.GetKeyDown(KeyCode))
         {
            Capture();
         }
      }

      #endregion


      #region Public methods

      ///<summary>Capture the screen.</summary>
      public void Capture()
      {
         string file = Application.persistentDataPath + System.IO.Path.DirectorySeparatorChar + Prefix + System.DateTime.Now.ToString("_dd-MM-yyyy-HH-mm-ss-f") + ".png";

         ScreenCapture.CaptureScreenshot(file, Scale);

         Debug.Log("Screenshot saved: " + file);

         if (!locationShown && ShowFileLocation)
         {
            BaseHelper.ShowFileLocation(file);
            locationShown = true;
         }
      }

      #endregion

#else
        public void Start()
        {
            Debug.LogWarning("'TakeScreenshot' doesn't work with the current platform!");
        }
#endif
   }
}
// © 2014-2020 crosstales LLC (https://www.crosstales.com)