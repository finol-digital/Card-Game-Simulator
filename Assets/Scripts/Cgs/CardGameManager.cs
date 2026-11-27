/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Performance;
using Cgs.Localization;
using Cgs.GameContent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
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
        public const string SelectionErrorMessage = "Could not select the card game because it is not recognized!: ";
        public const string DownloadErrorMessage = "Error downloading game!: ";
        public const string LoadErrorMessage = "Error loading game!: ";

        public const string FileNotFoundErrorMessage = "ERROR: File Not Found at {0}";
        public const string InvalidCgsZipFileErrorMessage = "Not a valid .cgs.zip file at {0}";
        public const string OverwriteGamePrompt = "Game already exists. Overwrite?";
        public const string ImportFailureErrorMessage = "ERROR: Failed to Import! ";
        private const string ExportFailureErrorMessage = "Failed to export game: ";

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

        public const string PlayerPrefsDefaultGame = "DefaultGame";
        private const string CgsZipExtension = ".cgs.zip";
        private const string AddressableAssetsFolderName = "aa";

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

        /// <summary>Startup's own work, such as the web build's default-game downloads, has finished.</summary>
        private static bool HasStartedUp { get; set; }

        /// <summary>
        /// The current game's properties (name, banner, and other cgs.json values) are available. This is all the Main
        /// Menu needs; card data loads after the menu is interactive.
        /// </summary>
        public static bool IsCurrentPropertiesReady =>
            HasStartedUp && Current.HasReadProperties && !Current.IsDownloading;

        /// <summary>The current game's cards, sets, and images have loaded, so a table can be played.</summary>
        public static bool IsCurrentContentReady =>
            IsCurrentPropertiesReady && Current.HasLoaded && !Current.IsLoading;

        /// <summary>
        /// Loading the current game's content has finished, successfully or not, so screens waiting for it can go on.
        /// </summary>
        public static bool IsCurrentContentSettled =>
            HasStartedUp && !Current.IsDownloading && !Current.IsLoading &&
            (Current.HasLoaded || !string.IsNullOrEmpty(Current.Error) || Current == UnityCardGame.UnityInvalid);

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

        [SerializeField] Dialog messenger;

        [SerializeField] ProgressBar progress;

        public Dialog Messenger => messenger;

        public ProgressBar Progress => progress;

        private InputAction _tooltipsAction;
        private InputAction _previewAction;
        private InputAction _reprintsAction;
        private InputAction _developerAction;

        private int _selectionVersion;

        // A scene has loaded and its content load will start after the scene's first frame
        internal bool IsContentLoadScheduled { get; private set; }

        /// <summary>
        /// The open screen shows the current game's cards, so it shows loading progress until they have loaded. Such a
        /// screen sets this when it starts, and it resets when the screen's scene unloads.
        /// </summary>
        internal bool SceneNeedsContent { get; private set; }

        private readonly ContentLoadProgress _contentLoadProgress = new();

        /// <summary>Reports the current game's content load to the progress bar.</summary>
        private sealed class ContentLoadProgress : IProgressible
        {
            private UnityCardGame _game;
            private UiMessage _message;

            public float ProgressPercentage => Current.ContentLoadProgress;
            public string ProgressStatus => string.Format(CardsLoadingMessage, Current.Name);

            public UiMessage LocalizedProgressStatus
            {
                get
                {
                    // The progress bar refreshes its text only when given a different message
                    if (_game == Current && _message != null)
                        return _message;
                    _game = Current;
                    _message = GameContentMessage.Create(Current, display => UiMessage.With("games.cards.loading",
                        string.Format(CardsLoadingMessage, display.GameName), ("gameName", display.GameName)));
                    return _message;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            Current = UnityCardGame.UnityInvalid;
            HasStartedUp = false;
            IsQuitting = false;
        }

        protected void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            UnityCardGame.UnityInvalid.CoroutineRunner = this;
            DontDestroyOnLoad(gameObject);

            LookupCardGames();

            RegisterStaticEventHandlers();

            ResetCurrentToDefault();

            Debug.Log("CardGameManager::Awake:CheckDeepLinks");
            CheckDeepLinks();
        }

        private void RegisterStaticEventHandlers()
        {
            if (Debug.isDebugBuild)
            {
                Application.logMessageReceived -= ShowLogToUser;
                Application.logMessageReceived += ShowLogToUser;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            Application.deepLinkActivated -= OnDeepLinkActivated;
            Application.deepLinkActivated += OnDeepLinkActivated;
        }

        protected void OnEnable()
        {
            if (_instance == this)
                RegisterStaticEventHandlers();
        }

        /// <summary>The bundled games being installed in the background on a first launch, if any.</summary>
        internal BundledGameInstaller BundledGames { get; private set; }

        /// <summary>Raised when games are added to <see cref="AllCardGames"/> without the player selecting one, as when
        /// a bundled game finishes installing.</summary>
        public event Action GamesAdded;

        /// <summary>Where the bundled games come from; tests substitute their own.</summary>
        internal static Func<IBundledGames> BundledGamesSource { get; set; } = DefaultBundledGamesSource;

        internal static IBundledGames DefaultBundledGamesSource()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidBundledGames();
#else
            return new FolderBundledGames(Application.streamingAssetsPath);
#endif
        }

        internal static string BundledGamesStatePath => Path.Combine(Application.persistentDataPath, "games-install");

        /// <summary>
        /// Installs the default game if this is a first launch, starts installing the other bundled games in the
        /// background, and reads the installed games.
        /// </summary>
        internal void LookupCardGames()
        {
#if UNITY_WEBGL
            // The web build downloads its default games instead
            if (!Directory.Exists(UnityCardGame.GamesDirectoryPath))
                return;
#else
            BundledGames = new BundledGameInstaller(BundledGamesSource(), UnityCardGame.GamesDirectoryPath,
                BundledGamesStatePath);
            BundledGames.Prepare();
            BundledGames.InstallNow(FirstBundledGameToInstall(BundledGames.GetPending()));
#endif
            foreach (var gameDirectory in Directory.GetDirectories(UnityCardGame.GamesDirectoryPath))
            {
                var gameDirectoryName = gameDirectory[(UnityCardGame.GamesDirectoryPath.Length + 1)..];
                if (AddressableAssetsFolderName.Equals(gameDirectoryName))
                    continue;
                var newCardGame = new UnityCardGame(this, gameDirectoryName);
                // Decoding every game's images here would delay the first frame; see LoadVisiblePreviewImages
                newCardGame.ReadProperties(false);
                if (!string.IsNullOrEmpty(newCardGame.Error))
                    Debug.LogError(newCardGame.Error);
                else
                    AllCardGames[newCardGame.Id] = newCardGame;
            }
#if !UNITY_WEBGL
            BundledGames.StartBackground();
#endif
        }

#if !UNITY_WEBGL
        // The game the Main Menu will show: the player's saved game, else Standard Playing Cards, else any
        private static string FirstBundledGameToInstall(IReadOnlyList<string> pending)
        {
            var preferredGameId = PlayerPrefs.GetString(PlayerPrefsDefaultGame, Tags.StandardPlayingCardsDirectoryName);
            if (pending.Contains(preferredGameId))
                return preferredGameId;
            if (pending.Contains(Tags.StandardPlayingCardsDirectoryName))
                return Tags.StandardPlayingCardsDirectoryName;
            return pending.FirstOrDefault();
        }
#endif

        /// <summary>Adds the bundled games that have finished installing in the background.</summary>
        private void AddInstalledBundledGames()
        {
            if (BundledGames == null)
                return;
            var isAdded = false;
            while (BundledGames.TryTakeInstalled(out var gameId))
            {
                if (AllCardGames.ContainsKey(gameId))
                    continue;
                var newCardGame = new UnityCardGame(this, gameId);
                newCardGame.ReadProperties(false);
                if (!string.IsNullOrEmpty(newCardGame.Error))
                {
                    Debug.LogError(newCardGame.Error);
                    continue;
                }

                AllCardGames[newCardGame.Id] = newCardGame;
                isAdded = true;
            }

            if (!isAdded)
                return;
            if (Current == UnityCardGame.UnityInvalid)
            {
                ResetCurrentToDefault();
                ResetGameScene();
            }
            else
                LoadVisiblePreviewImages();

            GamesAdded?.Invoke();
        }

        /// <summary>
        /// Decodes the banners and card backs that game selection shows now: the current game and its neighbors.
        /// </summary>
        private void LoadVisiblePreviewImages()
        {
            foreach (var game in new[] { Current, Previous, Next })
                if (game != UnityCardGame.UnityInvalid && !game.HasPreviewImages)
                    game.LoadPreviewImages();
        }

        /// <summary>Decodes the other games' banners and card backs in the background, one game at a time.</summary>
        private IEnumerator LoadPreviewImagesInBackground()
        {
            foreach (var game in AllCardGames.Values.ToList())
            {
                if (game.HasPreviewImages || !AllCardGames.ContainsValue(game))
                    continue;
                yield return game.LoadPreviewImagesAsync();
            }
        }

        public void ImportCardGame(string zipFilePath)
        {
            ImportCardGame(zipFilePath, null);
        }

        internal void ImportCardGame(string zipFilePath, Action completed)
        {
            if (!File.Exists(zipFilePath))
            {
                var errorMessage = string.Format(FileNotFoundErrorMessage, zipFilePath);
                Debug.LogError(errorMessage);
                Messenger.Show(UiMessage.With("errors.file.missing", errorMessage, ("detail", zipFilePath)));
                completed?.Invoke();
                return;
            }

            if (!zipFilePath.EndsWith(CgsZipExtension, StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage = string.Format(InvalidCgsZipFileErrorMessage, zipFilePath);
                Debug.LogError(errorMessage);
                Messenger.Show(UiMessage.With("errors.zip.invalid", errorMessage, ("detail", zipFilePath)));
                completed?.Invoke();
                return;
            }

            var fileName = Path.GetFileName(zipFilePath);
            var gameId = fileName.EndsWith(CgsZipExtension, StringComparison.OrdinalIgnoreCase)
                ? fileName[..^CgsZipExtension.Length]
                : Path.GetFileNameWithoutExtension(fileName);
            var targetGameDirectory = Path.Combine(UnityCardGame.GamesDirectoryPath, gameId);
            if (Directory.Exists(targetGameDirectory))
            {
                Messenger.Ask(new UiMessage("games.overwrite", OverwriteGamePrompt), null,
                    () => ForceImportCardGame(zipFilePath), closed: completed);
                return;
            }

            ImportAndComplete();

            void ImportAndComplete()
            {
                try
                {
                    ForceImportCardGame(zipFilePath);
                }
                finally
                {
                    completed?.Invoke();
                }
            }
        }

        private void ForceImportCardGame(string zipFilePath)
        {
            var fileName = Path.GetFileName(zipFilePath);
            var gameId = fileName.EndsWith(CgsZipExtension, StringComparison.OrdinalIgnoreCase)
                ? fileName[..^CgsZipExtension.Length]
                : Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(gameId))
            {
                var errorMessage = string.Format(FileNotFoundErrorMessage, zipFilePath);
                Debug.LogError(errorMessage);
                Messenger.Show(UiMessage.With("errors.file.missing", errorMessage, ("detail", zipFilePath)));
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
                        Messenger.Show(UiMessage.With("errors.file.missing", errorMessage, ("detail", importGameDirectory)));
                        return;
                    }
                }

                var targetGameDirectory = Path.Combine(UnityCardGame.GamesDirectoryPath, gameId);
                LocalizationCache.Invalidate(targetGameDirectory);
                if (Directory.Exists(targetGameDirectory))
                    Directory.Delete(targetGameDirectory, true);

                UnityFileMethods.CopyDirectory(importGameDirectory, targetGameDirectory);

                var newCardGame = new UnityCardGame(this, gameId);
                newCardGame.ReadProperties();
                if (!string.IsNullOrEmpty(newCardGame.Error))
                {
                    Debug.LogError(LoadErrorMessage + newCardGame.Error);
                    Messenger.Show(UiMessage.With("errors.game.load", LoadErrorMessage + newCardGame.Error, ("detail", newCardGame.Error)));
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
                Messenger.Show(UiMessage.With("errors.import", ImportFailureErrorMessage + e, ("detail", e)));
            }
        }

        private void ShowLogToUser(string logString, string stackTrace, LogType type)
        {
            if (!IsQuitting && Messenger != null && !LogType.Log.Equals(type))
                Messenger.Show(logString);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolutionManager.ScaleResolution();
            // The new scene shows the current game from its properties on its first frame. Its cards, sets, and
            // images load after that frame, and the scene fills in when they finish.
            RunSceneActions();
            // A scene opened after the content loaded has nothing to fill in, and its actions must not run twice
            if (!NeedsContentLoad(Current) && !Current.IsLoading)
            {
                ++_selectionVersion;
                return;
            }

            IsContentLoadScheduled = true;
            StartCoroutine(ResetGameSceneAsync(Current, ++_selectionVersion, true));
        }

        private void OnSceneUnloaded(Scene scene)
        {
            OnSceneActions.Clear();
            SceneNeedsContent = false;
        }

        /// <summary>
        /// Called by a screen that shows the current game's cards when it starts. The screen shows loading progress while
        /// the cards load, which also keeps the player from changing the screen before it fills in.
        /// </summary>
        internal void RequireContentForScene()
        {
            SceneNeedsContent = true;
            UpdateContentLoadProgress();
        }

        /// <summary>
        /// Waits until the current game's content has loaded, or failed to, and the open screen has filled in with it.
        /// </summary>
        internal static IEnumerator WaitForCurrentContent()
        {
            while (!IsCurrentContentSettled)
                yield return null;
            // The screen fills in on the frame the load finishes, so it is done by the next frame
            yield return null;
        }

        private void UpdateContentLoadProgress()
        {
            if (progress == null)
                return;

            // Downloads and updates show their own progress
            var shouldShow = SceneNeedsContent && !IsCurrentContentSettled && Current != UnityCardGame.UnityInvalid &&
                             !Current.IsDownloading && string.IsNullOrEmpty(Current.Error);
            if (shouldShow && !progress.gameObject.activeSelf)
                progress.Show(_contentLoadProgress);
            else if (!shouldShow && progress.IsShowing(_contentLoadProgress))
                progress.Hide();
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
            LoadVisiblePreviewImages();
            GameContentPresentation.Select(Current);
        }

        /// <summary>The link that launched CGS; tests substitute it to simulate a cold start from a link.</summary>
        internal static Func<string> LaunchDeepLink { get; set; } = () => Application.absoluteURL;

        private void CheckDeepLinks()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;
            Application.deepLinkActivated += OnDeepLinkActivated;

            Debug.Log("Checking Deep Links...");
            var launchDeepLink = LaunchDeepLink();
            if (string.IsNullOrEmpty(launchDeepLink))
            {
                Debug.Log("No Start Deep Link");
                return;
            }

            if (!Uri.IsWellFormedUriString(launchDeepLink, UriKind.RelativeOrAbsolute))
            {
                Debug.LogWarning("Start Deep Link malformed: " + launchDeepLink);
                return;
            }

            Debug.Log("Start Deep Link: " + launchDeepLink);
            OnDeepLinkActivated(launchDeepLink);
        }

        internal void OnDeepLinkActivated(string deepLink)
        {
            Debug.Log("OnDeepLinkActivated!");
            // Benchmark links are honored only when they launch CGS (see StartupTimeline); a benchmark link that
            // arrives later must leave the current session unchanged, and benchmark runs never download from links
            if (BenchmarkOptions.IsBenchmarkLink(deepLink) || StartupTimeline.IsBenchmarking)
            {
                Debug.Log("OnDeepLinkActivated: ignoring link during or for benchmark mode");
                return;
            }

            DeepLinkLoadScene(deepLink);
            DeepLinkGetAutoUpdateUrl(deepLink);
        }

        private static void DeepLinkLoadScene(string deepLink)
        {
            var scene = GetScene(deepLink);
            int sceneIndex;
            if ("main".Equals(scene, StringComparison.OrdinalIgnoreCase))
                sceneIndex = Tags.MainMenuSceneIndex;
            else if ("play".Equals(scene, StringComparison.OrdinalIgnoreCase))
                sceneIndex = Tags.PlayModeSceneIndex;
            else if ("decks".Equals(scene, StringComparison.OrdinalIgnoreCase))
                sceneIndex = Tags.DeckEditorSceneIndex;
            else if ("cards".Equals(scene, StringComparison.OrdinalIgnoreCase))
                sceneIndex = Tags.CardsExplorerSceneIndex;
            else if ("settings".Equals(scene, StringComparison.OrdinalIgnoreCase))
                sceneIndex = Tags.SettingsSceneIndex;
            else
                return;

            Debug.Log("DeepLinkLoadScene::" + scene);
            // CGS launches on the Main Menu, so a launch link to it must not load the Main Menu a second time
            if (sceneIndex == Tags.MainMenuSceneIndex &&
                SceneManager.GetActiveScene().buildIndex == Tags.MainMenuSceneIndex)
                return;
            SceneManager.LoadScene(sceneIndex);
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
            var autoUpdateUrl = GetQueryParameter(deepLinkUriQuery, "url");
            Debug.Log("GetAutoUpdateUrl::autoUpdateUrl: " + autoUpdateUrl);

            return autoUpdateUrl;
        }

        // Returns the first value of the named parameter in a URI query string, or null if it is missing.
        // Replaces System.Web.HttpUtility.ParseQueryString, which is not available on every platform.
        internal static string GetQueryParameter(string query, string name)
        {
            foreach (var parameter in query.TrimStart('?').Split('&'))
            {
                var separatorIndex = parameter.IndexOf('=');
                var key = separatorIndex < 0 ? parameter : parameter.Substring(0, separatorIndex);
                if (string.Equals(DecodeQueryComponent(key), name, StringComparison.OrdinalIgnoreCase))
                    return separatorIndex < 0 ? string.Empty : DecodeQueryComponent(parameter.Substring(separatorIndex + 1));
            }

            return null;
        }

        private static string DecodeQueryComponent(string value)
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        protected IEnumerator Start()
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
            // The current game's content loads after the first frame; wait for it before deciding it is missing
            while (IsContentLoadScheduled || Current is { IsLoading: true })
                yield return null;
            var isMissingGame =
 Current == null || Current == UnityCardGame.UnityInvalid || !string.IsNullOrEmpty(Current.Error) || !Current.HasLoaded;
            if (isMissingGame)
                yield return GetDefaultCardGames(DefaultWebGames);
