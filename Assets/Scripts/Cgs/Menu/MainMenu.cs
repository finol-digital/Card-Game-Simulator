/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Performance;
using Cgs.Localization;
using Cgs.GameContent;
using Cgs.Play.Multiplayer;
using System.Collections.Generic;
using JetBrains.Annotations;
using PrimeTween;
using Unity.Netcode;
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
            set
            {
                // Benchmark runs must not change saved preferences
                if (!StartupTimeline.IsBenchmarking)
                    PlayerPrefs.SetInt(PlayerPrefsHasSeenWelcome, value ? 1 : 0);
            }
        }

        public const string QuitPrompt = "Quit?";

        public static string VersionMessage => $"VERSION {Application.version}";

        public static UiMessage LocalizedVersion =>
            UiMessage.With("title.version", VersionMessage, ("version", Application.version));

        private const float AnimationDuration = 0.25f;

        /// <summary>
        /// The first Main Menu after launch stays hidden for at most this long while the startup language loads, so
        /// it does not show English first. After that it shows in English and switches in place when the language
        /// arrives. developer-docs/startup-performance.md records the measurements behind this bound.
        /// </summary>
        internal const float DefaultStartupLanguageWaitSeconds = 0.5f;

        internal static float StartupLanguageWaitSeconds { get; set; } = DefaultStartupLanguageWaitSeconds;

        private static bool _startupLanguageSettled;

        [SerializeField] GameObject gamesManagementMenuPrefab;

        [SerializeField] Text versionText;
        [SerializeField] Text copyrightText;
        [SerializeField] Text currentGameNameText;
        [SerializeField] Image currentCardImage;
        [SerializeField] Image currentBannerImage;
        [SerializeField] Image previousCardImage;
        [SerializeField] Image nextCardImage;
        [SerializeField] Image offLeftImage;
        [SerializeField] Image offRightImage;
        [SerializeField] List<GameObject> selectableButtons;

        [SerializeField] Button fullscreenButton;
        [SerializeField] GameObject quitButton;

#if !CGS_SINGLEGAME
        private GamesManagementMenu GamesManagement =>
            _gamesManagement ??= Instantiate(gamesManagementMenuPrefab).GetOrAddComponent<GamesManagementMenu>();
