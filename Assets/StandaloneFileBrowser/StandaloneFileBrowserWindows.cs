#if UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityExtensionMethods;

// ReSharper disable FieldCanBeMadeReadOnly.Global

// ReSharper disable IdentifierTypo

// ReSharper disable once CheckNamespace
namespace SFB
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OpenFileName
    {
        public int structSize;
        public IntPtr dlgOwner;
        public IntPtr instance;
        [MarshalAs(UnmanagedType.LPWStr)] public string filter;
        [MarshalAs(UnmanagedType.LPStr)] public string customFilter;
        public int maxCustFilter;
        public int filterIndex;
        public IntPtr file;
        public int maxFile;
        [MarshalAs(UnmanagedType.LPStr)] public string fileTitle;
        public int maxFileTitle;
        [MarshalAs(UnmanagedType.LPWStr)] public string initialDir;
        [MarshalAs(UnmanagedType.LPWStr)] public string title;
        public int flags;
        public ushort fileOffset;
        public ushort fileExtension;
        [MarshalAs(UnmanagedType.LPWStr)] public string defExt;
        [MarshalAs(UnmanagedType.LPWStr)] public string custData;
        public IntPtr hook;
        [MarshalAs(UnmanagedType.LPWStr)] public string templateName;
        public IntPtr reservedPtr;
        public int reservedInt;
        public int flagsEx;
    }

    public class StandaloneFileBrowserWindows : IStandaloneFileBrowser
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("Comdlg32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetOpenFileName(ref OpenFileName openFileName);

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            try
            {
                var openFileName = new OpenFileName {dlgOwner = GetActiveWindow()};

                if (extensions != null && extensions.Length > 0)
                {
                    var stringBuilder = new StringBuilder();

                    foreach (ExtensionFilter filter in extensions)
                    {
                        stringBuilder.Append(filter.Name);
                        stringBuilder.Append("\0");

                        for (var ii = 0; ii < filter.Extensions.Length; ii++)
                        {
                            stringBuilder.Append("*.");
                            stringBuilder.Append(filter.Extensions[ii]);

                            if (ii + 1 < filter.Extensions.Length)
                                stringBuilder.Append(";");
                        }

                        stringBuilder.Append("\0");
                    }

                    stringBuilder.Append("\0");

                    openFileName.filter = stringBuilder.ToString();
                }
                else
                    openFileName.filter = "All files\0*.*\0\0";

                openFileName.filterIndex = 1;

                if (!string.IsNullOrEmpty(directory))
                    openFileName.initialDir = directory;

                var chars = new char[65536];

                for (var ii = 0; ii < 65536; ii++)
                {
                    chars[ii] = ' ';
                }

                openFileName.file = Marshal.StringToBSTR(new string(chars));
                openFileName.maxFile = 65536;

                openFileName.title = title;
                openFileName.flags = 0x00000008 | 0x00001000 | 0x00000800 |
                                     (multiselect ? 0x00000200 | 0x00080000 : 0x00000000);

                openFileName.structSize = Marshal.SizeOf(openFileName);

                if (GetOpenFileName(ref openFileName))
                {
                    string file = UnityFileMethods.ValidateFile(Marshal.PtrToStringBSTR(openFileName.file));

                    if (!multiselect)
                        return new[] {file};

                    char[] nullChar = {(char) 0};
                    string[] files = file.Split(nullChar, StringSplitOptions.RemoveEmptyEntries);
                    if (files.Length <= 2)
                        return new[] {file};

                    List<string> selectedFilesList = new List<string>();
                    for (var ii = 1; ii < files.Length - 1; ii++)
                    {
                        string resultFile = files[0] + '\\' + files[ii];
                        selectedFilesList.Add(UnityFileMethods.ValidateFile(resultFile));
                    }

                    return selectedFilesList.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            return new string[0];
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions,
            bool multiselect,
            Action<string[]> cb)
        {
            cb.Invoke(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect)
        {
            return new string[0];
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName,
            ExtensionFilter[] extensions)
        {
            return string.Empty;
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName,
            ExtensionFilter[] extensions,
            Action<string> cb)
        {
            cb.Invoke(SaveFilePanel(title, directory, defaultName, extensions));
        }
    }
}

#endif
