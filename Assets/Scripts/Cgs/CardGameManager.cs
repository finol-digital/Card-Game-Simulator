/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_IOS
using Firebase;
using Firebase.Extensions;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.Menu;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityExtensionMethods;
#if UNITY_ANDROID || UNITY_IOS
using Firebase.DynamicLinks;
#endif

[assembly: InternalsVisibleTo("PlayMode")]

namespace Cgs
{
    public class CardGameManager : MonoBehaviour
    {
        // Show all Debug.Log() to help with debugging?
        private const bool IsMessengerDebugLogVerbose = false;
        public const string PlayerPrefsDefaultGame = "DefaultGame";
        public const string DefaultNameWarning = "Found game with default name. Deleting it.";
        public const string SelectionErrorMessage = "Could not select the card game because it is not recognized!: ";
        public const string DownloadErrorMessage = "Error downloading game!: ";
        public const string LoadErrorMessage = "Error loading game!: ";

        public const string LoadErrorPrompt =
            "Error loading game! The game may be corrupted. Delete (note that any decks would also be deleted)?";

        public const string CardsLoadedMessage = "{0} cards loaded!";
        public const string CardsLoadingMessage = "{0} cards loading...";
        public const string SetCardsLoadedMessage = "{0} set cards loaded!";
        public const string SetCardsLoadingMessage = "{0} set cards loading...";
        public const string DeleteErrorMessage = "Error deleting game!: ";
        public const string DeleteWarningMessage = "Please download additional card games before deleting.";

        public const string DeletePrompt =
            "Deleting a card game also deletes all decks saved for that card game. Are you sure you would like to delete this card game?";

        public const string ShareDeepLinkMessage = "Get CGS for {0}: {1}";

        public const string ShareWarningMessage =
            "Sharing {0} on CGS requires that it be uploaded to the web.\nIf you would like help with this upload, please contact david@finoldigital.com";

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

        public static UnityCardGame Current { get; private set; } = UnityCardGame.UnityInvalid;
        public static bool IsQuitting { get; private set; }

        public bool IsSearchingForServer { get; set; }

        public SortedDictionary<string, UnityCardGame> AllCardGames { get; } =
            new SortedDictionary<string, UnityCardGame>();

