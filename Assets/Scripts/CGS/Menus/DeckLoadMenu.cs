/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGameDef;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CGS.Menus
{
    public delegate void OnDeckLoadedDelegate(Deck loadedDeck);

    public class DeckLoadMenu : SelectionPanel
    {
        public const string DecInstructions = "//On each line, enter:\n//<Quantity> <Card Name>\n//For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
        public const string HsdInstructions = "#Paste the deck string/code here";
        public const string TxtInstructions = "### Instructions\n#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
        public const string YdkInstructions = "#On each line, enter <Card Id>\n#Copy/Paste recommended";

        public const string DeletePrompt = "Are you sure you would like to delete this deck?";
        public const string DeckDeleteErrorMessage = "There was an error while attempting to delete the deck: ";
        public const string ShareMessage = "Deck text copied to clipboard";
        public const string DeckLoadErrorMessage = "There was an error while loading the deck: ";
        public const string DeckSaveErrorMessage = "There was an error saving the deck to file: ";

        public Button deleteFileButton;
        public Button shareFileButton;
        public Button loadFromFileButton;

        public RectTransform newDeckPanel;
        public InputField nameInputField;
        public TMPro.TextMeshProUGUI instructionsText;
        public TMPro.TMP_InputField textInputField;

        public OnDeckLoadedDelegate LoadCallback { get; private set; }
        public string SelectedFileName { get; private set; }
        public SortedDictionary<string, string> DeckFiles { get; } = new SortedDictionary<string, string>();

        void LateUpdate()
        {
            if (nameInputField.isFocused || !Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
                return;

            if (newDeckPanel.gameObject.activeSelf)
            {
                if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && EventSystem.current.currentSelectedGameObject == null)
                    DoSaveDontOverwrite();
                else if (Input.GetButtonDown(Inputs.New) && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.text = string.Empty;
                else if (Input.GetButtonDown(Inputs.FocusName) && EventSystem.current.currentSelectedGameObject == null)
                    nameInputField.ActivateInputField();
                else if (Input.GetButtonDown(Inputs.FocusText) && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.ActivateInputField();
                else if (Input.GetButtonDown(Inputs.Save) && EventSystem.current.currentSelectedGameObject == null)
                    PasteClipboardIntoText();
                else if (Input.GetButtonDown(Inputs.Delete) && EventSystem.current.currentSelectedGameObject == null)
                    textInputField.text = string.Empty;
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                    HideNewDeckPanel();
            }
            else
            {
                if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && loadFromFileButton.interactable)
                    LoadFromFileAndHide();
                else if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Contains(EventSystem.current.currentSelectedGameObject))
                    EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
                else if (Input.GetButtonDown(Inputs.Sort) && shareFileButton.interactable)
                    Share();
                else if (Input.GetButtonDown(Inputs.New))
                    ShowNewDeckPanel();
                else if (Input.GetButtonDown(Inputs.Delete) && deleteFileButton.interactable)
                    PromptForDeleteFile();
                else if (Input.GetButtonDown(Inputs.Vertical))
                    ScrollToggles(Input.GetAxis(Inputs.Vertical) > 0);
                else if (Input.GetButtonDown(Inputs.Page))
                    ScrollPage(Input.GetAxis(Inputs.Page) < 0);
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                    Hide();
            }
        }

        public void Show(OnDeckLoadedDelegate loadCallback = null, string originalName = null, string originalText = null)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            LoadCallback = loadCallback;
            SelectedFileName = string.Empty;

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
        }

        public void BuildDeckFileSelectionOptions()
        {
            DeckFiles.Clear();
            string[] files = Directory.Exists(CardGameManager.Current.DecksFilePath) ? Directory.GetFiles(CardGameManager.Current.DecksFilePath) : new string[0];
            foreach (string file in files)
                if (GetFileTypeFromPath(file) == CardGameManager.Current.DeckFileType)
                    DeckFiles[GetNameFromPath((file))] = file;

            Rebuild(DeckFiles.Keys.ToList(), SelectFile, SelectedFileName);

            shareFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
            deleteFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
            loadFromFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
        }

        public void SelectFile(Toggle toggle, string deckFileName)
        {
            if (string.IsNullOrEmpty(deckFileName))
            {
                SelectedFileName = string.Empty;
                shareFileButton.interactable = false;
                deleteFileButton.interactable = false;
                loadFromFileButton.interactable = false;
                return;
            }

            if (toggle.isOn)
            {
                SelectedFileName = deckFileName;
                shareFileButton.interactable = true;
                deleteFileButton.interactable = true;
                loadFromFileButton.interactable = true;
            }
            else if (!toggle.group.AnyTogglesOn() && SelectedFileName.Equals(deckFileName))
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
                File.Delete(DeckFiles[SelectedFileName]);
            }
            catch (Exception e)
            {
                Debug.LogError(DeckDeleteErrorMessage + e.Message);
            }
            SelectedFileName = string.Empty;
            BuildDeckFileSelectionOptions();
        }

        public void Share()
        {
            string shareText = GetDeckText();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            (new NativeShare()).SetText(shareText).Share();
#else
            UniClipboard.SetText(shareText);
            CardGameManager.Instance.Messenger.Show(ShareMessage);
#endif
        }

        public string GetDeckText()
        {
            string deckText = string.Empty;
            try
            {
                deckText = File.ReadAllText(DeckFiles[SelectedFileName]);
            }
            catch (Exception e)
            {
                Debug.LogError(DeckLoadErrorMessage + e.Message);
            }
            return deckText;
        }

        public void LoadFromFileAndHide()
        {
            Deck newDeck = Deck.Parse(CardGameManager.Current, SelectedFileName, CardGameManager.Current.DeckFileType, GetDeckText());
            LoadCallback?.Invoke(newDeck);
            ResetCancelButton();
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

        public void ResetCancelButton()
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Hide);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
