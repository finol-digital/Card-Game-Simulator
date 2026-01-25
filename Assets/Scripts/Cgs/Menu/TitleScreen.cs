/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class TitleScreen : MonoBehaviour, IPointerDownHandler
    {
        public const string TouchlessStartMessage = "Press Any Key";

        public static string VersionMessage => $"VERSION {Application.version}";

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

            var rectTransform = (RectTransform)transform;
            if (rectTransform.rect.width < rectTransform.rect.height) // Portrait
            {
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

        private IEnumerator Start()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            centerText.text = TouchlessStartMessage;
#endif
            versionText.text = VersionMessage;

            yield return null;

            while (EventSystem.current.alreadySelecting)
                yield return null;
            EventSystem.current.SetSelectedGameObject(centerText.transform.parent.gameObject);

            yield return null;

            _anyButtonPressListener = InputSystem.onAnyButtonPress
                .CallOnce(_ => GoToMainMenu());
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            GoToMainMenu();
        }

        [UsedImplicitly]
        public void GoToMainMenu()
        {
            SceneManager.LoadScene(Tags.MainMenuSceneIndex);
        }

        private void OnDisable()
        {
            _anyButtonPressListener.Dispose();
        }
    }
}
