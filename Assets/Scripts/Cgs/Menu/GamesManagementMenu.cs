/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using JetBrains.Annotations;
using SimpleFileBrowser;
using UnityEngine;
using UnityEngine.Events;
using UnityExtensionMethods;

namespace Cgs.Menu
{
    public class GamesManagementMenu : Modal
    {
        public const string ImportGamePrompt = "Download from Web URL,\n or Load from ZIP File?";
        public const string DownloadFromWeb = "Download from Web URL";
        public const string LoadFromFile = "Load from ZIP File";
        public const string SelectZipFilePrompt = "Select ZIP File";

        public const string DownloadLabel = "Download Game";
        public const string DownloadPrompt = "Enter CGS AutoUpdate URL...";

        public GameObject cardGameEditorMenuPrefab;
        public GameObject gameImportModalPrefab;
        public GameObject downloadMenuPrefab;

        private CardGameEditorMenu CardGameEditor =>
            _cardGameEditor ??= Instantiate(cardGameEditorMenuPrefab).GetOrAddComponent<CardGameEditorMenu>();

        private CardGameEditorMenu _cardGameEditor;

        private DecisionModal ImportModal =>
            _importModal ??= Instantiate(gameImportModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _importModal;

        private DownloadMenu Downloader => _downloader ??= Instantiate(downloadMenuPrefab)
            .GetOrAddComponent<DownloadMenu>();

        private DownloadMenu _downloader;

#if UNITY_ANDROID || UNITY_IOS
        private static string _zipFileType;
#endif

        protected override void Start()
        {
            base.Start();
#if UNITY_ANDROID || UNITY_IOS
            _zipFileType = NativeFilePicker.ConvertExtensionToFileType("zip");
#endif
        }

        [UsedImplicitly]
        public void CreateNew()
        {
            CardGameEditor.Show();
        }

        [UsedImplicitly]
        public void Import()
        {
            ImportModal.Show(ImportGamePrompt, new Tuple<string, UnityAction>(DownloadFromWeb, ShowDownloader),
                new Tuple<string, UnityAction>(LoadFromFile, ShowFileLoader));
        }

        private void ShowDownloader()
        {
            Downloader.Show(DownloadLabel, DownloadPrompt, CardGameManager.Instance.GetCardGame, true);
        }

        private static void ShowFileLoader()
        {
#if UNITY_ANDROID || UNITY_IOS
            var permission = NativeFilePicker.PickFile(path =>
            {
                if (path == null)
                    Debug.Log("Operation cancelled");
                else
                    CardGameManager.Instance.ImportCardGame(path);
            }, _zipFileType);
            Debug.Log( "Permission result: " + permission );
#else
            FileBrowser.ShowLoadDialog((paths) => CardGameManager.Instance.ImportCardGame(paths[0]),
                () => { }, FileBrowser.PickMode.Files, false, null, null,
                SelectZipFilePrompt);
#endif
        }

        [UsedImplicitly]
        public void Sync()
        {
            CardGameManager.Instance.StartCoroutine(CardGameManager.Instance.UpdateCardGame(CardGameManager.Current));
        }

        [UsedImplicitly]
        public void Edit()
        {
            CardGameManager.Instance.Messenger.Show("Edit is Coming Soon!");
        }

        [UsedImplicitly]
        public void Delete()
        {
            CardGameManager.Instance.PromptDelete();
        }

        [UsedImplicitly]
        public void Share()
        {
            CardGameManager.Instance.Share();
        }
    }
}
