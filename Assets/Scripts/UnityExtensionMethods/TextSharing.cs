/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using UnityEngine;

namespace UnityExtensionMethods
{
    public static class TextSharing
    {
        public const string CopiedMessage = "Copied to clipboard.";
        public const string CopyErrorMessage = "Couldn't copy to the clipboard. Please try again.";
        public const string CopyUnconfirmedMessage = "Couldn't confirm the copy. Try pasting to check, or copy again.";
        public const string BrowserCopyErrorMessage = "The browser couldn't copy the text. Try again from an HTTPS page in a supported browser, and check clipboard permissions.";
        public const string BrowserPasteErrorMessage = "The browser couldn't read the clipboard. Allow clipboard access, or click the text field and paste with Ctrl+V (Command+V on a Mac).";
        public const string EmptyMessage = "There is no text to copy or share.";
        public const string ShareErrorMessage = "Couldn't open sharing. Please try again.";
        public const string ShareUnavailableMessage = "Sharing is not available on this platform.";
        public const string FileMissingMessage = "Couldn't find the file to share. Export it again and retry.";
        public const string FileReadErrorMessage = "Couldn't prepare the file for sharing. Please try again.";

        public static void CopyOrShare(string text, Action<string> report, string copiedMessage = CopiedMessage)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            if (string.IsNullOrEmpty(text))
            {
                report(EmptyMessage);
                return;
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            ShareNative(share => share.SetText(text), report);
#elif UNITY_WEBGL && !UNITY_EDITOR
            WebClipboard.Copy(text, succeeded => report(succeeded ? copiedMessage : BrowserCopyErrorMessage));
#else
            report(Copy(text, copiedMessage, UniClipboard.SetText, UniClipboard.GetText));
#endif
        }

        /// <summary>
        /// Reads clipboard text. <paramref name="pasted"/> receives the text, or null and a failure message.
        /// </summary>
        public static void Paste(Action<string, string> pasted)
        {
            if (pasted == null)
                throw new ArgumentNullException(nameof(pasted));

#if UNITY_WEBGL && !UNITY_EDITOR
            WebClipboard.Paste(text => pasted(text, text == null ? BrowserPasteErrorMessage : null));
#else
            pasted(UniClipboard.GetText(), null);
#endif
        }

        public static void ShareFile(string path, Action<string> report, string mimeType = null)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            if (!File.Exists(path))
            {
                report(FileMissingMessage);
                return;
            }

            ShareNative(share => share.AddFile(path, mimeType), report);
        }

        public static string GetShareFeedback(NativeShare.ShareResult result)
        {
            // NativeShare only confirms selection of a destination, not delivery or copying.
            return result switch
            {
                NativeShare.ShareResult.Shared => "Share destination selected. Check there to confirm it was shared or copied.",
                NativeShare.ShareResult.NotShared => "Sharing canceled or not completed.",
                _ => "Sharing closed. Couldn't confirm whether the content was shared or copied."
            };
        }

        private static void ShareNative(Action<NativeShare> prepare, Action<string> report)
        {
            if (Application.isEditor ||
                (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer))
            {
                report(ShareUnavailableMessage);
                return;
            }

            try
            {
                var share = new NativeShare();
                prepare(share);
                share.SetCallback((result, _) => report(GetShareFeedback(result))).Share();
            }
            catch (Exception exception)
            {
                Debug.Log(ShareErrorMessage + " " + exception);
                report(ShareErrorMessage);
            }
        }

        private static string Copy(string text, string copiedMessage, Action<string> write, Func<string> read)
        {
            if (string.IsNullOrEmpty(text))
                return EmptyMessage;

            try
            {
                write(text);
            }
            catch (Exception exception)
            {
                Debug.Log(CopyErrorMessage + " " + exception);
                return CopyErrorMessage;
            }

            try
            {
                return string.Equals(text.Replace("\r\n", "\n"), read()?.Replace("\r\n", "\n"), StringComparison.Ordinal)
                    ? copiedMessage
                    : CopyErrorMessage;
            }
            catch (Exception exception)
            {
                // A read failure doesn't prove the preceding write failed.
                Debug.Log(CopyUnconfirmedMessage + " " + exception);
                return CopyUnconfirmedMessage;
            }
        }
    }
}
