/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using Cgs.UI.ScrollRects;
using UnityEngine;

namespace Cgs.Play
{
    public static class PlaySettings
    {
        private const string PlayerPrefsAutoStackCards = "AutoStackCards";
        private const string PlayerPrefsDieFaceCount = "DieFaceCount";
        private const string PlayerPrefsDoubleClickToRollDice = "DoubleClickToRollDice";
        private const string PlayerPrefsDoubleClickToViewStacks = "DoubleClickToViewStacks";
        private const string PlayerPrefsStackViewerOverlap = "StackViewerOverlap";
        private const string PlayerPrefsShowActionsMenu = "ShowActionsMenu";
        private const string PlayerPrefsDefaultZoom = "DefaultZoom";

        public static bool AutoStackCards
        {
            get => PlayerPrefs.GetInt(PlayerPrefsAutoStackCards, 1) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsAutoStackCards, value ? 1 : 0);
        }

        public static int DieFaceCount
        {
            get => PlayerPrefs.GetInt(PlayerPrefsDieFaceCount, Die.DefaultMax);
            set => PlayerPrefs.SetInt(PlayerPrefsDieFaceCount, value);
        }

        public static bool DoubleClickToRollDice
        {
            get => PlayerPrefs.GetInt(PlayerPrefsDoubleClickToRollDice, 1) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsDoubleClickToRollDice, value ? 1 : 0);
        }

        public static bool DoubleClickToViewStacks
        {
            get => PlayerPrefs.GetInt(PlayerPrefsDoubleClickToViewStacks, 1) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsDoubleClickToViewStacks, value ? 1 : 0);
        }

        public static int StackViewerOverlap
        {
            get => PlayerPrefs.GetInt(PlayerPrefsStackViewerOverlap, 1);
            set => PlayerPrefs.SetInt(PlayerPrefsStackViewerOverlap, value);
        }

        public static bool ShowActionsMenu
        {
            get => PlayerPrefs.GetInt(PlayerPrefsShowActionsMenu, 1) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsShowActionsMenu, value ? 1 : 0);
        }

        public static float DefaultZoom
        {
            get => PlayerPrefs.GetFloat(PlayerPrefsDefaultZoom, RotateZoomableScrollRect.DefaultZoom);
            set => PlayerPrefs.SetFloat(PlayerPrefsDefaultZoom, value);
        }
    }
}
