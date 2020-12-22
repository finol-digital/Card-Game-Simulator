/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.Menu;
using JetBrains.Annotations;
using ScrollRects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Decks
{
    public delegate void OnDeckLoadedDelegate(UnityDeck loadedDeck);

    [RequireComponent(typeof(Modal))]
    public class DeckLoadMenu : SelectionPanel
    {
        public const string DecInstructions =
            "//On each line, enter:\n//<Quantity> <Card Name>\n//For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";

        public const string HsdInstructions = "#Paste the deck string/code here";

        public const string LorInstructions = "#Paste the deck string/code here";

        public const string TxtInstructions =
            "### Instructions\n#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";

        public const string YdkInstructions = "#On each line, enter <Card Id>\n#Copy/Paste recommended";

        public const string DeletePrompt = "Are you sure you would like to delete this deck?";
        public const string DeckDeleteErrorMessage = "There was an error while attempting to delete the deck: ";
        public const string DeckLoadErrorMessage = "There was an error while loading the deck: ";
        public const string DeckSaveErrorMessage = "There was an error saving the deck to file: ";

        public Button deleteFileButton;
        public Button shareFileButton;
        public Button loadFromFileButton;

        public RectTransform newDeckPanel;
        public InputField nameInputField;
        public TMP_Text instructionsText;
        public TMP_InputField textInputField;

        private OnDeckLoadedDelegate _loadCallback;

        private string _selectedFilePath;

        private readonly SortedList<string, string> _deckFiles = new SortedList<string, string>();

        private Modal Menu =>
            _menu ? _menu : (_menu = gameObject.GetOrAddComponent<Modal>());

        private Modal _menu;

        private void Update()
        {
            if (!Menu.IsFocused || nameInputField.isFocused || textInputField.isFocused)
                return;

            if (newDeckPanel.gameObject.activeSelf)
            {
                if (Inputs.IsSubmit && EventSystem.current.currentSelectedGameObject == null)
                    DoSaveDontOverwrite();
                else if (Inputs.IsNew && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.text = string.Empty;
                else if (Inputs.IsFocusBack && EventSystem.current.currentSelectedGameObject == null)
                    nameInputField.ActivateInputField();
                else if (Inputs.IsFocusNext && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.ActivateInputField();
                else if (Inputs.IsSave && EventSystem.current.currentSelectedGameObject == null)
                    PasteClipboardIntoText();
                else if (Inputs.IsOption && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.text = string.Empty;
                else if (Inputs.IsCancel)
                    HideNewDeckPanel();
            }
            else
            {
                if (Inputs.IsVertical)
                {
                    if (Inputs.IsUp && !Inputs.WasUp)
                        SelectPrevious();
                    else if (Inputs.IsDown && !Inputs.WasDown)
                        SelectNext();
                }

                if (Inputs.IsSubmit && loadFromFileButton.interactable)
                    LoadFromFileAndHide();
                else if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Select(toggle => toggle.gameObject)
                    .Contains(EventSystem.current.currentSelectedGameObject))
                    EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
                else if (Inputs.IsSort && shareFileButton.interactable)
                    Share();
                else if (Inputs.IsNew)
                    ShowNewDeckPanel();
                else if (Inputs.IsOption && deleteFileButton.interactable)
                    PromptForDeleteFile();
                else if (Inputs.IsPageVertical && !Inputs.WasPageVertical)
                    ScrollPage(Inputs.IsPageDown);
                else if (Inputs.IsCancel)
                    Hide();
            }
        }

        public void Show(OnDeckLoadedDelegate loadCallback = null, string originalName = null,
            string originalText = null)
        {
            Menu.Show();
            _loadCallback = loadCallback;
            _selectedFilePath = string.Empty;

            BuildDeckFileSelectionOptions();

            nameInputField.text = originalName ?? Deck.DefaultName;
            textInputField.text = originalText ?? string.Empty;
            switch (CardGameManager.Current.DeckFileType)
            {
                case DeckFileType.Dec:
                    instructionsText.text = DecInstructions;
                    break;
                case DeckFileType.Hsd:
                    instructionsText.text = HsdInstructions;
                    break;
                case DeckFileType.Lor:
                    instructionsText.text = LorInstructions;
                    break;
                case DeckFileType.Ydk:
                    instructionsText.text = YdkInstructions;
                    break;
                case DeckFileType.Txt:
                default:
                    instructionsText.text = TxtInstructions;
                    break;
            }

            HideNewDeckPanel();
        }

        private void BuildDeckFileSelectionOptions()
        {
            _deckFiles.Clear();
            string[] files = Directory.Exists(CardGameManager.Current.DecksDirectoryPath)
                ? Directory.GetFiles(CardGameManager.Current.DecksDirectoryPath)
                : new string[0];
            foreach (string file in files)
                if (GetFileTypeFromPath(file) == CardGameManager.Current.DeckFileType)
                    _deckFiles[file] = GetNameFromPath(file);

            Rebuild(_deckFiles, SelectFile, _selectedFilePath);

            shareFileButton.interactable = !string.IsNullOrEmpty(_selectedFilePath);
            deleteFileButton.interactable = !string.IsNullOrEmpty(_selectedFilePath);
            loadFromFileButton.interactable = !string.IsNullOrEmpty(_selectedFilePath);
        }

        [UsedImplicitly]
        public void SelectFile(Toggle toggle, string deckFilePath)
        {
            if (string.IsNullOrEmpty(deckFilePath))
            {
                _selectedFilePath = string.Empty;
                shareFileButton.interactable = false;
                deleteFileButton.interactable = false;
                loadFromFileButton.interactable = false;
                return;
            }

            if (toggle != null && toggle.isOn)
            {
                _selectedFilePath = deckFilePath;
                shareFileButton.interactable = true;
                deleteFileButton.interactable = true;
                loadFromFileButton.interactable = true;
            }
            else if (toggle != null && !toggle.group.AnyTogglesOn() && _selectedFilePath.Equals(deckFilePath))
                LoadFromFileAndHide();
        }

        private static string GetNameFromPath(string filePath)
        {
            int startName = filePath.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            int endName = filePath.LastIndexOf('.');
            return filePath.Substring(startName, endName > 0 ? endName - startName : 0);
        }

        private static DeckFileType GetFileTypeFromPath(string filePath)
        {
            var deckFileType = DeckFileType.Txt;
            string extension = filePath.Substring(filePath.LastIndexOf('.') + 1);
            if (extension.ToLower().Equals(DeckFileType.Dec.ToString().ToLower()))
                deckFileType = DeckFileType.Dec;
            else if (extension.ToLower().Equals(DeckFileType.Hsd.ToString().ToLower()))
                deckFileType = DeckFileType.Hsd;
            else if (extension.ToLower().Equals(DeckFileType.Lor.ToString().ToLower()))
                deckFileType = DeckFileType.Lor;
            else if (extension.ToLower().Equals(DeckFileType.Ydk.ToString().ToLower()))
                deckFileType = DeckFileType.Ydk;
            return deckFileType;
        }

        [UsedImplicitly]
        public void PromptForDeleteFile()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, DeleteFile);
        }

        [UsedImplicitly]
        public void DeleteFile()
        {
            try
            {
                File.Delete(_selectedFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError(DeckDeleteErrorMessage + e.Message);
            }

            _selectedFilePath = string.Empty;
            BuildDeckFileSelectionOptions();
        }

        [UsedImplicitly]
        public void Share()
        {
            try
            {
                string shareText = File.ReadAllText(_selectedFilePath);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                (new NativeShare()).SetText(shareText).Share();
#else
                UniClipboard.SetText(shareText);
                CardGameManager.Instance.Messenger.Show(DeckSaveMenu.DeckCopiedMessage);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadErrorMessage + e.Message);
                CardGameManager.Instance.Messenger.Show(DeckLoadErrorMessage + e.Message);
            }
        }

        [UsedImplicitly]
        public void LoadFromFileAndHide()
        {
            try
            {
                string deckText = File.ReadAllText(_selectedFilePath);
                UnityDeck newDeck = UnityDeck.Parse(CardGameManager.Current, _deckFiles[_selectedFilePath],
                    CardGameManager.Current.DeckFileType, deckText);
                _loadCallback?.Invoke(newDeck);
                Hide();
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadErrorMessage + e.Message);
                CardGameManager.Instance.Messenger.Show(DeckLoadErrorMessage + e.Message);
            }
        }

        [UsedImplicitly]
        public void ShowNewDeckPanel()
        {
            newDeckPanel.gameObject.SetActive(true);

            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
                return;

            try
            {
                nameInputField.text = GetNameFromPath(_selectedFilePath);
                textInputField.text = File.ReadAllText(_selectedFilePath);
            }
            catch
            {
                Debug.LogWarning("ShowNewDeckPanel had _selectedFilePath but failed to load it");
            }
        }

        [UsedImplicitly]
        public void ValidateDeckName(string deckName)
        {
            nameInputField.text = UnityFileMethods.GetSafeFileName(deckName);
        }

        [UsedImplicitly]
        public void Clear()
        {
            textInputField.text = string.Empty;
        }

        [UsedImplicitly]
        public void PasteClipboardIntoText()
        {
            textInputField.text = UniClipboard.GetText();
        }

        [UsedImplicitly]
        public void EnableSubmit()
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
        }

        [UsedImplicitly]
        public void DoSaveDontOverwrite()
        {
            var filePathFinder = new UnityDeck(CardGameManager.Current, nameInputField.text,
                CardGameManager.Current.DeckFileType);
            if (File.Exists(filePathFinder.FilePath))
                CardGameManager.Instance.StartCoroutine(WaitToPromptOverwrite());
            else
                DoSave();
        }

        private IEnumerator WaitToPromptOverwrite()
        {
            yield return null;
            CardGameManager.Instance.Messenger.Ask(DeckSaveMenu.OverWriteDeckPrompt, null, DoSave);
        }

        private void DoSave()
        {
            var filePathFinder = new UnityDeck(CardGameManager.Current, nameInputField.text,
                CardGameManager.Current.DeckFileType);
            try
            {
                if (!Directory.Exists(CardGameManager.Current.DecksDirectoryPath))
                    Directory.CreateDirectory(CardGameManager.Current.DecksDirectoryPath);
                File.WriteAllText(filePathFinder.FilePath, textInputField.text);
                _selectedFilePath = filePathFinder.FilePath;

                HideNewDeckPanel();
                BuildDeckFileSelectionOptions();
                foreach (string path in _deckFiles.Keys)
                    Debug.Log(path);

                LoadFromFileAndHide();
            }
            catch (Exception e)
            {
                Debug.LogError(DeckSaveErrorMessage + e.Message);
            }
        }

        [UsedImplicitly]
        public void HideNewDeckPanel()
        {
            newDeckPanel.gameObject.SetActive(false);
        }

        [UsedImplicitly]
        public void Hide()
        {
            Menu.Hide();
        }
    }
}
