#if (UNITY_WSA && !UNITY_EDITOR) //|| CT_DEVELOP
using UnityEngine;
using System;

namespace Crosstales.FB.Wrapper
{
    /// <summary>File browser implementation for WSA (UWP).</summary>
    public class FileBrowserWSA : FileBrowserBase
    {

        #region Variables

        private static FileBrowserWSAImpl fbWsa;

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for a WSA file browser.
        /// </summary>
        public FileBrowserWSA() : base()
        {
            if (fbWsa == null)
            {
                if (Util.Constants.DEV_DEBUG)
                    Debug.Log("Initializing WSA file browser...");

                fbWsa = new FileBrowserWSAImpl();

                fbWsa.DEBUG = Util.Config.DEBUG;
            }
        }

        #endregion


        #region Implemented methods

        public override bool canOpenMultipleFiles
        {
            get
            {
                return FileBrowserWSAImpl.canOpenMultipleFiles;
            }
        }

        public override bool canOpenMultipleFolders
        {
            get
            {
                return FileBrowserWSAImpl.canOpenMultipleFolders;
            }
        }

        public override bool isPlatformSupported
        {
            get
            {
                return Util.Helper.isWSABasedPlatform; // || Util.Helper.isWindowsEditor;
            }
        }

        public override string[] OpenFiles(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            if (!string.IsNullOrEmpty(title))
                Debug.LogWarning("'title' is not supported under WSA (UWP).");

            if (!string.IsNullOrEmpty(directory))
                Debug.LogWarning("'directory' is not supported under WSA (UWP).");

            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.OpenFiles(getExtensionsFromExtensionFilters(extensions), multiselect); }, false);

            do
            {
                //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
        }

        public override string[] OpenFolders(string title, string directory, bool multiselect)
        {
            if (Util.Config.DEBUG && !string.IsNullOrEmpty(title))
                Debug.LogWarning("'title' is not supported under WSA (UWP).");

            if (!string.IsNullOrEmpty(directory))
                Debug.LogWarning("'directory' is not supported under WSA (UWP).");

            if (multiselect)
                Debug.LogWarning("'multiselect' for folders is not supported under WSA (UWP).");

            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.OpenSingleFolder(); }, false);

            do
            {
                //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.ToArray();
        }

        public override string SaveFile(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            if (!string.IsNullOrEmpty(title))
                Debug.LogWarning("'title' is not supported under WSA (UWP).");

            if (!string.IsNullOrEmpty(directory))
                Debug.LogWarning("'directory' is not supported under WSA (UWP).");

            fbWsa.isBusy = true;
            UnityEngine.WSA.Application.InvokeOnUIThread(() => { fbWsa.SaveFile(defaultName, getExtensionsFromExtensionFilters(extensions)); }, false);

            do
            {
                //wait
            } while (fbWsa.isBusy);

            return fbWsa.Selection.Count > 0 ? fbWsa.Selection[0] : string.Empty;
        }

        public override void OpenFilesAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFiles(title, directory, extensions, multiselect));
        }

        public override void OpenFoldersAsync(string title, string directory, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFolders(title, directory, multiselect));
        }

        public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
        {
            cb.Invoke(SaveFile(title, directory, defaultName, extensions));
        }

        #endregion


        #region Private methods

        private static System.Collections.Generic.List<Extension> getExtensionsFromExtensionFilters(ExtensionFilter[] extensions)
        {
            System.Collections.Generic.List<Extension> list = new System.Collections.Generic.List<Extension>();

            if (extensions != null && extensions.Length > 0)
            {
                foreach (ExtensionFilter filter in extensions)
                {
                    list.Add(new Extension(filter.Name, filter.Extensions));

                    Debug.Log("filter.Extensions: " + filter.Extensions.CTDump());
                }
            }
            else
            {
                list.Add(new Extension(Util.Constants.TEXT_ALL_FILES, "*"));
            }

            if (Util.Config.DEBUG)
                Debug.Log("getExtensionsFromExtensionFilters: " + list.CTDump());

            return list;
        }

        #endregion
    }
}
#endif
// © 2018-2020 crosstales LLC (https://www.crosstales.com)