#endif

            HasStartedUp = true;
            StartCoroutine(LoadPreviewImagesInBackground());
        }

        // The games the web app downloads when it has none, by ID and definition URL
        internal static readonly (string Id, string Url)[] DefaultWebGames =
        {
            (Tags.StandardPlayingCardsDirectoryName, Tags.StandardPlayingCardsUrl),
            ("Dominoes@www.cardgamesimulator.com", Tags.DominoesUrl),
            ("Mahjong@www.cardgamesimulator.com", Tags.MahjongUrl)
        };

        // The new game downloading now, until it has loaded and been added to the games. A new game downloads into a
        // folder named for it only once its definition has been read, so new games download one at a time.
        private UnityCardGame _gameDownload;

        private bool IsDownloadingGame => _gameDownload != null;

        private UnityCardGame InstalledGameFrom(string gameUrl)
        {
            var url = new Uri(gameUrl);
            return AllCardGames.Values.FirstOrDefault(game => game?.AutoUpdateUrl != null && game.AutoUpdateUrl.Equals(url));
        }

        /// <summary>Whether default games are still downloading in the background.</summary>
        internal bool IsDownloadingDefaultGames { get; private set; }

        /// <summary>
        /// Downloads <paramref name="games"/> when CGS has none, as on a first web visit. The player's saved game, else
        /// Standard Playing Cards, downloads first and is selected, so that it can be started; the others then download
        /// in the background without selecting them, showing progress, or keeping the menu from Start Game.
        /// </summary>
        internal IEnumerator GetDefaultCardGames(IReadOnlyList<(string Id, string Url)> games)
        {
            if (games.Count == 0)
                yield break;
            var preferredId = PlayerPrefs.GetString(PlayerPrefsDefaultGame, Tags.StandardPlayingCardsDirectoryName);
            var first = games.FirstOrDefault(game => game.Id == preferredId);
            if (first.Url == null)
                first = games.FirstOrDefault(game => game.Id == Tags.StandardPlayingCardsDirectoryName);
            if (first.Url == null)
                first = games[0];

            IsDownloadingDefaultGames = true;
            yield return GetCardGame(first.Url);
            StartCoroutine(DownloadGamesInBackground(games.Where(game => game.Url != first.Url)
                .Select(game => game.Url).ToList()));
        }

        private IEnumerator DownloadGamesInBackground(IEnumerable<string> gameUrls)
        {
            // The selected game's screen draws first
            yield return null;
            foreach (var gameUrl in gameUrls)
            {
                // Let a download the player started finish first, then check again whether it installed this game
                while (IsDownloadingGame || Current is { IsDownloading: true })
                    yield return null;
                if (InstalledGameFrom(gameUrl) != null)
                    continue;

                var cardGame = new UnityCardGame(this, CardGame.DefaultName, gameUrl);
                _gameDownload = cardGame;
                yield return cardGame.Download();
                if (_gameDownload == cardGame)
                    _gameDownload = null;
                if (!string.IsNullOrEmpty(cardGame.Error) || !cardGame.HasReadProperties)
                {
                    Debug.LogWarning(DownloadErrorMessage + cardGame.Error);
                    DeleteFailedDownload(cardGame);
                    continue;
                }

                // Added in the same frame that its download is released, so no other request downloads it again
                AllCardGames[cardGame.Id] = cardGame;
                // If the player's game failed to download, show one that did
                if (Current == UnityCardGame.UnityInvalid)
                {
                    ResetCurrentToDefault();
                    ResetGameScene();
                }
                else
                    LoadVisiblePreviewImages();
                GamesAdded?.Invoke();
            }

            IsDownloadingDefaultGames = false;
        }

        private static void DeleteFailedDownload(UnityCardGame cardGame)
        {
            if (!Directory.Exists(cardGame.GameDirectoryPath))
                return;
            try
            {
                LocalizationCache.Invalidate(cardGame.GameDirectoryPath);
                Directory.Delete(cardGame.GameDirectoryPath, true);
            }
            catch (Exception ex)
            {
                Debug.LogError(DeleteErrorMessage + ex.Message);
            }
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
                Messenger.Show(new UiMessage("games.url.missing", "Cannot download the game: its URL is missing."));
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
            while (IsDownloadingGame)
                yield return null;
            // A background download may have installed this game while this one waited
            var installedGame = InstalledGameFrom(gameUrl);
            if (installedGame != null)
            {
                Progress.Hide();
                Select(installedGame.Id);
                yield break;
            }

            // The download keeps its slot until the game has loaded and been added, since a load reads the files that
            // another download of the same game would write
            _gameDownload = cardGame;
            yield return cardGame.Download();
            Progress.Hide();

            yield return cardGame.LoadRoutine(UpdateCardGame, LoadCards, LoadSetCards);

            if (!string.IsNullOrEmpty(cardGame.Error))
            {
                Debug.LogError(DownloadErrorMessage + cardGame.Error);
                ShowDownloadError(cardGame.Error);
                DeleteFailedDownload(cardGame);
            }
            else
            {
                AllCardGames[cardGame.Id] = cardGame;
                Select(cardGame.Id);
            }

            if (_gameDownload == cardGame)
                _gameDownload = null;

            Debug.Log("DownloadCardGame: end");
        }

        public IEnumerator UpdateCardGame(UnityCardGame cardGame)
        {
            cardGame ??= Current;

            // The download replaces the loaded cards, so let a load that is still running finish first; otherwise that
            // load would end without the downloaded cards
            while (cardGame.IsLoading)
                yield return null;

            Progress.Show(cardGame);
            yield return cardGame.Download(true);
            Progress.Hide();

            // Notify about the failed update, but otherwise ignore errors
            if (!string.IsNullOrEmpty(cardGame.Error))
            {
                Debug.LogError(DownloadErrorMessage + cardGame.Error);
                ShowDownloadError(cardGame.Error);
                cardGame.ClearError();
            }

            yield return cardGame.LoadRoutine(null, LoadCards, LoadSetCards);
            if (cardGame == Current)
                ResetGameScene();
        }

        private void ShowDownloadError(string detail)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            detail += "\n\n" + BrowserFileBrowser.DownloadHelp;
