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
using CGS.Menu;
using CGS.Play.Multiplayer;

namespace CGS
{
    public class CardGameManager : MonoBehaviour
    {
        // Show all Debug.Log() to help with debugging?
        public const bool IsMessengerDebugLogVerbose = false;
        public const string GameId = "GameId";
        public const string PlayerPrefDefaultGame = "DefaultGame";
        public const string BranchCallbackErrorMessage = "Branch Callback Error!: ";
        public const string BranchCallbackWarning = "Branch Callback has GameId, but it is not a string?";
        public const string EmptyNameWarning = "Found game with missing name!";
        public const string DefaultNameWarning = "Found game with default name. Deleting it.";
        public const string SelectionErrorMessage = "Could not select the card game because it is not recognized!";
        public const string DownloadErrorMessage = "Error downloading game!: ";
        public const string LoadErrorMessage = "Error loading game!: ";
        public const string LoadErrorPrompt = "Error loading game! The game may be corrupted. Delete (note that any decks would also be deleted)?";
        public const string CardsLoadedMessage = "{0} cards loaded!";
        public const string CardsLoadingMessage = "{0} cards loading...";
        public const string DeleteErrorMessage = "Error deleting game!: ";
        public const string DeleteWarningMessage = "Please download additional card games before deleting.";
        public const string DeletePrompt = "Deleting a card game also deletes all decks saved for that card game. Are you sure you would like to delete this card game?";
        public const string ShareTitle = "Card Game Simulator - {0}";
        public const string ShareDescription = "Play {0} on CGS!";
        public const string ShareBranchMessage = "Get CGS for {0}: {1}";
        public const string ShareUrlMessage = "The CGS AutoUpdate Url for {0} has been copied to the clipboard: {1}";
        public const int CardsLoadingMessageThreshold = 60;
        public const int PixelsPerInch = 100;

        public static CardGameManager Instance
        {
            get
            {
                if (IsQuitting)
                    return null;
                if (_instance == null)
                    _instance = GameObject.FindGameObjectWithTag(Tags.CardGameManager).GetComponent<CardGameManager>();
                return _instance;
            }
        }
        private static CardGameManager _instance;

        public static CardGame Current { get; private set; } = CardGame.Invalid;
        public static bool IsQuitting { get; private set; } = false;

        // TODO: Network Discovery
        public bool IsSearching { get; set; }

        public SortedDictionary<string, CardGame> AllCardGames { get; } = new SortedDictionary<string, CardGame>();
        public SortedList<string, string> GamesListing => new SortedList<string, string>(AllCardGames.ToDictionary(game => game.Key, game => game.Value.Name));
        public CardGame Previous
        {
            get
            {
                CardGame previous = AllCardGames.Values.LastOrDefault() ?? CardGame.Invalid;
                SortedDictionary<string, CardGame>.Enumerator allCardGamesEnum = AllCardGames.GetEnumerator();
                bool found = false;
                while (!found && allCardGamesEnum.MoveNext())
                {
                    if (allCardGamesEnum.Current.Value != Current)
                        previous = allCardGamesEnum.Current.Value;
                    else
                        found = true;
                }
                return previous;
            }
        }
        public CardGame Next
        {
            get
            {
                CardGame next = AllCardGames.Values.FirstOrDefault() ?? CardGame.Invalid;
                SortedDictionary<string, CardGame>.Enumerator allCardGamesEnum = AllCardGames.GetEnumerator();
                bool found = false;
                while (!found && allCardGamesEnum.MoveNext())
                    if (allCardGamesEnum.Current.Value == Current)
                        found = true;
                if (allCardGamesEnum.MoveNext())
                    next = allCardGamesEnum.Current.Value;
                return next;
            }
        }

        public HashSet<UnityAction> OnSceneActions { get; } = new HashSet<UnityAction>();

        public HashSet<Canvas> CardCanvases { get; } = new HashSet<Canvas>();
        public Canvas CardCanvas
        {
            get
            {
                Canvas topCanvas = null;
                CardCanvases.RemoveWhere((canvas) => canvas == null);
                foreach (Canvas canvas in CardCanvases)
                    if (canvas.gameObject.activeSelf && (topCanvas == null || canvas.sortingOrder > topCanvas.sortingOrder))
                        topCanvas = canvas;
                return topCanvas;
            }
        }

        public HashSet<Canvas> ModalCanvases { get; } = new HashSet<Canvas>();
        public Canvas ModalCanvas
        {
            get
            {
                Canvas topCanvas = null;
                ModalCanvases.RemoveWhere((canvas) => canvas == null);
                foreach (Canvas canvas in ModalCanvases)
                    if (canvas.gameObject.activeSelf && (topCanvas == null || canvas.sortingOrder > topCanvas.sortingOrder))
                        topCanvas = canvas;
                return topCanvas;
            }
        }

