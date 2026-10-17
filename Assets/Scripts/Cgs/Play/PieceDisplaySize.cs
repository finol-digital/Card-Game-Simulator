/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Cgs.Play
{
    public enum PieceDisplaySize
    {
        Auto,
        Standard,
        Large,
        ExtraLarge
    }

    public static class PieceDisplayPolicy
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool CgsIsMobileBrowser();
#endif

        public static bool IsMobile
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return CgsIsMobileBrowser();
#else
                return Application.isMobilePlatform;
#endif
            }
        }

        public static PieceDisplaySize Normalize(int value)
        {
            return value >= (int)PieceDisplaySize.Auto && value <= (int)PieceDisplaySize.ExtraLarge
                ? (PieceDisplaySize)value : PieceDisplaySize.Auto;
        }

        public static float Scale(PieceDisplaySize choice, bool mobile)
        {
            switch (Normalize((int)choice))
            {
                case PieceDisplaySize.Standard: return 1f;
                case PieceDisplaySize.Large: return 1.5f;
                case PieceDisplaySize.ExtraLarge: return 2f;
                default: return mobile ? 1.5f : 1f;
            }
        }
    }
}