#endif
            Messenger.Show(UiMessage.With("errors.game.download", DownloadErrorMessage + detail, ("detail", detail)));
        }

        private IEnumerator LoadCards(UnityCardGame cardGame)
        {
            cardGame ??= Current;

            for (var page = cardGame.AllCardsUrlPageCountStartIndex;
                 page < cardGame.AllCardsUrlPageCountStartIndex + cardGame.AllCardsUrlPageCount;
                 page++)
            {
                // Card files parse off the main thread and fill in over several frames, so the screen stays responsive
                yield return cardGame.LoadCardsRoutine(page);
                if (page == cardGame.AllCardsUrlPageCountStartIndex && cardGame == Current &&
                    cardGame.AllCardsUrlPageCount > CardsLoadingMessageThreshold)
                    Messenger.Show(GameContentMessage.Create(cardGame, display => UiMessage.With("games.cards.loading",
                        string.Format(CardsLoadingMessage, display.GameName), ("gameName", display.GameName))));
                yield return null;
            }

            if (!string.IsNullOrEmpty(cardGame.Error))
                Debug.LogError(LoadErrorMessage + cardGame.Error);
            else if (cardGame == Current && cardGame.AllCardsUrlPageCount > CardsLoadingMessageThreshold)
                Messenger.Show(GameContentMessage.Create(cardGame, display => UiMessage.With("games.cards.loaded",
                    string.Format(CardsLoadedMessage, display.GameName), ("gameName", display.GameName))));
        }

