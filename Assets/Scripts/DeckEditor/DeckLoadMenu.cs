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
    public const string DefaultName = "Untitled";
    public const string SavePrompt = "Would you like to save this deck to file?";

    public RectTransform fileSelectionArea;
    public RectTransform fileSelectionTemplate;
    public Button loadFromFileButton;
    public InputField nameInputField;
    public TMPro.TMP_InputField textInputField;

    public OnDeckLoadedDelegate LoadCallback { get; private set; }

    public NameChangeDelegate NameChangeCallback { get; private set; }

    public string OriginalName { get; private set; }

    public string SelectedFileName { get; private set; }

    public Deck LoadedDeck { get; private set; }

    public void Show(OnDeckLoadedDelegate loadCallback, NameChangeDelegate nameChangeCallback, string originalName = DefaultName)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        LoadCallback = loadCallback;
        NameChangeCallback = nameChangeCallback;
        OriginalName = originalName;
        SelectedFileName = string.Empty;
        string[] files = Directory.Exists(CardGameManager.Current.DecksFilePath) ? Directory.GetFiles(CardGameManager.Current.DecksFilePath) : new string[0];
        List<string> deckFiles = new List<string>();
        foreach (string fileName in files)
            if (fileName.Substring(fileName.LastIndexOf('.') + 1).Equals(CardGameManager.Current.DeckFileType))
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
            UnityAction<bool> valueChange = new UnityAction<bool>(isOn => SelectFileToLoad(isOn, deckFile));
            toggle.onValueChanged.AddListener(valueChange);
            Text labelText = deckFileSelection.GetComponentInChildren<Text>();
            labelText.text = GetNameFromPath(deckFile);
            pos.y -= fileSelectionTemplate.rect.height;
        }
        fileSelectionTemplate.SetParent(fileSelectionArea.parent);
        fileSelectionTemplate.gameObject.SetActive(deckFiles.Count < 1);
        fileSelectionArea.sizeDelta = new Vector2(fileSelectionArea.sizeDelta.x, fileSelectionTemplate.rect.height * deckFiles.Count);

        nameInputField.text = originalName;
    }

    void Update()
    {
        loadFromFileButton.interactable = !string.IsNullOrEmpty(SelectedFileName);
    }

    public void SelectFileToLoad(bool isSelected, string deckFileName)
    {
        if (!isSelected || string.IsNullOrEmpty(deckFileName))
            return;
        
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

    public void LoadFromFileAndHide()
    {
        string deckText = string.Empty;
        try { 
            deckText = File.ReadAllText(SelectedFileName);
        } catch (Exception e) {
            Debug.LogError("Failed to load deck!: " + e.Message);
            CardGameManager.Instance.Popup.Show("There was an error while attempting to read the deck list from file: " + e.Message);
        }

        Deck newDeck = new Deck(GetNameFromPath(SelectedFileName), deckText);
        LoadCallback(newDeck);
        Hide();
    }

    public void ChangeName(string newName)
    {
        nameInputField.text = NameChangeCallback(newName);
    }

    public void PasteClipboardIntoText()
    {
        textInputField.text = UniClipboard.GetText();
    }

    public void LoadFromTextAndHide()
    {
        Deck newDeck = new Deck(nameInputField.text, textInputField.text);
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
        NameChangeCallback(OriginalName);
        Hide();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
