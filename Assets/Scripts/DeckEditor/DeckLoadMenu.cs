using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public delegate void OnDeckLoadedDelegate(Deck loadedDeck);

public class DeckLoadMenu : SelectionPanel
{
    public const string DecInstructions = "//On each line, enter:\n//<Quantity> <Card Name>\n//For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
    public const string HsdInstructions = "#Paste the deck string/code here";
    public const string TxtInstructions = "#On each line, enter:\n#<Quantity> <Card Name>\n#For example:\n4 Super Awesome Card\n3 Less Awesome Card I Still Like\n1 Card That Is Situational";
    public const string YdkInstructions = "#On each line, enter <Card Id>\n#Copy/Paste recommended";

    public const string DeletePrompt = "Are you sure you would like to delete this deck?";
    public const string DeckDeleteErrorMessage = "There was an error while attempting to delete the deck: ";
    public const string DeckLoadErrorMessage = "There was an error while loading the deck: ";
    public const string DeckSaveErrorMessage = "There was an error saving the deck to file: ";

    public Button loadCancelButton;
    public Button deleteFileButton;
    public Button loadFromFileButton;

    public RectTransform newDeckPanel;
    public InputField nameInputField;
    public TMPro.TextMeshProUGUI instructionsText;
    public TMPro.TMP_InputField textInputField;
    public Button saveCancelButton;

    public OnDeckLoadedDelegate LoadCallback { get; private set; }
    public string SelectedFileName { get; private set; }
    public Dictionary<string, string> DeckFiles { get; } = new Dictionary<string, string>();

    void Update()
    {
        if (!Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
            return;
        
        if (newDeckPanel.gameObject.activeSelf) {
            if (Input.GetButtonDown(CardIn.SubmitInput) && EventSystem.current.currentSelectedGameObject == null)
                DoSaveDontOverwrite();
            else if (Input.GetButtonDown(CardIn.NewInput) && EventSystem.current.currentSelectedGameObject == null)
                textInputField.text = string.Empty;
            else if (Input.GetButtonDown(CardIn.FocusNameInput) && EventSystem.current.currentSelectedGameObject == null)
                nameInputField.ActivateInputField();
            else if (Input.GetButtonDown(CardIn.FocusTextInput) && EventSystem.current.currentSelectedGameObject == null)
                textInputField.ActivateInputField();
            else if (Input.GetButtonDown(CardIn.SaveInput) && EventSystem.current.currentSelectedGameObject == null)
                PasteClipboardIntoText();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput))
                HideNewDeckPanel();
        } else {
            if (Input.GetButtonDown(CardIn.SubmitInput) && loadFromFileButton.interactable)
                LoadFromFileAndHide();
            else if (Input.GetButtonDown(CardIn.NewInput))
                ShowNewDeckPanel();
            else if (Input.GetButtonDown(CardIn.DeleteInput) && deleteFileButton.interactable)
                PromptForDeleteFile();
            else if (Input.GetButtonDown(CardIn.VerticalInput) && EventSystem.current.currentSelectedGameObject == null)
                EventSystem.SetSelectedGameObject(selectionContent.GetChild(0));
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput))
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
        switch (CardGameManager.Current.DeckFileType) {
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

        loadFromFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
        deleteFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
    }

    public void SelectFile(bool isSelected, string deckFileName)
    {
        if (!isSelected || string.IsNullOrEmpty(deckFileName)) {
            SelectedFileName = string.Empty;
            loadFromFileButton.interactable = false;
            deleteFileButton.interactable = false;
            return;
        }

        bool isDoubleSelect = SelectedFileName.Equals(deckFileName);
        SelectedFileName = deckFileName;

        loadFromFileButton.interactable = true;
        deleteFileButton.interactable = true;

        if(isDoubleSelect)
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
        try {
            File.Delete(DeckFiles[SelectedFileName]);
        } catch (Exception e) {
            Debug.LogError(DeckDeleteErrorMessage + e.Message);
        }
        SelectedFileName = string.Empty;
        BuildDeckFileSelectionOptions();
    }

    public void LoadFromFileAndHide()
    {
        string deckText = string.Empty;
        try {
            deckText = File.ReadAllText(DeckFiles[SelectedFileName]);
        } catch (Exception e) {
            Debug.LogError(DeckLoadErrorMessage + e.Message);
        }

        Deck newDeck = Deck.Parse(SelectedFileName, CardGameManager.Current.DeckFileType, deckText);
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

    public void PasteClipboardIntoText()
    {
        textInputField.text = UniClipboard.GetText();
    }

    public void DoSaveDontOverwrite()
    {
        Deck filePathFinder = new Deck(nameInputField.text, CardGameManager.Current.DeckFileType);
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
        Deck filePathFinder = new Deck(nameInputField.text, CardGameManager.Current.DeckFileType);
        try {
            if (!Directory.Exists(CardGameManager.Current.DecksFilePath))
                Directory.CreateDirectory(CardGameManager.Current.DecksFilePath);
            File.WriteAllText(filePathFinder.FilePath, textInputField.text);
        } catch (Exception e) {
            Debug.LogError(DeckSaveErrorMessage + e.Message);
        }
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
