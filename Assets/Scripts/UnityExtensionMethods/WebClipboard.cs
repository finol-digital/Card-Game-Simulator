/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UnityExtensionMethods
{
    internal static class WebClipboard
    {
        private const string RequiresWebGlPlayerMessage = "Browser clipboard requires a WebGL player.";
        private static readonly Dictionary<int, Action<bool>> Pending = new();
        private static readonly Dictionary<int, Action<string>> PendingReads = new();
        private static int _nextRequest;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CgsCopyToClipboard(string text, int requestId, Action<int, int> completed);

        [DllImport("__Internal")]
        private static extern void CgsReadClipboard(int requestId, Action<int, IntPtr> completed);
#endif

        public static void Copy(string text, Action<bool> completed)
        {
            var requestId = ++_nextRequest;
            Pending.Add(requestId, completed);
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                CgsCopyToClipboard(text, requestId, Complete);
#else
                throw new PlatformNotSupportedException(RequiresWebGlPlayerMessage);
#endif
            }
            catch (Exception exception)
            {
                Debug.Log(TextSharing.BrowserCopyErrorMessage + " " + exception);
                Complete(requestId, 0);
            }
        }

        /// <summary>Reads the browser clipboard; <paramref name="completed"/> receives null if it can't.</summary>
        public static void Paste(Action<string> completed)
        {
            var requestId = ++_nextRequest;
            PendingReads.Add(requestId, completed);
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                CgsReadClipboard(requestId, CompleteRead);
#else
                throw new PlatformNotSupportedException(RequiresWebGlPlayerMessage);
#endif
            }
            catch (Exception exception)
            {
                Debug.Log(TextSharing.BrowserPasteErrorMessage + " " + exception);
                CompleteRead(requestId, IntPtr.Zero);
            }
        }

        [MonoPInvokeCallback(typeof(Action<int, IntPtr>))]
        private static void CompleteRead(int requestId, IntPtr text)
        {
            if (!PendingReads.Remove(requestId, out var completed))
                return;

            completed(text == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(text));
        }

        [MonoPInvokeCallback(typeof(Action<int, int>))]
        private static void Complete(int requestId, int succeeded)
        {
            if (!Pending.Remove(requestId, out var completed))
                return;

            completed(succeeded == 1);
        }
    }
}
#endif
