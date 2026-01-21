/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using JetBrains.Annotations;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Menu
{
    public class MainMenu : MonoBehaviour, IDragHandler, IEndDragHandler
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
        private const float AnimationDuration = 0.25f;

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

        public Button fullscreenButton;
        public GameObject quitButton;

#if !CGS_SINGLEGAME
        private GamesManagementMenu GamesManagement =>
            _gamesManagement ??= Instantiate(gamesManagementMenuPrefab).GetOrAddComponent<GamesManagementMenu>();
#endif

        private GamesManagementMenu _gamesManagement;

        private bool _isAnimating;

        private InputAction _moveAction;
        private InputAction _pageAction;

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(ResetGameSelectionCarousel);
            CardGameManager.Instance.OnSceneActions.Add(SetCopyright);

            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed += InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed += InputSelectNext;
            InputSystem.actions.FindAction(Tags.MainMenuGamesManagementMenu).performed += InputGamesManagementMenu;
            InputSystem.actions.FindAction(Tags.MainMenuSettings).performed += InputSettings;
            InputSystem.actions.FindAction(Tags.MainMenuExploreCards).performed += InputExploreCards;
            InputSystem.actions.FindAction(Tags.MainMenuEditDeck).performed += InputEditDeck;
            InputSystem.actions.FindAction(Tags.MainMenuJoinGame).performed += InputJoinGame;
            InputSystem.actions.FindAction(Tags.MainMenuStartGame).performed += InputStartGame;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
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

            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            var pageHorizontal = _pageAction?.ReadValue<Vector2>().x ?? 0;
            var moveHorizontal = _moveAction?.ReadValue<Vector2>().x ?? 0;
            var moveVertical = _moveAction?.ReadValue<Vector2>().y ?? 0;
            if (_pageAction?.WasPressedThisFrame() ?? false)
            {
                if (pageVertical < 0)
                    SelectNext();
                else if (pageVertical > 0)
                    SelectPrevious();
                else if (pageHorizontal < 0)
                    SelectPrevious();
                else if (pageHorizontal > 0)
                    SelectNext();
            }
            else if ((_moveAction?.WasPressedThisFrame() ?? false) && Mathf.Abs(moveHorizontal) > 0 &&
                     (EventSystem.current.currentSelectedGameObject == null ||
                     EventSystem.current.currentSelectedGameObject == selectableButtons[0].gameObject))
            {
                if (moveHorizontal < 0)
                    SelectPrevious();
                else if (moveHorizontal > 0)
                    SelectNext();
            }
            else if ((_moveAction?.WasPressedThisFrame() ?? false) && Mathf.Abs(moveVertical) > 0 &&
                     !selectableButtons.Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.SetSelectedGameObject(selectableButtons[0].gameObject);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required by the interface to get OnEndDrag called
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;

            var dragDelta = eventData.position - eventData.pressPosition;
            var swipeDirection = UnityExtensionMethods.UnityExtensionMethods.GetSwipeDirection(dragDelta);
            if (swipeDirection == UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Left)
                SelectNext();
            else if (swipeDirection == UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Right)
                SelectPrevious();
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

        private void ResetGameSelectionCarousel()
        {
            currentGameNameText.text = CardGameManager.Current.Name;
            currentCardImage.sprite = CardGameManager.Current.CardBackImageSprite;
            currentBannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            previousCardImage.sprite = CardGameManager.Instance.Previous.CardBackImageSprite;
            nextCardImage.sprite = CardGameManager.Instance.Next.CardBackImageSprite;
        }

        private void InputSelectPrevious(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                SelectPrevious();
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

        private void InputSelectNext(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                SelectNext();
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

        private void InputGamesManagementMenu(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                ShowGamesManagementMenu();
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

        private void InputSettings(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                ShowSettings();
        }

        [UsedImplicitly]
        public void ShowSettings()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(Tags.SettingsSceneIndex);
        }

        private void InputExploreCards(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                ExploreCards();
        }

        [UsedImplicitly]
        public void ExploreCards()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(Tags.CardsExplorerSceneIndex);
        }

        private void InputEditDeck(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                EditDeck();
        }

        [UsedImplicitly]
        public void EditDeck()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(Tags.DeckEditorSceneIndex);
        }

        private void InputJoinGame(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                JoinGame();
        }

        [UsedImplicitly]
        public void JoinGame()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.IsSearchingForServer = true;
            SceneManager.LoadScene(Tags.PlayModeSceneIndex);
        }

        private void InputStartGame(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas == null)
                StartGame();
        }

        [UsedImplicitly]
        public void StartGame()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.IsSearchingForServer = false;
            SceneManager.LoadScene(Tags.PlayModeSceneIndex);
        }

        private void InputCancel(InputAction.CallbackContext context)
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;
            if (EventSystem.current.currentSelectedGameObject == null)
                PromptQuit();
            else if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
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

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed -= InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed -= InputSelectNext;
            InputSystem.actions.FindAction(Tags.MainMenuGamesManagementMenu).performed -= InputGamesManagementMenu;
            InputSystem.actions.FindAction(Tags.MainMenuSettings).performed -= InputSettings;
            InputSystem.actions.FindAction(Tags.MainMenuExploreCards).performed -= InputExploreCards;
            InputSystem.actions.FindAction(Tags.MainMenuEditDeck).performed -= InputEditDeck;
            InputSystem.actions.FindAction(Tags.MainMenuJoinGame).performed -= InputJoinGame;
            InputSystem.actions.FindAction(Tags.MainMenuStartGame).performed -= InputStartGame;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
