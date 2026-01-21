/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
        public const string WebWarningMessage =
 "The CGS web client cannot access files on your device; please use the appropriate CGS native app";
#endif
#if ENABLE_WINMD_SUPPORT
        public const string UwpWarningMessage =
 "The CGS Windows Store client cannot access folders; please use the Steam client or other native app";
#endif

        public const string SelectFolderPrompt = "Select Folder";
        public const string ImportFolderWarningMessage = "No folder found for import! ";
        public const string ImportCardFailedWarningMessage = "Failed to find card: ";
        public const string ImportErrorMessage = "Error during import!: ";

        public string SetImportMissingWarningMessage =>
            $"No images found! Does the selected folder have image files saved as {CardGameManager.Current.CardImageFileType}?";

        public string ImportStatus => $"Importing {SetName}...";

        public Text setNameText;
        public TMP_Text cardNamesText;
        public Dropdown backSelector;
        public Button importButton;

        public float ProgressPercentage { get; private set; }
        public string ProgressStatus { get; private set; }

        private string SetName => FileBrowserHelpers.GetFilename(_setFolderPath);

        private string SetFolderPath
        {
            get => _setFolderPath;
            set
            {
                if (!FileBrowserHelpers.DirectoryExists(value))
                {
                    Debug.LogWarning(ImportFolderWarningMessage + value);
                    CardGameManager.Instance.Messenger.Show(ImportFolderWarningMessage + value);
                    return;
                }

                _setFolderPath = value;

                Debug.Log("Import Set Folder Path set: " + _setFolderPath);
                setNameText.text = SetName;
                var cardNames = FileBrowserHelpers.GetEntriesInDirectory(_setFolderPath, true)
                    .Where(fileSystemEntry =>
                        !fileSystemEntry.IsDirectory && !string.IsNullOrEmpty(fileSystemEntry.Extension)
                                                     && fileSystemEntry.Extension.EndsWith(CardGameManager.Current
                                                         .CardImageFileType))
                    .Aggregate(string.Empty, (current, file) =>
                    {
                        var fileName = file.Name;
                        if (fileName.EndsWith(CardGameManager.Current.CardImageFileType))
                            fileName = fileName[..(fileName.LastIndexOf(CardGameManager.Current.CardImageFileType,
                                StringComparison.Ordinal) - 1)];
                        return current + fileName + "\n";
                    });
                cardNamesText.text = !string.IsNullOrWhiteSpace(cardNames) ? cardNames : SetImportMissingWarningMessage;
                ValidateImportButton();
            }
        }

        private string _setFolderPath;

        [UsedImplicitly] public int Back { get; set; }

        private string BackFace => Back < BackFaceOptions.Count ? BackFaceOptions[Back].text : string.Empty;

        private List<Dropdown.OptionData> BackFaceOptions { get; } = new();

        private string BackFaceId => CardGameManager.Current.CardBackFaceImageSprites.ContainsKey(BackFace)
            ? BackFace
            : string.Empty;

        private UnityAction _onCreationCallback;

        private void OnEnable()
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
            Debug.LogWarning(WebWarningMessage);
            CardGameManager.Instance.Messenger.Show(WebWarningMessage);
#elif ENABLE_WINMD_SUPPORT
            Debug.LogWarning(UwpWarningMessage);
            CardGameManager.Instance.Messenger.Show(UwpWarningMessage);
#else
            FileBrowser.ShowLoadDialog((paths) => { SetFolderPath = paths[0]; }, () => { },
                FileBrowser.PickMode.Folders, false, null, null, SelectFolderPrompt);
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
                CardGameManager.Instance.Messenger.Show("ImportSet::invalid: " + SetFolderPath);
                yield break;
            }

            importButton.interactable = false;
            yield return null;

            var setFilePath = Path.Combine(CardGameManager.Current.SetsDirectoryPath, SetName);
            if (!Directory.Exists(setFilePath))
                Directory.CreateDirectory(setFilePath);

            CardGameManager.Instance.Progress.Show(this);
            ProgressStatus = ImportStatus;

            var cardPathsToImport = FileBrowserHelpers.GetEntriesInDirectory(_setFolderPath, true)
                .Where(fileSystemEntry => !fileSystemEntry.IsDirectory &&
                                          !string.IsNullOrEmpty(fileSystemEntry.Extension) &&
                                          fileSystemEntry.Extension.EndsWith(CardGameManager.Current.CardImageFileType))
                .ToList();
            var cardCount = cardPathsToImport.Count;
            for (var i = 0; i < cardCount; i++)
            {
                try
                {
                    ProgressPercentage = (float)i / cardCount;
                    var fileName = cardPathsToImport[i].Name;
                    var end = fileName.LastIndexOf(CardGameManager.Current.CardImageFileType,
                        StringComparison.Ordinal);
                    var cardName = fileName[..(end - 1)];
                    var cardId = UnityFileMethods.GetSafeFileName(cardName).Replace(" ", "_");
                    var card = new UnityCard(CardGameManager.Current, cardId, cardName, SetName, null, false, false,
                        BackFaceId);
                    FileBrowserHelpers.CopyFile(cardPathsToImport[i].Path, card.ImageFilePath);

                    if (!File.Exists(card.ImageFilePath))
                    {
                        Debug.LogWarning(ImportCardFailedWarningMessage + card.Name);
                        CardGameManager.Instance.Messenger.Show(ImportCardFailedWarningMessage + card.Name);
                    }
                    else
                    {
                        var fileInfo = new FileInfo(card.ImageFilePath);
                        if (fileInfo.Exists && fileInfo.Length > ImageQueueService.MaxImageFileSizeBytes)
                        {
                            var sizeWarningMessage =
                                string.Format(ImageQueueService.SizeWarningMessage, card.Name, card.Id);
                            Debug.LogWarning(sizeWarningMessage);
                            CardGameManager.Instance.Messenger.Show(sizeWarningMessage, true);
                        }

                        CardGameManager.Current.Add(card, false);
                    }
                }
                catch
                {
                    Debug.LogWarning(ImportCardFailedWarningMessage + cardPathsToImport[i]);
                    CardGameManager.Instance.Messenger.Show(ImportCardFailedWarningMessage + cardPathsToImport[i]);
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
                CardGameManager.Instance.Messenger.Show(ImportErrorMessage + e);
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

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.DecksSave).performed -= InputSelectFolder;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
