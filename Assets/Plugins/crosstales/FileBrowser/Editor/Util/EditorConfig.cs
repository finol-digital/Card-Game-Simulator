#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.FB.EditorUtil
{
   /// <summary>Editor configuration for the asset.</summary>
   [InitializeOnLoad]
   public static class EditorConfig
   {
      #region Variables

      /// <summary>Enable or disable update-checks for the asset.</summary>
      public static bool UPDATE_CHECK = EditorConstants.DEFAULT_UPDATE_CHECK;

      /// <summary>Is the configuration loaded?</summary>
      public static bool isLoaded = false;

      private static string assetPath = null;
      private const string idPath = "Documentation/id/";
      private static readonly string idName = EditorConstants.ASSET_UID + ".txt";

      #endregion


      #region Constructor

      static EditorConfig()
      {
         if (!isLoaded)
         {
            Load();
         }
      }

      #endregion


      #region Properties

      /// <summary>Returns the path to the asset inside the Unity project.</summary>
      /// <returns>The path to the asset inside the Unity project.</returns>
      public static string ASSET_PATH
      {
         get
         {
            if (assetPath == null)
            {
               try
               {
                  if (System.IO.File.Exists(Application.dataPath + EditorConstants.DEFAULT_ASSET_PATH + idPath + idName))
                  {
                     assetPath = EditorConstants.DEFAULT_ASSET_PATH;
                  }
                  else
                  {
                     string[] files = System.IO.Directory.GetFiles(Application.dataPath, idName, System.IO.SearchOption.AllDirectories);

                     if (files.Length > 0)
                     {
                        string name = files[0].Substring(Application.dataPath.Length);
                        assetPath = name.Substring(0, name.Length - idPath.Length - idName.Length).Replace("\\", "/");
                     }
                     else
                     {
                        Debug.LogWarning("Could not locate the asset! File not found: " + idName);
                        assetPath = EditorConstants.DEFAULT_ASSET_PATH;
                     }
                  }

                  Common.Util.CTPlayerPrefs.SetString(Util.Constants.KEY_ASSET_PATH, assetPath);
                  Common.Util.CTPlayerPrefs.Save();
               }
               catch (System.Exception ex)
               {
                  Debug.LogWarning("Could not locate asset: " + ex);
               }
            }

            return assetPath;
         }
      }

      #endregion


      #region Public static methods

      /// <summary>Resets all changeable variables to their default value.</summary>
      public static void Reset()
      {
         UPDATE_CHECK = EditorConstants.DEFAULT_UPDATE_CHECK;
      }

      /// <summary>Loads the all changeable variables.</summary>
      public static void Load()
      {
         if (Common.Util.CTPlayerPrefs.HasKey(EditorConstants.KEY_UPDATE_CHECK))
         {
            UPDATE_CHECK = Common.Util.CTPlayerPrefs.GetBool(EditorConstants.KEY_UPDATE_CHECK);
         }

         isLoaded = true;
      }

      /// <summary>Saves the all changeable variables.</summary>
      public static void Save()
      {
         Common.Util.CTPlayerPrefs.SetBool(EditorConstants.KEY_UPDATE_CHECK, UPDATE_CHECK);

         Common.Util.CTPlayerPrefs.Save();
      }

      #endregion
   }
}
#endif
// © 2017-2020 crosstales LLC (https://www.crosstales.com)