/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

using CardGameDef;
using CardGameView;
using CGS.Menus;
using CGS.Play;
using CGS.Play.Multiplayer;

namespace CGS
{
    public class CardGameManager : MonoBehaviour
    {
        public const string GameName = "GameName";
        public const string GameUrl = "GameUrl";
        public const string PlayerPrefGameName = "DefaultGame";
        public const string SelectorPrefabName = "Game Selection Menu";
        public const string MessengerPrefabName = "Popup";
        public const string SpinnerPrefabName = "Spinner";
        public const string InvalidGameSelectionMessage = "Could not select the card game because the name is not recognized in the list of card games! Try selecting a different card game.";
        public const string GameLoadErrorMessage = "Error loading game!: ";
        public const string GameDeleteErrorMessage = "Error deleting game!: ";
        public const string CardsLoadedMessage = "{0} cards loaded!";
        public const string CardsLoadingMessage = "{0} cards loading...";
        public const int CardsLoadingMessageThreshold = 60;
        public const int PixelsPerInch = 100;

        public static CardGameManager Instance
        {
            get
            {
                if (IsQuitting) return null;
                if (_instance != null) return _instance;
                GameObject cardGameManager = GameObject.FindGameObjectWithTag(Tags.CardGameManager);
                if (cardGameManager == null)
                {
                    cardGameManager = new GameObject(Tags.CardGameManager) { tag = Tags.CardGameManager };
                    cardGameManager.transform.position = Vector3.zero;
                }
                _instance = cardGameManager.GetOrAddComponent<CardGameManager>();
                return _instance;
            }
        }
        private static CardGameManager _instance;

        public static CardGame Current { get; private set; } = CardGame.Invalid;
        public static bool IsQuitting { get; private set; } = false;

        public SortedDictionary<string, CardGame> AllCardGames { get; } = new SortedDictionary<string, CardGame>();
        public List<UnityAction> OnSceneActions { get; } = new List<UnityAction>();

        public LobbyDiscovery Discovery => _discovery ?? (_discovery = gameObject.GetOrAddComponent<LobbyDiscovery>());
        private LobbyDiscovery _discovery;

        public GameSelectionMenu Selector
        {
            get
            {
                if (_selector != null) return _selector;
                _selector = Instantiate(Resources.Load<GameObject>(SelectorPrefabName)).GetOrAddComponent<GameSelectionMenu>();
                _selector.transform.SetParent(null);
                return _selector;
            }
        }
        private GameSelectionMenu _selector;

        public Popup Messenger
        {
            get
            {
                if (_messenger != null) return _messenger;
                _messenger = Instantiate(Resources.Load<GameObject>(MessengerPrefabName)).GetOrAddComponent<Popup>();
                _messenger.transform.SetParent(transform);
                return _messenger;
            }
        }
        private Popup _messenger;

        public SpinningLoadingPanel Spinner
        {
            get
            {
                if (_spinner != null) return _spinner;
                _spinner = Instantiate(Resources.Load<GameObject>(SpinnerPrefabName)).GetOrAddComponent<SpinningLoadingPanel>();
                _spinner.transform.SetParent(transform);
                return _spinner;
            }
        }
        private SpinningLoadingPanel _spinner;

        public Image BackgroundImage
        {
            get
            {
                if (_backgroundImage == null && GameObject.FindGameObjectWithTag(Tags.BackgroundImage) != null)
                    _backgroundImage = GameObject.FindGameObjectWithTag(Tags.BackgroundImage).GetOrAddComponent<Image>();
                return _backgroundImage;
            }
        }
        private Image _backgroundImage;

        // TODO: IMPROVE PERFORMANCE; MAYBE TRACK THIS USING OBSERVER PATTERN?
        public Canvas TopCardCanvas
        {
            get
            {
                Canvas topCanvas = null;
                foreach (GameObject canvas in GameObject.FindGameObjectsWithTag(Tags.CardCanvas))
                    if (canvas.activeSelf && (topCanvas == null || canvas.GetComponent<Canvas>().sortingOrder > topCanvas.sortingOrder))
                        topCanvas = canvas.GetComponent<Canvas>();
                return topCanvas;
            }
        }

