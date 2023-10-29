/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;
#if !UNITY_ANDROID && !UNITY_IOS
#endif

namespace Cgs.Menu
{
    public class MainMenu : MonoBehaviour
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
            set => PlayerPrefs.SetInt(PlayerPrefsHasSeenWelcome, value ? 1 : 0);
        }

        public const string QuitPrompt = "Quit?";

        public const int MainMenuSceneIndex = 1;
        private const int PlayModeSceneIndex = 2;
        private const int DeckEditorSceneIndex = 3;
        private const int CardsExplorerSceneIndex = 4;
        private const int SettingsSceneIndex = 5;

        private const float StartBufferTime = 0.1f;

        public GameObject gamesManagementMenuPrefab;

        public Text versionText;
        public Text copyrightText;
        public Text currentGameNameText;
        public Image currentCardImage;
        public Image currentBannerImage;
        public Image previousCardImage;
        public Image nextCardImage;
        public List<GameObject> selectableButtons;

        // ReSharper disable once NotAccessedField.Global
        public Button joinButton;
        public GameObject quitButton;

        private GamesManagementMenu GamesManagement =>
            _gamesManagement ??= Instantiate(gamesManagementMenuPrefab).GetOrAddComponent<GamesManagementMenu>();

        private GamesManagementMenu _gamesManagement;

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

        private void Start()
        {
            versionText.text = TitleScreen.VersionMessage;

#if UNITY_WEBGL
            joinButton.interactable = false;
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
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
        }

        [UsedImplicitly]
        public void SelectNext()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.Select(CardGameManager.Instance.Next.Id);
        }

        [UsedImplicitly]
        public void ShowGamesManagementMenu()
        {
#if !UNITY_WEBGL
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
            SceneManager.LoadScene(PlayModeSceneIndex);
        }

        [UsedImplicitly]
        public void JoinGame()
        {
#if !UNITY_WEBGL
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.IsSearchingForServer = true;
            SceneManager.LoadScene(PlayModeSceneIndex);
#endif
        }

        [UsedImplicitly]
        public void EditDeck()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(DeckEditorSceneIndex);
        }

        [UsedImplicitly]
        public void ExploreCards()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(CardsExplorerSceneIndex);
        }

        [UsedImplicitly]
        public void ShowSettings()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            SceneManager.LoadScene(SettingsSceneIndex);
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

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void Quit() =>
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