#pragma warning disable S2325
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private IEnumerator LoadSetCards(UnityCardGame cardGame)
#pragma warning restore S2325
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
                    yield return cardGame.LoadCardsRoutine(setCardsFilePath, set.Code);
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
                Messenger.Show(UiMessage.With("errors.game.select", SelectionErrorMessage + gameId, ("detail", gameId)));
                return;
            }

            // Immediately set current so UI can update without waiting for heavy load work
            Current = game;
            LoadVisiblePreviewImages();
            GameContentPresentation.Select(game);
            // Start an async reset so heavy load work doesn't block the main thread/UI animation
            StartCoroutine(ResetGameSceneAsync(game, ++_selectionVersion));
        }

        /// <summary>
        /// Loads <paramref name="targetGame"/>'s content in the background, then resets the scene. A later selection or
        /// scene load supersedes this one; a load that is already running is waited for instead of being repeated.
        /// </summary>
        /// <param name="afterFirstFrame">Waits until the scene has drawn and accepted input once, as on scene load.
        /// Otherwise, as when the player selects a game, the scene also refreshes every frame while it loads.</param>
        private IEnumerator ResetGameSceneAsync(UnityCardGame targetGame, int selectionVersion,
            bool afterFirstFrame = false)
        {
            if (afterFirstFrame)
            {
                yield return null;
                // Batch-mode Editors and players never reach the end of a rendered frame
                if (Application.isBatchMode)
                    yield return null;
                else
                    yield return new WaitForEndOfFrame();
                IsContentLoadScheduled = false;
            }

            if (IsSuperseded(targetGame, selectionVersion))
                yield break;

            if (NeedsContentLoad(targetGame) || targetGame.IsLoading)
            {
                if (!targetGame.IsLoading)
                    targetGame.LoadAsync(UpdateCardGame, LoadCards, LoadSetCards);
                yield return null;
                while (targetGame.IsLoading)
                {
                    if (IsSuperseded(targetGame, selectionVersion))
                        yield break;
                    // A screen that needs the cards shows progress instead and fills in once they have loaded
                    if (!afterFirstFrame && !SceneNeedsContent && string.IsNullOrEmpty(targetGame.Error))
                        RunSceneActions();
                    yield return null;
                }
            }

            if (!IsSuperseded(targetGame, selectionVersion))
                RunSceneActions();
        }

        private bool IsSuperseded(UnityCardGame targetGame, int selectionVersion) =>
            targetGame != Current || selectionVersion != _selectionVersion;

        private static bool NeedsContentLoad(UnityCardGame game) =>
            game != UnityCardGame.UnityInvalid && !game.HasLoaded && !game.IsLoading && !game.IsDownloading &&
            string.IsNullOrEmpty(game.Error);

        /// <summary>
        /// Refreshes the scene for the current game, first loading its content in the background if it has not loaded.
        /// </summary>
        internal void ResetGameScene()
        {
            if (NeedsContentLoad(Current))
            {
                StartCoroutine(ResetGameSceneAsync(Current, ++_selectionVersion));
                return;
            }

            RunSceneActions();
        }

        /// <summary>Runs the scene's actions for the current game as it is now, without loading anything.</summary>
        private void RunSceneActions()
        {
            // No game is installed yet (as on a first web visit), or it is still downloading
            if (Current == UnityCardGame.UnityInvalid || Current.IsDownloading)
                return;

            if (!string.IsNullOrEmpty(Current.Error))
            {
                if (UnityCardGame.UnityInvalid == Current || Current == CardGame.Invalid)
                    return;
                Debug.LogError(LoadErrorMessage + Current.Error);
                Messenger.Ask(new UiMessage("games.load.corrupt", LoadErrorPrompt), IgnoreCurrentErroredGame, Delete);
                return;
            }

            // Once the game has loaded is the safest time to set it as the preferred default game for the player.
            // Benchmark runs must not change saved preferences.
            if (Current.HasLoaded && !StartupTimeline.IsBenchmarking)
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
                TextSharing.CopyOrShare(shareMessage, Messenger.ShowSharingStatus);
            }
            else
                ExportGame();
        }

        private static void ExportGame()
        {
            try
            {
                ExportGameFile();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(ExportFailureErrorMessage + exception.Message);
                Instance.Messenger.Show(UiMessage.With("errors.game.export",
                    ExportFailureErrorMessage + Current.Name, ("path", Current.Name)));
            }
        }

        private static void ExportGameFile()
        {
            var container = Path.Combine(UnityCardGame.GamesExportPath, UnityFileMethods.GetSafeFileName(Current.Id));
            Debug.Log("CGS Share::container: " + container);
            if (Directory.Exists(container))
                Directory.Delete(container, true);

            var subContainer = Path.Combine(container, UnityFileMethods.GetSafeFileName(Current.Id));
            Debug.Log("CGS Share::subContainer: " + subContainer);
            LocalizationFiles.CopyPackageSnapshot(Current.GameDirectoryPath, subContainer);

            var zipFileName = UnityFileMethods.GetSafeFileName(Current.Id + CgsZipExtension);
            Debug.Log("CGS Share::zipFileName: " + zipFileName);
            UnityFileMethods.CreateZip(container, UnityCardGame.GamesExportPath, zipFileName);
            Directory.Delete(container, true);

            var targetZipFilePath = Path.Combine(UnityCardGame.GamesExportPath, zipFileName);
            Debug.Log("CGS Share::targetZipFilePath: " + targetZipFilePath);
            Debug.Log("CGS Share::size: " + new FileInfo(targetZipFilePath).Length);

#if UNITY_WEBGL && !UNITY_EDITOR
            BrowserFileBrowser.Download(targetZipFilePath);
            File.Delete(targetZipFilePath);
#else
            var exportGameZipUri = new Uri(targetZipFilePath);
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
                            CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.game.export", ExportGameErrorMessage + exportGameZipUri.LocalPath, ("path", exportGameZipUri.LocalPath)));
                        }
                    }
                    else
                    {
                        Debug.LogError(ExportGameErrorMessage + exportGameZipUri.LocalPath);
                        CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.game.export", ExportGameErrorMessage + exportGameZipUri.LocalPath, ("path", exportGameZipUri.LocalPath)));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + e.StackTrace);
                    CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.game.export", ExportGameErrorMessage + exportGameZipUri.LocalPath, ("path", exportGameZipUri.LocalPath)));
                }
            }, false);
