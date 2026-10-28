/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs.Menu;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class SetImportMenu : Modal, IProgressible
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private Action _releaseBrowserFolder;
#endif
#if ENABLE_WINMD_SUPPORT
        public const string UwpWarningMessage =
 "The CGS Windows Store client cannot access folders; please use the Steam client or other native app";
#endif

        public const string SelectFolderPrompt = "Select Folder";
        public const string ImportFolderWarningMessage = "No folder found for import! ";
        public const string ImportCardFailedWarningMessage = "Failed to find card: ";
        public const string ImportErrorMessage = "Error during import!: ";
        public const string DuplicateCardIdErrorMessage = "A card with the same ID already exists: ";

        public string SetImportMissingWarningMessage =>
            $"No images found! Does the selected folder have image files saved as {CardGameManager.Current.CardImageFileType}?";

        public string ImportStatus => $"Importing {SetName}...";

        [SerializeField] Text setNameText;
        [SerializeField] TMP_Text cardNamesText;
        [SerializeField] Dropdown backSelector;
        [SerializeField] Button importButton;

        public float ProgressPercentage { get; private set; }
        public string ProgressStatus { get; private set; }
        public UiMessage LocalizedProgressStatus { get; private set; }

        private string SetName => FileBrowserHelpers.GetFilename(_setFolderPath);

        private string SetFolderPath
        {
            get => _setFolderPath;
            set
            {
                if (!FileBrowserHelpers.DirectoryExists(value))
                {
                    Debug.LogWarning(ImportFolderWarningMessage + value);
                    CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.folder.missing", ImportFolderWarningMessage + value, ("detail", value)));
                    return;
                }

                _setFolderPath = value;

                Debug.Log("Import Set Folder Path set: " + _setFolderPath);
                Cgs.Localization.LocalizedUiText.SetLiteral(setNameText, SetName);
                var cardNames = string.Join("\n", GetCardFilesToImport()
                    .Select(file => Path.GetFileNameWithoutExtension(file.Name)));
                if (!string.IsNullOrWhiteSpace(cardNames))
                    LocalizedUiText.SetLiteral(cardNamesText, cardNames);
                else
                    LocalizedUiText.Set(cardNamesText, UiMessage.With("sets.images.missing", SetImportMissingWarningMessage,
                        ("extension", CardGameManager.Current.CardImageFileType)));
                ValidateImportButton();
            }
        }

        private string _setFolderPath;

        private IEnumerable<FileSystemEntry> GetCardFilesToImport()
        {
            var extension = "." + CardGameManager.Current.CardImageFileType;
            return FileBrowserHelpers.GetEntriesInDirectory(_setFolderPath, true)
                .Where(file => !file.IsDirectory &&
                               string.Equals(file.Extension, extension, StringComparison.OrdinalIgnoreCase));
        }

        [UsedImplicitly] public int Back { get; set; }

        private string BackFace => Back < BackFaceOptions.Count ? BackFaceOptions[Back].text : string.Empty;

        private List<Dropdown.OptionData> BackFaceOptions { get; } = new();

        private string BackFaceId => CardGameManager.Current.CardBackFaceImageSprites.ContainsKey(BackFace)
            ? BackFace
            : string.Empty;

        private UnityAction _onCreationCallback;

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.DecksSave).performed += InputSelectFolder;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        public void Show(UnityAction onCreationCallback)
        {
            Show();

            BackFaceOptions.Clear();
            BackFaceOptions.Add(new Dropdown.OptionData() { text = string.Empty });
            foreach (var backFaceKey in CardGameManager.Current.CardBackFaceImageSprites.Keys)
                BackFaceOptions.Add(new Dropdown.OptionData() { text = backFaceKey });
            backSelector.options = BackFaceOptions;
            backSelector.value = 0;

            _onCreationCallback = onCreationCallback;
        }

        private void InputSelectFolder(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (importButton.interactable)
                SelectFolder();
        }

        [UsedImplicitly]
        public void SelectFolder()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BrowserFileBrowser.OpenFolder(new UiMessage("files.select.folder", SelectFolderPrompt), (path, release) =>
            {
                if (this == null || !isActiveAndEnabled)
                {
                    release();
                    return;
                }
                _releaseBrowserFolder?.Invoke();
                _releaseBrowserFolder = release;
                SetFolderPath = path;
            });
#elif ENABLE_WINMD_SUPPORT
            Debug.LogWarning(UwpWarningMessage);
            CardGameManager.Instance.Messenger.Show(new UiMessage("files.uwp.unavailable", UwpWarningMessage));