        public Dialog Messenger
        {
            get
            {
                if (_messenger != null) return _messenger;
                _messenger = Instantiate(Resources.Load<GameObject>("Dialog")).GetOrAddComponent<Dialog>();
                _messenger.transform.SetParent(transform);
                return _messenger;
            }
        }
        private Dialog _messenger;

        public SpinningLoadingPanel Progress
        {
            get
            {
                if (_spinner != null) return _spinner;
                _spinner = Instantiate(Resources.Load<GameObject>("ProgressBar")).GetOrAddComponent<SpinningLoadingPanel>();
                _spinner.transform.SetParent(transform);
                return _spinner;
            }
        }
        private SpinningLoadingPanel _spinner;

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

            if (Debug.isDebugBuild)
                Application.logMessageReceived += ShowLogToUser;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            ResetCurrentToDefault();
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
                string gameDirectoryName = gameDirectory.Substring(CardGame.GamesDirectoryPath.Length + 1);
                (string name, string url) game = CardGame.Decode(gameDirectoryName);
                if (string.IsNullOrEmpty(name))
                    Debug.LogWarning(EmptyNameWarning);
                else if (name.Equals(CardGame.DefaultName))
                {
                    Debug.LogWarning(DefaultNameWarning);
                    try { Directory.Delete(gameDirectory, true); }
                    catch (Exception ex) { Debug.LogError(DeleteErrorMessage + ex.Message); }
                }
                else
                {
                    CardGame newCardGame = new CardGame(this, game.name, game.url);
                    newCardGame.ReadProperties();
                    if (!string.IsNullOrEmpty(newCardGame.Error))
                        Debug.LogError(LoadErrorMessage + newCardGame.Error);
                    else
                        AllCardGames[newCardGame.Id] = newCardGame;
                }
            }
        }

        void ShowLogToUser(string logString, string stackTrace, LogType type)
        {
            if (IsMessengerDebugLogVerbose || !LogType.Log.Equals(type))
                Messenger.Show(logString);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetGameScene();
        }

        void OnSceneUnloaded(Scene scene)
        {
            OnSceneActions.Clear();
        }

        public void BranchCallbackWithParams(Dictionary<string, object> parameters, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(BranchCallbackErrorMessage + error);
                Messenger.Show(BranchCallbackErrorMessage + error);
                return;
            }

            if (parameters.TryGetValue(GameId, out object gameId))
            {
                if (gameId is string)
                    Select((string)gameId);
                else
                    Debug.LogWarning(BranchCallbackWarning);
            }
        }

        // Note: Does NOT Reset Game Scene
        public void ResetCurrentToDefault()
        {
            string preferredGameId = PlayerPrefs.GetString(PlayerPrefDefaultGame);
            Current = AllCardGames.TryGetValue(preferredGameId, out CardGame currentGame) && string.IsNullOrEmpty(currentGame.Error)
                ? currentGame : (AllCardGames.FirstOrDefault().Value ?? CardGame.Invalid);
        }

        public IEnumerator DownloadCardGame(string gameUrl)
        {
            CardGame newGame = new CardGame(this, CardGame.DefaultName, gameUrl);

            Progress.Show(newGame);
            yield return newGame.Download();
            Progress.Hide();

            newGame.Load(UpdateCardGame, LoadCards);

            if (!string.IsNullOrEmpty(newGame.Error))
            {
                Debug.LogError(DownloadErrorMessage + Current.Error);
                Messenger.Show(DownloadErrorMessage + Current.Error);
                if (Directory.Exists(newGame.GameDirectoryPath))
                {
                    try { Directory.Delete(newGame.GameDirectoryPath, true); }
                    catch (Exception ex) { Debug.LogError(DeleteErrorMessage + ex.Message); }
                }
            }
            else
            {
                AllCardGames[newGame.Id] = newGame;
                Select(newGame.Id);
            }
        }

        public IEnumerator UpdateCardGame(CardGame cardGame)
        {
            if (cardGame == null)
                cardGame = Current;

            Progress.Show(cardGame);
            yield return cardGame.Download();
            Progress.Hide();

            // Notify about the failed update, but otherwise ignore errors
            if (!string.IsNullOrEmpty(cardGame.Error))
            {
                Debug.LogError(DownloadErrorMessage + cardGame.Error);
                Messenger.Show(DownloadErrorMessage + cardGame.Error);
                cardGame.ClearError();
            }

            cardGame.Load(UpdateCardGame, LoadCards);
            if (cardGame == Current)
                ResetGameScene();
        }

        public IEnumerator LoadCards(CardGame cardGame)
        {
            if (cardGame == null)
                cardGame = Current;

            for (int page = cardGame.AllCardsUrlPageCountStartIndex; page < cardGame.AllCardsUrlPageCountStartIndex + cardGame.AllCardsUrlPageCount; page++)
            {
                cardGame.LoadCards(page);
                if (page == cardGame.AllCardsUrlPageCountStartIndex && cardGame.AllCardsUrlPageCount > CardsLoadingMessageThreshold)
                    Messenger.Show(string.Format(CardsLoadingMessage, cardGame.Name));
                yield return null;
            }

            if (!string.IsNullOrEmpty(cardGame.Error))
                Debug.LogError(LoadErrorMessage + cardGame.Error);
            else if (cardGame.AllCardsUrlPageCount > CardsLoadingMessageThreshold)
                Messenger.Show(string.Format(CardsLoadedMessage, cardGame.Name));
        }

        public void Select(string gameId)
        {
            if (string.IsNullOrEmpty(gameId) || !AllCardGames.ContainsKey(gameId))
            {
                (_, string gameUrl) = CardGame.Decode(gameId);
                if (!Uri.IsWellFormedUriString(gameUrl, UriKind.Absolute))
                {
                    Debug.LogError(SelectionErrorMessage);
                    Messenger.Show(SelectionErrorMessage);
                }
                else
                    StartCoroutine(DownloadCardGame(gameUrl));
                return;
            }

            Current = AllCardGames[gameId];
            ResetGameScene();
        }

        public void ResetGameScene()
        {
            if (!Current.HasLoaded)
            {
                Current.Load(UpdateCardGame, LoadCards);
                if (Current.IsDownloading)
                    return;
            }

            if (!string.IsNullOrEmpty(Current.Error))
            {
                Debug.LogError(LoadErrorMessage + Current.Error);
                Messenger.Ask(LoadErrorPrompt, IgnoreCurrentErroredGame, Delete);
                return;
            }

            // Now is the safest time to set this game as the preferred default game for the player
            PlayerPrefs.SetString(PlayerPrefDefaultGame, Current.Id);

            // Each scene is responsible for adding to OnSceneActions, but they may not remove
            OnSceneActions.RemoveWhere((action) => action == null);
            foreach (UnityAction action in OnSceneActions)
                action();
        }

        public void IgnoreCurrentErroredGame()
        {
            Current.ClearError();
            ResetCurrentToDefault();
            ResetGameScene();
        }

        public void PromptDelete()
        {
            if (AllCardGames.Count > 1)
                Messenger.Prompt(DeletePrompt, Delete);
            else
                Messenger.Show(DeleteWarningMessage);
        }

        public void Delete()
        {
            if (AllCardGames.Count < 1)
            {
                Debug.LogError(DeleteErrorMessage + DeleteWarningMessage);
                return;
            }

            try
            {
                Directory.Delete(Current.GameDirectoryPath, true);
                AllCardGames.Remove(Current.Id);
                ResetCurrentToDefault();
                ResetGameScene();
            }
            catch (Exception ex)
            {
                Debug.LogError(DeleteErrorMessage + ex.Message);
            }
        }

        public void Share()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            ShareBranch();
