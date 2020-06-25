#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Crosstales.FB.EditorUtil;

namespace Crosstales.FB.EditorIntegration
{
   /// <summary>Editor window extension.</summary>
   //[InitializeOnLoad]
   public class ConfigWindow : ConfigBase
   {
      #region Variables

      private int tab = 0;
      private int lastTab = 0;
      private string path;

      #endregion


      #region EditorWindow methods

      [MenuItem("Window/" + Util.Constants.ASSET_NAME, false, 1010)]
      public static void ShowWindow()
      {
         GetWindow(typeof(ConfigWindow));
      }

      public static void ShowWindow(int tab)
      {
         ConfigWindow window = GetWindow(typeof(ConfigWindow)) as ConfigWindow;
         if (window != null) window.tab = tab;
      }

      public void OnEnable()
      {
         titleContent = new GUIContent(Util.Constants.ASSET_NAME_SHORT, EditorHelper.Logo_Asset_Small);
      }

      public void OnDestroy()
      {
         save();
      }

      public void OnLostFocus()
      {
         save();
      }

      public void OnGUI()
      {
         tab = GUILayout.Toolbar(tab, new[] {"Configuration", "Test-Drive", "Help", "About"});

         if (tab != lastTab)
         {
            lastTab = tab;
            GUI.FocusControl(null);
         }

         switch (tab)
         {
            case 0:
            {
               showConfiguration();

               EditorHelper.SeparatorUI();

               GUILayout.BeginHorizontal();
               {
                  if (GUILayout.Button(new GUIContent(" Save", EditorHelper.Icon_Save, "Saves the configuration settings for this project.")))
                  {
                     save();
                  }

                  if (GUILayout.Button(new GUIContent(" Reset", EditorHelper.Icon_Reset, "Resets the configuration settings for this project.")))
                  {
                     if (EditorUtility.DisplayDialog("Reset configuration?", "Reset the configuration of " + Util.Constants.ASSET_NAME + "?", "Yes", "No"))
                     {
                        Util.Config.Reset();
                        EditorConfig.Reset();
                        save();
                     }
                  }
               }
               GUILayout.EndHorizontal();

               GUILayout.Space(6);
               break;
            }
            case 1:
               showTestDrive();
               break;
            case 2:
               showHelp();
               break;
            default:
               showAbout();
               break;
         }
      }

      public void OnInspectorUpdate()
      {
         Repaint();
      }

      #endregion

      private void showTestDrive()
      {
         GUILayout.Space(3);
         GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

         if (Util.Helper.isEditorMode)
         {
            GUILayout.Space(6);

            if (GUILayout.Button(new GUIContent(" Open Single File", EditorUtil.EditorHelper.Icon_File, "Opens a single file.")))
            {
               path = FileBrowser.OpenSingleFile();
            }

            GUILayout.Space(6);

            if (GUILayout.Button(new GUIContent(" Open Single Folder", EditorUtil.EditorHelper.Icon_Folder, "Opens a single folder.")))
            {
               path = FileBrowser.OpenSingleFolder();
            }

            GUILayout.Space(6);

            if (GUILayout.Button(new GUIContent(" Save File", EditorUtil.EditorHelper.Icon_Save, "Saves a file.")))
            {
               path = FileBrowser.SaveFile();
            }

            GUILayout.Space(6);

            GUILayout.Label("Path: " + (string.IsNullOrEmpty(path) ? "nothing selected" : path));

            GUILayout.Space(6);
         }
         else
         {
            EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
         }
      }
   }
}
#endif
// © 2019-2020 crosstales LLC (https://www.crosstales.com)