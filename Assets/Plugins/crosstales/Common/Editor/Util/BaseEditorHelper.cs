#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Crosstales.Common.EditorUtil
{
   /// <summary>Base for various Editor helper functions.</summary>
   public abstract class BaseEditorHelper : Util.BaseHelper
   {
      #region Static variables

      private static readonly System.Type moduleManager = System.Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
      private static readonly System.Reflection.MethodInfo isPlatformSupportLoaded = moduleManager.GetMethod("IsPlatformSupportLoaded", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
      private static readonly System.Reflection.MethodInfo getTargetStringFromBuildTarget = moduleManager.GetMethod("GetTargetStringFromBuildTarget", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

      private static Texture2D logo_asset_bwf;
      private static Texture2D logo_asset_dj;
      private static Texture2D logo_asset_fb;
      private static Texture2D logo_asset_oc;
      private static Texture2D logo_asset_radio;
      private static Texture2D logo_asset_rtv;
      private static Texture2D logo_asset_tb;
      private static Texture2D logo_asset_tpb;
      private static Texture2D logo_asset_tps;
      private static Texture2D logo_asset_tr;

      private static Texture2D logo_ct;
      private static Texture2D logo_unity;

      private static Texture2D icon_save;
      private static Texture2D icon_reset;
      private static Texture2D icon_refresh;
      private static Texture2D icon_delete;
      private static Texture2D icon_folder;
      private static Texture2D icon_plus;
      private static Texture2D icon_minus;

      private static Texture2D icon_manual;
      private static Texture2D icon_api;
      private static Texture2D icon_forum;
      private static Texture2D icon_product;

      private static Texture2D icon_check;

      private static Texture2D social_Discord;
      private static Texture2D social_Facebook;
      private static Texture2D social_Twitter;
      private static Texture2D social_Youtube;
      private static Texture2D social_Linkedin;

      private static Texture2D video_promo;
      private static Texture2D video_tutorial;

      private static Texture2D icon_videos;

      private static Texture2D icon_3p_assets;
      private static Texture2D asset_PlayMaker;

      #endregion


      #region Static properties

      public static Texture2D Logo_Asset_BWF
      {
         get { return loadImage(ref logo_asset_bwf, "logo_asset_bwf.png"); }
      }

      public static Texture2D Logo_Asset_DJ
      {
         get { return loadImage(ref logo_asset_dj, "logo_asset_dj.png"); }
      }

      public static Texture2D Logo_Asset_FB
      {
         get { return loadImage(ref logo_asset_fb, "logo_asset_fb.png"); }
      }

      public static Texture2D Logo_Asset_OC
      {
         get { return loadImage(ref logo_asset_oc, "logo_asset_oc.png"); }
      }

      public static Texture2D Logo_Asset_Radio
      {
         get { return loadImage(ref logo_asset_radio, "logo_asset_radio.png"); }
      }

      public static Texture2D Logo_Asset_RTV
      {
         get { return loadImage(ref logo_asset_rtv, "logo_asset_rtv.png"); }
      }

      public static Texture2D Logo_Asset_TB
      {
         get { return loadImage(ref logo_asset_tb, "logo_asset_tb.png"); }
      }

      public static Texture2D Logo_Asset_TPB
      {
         get { return loadImage(ref logo_asset_tpb, "logo_asset_tpb.png"); }
      }

      public static Texture2D Logo_Asset_TPS
      {
         get { return loadImage(ref logo_asset_tps, "logo_asset_tps.png"); }
      }

      public static Texture2D Logo_Asset_TR
      {
         get { return loadImage(ref logo_asset_tr, "logo_asset_tr.png"); }
      }

      public static Texture2D Logo_CT
      {
         get { return loadImage(ref logo_ct, "logo_ct.png"); }
      }

      public static Texture2D Logo_Unity
      {
         get { return loadImage(ref logo_unity, "logo_unity.png"); }
      }

      public static Texture2D Icon_Save
      {
         get { return loadImage(ref icon_save, "icon_save.png"); }
      }

      public static Texture2D Icon_Reset
      {
         get { return loadImage(ref icon_reset, "icon_reset.png"); }
      }

      public static Texture2D Icon_Refresh
      {
         get { return loadImage(ref icon_refresh, "icon_refresh.png"); }
      }

      public static Texture2D Icon_Delete
      {
         get { return loadImage(ref icon_delete, "icon_delete.png"); }
      }

      public static Texture2D Icon_Folder
      {
         get { return loadImage(ref icon_folder, "icon_folder.png"); }
      }

      public static Texture2D Icon_Plus
      {
         get { return loadImage(ref icon_plus, "icon_plus.png"); }
      }

      public static Texture2D Icon_Minus
      {
         get { return loadImage(ref icon_minus, "icon_minus.png"); }
      }

      public static Texture2D Icon_Manual
      {
         get { return loadImage(ref icon_manual, "icon_manual.png"); }
      }

      public static Texture2D Icon_API
      {
         get { return loadImage(ref icon_api, "icon_api.png"); }
      }

      public static Texture2D Icon_Forum
      {
         get { return loadImage(ref icon_forum, "icon_forum.png"); }
      }

      public static Texture2D Icon_Product
      {
         get { return loadImage(ref icon_product, "icon_product.png"); }
      }

      public static Texture2D Icon_Check
      {
         get { return loadImage(ref icon_check, "icon_check.png"); }
      }

      public static Texture2D Social_Discord
      {
         get { return loadImage(ref social_Discord, "social_Discord.png"); }
      }

      public static Texture2D Social_Facebook
      {
         get { return loadImage(ref social_Facebook, "social_Facebook.png"); }
      }

      public static Texture2D Social_Twitter
      {
         get { return loadImage(ref social_Twitter, "social_Twitter.png"); }
      }

      public static Texture2D Social_Youtube
      {
         get { return loadImage(ref social_Youtube, "social_Youtube.png"); }
      }

      public static Texture2D Social_Linkedin
      {
         get { return loadImage(ref social_Linkedin, "social_Linkedin.png"); }
      }

      public static Texture2D Video_Promo
      {
         get { return loadImage(ref video_promo, "video_promo.png"); }
      }

      public static Texture2D Video_Tutorial
      {
         get { return loadImage(ref video_tutorial, "video_tutorial.png"); }
      }

      public static Texture2D Icon_Videos
      {
         get { return loadImage(ref icon_videos, "icon_videos.png"); }
      }

      public static Texture2D Icon_3p_Assets
      {
         get { return loadImage(ref icon_3p_assets, "icon_3p_assets.png"); }
      }

      public static Texture2D Asset_PlayMaker
      {
         get { return loadImage(ref asset_PlayMaker, "asset_PlayMaker.png"); }
      }

      #endregion


      #region Public methods

      /// <summary>Restart Unity.</summary>
      /// <param name="executeMethod">Executed method after the restart (optional)</param>
      public static void RestartUnity(string executeMethod = "")
      {
         UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

         bool success = false;
         string scriptfile;

         using (System.Diagnostics.Process process = new System.Diagnostics.Process())
         {
            try
            {
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;

               switch (Application.platform)
               {
                  case RuntimePlatform.WindowsEditor:
                     scriptfile = System.IO.Path.GetTempPath() + "RestartUnity-" + System.Guid.NewGuid() + ".cmd";

                     System.IO.File.WriteAllText(scriptfile, generateWindowsRestartScript(executeMethod));

                     process.StartInfo.FileName = "cmd.exe";
                     process.StartInfo.Arguments = "/c start  \"\" " + '"' + scriptfile + '"';
                     break;
                  case RuntimePlatform.OSXEditor:
                     scriptfile = System.IO.Path.GetTempPath() + "RestartUnity-" + System.Guid.NewGuid() + ".sh";

                     System.IO.File.WriteAllText(scriptfile, generateMacRestartScript(executeMethod));

                     process.StartInfo.FileName = "/bin/sh";
                     process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                     break;
                  case RuntimePlatform.LinuxEditor:
                     scriptfile = System.IO.Path.GetTempPath() + "RestartUnity-" + System.Guid.NewGuid() + ".sh";

                     System.IO.File.WriteAllText(scriptfile, generateLinuxRestartScript(executeMethod));

                     process.StartInfo.FileName = "/bin/sh";
                     process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                     break;
                  default:
                     Debug.LogError("Unsupported Unity Editor: " + Application.platform);
                     return;
               }

               process.Start();

               if (isWindowsEditor)
                  process.WaitForExit(Util.BaseConstants.PROCESS_KILL_TIME);

               success = true;
            }
            catch (System.Exception ex)
            {
               string errorMessage = "Could restart Unity!" + System.Environment.NewLine + ex;
               Debug.LogError(errorMessage);
            }
         }

         if (success)
            EditorApplication.Exit(0);
      }

      /// <summary>Shows a separator-UI.</summary>
      /// <param name="space">Space in pixels between the component and the separator line (default: 12, optional).</param>
      public static void SeparatorUI(int space = 12)
      {
         GUILayout.Space(space);
         GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(1));
      }

      /// <summary>Generates a read-only text field with a label.</summary>
      public static void ReadOnlyTextField(string label, string text)
      {
         EditorGUILayout.BeginHorizontal();
         {
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 4));
            EditorGUILayout.SelectableLabel(text, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
         }
         EditorGUILayout.EndHorizontal();
      }

      /// <summary>Refreshes the asset database.</summary>
      /// <param name="options">Asset import options (default: ImportAssetOptions.Default, optional).</param>
      public static void RefreshAssetDatabase(ImportAssetOptions options = ImportAssetOptions.Default)
      {
         if (isEditorMode)
         {
            AssetDatabase.Refresh(options);
         }
      }

      /// <summary>Invokes a public static method on a full qualified class.</summary>
      /// <param name="className">Full qualified name of the class</param>
      /// <param name="methodName">Public static method of the class to execute</param>
      /// <param name="parameters">Parameters for the method (optional)</param>
      public static void InvokeMethod(string className, string methodName, params object[] parameters)
      {
         if (string.IsNullOrEmpty(className))
         {
            Debug.LogWarning("'className' is null or empty; can not execute.");
            return;
         }

         if (string.IsNullOrEmpty(methodName))
         {
            Debug.LogWarning("'methodName' is null or empty; can not execute.");
            return;
         }

         foreach (var type in System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()))
         {
            try
            {
               if (type.FullName != null && type.FullName.Equals(className))
               {
                  if (type.IsClass)
                  {
                     type.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Invoke(null, parameters);
                  }
               }
            }
            catch (System.Exception ex)
            {
               Debug.LogWarning("Could not execute method call: " + ex);
            }
         }
      }

      /// <summary>Returns the true if the BuildTarget is installed in Unity.</summary>
      /// <param name="target">BuildTarget to test</param>
      /// <returns>True if the BuildTarget is installed in Unity.</returns>
      public static bool isValidBuildTarget(BuildTarget target)
      {
         return (bool)isPlatformSupportLoaded.Invoke(null, new object[] {(string)getTargetStringFromBuildTarget.Invoke(null, new object[] {target})});
      }

      /*
      public static IEnumerable<BuildTarget> GetAvailableBuildTargets()
      {
          foreach (BuildTarget target in (BuildTarget[])Enum.GetValues(typeof(BuildTarget)))
          {
              BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
              if (BuildPipeline.IsBuildTargetSupported(group, target))
              {
                  yield return target;
              }
          }
      }
      */

      /// <summary>Returns an argument for a name from the command line.</summary>
      /// <param name="name">Name for the argument</param>
      /// <returns>True if the BuildTarget is installed in Unity.</returns>
      public static string getCLIArgument(string name)
      {
         string[] args = System.Environment.GetCommandLineArgs();

         for (int ii = 0; ii < args.Length; ii++)
         {
            if (name.CTEquals(args[ii]) && args.Length > ii + 1)
               return args[ii + 1];
         }

         return null;
      }

      /// <summary>Returns the BuildTarget for a build name, like 'win64'.</summary>
      /// <param name="build">Build name, like 'win64'</param>
      /// <returns>The BuildTarget for a build name.</returns>
      public static BuildTarget getBuildTargetForBuildName(string build)
      {
         if ("win32".CTEquals(build) || "win".CTEquals(build))
         {
            return BuildTarget.StandaloneWindows;
         }

         if ("win64".CTEquals(build))
         {
            return BuildTarget.StandaloneWindows64;
         }

         if (!string.IsNullOrEmpty(build) && build.CTContains("osx"))
         {
            return BuildTarget.StandaloneOSX;
         }
#if UNITY_2019_2_OR_NEWER
            if (!string.IsNullOrEmpty(build) && build.CTContains("linux"))
            {
                return BuildTarget.StandaloneLinux64;
            }
#else
         if ("linux".CTEquals(build))
         {
            return BuildTarget.StandaloneLinux;
         }

         if ("linux64".CTEquals(build))
         {
            return BuildTarget.StandaloneLinux64;
         }

         if ("linuxuniversal".CTEquals(build))
         {
            return BuildTarget.StandaloneLinuxUniversal;
         }
#endif
         if ("android".CTEquals(build))
         {
            return BuildTarget.Android;
         }

         if ("ios".CTEquals(build))
         {
            return BuildTarget.iOS;
         }

         if ("wsaplayer".CTEquals(build) || "WindowsStoreApps".CTEquals(build))
         {
            return BuildTarget.WSAPlayer;
         }

         if ("webgl".CTEquals(build))
         {
            return BuildTarget.WebGL;
         }

         if ("tvOS".CTEquals(build))
         {
            return BuildTarget.tvOS;
         }

         if ("ps4".CTEquals(build))
         {
            return BuildTarget.PS4;
         }
#if !UNITY_2018_2_OR_NEWER
         if ("psp2".CTEquals(build))
         {
            return BuildTarget.PSP2;
         }
#endif
         if ("xboxone".CTEquals(build))
         {
            return BuildTarget.XboxOne;
         }
#if !UNITY_2018_1_OR_NEWER
         if ("wiiu".CTEquals(build))
         {
            return BuildTarget.WiiU;
         }
#endif
#if !UNITY_2018_2_OR_NEWER
         if ("N3DS".CTEquals(build))
         {
            return BuildTarget.N3DS;
         }
#endif
         if ("switch".CTEquals(build))
         {
            return BuildTarget.Switch;
         }

         Debug.LogWarning("Build target '" + build + "' not found! Returning 'StandaloneWindows64'.");
         return BuildTarget.StandaloneWindows64;
      }

      /// <summary>Returns the build name for a BuildTarget.</summary>
      /// <param name="build">BuildTarget for a build name</param>
      /// <returns>The build name for a BuildTarget.</returns>
      public static string getBuildNameFromBuildTarget(BuildTarget build)
      {
         switch (build)
         {
            case BuildTarget.StandaloneWindows:
               return "Win";
            case BuildTarget.StandaloneWindows64:
               return "Win64";
            case BuildTarget.StandaloneOSX:
               return "OSXUniversal";
#if UNITY_2019_2_OR_NEWER
                case BuildTarget.StandaloneLinux64:
                    return "Linux64";
#else
            case BuildTarget.StandaloneLinux:
               return "Linux";
            case BuildTarget.StandaloneLinux64:
               return "Linux64";
            case BuildTarget.StandaloneLinuxUniversal:
               return "LinuxUniversal";
#endif
            case BuildTarget.Android:
               return "Android";
            case BuildTarget.iOS:
               return "iOS";
            case BuildTarget.WSAPlayer:
               return "WindowsStoreApps";
            case BuildTarget.WebGL:
               return "WebGL";
            case BuildTarget.tvOS:
               return "tvOS";
            case BuildTarget.PS4:
               return "PS4";
#if !UNITY_2018_2_OR_NEWER
            case BuildTarget.PSP2:
               return "PSP2";
#endif
            case BuildTarget.XboxOne:
               return "XboxOne";
#if !UNITY_2018_1_OR_NEWER
            case BuildTarget.WiiU:
               return "WiiU";
#endif
#if !UNITY_2018_2_OR_NEWER
            case BuildTarget.N3DS:
               return "N3DS";
#endif
            case BuildTarget.Switch:
               return "Switch";
            default:
               Debug.LogWarning("Build target '" + build + "' not found! Returning Windows standalone.");
               return "Win64";
         }
      }

      /// <summary>Returns assets for a certain type.</summary>
      /// <returns>List of assets for a certain type.</returns>
      public static System.Collections.Generic.List<T> FindAssetsByType<T>() where T : Object
      {
         string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
         return guids.Select(t => AssetDatabase.GUIDToAssetPath(t)).Select(assetPath => AssetDatabase.LoadAssetAtPath<T>(assetPath)).Where(asset => asset != null).ToList();
      }

      #endregion


      #region Private methods

      private static string generateWindowsRestartScript(string executeMethod)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("@echo off");
         sb.AppendLine("cls");

         // title
         sb.Append("title Restart of ");
         sb.Append(Application.productName);
         sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!");

         // header
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  Common 2020.1.0 - Windows                                                 #");
         sb.AppendLine("echo #  Copyright 2018-2020 by www.crosstales.com                                 #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo #  This script restarts Unity.                                               #");
         sb.AppendLine("echo #  This will take some time, so please be patient and DON'T CLOSE THIS       #");
         sb.AppendLine("echo #  WINDOW before the process is finished!                                    #");
         sb.AppendLine("echo #                                                                            #");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo " + Application.productName);
         sb.AppendLine("echo.");
         sb.AppendLine("echo.");

         // check if Unity is closed
         sb.AppendLine(":waitloop");
         sb.Append("if not exist \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend");
         sb.AppendLine();
         sb.AppendLine("echo.");
         sb.AppendLine("echo Waiting for Unity to close...");
         sb.AppendLine("timeout /t 3");
         /*
#if UNITY_2018_2_OR_NEWER
                     sb.Append("del \"");
                     sb.Append(Constants.PATH);
                     sb.Append("Temp\\UnityLockfile\" /q");
                     sb.AppendLine();
#endif
         */
         sb.AppendLine("goto waitloop");
         sb.AppendLine(":waitloopend");

         // Restart Unity
         sb.AppendLine("echo.");
         sb.AppendLine("echo ##############################################################################");
         sb.AppendLine("echo #  Restarting Unity                                                          #");
         sb.AppendLine("echo ##############################################################################");
         sb.Append("start \"\" \"");
         sb.Append(ValidatePath(EditorApplication.applicationPath, false));
         sb.Append("\" -projectPath \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH.Substring(0, Util.BaseConstants.APPLICATION_PATH.Length - 1));
         sb.Append("\"");

         if (!string.IsNullOrEmpty(executeMethod))
         {
            sb.Append(" -executeMethod ");
            sb.Append(executeMethod);
         }

         sb.AppendLine();
         sb.AppendLine("echo.");

         // check if Unity is started
         sb.AppendLine(":waitloop2");
         sb.Append("if exist \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp\\UnityLockfile\" goto waitloopend2");
         sb.AppendLine();
         sb.AppendLine("echo Waiting for Unity to start...");
         sb.AppendLine("timeout /t 3");
         sb.AppendLine("goto waitloop2");
         sb.AppendLine(":waitloopend2");
         sb.AppendLine("echo.");
         sb.AppendLine("echo Bye!");
         sb.AppendLine("timeout /t 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static string generateMacRestartScript(string executeMethod)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='Relaunch of ");
         sb.Append(Application.productName);
         sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Common 2020.1.0 - macOS                                                   ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2020 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  This script restarts Unity.                                               ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"" + Application.productName + "\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");
         /*
#if UNITY_2018_2_OR_NEWER
                     sb.Append("  rm \"");
                     sb.Append(Constants.PATH);
                     sb.Append("Temp/UnityLockfile\"");
                     sb.AppendLine();
#endif
         */
         sb.AppendLine("done");

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append("open -a \"");
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("\"");

         if (!string.IsNullOrEmpty(executeMethod))
         {
            sb.Append(" -executeMethod ");
            sb.Append(executeMethod);
         }

         sb.AppendLine();

         //check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static string generateLinuxRestartScript(string executeMethod)
      {
         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         // setup
         sb.AppendLine("#!/bin/bash");
         sb.AppendLine("set +v");
         sb.AppendLine("clear");

         // title
         sb.Append("title='Relaunch of ");
         sb.Append(Application.productName);
         sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!'");
         sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

         // header
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  Common 2020.1.0 - Linux                                                   ¦\"");
         sb.AppendLine("echo \"¦  Copyright 2018-2020 by www.crosstales.com                                 ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"¦  This script restarts Unity.                                               ¦\"");
         sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
         sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
         sb.AppendLine("echo \"¦                                                                            ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"" + Application.productName + "\"");
         sb.AppendLine("echo");
         sb.AppendLine("echo");

         // check if Unity is closed
         sb.Append("while [ -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to close...\"");
         sb.AppendLine("  sleep 3");
         /*
#if UNITY_2018_2_OR_NEWER
                     sb.Append("  rm \"");
                     sb.Append(Constants.PATH);
                     sb.Append("Temp/UnityLockfile\"");
                     sb.AppendLine();
#endif
         */
         sb.AppendLine("done");

         // Restart Unity
         sb.AppendLine("echo");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
         sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
         sb.Append('"');
         sb.Append(EditorApplication.applicationPath);
         sb.Append("\" --args -projectPath \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("\"");

         if (!string.IsNullOrEmpty(executeMethod))
         {
            sb.Append(" -executeMethod ");
            sb.Append(executeMethod);
         }

         sb.Append(" &");
         sb.AppendLine();

         // check if Unity is started
         sb.AppendLine("echo");
         sb.Append("while [ ! -f \"");
         sb.Append(Util.BaseConstants.APPLICATION_PATH);
         sb.Append("Temp/UnityLockfile\" ]");
         sb.AppendLine();
         sb.AppendLine("do");
         sb.AppendLine("  echo \"Waiting for Unity to start...\"");
         sb.AppendLine("  sleep 3");
         sb.AppendLine("done");
         sb.AppendLine("echo");
         sb.AppendLine("echo \"Bye!\"");
         sb.AppendLine("sleep 1");
         sb.AppendLine("exit");

         return sb.ToString();
      }

      private static Texture2D loadImage(ref Texture2D logo, string fileName)
      {
         if (logo == null)
         {
#if CT_DEVELOP
            logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Plugins/crosstales/Common/Icons/" + fileName, typeof(Texture2D));
#else
                logo = (Texture2D)EditorGUIUtility.Load("crosstales/Common/" + fileName);
#endif

            if (logo == null)
            {
               Debug.LogWarning("Image not found: " + fileName);
            }
         }

         return logo;
      }

      #endregion


      /*
// compress the folder into a ZIP file, uses https://github.com/r2d2rigo/dotnetzip-for-unity
static void CompressDirectory(string directory, string zipFileOutputPath)
{
  Debug.Log("attempting to compress " + directory + " into " + zipFileOutputPath);
  // display fake percentage, I can't get zip.SaveProgress event handler to work for some reason, whatever
  EditorUtility.DisplayProgressBar("COMPRESSING... please wait", zipFileOutputPath, 0.38f);
  using (ZipFile zip = new ZipFile())
  {
      zip.ParallelDeflateThreshold = -1; // DotNetZip bugfix that corrupts DLLs / binaries http://stackoverflow.com/questions/15337186/dotnetzip-badreadexception-on-extract
      zip.AddDirectory(directory);
      zip.Save(zipFileOutputPath);
  }
  EditorUtility.ClearProgressBar();
}
*/
   }
}
#endif
// © 2018-2020 crosstales LLC (https://www.crosstales.com)