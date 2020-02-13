#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine;
using System;

namespace Crosstales.FB.Wrapper
{
   /// <summary>File browser implementation for Windows.</summary>
   public class FileBrowserWindows : FileBrowserBase
   {
      #region Variables

      private static string _initialPath = string.Empty;

      private const int OFN_NOCHANGEDIR = 0x00000008;
      private const int OFN_ALLOWMULTISELECT = 0x00000200;
      private const int OFN_EXPLORER = 0x00080000;
      private const int OFN_FILEMUSTEXIST = 0x00001000;
      private const int OFN_PATHMUSTEXIST = 0x00000800;
      private const int OFN_OVERWRITEPROMPT = 0x00000002;
      private const int MAX_OPEN_FILE_LENGTH = 65536;
      private const int MAX_SAVE_FILE_LENGTH = 4096;
      private const int MAX_PATH_LENGTH = 4096;

      private const int WM_USER = 0x400;
      private const int BFFM_INITIALIZED = 1;
      private const int BFFM_SELCHANGED = 2;
      private const int BFFM_SETSELECTIONW = WM_USER + 103;
      private const int BFFM_SETSTATUSTEXTW = WM_USER + 104;

      private const uint BIF_NEWDIALOGSTYLE = 0x0040; // Use the new dialog layout with the ability to resize
      private const uint BIF_SHAREABLE = 0x8000; // sharable resources displayed (remote shares, requires BIF_USENEWUI)

      private static readonly IntPtr currentWindow = NativeMethods.GetActiveWindow();
      private static readonly char[] nullChar = {(char)0};

      #endregion


      #region Implemented methods

      public override bool canOpenMultipleFiles
      {
         get { return true; }
      }

      public override bool canOpenMultipleFolders
      {
         get { return false; }
      }

      public override bool isPlatformSupported
      {
         get { return Util.Helper.isWindowsPlatform || Util.Helper.isWindowsEditor; }
      }

      public override string[] OpenFiles(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
      {
         if (IntPtr.Size == 8)
         {
            NativeMethods.OpenFileName ofn = new NativeMethods.OpenFileName();
            string dir = Util.Helper.ValidatePath(directory);

            try
            {
               ofn.dlgOwner = currentWindow;
               ofn.filter = getFilterFromFileExtensionList(extensions);
               ofn.filterIndex = 1;

               if (!string.IsNullOrEmpty(dir))
               {
                  ofn.initialDir = dir;
               }

#if ENABLE_IL2CPP
                //ofn.file = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni(Util.Helper.CreateString(" ", MAX_OPEN_FILE_LENGTH));
                ofn.file =
 System.Runtime.InteropServices.Marshal.StringToBSTR(Util.Helper.CreateString(" ", MAX_OPEN_FILE_LENGTH));
#else
               ofn.file = Util.Helper.CreateString(" ", MAX_OPEN_FILE_LENGTH);
#endif
               ofn.maxFile = MAX_OPEN_FILE_LENGTH;

               ofn.title = title;
               ofn.flags = OFN_NOCHANGEDIR | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST |
                           (multiselect ? OFN_ALLOWMULTISELECT | OFN_EXPLORER : 0x00000000);

               ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);

               if (NativeMethods.GetOpenFileName(ofn))
               {
#if ENABLE_IL2CPP
                    //string file = System.Runtime.InteropServices.Marshal.PtrToStringUni(ofn.file);
                    string file = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ofn.file);
#else
                  string file = ofn.file;
#endif
                  if (multiselect)
                  {
                     string[] files = file.Split(nullChar, StringSplitOptions.RemoveEmptyEntries);

                     if (files.Length > 2)
                     {
                        System.Collections.Generic.List<string> selectedFilesList =
                           new System.Collections.Generic.List<string>();

                        for (int ii = 1; ii < files.Length - 1; ii++)
                        {
                           var resultFile = Util.Helper.ValidateFile(files[0] + '\\' + files[ii]);
                           selectedFilesList.Add(resultFile);
                        }

                        return selectedFilesList.ToArray();
                     }
                  }

                  return new[] {Util.Helper.ValidateFile(file)};
               }
            }
            catch (Exception ex)
            {
               Debug.LogError(ex);
            }
         }
         else
         {
            Debug.LogError("'OpenFiles' only works with 64bit Windows builds!");
         }

         return new string[0];
      }