#elif UNITY_ANDROID && !UNITY_EDITOR
            var tempCgsZipFilePath = Path.Combine( Application.temporaryCachePath, Current.Id + CgsZipExtension );
            Instance.StartCoroutine(Instance.OpenZip(exportGameZipUri, tempCgsZipFilePath));
#elif UNITY_IOS && !UNITY_EDITOR
            TextSharing.ShareFile(targetZipFilePath, Instance.Messenger.ShowSharingStatus, "application/zip");
#else
            Application.OpenURL(exportGameZipUri.AbsoluteUri);
#endif
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        public IEnumerator OpenZip(Uri uri, string tempCgsZipFilePath)
        {
            using var uwr = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET);
            uwr.downloadHandler = new DownloadHandlerFile(tempCgsZipFilePath);
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Messenger.ShowSharingStatus(TextSharing.FileReadErrorMessage);
                yield break;
            }

            TextSharing.ShareFile(tempCgsZipFilePath, Messenger.ShowSharingStatus, "application/zip");
        }
#endif

        public void PromptDelete()
        {
            if (AllCardGames.Count > 1)
                Messenger.Prompt(new UiMessage("games.delete.confirm", DeletePrompt), Delete);
            else
                Messenger.Show(new UiMessage("games.delete.warning", DeleteWarningMessage));
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
                LocalizationCache.Invalidate(Current.GameDirectoryPath);
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

        protected void Update()
        {
            if (!InputSystem.actions.enabled)
            {
                Debug.LogWarning("InputSystem.actions.enabled is false, enabling it.");
                InputSystem.actions.Enable();
            }

            if (_tooltipsAction?.WasPressedThisFrame() ?? false)
                Settings.ButtonTooltipsEnabled = !Settings.ButtonTooltipsEnabled;
            if (_previewAction?.WasPressedThisFrame() ?? false)
                Settings.PreviewOnMouseOver = !Settings.PreviewOnMouseOver;
            if (_reprintsAction?.WasPressedThisFrame() ?? false)
                Settings.HideReprints = !Settings.HideReprints;
            if (_developerAction?.WasPressedThisFrame() ?? false)
                Settings.DeveloperMode = !Settings.DeveloperMode;

            ImageQueueService.Instance.ProcessQueue(this);
            UpdateContentLoadProgress();
            AddInstalledBundledGames();
        }

        protected void OnDisable()
        {
            UnregisterStaticEventHandlers();
        }

        private void UnregisterStaticEventHandlers()
        {
            Application.logMessageReceived -= ShowLogToUser;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Application.deepLinkActivated -= OnDeepLinkActivated;
        }

        protected void OnDestroy()
        {
            UnregisterStaticEventHandlers();
            if (_instance != this)
                return;
            Instance = null;
            // The network manager used to be part of this object, so it still goes with it
            if (!IsQuitting)
                CgsNetManager.DestroyCreated();
        }

#pragma warning disable S2325
        // ReSharper disable once MemberCanBeMadeStatic.Local
        protected void OnApplicationQuit()
#pragma warning restore S2325
        {
            IsQuitting = true;
        }
    }
}
