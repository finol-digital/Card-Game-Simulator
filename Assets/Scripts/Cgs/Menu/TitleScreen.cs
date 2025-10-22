/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class TitleScreen : MonoBehaviour
    {
        public const string TouchlessStartMessage = "Press Any Key";

        public static string VersionMessage => $"VERSION {Application.version}";

        private const float MinWidth = 1080;
        private const int CenterTextFontSizePortrait = 30;
        private const int CenterTextFontSizeLandscape = 40;
        private const int MetaTextFontSizePortrait = 25;
        private const int MetaTextFontSizeLandscape = 30;

        private static readonly Vector2 CompanyTextPortraitDimensions = new(250, 75);
        private static readonly Vector2 CompanyTextLandscapeDimensions = new(-400, 75);
        private static readonly Vector2 CompanyTextLandscapePosition = new(-700, 0);
        private static readonly Vector2 VersionTextPortraitOffsetMin = new(-250, 0);
        private static readonly Vector2 VersionTextPortraitOffsetMax = new(0, 75);
        private static readonly Vector2 VersionTextLandscapeOffsetMin = new(400, 0);
        private static readonly Vector2 VersionTextLandscapeOffsetMax = new(700, 75);

        public Image backgroundImage;
        public Sprite backgroundSpritePortrait;
        public Sprite backgroundSpriteLandscape;
        public Image footerImage;
        public Sprite footerSpritePortrait;
        public Sprite footerSpriteLandscape;
        public Text companyText;
        public Text centerText;
        public Text versionText;

        private IDisposable _anyButtonPressListener;

        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (((RectTransform)transform).rect.width < MinWidth) // Portrait
            {
                backgroundImage.sprite = backgroundSpritePortrait;
                footerImage.preserveAspect = false;
                footerImage.sprite = footerSpritePortrait;
                centerText.fontSize = CenterTextFontSizePortrait;
                companyText.fontSize = MetaTextFontSizePortrait;
                var companyTransform = companyText.rectTransform;
                companyTransform.anchorMin = Vector2.zero;
                companyTransform.anchorMax = Vector2.zero;
                companyTransform.pivot = Vector2.zero;
                companyTransform.offsetMin = Vector2.zero;
                companyTransform.offsetMax = CompanyTextPortraitDimensions;
                versionText.fontSize = MetaTextFontSizePortrait;
                var versionTransform = versionText.rectTransform;
                versionTransform.anchorMin = Vector2.right;
                versionTransform.anchorMax = Vector2.right;
                versionTransform.pivot = Vector2.right;
                versionTransform.offsetMin = VersionTextPortraitOffsetMin;
                versionTransform.offsetMax = VersionTextPortraitOffsetMax;
            }
            else // Landscape
            {
                backgroundImage.sprite = backgroundSpriteLandscape;
                footerImage.preserveAspect = true;
                footerImage.sprite = footerSpriteLandscape;
                centerText.fontSize = CenterTextFontSizeLandscape;
                companyText.fontSize = MetaTextFontSizeLandscape;
                var companyTransform = companyText.rectTransform;
                companyTransform.anchorMin = Vector2.right / 2;
                companyTransform.anchorMax = Vector2.right / 2;
                companyTransform.pivot = Vector2.right / 2;
                companyTransform.offsetMin = CompanyTextLandscapePosition;
                companyTransform.offsetMax = CompanyTextLandscapeDimensions;
                versionText.fontSize = MetaTextFontSizeLandscape;
                var versionTransform = versionText.rectTransform;
                versionTransform.anchorMin = Vector2.right / 2;
                versionTransform.anchorMax = Vector2.right / 2;
                versionTransform.pivot = Vector2.right / 2;
                versionTransform.offsetMin = VersionTextLandscapeOffsetMin;
                versionTransform.offsetMax = VersionTextLandscapeOffsetMax;
            }
        }

        private void Start()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            centerText.text = TouchlessStartMessage;
#endif
            versionText.text = VersionMessage;

            _anyButtonPressListener = InputSystem.onAnyButtonPress
                .CallOnce(_ => SceneManager.LoadScene(Tags.MainMenuSceneIndex));
        }

        private void OnDisable()
        {
            _anyButtonPressListener.Dispose();
        }
    }
}
