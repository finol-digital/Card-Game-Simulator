/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace UnityExtensionMethods
{
    internal static class WebClipboard
    {
        private static readonly Dictionary<int, Action<bool>> Pending = new();
        private static int _nextRequest;

        [DllImport("__Internal")]
        private static extern void CgsCopyToClipboard(string text, int requestId, Action<int, int> completed);

        public static void Copy(string text, Action<bool> completed)
        {
            var requestId = ++_nextRequest;
            Pending.Add(requestId, completed);
            CgsCopyToClipboard(text, requestId, Complete);
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
