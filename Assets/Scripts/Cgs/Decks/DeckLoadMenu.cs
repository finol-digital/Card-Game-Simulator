/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

using CardGameDef;
using Cgs.Menu;
using ScrollRects;

namespace Cgs.Decks
{
    public delegate void OnDeckLoadedDelegate(Deck loadedDeck);

    [RequireComponent(typeof(Modal))]
    public class DeckLoadMenu : SelectionPanel
    {
        public const string DecInstructions = "//On each line, enter:\n//<Quantity> <Card Name>\n//For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
        public const string HsdInstructions = "#Paste the deck string/code here";
        public const string TxtInstructions = "### Instructions\n#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
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

        public OnDeckLoadedDelegate LoadCallback { get; private set; }
        public string SelectedFilePath { get; private set; }
        public SortedList<string, string> DeckFiles { get; } = new SortedList<string, string>();

        private bool _wasDown;
        private bool _wasUp;
        private bool _wasPage;

        private Modal _modal;

        void Start()
        {
            _modal = GetComponent<Modal>();
        }

        void Update()
        {
            if (!_modal.IsFocused || nameInputField.isFocused)
                return;

            if (newDeckPanel.gameObject.activeSelf)
            {
                if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && EventSystem.current.currentSelectedGameObject == null)
                    DoSaveDontOverwrite();
                else if (Input.GetButtonDown(Inputs.New) && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.text = string.Empty;
                else if ((Input.GetButtonDown(Inputs.FocusBack) || Input.GetAxis(Inputs.FocusBack) != 0) && EventSystem.current.currentSelectedGameObject == null)
                    nameInputField.ActivateInputField();
                else if ((Input.GetButtonDown(Inputs.FocusNext) || Input.GetAxis(Inputs.FocusNext) != 0) && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.ActivateInputField();
                else if (Input.GetButtonDown(Inputs.Save) && EventSystem.current.currentSelectedGameObject == null)
                    PasteClipboardIntoText();
                else if (Input.GetButtonDown(Inputs.Option) && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.text = string.Empty;
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                    HideNewDeckPanel();
            }
            else
            {
                if (Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
                {
                    if (Input.GetAxis(Inputs.Vertical) > 0 && !_wasUp)
                        SelectPrevious();
                    else if (Input.GetAxis(Inputs.Vertical) < 0 && !_wasDown)
                        SelectNext();
                }

                if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && loadFromFileButton.interactable)
                    LoadFromFileAndHide();
                else if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Select(toggle => toggle.gameObject).Contains(EventSystem.current.currentSelectedGameObject))
                    EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
                else if (Input.GetButtonDown(Inputs.Sort) && shareFileButton.interactable)
                    Share();
                else if (Input.GetButtonDown(Inputs.New))
                    ShowNewDeckPanel();
                else if (Input.GetButtonDown(Inputs.Option) && deleteFileButton.interactable)
                    PromptForDeleteFile();
                else if ((Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0) && !_wasPage)
                    ScrollPage(Input.GetAxis(Inputs.PageVertical));
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                    Hide();
            }

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasPage = Input.GetAxis(Inputs.PageVertical) != 0;
        }

        public void Show(OnDeckLoadedDelegate loadCallback = null, string originalName = null, string originalText = null)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            LoadCallback = loadCallback;
            SelectedFilePath = string.Empty;

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

        public void BuildDeckFileSelectionOptions()
        {
            DeckFiles.Clear();
            string[] files = Directory.Exists(CardGameManager.Current.DecksFilePath) ? Directory.GetFiles(CardGameManager.Current.DecksFilePath) : new string[0];
            foreach (string file in files)
                if (GetFileTypeFromPath(file) == CardGameManager.Current.DeckFileType)
                    DeckFiles[file] = GetNameFromPath((file));

            Rebuild<string, string>(DeckFiles, SelectFile, SelectedFilePath);

            shareFileButton.interactable = !string.IsNullOrEmpty(SelectedFilePath);
            deleteFileButton.interactable = !string.IsNullOrEmpty(SelectedFilePath);
            loadFromFileButton.interactable = !string.IsNullOrEmpty(SelectedFilePath);
        }

        public void SelectFile(Toggle toggle, string deckFilePath)
        {
            if (string.IsNullOrEmpty(deckFilePath))
            {
                SelectedFilePath = string.Empty;
                shareFileButton.interactable = false;
                deleteFileButton.interactable = false;
                loadFromFileButton.interactable = false;
                return;
            }

            if (toggle.isOn)
            {
                SelectedFilePath = deckFilePath;
                shareFileButton.interactable = true;
                deleteFileButton.interactable = true;
                loadFromFileButton.interactable = true;
            }
            else if (!toggle.group.AnyTogglesOn() && SelectedFilePath.Equals(deckFilePath))
                LoadFromFileAndHide();
        }

        public string GetNameFromPath(string filePath)
        {
            int startName = filePath.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            int endName = filePath.LastIndexOf('.');
            return filePath.Substring(startName, endName > 0 ? endName - startName : 0);
        }

        public DeckFileType GetFileTypeFromPath(string filePath)
        {
            DeckFileType fileType = DeckFileType.Txt;
            string extension = filePath.Substring(filePath.LastIndexOf('.') + 1);
            if (extension.ToLower().Equals(DeckFileType.Dec.ToString().ToLower()))
                fileType = DeckFileType.Dec;
            else if (extension.ToLower().Equals(DeckFileType.Hsd.ToString().ToLower()))
                fileType = DeckFileType.Hsd;
            else if (extension.ToLower().Equals(DeckFileType.Ydk.ToString().ToLower()))
                fileType = DeckFileType.Ydk;
            return fileType;
        }

        public void PromptForDeleteFile()
        {
            CardGameManager.Instance.Messenger.Prompt(DeletePrompt, DeleteFile);
        }

        public void DeleteFile()
        {
            try
            {
                File.Delete(SelectedFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError(DeckDeleteErrorMessage + e.Message);
            }
            SelectedFilePath = string.Empty;
            BuildDeckFileSelectionOptions();
        }

        public void Share()
        {
            string shareText = GetDeckText();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            (new NativeShare()).SetText(shareText).Share();
#else
            UniClipboard.SetText(shareText);
            CardGameManager.Instance.Messenger.Show(DeckSaveMenu.DeckCopiedMessage);
#endif
        }

        public string GetDeckText()
        {
            string deckText = string.Empty;
            try
            {
                deckText = File.ReadAllText(SelectedFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadErrorMessage + e.Message);
            }
            return deckText;
        }

        public void LoadFromFileAndHide()
        {
            Deck newDeck = Deck.Parse(CardGameManager.Current, DeckFiles[SelectedFilePath], CardGameManager.Current.DeckFileType, GetDeckText());
            LoadCallback?.Invoke(newDeck);
            Hide();
        }

        public void ShowNewDeckPanel()
        {
            newDeckPanel.gameObject.SetActive(true);
        }

        public void ValidateDeckName(string deckName)
        {
            nameInputField.text = UnityExtensionMethods.GetSafeFileName(deckName);
        }

        public void Clear()
        {
            textInputField.text = string.Empty;
        }

        public void PasteClipboardIntoText()
        {
            textInputField.text = UniClipboard.GetText();
        }

        public void EnableSubmit()
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public void DoSaveDontOverwrite()
        {
            Deck filePathFinder = new Deck(CardGameManager.Current, nameInputField.text, CardGameManager.Current.DeckFileType);
            if (File.Exists(filePathFinder.FilePath))
                CardGameManager.Instance.StartCoroutine(WaitToPromptOverwrite());
            else
                DoSave();
        }

        public IEnumerator WaitToPromptOverwrite()
        {
            yield return null;
            CardGameManager.Instance.Messenger.Ask(DeckSaveMenu.OverWriteDeckPrompt, null, DoSave);
        }

        public void DoSave()
        {
            Deck filePathFinder = new Deck(CardGameManager.Current, nameInputField.text, CardGameManager.Current.DeckFileType);
            try
            {
                if (!Directory.Exists(CardGameManager.Current.DecksFilePath))
                    Directory.CreateDirectory(CardGameManager.Current.DecksFilePath);
                File.WriteAllText(filePathFinder.FilePath, textInputField.text);
            }
            catch (Exception e)
            {
                Debug.LogError(DeckSaveErrorMessage + e.Message);
            }
            BuildDeckFileSelectionOptions();
            HideNewDeckPanel();
        }

        public void HideNewDeckPanel()
        {
            newDeckPanel.gameObject.SetActive(false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
