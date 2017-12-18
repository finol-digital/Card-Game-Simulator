using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public delegate void OnDeckLoadedDelegate(Deck loadedDeck);

public class DeckLoadMenu : MonoBehaviour
{
    public const string DeletePrompt = "Are you sure you would like to delete this deck?";
    public const string DeckDeleteErrorMessage = "There was an error while attempting to delete the deck: ";
    public const string DeckLoadErrorMessage = "There was an error while loading the deck: ";
    public const string DeckSaveErrorMessage = "There was an error saving the deck to file: ";

    public RectTransform fileSelectionArea;
    public RectTransform fileSelectionTemplate;
    public Button loadCancelButton;
    public Button deleteFileButton;
    public Button loadFromFileButton;

    public RectTransform newDeckPanel;
    public InputField nameInputField;
    public TMPro.TextMeshProUGUI instructionsText;
    public TMPro.TMP_InputField textInputField;
    public Button saveCancelButton;

    public string SelectedFileName { get; private set; }

    public OnDeckLoadedDelegate LoadCallback { get; private set; }

    public void Show(OnDeckLoadedDelegate loadCallback = null, string originalName = null, string originalText = null)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        SelectedFileName = string.Empty;
        LoadCallback = loadCallback;

        BuildDeckFileSelectionOptions();
        // TODO: REPLACE THIS WITH LANGUAGE LOCALIZATION
        switch (CardGameManager.Current.DeckFileType) {
            case DeckFileType.Dec:
                instructionsText.text = Deck.DecInstructions;
                break;
            case DeckFileType.Hsd:
                instructionsText.text = Deck.HsdInstructions;
                break;
            case DeckFileType.Ydk:
                instructionsText.text = Deck.YdkInstructions;
                break;
            case DeckFileType.Txt:
            default:
                instructionsText.text = Deck.TxtInstructions;
                break;
        }

        nameInputField.text = originalName ?? Deck.DefaultName;
        textInputField.text = originalText ?? string.Empty;
    }

    public void BuildDeckFileSelectionOptions()
    {
        string[] files = Directory.Exists(CardGameManager.Current.DecksFilePath) ? Directory.GetFiles(CardGameManager.Current.DecksFilePath) : new string[0];
        List<string> deckFiles = new List<string>();
        foreach (string fileName in files)
            if (string.Equals(fileName.Substring(fileName.LastIndexOf('.') + 1), CardGameManager.Current.DeckFileType.ToString(), StringComparison.OrdinalIgnoreCase))
                deckFiles.Add(fileName);

        fileSelectionArea.DestroyAllChildren();
        fileSelectionTemplate.SetParent(fileSelectionArea);
        Vector3 pos = fileSelectionTemplate.localPosition;
        pos.y = 0;
        foreach (string deckFile in deckFiles) {
            GameObject deckFileSelection = Instantiate(fileSelectionTemplate.gameObject, fileSelectionArea) as GameObject;
            deckFileSelection.SetActive(true);
            // FIX FOR UNITY BUG SETTING SCALE TO 0 WHEN RESOLUTION=REFERENCE_RESOLUTION(1080p)
            deckFileSelection.transform.localScale = Vector3.one;
            deckFileSelection.transform.localPosition = pos;
            Toggle toggle = deckFileSelection.GetComponent<Toggle>();
            toggle.isOn = false;
            UnityAction<bool> valueChange = new UnityAction<bool>(isOn => SelectFile(isOn, deckFile));
            toggle.onValueChanged.AddListener(valueChange);
            Text labelText = deckFileSelection.GetComponentInChildren<Text>();
            labelText.text = GetNameFromPath(deckFile);
            pos.y -= fileSelectionTemplate.rect.height;
        }

        fileSelectionTemplate.SetParent(fileSelectionArea.parent);
        fileSelectionTemplate.gameObject.SetActive(deckFiles.Count < 1);
        fileSelectionArea.sizeDelta = new Vector2(fileSelectionArea.sizeDelta.x, fileSelectionTemplate.rect.height * deckFiles.Count);

        loadFromFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
        deleteFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
    }

    public void SelectFile(bool isSelected, string deckFileName)
    {
        loadFromFileButton.interactable = !string.IsNullOrEmpty(deckFileName);
        deleteFileButton.interactable = !string.IsNullOrEmpty(deckFileName);

        if (!isSelected || string.IsNullOrEmpty(deckFileName))
            return;
        
        if (deckFileName.Equals(SelectedFileName))
            LoadFromFileAndHide();
        else
            SelectedFileName = deckFileName;
    }

    public string GetNameFromPath(string filePath)
    {
        int startName = filePath.LastIndexOf(Path.DirectorySeparatorChar) + 1;
        int endName = filePath.LastIndexOf('.');
        return filePath.Substring(startName, endName - startName);
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
            File.Delete(SelectedFileName);
        } catch (Exception e) {
            Debug.LogError(DeckDeleteErrorMessage + e.Message);
            CardGameManager.Instance.Messenger.Show(DeckDeleteErrorMessage + e.Message);
        }
        SelectedFileName = string.Empty;
        BuildDeckFileSelectionOptions();
    }

    public void LoadFromFileAndHide()
    {
        string deckText = string.Empty;
        try {
            deckText = File.ReadAllText(SelectedFileName);
        } catch (Exception e) {
            Debug.LogError(DeckLoadErrorMessage + e.Message);
            CardGameManager.Instance.Messenger.Show(DeckLoadErrorMessage + e.Message);
        }

        Deck newDeck = Deck.Parse(GetNameFromPath(SelectedFileName), GetFileTypeFromPath(SelectedFileName), deckText);
        if (LoadCallback != null)
            LoadCallback(newDeck);
        Hide();
    }

    public void ShowNewDeckPanel()
    {
        newDeckPanel.gameObject.SetActive(true);
    }

    public void ValidateDeckName(string name)
    {
        nameInputField.text = UnityExtensionMethods.GetSafeFileName(name);
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
            CardGameManager.Instance.Messenger.Show(DeckSaveErrorMessage + e.Message);
        }
        HideNewDeckPanel();
    }

    public void HideNewDeckPanel()
    {
        newDeckPanel.gameObject.SetActive(false);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
