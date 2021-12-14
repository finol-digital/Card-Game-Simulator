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

namespace Cgs.Menu
{
    public class MainMenu : MonoBehaviour
    {
        public const string GameLabel = "Download Game";
        public const string GamePrompt = "Enter CGS AutoUpdate URL...";

        public static string VersionMessage => $"VERSION {Application.version}";

        public const string TutorialPrompt =
            "If you are new to Card Game Simulator (CGS), you may want to watch the CGS tutorial video.\nGo to the YouTube video?";

        public const string QuitPrompt = "Quit?";

        private const string TutorialUrl = "https://youtu.be/PriDuaM6MEk";

        private const string PlayerPrefsHasSeenTutorial = "HasSeenTutorial";

        private static bool HasSeenTutorial
        {
            get => PlayerPrefs.GetInt(PlayerPrefsHasSeenTutorial, 0) == 1;
            set => PlayerPrefs.SetInt(PlayerPrefsHasSeenTutorial, value ? 1 : 0);
        }

        public const int MainMenuSceneIndex = 1;
        private const int PlayModeSceneIndex = 2;
        private const int DeckEditorSceneIndex = 3;
        private const int CardsExplorerSceneIndex = 4;
        private const int SettingsSceneIndex = 5;

        private const float StartBufferTime = 0.1f;

        public GameObject downloadMenuPrefab;
        public GameObject createMenuPrefab;
        public GameObject gameManagement;
        public GameObject versionInfo;
        public Text currentGameNameText;
        public Image currentCardImage;
        public Image currentBannerImage;
        public Image previousCardImage;
        public Image nextCardImage;
        public List<GameObject> selectableButtons;

        // ReSharper disable once NotAccessedField.Global
        public GameObject createButton;
        // ReSharper disable once NotAccessedField.Global
        public GameObject editButton;
        public Button joinButton;
        public GameObject quitButton;
        public Text versionText;

        private DownloadMenu Downloader => _downloader
            ? _downloader
            : (_downloader = Instantiate(downloadMenuPrefab)
                .GetOrAddComponent<DownloadMenu>());

        private DownloadMenu _downloader;

        private GameCreationMenu Creator =>
            _creator ? _creator : (_creator = Instantiate(createMenuPrefab).GetOrAddComponent<GameCreationMenu>());

        private GameCreationMenu _creator;

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(ResetGameSelectionCarousel);
        }

        private void Start()
        {
            createButton.SetActive(Settings.DeveloperMode);
            editButton.SetActive(Settings.DeveloperMode);
#if UNITY_WEBGL
            joinButton.interactable = false;
#endif
#if UNITY_STANDALONE || UNITY_WSA
            quitButton.SetActive(true);
#else
            quitButton.SetActive(false);
#endif
            versionText.text = VersionMessage;

            if (!HasSeenTutorial)
                CardGameManager.Instance.Messenger.Ask(TutorialPrompt, ConfirmHasSeenTutorial, GoToTutorial);
        }

        private static void ConfirmHasSeenTutorial()
        {
            HasSeenTutorial = true;
        }

        private static void GoToTutorial()
        {
            ConfirmHasSeenTutorial();
            Application.OpenURL(TutorialUrl);
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
            {
                if (gameManagement.activeSelf && createButton.activeSelf)
                    Create();
                else
                    StartGame();
            }
            else if (Inputs.IsLoad)
            {
                if (gameManagement.activeSelf)
                    Download();
                else
                    JoinGame();
            }
            else if (Inputs.IsSave)
            {
                if (gameManagement.activeSelf)
                    Share();
                else
                    EditDeck();
            }
            else if (Inputs.IsFocusBack && !Inputs.WasFocusBack)
                ToggleGameManagement();
            else if (Inputs.IsFocusNext && !Inputs.WasFocusNext)
            {
                if (gameManagement.activeSelf && editButton.activeSelf)
                    Edit();
                else
                    ExploreCards();
            }
            else if (Inputs.IsOption)
            {
                if (gameManagement.activeSelf)
                    Delete();
                else
                    ShowSettings();
            }
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
            gameManagement.SetActive(!gameManagement.activeSelf);
            versionInfo.SetActive(gameManagement.activeSelf);
            EventSystem.current.SetSelectedGameObject(null);
#endif
        }

        [UsedImplicitly]
        public void SelectPrevious()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            gameManagement.SetActive(false);
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
        }

        [UsedImplicitly]
        public void SelectNext()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            gameManagement.SetActive(false);
            CardGameManager.Instance.Select(CardGameManager.Instance.Next.Id);
        }

        [UsedImplicitly]
        public void Create()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            Creator.Show();
        }

        [UsedImplicitly]
        public void Download()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            Downloader.Show(GameLabel, GamePrompt, CardGameManager.Instance.GetCardGame);
        }

        [UsedImplicitly]
        public void Edit()
        {
            if (Time.timeSinceLevelLoad < StartBufferTime)
                return;
            CardGameManager.Instance.Messenger.Show("Edit is Coming Soon!");
            // TODO: Creator.Edit();
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

        private void Quit() =>
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
