/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using CardGameDef;

namespace CGS.Menus
{
    public class MainMenu : MonoBehaviour
    {
        public const int MainMenuSceneIndex = 1;
        public const int PlayModeSceneIndex = 2;
        public const int DeckEditorSceneIndex = 3;
        public const int CardsExplorerSceneIndex = 4;
        public const int OptionsMenuSceneIndex = 5;
        public const string VersionMessage = "Ver. ";

        public List<GameObject> buttons;
        public GameObject quitButton;
        public Text versionText;

        private bool _wasLeft;
        private bool _wasRight;

        void Start()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            quitButton.SetActive(false);
#endif
            versionText.text = VersionMessage + Application.version;

            if (CardGameManager.Instance.Discovery.running)
                CardGameManager.Instance.Discovery.StopBroadcast();
            CardGameManager.Instance.Discovery.HasReceivedBroadcast = false;
            CardGameManager.Instance.Discovery.SearchForHost();
        }

        void Update()
        {
            if (CardGameManager.Instance.TopMenuCanvas != null)
                return;

            if ((Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
                    && !buttons.Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.SetSelectedGameObject(buttons[1].gameObject);
            if (Input.GetButtonDown(Inputs.Horizontal) || Input.GetAxis(Inputs.Horizontal) != 0)
            {
                if (Input.GetAxis(Inputs.Horizontal) < 0 && !_wasLeft)
                    CardGameManager.Instance.SelectLeft();
                else if (Input.GetAxis(Inputs.Horizontal) > 0 && !_wasRight)
                    CardGameManager.Instance.SelectRight();
            }

            if (Input.GetKeyDown(Inputs.BluetoothReturn))
                EventSystem.current.currentSelectedGameObject?.GetComponent<Button>()?.onClick?.Invoke();
            else if (Input.GetButtonDown(Inputs.Sort))
                ShowGameSelectionMenu();
            else if (Input.GetButtonDown(Inputs.New))
                StartGame();
            else if (Input.GetButtonDown(Inputs.Load))
                JoinGame();
            else if (Input.GetButtonDown(Inputs.Save))
                EditDeck();
            else if (Input.GetButtonDown(Inputs.Filter))
                ExploreCards();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Quit();

            _wasLeft = Input.GetAxis(Inputs.Horizontal) < 0;
            _wasRight = Input.GetAxis(Inputs.Horizontal) > 0;
        }

        public void ShowGameSelectionMenu()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            CardGameManager.Instance.Selector.Show();
        }

        public void StartGame()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            if (CardGameManager.Instance.Discovery.running)
                CardGameManager.Instance.Discovery.StopBroadcast();
            CardGameManager.Instance.Discovery.HasReceivedBroadcast = false;
            SceneManager.LoadScene(PlayModeSceneIndex);
        }

        public void PlayGame()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            SceneManager.LoadScene(PlayModeSceneIndex);
        }

        public void JoinGame()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            CardGameManager.Instance.Discovery.HasReceivedBroadcast = true;
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
            CardGameManager.Instance.Messenger.Show("Coming Soon!");
        }

        public void ShowOptions()
        {
            if (Time.timeSinceLevelLoad < 0.1)
                return;
            SceneManager.LoadScene(OptionsMenuSceneIndex);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WSA
            System.Diagnostics.Process.GetCurrentProcess().Kill();
#else
            Application.Quit();
#endif
        }
    }
}
