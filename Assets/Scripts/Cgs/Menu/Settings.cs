/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class Settings : MonoBehaviour
    {
#if UNITY_IOS || UNITY_ANDROID
        private const int DefaultButtonTooltipsEnabled = 0;
#else
        private const int DefaultButtonTooltipsEnabled = 1;
#endif

#if UNITY_IOS || UNITY_ANDROID
        private const int DefaultPreviewOnMouseOver = 0;
#else
        private const int DefaultPreviewOnMouseOver = 1;
#endif

        private const string PlayerPrefsButtonTooltipsEnabled = "ButtonTooltipsEnabled";
        private const string PlayerPrefsPreviewOnMouseOver = "PreviewOnMouseOver";
        private const string PlayerPrefsHideReprints = "HideReprints";
        private const string PlayerPrefsDeveloperMode = "DeveloperMode";

        public static bool ButtonTooltipsEnabled
        {
            get => PlayerPrefs.GetInt(PlayerPrefsButtonTooltipsEnabled, DefaultButtonTooltipsEnabled) == 1;
            private set => PlayerPrefs.SetInt(PlayerPrefsButtonTooltipsEnabled, value ? 1 : 0);
        }

        public static bool PreviewOnMouseOver
        {
            get => PlayerPrefs.GetInt(PlayerPrefsPreviewOnMouseOver, DefaultPreviewOnMouseOver) == 1;
            private set => PlayerPrefs.SetInt(PlayerPrefsPreviewOnMouseOver, value ? 1 : 0);
        }

        public static bool HideReprints
        {
            get => PlayerPrefs.GetInt(PlayerPrefsHideReprints, 1) == 1;
            private set => PlayerPrefs.SetInt(PlayerPrefsHideReprints, value ? 1 : 0);
        }

        public static bool DeveloperMode
        {
            get => PlayerPrefs.GetInt(PlayerPrefsDeveloperMode, 0) == 1;
            private set => PlayerPrefs.SetInt(PlayerPrefsDeveloperMode, value ? 1 : 0);
        }

        public ScrollRect scrollRect;
        public Dropdown framerateDropdown;
        public Dropdown resolutionDropdown;
        public Toggle screenOsControlToggle;
        public Toggle screenAutoRotateToggle;
        public Toggle screenPortraitToggle;
        public Toggle screenLandscapeToggle;
        public Toggle controllerLockToLandscapeToggle;
        public Toggle buttonTooltipsEnabledToggle;
        public Toggle previewOnMouseOverToggle;
        public Toggle hideReprintsToggle;
        public Toggle developerModeToggle;
        public List<Transform> orientationOptions;

        private void Start()
        {
            framerateDropdown.value = FrameRateManager.FrameRateIndex;
            resolutionDropdown.value = ResolutionManager.ResolutionIndex;

            switch (ScreenOrientationManager.PreferredScreenOrientation)
            {
                case ScreenOrientationPref.OsControl:
                    screenOsControlToggle.isOn = true;
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
                    Debug.LogError("Invalid value for ScreenOrientationManager.PreferredScreenOrientation!");
                    break;
            }

            controllerLockToLandscapeToggle.isOn = ScreenOrientationManager.DoesControllerLockToLandscape;
            previewOnMouseOverToggle.isOn = PreviewOnMouseOver;
            buttonTooltipsEnabledToggle.isOn = ButtonTooltipsEnabled;
            hideReprintsToggle.isOn = HideReprints;
            developerModeToggle.isOn = DeveloperMode;
#if !UNITY_ANDROID && !UNITY_IOS
            foreach (var option in orientationOptions)
                option.gameObject.SetActive(false);
#endif
        }

        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;

            if ((Inputs.IsVertical || Inputs.IsHorizontal) && EventSystem.current.currentSelectedGameObject == null)
                EventSystem.current.SetSelectedGameObject(framerateDropdown.gameObject);
            else if (Inputs.IsPageVertical && !Inputs.WasPageVertical)
                ScrollPage(Inputs.IsPageDown);
            else if (Inputs.IsOption)
                GoToWebsite();
            else if (Inputs.IsCancel)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                    BackToMainMenu();
                else if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void ScrollPage(bool scrollDown)
        {
            scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(scrollRect.verticalNormalizedPosition + (scrollDown ? -0.1f : 0.1f));
        }

        [UsedImplicitly]
        public void SetFramerate(int framerateIndex)
        {
            FrameRateManager.FrameRateIndex = framerateIndex;
        }

        [UsedImplicitly]
        public void SetResolution(int resolutionIndex)
        {
            ResolutionManager.ResolutionIndex = resolutionIndex;
        }

        [UsedImplicitly]
        public void SetScreenOsControl(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.OsControl;
        }

        [UsedImplicitly]
        public void SetScreenAutoRotate(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.AutoRotate;
        }

        [UsedImplicitly]
        public void SetScreenPortrait(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.Portrait;
        }

        [UsedImplicitly]
        public void SetScreenLandscape(bool isOn)
        {
            if (isOn)
                ScreenOrientationManager.PreferredScreenOrientation = ScreenOrientationPref.Landscape;
        }

        [UsedImplicitly]
        public void SetControllerLockToLandscape(bool controllerLockToLandscape)
        {
            ScreenOrientationManager.DoesControllerLockToLandscape = controllerLockToLandscape;
        }

        [UsedImplicitly]
        public void SetButtonTooltipsEnabled(bool buttonTooltipsEnabled)
        {
            ButtonTooltipsEnabled = buttonTooltipsEnabled;
        }

        [UsedImplicitly]
        public void SetPreviewOnMouseOver(bool previewOnMouseOver)
        {
            PreviewOnMouseOver = previewOnMouseOver;
        }

        [UsedImplicitly]
        public void SetHideReprints(bool hideReprints)
        {
            HideReprints = hideReprints;
        }

        [UsedImplicitly]
        public void SetDeveloperMode(bool developerMode)
        {
            DeveloperMode = developerMode;
        }

        [UsedImplicitly]
        public void GoToWebsite()
        {
            Application.OpenURL(Tags.CgsWebsite);
        }

        [UsedImplicitly]
        public void BackToMainMenu()
        {
            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