      public override string[] OpenFolders(string title, string directory, bool multiselect)
      {
         if (Util.Config.DEBUG && !string.IsNullOrEmpty(title))
            Debug.LogWarning("'title' is not supported under Windows.");

         if (multiselect)
            Debug.LogWarning("'multiselect' for folders is not supported under Windows.");

         NativeMethods.BROWSEINFO bi = new NativeMethods.BROWSEINFO();

         if (!string.IsNullOrEmpty(directory))
            _initialPath = Util.Helper.ValidatePath(directory);

         IntPtr pidl = IntPtr.Zero;
         IntPtr bufferAddress = IntPtr.Zero;

         string folder = string.Empty;
         try
         {
            bufferAddress = System.Runtime.InteropServices.Marshal.AllocHGlobal(MAX_PATH_LENGTH);

            bi.dlgOwner = currentWindow;
            bi.pidlRoot = IntPtr.Zero;
            bi.ulFlags = BIF_NEWDIALOGSTYLE | BIF_SHAREABLE;
            bi.lpfn = onBrowseEvent;
            bi.lParam = IntPtr.Zero;
            bi.iImage = 0;

            pidl = NativeMethods.SHBrowseForFolder(ref bi);

            if (NativeMethods.SHGetPathFromIDList(pidl, bufferAddress))
            {
               folder = System.Runtime.InteropServices.Marshal.PtrToStringUni(bufferAddress);
               _initialPath = folder;
            }
         }
         catch (Exception ex)
         {
            Debug.LogError(ex);
         }
         finally
         {
            if (bufferAddress != IntPtr.Zero)
               System.Runtime.InteropServices.Marshal.FreeHGlobal(bufferAddress);

            if (pidl != IntPtr.Zero)
               System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pidl);
         }

