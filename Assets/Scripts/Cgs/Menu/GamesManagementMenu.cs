/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Localization;
using System.Collections;
using System.IO;
using Cgs.UI;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;
using System;
using UnityEngine.Events;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
using SFB;
#elif !UNITY_ANDROID && !UNITY_IOS
using SimpleFileBrowser;
#endif

namespace Cgs.Menu
{
    [RequireComponent(typeof(Modal))]
    public class GamesManagementMenu : SelectionPanel
    {
        public const string ImportGamePrompt = "Download from Web AutoUpdate URL,\n or Load from CGS ZIP File?";
        public const string DownloadFromWeb = "Download from Web AutoUpdate URL";
        public const string LoadFromFile = "Load from CGS ZIP File";
        public const string SelectZipFilePrompt = "Select CGS ZIP File";

        public const string DownloadLabel = "Download Game";
        public const string DownloadPrompt = "Enter CGS AutoUpdate URL...";

        public static string NoSyncMessage => $"{CardGameManager.Current.Name} does not have a CGS AutoUpdate URL!";

        [SerializeField] GameObject cgsGamesBrowserPrefab;
        [SerializeField] GameObject cardGameEditorMenuPrefab;
        [SerializeField] GameObject gameImportModalPrefab;
        [SerializeField] GameObject downloadMenuPrefab;

        [SerializeField] RectTransform browseButton;
        [SerializeField] RectTransform newButton;
        [SerializeField] RectTransform editButton;
        [SerializeField] RectTransform syncButton;

        protected override bool AllowSwitchOff => false;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private CgsGamesBrowser CgsGamesBrowserMenu =>
            _cgsGamesBrowser ??= Instantiate(cgsGamesBrowserPrefab).GetOrAddComponent<CgsGamesBrowser>();

        private CgsGamesBrowser _cgsGamesBrowser;

        private CardGameEditorMenu CardGameEditor =>
            _cardGameEditor ??= Instantiate(cardGameEditorMenuPrefab).GetOrAddComponent<CardGameEditorMenu>();

        private CardGameEditorMenu _cardGameEditor;

        private DecisionModal ImportModal =>
            _importModal ??= Instantiate(gameImportModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _importModal;

        private DownloadMenu Downloader => _downloader ??= Instantiate(downloadMenuPrefab)
            .GetOrAddComponent<DownloadMenu>();

        private DownloadMenu _downloader;

        private InputAction _moveAction;
        private InputAction _pageAction;

        protected void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(BuildGameSelectionOptions);
            _gamesSource = CardGameManager.Instance;
            _gamesSource.GamesAdded += BuildGameSelectionOptions;

            InputSystem.actions.FindAction(Tags.DecksNew).performed += InputNew;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed += InputLoad;
            InputSystem.actions.FindAction(Tags.SubMenuCopyShare).performed += InputCopyShare;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed += InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

#if UNITY_ANDROID || UNITY_IOS
        private static string ZipFileType { get; set; }
#endif

        protected void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            ZipFileType = NativeFilePicker.ConvertExtensionToFileType("zip");
#endif
            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        // Poll for Vector2 inputs
        protected void Update()
        {
            if (Menu.IsBlocked)
                return;

            var pageVertical = _pageAction?.ReadValue<Vector2>().y ?? 0;
            if (Mathf.Abs(pageVertical) > 0)
            {
                var delta = pageVertical * Time.deltaTime;
                ScrollRect.verticalNormalizedPosition = Mathf.Clamp01(ScrollRect.verticalNormalizedPosition + delta);
            }

            if (!(_moveAction?.WasPressedThisFrame() ?? false))
                return;
            var moveVertical = _moveAction.ReadValue<Vector2>().y;
            switch (moveVertical)
            {
                case > 0f:
                    SelectPrevious();
                    break;
                case < 0f:
                    SelectNext();
                    break;
            }
        }

        public void Show()
        {
            Menu.Show();
            browseButton.gameObject.SetActive(!Settings.DeveloperMode);
            newButton.gameObject.SetActive(Settings.DeveloperMode);
            editButton.gameObject.SetActive(Settings.DeveloperMode && !CardGameManager.Current.IsUploaded);
            syncButton.gameObject.SetActive(!Settings.DeveloperMode || CardGameManager.Current.IsUploaded);
            BuildGameSelectionOptions();
        }

        private void BuildGameSelectionOptions()
        {
            Rebuild(CardGameManager.Instance.AllCardGames, SelectGame, CardGameManager.Current.Id);
        }

        [UsedImplicitly]
        public void SelectGame(Toggle toggle, string gameId)
        {
            if (toggle != null && toggle.isOn && gameId != CardGameManager.Current.Id)
                CardGameManager.Instance.Select(gameId);
        }

        private void InputNew(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            if (Settings.DeveloperMode)
                CreateNew();
            else
                ShowCgsGamesBrowser();
        }

        [UsedImplicitly]
        public void ShowCgsGamesBrowser()
        {
            CgsGamesBrowserMenu.Show();
        }

        [UsedImplicitly]
        public void CreateNew()
        {
            CardGameEditor.ParentMenu = this;
            CardGameEditor.ShowNew();
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            if (Settings.DeveloperMode && !CardGameManager.Current.IsUploaded)
                EditCurrent();
            else
                Sync();
        }

        [UsedImplicitly]
        public void EditCurrent()
        {
            CardGameEditor.ParentMenu = this;
            CardGameEditor.ShowCurrent();
        }

        private void InputLoad(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            Import();
        }

        [UsedImplicitly]
        public void Import()
        {
            ImportModal.Show(new UiMessage("games.import.choice", ImportGamePrompt),
                new Tuple<UiMessage, UnityAction>(new UiMessage("games.import.web", DownloadFromWeb), ShowDownloader),
                new Tuple<UiMessage, UnityAction>(new UiMessage("games.import.file", LoadFromFile), ShowFileLoader));
        }

        private void ShowDownloader()
        {
            Downloader.Show(new UiMessage("games.download", DownloadLabel), new UiMessage("games.download.url", DownloadPrompt), CardGameManager.Instance.GetCardGame, true);
        }

        // ReSharper disable once UnusedMember.Local
        private static void ShowFileLoader()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BrowserFileBrowser.OpenGameZip();
#elif UNITY_ANDROID || UNITY_IOS
            NativeFilePicker.PickFile(path =>
            {
                if (path == null)
                    Debug.Log("Operation cancelled");
                else
                    CardGameManager.Instance.ImportCardGame(path);
            }, ZipFileType);
#elif ENABLE_WINMD_SUPPORT
            ImportCardGameFromUwpFile();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            // The sandboxed Mac app can read only files chosen in the system dialog.
            StandaloneFileBrowser.OpenFilePanelAsync(
                CgsLocalization.Text(new UiMessage("files.select.zip", SelectZipFilePrompt)), string.Empty,
                new[] { new ExtensionFilter(string.Empty, "zip") }, false, paths =>
                {
                    if (paths is { Length: > 0 } && !string.IsNullOrEmpty(paths[0]))
                        CardGameManager.Instance.ImportCardGame(paths[0]);
                    else
                        Debug.Log("Operation cancelled");
                });
#else
            FileBrowser.ShowLoadDialog((paths) => CardGameManager.Instance.ImportCardGame(paths[0]),
                () => { }, FileBrowser.PickMode.Files, false, null, null,
                CgsLocalization.Text(new UiMessage("files.select.zip", SelectZipFilePrompt)));
#endif
        }

#if ENABLE_WINMD_SUPPORT
        // The Store app can read only files chosen in the Windows picker, so import a cached copy.
        private static async void ImportCardGameFromUwpFile()
        {
            var path = UwpFileBrowser.OpenFilePanel();
            // A picked file can lack a filesystem path, so only a missing file means cancellation.
            if (UwpFileBrowser.LastOpenFile == null)
            {
                Debug.Log("Operation cancelled");
                return;
            }

            try
            {
                CardGameManager.Instance.ImportCardGame(await UnityFileMethods.CacheFileAsync(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(CardGameManager.ImportFailureErrorMessage + exception.Message);
                CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.import",
                    CardGameManager.ImportFailureErrorMessage + exception.Message, ("detail", exception.Message)));
            }
        }
#endif

