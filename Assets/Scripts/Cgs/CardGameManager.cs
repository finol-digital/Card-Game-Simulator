/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using Cgs.Menu;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityExtensionMethods;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

[assembly: InternalsVisibleTo("PlayMode")]

namespace Cgs
{
    public class CardGameManager : MonoBehaviour
    {
        // Show all Debug.Log() to help with debugging?
        private const bool IsMessengerDebugLogVerbose = false;
        private const string CgsZipExtension = ".cgs.zip";
        private const string AddressableAssetsFolderName = "aa";
        public const string PlayerPrefsDefaultGame = "DefaultGame";
        public const string SelectionErrorMessage = "Could not select the card game because it is not recognized!: ";
        public const string DownloadErrorMessage = "Error downloading game!: ";
        public const string LoadErrorMessage = "Error loading game!: ";

        public const string FileNotFoundErrorMessage = "ERROR: File Not Found at {0}";
        public const string InvalidCgsZipFileErrorMessage = "Not a valid .cgs.zip file at {0}";
        public const string OverwriteGamePrompt = "Game already exists. Overwrite?";
        public const string ImportFailureErrorMessage = "ERROR: Failed to Import! ";

        public const string LoadErrorPrompt =
            "Error loading game! The game may be corrupted. Delete (note that any decks would also be deleted)?";

        public const string CardsLoadedMessage = "{0} cards loaded!";
        public const string CardsLoadingMessage = "{0} cards loading...";
        public const string DeleteErrorMessage = "Error deleting game!: ";
        public const string DeleteWarningMessage = "Please download additional card games before deleting.";

        public const string DeletePrompt =
            "Deleting a card game also deletes all decks saved for that card game. Are you sure you would like to delete this card game?";

        public const string ShareDeepLinkMessage = "Get CGS for {0}: {1}";

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
            private set => _instance = value;
        }

        private static CardGameManager _instance;

        public static UnityCardGame Current { get; private set; } = UnityCardGame.UnityInvalid;
        public static bool IsCurrentReady { get; private set; }
        public static bool IsQuitting { get; private set; }

        public bool IsSearchingForServer { get; set; }

        public SortedDictionary<string, UnityCardGame> AllCardGames { get; } = new();

        public UnityCardGame Previous
        {
            get
            {
                var previousCardGame = AllCardGames.Values.LastOrDefault() ?? UnityCardGame.UnityInvalid;

                using var allCardGamesEnumerator = AllCardGames.GetEnumerator();
                var found = false;
                while (!found && allCardGamesEnumerator.MoveNext())
                {
                    if (allCardGamesEnumerator.Current.Value != Current)
                        previousCardGame = allCardGamesEnumerator.Current.Value;
                    else
                        found = true;
                }

                return previousCardGame;
            }
        }

        public UnityCardGame Previous2
        {
            get
            {
                var games = AllCardGames.Values.ToList();
                var count = games.Count;
                if (count == 0) return UnityCardGame.UnityInvalid;
                var currentIndex = games.IndexOf(Current);
                if (currentIndex == -1) return UnityCardGame.UnityInvalid;
                var prev2Index = (currentIndex - 2 + count) % count;
                return games[prev2Index];
            }
        }

        public UnityCardGame Next
        {
            get
            {
                var nextCardGame = AllCardGames.Values.FirstOrDefault() ?? UnityCardGame.UnityInvalid;

                using var allCardGamesEnumerator = AllCardGames.GetEnumerator();
                var found = false;
                while (!found && allCardGamesEnumerator.MoveNext())
                    if (allCardGamesEnumerator.Current.Value == Current)
                        found = true;
                if (allCardGamesEnumerator.MoveNext())
                    nextCardGame = allCardGamesEnumerator.Current.Value;

                return nextCardGame;
            }
        }