         return new[] {folder};
      }

      public override string SaveFile(string title, string directory, string defaultName, ExtensionFilter[] extensions)
      {
         if (IntPtr.Size == 8)
         {
            NativeMethods.OpenFileName sfn = new NativeMethods.OpenFileName();

            string dir = Util.Helper.ValidatePath(directory);
            string defaultExtension = getDefaultExtension(extensions);

            try
            {
               sfn.dlgOwner = currentWindow;
               sfn.filter = getFilterFromFileExtensionList(extensions);
               sfn.filterIndex = 1;

               var fileNames = defaultExtension.Equals("*") ? defaultExtension : defaultName + "." + defaultExtension;

               if (!string.IsNullOrEmpty(dir))
                  sfn.initialDir = dir;

#if ENABLE_IL2CPP
                    //sfn.file = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni(fileNames + Util.Helper.CreateString(" ", MAX_SAVE_FILE_LENGTH - fileNames.Length));
                    sfn.file = System.Runtime.InteropServices.Marshal.StringToBSTR(fileNames + Util.Helper.CreateString(" ", MAX_SAVE_FILE_LENGTH - fileNames.Length));
#else
               sfn.file = fileNames + Util.Helper.CreateString(" ", MAX_SAVE_FILE_LENGTH - fileNames.Length);
#endif
               sfn.maxFile = MAX_SAVE_FILE_LENGTH;

               sfn.title = title;
               sfn.defExt = defaultExtension;
               sfn.flags = OFN_NOCHANGEDIR | OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT;

               sfn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(sfn);

               if (NativeMethods.GetSaveFileName(sfn))
               {
#if ENABLE_IL2CPP
                        //string file = System.Runtime.InteropServices.Marshal.PtrToStringUni(sfn.file);
                        string file = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(sfn.file);
                        string newFile = Util.Helper.ValidateFile(file);
                        //return newFile.Substring(0, newFile.Length - 1);
                        return newFile.Substring(0, newFile.Length);
#else
                  string file = sfn.file;
                  string newFile = Util.Helper.ValidateFile(file);
                  return newFile;
#endif
               }
            }
            catch (Exception ex)
            {
               Debug.LogError(ex);
            }
         }
         else
         {
            Debug.LogError("'SaveFile' only works with 64bit Windows builds!");
         }

         return string.Empty;
      }

      public override void OpenFilesAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
      {
         new System.Threading.Thread(() => { cb.Invoke(OpenFiles(title, directory, extensions, multiselect)); }).Start();
      }

      public override void OpenFoldersAsync(string title, string directory, bool multiselect, Action<string[]> cb)
      {
#if UNITY_EDITOR || UNITY_2018_4_OR_NEWER
         Debug.LogWarning("'OpenFoldersAsync' is running synchronously in the Editor and Unity builds newer than 2018.4.");
         cb.Invoke(OpenFolders(title, directory, multiselect));
#else
            new System.Threading.Thread(() => { cb.Invoke(OpenFolders(title, directory, multiselect)); }).Start();
#endif
      }

      public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
      {
         new System.Threading.Thread(() => { cb.Invoke(SaveFile(title, directory, defaultName, extensions)); }).Start();
      }

      #endregion


      #region Private methods

      [AOT.MonoPInvokeCallback(typeof(NativeMethods.BrowseCallbackProc))]
      private static int onBrowseEvent(IntPtr hWnd, int msg, IntPtr lp, IntPtr lpData)
      {
         switch (msg)
         {
            case BFFM_INITIALIZED:
               NativeMethods.SendMessage(new System.Runtime.InteropServices.HandleRef(null, hWnd), BFFM_SETSELECTIONW, 1, _initialPath);
               break;
            case BFFM_SELCHANGED:
            {
               IntPtr pathPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(260 * System.Runtime.InteropServices.Marshal.SystemDefaultCharSize);

               if (NativeMethods.SHGetPathFromIDList(lp, pathPtr))
                  NativeMethods.SendMessage(new System.Runtime.InteropServices.HandleRef(null, hWnd), BFFM_SETSTATUSTEXTW, 0, pathPtr);

               System.Runtime.InteropServices.Marshal.FreeHGlobal(pathPtr);
               break;
            }
         }

         return 0;
      }

      /*
      private string getDirectory(string directory, bool addEndDelimiter = true)
      {
          string result = string.Empty;

          if (!string.IsNullOrEmpty(directory))
          {
              result = directory.Replace('/', '\\');

              if (addEndDelimiter)
              {
                  if (!result.EndsWith(Util.Constants.PATH_DELIMITER_WINDOWS))
                  {
                      result += Util.Constants.PATH_DELIMITER_WINDOWS;
                  }
              }

              return result;
          }

          return directory;
      }
      */

      private static string getDefaultExtension(ExtensionFilter[] extensions)
      {
         if (extensions != null && extensions.Length > 0 && extensions[0].Extensions.Length > 0)
         {
            return extensions[0].Extensions[0];
         }

         return "*";
      }

      private static string getFilterFromFileExtensionList(ExtensionFilter[] extensions)
      {
         if (extensions != null && extensions.Length > 0)
         {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var filter in extensions)
            {
               sb.Append(filter.Name);
               sb.Append("\0");

               for (int ii = 0; ii < filter.Extensions.Length; ii++)
               {
                  sb.Append("*.");
                  sb.Append(filter.Extensions[ii]);

                  if (ii + 1 < filter.Extensions.Length)
                     sb.Append(";");
               }

               sb.Append("\0");
            }

            sb.Append("\0");

            if (Util.Config.DEBUG)
               Debug.Log("getFilterFromFileExtensionList: " + sb);

            return sb.ToString();
         }

         return Util.Constants.TEXT_ALL_FILES + "\0*.*\0\0";
      }

      #endregion
   }

   internal static class NativeMethods
   {
      public delegate int BrowseCallbackProc(IntPtr hwnd, int uMsg, IntPtr lParam, IntPtr lpData);

      [System.Runtime.InteropServices.DllImport("Comdlg32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      public static extern bool GetOpenFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);

      [System.Runtime.InteropServices.DllImport("Comdlg32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      public static extern bool GetSaveFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName sfn);

      [System.Runtime.InteropServices.DllImport("shell32.dll")]
      internal static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

      [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      internal static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

      [System.Runtime.InteropServices.DllImport("user32.dll")]
      internal static extern IntPtr GetActiveWindow();

      [System.Runtime.InteropServices.DllImport("user32.dll", PreserveSig = true)]
      public static extern IntPtr SendMessage(System.Runtime.InteropServices.HandleRef hWnd, uint Msg, int wParam, IntPtr lParam);

      [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      public static extern IntPtr SendMessage(System.Runtime.InteropServices.HandleRef hWnd, int msg, int wParam, string lParam);

      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      internal struct OpenFileName
      {
         public int structSize;
         public IntPtr dlgOwner;
         public IntPtr instance;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string filter;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] public string customFilter;
         public int maxCustFilter;
         public int filterIndex;
#if ENABLE_IL2CPP
            public IntPtr file;
#else
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string file;
#endif
         public int maxFile;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] public string fileTitle;
         public int maxFileTitle;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string initialDir;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string title;
         public int flags;
         public ushort fileOffset;
         public ushort fileExtension;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string defExt;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string custData;
         public IntPtr hook;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] public string templateName;
         public IntPtr reservedPtr;
         public int reservedInt;
         public int flagsEx;
      }

      [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
      internal struct BROWSEINFO
      {
         public IntPtr dlgOwner;
         public IntPtr pidlRoot;
         public IntPtr pszDisplayName;
         [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] public string lpszTitle;
         public uint ulFlags;
         public BrowseCallbackProc lpfn;
         public IntPtr lParam;
         public int iImage;
      }
   }
}
#endif
// © 2017-2020 crosstales LLC (https://www.crosstales.com)