        private void InputCopyShare(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            Share();
        }

        [UsedImplicitly]
        public void Share()
        {
            CardGameManager.Instance.Share();
        }

        private void InputDelete(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            Delete();
        }

        [UsedImplicitly]
        public void Delete()
        {
            CardGameManager.Instance.PromptDelete();
        }

        [UsedImplicitly]
        public void Sync()
        {
            // The manager runs the sync, so it continues after this menu hides
            if (CardGameManager.Current.AutoUpdateUrl?.IsWellFormedOriginalString() ?? false)
                CardGameManager.Instance.StartCoroutine(SyncGame(CardGameManager.Current));
            else
                CardGameManager.Instance.Messenger.Show(Cgs.GameContent.GameContentMessage.Create(CardGameManager.Current, display =>
                    UiMessage.With("games.sync.unavailable", NoSyncMessage, ("gameName", display.GameName))));

            Hide();
        }

        private static IEnumerator SyncGame(UnityCardGame game)
        {
            // Card data loads in the background after a screen opens; let it finish before deleting its files
            while (game.IsLoading)
                yield return null;

            if (File.Exists(game.CardsFilePath))
                File.Delete(game.CardsFilePath);
            if (File.Exists(game.DecksFilePath))
                File.Delete(game.DecksFilePath);
            if (File.Exists(game.SetsFilePath))
                File.Delete(game.SetsFilePath);
            if (File.Exists(game.BannerImageFilePath))
                File.Delete(game.BannerImageFilePath);
            if (File.Exists(game.CardBackImageFilePath))
                File.Delete(game.CardBackImageFilePath);
            if (File.Exists(game.PlayMatImageFilePath))
                File.Delete(game.PlayMatImageFilePath);
            if (Directory.Exists(game.SetsDirectoryPath))
                Directory.Delete(game.SetsDirectoryPath, true);
            yield return CardGameManager.Instance.UpdateCardGame(game);
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            Hide();
        }

        [UsedImplicitly]
        public void Hide()
        {
            Menu.Hide();
        }

        // The manager this menu listens to for newly installed games
        private CardGameManager _gamesSource;

        protected void OnDisable()
        {
            if (_gamesSource != null)
                _gamesSource.GamesAdded -= BuildGameSelectionOptions;
            _gamesSource = null;

            InputSystem.actions.FindAction(Tags.DecksNew).performed -= InputNew;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed -= InputLoad;
            InputSystem.actions.FindAction(Tags.SubMenuCopyShare).performed -= InputCopyShare;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed -= InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
