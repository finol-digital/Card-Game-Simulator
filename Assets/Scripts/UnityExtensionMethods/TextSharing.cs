/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;

namespace UnityExtensionMethods
{
    public static class TextSharing
    {
        public const string CopiedMessage = "Copied to clipboard.";
        public const string CopyErrorMessage = "Couldn't copy to the clipboard. Please try again.";
        public const string CopyUnconfirmedMessage = "Couldn't confirm the copy. Try pasting to check, or copy again.";
        public const string EmptyMessage = "There is no text to copy or share.";
        public const string ShareErrorMessage = "Couldn't open sharing. Please try again.";

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
            try
            {
                new NativeShare().SetText(text).SetCallback((result, _) =>
                {
                    // NativeShare only confirms selection of a destination, not delivery or copying.
                    report(result switch
                    {
                        NativeShare.ShareResult.Shared => "Share destination selected. Check there to confirm it was shared or copied.",
                        NativeShare.ShareResult.NotShared => "Sharing canceled or not completed.",
                        _ => "Sharing closed. Couldn't confirm whether the text was shared or copied."
                    });
                }).Share();
            }
            catch (Exception exception)
            {
                Debug.Log(ShareErrorMessage + " " + exception);
                report(ShareErrorMessage);
            }
#else
            report(Copy(text, copiedMessage));
#endif
        }

        public static string Copy(string text, string copiedMessage = CopiedMessage)
        {
            return Copy(text, copiedMessage, UniClipboard.SetText, UniClipboard.GetText);
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
