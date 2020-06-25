#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace Crosstales.FB.Wrapper
{
   public class FileBrowserEditor : FileBrowserBase
   {
      #region Implemented methods

      public override bool canOpenMultipleFiles
      {
         get { return false; }
      }

      public override bool canOpenMultipleFolders
      {
         get { return false; }
      }

      public override bool isPlatformSupported
      {
         get { return Util.Helper.isWindowsPlatform || Util.Helper.isMacOSPlatform || Util.Helper.isLinuxPlatform || Util.Helper.isWSABasedPlatform; }
      }

      public override string[] OpenFiles(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
      {
         if (Util.Helper.isMacOSEditor && extensions != null && extensions.Length > 1)
            Debug.LogWarning("Multiple 'extensions' are not supported in the Editor.");

         if (multiselect)
            Debug.LogWarning("'multiselect' for files is not supported in the Editor.");

         string path = string.Empty;

         path = extensions == null ? EditorUtility.OpenFilePanel(title, directory, string.Empty) : EditorUtility.OpenFilePanelWithFilters(title, directory, getFilterFromFileExtensionList(extensions));

         return string.IsNullOrEmpty(path) ? new string[0] : new[] {Util.Helper.ValidateFile(path)};
      }

      public override string[] OpenFolders(string title, string directory, bool multiselect)
      {
         if (multiselect)
            Debug.LogWarning("'multiselect' for folders is not supported in the Editor.");

         string path = EditorUtility.OpenFolderPanel(title, directory, string.Empty);

         return string.IsNullOrEmpty(path) ? new string[0] : new[] {Util.Helper.ValidatePath(path)};
      }

      public override string SaveFile(string title, string directory, string defaultName, ExtensionFilter[] extensions)
      {
         if (extensions != null && extensions.Length > 1)
            Debug.LogWarning("Multiple 'extensions' are not supported in the Editor.");

         string ext = extensions != null && extensions.Length > 0 ? extensions[0].Extensions[0].Equals("*") ? string.Empty : extensions[0].Extensions[0] : string.Empty;
         string name = string.IsNullOrEmpty(ext) ? defaultName : defaultName + "." + ext;

         return Util.Helper.ValidateFile(EditorUtility.SaveFilePanel(title, directory, name, ext));
      }

      public override void OpenFilesAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
      {
         Debug.LogWarning("'OpenFilesAsync' is running synchronously in the Editor.");
         cb.Invoke(OpenFiles(title, directory, extensions, multiselect));
      }

      public override void OpenFoldersAsync(string title, string directory, bool multiselect, Action<string[]> cb)
      {
         Debug.LogWarning("'OpenFoldersAsync' is running synchronously in the Editor.");
         cb.Invoke(OpenFolders(title, directory, multiselect));
      }

      public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
      {
         Debug.LogWarning("'SaveFileAsync' is running synchronously in the Editor.");
         cb.Invoke(SaveFile(title, directory, defaultName, extensions));
      }

      #endregion


      #region Private methods

      private static string[] getFilterFromFileExtensionList(ExtensionFilter[] extensions)
      {
         if (extensions != null && extensions.Length > 0)
         {
            string[] filters = new string[extensions.Length * 2];

            for (int ii = 0; ii < extensions.Length; ii++)
            {
               filters[ii * 2] = extensions[ii].Name;
               filters[ii * 2 + 1] = string.Join(",", extensions[ii].Extensions);
            }

            if (Util.Config.DEBUG)
               Debug.Log("getFilterFromFileExtensionList: " + filters.CTDump());

            return filters;
         }

         return new string[0];
      }

      #endregion
   }
}
#endif
// © 2017-2020 crosstales LLC (https://www.crosstales.com)