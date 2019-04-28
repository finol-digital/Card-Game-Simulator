/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using CardGameDef;

namespace CGS.Menu
{
    public class MainMenu : MonoBehaviour
    {
        public const int MainMenuSceneIndex = 1;
        public const int PlayModeSceneIndex = 2;
        public const int DeckEditorSceneIndex = 3;
        public const int CardsExplorerSceneIndex = 4;
        public const int SettingsSceneIndex = 5;
        public const string VersionMessage = "VERSION ";

        public DownloadMenu downloadMenu;
        public GameObject gameManagement;
        public Image currentCardImage;
        public Image currentBannerImage;
        public Image previousCardImage;
        public Image nextCardImage;
        public List<GameObject> selectableButtons;
        public GameObject quitButton;
        public Text versionText;

        private bool _wasLeft;
        private bool _wasRight;
        private bool _wasPageDown;
        private bool _wasPageUp;
        private bool _wasPageLeft;
        private bool _wasPageRight;
        private bool _wasFocusBack;
        private bool _wasFocusNext;

        void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(ResetGameSelectionCarousel);
        }

        void Start()
        {
#if (UNITY_ANDROID || UNITY_IOS)
            quitButton.SetActive(false);
#else
            quitButton.SetActive(true);
#endif
            versionText.text = VersionMessage + Application.version;
        }

        void Update()
        {
            if (CardGameManager.Instance.TopMenuCanvas != null)
                return;

            if (Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0)
            {
                if (Input.GetAxis(Inputs.PageVertical) < 0 && !_wasPageDown)
                    SelectNext();
                else if (Input.GetAxis(Inputs.PageVertical) > 0 && !_wasPageUp)
                    SelectPrevious();
            }
            else if ((Input.GetButtonDown(Inputs.PageHorizontal) || Input.GetAxis(Inputs.PageHorizontal) != 0))
            {
                if (Input.GetAxis(Inputs.PageHorizontal) < 0 && !_wasPageLeft)
                    SelectPrevious();
                else if (Input.GetAxis(Inputs.PageHorizontal) > 0 && !_wasPageRight)
                    SelectNext();
            }
            else if ((Input.GetButtonDown(Inputs.Horizontal) || Input.GetAxis(Inputs.Horizontal) != 0) &&
                    (EventSystem.current.currentSelectedGameObject == null
                    || EventSystem.current.currentSelectedGameObject == selectableButtons[0].gameObject))
            {
                if (Input.GetAxis(Inputs.Horizontal) < 0 && !_wasLeft)
                    SelectPrevious();
                else if (Input.GetAxis(Inputs.Horizontal) > 0 && !_wasRight)
                    SelectNext();
            }
            else if ((Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
                    && !selectableButtons.Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.SetSelectedGameObject(selectableButtons[0].gameObject);

            if (Input.GetKeyDown(Inputs.BluetoothReturn))
                EventSystem.current.currentSelectedGameObject?.GetComponent<Button>()?.onClick?.Invoke();
            else if (Input.GetButtonDown(Inputs.Sort))
                SelectPrevious();
            else if (Input.GetButtonDown(Inputs.Filter))
                SelectNext();
            else if (Input.GetButtonDown(Inputs.New))
            {
                if (gameManagement.activeSelf)
                    Download();
                else
                    StartGame();
            }
            else if (Input.GetButtonDown(Inputs.Load))
            {
                if (gameManagement.activeSelf)
                    Download();
                else
                    JoinGame();
            }
            else if (Input.GetButtonDown(Inputs.Save))
            {
                if (gameManagement.activeSelf)
                    Share();
                else
                    EditDeck();
            }
            else if (Input.GetButtonDown(Inputs.Option))
            {
                if (gameManagement.activeSelf)
                    Delete();
                else
                    ExploreCards();
            }
            else if (Input.GetButtonDown(Inputs.FocusBack) || (Input.GetAxis(Inputs.FocusBack) > 0 && !_wasFocusBack))
                ToggleGameManagement();
            else if (Input.GetButtonDown(Inputs.FocusNext) || (Input.GetAxis(Inputs.FocusNext) > 0 && !_wasFocusNext))
                ShowSettings();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                    Quit();
                else if (!EventSystem.current.alreadySelecting)
                    EventSystem.current.SetSelectedGameObject(null);
            }

            _wasLeft = Input.GetAxis(Inputs.Horizontal) < 0;
            _wasRight = Input.GetAxis(Inputs.Horizontal) > 0;
            _wasPageDown = Input.GetAxis(Inputs.PageVertical) < 0;
            _wasPageUp = Input.GetAxis(Inputs.PageVertical) > 0;
            _wasPageLeft = Input.GetAxis(Inputs.PageHorizontal) < 0;
            _wasPageRight = Input.GetAxis(Inputs.PageHorizontal) > 0;
            _wasFocusBack = Input.GetAxis(Inputs.FocusBack) > 0;
            _wasFocusNext = Input.GetAxis(Inputs.FocusNext) > 0;
        }

        public void ResetGameSelectionCarousel()
        {
            currentCardImage.sprite = CardGameManager.Current.CardBackImageSprite;
            currentBannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            previousCardImage.sprite = CardGameManager.Instance.Previous.CardBackImageSprite;
            nextCardImage.sprite = CardGameManager.Instance.Next.CardBackImageSprite;
        }

        public void ToggleGameManagement()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            if (gameManagement.activeSelf)
                gameManagement.SetActive(false);
            else
                gameManagement.SetActive(true);
        }

        public void SelectPrevious()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            gameManagement.SetActive(false);
            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
        }

        public void SelectNext()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            gameManagement.SetActive(false);
            CardGameManager.Instance.Select(CardGameManager.Instance.Next.Id);
        }

        public void Download()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            downloadMenu.Show();
        }

        public void Delete()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            CardGameManager.Instance.PromptDelete();
        }

        public void Share()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            CardGameManager.Instance.Share();
        }

        public void StartGame()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            CardGameManager.Instance.IsSearching = false;
            SceneManager.LoadScene(PlayModeSceneIndex);
        }

        public void JoinGame()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            CardGameManager.Instance.IsSearching = true;
            SceneManager.LoadScene(PlayModeSceneIndex);
        }

        public void EditDeck()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            SceneManager.LoadScene(DeckEditorSceneIndex);
        }

        public void ExploreCards()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            SceneManager.LoadScene(CardsExplorerSceneIndex);
        }

        public void ShowSettings()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            SceneManager.LoadScene(SettingsSceneIndex);
        }

        public void Quit() =>
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif

    }
}
