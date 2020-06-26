#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.FB.EditorUtil;
using Crosstales.FB.EditorTask;

namespace Crosstales.FB.EditorIntegration
{
   /// <summary>Base class for editor windows.</summary>
   public abstract class ConfigBase : EditorWindow
   {
      #region Variables

      private System.Threading.Thread worker;

      private Vector2 scrollPosConfig;
      private Vector2 scrollPosHelp;
      private Vector2 scrollPosAboutUpdate;
      private Vector2 scrollPosAboutReadme;
      private Vector2 scrollPosAboutVersions;

      private static string readme;
      private static string versions;

      #endregion


      #region Protected methods

      protected void showConfiguration()
      {
         if (!FileBrowser.isPlatformSupported)
         {
            EditorGUILayout.HelpBox("The current platform is not supported in builds!", MessageType.Error);
         }

         scrollPosConfig = EditorGUILayout.BeginScrollView(scrollPosConfig, false, false);
         {
            GUILayout.Label("Global Settings", EditorStyles.boldLabel);

            Util.Config.DEBUG = EditorGUILayout.Toggle(new GUIContent("Debug", "Enable or disable debug logs (default: " + Util.Constants.DEFAULT_DEBUG + ")"), Util.Config.DEBUG);

            EditorConfig.UPDATE_CHECK = EditorGUILayout.Toggle(new GUIContent("Update Check", "Enable or disable the update-checks for the asset (default: " + EditorConstants.DEFAULT_UPDATE_CHECK + ")"), EditorConfig.UPDATE_CHECK);

            EditorConfig.COMPILE_DEFINES = EditorGUILayout.Toggle(new GUIContent("Compile Defines", "Enable or disable adding compile defines 'CT_FB' and 'CT_FB_PRO' for the asset (default: " + EditorConstants.DEFAULT_COMPILE_DEFINES + ")"), EditorConfig.COMPILE_DEFINES);

            GUILayout.Label("Windows", EditorStyles.boldLabel);

            Util.Config.NATIVE_WINDOWS = EditorGUILayout.Toggle(new GUIContent("Native Inside Editor", "Enable or disable native file browser inside the Unity Editor (default: " + Util.Constants.DEFAULT_NATIVE_WINDOWS + ")"), Util.Config.NATIVE_WINDOWS);
         }
         EditorGUILayout.EndScrollView();
      }

      protected void showHelp()
      {
         if (!FileBrowser.isPlatformSupported)
         {
            EditorGUILayout.HelpBox("The current platform is not supported in builds!", MessageType.Error);
         }

         scrollPosHelp = EditorGUILayout.BeginScrollView(scrollPosHelp, false, false);
         {
            GUILayout.Label("Resources", EditorStyles.boldLabel);
         }
         EditorGUILayout.EndScrollView();

         GUILayout.Space(6);
      }

      protected void showAbout()
      {
         if (!FileBrowser.isPlatformSupported)
         {
            EditorGUILayout.HelpBox("The current platform is not supported in builds!", MessageType.Error);
         }

         GUILayout.Space(3);
         GUILayout.Label(Util.Constants.ASSET_NAME, EditorStyles.boldLabel);

         GUILayout.BeginHorizontal();
         {
         }
         GUILayout.EndHorizontal();

         GUILayout.Space(6);
      }

      protected static void save()
      {
         Util.Config.Save();
         EditorConfig.Save();

         if (Util.Config.DEBUG)
            Debug.Log("Config data saved");
      }

      #endregion
   }
}
#endif
// © 2019 crosstales LLC (https://www.crosstales.com)