#else
            ShareUrl();
#endif
        }

        public void ShareBranch()
        {
            BranchUniversalObject universalObject = new BranchUniversalObject();
            universalObject.contentIndexMode = 1;
            universalObject.canonicalIdentifier = Current.Id;
            universalObject.title = string.Format(ShareTitle, Current.Name);
            universalObject.contentDescription = string.Format(ShareDescription, Current.Name);
            universalObject.imageUrl = Current.BannerImageUrl;
            universalObject.metadata.AddCustomMetadata(GameId, Current.Id);
            BranchLinkProperties linkProperties = new BranchLinkProperties();
            linkProperties.controlParams.Add(GameId, Current.Id);
            Branch.getShortURL(universalObject, linkProperties, BranchCallbackWithUrl);
        }

        public void BranchCallbackWithUrl(string url, string error)
        {
            if (error != null)
            {
                Debug.LogError(error);
                return;
            }

            NativeShare nativeShare = new NativeShare();
            nativeShare.SetText(string.Format(ShareBranchMessage, Current.Name, url)).Share();
        }

        public void ShareUrl()
        {
            UniClipboard.SetText(Current.AutoUpdateUrl);
            Messenger.Show(string.Format(ShareUrlMessage, Current.Name, Current.AutoUpdateUrl));
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
