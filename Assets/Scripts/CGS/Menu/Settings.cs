/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using CardGameDef;

namespace CGS.Menu
{
    public class Settings : MonoBehaviour
    {
        public const string PlayerPrefHideReprints = "HideReprints";

        public static bool HideReprints
        {
            get { return PlayerPrefs.GetInt(PlayerPrefHideReprints, 1) == 1; }
            set { PlayerPrefs.SetInt(PlayerPrefHideReprints, value ? 1 : 0); }
        }

        public Toggle screenOSControlToggle;
        public Toggle screenAutoRotateToggle;
        public Toggle screenPortraitToggle;
        public Toggle screenLandscapeToggle;
        public Toggle controllerLockToLandscapeToggle;
        public Toggle hideReprintsToggle;

        void Start()
        {
            switch (ScreenOrientationManager.PreferredScreenOrientation)
            {
                case ScreenOrientationPref.OSControl:
                    screenOSControlToggle.isOn = true;
                    break;
                case ScreenOrientationPref.AutoRotate:
                    screenAutoRotateToggle.isOn = true;
                    break;
                case ScreenOrientationPref.Portrait:
                    screenPortraitToggle.isOn = true;
                    break;
                case ScreenOrientationPref.Landscape:
                    screenLandscapeToggle.isOn = true;
                    break;
                default:
                    break;
            }
            controllerLockToLandscapeToggle.isOn = ScreenOrientationManager.DoesControllerLockToLandscape;
            hideReprintsToggle.isOn = HideReprints;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                BackToMainMenu();
        }

        public void SetScreenOSControl(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.OSControl;
        }

        public void SetScreenAutoRotate(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.AutoRotate;
        }

        public void SetScreenPortrait(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.Portrait;
        }

        public void SetScreenLandscape(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.Landscape;
        }

        public void SetControllerLockToLandscape(bool controllerLockToLandscape)
        {
            ScreenOrientationManager.DoesControllerLockToLandscape = controllerLockToLandscape;
        }

        public void SetHideReprints(bool hideReprints)
        {
            HideReprints = hideReprints;
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene(CGS.Menu.MainMenu.MainMenuSceneIndex);
        }
    }
}