        // TODO: IMPROVE PERFORMANCE; MAYBE TRACK THIS USING OBSERVER PATTERN?
        public Canvas TopMenuCanvas
        {
            get
            {
                Canvas topCanvas = null;
                foreach (GameObject canvas in GameObject.FindGameObjectsWithTag(Tags.MenuCanvas))
                    if (canvas.activeSelf && (topCanvas == null || canvas.GetComponent<Canvas>().sortingOrder > topCanvas.sortingOrder))
                        topCanvas = canvas.GetComponent<Canvas>();
                return topCanvas;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            CardGame.Invalid.CoroutineRunner = this;
            DontDestroyOnLoad(gameObject);

            if (!Directory.Exists(CardGame.GamesDirectoryPath))
                CreateDefaultCardGames();
            LookupCardGames();

            CardGame currentGame;
            Current = AllCardGames.TryGetValue(PlayerPrefs.GetString(PlayerPrefGameName), out currentGame)
                 ? currentGame : AllCardGames.First().Value;

            if (Debug.isDebugBuild)
                Application.logMessageReceived += ShowLogToUser;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void CreateDefaultCardGames()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            UnityExtensionMethods.ExtractAndroidStreamingAssets(CardGame.GamesDirectoryPath);
#else
            UnityExtensionMethods.CopyDirectory(Application.streamingAssetsPath, CardGame.GamesDirectoryPath);
#endif
        }

        private void LookupCardGames()
        {
            if (!Directory.Exists(CardGame.GamesDirectoryPath) || Directory.GetDirectories(CardGame.GamesDirectoryPath).Length < 1)
                CreateDefaultCardGames();

            foreach (string gameDirectory in Directory.GetDirectories(CardGame.GamesDirectoryPath))
            {
                string gameName = gameDirectory.Substring(CardGame.GamesDirectoryPath.Length + 1);
                AllCardGames[gameName] = new CardGame(this, gameName, string.Empty);
            }
        }

        void ShowLogToUser(string logString, string stackTrace, LogType type)
        {
            Messenger.Show(logString);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DoGameSceneActions();
        }

        void OnSceneUnloaded(Scene scene)
        {
            OnSceneActions.Clear();
        }

        public void BranchCallbackWithParams(Dictionary<string, object> parameters, string error)
        {
            if (error != null)
            {
                Debug.LogError(error);
                return;
            }

            object gameName, gameUrl;
            if (parameters.TryGetValue(GameName, out gameName) && (gameName is string) && AllCardGames.ContainsKey((string)gameName))
                SelectCardGame((string)gameName);
            else if (parameters.TryGetValue(GameUrl, out gameUrl) && (gameUrl is string) && !string.IsNullOrEmpty((string)gameUrl))
                DownloadCardGame((string)gameUrl);
        }

        public IEnumerator DownloadCardGame(string gameUrl)
        {
            Spinner.Show();
            CardGame newGame = new CardGame(this, CardGame.DefaultName, gameUrl) { AutoUpdate = true };
            Current = newGame;
            yield return newGame.Download();
            if (string.IsNullOrEmpty(newGame.Error))
                AllCardGames[newGame.Name] = newGame;
            else
                Debug.LogError(GameLoadErrorMessage + newGame.Error);
            SelectCardGame(newGame.Name);
            Spinner.Hide();
        }

        public void SelectLeft()
        {
            string prevGameName = AllCardGames.Keys.Last();
            SortedDictionary<string, CardGame>.Enumerator allCardGamesEnum = AllCardGames.GetEnumerator();
            bool found = false;
            while (!found && allCardGamesEnum.MoveNext())
            {
                if (!allCardGamesEnum.Current.Key.Equals(Current.Name))
                    prevGameName = allCardGamesEnum.Current.Key;
                else
                    found = true;
            }
            SelectCardGame(prevGameName);
        }

        public void SelectRight()
        {
            string nextGameName = Current.Name;
            SortedDictionary<string, CardGame>.Enumerator allCardGamesEnum = AllCardGames.GetEnumerator();
            bool found = false;
            while (!found && allCardGamesEnum.MoveNext())
                if (allCardGamesEnum.Current.Key.Equals(Current.Name))
                    found = true;
            if (allCardGamesEnum.MoveNext())
                nextGameName = allCardGamesEnum.Current.Key;
            else if (found)
                nextGameName = AllCardGames.Keys.First();
            SelectCardGame(nextGameName);
        }

        public void SelectCardGame(string gameName, string gameUrl)
        {
            if (string.IsNullOrEmpty(gameName) || !AllCardGames.ContainsKey(gameName))
            {
                StartCoroutine(DownloadCardGame(gameUrl));
                return;
            }
            SelectCardGame(gameName);
        }

        public void SelectCardGame(string gameName)
        {
            if (string.IsNullOrEmpty(gameName) || !AllCardGames.ContainsKey(gameName))
            {
                Debug.LogError(InvalidGameSelectionMessage);
                Selector.Show();
                return;
            }

            Current = AllCardGames[gameName];
            DoGameSceneActions();
        }

        public void DoGameSceneActions()
        {
            if (!Current.IsLoaded)
                Current.Load();

            if (!string.IsNullOrEmpty(Current.Error))
                Debug.LogError(GameLoadErrorMessage + Current.Error);
            else
                PlayerPrefs.SetString(PlayerPrefGameName, Current.Name);

            if (BackgroundImage != null)
                BackgroundImage.sprite = Current.BackgroundImageSprite;
            CardInfoViewer.Instance?.ResetInfo();

            for (int i = OnSceneActions.Count - 1; i >= 0; i--)
                if (OnSceneActions[i] == null)
                    OnSceneActions.RemoveAt(i);
            foreach (UnityAction action in OnSceneActions)
                action();
        }

        public IEnumerator LoadCards()
        {
            yield return Current.LoadAllCards();
            if (!string.IsNullOrEmpty(Current.Error))
                Debug.LogError(GameLoadErrorMessage + Current.Error);
        }

        public void DeleteGame()
        {
            try
            {
                Directory.Delete(Current.GameFolderPath, true);
                AllCardGames.Remove(Current.Name);
                SelectCardGame(AllCardGames.Keys.First());
                Selector.Show();
            }
            catch (Exception ex)
            {
                Debug.LogError(GameDeleteErrorMessage + ex.Message);
            }
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnApplicationQuit()
        {
            IsQuitting = true;
        }
    }
}
