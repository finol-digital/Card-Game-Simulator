/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using System.Linq;
using Cgs.UI;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;
#if !UNITY_WEBGL
using System;
using UnityEngine.Events;
#endif
#if !UNITY_ANDROID && !UNITY_IOS
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

        public GameObject cgsGamesBrowserPrefab;
        public GameObject cardGameEditorMenuPrefab;
        public GameObject gameImportModalPrefab;
        public GameObject downloadMenuPrefab;

        public RectTransform browseButton;
        public RectTransform newButton;
        public RectTransform editButton;
        public RectTransform syncButton;

        protected override bool AllowSwitchOff => false;

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private CgsGamesBrowser CgsGamesBrowserMenu =>
            _cgsGamesBrowser ??= Instantiate(cgsGamesBrowserPrefab).GetOrAddComponent<CgsGamesBrowser>();

        private CgsGamesBrowser _cgsGamesBrowser;

        private CardGameEditorMenu CardGameEditor =>
            _cardGameEditor ??= Instantiate(cardGameEditorMenuPrefab).GetOrAddComponent<CardGameEditorMenu>();

        private CardGameEditorMenu _cardGameEditor;

#if !UNITY_WEBGL
        private DecisionModal ImportModal =>
            _importModal ??= Instantiate(gameImportModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _importModal;
#endif

        private DownloadMenu Downloader => _downloader ??= Instantiate(downloadMenuPrefab)
            .GetOrAddComponent<DownloadMenu>();

        private DownloadMenu _downloader;

#if UNITY_ANDROID || UNITY_IOS
        private static string ZipFileType { get; set; }

        private void Start()
        {
            ZipFileType = NativeFilePicker.ConvertExtensionToFileType("zip");
        }
#endif

        private void OnEnable()
        {
            CardGameManager.Instance.OnSceneActions.Add(BuildGameSelectionOptions);
        }

        private void Update()
        {
            if (!Menu.IsFocused)
                return;

            if (Inputs.IsVertical)
            {
                if (Inputs.IsUp && !Inputs.WasUp)
                    SelectPrevious();
                else if (Inputs.IsDown && !Inputs.WasDown)
                    SelectNext();
            }

            if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Select(toggle => toggle.gameObject)
                    .Contains(EventSystem.current.currentSelectedGameObject))
                EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
            else if (Inputs.IsSubmit)
            {
                if (Settings.DeveloperMode)
                    EditCurrent();
                else
                    Sync();
            }
            else if (Inputs.IsNew)
            {
                if (Settings.DeveloperMode)
                    CreateNew();
                else
                    ShowCgsGamesBrowser();
            }
            else if (Inputs.IsLoad)
                Import();
            else if (Inputs.IsSave)
                Share();
            else if (Inputs.IsOption)
                Delete();
            else if (Inputs.IsPageVertical && !Inputs.WasPageVertical)
                ScrollPage(Inputs.IsPageDown);
            else if (Inputs.IsCancel)
                Hide();
        }

        public void Show()
        {
            Menu.Show();
            browseButton.gameObject.SetActive(!Settings.DeveloperMode);
            newButton.gameObject.SetActive(Settings.DeveloperMode);
            editButton.gameObject.SetActive(Settings.DeveloperMode);
            syncButton.gameObject.SetActive(!Settings.DeveloperMode);
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

        [UsedImplicitly]
        public void ShowCgsGamesBrowser()
        {
            CgsGamesBrowserMenu.Show();
        }

        [UsedImplicitly]
        public void CreateNew()
        {
            CardGameEditor.ShowNew();
        }

        [UsedImplicitly]
        public void EditCurrent()
        {
            CardGameEditor.ShowCurrent();
        }

        [UsedImplicitly]
        public void Import()
        {
#if UNITY_WEBGL
            ShowDownloader();
#else
            ImportModal.Show(ImportGamePrompt, new Tuple<string, UnityAction>(DownloadFromWeb, ShowDownloader),
                new Tuple<string, UnityAction>(LoadFromFile, ShowFileLoader));
#endif
        }

        private void ShowDownloader()
        {
            Downloader.Show(DownloadLabel, DownloadPrompt, CardGameManager.Instance.GetCardGame, true);
        }

        // ReSharper disable once UnusedMember.Local
        private static void ShowFileLoader()
        {
#if UNITY_ANDROID || UNITY_IOS
            NativeFilePicker.PickFile(path =>
            {
                if (path == null)
                    Debug.Log("Operation cancelled");
                else
                    CardGameManager.Instance.ImportCardGame(path);
            }, ZipFileType);
#else
            FileBrowser.ShowLoadDialog((paths) => CardGameManager.Instance.ImportCardGame(paths[0]),
                () => { }, FileBrowser.PickMode.Files, false, null, null,
                SelectZipFilePrompt);
#endif
        }

        [UsedImplicitly]
        public void Share()
        {
            CardGameManager.Instance.Share();
        }

        [UsedImplicitly]
        public void Delete()
        {
            CardGameManager.Instance.PromptDelete();
        }

        [UsedImplicitly]
        public void Sync()
        {
            if (CardGameManager.Current.AutoUpdateUrl?.IsWellFormedOriginalString() ?? false)
            {
                if (File.Exists(CardGameManager.Current.BannerImageFilePath))
                    File.Delete(CardGameManager.Current.BannerImageFilePath);
                if (File.Exists(CardGameManager.Current.CardBackImageFilePath))
                    File.Delete(CardGameManager.Current.CardBackImageFilePath);
                if (File.Exists(CardGameManager.Current.PlayMatImageFilePath))
                    File.Delete(CardGameManager.Current.PlayMatImageFilePath);
                if (Directory.Exists(CardGameManager.Current.SetsDirectoryPath))
                    Directory.Delete(CardGameManager.Current.SetsDirectoryPath, true);
                CardGameManager.Instance.StartCoroutine(
                    CardGameManager.Instance.UpdateCardGame(CardGameManager.Current));
            }
            else
                CardGameManager.Instance.Messenger.Show(NoSyncMessage);

            Hide();
        }

        [UsedImplicitly]
        public void Hide()
        {
            Menu.Hide();
        }
    }
}
