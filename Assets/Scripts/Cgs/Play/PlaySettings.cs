/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using UnityEngine;

namespace Cgs.Play
{
    public static class PlaySettings
    {
        private const string PlayerPrefsAutoStackCards = "AutoStackCards";
        private const string PlayerPrefsStackViewerOverlap = "StackViewerOverlap";
        private const string PlayerPrefsDieFaceCount = "DieFaceCount";

        public static bool AutoStackCards
        {
            get => PlayerPrefs.GetInt(PlayerPrefsAutoStackCards, 1) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsAutoStackCards, value ? 1 : 0);
        }

        public static int StackViewerOverlap
        {
            get => PlayerPrefs.GetInt(PlayerPrefsStackViewerOverlap, 1);
            set => PlayerPrefs.SetInt(PlayerPrefsStackViewerOverlap, value);
        }

        public static int DieFaceCount
        {
            get => PlayerPrefs.GetInt(PlayerPrefsDieFaceCount, Die.DefaultMax);
            set => PlayerPrefs.SetInt(PlayerPrefsDieFaceCount, value);
        }
    }
}