#endif

        private GamesManagementMenu _gamesManagement;

        private readonly HeldInputGate _heldInput = new();

        private bool _isAnimating;

        private InputAction _moveAction;
        private InputAction _pageAction;

        private CanvasGroup _languageWait;
        private float _languageWaitStarted;
        private int _languageReadyFrame = -1;
        private bool _isWelcomeDeferred;

        /// <summary>How long the menu is shown before it creates the network manager.</summary>
        internal const float NetworkPrepareDelaySeconds = 0.5f;

        private float _interactiveTime = -1;

        /// <summary>Hidden, and ignoring input, while the first Main Menu waits for the startup language.</summary>
        internal bool IsWaitingForLanguage => _languageWait != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStartupLanguageWait()
        {
            _startupLanguageSettled = false;
            StartupLanguageWaitSeconds = DefaultStartupLanguageWaitSeconds;
        }

        // Later Main Menus in this session show at once
        private static void SettleStartupLanguage() => _startupLanguageSettled = true;

        protected void OnEnable()
        {
            if (!_startupLanguageSettled)
            {
                // The menu is authored in English, so only another startup language is worth waiting for
                if (CgsLocalization.IsInitializationComplete
                    || CgsLocalization.StartupLanguage is null or CgsLocaleSelector.EnglishCode)
                    SettleStartupLanguage();
                else
                    BeginLanguageWait();
            }

            _heldInput.Arm();

            CardGameManager.Instance.OnSceneActions.Add(ResetGameSelectionCarousel);
            CardGameManager.Instance.OnSceneActions.Add(SetCopyright);
            _gamesSource = CardGameManager.Instance;
            _gamesSource.GamesAdded += ResetGameSelectionCarousel;

            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed += InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed += InputSelectNext;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
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
            GameContentText.Set(copyrightText, CardGameManager.Current, display =>
                string.IsNullOrWhiteSpace(display.Copyright) ? FinolDigitalLlc : display.Copyright);
        }

        protected void Start()
        {
            LocalizedUiText.Set(versionText, LocalizedVersion);

#if UNITY_WEBGL
            fullscreenButton.gameObject.SetActive(true);
            quitButton.gameObject.SetActive(false);
#endif
#if UNITY_STANDALONE || UNITY_WSA
            quitButton.SetActive(true);
#else
            quitButton.SetActive(false);
#endif

            SelectFirstControl();

            if (!HasSeenWelcome)
            {
                if (IsWaitingForLanguage)
                    _isWelcomeDeferred = true;
                else
                    AskWelcome();
            }

            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        private static void AskWelcome()
        {
            CardGameManager.Instance.Messenger.Ask(new UiMessage(
#if UNITY_ANDROID || UNITY_IOS
                "welcome.mobile",
#else
                "welcome.desktop",
#endif
                WelcomeMessage), DeclineWelcomeMessage, AcceptWelcomeMessage, true);
        }

        private void BeginLanguageWait()
        {
            _languageWait = gameObject.AddComponent<CanvasGroup>();
            _languageWait.alpha = 0;
            _languageWait.interactable = false;
            _languageWait.blocksRaycasts = false;
            _languageWaitStarted = -1;
            _languageReadyFrame = -1;
        }

        // True once the menu is shown: the frame after the language is ready, so text and font bindings have applied
        // it, or when the wait runs out. The wait counts from the menu's first frame, not from loading the scene.
        private bool TryEndLanguageWait()
        {
            if (_languageWaitStarted < 0)
                _languageWaitStarted = Time.realtimeSinceStartup;

            if (CgsLocalization.IsInitializationComplete)
            {
                if (_languageReadyFrame < 0)
                    _languageReadyFrame = Time.frameCount;
                if (Time.frameCount == _languageReadyFrame)
                    return false;
            }
            else if (Time.realtimeSinceStartup - _languageWaitStarted < StartupLanguageWaitSeconds)
                return false;
            else
                Debug.LogWarning($"MainMenu: the startup language was not ready after " +
                                 $"{StartupLanguageWaitSeconds * 1000:0} ms; showing English until it loads");

            _languageWait.alpha = 1;
            _languageWait.interactable = true;
            _languageWait.blocksRaycasts = true;
            Destroy(_languageWait);
            _languageWait = null;
            SettleStartupLanguage();
            // Like a newly opened screen, ignore what was already held while the menu was hidden
            _heldInput.Arm();
            if (_isWelcomeDeferred)
            {
                _isWelcomeDeferred = false;
                AskWelcome();
            }

            return true;
        }

        // Shortcuts and swipes act only on the visible menu, without a dialog over it
        private bool AcceptsInput => !IsWaitingForLanguage && CardGameManager.Instance.ModalCanvas == null;

        // Focus the current game so the first gamepad or keyboard input acts on it, as Submit did with nothing focused.
        // Touch-first devices without a gamepad show no focus highlight.
        private void SelectFirstControl()
        {
            if (!ActiveInputDevice.UseGamepad && Application.isMobilePlatform)
                return;
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(selectableButtons[0]);
        }

        // Poll for Vector2 inputs
        protected void Update()
        {
            if (IsWaitingForLanguage && !TryEndLanguageWait())
                return;

            // Interactive means the Main Menu screen accepts input, which it does from its first frame. A first-launch
            // welcome prompt is part of that screen and accepts input too; budget runs use saved data, where the
            // prompt is not shown.
            StartupTimeline.Mark(StartupMilestone.MainMenuInteractive);
            PrepareNetworkWhenIdle();
            _heldInput.Refresh();

            if (!AcceptsInput)
                return;

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            var pageHorizontal = _pageAction?.ReadValue<Vector2>().x ?? 0;
            var moveHorizontal = _moveAction?.ReadValue<Vector2>().x ?? 0;
            var moveVertical = _moveAction?.ReadValue<Vector2>().y ?? 0;
            if ((_pageAction?.WasPressedThisFrame() ?? false) && _heldInput.Allows(_pageAction.activeControl))
            {
                if (pageVertical < 0)
                    DoSelectNext();
                else if (pageVertical > 0)
                    DoSelectPrevious();
                else if (pageHorizontal < 0)
                    DoSelectPrevious();
                else if (pageHorizontal > 0)
                    DoSelectNext();
            }
            else if ((_moveAction?.WasPressedThisFrame() ?? false) && _heldInput.Allows(_moveAction.activeControl) &&
                     Mathf.Abs(moveHorizontal) > 0 &&
                     (EventSystem.current.currentSelectedGameObject == null ||
                      EventSystem.current.currentSelectedGameObject == selectableButtons[0].gameObject))
            {
                if (moveHorizontal < 0)
                    DoSelectPrevious();
                else if (moveHorizontal > 0)
                    DoSelectNext();
            }
            else if ((_moveAction?.WasPressedThisFrame() ?? false) && _heldInput.Allows(_moveAction.activeControl) &&
                     Mathf.Abs(moveVertical) > 0 &&
                     !selectableButtons.Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.SetSelectedGameObject(selectableButtons[0].gameObject);
        }

        // The play screen needs the network manager. Creating it while the menu is shown keeps it out of both launch
        // and Start Game; if Start Game comes first, the play screen creates it.
        private void PrepareNetworkWhenIdle()
        {
            if (_interactiveTime < 0)
                _interactiveTime = Time.realtimeSinceStartup;
            else if (Time.realtimeSinceStartup - _interactiveTime >= NetworkPrepareDelaySeconds &&
                     NetworkManager.Singleton == null)
                CgsNetManager.GetOrCreate();
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required by the interface to get OnEndDrag called
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!AcceptsInput)
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
            GameContentText.Set(currentGameNameText, CardGameManager.Current, display => display.GameName);
            GameContentImage.Set(currentBannerImage, CardGameManager.Current, CardGameManager.Current.BannerImageSprite, false, "bannerImageUrl");
            GameContentImage.Set(currentCardImage, CardGameManager.Current, CardGameManager.Current.CardBackImageSprite, false, "cardBackImageUrl");
            GameContentImage.Set(previousCardImage, CardGameManager.Instance.Previous, CardGameManager.Instance.Previous.CardBackImageSprite, false, "cardBackImageUrl");
            GameContentImage.Set(nextCardImage, CardGameManager.Instance.Next, CardGameManager.Instance.Next.CardBackImageSprite, false, "cardBackImageUrl");
        }

        private void InputSelectPrevious(InputAction.CallbackContext context)
        {
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoSelectPrevious();
        }

        [UsedImplicitly]
        public void SelectPrevious()
        {
            if (_heldInput.AllowsPointerAction)
                DoSelectPrevious();
        }

        private void DoSelectPrevious()
        {
            if (_isAnimating)
                return;

            // Create duplicate cards to animate
            var offLeft = Instantiate(offLeftImage.gameObject, offLeftImage.transform.parent);
            GameContentImage.Set(offLeft.GetOrAddComponent<Image>(), CardGameManager.Instance.Previous2,
                CardGameManager.Instance.Previous2.CardBackImageSprite, false, "cardBackImageUrl");
            var left = Instantiate(previousCardImage.gameObject, previousCardImage.transform.parent);
            Destroy(left.GetComponent<Button>());
            GameContentImage.Set(left.GetComponent<Image>(), CardGameManager.Instance.Previous, CardGameManager.Instance.Previous.CardBackImageSprite, false, "cardBackImageUrl");
            var middle = Instantiate(currentCardImage.gameObject, currentCardImage.transform.parent);
            Destroy(middle.GetComponent<Button>());
            GameContentImage.Set(middle.GetComponent<Image>(), CardGameManager.Current, CardGameManager.Current.CardBackImageSprite, false, "cardBackImageUrl");
            var right = Instantiate(nextCardImage.gameObject, nextCardImage.transform.parent);
            Destroy(right.GetComponent<Button>());
            GameContentImage.Set(right.GetComponent<Image>(), CardGameManager.Instance.Next, CardGameManager.Instance.Next.CardBackImageSprite, false, "cardBackImageUrl");

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
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoSelectNext();
        }

        [UsedImplicitly]
        public void SelectNext()
        {
            if (_heldInput.AllowsPointerAction)
                DoSelectNext();
        }

        private void DoSelectNext()
        {
            if (_isAnimating)
                return;

            // Create duplicate cards to animate
            var offRight = Instantiate(offRightImage.gameObject, nextCardImage.transform.parent);
            GameContentImage.Set(offRight.GetOrAddComponent<Image>(), CardGameManager.Instance.Next2,
                CardGameManager.Instance.Next2.CardBackImageSprite, false, "cardBackImageUrl");
            var right = Instantiate(nextCardImage.gameObject, nextCardImage.transform.parent);
            Destroy(right.GetComponent<Button>());
            GameContentImage.Set(right.GetComponent<Image>(), CardGameManager.Instance.Next, CardGameManager.Instance.Next.CardBackImageSprite, false, "cardBackImageUrl");
            var middle = Instantiate(currentCardImage.gameObject, currentCardImage.transform.parent);
            Destroy(middle.GetComponent<Button>());
            GameContentImage.Set(middle.GetComponent<Image>(), CardGameManager.Current, CardGameManager.Current.CardBackImageSprite, false, "cardBackImageUrl");
            var left = Instantiate(previousCardImage.gameObject, previousCardImage.transform.parent);
            Destroy(left.GetComponent<Button>());
            GameContentImage.Set(left.GetComponent<Image>(), CardGameManager.Instance.Previous, CardGameManager.Instance.Previous.CardBackImageSprite, false, "cardBackImageUrl");

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

        private void InputSubmit(InputAction.CallbackContext context)
        {
            if (EventSystem.current.currentSelectedGameObject == null && AcceptsInput &&
                _heldInput.Allows(context.control))
                DoShowGamesManagementMenu();
        }

        private void InputGamesManagementMenu(InputAction.CallbackContext context)
        {
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoShowGamesManagementMenu();
        }

        [UsedImplicitly]
        public void ShowGamesManagementMenu()
        {
            if (_heldInput.AllowsPointerAction)
                DoShowGamesManagementMenu();
        }

        private void DoShowGamesManagementMenu()
        {
#if !CGS_SINGLEGAME
            GamesManagement.Show();
#endif
        }

        private void InputSettings(InputAction.CallbackContext context)
        {
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoShowSettings();
        }

        [UsedImplicitly]
        public void ShowSettings()
        {
            if (_heldInput.AllowsPointerAction)
                DoShowSettings();
        }

        private static void DoShowSettings()
        {
            SceneManager.LoadScene(Tags.SettingsSceneIndex);
        }

        private void InputExploreCards(InputAction.CallbackContext context)
        {
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoExploreCards();
        }

        [UsedImplicitly]
        public void ExploreCards()
        {
            if (_heldInput.AllowsPointerAction)
                DoExploreCards();
        }

        private static void DoExploreCards()
        {
            SceneManager.LoadScene(Tags.CardsExplorerSceneIndex);
        }

        private void InputEditDeck(InputAction.CallbackContext context)
        {
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoEditDeck();
        }

        [UsedImplicitly]
        public void EditDeck()
        {
            if (_heldInput.AllowsPointerAction)
                DoEditDeck();
        }

        private static void DoEditDeck()
        {
            SceneManager.LoadScene(Tags.DeckEditorSceneIndex);
        }

        private void InputJoinGame(InputAction.CallbackContext context)
        {
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoJoinGame();
        }

        [UsedImplicitly]
        public void JoinGame()
        {
            if (_heldInput.AllowsPointerAction)
                DoJoinGame();
        }

        private static void DoJoinGame()
        {
            CardGameManager.Instance.IsSearchingForServer = true;
            SceneManager.LoadScene(Tags.PlayModeSceneIndex);
        }

        private void InputStartGame(InputAction.CallbackContext context)
        {
            if (AcceptsInput && _heldInput.Allows(context.control))
                DoStartGame();
        }

        [UsedImplicitly]
        public void StartGame()
        {
            if (_heldInput.AllowsPointerAction)
                DoStartGame();
        }

        private static void DoStartGame()
        {
            CardGameManager.Instance.IsSearchingForServer = false;
            StartupTimeline.Mark(StartupMilestone.StartGameChosen);
            SceneManager.LoadScene(Tags.PlayModeSceneIndex);
        }

        private void InputCancel(InputAction.CallbackContext context)
        {
            if (!AcceptsInput || !_heldInput.Allows(context.control))
                return;
            if (EventSystem.current.currentSelectedGameObject == null)
                DoPromptQuit();
            else if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
        }

        [UsedImplicitly]
        public void PromptQuit()
        {
            if (_heldInput.AllowsPointerAction)
                DoPromptQuit();
        }

        private void DoPromptQuit()
        {
#if UNITY_ANDROID
            Quit();
#else
            CardGameManager.Instance.Messenger.Prompt(new UiMessage("prompt.quit", QuitPrompt), Quit);
#endif
        }

        [UsedImplicitly]
        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

#pragma warning disable S2325
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void Quit() =>
#pragma warning restore S2325
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif

        // The manager this menu listens to for newly installed games
        private CardGameManager _gamesSource;

        protected void OnDisable()
        {
            if (_gamesSource != null)
                _gamesSource.GamesAdded -= ResetGameSelectionCarousel;
            _gamesSource = null;

            InputSystem.actions.FindAction(Tags.ViewerSelectPrevious).performed -= InputSelectPrevious;
            InputSystem.actions.FindAction(Tags.ViewerSelectNext).performed -= InputSelectNext;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
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