        public UnityCardGame Previous
        {
            get
            {
                UnityCardGame previous = AllCardGames.Values.LastOrDefault() ?? UnityCardGame.UnityInvalid;

                using SortedDictionary<string, UnityCardGame>.Enumerator allCardGamesEnum =
                    AllCardGames.GetEnumerator();
                var found = false;
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

        public UnityCardGame Next
        {
            get
            {
                UnityCardGame next = AllCardGames.Values.FirstOrDefault() ?? UnityCardGame.UnityInvalid;

                using SortedDictionary<string, UnityCardGame>.Enumerator allCardGamesEnum =
                    AllCardGames.GetEnumerator();
                var found = false;
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
                    if (canvas.gameObject.activeSelf &&
                        (topCanvas == null || canvas.sortingOrder > topCanvas.sortingOrder))
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
                    if (canvas.gameObject.activeSelf &&
                        (topCanvas == null || canvas.sortingOrder > topCanvas.sortingOrder))
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

        public ProgressBar Progress
        {
            get
            {
                if (_progress != null) return _progress;
                _progress = Instantiate(Resources.Load<GameObject>("ProgressBar")).GetOrAddComponent<ProgressBar>();
                _progress.transform.SetParent(transform);
                return _progress;
            }
        }

        private ProgressBar _progress;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            UnityCardGame.UnityInvalid.CoroutineRunner = this;
            DontDestroyOnLoad(gameObject);

            if (!Directory.Exists(UnityCardGame.GamesDirectoryPath))
                CreateDefaultCardGames();
            LookupCardGames();

            if (Debug.isDebugBuild)
                Application.logMessageReceived += ShowLogToUser;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            ResetCurrentToDefault();

            Debug.Log("CardGameManager is Awake!");
        }

        private void Start()
        {
#if !UNITY_WEBGL
            Debug.Log("CardGameManager::Start:CheckDeepLinks");
            CheckDeepLinks();
#endif
        }

        private void CreateDefaultCardGames()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            UnityFileMethods.ExtractAndroidStreamingAssets(UnityCardGame.GamesDirectoryPath);
#elif UNITY_WEBGL
            if (!Directory.Exists(UnityCardGame.GamesDirectoryPath))
                Directory.CreateDirectory(UnityCardGame.GamesDirectoryPath);
            string standardPlayingCardsDirectory =
                UnityCardGame.GamesDirectoryPath + "/" + Tags.StandardPlayingCardsDirectoryName;
            if (!Directory.Exists(standardPlayingCardsDirectory))
                Directory.CreateDirectory(standardPlayingCardsDirectory);
            File.WriteAllText(standardPlayingCardsDirectory + "/" + Tags.StandardPlayingCardsJsonFileName,
                Tags.StandPlayingCardsJsonFileContent);
            string dominoesDirectory = UnityCardGame.GamesDirectoryPath + "/" + Tags.DominoesDirectoryName;
            if (!Directory.Exists(dominoesDirectory))
                Directory.CreateDirectory(dominoesDirectory);
            File.WriteAllText(dominoesDirectory + "/" + Tags.DominoesJsonFileName, Tags.DominoesJsonFileContent);
            StartCoroutine(
                UnityFileMethods.SaveUrlToFile(Tags.DominoesCardBackUrl, dominoesDirectory + "/CardBack.png"));
            string mahjongDirectory = UnityCardGame.GamesDirectoryPath + "/" + Tags.MahjongDirectoryName;
            if (!Directory.Exists(mahjongDirectory))
                Directory.CreateDirectory(mahjongDirectory);
            File.WriteAllText(mahjongDirectory + "/" + Tags.MahjongJsonFileName, Tags.MahjongJsonFileContent);
            StartCoroutine(UnityFileMethods.SaveUrlToFile(Tags.MahjongCardBackUrl, mahjongDirectory + "/CardBack.png"));
#else
            UnityFileMethods.CopyDirectory(Application.streamingAssetsPath, UnityCardGame.GamesDirectoryPath);
#endif
        }

        internal void LookupCardGames()
        {
            if (!Directory.Exists(UnityCardGame.GamesDirectoryPath) ||
                Directory.GetDirectories(UnityCardGame.GamesDirectoryPath).Length < 1)
                CreateDefaultCardGames();

            foreach (string gameDirectory in Directory.GetDirectories(UnityCardGame.GamesDirectoryPath))
            {
                string gameDirectoryName = gameDirectory.Substring(UnityCardGame.GamesDirectoryPath.Length + 1);
                (string gameName, _) = CardGame.GetNameAndHost(gameDirectoryName);
                if (gameName.Equals(CardGame.DefaultName))
                {
                    Debug.LogWarning(DefaultNameWarning);
                    try
                    {
                        Directory.Delete(gameDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(DeleteErrorMessage + ex.Message);
                    }
                }
                else
                {
                    var newCardGame = new UnityCardGame(this, gameDirectoryName);
                    newCardGame.ReadProperties();
                    if (!string.IsNullOrEmpty(newCardGame.Error))
                        Debug.LogError(LoadErrorMessage + newCardGame.Error);
                    else
                        AllCardGames[newCardGame.Id] = newCardGame;
                }
            }
        }

        private void ShowLogToUser(string logString, string stackTrace, LogType type)
        {
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (this != null && Messenger != null && (IsMessengerDebugLogVerbose || !LogType.Log.Equals(type)))
                Messenger.Show(logString);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolutionManager.ScaleResolution();
            ResetGameScene();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            OnSceneActions.Clear();
        }

        private void CheckDeepLinks()
        {
            Debug.Log("Checking Deep Links...");
#if UNITY_IOS
            Debug.Log("Should use Firebase Dynamic Links for iOS...");
            Application.deepLinkActivated += OnDeepLinkActivated;/*
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus != DependencyStatus.Available)
                {
                    Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
                    Messenger.Show("Could not resolve all Firebase dependencies: " + dependencyStatus);
                    return;
                }

                DynamicLinks.DynamicLinkReceived += OnDynamicLinkReceived;
                Debug.Log("Using Firebase Dynamic Links for iOS!");
            });*/
#elif UNITY_ANDROID
            if (string.IsNullOrEmpty(Application.absoluteURL))
            {
                DynamicLinks.DynamicLinkReceived += OnDynamicLinkReceived;
                Debug.Log("Using Firebase Dynamic Links for Android!");
            }
#else
            Application.deepLinkActivated += OnDeepLinkActivated;
            Debug.Log("Using Native Deep Links!");
#endif

            if (string.IsNullOrEmpty(Application.absoluteURL))
            {
                Debug.Log("No Start Deep Link");
                return;
            }

            if (!Uri.IsWellFormedUriString(Application.absoluteURL, UriKind.RelativeOrAbsolute))
            {
                Debug.LogWarning("Start Deep Link malformed: " + Application.absoluteURL);
                return;
            }

            Debug.Log("Start Deep Link: " + Application.absoluteURL);
            OnDeepLinkActivated(Application.absoluteURL);
        }

#if UNITY_ANDROID || UNITY_IOS
        private void OnDynamicLinkReceived(object sender, EventArgs args)
        {
            var dynamicLinkEventArgs = args as ReceivedDynamicLinkEventArgs;
            var deepLink = dynamicLinkEventArgs?.ReceivedDynamicLink.Url.OriginalString;
            if (string.IsNullOrEmpty(deepLink))
            {
                Debug.LogError("OnDynamicLinkReceived::deepLinkEmpty");
                Messenger.Show("OnDynamicLinkReceived::deepLinkEmpty");
            }
            else
                OnDeepLinkActivated(deepLink);
        }
#endif

        private void OnDeepLinkActivated(string deepLink)
        {
            var autoUpdateUrl = GetAutoUpdateUrl(deepLink);
            if (string.IsNullOrEmpty(autoUpdateUrl) || !Uri.IsWellFormedUriString(autoUpdateUrl, UriKind.RelativeOrAbsolute))
            {
                Debug.LogError("OnDeepLinkActivated::autoUpdateUrlMalformed: " + deepLink);
                Messenger.Show("OnDeepLinkActivated::autoUpdateUrlMalformed: " + deepLink);
            }
            else
                StartCoroutine(GetCardGame(autoUpdateUrl));
        }

        private static string GetAutoUpdateUrl(string deepLink)
        {
            Debug.Log("GetAutoUpdateUrl::deepLink: " + deepLink);
            if (string.IsNullOrEmpty(deepLink) || !Uri.IsWellFormedUriString(deepLink, UriKind.RelativeOrAbsolute))
            {
                Debug.LogWarning("GetAutoUpdateUrl::deepLinkMalformed: " + deepLink);
                return null;
            }

            if (deepLink.StartsWith(Tags.DynamicLinkUriDomain))
            {
                var dynamicLinkUri = new Uri(deepLink);
                deepLink = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(dynamicLinkUri.Query).Get("link"));
                Debug.Log("GetAutoUpdateUrl::dynamicLink: " + deepLink);
                if (string.IsNullOrEmpty(deepLink) || !Uri.IsWellFormedUriString(deepLink, UriKind.RelativeOrAbsolute))
                {
                    Debug.LogWarning("GetAutoUpdateUrl::dynamicLinkMalformed: " + deepLink);
                    return null;
                }
            }

            var deepLinkUri = new Uri(deepLink);
            var autoUpdateUrl = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(deepLinkUri.Query).Get("url"));
            Debug.Log("GetAutoUpdateUrl::autoUpdateUrl: " + autoUpdateUrl);

            return autoUpdateUrl;
        }

        // Note: Does NOT Reset Game Scene
        internal void ResetCurrentToDefault()
        {
            string preferredGameId =
                PlayerPrefs.GetString(PlayerPrefsDefaultGame, Tags.StandardPlayingCardsDirectoryName);
            Current = AllCardGames.TryGetValue(preferredGameId, out UnityCardGame currentGame) &&
                      string.IsNullOrEmpty(currentGame.Error)
                ? currentGame
                : (AllCardGames.FirstOrDefault().Value ?? UnityCardGame.UnityInvalid);
        }

        public IEnumerator GetCardGame(string gameUrl)
        {
            Debug.Log("GetCardGame: Starting...");
            // If user attempts to download a game they already have, we should just update that game
            UnityCardGame existingGame = null;
            foreach (UnityCardGame cardGame in AllCardGames.Values)
                if (cardGame.AutoUpdateUrl.Equals(new Uri(gameUrl)))
                    existingGame = cardGame;
            Debug.Log("GetCardGame: Existing game search complete...");
            if (existingGame != null)
            {
                Debug.Log("GetCardGame: Existing game found; updating...");
                yield return UpdateCardGame(existingGame);
                Debug.Log("GetCardGame: Existing game found; updated!");
                if (string.IsNullOrEmpty(existingGame.Error))
                    Select(existingGame.Id);
                else
                    Debug.LogError("GetCardGame: Not selecting card game because of error after update");
            }
            else
                yield return DownloadCardGame(gameUrl);

            Debug.Log("GetCardGame: Done!");
        }

        private IEnumerator DownloadCardGame(string gameUrl)
        {
            Debug.Log("DownloadCardGame: start");
            var cardGame = new UnityCardGame(this, CardGame.DefaultName, gameUrl);

            Progress.Show(cardGame);
            yield return cardGame.Download();
            Progress.Hide();

            cardGame.Load(UpdateCardGame, LoadCards, LoadSetCards);

            if (!string.IsNullOrEmpty(cardGame.Error))
            {
                Debug.LogError(DownloadErrorMessage + cardGame.Error);
                Messenger.Show(DownloadErrorMessage + cardGame.Error);

                if (!Directory.Exists(cardGame.GameDirectoryPath))
                    yield break;

                try
                {
                    Directory.Delete(cardGame.GameDirectoryPath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogError(DeleteErrorMessage + ex.Message);
                }
            }
            else
            {
                AllCardGames[cardGame.Id] = cardGame;
                Select(cardGame.Id);
            }

            Debug.Log("DownloadCardGame: end");
        }

        public IEnumerator UpdateCardGame(UnityCardGame cardGame)
        {
            cardGame ??= Current;

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

            cardGame.Load(UpdateCardGame, LoadCards, LoadSetCards);
            if (cardGame == Current)
                ResetGameScene();
        }

        private IEnumerator LoadCards(UnityCardGame cardGame)
        {
            cardGame ??= Current;

            for (int page = cardGame.AllCardsUrlPageCountStartIndex;
                 page < cardGame.AllCardsUrlPageCountStartIndex + cardGame.AllCardsUrlPageCount;
                 page++)
            {
                cardGame.LoadCards(page);
                if (page == cardGame.AllCardsUrlPageCountStartIndex &&
                    cardGame.AllCardsUrlPageCount > CardsLoadingMessageThreshold)
                    Messenger.Show(string.Format(CardsLoadingMessage, cardGame.Name));
                yield return null;
            }

            if (!string.IsNullOrEmpty(cardGame.Error))
                Debug.LogError(LoadErrorMessage + cardGame.Error);
            else if (cardGame.AllCardsUrlPageCount > CardsLoadingMessageThreshold)
                Messenger.Show(string.Format(CardsLoadedMessage, cardGame.Name));
        }

        private IEnumerator LoadSetCards(UnityCardGame cardGame)
        {
            cardGame ??= Current;

            var setCardsLoaded = false;
            foreach (Set set in cardGame.Sets.Values)
            {
                if (string.IsNullOrEmpty(set.CardsUrl))
                    continue;
                if (!setCardsLoaded)
                    Messenger.Show(string.Format(SetCardsLoadingMessage, cardGame.Name));
                setCardsLoaded = true;
                string setCardsFilePath = Path.Combine(cardGame.SetsDirectoryPath,
                    UnityFileMethods.GetSafeFileName(set.Code + UnityFileMethods.JsonExtension));
                if (!File.Exists(setCardsFilePath))
                    yield return UnityFileMethods.SaveUrlToFile(set.CardsUrl, setCardsFilePath);
                if (File.Exists(setCardsFilePath))
                    cardGame.LoadCards(setCardsFilePath, set.Code);
                else
                {
                    Debug.LogError(LoadErrorMessage + set.CardsUrl);
                    yield break;
                }
            }

            if (!string.IsNullOrEmpty(cardGame.Error))
                Debug.LogError(LoadErrorMessage + cardGame.Error);
            else if (setCardsLoaded)
                Messenger.Show(string.Format(SetCardsLoadedMessage, cardGame.Name));
        }

        public void Select(string gameId)
        {
            if (string.IsNullOrEmpty(gameId) || !AllCardGames.ContainsKey(gameId))
            {
                Debug.LogError(SelectionErrorMessage + gameId);
                Messenger.Show(SelectionErrorMessage + gameId);
                return;
            }

            Current = AllCardGames[gameId];
            ResetGameScene();
        }

        internal void ResetGameScene()
        {
            if (!Current.HasLoaded)
            {
                Current.Load(UpdateCardGame, LoadCards, LoadSetCards);
                if (Current.IsDownloading)
                    return;
            }

            if (!string.IsNullOrEmpty(Current.Error))
            {
                Debug.LogError(LoadErrorMessage + Current.Error);
                Messenger.Ask(LoadErrorPrompt, IgnoreCurrentErroredGame, Delete);
                return;
            }

#if UNITY_WEBGL
            foreach (UnityCardGame game in AllCardGames.Values)
                game.ReadProperties();
#endif

            // Now is the safest time to set this game as the preferred default game for the player
            PlayerPrefs.SetString(PlayerPrefsDefaultGame, Current.Id);

            // Each scene is responsible for adding to OnSceneActions, but they may not remove
            OnSceneActions.RemoveWhere((action) => action == null);
            foreach (UnityAction action in OnSceneActions)
                action();
        }

        private void IgnoreCurrentErroredGame()
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

        private void Delete()
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
            Debug.Log("CGS Share:: Deep:" + Current.CgsDeepLink + " Auto:" + Current.AutoUpdateUrl);
            if (Current.AutoUpdateUrl != null && Current.AutoUpdateUrl.IsWellFormedOriginalString())
            {
                var deepLink = Current.CgsDeepLink?.OriginalString ?? BuildDeepLink();
                var shareMessage = string.Format(ShareDeepLinkMessage, Current.Name, deepLink);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                var nativeShare = new NativeShare();
                nativeShare.SetText(shareMessage).Share();
#else
                UniClipboard.SetText(shareMessage);
                Messenger.Show(shareMessage);
#endif
            }
            else
            {
                Debug.LogWarningFormat(ShareWarningMessage, Current.Name);
                Messenger.Show(string.Format(ShareWarningMessage, Current.Name));
            }
        }

        private string BuildDeepLink()
        {
            if (Current.AutoUpdateUrl == null || !Current.AutoUpdateUrl.IsWellFormedOriginalString())
            {
                Debug.LogErrorFormat(ShareWarningMessage, Current.Name);
                Messenger.Show(string.Format(ShareWarningMessage, Current.Name));
                return null;
            }

            var deepLink = "https://cgs.link/?link=";
            deepLink += "https://www.cardgamesimulator.com/link?url%3D" + Current.AutoUpdateUrl.OriginalString;
            deepLink += "&apn=com.finoldigital.cardgamesim&isi=1392877362&ibi=com.finoldigital.CardGameSim";
            var regex = new Regex("[^a-zA-Z0-9 -]");
            var encodedName = regex.Replace(Current.Name, "+");
            deepLink += "&st=Card+Game+Simulator+-+" + encodedName + "&sd=Play+" + encodedName + "+on+CGS!";
            if (Current.BannerImageUrl != null && Current.BannerImageUrl.IsWellFormedOriginalString())
                deepLink += "&si=" + Current.BannerImageUrl.OriginalString;
            return deepLink;
        }

        private void LateUpdate()
        {
            Inputs.WasFocusBack = Inputs.IsFocusBack;
            Inputs.WasFocusNext = Inputs.IsFocusNext;
            Inputs.WasDown = Inputs.IsDown;
            Inputs.WasUp = Inputs.IsUp;
            Inputs.WasLeft = Inputs.IsLeft;
            Inputs.WasRight = Inputs.IsRight;
            Inputs.WasPageVertical = Inputs.IsPageVertical;
            Inputs.WasPageDown = Inputs.IsPageDown;
            Inputs.WasPageUp = Inputs.IsPageUp;
            Inputs.WasPageHorizontal = Inputs.IsPageHorizontal;
            Inputs.WasPageLeft = Inputs.IsPageLeft;
            Inputs.WasPageRight = Inputs.IsPageRight;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnApplicationQuit()
        {
            IsQuitting = true;
        }
    }
}
