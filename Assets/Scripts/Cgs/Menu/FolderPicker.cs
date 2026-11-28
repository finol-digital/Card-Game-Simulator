/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.Localization;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
using SFB;
#elif UNITY_IOS && !UNITY_EDITOR
using UnityExtensionMethods;
#elif !ENABLE_WINMD_SUPPORT
using SimpleFileBrowser;
#endif

namespace Cgs.Menu
{
    /// <summary>
    /// Lets the player choose a folder to import from. Web players choose folders through BrowserFileBrowser.
    /// </summary>
    internal static class FolderPicker
    {
        /// <summary>
        /// Calls <paramref name="picked"/> with a readable folder path; not called if cancelled. It returns whether
        /// the owner accepted the folder: platforms that copy the folder keep one accepted copy per
        /// <paramref name="owner"/>, and delete a rejected one.
        /// </summary>
        public static void Pick(string owner, UiMessage title, Func<string, bool> picked)
        {
            if (picked == null)
                throw new ArgumentNullException(nameof(picked));

#if ENABLE_WINMD_SUPPORT
            PickWithWindowsPicker(owner, picked);
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            // The sandboxed Mac app can read only folders chosen in the system dialog.
            StandaloneFileBrowser.OpenFolderPanelAsync(CgsLocalization.Text(title), string.Empty, false, paths =>
            {
                if (paths is { Length: > 0 } && !string.IsNullOrEmpty(paths[0]))
                    picked(paths[0]);
            });
#elif UNITY_IOS && !UNITY_EDITOR
            // SimpleFileBrowser sees only the app's own storage on iOS, not the Files app.
            IosFilePicker.PickFolder(owner, (path, error) =>
            {
                if (!string.IsNullOrEmpty(path))
                    return picked(path);
                if (!string.IsNullOrEmpty(error))
                    ReportError(error);
                return false;
            });
#else
            FileBrowser.ShowLoadDialog(paths => picked(paths[0]), () => { }, FileBrowser.PickMode.Folders, false,
                null, null, CgsLocalization.Text(title));
#endif
        }

#if ENABLE_WINMD_SUPPORT
        // The Store app reads a copy of each picked folder from its cache, which persists across sessions; keep only
        // the copy each owner accepted.
        private static readonly System.Collections.Generic.Dictionary<string, string> WindowsFolderCopies = new();
        private static bool _isWindowsCacheCleared;

        private static async void PickWithWindowsPicker(string owner, Func<string, bool> picked)
        {
            try
            {
                // Menus forget their folders when the app closes, so copies from earlier sessions are unused.
                if (!_isWindowsCacheCleared)
                {
                    _isWindowsCacheCleared = true;
                    var cache = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
                    foreach (var oldCopy in System.IO.Directory.GetDirectories(cache, "CgsImport-*"))
                        DeleteWindowsFolderCopy(oldCopy);
                }

                var path = await UnityExtensionMethods.UwpFileBrowser.OpenFolderAsync();
                if (string.IsNullOrEmpty(path))
                    return;

                // Each copy sits in its own CgsImport folder.
                var copy = System.IO.Path.GetDirectoryName(path);
                if (!picked(path))
                {
                    DeleteWindowsFolderCopy(copy);
                    return;
                }

                if (WindowsFolderCopies.TryGetValue(owner, out var previousCopy))
                    DeleteWindowsFolderCopy(previousCopy);
                WindowsFolderCopies[owner] = copy;
            }
            catch (Exception exception)
            {
                ReportError(exception.Message);
            }
        }

        private static void DeleteWindowsFolderCopy(string copy)
        {
            try
            {
                if (!string.IsNullOrEmpty(copy) && System.IO.Directory.Exists(copy))
                    System.IO.Directory.Delete(copy, true);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("FolderPicker::DeleteFailed: " + exception.Message);
            }
        }
#endif

#if ENABLE_WINMD_SUPPORT || (UNITY_IOS && !UNITY_EDITOR)
        private static void ReportError(string detail)
        {
            UnityEngine.Debug.LogWarning(CardGameManager.ImportFailureErrorMessage + detail);
            CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.import",
                CardGameManager.ImportFailureErrorMessage + detail, ("detail", detail)));
        }
#endif
    }
}