        public UnityCardGame Next2
        {
            get
            {
                var games = AllCardGames.Values.ToList();
                var count = games.Count;
                if (count == 0) return UnityCardGame.UnityInvalid;
                var currentIndex = games.IndexOf(Current);
                if (currentIndex == -1) return UnityCardGame.UnityInvalid;
                var next2Index = (currentIndex + 2) % count;
                return games[next2Index];
            }
        }

        public HashSet<UnityAction> OnSceneActions { get; } = new();

        public HashSet<Canvas> CardCanvases { get; } = new();

        public Canvas CardCanvas
        {
            get
            {
                Canvas topCanvas = null;
                CardCanvases.RemoveWhere((canvas) => canvas == null);
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var canvas in CardCanvases)
                    if (canvas.gameObject.activeSelf &&
                        (topCanvas == null || canvas.sortingOrder > topCanvas.sortingOrder))
                        topCanvas = canvas;
                return topCanvas;
            }
        }

        public HashSet<Canvas> ModalCanvases { get; } = new();

        public Canvas ModalCanvas
        {
            get
            {
                Canvas topCanvas = null;
                ModalCanvases.RemoveWhere((canvas) => canvas == null);
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var canvas in ModalCanvases)
                    if (canvas.gameObject.activeSelf &&
                        (topCanvas == null || canvas.sortingOrder > topCanvas.sortingOrder))
                        topCanvas = canvas;
                return topCanvas;
            }
        }

        public Dialog Messenger => messenger;

        public Dialog messenger;

        public ProgressBar Progress => progress;

        public ProgressBar progress;

        private InputAction _tooltipsAction;
        private InputAction _previewAction;
        private InputAction _reprintsAction;
        private InputAction _developerAction;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

            Debug.Log("CardGameManager::Awake:CheckDeepLinks");
            CheckDeepLinks();
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CreateDefaultCardGames()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            UnityFileMethods.ExtractAndroidStreamingAssets(UnityCardGame.GamesDirectoryPath);
#elif !UNITY_WEBGL
            UnityFileMethods.CopyDirectory(Application.streamingAssetsPath, UnityCardGame.GamesDirectoryPath);
            var aaDirectory = Path.Combine(UnityCardGame.GamesDirectoryPath, AddressableAssetsFolderName);
            if (Directory.Exists(aaDirectory))
                Directory.Delete(aaDirectory, true);
