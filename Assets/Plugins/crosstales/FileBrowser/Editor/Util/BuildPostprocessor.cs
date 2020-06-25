#if UNITY_EDITOR && UNITY_STANDALONE_OSX //|| CT_DEVELOP
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using Crosstales.FB.Util;

namespace Crosstales.FB.EditorUtil
{
   /// <summary>BuildPostprocessor for macOS.</summary>
   public class BuildPostprocessor
   {
      private const string id = "com.crosstales.fb.macOS";

      [PostProcessBuildAttribute(1)]
      public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
      {
         if (EditorHelper.isMacOSPlatform)
         {
            //remove all meta-files
            string[] files = Helper.GetFiles(pathToBuiltProject, true, "meta");

            try
            {
               foreach (string file in files)
               {
                  //Debug.Log(file);
                  System.IO.File.Delete(file);
               }
            }
            catch (System.Exception ex)
            {
               Debug.Log("Could not delete files: " + ex);
            }

            //rewrite Info.plist
            files = Helper.GetFiles(pathToBuiltProject, true, "plist");

            try
            {
               foreach (string file in files)
               {
                  string content = System.IO.File.ReadAllText(file);

                  if (content.Contains(id))
                  {
                     content = content.Replace(id, id + "." + System.DateTime.Now.ToString("yyyyMMddHHmmss"));
                     System.IO.File.WriteAllText(file, content);
                  }
               }
            }
            catch (System.Exception ex)
            {
               Debug.Log("Could not rewrite 'Info.plist' files: " + ex);
            }
         }
      }
   }
}
#endif
// © 2015-2020 crosstales LLC (https://www.crosstales.com)