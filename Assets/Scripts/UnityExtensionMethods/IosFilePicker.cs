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

        private static Func<string, string, bool> _folderPicked;
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
        /// path, or a null path and an error message; both are null when the player cancels. For a copy, it returns
        /// whether its <paramref name="owner"/> accepted it: an accepted copy replaces the owner's previous copy,
        /// and a rejected copy is deleted while the previous one stays.
        /// </summary>
        public static void PickFolder(string owner, Func<string, string, bool> picked)
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
            var copy = _pendingFolderCopy;
            var owner = _pendingOwner;
            _pendingFolderCopy = null;
            _pendingOwner = null;

            if (status != PickSucceeded)
            {
                DeleteFolderCopy(copy);
                picked?.Invoke(null, status == PickFailed ? text : null);
                return;
            }

            // Keep the owner's previous copy until the owner accepts this one; the menu may still show it.
            if (picked == null || !picked(text, null))
            {
                DeleteFolderCopy(copy);
                return;
            }

            if (FolderCopies.TryGetValue(owner, out var previousCopy))
                DeleteFolderCopy(previousCopy);
            FolderCopies[owner] = copy;
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
