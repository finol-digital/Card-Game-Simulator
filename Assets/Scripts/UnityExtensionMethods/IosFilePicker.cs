/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UnityExtensionMethods
{
    public enum PickedImageSource
    {
        Cancelled = 0,
        Photos = 1,
        Files = 2
    }

    /// <summary>
    /// Native iOS pickers that SimpleFileBrowser and NativeGallery do not provide: folders from the Files app,
    /// and a choice between the photo library and the Files app.
    /// </summary>
    public static class IosFilePicker
    {
        private const int PickSucceeded = 1;
        private const int PickFailed = 2;

        private delegate void FolderPickedCallback(int status, IntPtr value);

        private delegate void ImageSourceCallback(int choice);

        private static Action<string, string> _folderPicked;
        private static Action<PickedImageSource> _imageSourceChosen;
        // Each menu keeps its own copy, so one menu's pick never deletes a folder another menu still shows.
        private static readonly Dictionary<string, string> FolderCopies = new();
        private static string _pendingFolderCopy;
        private static string _pendingOwner;

        [DllImport("__Internal")]
        private static extern int CgsPickFolder(string destination, FolderPickedCallback callback);

        [DllImport("__Internal")]
        private static extern void CgsChooseImageSource(string title, string photos, string files, string cancel,
            ImageSourceCallback callback);

        /// <summary>
        /// Copies a folder chosen in the Files app into the cache. <paramref name="picked"/> receives the copy's
        /// path, or a null path and an error message; both are null when the player cancels. A new copy replaces
        /// only the previous copy made for the same <paramref name="owner"/>.
        /// </summary>
        public static void PickFolder(string owner, Action<string, string> picked)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (picked == null)
                throw new ArgumentNullException(nameof(picked));
            if (_folderPicked != null)
                return;

            _pendingFolderCopy = Path.Combine(Application.temporaryCachePath,
                "CgsImport-" + Guid.NewGuid().ToString("N"));
            _pendingOwner = owner;
            _folderPicked = picked;
            if (CgsPickFolder(_pendingFolderCopy, OnFolderPicked) != 0)
                return;

            _folderPicked = null;
            _pendingFolderCopy = null;
            _pendingOwner = null;
            picked(null, null);
        }

        public static void ChooseImageSource(string title, string photos, string files, string cancel,
            Action<PickedImageSource> chosen)
        {
            _imageSourceChosen = chosen ?? throw new ArgumentNullException(nameof(chosen));
            CgsChooseImageSource(title, photos, files, cancel, OnImageSourceChosen);
        }

        [MonoPInvokeCallback(typeof(FolderPickedCallback))]
        private static void OnFolderPicked(int status, IntPtr value)
        {
            var picked = _folderPicked;
            _folderPicked = null;
            var text = value == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(value);
            // Keep the owner's previous copy until a new one replaces it; the menu may still show it.
            if (status == PickSucceeded)
            {
                if (FolderCopies.TryGetValue(_pendingOwner, out var previousCopy))
                    DeleteFolderCopy(previousCopy);
                FolderCopies[_pendingOwner] = _pendingFolderCopy;
            }
            else
                DeleteFolderCopy(_pendingFolderCopy);
            _pendingFolderCopy = null;
            _pendingOwner = null;

            if (status == PickSucceeded)
                picked?.Invoke(text, null);
            else if (status == PickFailed)
                picked?.Invoke(null, text);
            else
                picked?.Invoke(null, null);
        }

        [MonoPInvokeCallback(typeof(ImageSourceCallback))]
        private static void OnImageSourceChosen(int choice)
        {
            var chosen = _imageSourceChosen;
            _imageSourceChosen = null;
            chosen?.Invoke(Enum.IsDefined(typeof(PickedImageSource), choice) ? (PickedImageSource)choice : PickedImageSource.Cancelled);
        }

        private static void DeleteFolderCopy(string folderCopy)
        {
            if (string.IsNullOrEmpty(folderCopy) || !Directory.Exists(folderCopy))
                return;

            try
            {
                Directory.Delete(folderCopy, true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("IosFilePicker::DeleteFailed: " + exception.Message);
            }
        }
    }
}
#endif
