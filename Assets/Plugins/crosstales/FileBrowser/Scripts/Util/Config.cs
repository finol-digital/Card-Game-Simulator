namespace Crosstales.FB.Util
{
   /// <summary>Configuration for the asset.</summary>
   public static class Config
   {
      #region Changable variables

      /// <summary>Path to the asset inside the Unity project.</summary>
      public static string ASSET_PATH = "/Plugins/crosstales/FileBrowser/";

      /// <summary>Enable or disable debug logging for the asset.</summary>
      public static bool DEBUG = Constants.DEFAULT_DEBUG || Constants.DEV_DEBUG;

      /// <summary>Enable or disable native file browser inside the Unity Editor.</summary>
      public static bool NATIVE_WINDOWS = Constants.DEFAULT_NATIVE_WINDOWS;

      /// <summary>Is the configuration loaded?</summary>
      public static bool isLoaded = false;

      #endregion

#if UNITY_EDITOR

      #region Public static methods

      /// <summary>Resets all changeable variables to their default value.</summary>
      public static void Reset()
      {
         if (!Constants.DEV_DEBUG)
            DEBUG = Constants.DEFAULT_DEBUG;

         NATIVE_WINDOWS = Constants.DEFAULT_NATIVE_WINDOWS;
      }

      /// <summary>Loads the all changeable variables.</summary>
      public static void Load()
      {
         if (Common.Util.CTPlayerPrefs.HasKey(Constants.KEY_ASSET_PATH))
         {
            ASSET_PATH = Common.Util.CTPlayerPrefs.GetString(Constants.KEY_ASSET_PATH);
         }

         if (!Constants.DEV_DEBUG)
         {
            if (Common.Util.CTPlayerPrefs.HasKey(Constants.KEY_DEBUG))
            {
               DEBUG = Common.Util.CTPlayerPrefs.GetBool(Constants.KEY_DEBUG);
            }
         }
         else
         {
            DEBUG = Constants.DEV_DEBUG;
         }

         if (Common.Util.CTPlayerPrefs.HasKey(Constants.KEY_NATIVE_WINDOWS))
         {
            NATIVE_WINDOWS = Common.Util.CTPlayerPrefs.GetBool(Constants.KEY_NATIVE_WINDOWS);
         }

         isLoaded = true;
      }

      /// <summary>Saves the all changeable variables.</summary>
      public static void Save()
      {
         if (!Constants.DEV_DEBUG)
            Common.Util.CTPlayerPrefs.SetBool(Constants.KEY_DEBUG, DEBUG);

         Common.Util.CTPlayerPrefs.SetBool(Constants.KEY_NATIVE_WINDOWS, NATIVE_WINDOWS);

         Common.Util.CTPlayerPrefs.Save();
      }

      #endregion

#endif
   }
}
// © 2017-2020 crosstales LLC (https://www.crosstales.com)