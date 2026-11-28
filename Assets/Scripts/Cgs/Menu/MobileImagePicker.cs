/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_ANDROID || UNITY_IOS
using System;
using Cgs.Localization;
#if UNITY_IOS && !UNITY_EDITOR
using UnityExtensionMethods;
#endif

namespace Cgs.Menu
{
    /// <summary>Lets mobile players choose an image to import.</summary>
    internal static class MobileImagePicker
    {
        public const string PhotosLabel = "Photos";
        public const string FilesLabel = "Files";
        private const string ImageFileType = "public.image";

        /// <summary>Calls <paramref name="picked"/> with the image's path, or null if cancelled.</summary>
        public static void Pick(UiMessage title, Action<string> picked)
        {
            if (picked == null)
                throw new ArgumentNullException(nameof(picked));

            var titleText = CgsLocalization.Text(title);
#if UNITY_IOS && !UNITY_EDITOR
            // The photo picker can't reach images saved in the Files app, so offer both.
            IosFilePicker.ChooseImageSource(titleText, CgsLocalization.Text(new UiMessage("images.source.photos", PhotosLabel)),
                CgsLocalization.Text(new UiMessage("images.source.files", FilesLabel)),
                CgsLocalization.Text(new UiMessage("common.cancel", "Cancel")), source =>
                {
                    switch (source)
                    {
                        case PickedImageSource.Photos:
                            NativeGallery.GetImageFromGallery(path => picked(path), titleText);
                            break;
                        case PickedImageSource.Files:
                            NativeFilePicker.PickFile(path => picked(path), ImageFileType);
                            break;
                        default:
                            picked(null);
                            break;
                    }
                });
#else
            // Android's photo picker also lists images saved in Downloads and other folders.
            NativeGallery.GetImageFromGallery(path => picked(path), titleText);
#endif
        }
    }
}
#endif
