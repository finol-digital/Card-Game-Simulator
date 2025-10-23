/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using JetBrains.Annotations;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Menu
{
    public class MainMenu : MonoBehaviour
    {
        private const float MinWidth = 1200f;
        private const float FooterPortraitWidth = 260f;
        private const float FooterLandscapeWidth = 500f;

        public static string WelcomeMessage => "Welcome to CGS!\n" + WelcomeMessageExt;

#if UNITY_ANDROID || UNITY_IOS
        public static string WelcomeMessageExt =>
            "This Mobile version of CGS is intended as a companion to the PC version of CGS.\n" +
            "The PC version of CGS is available from the CGS website.\n" + "Go to the CGS website?";
#else
        public static string WelcomeMessageExt =>
            "The CGS website has guides/resources that may help new users.\n" + "Go to the CGS website?";
#endif
        private const string FinolDigitalLlc = "Finol Digital LLC";
        private const string PlayerPrefsHasSeenWelcome = "HasSeenWelcome";

        private static bool HasSeenWelcome
        {
            get => PlayerPrefs.GetInt(PlayerPrefsHasSeenWelcome, 0) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsHasSeenWelcome, value ? 1 : 0);
        }

        public const string QuitPrompt = "Quit?";

        private const float StartBufferTime = 0.1f;
        private const float AnimationDuration = 0.3f;

        public GameObject gamesManagementMenuPrefab;

        public Text versionText;
        public Text copyrightText;
        public Text currentGameNameText;
        public Image currentCardImage;
        public Image currentBannerImage;
        public Image previousCardImage;
        public Image nextCardImage;
        public Image offLeftImage;
        public Image offRightImage;
        public List<GameObject> selectableButtons;

        public Button joinButton;
        public Button fullscreenButton;
        public GameObject quitButton;

#if !CGS_SINGLEGAME
        private GamesManagementMenu GamesManagement =>
            _gamesManagement ??= Instantiate(gamesManagementMenuPrefab).GetOrAddComponent<GamesManagementMenu>();
#endif

        private GamesManagementMenu _gamesManagement;

        private bool _isAnimating;

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(ResetGameSelectionCarousel);
            CardGameManager.Instance.OnSceneActions.Add(SetCopyright);
        }

        private void SetCopyright()
        {
            var copyright = CardGameManager.Current.Copyright;
            copyrightText.text = string.IsNullOrWhiteSpace(copyright) ? FinolDigitalLlc : copyright;
        }


        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            ResizeFooter();
        }

        private void ResizeFooter()
        {
            var screenWidth = ((RectTransform)transform).rect.width;

            var sizeDelta = copyrightText.rectTransform.sizeDelta;
            sizeDelta = screenWidth < MinWidth
                ? new Vector2(FooterPortraitWidth, sizeDelta.y)
                : new Vector2(FooterLandscapeWidth, sizeDelta.y);
            copyrightText.rectTransform.sizeDelta = sizeDelta;

            sizeDelta = versionText.rectTransform.sizeDelta;
            sizeDelta = screenWidth < MinWidth
                ? new Vector2(FooterPortraitWidth, sizeDelta.y)
                : new Vector2(FooterLandscapeWidth, sizeDelta.y);
            versionText.rectTransform.sizeDelta = sizeDelta;

            sizeDelta = currentGameNameText.rectTransform.sizeDelta;
            sizeDelta = screenWidth < MinWidth
                ? new Vector2(-2f * FooterPortraitWidth, sizeDelta.y)
                : new Vector2(-2f * FooterLandscapeWidth, sizeDelta.y);
            currentGameNameText.rectTransform.sizeDelta = sizeDelta;
        }

        private void Start()
        {
            ResizeFooter();
            versionText.text = TitleScreen.VersionMessage;

#if UNITY_WEBGL
            joinButton.interactable = false;
            fullscreenButton.gameObject.SetActive(true);
            quitButton.gameObject.SetActive(false);
#endif
#if UNITY_STANDALONE || UNITY_WSA
            quitButton.SetActive(true);
#else
            quitButton.SetActive(false);
#endif

            if (!HasSeenWelcome)
                CardGameManager.Instance.Messenger.Ask(WelcomeMessage, DeclineWelcomeMessage, AcceptWelcomeMessage,
                    true);
        }

        private static void DeclineWelcomeMessage()
        {
            HasSeenWelcome = true;
        }

        private static void AcceptWelcomeMessage()
        {
            HasSeenWelcome = true;
            Application.OpenURL(Tags.CgsWebsite);
        }

        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;

            if (SwipeManager.DetectSwipe())
            {
                if (SwipeManager.IsSwipingRight())
                    SelectPrevious();
                else if (SwipeManager.IsSwipingLeft())
                    SelectNext();
            }

            if (Inputs.IsPageVertical)
            {
                if (Inputs.IsPageDown && !Inputs.WasPageDown)
                    SelectNext();
                else if (Inputs.IsPageUp && !Inputs.WasPageUp)
                    SelectPrevious();
            }
            else if (Inputs.IsPageHorizontal)
            {
                if (Inputs.IsPageLeft && !Inputs.WasPageLeft)
                    SelectPrevious();
                else if (Inputs.IsPageRight && !Inputs.WasPageRight)
                    SelectNext();
            }
            else if (Inputs.IsHorizontal && EventSystem.current.currentSelectedGameObject == null ||
                     EventSystem.current.currentSelectedGameObject == selectableButtons[0].gameObject)
            {
                if (Inputs.IsLeft && !Inputs.WasLeft)
                    SelectPrevious();
                else if (Inputs.IsRight && !Inputs.WasRight)
                    SelectNext();
            }
            else if (Inputs.IsVertical && !selectableButtons.Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.SetSelectedGameObject(selectableButtons[0].gameObject);

            if (Input.GetKeyDown(Inputs.BluetoothReturn))
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                    EventSystem.current.currentSelectedGameObject.GetComponent<Button>()?.onClick?.Invoke();
            }
            else if (Inputs.IsSort)
                SelectPrevious();
            else if (Inputs.IsFilter)
                SelectNext();
            else if (Inputs.IsNew)
                StartGame();
            else if (Inputs.IsLoad)
                JoinGame();
            else if (Inputs.IsSave)
                EditDeck();
            else if (Inputs.IsFocusBack && !Inputs.WasFocusBack)
                ShowGamesManagementMenu();
            else if (Inputs.IsFocusNext && !Inputs.WasFocusNext)
                ExploreCards();
            else if (Inputs.IsOption)
                ShowSettings();
            else if (Inputs.IsCancel)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                    PromptQuit();
                else if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void ResetGameSelectionCarousel()
        {
            currentGameNameText.text = CardGameManager.Current.Name;
            currentCardImage.sprite = CardGameManager.Current.CardBackImageSprite;
            currentBannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            previousCardImage.sprite = CardGameManager.Instance.Previous.CardBackImageSprite;
            nextCardImage.sprite = CardGameManager.Instance.Next.CardBackImageSprite;
        }

        [UsedImplicitly]
        public void SelectPrevious()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime || _isAnimating)
                return;

            // Create duplicate cards to animate
            var offLeft = Instantiate(offLeftImage.gameObject, offLeftImage.transform.parent);
            offLeft.GetOrAddComponent<Image>().sprite = CardGameManager.Instance.Previous2.CardBackImageSprite;
            var left = Instantiate(previousCardImage.gameObject, previousCardImage.transform.parent);
            Destroy(left.GetComponent<Button>());
            var middle = Instantiate(currentCardImage.gameObject, currentCardImage.transform.parent);
            Destroy(middle.GetComponent<Button>());
            var right = Instantiate(nextCardImage.gameObject, nextCardImage.transform.parent);
            Destroy(right.GetComponent<Button>());

            // Do selection and hide originals
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
            var leftRectTransform = (RectTransform)left.transform;
            var currentRectTransform = (RectTransform)currentCardImage.transform;
            leftRectTransform.anchorMin = currentRectTransform.anchorMin;
            leftRectTransform.anchorMax = currentRectTransform.anchorMax;
            leftRectTransform.pivot = currentRectTransform.pivot;
            leftRectTransform.position = previousCardImage.transform.position;
            var middleRectTransform = (RectTransform)middle.transform;
            var nextRectTransform = (RectTransform)nextCardImage.transform;
            middleRectTransform.anchorMin = nextRectTransform.anchorMin;
            middleRectTransform.anchorMax = nextRectTransform.anchorMax;
            middleRectTransform.pivot = nextRectTransform.pivot;
            middleRectTransform.position = currentCardImage.transform.position;
            previousCardImage.gameObject.SetActive(false);
            currentCardImage.gameObject.SetActive(false);
            nextCardImage.gameObject.SetActive(false);

            // Do Animation
            _isAnimating = true;
            Tween.UIAnchoredPosition((RectTransform)offLeft.transform,
                ((RectTransform)previousCardImage.transform).anchoredPosition, AnimationDuration);
            Tween.UIAnchoredPosition((RectTransform)left.transform,
                ((RectTransform)currentCardImage.transform).anchoredPosition, AnimationDuration);
            Tween.UIAnchoredPosition((RectTransform)middle.transform,
                ((RectTransform)nextCardImage.transform).anchoredPosition, AnimationDuration);
            Tween.UIAnchoredPosition((RectTransform)right.transform,
                ((RectTransform)offRightImage.transform).anchoredPosition, AnimationDuration)
                .OnComplete(() =>
                {
                    // Remove duplicates and show originals
                    Destroy(offLeft);
                    Destroy(left);
                    Destroy(middle);
                    Destroy(right);
                    previousCardImage.gameObject.SetActive(true);
                    currentCardImage.gameObject.SetActive(true);
                    nextCardImage.gameObject.SetActive(true);
                    _isAnimating = false;
                });
        }

        [UsedImplicitly]
        public void SelectNext()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime || _isAnimating)
                return;

            // Create duplicate cards to animate
            var offRight = Instantiate(offRightImage.gameObject, nextCardImage.transform.parent);
            offRight.GetOrAddComponent<Image>().sprite = CardGameManager.Instance.Next2.CardBackImageSprite;
            var right = Instantiate(nextCardImage.gameObject, nextCardImage.transform.parent);
            Destroy(right.GetComponent<Button>());
            var middle = Instantiate(currentCardImage.gameObject, currentCardImage.transform.parent);
            Destroy(middle.GetComponent<Button>());
            var left = Instantiate(previousCardImage.gameObject, previousCardImage.transform.parent);
            Destroy(left.GetComponent<Button>());

            // Do selection and hide originals
            CardGameManager.Instance.Select(CardGameManager.Instance.Next.Id);
            var rightRectTransform = (RectTransform)right.transform;
            var currentRectTransform = (RectTransform)currentCardImage.transform;
            rightRectTransform.anchorMin = currentRectTransform.anchorMin;
            rightRectTransform.anchorMax = currentRectTransform.anchorMax;
            rightRectTransform.pivot = currentRectTransform.pivot;
            rightRectTransform.position = nextCardImage.transform.position;
            var middleRectTransform = (RectTransform)middle.transform;
            var previousRectTransform = (RectTransform)previousCardImage.transform;
            middleRectTransform.anchorMin = previousRectTransform.anchorMin;
            middleRectTransform.anchorMax = previousRectTransform.anchorMax;
            middleRectTransform.pivot = previousRectTransform.pivot;
            middleRectTransform.position = currentCardImage.transform.position;
            nextCardImage.gameObject.SetActive(false);
            currentCardImage.gameObject.SetActive(false);
            previousCardImage.gameObject.SetActive(false);

            // Do Animation
            _isAnimating = true;
            Tween.UIAnchoredPosition((RectTransform)offRight.transform,
                ((RectTransform)nextCardImage.transform).anchoredPosition, AnimationDuration);
            Tween.UIAnchoredPosition((RectTransform)right.transform,
                ((RectTransform)currentCardImage.transform).anchoredPosition, AnimationDuration);
            Tween.UIAnchoredPosition((RectTransform)middle.transform,
                ((RectTransform)previousCardImage.transform).anchoredPosition, AnimationDuration);
            Tween.UIAnchoredPosition((RectTransform)left.transform,
                    ((RectTransform)offLeftImage.transform).anchoredPosition, AnimationDuration)
                .OnComplete(() =>
                {
                    // Remove duplicates and show originals
                    Destroy(offRight);
                    Destroy(right);
                    Destroy(middle);
                    Destroy(left);
                    nextCardImage.gameObject.SetActive(true);
                    currentCardImage.gameObject.SetActive(true);
                    previousCardImage.gameObject.SetActive(true);
                    _isAnimating = false;
                });
        }

        [UsedImplicitly]
        public void ShowGamesManagementMenu()
        {
#if !CGS_SINGLEGAME
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            GamesManagement.Show();
#endif
        }

        [UsedImplicitly]
        public void StartGame()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.IsSearchingForServer = false;
            SceneManager.LoadScene(Tags.PlayModeSceneIndex);
        }

        [UsedImplicitly]
        public void JoinGame()
        {
#if !UNITY_WEBGL
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.IsSearchingForServer = true;
            SceneManager.LoadScene(Tags.PlayModeSceneIndex);
#endif
        }

        [UsedImplicitly]
        public void EditDeck()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(Tags.DeckEditorSceneIndex);
        }

        [UsedImplicitly]
        public void ExploreCards()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(Tags.CardsExplorerSceneIndex);
        }

        [UsedImplicitly]
        public void ShowSettings()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(Tags.SettingsSceneIndex);
        }

        [UsedImplicitly]
        public void PromptQuit()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
#if UNITY_ANDROID
            Quit();
#else
            CardGameManager.Instance.Messenger.Prompt(QuitPrompt, Quit);
#endif
        }

        [UsedImplicitly]
        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void Quit() =>
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
