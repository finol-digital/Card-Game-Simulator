/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cgs.Menu;
using Cgs.UI;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Decks
{
    public delegate void OnDeckLoadedDelegate(UnityDeck loadedDeck);

    [RequireComponent(typeof(Modal))]
    public class DeckLoadMenu : SelectionPanel
    {
        public const string Untitled = "Untitled";

        public const string DecInstructions =
            "//On each line, enter:\n//<Quantity> <Card Name>\n//For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";

        public const string HsdInstructions = "#Paste the deck string/code here";

        public const string LorInstructions = "#Paste the deck string/code here";

        public const string TxtInstructions =
            "### Instructions\n#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";

        public const string YdkInstructions = "#On each line, enter <Card Id>\n#Copy/Paste recommended";

        public const string DeletePrompt = "Are you sure you would like to delete this deck?";
        public const string ClearPrompt = "Clear the deck text?";
        public const string DeckDeleteErrorMessage = "There was an error while attempting to delete the deck: ";
        public const string DeckLoadErrorMessage = "There was an error while loading the deck: ";
        public const string DeckSaveErrorMessage = "There was an error saving the deck to file: ";

        public Button deleteFileButton;
        public Button editFileButton;
        public Button shareFileButton;
        public Button loadFromFileButton;

        public RectTransform newDeckPanel;
        public InputField nameInputField;
        public TMP_Text instructionsText;
        public TMP_InputField textInputField;

        private OnDeckLoadedDelegate _loadCallback;

        private string _selectedFilePath;

        private readonly SortedList<string, string> _deckFiles = new();

        private Modal Menu => _menu ??= gameObject.GetOrAddComponent<Modal>();

        private Modal _menu;

        private bool IsBlocked => Menu.IsBlocked || nameInputField.isFocused || textInputField.isFocused;

        private InputAction _moveAction;
        private InputAction _pageAction;
        private InputAction _shiftAction;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.DecksNew).performed += InputDecksNew;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed += InputDecksLoad;
            InputSystem.actions.FindAction(Tags.SubMenuCopyShare).performed += InputCopyShare;
            InputSystem.actions.FindAction(Tags.SubMenuFocusPrevious).performed += InputFocusName;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed += InputFocusText;
            InputSystem.actions.FindAction(Tags.SubMenuPaste).performed += InputPaste;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed += InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        private void Start()
        {
            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
            _shiftAction = InputSystem.actions.FindAction(Tags.SubMenuShift);
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            if (IsBlocked || newDeckPanel.gameObject.activeSelf)
                return;

            if (_moveAction?.WasPressedThisFrame() ?? false)
            {
                var moveVertical = _moveAction.ReadValue<Vector2>().y;
                switch (moveVertical)
                {
                    case > 0:
                        SelectPrevious();
                        break;
                    case < 0:
                        SelectNext();
                        break;
                }
            }
            else if (_pageAction?.WasPressedThisFrame() ?? false)
            {
                var pageVertical = _pageAction.ReadValue<Vector2>().y;
                if (Mathf.Abs(pageVertical) > 0)
                    ScrollPage(pageVertical < 0);
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
            var filePaths = Directory.Exists(CardGameManager.Current.DecksDirectoryPath)
                ? Directory.GetFiles(CardGameManager.Current.DecksDirectoryPath)
                : Array.Empty<string>();
            foreach (var filePath in filePaths)
                if (GetFileTypeFromPath(filePath) == CardGameManager.Current.DeckFileType)
                    _deckFiles[filePath] = GetNameFromPath(filePath);

            Rebuild(_deckFiles, SelectFile, _selectedFilePath);

            editFileButton.interactable = !string.IsNullOrEmpty(_selectedFilePath);
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
                editFileButton.interactable = false;
                shareFileButton.interactable = false;
                deleteFileButton.interactable = false;
                loadFromFileButton.interactable = false;
                return;
            }

            if (toggle != null && toggle.isOn)
            {
                _selectedFilePath = deckFilePath;
                editFileButton.interactable = true;
                shareFileButton.interactable = true;
                deleteFileButton.interactable = true;
                loadFromFileButton.interactable = true;
            }
            else if (toggle != null && !toggle.group.AnyTogglesOn() && _selectedFilePath.Equals(deckFilePath))
                LoadFromFileAndHide();
        }

        private static string GetNameFromPath(string filePath)
        {
            var startNameIndex = filePath.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            var endNameIndex = filePath.LastIndexOf('.');
            return filePath.Substring(startNameIndex, endNameIndex > 0 ? endNameIndex - startNameIndex : 0);
        }

        private static DeckFileType GetFileTypeFromPath(string filePath)
        {
            var deckFileType = DeckFileType.Txt;
            var extension = filePath[(filePath.LastIndexOf('.') + 1)..];
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

        private void InputDelete(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (!newDeckPanel.gameObject.activeSelf && deleteFileButton.gameObject.activeSelf)
                PromptForDeleteFile();
            else if (newDeckPanel.gameObject.activeSelf && EventSystem.current.currentSelectedGameObject == null)
                PromptForClear();
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
                Debug.LogError(DeckDeleteErrorMessage + e);
            }

            _selectedFilePath = string.Empty;
            BuildDeckFileSelectionOptions();
        }

        [UsedImplicitly]
        public void Edit()
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                Debug.LogError("Edit missing _selectedFilePath!");
                return;
            }

            newDeckPanel.gameObject.SetActive(true);

            try
            {
                nameInputField.text = GetNameFromPath(_selectedFilePath);
                textInputField.text = File.ReadAllText(_selectedFilePath);
            }
            catch
            {
                Debug.LogWarning("Edit had _selectedFilePath but failed to load it");
            }
        }

        private void InputCopyShare(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (!newDeckPanel.gameObject.activeSelf && shareFileButton.gameObject.activeSelf)
                Share();
        }

        [UsedImplicitly]
        public void Share()
        {
            try
            {
                var shareText = File.ReadAllText(_selectedFilePath);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                (new NativeShare()).SetText(shareText).Share();
#else
                UniClipboard.SetText(shareText);
                CardGameManager.Instance.Messenger.Show(DeckSaveMenu.DeckCopiedMessage);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadErrorMessage + e);
                CardGameManager.Instance.Messenger.Show(DeckLoadErrorMessage + e.Message);
            }
        }

        private void InputDecksLoad(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (!newDeckPanel.gameObject.activeSelf && loadFromFileButton.gameObject.activeSelf)
                LoadFromFileAndHide();
        }

        [UsedImplicitly]
        public void LoadFromFileAndHide()
        {
            try
            {
                var deckText = File.ReadAllText(_selectedFilePath);
                var newDeck = UnityDeck.Parse(CardGameManager.Current, _deckFiles[_selectedFilePath],
                    CardGameManager.Current.DeckFileType, deckText);
                _loadCallback?.Invoke(newDeck);
                Hide();
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadErrorMessage + e);
                CardGameManager.Instance.Messenger.Show(DeckLoadErrorMessage + e.Message);
            }
        }

        private void InputDecksNew(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (!newDeckPanel.gameObject.activeSelf)
                ShowNewDeckPanel();
        }

        [UsedImplicitly]
        public void ShowNewDeckPanel()
        {
            newDeckPanel.gameObject.SetActive(true);
            nameInputField.text = Untitled;
            textInputField.text = string.Empty;
        }

        private void InputFocusName(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked)
                return;

            if (newDeckPanel.gameObject.activeSelf)
                nameInputField.ActivateInputField();
        }

        [UsedImplicitly]
        public void ValidateDeckName(string deckName)
        {
            nameInputField.text = UnityFileMethods.GetSafeFileName(deckName);
        }

        private void InputFocusText(InputAction.CallbackContext callbackContext)
        {
            if (Menu.IsBlocked || _shiftAction?.ReadValue<float>() > 0.9f)
                return;

            if (newDeckPanel.gameObject.activeSelf)
                textInputField.ActivateInputField();
        }

        [UsedImplicitly]
        public void PromptForClear()
        {
            CardGameManager.Instance.Messenger.Prompt(ClearPrompt, Clear);
        }

        private void Clear()
        {
            textInputField.text = string.Empty;
        }

        private void InputPaste(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (newDeckPanel.gameObject.activeSelf && EventSystem.current.currentSelectedGameObject == null)
                PasteClipboardIntoText();
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

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            switch (newDeckPanel.gameObject.activeSelf)
            {
                case true when EventSystem.current.currentSelectedGameObject == null:
                    DoSaveDontOverwrite();
                    break;
                case false when loadFromFileButton.interactable:
                    LoadFromFileAndHide();
                    break;
            }
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
                foreach (var path in _deckFiles.Keys)
                    Debug.Log(path);

                LoadFromFileAndHide();
            }
            catch (Exception e)
            {
                Debug.LogError(DeckSaveErrorMessage + e);
            }
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (newDeckPanel.gameObject.activeSelf)
                HideNewDeckPanel();
            else
                Hide();
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

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.DecksNew).performed -= InputDecksNew;
            InputSystem.actions.FindAction(Tags.DecksLoad).performed -= InputDecksLoad;
            InputSystem.actions.FindAction(Tags.SubMenuCopyShare).performed -= InputCopyShare;
            InputSystem.actions.FindAction(Tags.SubMenuFocusPrevious).performed -= InputFocusName;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed -= InputFocusText;
            InputSystem.actions.FindAction(Tags.SubMenuPaste).performed -= InputPaste;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed -= InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
