using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public delegate void OnDeckLoadedDelegate(Deck loadedDeck);

public class DeckLoadMenu : MonoBehaviour
{
    public const string SavePrompt = "Would you like to save this deck to file?";
    public const string DeletePrompt = "Are you sure you would like to delete this deck?";

    public RectTransform fileSelectionArea;
    public RectTransform fileSelectionTemplate;
    public Button loadFromFileButton;
    public Button fileCancelButton;
    public Button deleteFileButton;
    public Button textCancelButton;
    public InputField nameInputField;
    public TMPro.TextMeshProUGUI instructionsText;
    public TMPro.TMP_InputField textInputField;

    public string OriginalName { get; private set; }

    public OnDeckNameChangeDelegate NameChangeCallback { get; private set; }

    public OnDeckLoadedDelegate LoadCallback { get; private set; }

    public string SelectedFileName { get; private set; }

    public Deck LoadedDeck { get; private set; }

    public void Show(string originalName = Deck.DefaultName, OnDeckNameChangeDelegate nameChangeCallback = null, OnDeckLoadedDelegate loadCallback = null)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        OriginalName = originalName;
        NameChangeCallback = nameChangeCallback;
        LoadCallback = loadCallback;
        SelectedFileName = string.Empty;
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
            deckFileSelection.transform.localPosition = pos;
            Toggle toggle = deckFileSelection.GetComponent<Toggle>();
            toggle.isOn = false;
            UnityAction<bool> valueChange = new UnityAction<bool>(isOn => SelectFile(isOn, deckFile));
            toggle.onValueChanged.AddListener(valueChange);
            Text labelText = deckFileSelection.GetComponentInChildren<Text>();
            labelText.text = deckFile.Substring(deckFile.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            pos.y -= fileSelectionTemplate.rect.height;
        }
        fileSelectionTemplate.SetParent(fileSelectionArea.parent);
        fileSelectionTemplate.gameObject.SetActive(deckFiles.Count < 1);
        fileSelectionArea.sizeDelta = new Vector2(fileSelectionArea.sizeDelta.x, fileSelectionTemplate.rect.height * deckFiles.Count);

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

        nameInputField.text = originalName;
    }

    void Update()
    {
        loadFromFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
        deleteFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
    }

    public void SelectFile(bool isSelected, string deckFileName)
    {
        if (!isSelected || string.IsNullOrEmpty(deckFileName))
            return;

        if (NameChangeCallback != null)
            NameChangeCallback(GetNameFromPath(deckFileName));
        if (deckFileName.Equals(SelectedFileName))
            LoadFromFileAndHide();
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

    public void LoadFromFileAndHide()
    {
        string deckText = string.Empty;
        try {
            deckText = File.ReadAllText(SelectedFileName);
        } catch (Exception e) {
            Debug.LogError("Failed to load deck!: " + e.Message);
            CardGameManager.Instance.Popup.Show("There was an error while attempting to read the deck list from file: " + e.Message);
        }

        Deck newDeck = Deck.Parse(GetNameFromPath(SelectedFileName), GetFileTypeFromPath(SelectedFileName), deckText);
        if (LoadCallback != null)
            LoadCallback(newDeck);
        Hide();
    }

    public void PromptForDeleteFile()
    {
        CardGameManager.Instance.Popup.Prompt(DeletePrompt, DeleteFile);
    }

    public void DeleteFile()
    {
        try { 
            File.Delete(SelectedFileName);
            Show(OriginalName, NameChangeCallback, LoadCallback);
        } catch (Exception e) {
            Debug.LogError("Failed to delete deck!: " + e.Message);
            CardGameManager.Instance.Popup.Show("There was an error while attempting to delete the deck: " + e.Message);
        }
    }

    public void ChangeName(string newName)
    {
        if (NameChangeCallback != null)
            newName = NameChangeCallback(newName);
        nameInputField.text = newName;
    }

    public void PasteClipboardIntoText()
    {
        textInputField.text = UniClipboard.GetText();
    }

    public void LoadFromTextAndHide()
    {
        Deck newDeck = Deck.Parse(nameInputField.text, CardGameManager.Current.DeckFileType, textInputField.text);
        if (LoadCallback != null)
            LoadCallback(newDeck);
        LoadedDeck = newDeck;
        PromptForSave();
        Hide();
    }

    public void PromptForSave()
    {
        CardGameManager.Instance.Popup.Prompt(SavePrompt, DoSaveNoOverwrite);
    }

    public void DoSaveNoOverwrite()
    {
        if (LoadedDeck == null || !File.Exists(LoadedDeck.FilePath)) {
            DoSave();
            return;
        }
        CardGameManager.Instance.StartCoroutine(WaitToPromptOverwrite());
    }

    public IEnumerator WaitToPromptOverwrite()
    {
        yield return new WaitForSeconds(0.1f);
        CardGameManager.Instance.Popup.Prompt(DeckSaveMenu.OverWriteDeckPrompt, DoSave);
    }

    public void DoSave()
    {
        DeckSaveMenu.SaveToFile(LoadedDeck);
    }

    public void CancelAndHide()
    {
        if (NameChangeCallback != null)
            NameChangeCallback(OriginalName);
        Hide();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
