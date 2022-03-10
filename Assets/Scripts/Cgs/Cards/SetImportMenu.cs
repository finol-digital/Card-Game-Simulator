/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using System.Linq;
using CardGameDef.Unity;
using Cgs.Menu;
using JetBrains.Annotations;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class SetImportMenu : Modal, IProgressible
    {
#if ENABLE_WINMD_SUPPORT
        public const string PlatformWarningMessage = "Sorry, Set Import is not supported from Windows Store!";
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
                            fileName = fileName.Substring(0,
                                fileName.LastIndexOf(CardGameManager.Current.CardImageFileType,
                                    StringComparison.Ordinal) - 1);
                        return current + fileName + "\n";
                    });
                cardNamesText.text = !string.IsNullOrWhiteSpace(cardNames) ? cardNames : SetImportMissingWarningMessage;
                ValidateImportButton();
            }
        }

        private string _setFolderPath;

        private UnityAction _onCreationCallback;

        public void Show(UnityAction onCreationCallback)
        {
            Show();
            _onCreationCallback = onCreationCallback;
        }

        private void Update()
        {
            if (!IsFocused)
                return;

            if ((Inputs.IsSubmit || Inputs.IsSave) && importButton.interactable)
                StartImport();
            if ((Inputs.IsNew || Inputs.IsLoad) && importButton.interactable)
                SelectFolder();
            else if (Inputs.IsCancel || Inputs.IsOption)
                Hide();
        }

        [UsedImplicitly]
        public void SelectFolder()
        {
#if ENABLE_WINMD_SUPPORT
            CardGameManager.Instance.Messenger.Show(PlatformWarningMessage);
            Hide();
#else
            FileBrowser.ShowLoadDialog((paths) => { SetFolderPath = paths[0]; },
                () => { Debug.Log("FileBrowser Canceled"); }, FileBrowser.PickMode.Folders, false, null, null,
                SelectFolderPrompt);
#endif
        }

        private void ValidateImportButton()
        {
            importButton.interactable = FileBrowserHelpers.DirectoryExists(SetFolderPath);
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
                    ProgressPercentage = (float) i / cardCount;
                    var fileName = cardPathsToImport[i].Name;
                    var end = fileName.LastIndexOf(CardGameManager.Current.CardImageFileType,
                        StringComparison.Ordinal);
                    var cardName = fileName.Substring(0, end - 1);
                    var card = new UnityCard(CardGameManager.Current, cardName, cardName, SetName, null, false);
                    FileBrowserHelpers.CopyFile(cardPathsToImport[i].Path, card.ImageFilePath);

                    if (!File.Exists(card.ImageFilePath))
                    {
                        Debug.LogWarning(ImportCardFailedWarningMessage + card.Name);
                        CardGameManager.Instance.Messenger.Show(ImportCardFailedWarningMessage + card.Name);
                    }
                    else
                        CardGameManager.Current.Add(card, false);
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
    }
}
