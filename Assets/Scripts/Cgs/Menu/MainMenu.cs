/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;
#if !UNITY_ANDROID && !UNITY_IOS
using SimpleFileBrowser;
#endif

namespace Cgs.Menu
{
    public class MainMenu : MonoBehaviour
    {
        public const string ImportGamePrompt = "Download from Web URL,\n or Load from ZIP File?";
        public const string DownloadFromWeb = "Download from Web URL";
        public const string LoadFromFile = "Load from ZIP File";
        public const string SelectZipFilePrompt = "Select ZIP File";

        public const string DownloadLabel = "Download Game";
        public const string DownloadPrompt = "Enter CGS AutoUpdate URL...";

        public static string WelcomeMessage => "Welcome to CGS!\n" + WelcomeMessageExt;

#if UNITY_ANDROID || UNITY_IOS
        public static string WelcomeMessageExt =>
            "This Mobile version of CGS is intended as a companion to the PC version of CGS.\n" +
            "The PC version of CGS is available from the CGS website.\n" + "Go to the CGS website?";
#else
        public static string WelcomeMessageExt =>
            "The CGS website has guides/resources that may help new users.\n" + "Go to the CGS website?";
#endif
        private const string PlayerPrefsHasSeenWelcome = "HasSeenWelcome";

        private static bool HasSeenWelcome
        {
            get => PlayerPrefs.GetInt(PlayerPrefsHasSeenWelcome, 0) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsHasSeenWelcome, value ? 1 : 0);
        }

        public static string VersionMessage => $"VERSION {Application.version}";

        public const string QuitPrompt = "Quit?";

        public const int MainMenuSceneIndex = 1;
        private const int PlayModeSceneIndex = 2;
        private const int DeckEditorSceneIndex = 3;
        private const int CardsExplorerSceneIndex = 4;
        private const int SettingsSceneIndex = 5;

        private const float StartBufferTime = 0.1f;

        public GameObject cardGameEditorMenuPrefab;
        public GameObject gameImportModalPrefab;
        public GameObject downloadMenuPrefab;
        public GameObject gamesManagement;
        public GameObject versionInfo;
        public Text currentGameNameText;
        public Image currentCardImage;
        public Image currentBannerImage;
        public Image previousCardImage;
        public Image nextCardImage;
        public List<GameObject> selectableButtons;

        // ReSharper disable once NotAccessedField.Global
        public Button joinButton;
        public GameObject quitButton;
        public Text versionText;

        private CardGameEditorMenu CardGameEditor =>
            _cardGameEditor ??= Instantiate(cardGameEditorMenuPrefab).GetOrAddComponent<CardGameEditorMenu>();

        private CardGameEditorMenu _cardGameEditor;

        private DecisionModal ImportModal =>
            _importModal ??= Instantiate(gameImportModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _importModal;

        private DownloadMenu Downloader => _downloader ??= Instantiate(downloadMenuPrefab)
            .GetOrAddComponent<DownloadMenu>();

        private DownloadMenu _downloader;

#if UNITY_ANDROID || UNITY_IOS
        private static string _zipFileType;
#endif

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(ResetGameSelectionCarousel);
        }

        private void Start()
        {
#if UNITY_WEBGL
            joinButton.interactable = false;
#endif
#if UNITY_STANDALONE || UNITY_WSA
            quitButton.SetActive(true);
#else
            quitButton.SetActive(false);
#endif
            gamesManagement.SetActive(false);
            versionText.text = VersionMessage;
            versionInfo.SetActive(false);

#if UNITY_ANDROID || UNITY_IOS
            _zipFileType = NativeFilePicker.ConvertExtensionToFileType("zip");
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
                ToggleGameManagement();
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
        public void ToggleGameManagement()
        {
#if !UNITY_WEBGL
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            gamesManagement.SetActive(!gamesManagement.activeSelf);
            versionInfo.SetActive(gamesManagement.activeSelf);
            EventSystem.current.SetSelectedGameObject(null);
#endif
        }

        [UsedImplicitly]
        public void SelectPrevious()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            gamesManagement.SetActive(false);
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
        }

        [UsedImplicitly]
        public void SelectNext()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            gamesManagement.SetActive(false);
            CardGameManager.Instance.Select(CardGameManager.Instance.Next.Id);
        }

        [UsedImplicitly]
        public void CreateNew()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameEditor.Show();
        }

        [UsedImplicitly]
        public void Import()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;

            ImportModal.Show(ImportGamePrompt, new Tuple<string, UnityAction>(DownloadFromWeb, ShowDownloader),
                new Tuple<string, UnityAction>(LoadFromFile, ShowFileLoader));
        }

        private void ShowDownloader()
        {
            Downloader.Show(DownloadLabel, DownloadPrompt, CardGameManager.Instance.GetCardGame, true);
        }

        private static void ShowFileLoader()
        {
#if UNITY_ANDROID || UNITY_IOS
            var permission = NativeFilePicker.PickFile(path =>
            {
                if (path == null)
                    Debug.Log("Operation cancelled");
                else
                    CardGameManager.Instance.ImportCardGame(path);
            }, _zipFileType);
            Debug.Log( "Permission result: " + permission );
#else
            FileBrowser.ShowLoadDialog((paths) => CardGameManager.Instance.ImportCardGame(paths[0]),
                () => { }, FileBrowser.PickMode.Files, false, null, null,
                SelectZipFilePrompt);
#endif
        }

        [UsedImplicitly]
        public void Sync()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.StartCoroutine(CardGameManager.Instance.UpdateCardGame(CardGameManager.Current));
        }

        [UsedImplicitly]
        public void Edit()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.Messenger.Show("Edit is Coming Soon!");
        }

        [UsedImplicitly]
        public void Delete()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.PromptDelete();
        }

        [UsedImplicitly]
        public void Share()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.Share();
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