#else
            FileBrowser.ShowLoadDialog((paths) => { SetFolderPath = paths[0]; }, () => { },
                FileBrowser.PickMode.Folders, false, null, null, CgsLocalization.Text(new UiMessage("files.select.folder", SelectFolderPrompt)));
#endif
        }

        private void ValidateImportButton()
        {
            importButton.interactable = FileBrowserHelpers.DirectoryExists(SetFolderPath);
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (importButton.interactable)
                StartImport();
        }

        [UsedImplicitly]
        public void StartImport()
        {
            Debug.Log("Start Set Import: " + SetFolderPath);
            StartCoroutine(ImportSet());
        }

        private IEnumerator ImportSet()
        {
            ValidateImportButton();
            if (!importButton.interactable)
            {
                Debug.LogError("ImportSet::invalid: " + SetFolderPath);
                CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.folder.missing", "ImportSet::invalid: " + SetFolderPath, ("detail", SetFolderPath)));
                yield break;
            }

            importButton.interactable = false;
            yield return null;

            var setFilePath = Path.Combine(CardGameManager.Current.SetsDirectoryPath, SetName);
            if (!Directory.Exists(setFilePath))
                Directory.CreateDirectory(setFilePath);

            CardGameManager.Instance.Progress.Show(this);
            ProgressStatus = ImportStatus;
            LocalizedProgressStatus = UiMessage.With("sets.import.progress", ImportStatus, ("setName", SetName));

            var cardPathsToImport = GetCardFilesToImport().ToList();
            var cardCount = cardPathsToImport.Count;
            for (var i = 0; i < cardCount; i++)
            {
                try
                {
                    ProgressPercentage = (float)i / cardCount;
                    var fileName = cardPathsToImport[i].Name;
                    var cardName = Path.GetFileNameWithoutExtension(fileName);
                    var cardId = UnityFileMethods.GetSafeFileName(cardName).Replace(" ", "_");
                    if (CardGameManager.Current.Cards.ContainsKey(cardId))
                        throw new InvalidOperationException(DuplicateCardIdErrorMessage + cardId);

                    var card = new UnityCard(CardGameManager.Current, cardId, cardName, SetName, null, false, false,
                        BackFaceId);
                    FileBrowserHelpers.CopyFile(cardPathsToImport[i].Path, card.ImageFilePath);

                    if (!File.Exists(card.ImageFilePath))
                    {
                        Debug.LogWarning(ImportCardFailedWarningMessage + card.Name);
                        CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.card.missing", ImportCardFailedWarningMessage + card.Name, ("detail", card.Name)));
                    }
                    else
                    {
                        var fileInfo = new FileInfo(card.ImageFilePath);
                        if (fileInfo.Exists && fileInfo.Length > ImageQueueService.MaxImageFileSizeBytes)
                        {
                            var sizeWarningMessage =
                                string.Format(ImageQueueService.SizeWarningMessage, card.Name, card.Id);
                            Debug.LogWarning(sizeWarningMessage);
                            CardGameManager.Instance.Messenger.Show(UiMessage.With("images.size.warning", sizeWarningMessage, ("name", card.Name), ("id", card.Id)), true);
                        }

                        CardGameManager.Current.Add(card, false);
                    }
                }
                catch (Exception e)
                {
                    var file = cardPathsToImport[i];
                    Debug.LogWarning(ImportErrorMessage + file.Path + "\n" + e);
                    CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.card.missing",
                        ImportCardFailedWarningMessage + file.Name, ("detail", file.Name)));
                }

                yield return null;
            }

            try
            {
                CardGameManager.Current.WriteAllCardsJson();
            }
            catch (Exception e)
            {
                Debug.LogError(ImportErrorMessage + e);
                CardGameManager.Instance.Messenger.Show(UiMessage.With("errors.import", ImportErrorMessage + e, ("detail", e)));
            }

            CardGameManager.Instance.Progress.Hide();
            _onCreationCallback?.Invoke();

            ValidateImportButton();
            Hide();
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Hide();
        }

        protected void OnDisable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _releaseBrowserFolder?.Invoke();
            _releaseBrowserFolder = null;
            _setFolderPath = null;
            LocalizedUiText.SetLiteral(setNameText, string.Empty);
            LocalizedUiText.SetLiteral(cardNamesText, string.Empty);
            importButton.interactable = false;
#endif
            InputSystem.actions.FindAction(Tags.DecksSave).performed -= InputSelectFolder;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