#endif
        }

        internal void LookupCardGames()
        {
            if (!Directory.Exists(UnityCardGame.GamesDirectoryPath) ||
                Directory.GetDirectories(UnityCardGame.GamesDirectoryPath).Length < 1)
#if UNITY_WEBGL
                return;
#else
                CreateDefaultCardGames();
#endif
            foreach (var gameDirectory in Directory.GetDirectories(UnityCardGame.GamesDirectoryPath))
            {
                var gameDirectoryName = gameDirectory[(UnityCardGame.GamesDirectoryPath.Length + 1)..];
                if (AddressableAssetsFolderName.Equals(gameDirectoryName))
                    continue;
                var newCardGame = new UnityCardGame(this, gameDirectoryName);
                newCardGame.ReadProperties();
                if (!string.IsNullOrEmpty(newCardGame.Error))
                    Debug.LogError(newCardGame.Error);
                else
                    AllCardGames[newCardGame.Id] = newCardGame;
            }
        }

        public void ImportCardGame(string zipFilePath)
        {
            if (!File.Exists(zipFilePath))
            {
                var errorMessage = string.Format(FileNotFoundErrorMessage, zipFilePath);
                Debug.LogError(errorMessage);
                Messenger.Show(errorMessage);
                return;
            }

            if (!zipFilePath.EndsWith(CgsZipExtension, StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage = string.Format(InvalidCgsZipFileErrorMessage, zipFilePath);
                Debug.LogError(errorMessage);
                Messenger.Show(errorMessage);
                return;
            }

            var gameId = Path.GetFileNameWithoutExtension(zipFilePath);
            var targetGameDirectory = Path.Combine(UnityCardGame.GamesDirectoryPath, gameId);
            if (File.Exists(targetGameDirectory))
            {
                Messenger.Ask(OverwriteGamePrompt, () => { },
                    () => ForceImportCardGame(zipFilePath));
                return;
            }

            ForceImportCardGame(zipFilePath);
        }

        private void ForceImportCardGame(string zipFilePath)
        {
            var gameId = Path.GetFileNameWithoutExtension(zipFilePath);
            if (string.IsNullOrEmpty(gameId))
            {
                var errorMessage = string.Format(FileNotFoundErrorMessage, zipFilePath);
                Debug.LogError(errorMessage);
                Messenger.Show(errorMessage);
                return;
            }

            try
            {
                UnityFileMethods.ExtractZip(zipFilePath, UnityCardGame.GamesImportPath);

                var importGameDirectory = Path.Combine(UnityCardGame.GamesImportPath, gameId);
                if (!Directory.Exists(importGameDirectory))
                {
                    gameId = Path.GetFileName(Directory.GetDirectories(UnityCardGame.GamesImportPath)[0]);
                    importGameDirectory = Path.Combine(UnityCardGame.GamesImportPath, gameId);
                    if (!Directory.Exists(importGameDirectory))
                    {
                        var errorMessage = string.Format(FileNotFoundErrorMessage, importGameDirectory);
                        Debug.LogError(errorMessage);
                        Messenger.Show(errorMessage);
                        return;
                    }
                }

                var targetGameDirectory = Path.Combine(UnityCardGame.GamesDirectoryPath, gameId);
                if (Directory.Exists(targetGameDirectory))
                    Directory.Delete(targetGameDirectory, true);

                UnityFileMethods.CopyDirectory(importGameDirectory, targetGameDirectory);

                var newCardGame = new UnityCardGame(this, gameId);
                newCardGame.ReadProperties();
                if (!string.IsNullOrEmpty(newCardGame.Error))
                {
                    Debug.LogError(LoadErrorMessage + newCardGame.Error);
                    Messenger.Show(LoadErrorMessage + newCardGame.Error);
                }
                else
                {
                    AllCardGames[newCardGame.Id] = newCardGame;
                    Select(newCardGame.Id);
                }

                if (Directory.Exists(importGameDirectory))
                    Directory.Delete(importGameDirectory, true);
            }
            catch (Exception e)
            {
                Debug.LogError(ImportFailureErrorMessage + e);
                Messenger.Show(ImportFailureErrorMessage + e);
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

        // Note: Does NOT Reset Game Scene
        internal void ResetCurrentToDefault()
        {
            var preferredGameId =
                PlayerPrefs.GetString(PlayerPrefsDefaultGame, Tags.StandardPlayingCardsDirectoryName);
            Current = AllCardGames.TryGetValue(preferredGameId, out var currentGame) &&
                      string.IsNullOrEmpty(currentGame.Error)
                ? currentGame
                : (AllCardGames.FirstOrDefault().Value ?? UnityCardGame.UnityInvalid);
        }

        private void CheckDeepLinks()
        {
            Application.deepLinkActivated += OnDeepLinkActivated;

            Debug.Log("Checking Deep Links...");
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

        private void OnDeepLinkActivated(string deepLink)
        {
            Debug.Log("OnDeepLinkActivated!");
            DeepLinkLoadScene(deepLink);
            DeepLinkGetAutoUpdateUrl(deepLink);
        }

        private static void DeepLinkLoadScene(string deepLink)
        {
            var scene = GetScene(deepLink);
            if ("main".Equals(scene, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("DeepLinkLoadScene::main");
                SceneManager.LoadScene(Tags.MainMenuSceneIndex);
            }
            else if ("play".Equals(scene, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("DeepLinkLoadScene::play");
                SceneManager.LoadScene(Tags.PlayModeSceneIndex);
            }
            else if ("decks".Equals(scene, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("DeepLinkLoadScene::decks");
                SceneManager.LoadScene(Tags.DeckEditorSceneIndex);
            }
            else if ("cards".Equals(scene, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("DeepLinkLoadScene::cards");
                SceneManager.LoadScene(Tags.CardsExplorerSceneIndex);
            }
            else if ("settings".Equals(scene, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("DeepLinkLoadScene::settings");
                SceneManager.LoadScene(Tags.SettingsSceneIndex);
            }
        }

        private static string GetScene(string deepLink)
        {
            Debug.Log("GetScene::deepLink: " + deepLink);
            if (string.IsNullOrEmpty(deepLink) || !Uri.IsWellFormedUriString(deepLink, UriKind.Absolute))
            {
                Debug.LogWarning("GetScene::deepLinkMalformed: " + deepLink);
                return null;
            }

            var deepLinkUriLast = new Uri(deepLink).Segments.LastOrDefault()?.TrimEnd('/');
            Debug.Log("GetScene::deepLinkUriLast: " + deepLinkUriLast);
            return deepLinkUriLast;
        }

        private void DeepLinkGetAutoUpdateUrl(string deepLink)
        {
            var autoUpdateUrl = GetAutoUpdateUrl(deepLink);
            if (string.IsNullOrEmpty(autoUpdateUrl) ||
                !Uri.IsWellFormedUriString(autoUpdateUrl, UriKind.RelativeOrAbsolute))
                Debug.LogWarning("DeepLinkGetAutoUpdateUrl::autoUpdateUrlMissingOrMalformed: " + deepLink);
            else
                StartGetCardGame(autoUpdateUrl);
        }

        private static string GetAutoUpdateUrl(string deepLink)
        {
            Debug.Log("GetAutoUpdateUrl::deepLink: " + deepLink);
            if (string.IsNullOrEmpty(deepLink) || !Uri.IsWellFormedUriString(deepLink, UriKind.RelativeOrAbsolute))
            {
                Debug.LogWarning("GetAutoUpdateUrl::deepLinkMalformed: " + deepLink);
                return null;
            }

            var deepLinkUriQuery = new Uri(deepLink).Query;
            Debug.Log("GetAutoUpdateUrl::deepLinkUriQuery: " + deepLinkUriQuery);
            var autoUpdateUrl = HttpUtility.ParseQueryString(deepLinkUriQuery).Get("url");
            Debug.Log("GetAutoUpdateUrl::autoUpdateUrl: " + autoUpdateUrl);

            return autoUpdateUrl;
        }

        private IEnumerator Start()
        {
            _tooltipsAction = InputSystem.actions.FindAction(Tags.SettingsToolTips);
            _previewAction = InputSystem.actions.FindAction(Tags.SettingsPreviewMouseOver);
            _reprintsAction = InputSystem.actions.FindAction(Tags.SettingsHideReprints);
            _developerAction = InputSystem.actions.FindAction(Tags.SettingsDeveloperMode);

            yield return null;
            while (Current is { IsDownloading: true })
                yield return null;

            Debug.Log("CardGameManager::Start");

#if UNITY_WEBGL && CGS_SINGLEGAME
            var uri = new Uri(Application.absoluteURL);
            yield return GetCardGame("https://" + uri.Host + "/cgs.json");
#elif UNITY_WEBGL
            var isMissingGame =
 Current == null || Current == UnityCardGame.UnityInvalid || !string.IsNullOrEmpty(Current.Error) || !Current.HasLoaded;
            if (isMissingGame)
                yield return StartGetDefaultCardGames();
#endif

            IsCurrentReady = true;
        }

        // ReSharper disable once UnusedMember.Local
        private IEnumerator StartGetDefaultCardGames()
        {
            yield return GetCardGame(Tags.DominoesUrl);
            yield return GetCardGame(Tags.MahjongUrl);
            yield return GetCardGame(Tags.StandardPlayingCardsUrl);
        }

        [PublicAPI]
        public void StartGetCardGame(string autoUpdateUrl)
        {
            if (Current is { IsDownloading: true })
            {
                Debug.LogError("ERROR: StartGetCardGame while already downloading");
                return;
            }

            StartCoroutine(GetCardGame(autoUpdateUrl));
        }

        public IEnumerator GetCardGame(string gameUrl)
        {
            if (string.IsNullOrEmpty(gameUrl))
            {
                Debug.LogError("ERROR: GetCardGame has gameUrl missing!");
                Messenger.Show("ERROR: GetCardGame has gameUrl missing!");
                yield break;
            }

            Debug.Log("GetCardGame: Starting...");
            // If user attempts to download a game they already have, we should just update that game
            UnityCardGame existingGame = null;
            foreach (var cardGame in AllCardGames.Values.Where(cardGame => cardGame != null &&
                                                                           cardGame.AutoUpdateUrl != null &&
                                                                           cardGame.AutoUpdateUrl.Equals(
                                                                               new Uri(gameUrl))))
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
                    Debug.LogError("GetCardGame: Not selecting card game because of an error after update!");
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
            yield return cardGame.Download(true);
            Progress.Hide();

            // Notify about the failed update, but otherwise ignore errors
            if (!string.IsNullOrEmpty(cardGame.Error))
            {
                Debug.LogError(DownloadErrorMessage + cardGame.Error);
                Messenger.Show(DownloadErrorMessage + cardGame.Error);
                cardGame.ClearError();
            }

            cardGame.Load(null, LoadCards, LoadSetCards);
            if (cardGame == Current)
                ResetGameScene();
        }

        private IEnumerator LoadCards(UnityCardGame cardGame)
        {
            cardGame ??= Current;

            for (var page = cardGame.AllCardsUrlPageCountStartIndex;
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

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private IEnumerator LoadSetCards(UnityCardGame cardGame)
        {
            cardGame ??= Current;

            foreach (var set in cardGame.Sets.Values.ToList())
            {
                if (string.IsNullOrEmpty(set.CardsUrl))
                    continue;
                var setCardsFilePath = Path.Combine(cardGame.SetsDirectoryPath,
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
        }

        public void Select(string gameId)
        {
            if (string.IsNullOrEmpty(gameId) || !AllCardGames.TryGetValue(gameId, out var game))
            {
                Debug.LogError(SelectionErrorMessage + gameId);
                Messenger.Show(SelectionErrorMessage + gameId);
                return;
            }

            Current = game;
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
                if (UnityCardGame.UnityInvalid == Current || Current == CardGame.Invalid)
                    return;
                Debug.LogError(LoadErrorMessage + Current.Error);
                Messenger.Ask(LoadErrorPrompt, IgnoreCurrentErroredGame, Delete);
                return;
            }

            // Now is the safest time to set this game as the preferred default game for the player
            PlayerPrefs.SetString(PlayerPrefsDefaultGame, Current.Id);

            // Each scene is responsible for adding to OnSceneActions, but they may not remove
            OnSceneActions.RemoveWhere((action) => action == null);
            foreach (var action in OnSceneActions)
                action();
        }

        private void IgnoreCurrentErroredGame()
        {
            Current.ClearError();
            ResetCurrentToDefault();
            ResetGameScene();
        }

        public void Share()
        {
            Debug.Log("CGS Share::CgsGamesLink: " + Current.CgsGamesLink);
            if (Current.CgsGamesLink != null && Current.CgsGamesLink.IsWellFormedOriginalString())
            {
                var shareMessage = string.Format(ShareDeepLinkMessage, Current.Name, Current.CgsGamesLink);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                var nativeShare = new NativeShare();
                nativeShare.SetText(shareMessage).Share();
#else
                UniClipboard.SetText(shareMessage);
                Messenger.Show(shareMessage);
#endif
            }
            else
                ExportGame();
        }

        private static void ExportGame()
        {
            var container = Path.Combine(UnityCardGame.GamesExportPath, UnityFileMethods.GetSafeFileName(Current.Id));
            Debug.Log("CGS Share::container: " + container);
            if (Directory.Exists(container))
                Directory.Delete(container, true);

            var subContainer = Path.Combine(container, UnityFileMethods.GetSafeFileName(Current.Id));
            Debug.Log("CGS Share::subContainer: " + subContainer);
            UnityFileMethods.CopyDirectory(Current.GameDirectoryPath, subContainer);

            var zipFileName = UnityFileMethods.GetSafeFileName(Current.Id + CgsZipExtension);
            Debug.Log("CGS Share::zipFileName: " + zipFileName);
            UnityFileMethods.CreateZip(container, UnityCardGame.GamesExportPath, zipFileName);
            Directory.Delete(container, true);

            var targetZipFilePath = Path.Combine(UnityCardGame.GamesExportPath, zipFileName);
            Debug.Log("CGS Share::targetZipFilePath: " + targetZipFilePath);
            var exportGameZipUri = new Uri(targetZipFilePath);
            Debug.Log("CGS Share::size: " + new FileInfo(targetZipFilePath).Length);

#if ENABLE_WINMD_SUPPORT
            var ExportGameErrorMessage = "ERROR: Failed to Export! ";
            UnityEngine.WSA.Application.InvokeOnUIThread(async () => {
                try
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(exportGameZipUri.LocalPath);
                    if (file != null)
                    {
                        // Launch the retrieved file
                        var success = await Windows.System.Launcher.LaunchFileAsync(file);
                        if (!success)
                        {
                            Debug.LogError(ExportGameErrorMessage + exportGameZipUri.LocalPath);
                            CardGameManager.Instance.Messenger.Show(ExportGameErrorMessage + exportGameZipUri.LocalPath);
                        }
                    }
                    else
                    {
                        Debug.LogError(ExportGameErrorMessage + exportGameZipUri.LocalPath);
                        CardGameManager.Instance.Messenger.Show(ExportGameErrorMessage + exportGameZipUri.LocalPath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + e.StackTrace);
                    CardGameManager.Instance.Messenger.Show(ExportGameErrorMessage + exportGameZipUri.LocalPath);
                }
            }, false);
#elif UNITY_ANDROID && !UNITY_EDITOR
            var tempCgsZipFilePath = Path.Combine( Application.temporaryCachePath, Current.Id + CgsZipExtension );
            Instance.StartCoroutine(Instance.OpenZip(exportGameZipUri, tempCgsZipFilePath));
#elif UNITY_IOS && !UNITY_EDITOR
            UnityNative.Sharing.UnityNativeSharing.Create().ShareScreenshotAndText("", targetZipFilePath, false, "", "");
#else
            Application.OpenURL(exportGameZipUri.AbsoluteUri);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        public IEnumerator OpenZip(Uri uri, string tempCgsZipFilePath)
        {
            var uwr = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET);
            uwr.downloadHandler = new DownloadHandlerFile(tempCgsZipFilePath);
            yield return uwr.SendWebRequest();
            new NativeShare().AddFile(tempCgsZipFilePath, "application/zip").Share();
        }
#endif

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

        private void Update()
        {
            if (_tooltipsAction?.WasPressedThisFrame() ?? false)
                Settings.ButtonTooltipsEnabled = !Settings.ButtonTooltipsEnabled;
            if (_previewAction?.WasPressedThisFrame() ?? false)
                Settings.PreviewOnMouseOver = !Settings.PreviewOnMouseOver;
            if (_reprintsAction?.WasPressedThisFrame() ?? false)
                Settings.HideReprints = !Settings.HideReprints;
            if (_developerAction?.WasPressedThisFrame() ?? false)
                Settings.DeveloperMode = !Settings.DeveloperMode;

            ImageQueueService.Instance.ProcessQueue(this);
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
