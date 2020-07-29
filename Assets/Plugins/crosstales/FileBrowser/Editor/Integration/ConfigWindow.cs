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

            EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
      }
   }
}
#endif
// © 2019-2020 crosstales LLC (https://www.crosstales.com)
